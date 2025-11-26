using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using BacklogManager.Domain;
using BacklogManager.Services;

namespace BacklogManager.Views
{
    public partial class AnalyseEmailDemandeWindow : Window
    {
        private const string API_URL = "https://genfactory-ai.analytics.cib.echonet/genai/api/v2/chat/completions";
        private const string MODEL = "gpt-oss-120b";
        private const string TOKEN_KEY = "AgentChatToken";

        private readonly IDatabase _database;
        private readonly AuthenticationService _authService;
        private string _apiToken;

        public Demande DemandeCreee { get; private set; }

        public AnalyseEmailDemandeWindow(IDatabase database, AuthenticationService authService)
        {
            InitializeComponent();
            _database = database;
            _authService = authService;

            // Charger le token API
            _apiToken = BacklogManager.Properties.Settings.Default[TOKEN_KEY]?.ToString()?.Trim();

            InitialiserComboBoxes();
        }

        private void InitialiserComboBoxes()
        {
            // Type
            CmbTypeResult.ItemsSource = Enum.GetValues(typeof(TypeDemande)).Cast<TypeDemande>()
                .Select(t => new { Value = t, Display = FormatTypeDemande(t) });
            CmbTypeResult.DisplayMemberPath = "Display";
            CmbTypeResult.SelectedValuePath = "Value";
            CmbTypeResult.SelectedIndex = 0;

            // Criticité
            CmbCriticiteResult.ItemsSource = Enum.GetValues(typeof(Criticite)).Cast<Criticite>();
            CmbCriticiteResult.SelectedIndex = 0;
        }

        private string FormatTypeDemande(TypeDemande type)
        {
            switch (type)
            {
                case TypeDemande.Run:
                    return "Run";
                case TypeDemande.Dev:
                    return "Dev";
                case TypeDemande.Support:
                    return "Support";
                case TypeDemande.Conges:
                    return "Congés";
                case TypeDemande.NonTravaille:
                    return "Non travaillé";
                default:
                    return type.ToString();
            }
        }

        private async void BtnAnalyser_Click(object sender, RoutedEventArgs e)
        {
            var emailContent = TxtEmail.Text?.Trim();
            
            if (string.IsNullOrWhiteSpace(emailContent))
            {
                MessageBox.Show(
                    "Veuillez coller le contenu d'un email à analyser.",
                    "Email requis",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // Vérifier le token
            if (string.IsNullOrWhiteSpace(_apiToken))
            {
                var result = MessageBox.Show(
                    "Le token API n'est pas configuré.\n\n" +
                    "Pour utiliser l'analyse IA, vous devez configurer votre token dans la section '💬 Chat avec l'IA'.\n\n" +
                    "Voulez-vous continuer sans l'IA (saisie manuelle) ?",
                    "Token API manquant",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                    return;

                // Ouvrir la fenêtre d'édition classique
                var editWindow = new EditionDemandeWindow(_database, _authService);
                editWindow.ShowDialog();
                Close();
                return;
            }

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                BtnAnalyser.IsEnabled = false;
                BtnAnalyser.Content = "⏳ Analyse en cours...";

                // Appeler l'IA
                var analysisResult = await AnalyserEmailAvecIA(emailContent);

                if (analysisResult != null)
                {
                    // Remplir les champs avec les résultats
                    TxtTitreResult.Text = analysisResult.Titre;
                    TxtDescriptionResult.Text = analysisResult.Description;
                    TxtSpecificationsResult.Text = analysisResult.Specifications;
                    TxtContexteResult.Text = analysisResult.ContexteMetier;
                    TxtBeneficesResult.Text = analysisResult.BeneficesAttendus;
                    TxtChiffrageResult.Text = analysisResult.ChiffrageEstimeJours?.ToString("F1") ?? "1.0";

                    // Sélectionner le type
                    var typeItem = CmbTypeResult.Items.Cast<dynamic>()
                        .FirstOrDefault(i => i.Value == analysisResult.Type);
                    if (typeItem != null)
                        CmbTypeResult.SelectedItem = typeItem;

                    // Sélectionner la criticité
                    CmbCriticiteResult.SelectedItem = analysisResult.Criticite;

                    // Afficher les résultats
                    PanelResultats.Visibility = Visibility.Visible;
                    BtnCreerDemande.IsEnabled = true;

                    MessageBox.Show(
                        "✅ Analyse terminée avec succès !\n\n" +
                        "Veuillez vérifier et ajuster les informations si nécessaire avant de créer la demande.",
                        "Succès",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"❌ Erreur lors de l'analyse IA :\n\n{ex.Message}\n\n" +
                    "Vous pouvez créer la demande manuellement via le bouton 'Nouvelle demande' classique.",
                    "Erreur",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
                BtnAnalyser.IsEnabled = true;
                BtnAnalyser.Content = "🤖 Analyser avec l'IA";
            }
        }

        private async System.Threading.Tasks.Task<Demande> AnalyserEmailAvecIA(string emailContent)
        {
            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(90);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiToken}");
                client.DefaultRequestHeaders.Add("Accept", "application/json");

                var systemPrompt = @"Tu es un assistant spécialisé dans l'analyse d'emails pour créer des demandes de projet IT dans un système de gestion de backlog.

**Ton rôle** : Analyser l'email fourni et extraire les informations clés pour pré-remplir une demande.

**Champs à extraire** :
1. **Titre** : Résumé court et clair du sujet (max 100 caractères)
2. **Type** : Dev (développement), Run (exploitation/maintenance), Support (assistance), ou Autre
3. **Criticité** : Basse, Moyenne, Haute, ou Bloquante (selon l'urgence mentionnée)
4. **Description** : Résumé du problème ou de la demande (2-3 phrases)
5. **Spécifications** : Détails techniques (codes, références, contraintes, règles métier)
6. **ContexteMetier** : Contexte business/métier, enjeux, acteurs impliqués
7. **BeneficesAttendus** : Bénéfices, gains, objectifs de la demande
8. **ChiffrageEstime** : Estimation en jours (0.5 à 10 jours selon la complexité)

**Règles d'estimation du chiffrage** :
- Simple (configuration, paramétrage) : 0.5 - 1 jour
- Moyen (développement standard, investigation) : 2 - 3 jours
- Complexe (intégration, multiples systèmes) : 3 - 5 jours
- Très complexe (refonte, architecture) : 5 - 10 jours

**Format de réponse** : JSON strict avec cette structure exacte :
```json
{
  ""Titre"": ""..."",
  ""Type"": ""Dev"",
  ""Criticite"": ""Haute"",
  ""Description"": ""..."",
  ""Specifications"": ""..."",
  ""ContexteMetier"": ""..."",
  ""BeneficesAttendus"": ""..."",
  ""ChiffrageEstimeJours"": 2.5
}
```

**Type** doit être exactement : ""Dev"", ""Run"", ""Support""
**Criticite** doit être exactement : ""Basse"", ""Moyenne"", ""Haute"", ""Bloquante""

Analyse maintenant cet email et réponds UNIQUEMENT avec le JSON (pas de texte avant/après) :";

                var messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = $"Email à analyser :\n\n{emailContent}" }
                };

                var requestBody = new
                {
                    model = MODEL,
                    messages = messages,
                    temperature = 0.3, // Plus bas pour des résultats plus déterministes
                    max_tokens = 1000
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(API_URL, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Erreur API {(int)response.StatusCode}: {errorContent}");
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                var jsonResponse = JsonDocument.Parse(responseBody);

                var aiResponse = jsonResponse.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                // Parser la réponse JSON de l'IA
                return ParseAIResponse(aiResponse);
            }
        }

        private Demande ParseAIResponse(string aiResponse)
        {
            try
            {
                // Nettoyer la réponse (enlever les markdown code blocks si présents)
                aiResponse = aiResponse.Trim();
                if (aiResponse.StartsWith("```json"))
                    aiResponse = aiResponse.Substring(7);
                if (aiResponse.StartsWith("```"))
                    aiResponse = aiResponse.Substring(3);
                if (aiResponse.EndsWith("```"))
                    aiResponse = aiResponse.Substring(0, aiResponse.Length - 3);
                aiResponse = aiResponse.Trim();

                using (var doc = JsonDocument.Parse(aiResponse))
                {
                    var root = doc.RootElement;

                    var demande = new Demande
                    {
                        Titre = root.GetProperty("Titre").GetString(),
                        Description = root.GetProperty("Description").GetString(),
                        Specifications = root.GetProperty("Specifications").GetString(),
                        ContexteMetier = root.GetProperty("ContexteMetier").GetString(),
                        BeneficesAttendus = root.GetProperty("BeneficesAttendus").GetString(),
                        ChiffrageEstimeJours = root.GetProperty("ChiffrageEstimeJours").GetDouble(),
                        Type = ParseType(root.GetProperty("Type").GetString()),
                        Criticite = ParseCriticite(root.GetProperty("Criticite").GetString()),
                        DateCreation = DateTime.Now,
                        Statut = StatutDemande.EnAttenteSpecification,
                        DemandeurId = _authService.CurrentUser?.Id ?? 0
                    };

                    return demande;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Erreur lors du parsing de la réponse IA: {ex.Message}\n\nRéponse brute: {aiResponse}");
            }
        }

        private TypeDemande ParseType(string type)
        {
            switch (type?.ToLower())
            {
                case "dev": return TypeDemande.Dev;
                case "run": return TypeDemande.Run;
                case "support": return TypeDemande.Support;
                default: return TypeDemande.Dev;
            }
        }

        private Criticite ParseCriticite(string criticite)
        {
            switch (criticite?.ToLower())
            {
                case "basse": return Criticite.Basse;
                case "moyenne": return Criticite.Moyenne;
                case "haute": return Criticite.Haute;
                case "bloquante": return Criticite.Bloquante;
                default: return Criticite.Moyenne;
            }
        }

        private void BtnCreerDemande_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Valider les champs obligatoires
                if (string.IsNullOrWhiteSpace(TxtTitreResult.Text))
                {
                    MessageBox.Show("Le titre est obligatoire.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Mouse.OverrideCursor = Cursors.Wait;

                // Créer la demande avec les valeurs modifiées par l'utilisateur
                DemandeCreee = new Demande
                {
                    Titre = TxtTitreResult.Text.Trim(),
                    Description = TxtDescriptionResult.Text?.Trim(),
                    Specifications = TxtSpecificationsResult.Text?.Trim(),
                    ContexteMetier = TxtContexteResult.Text?.Trim(),
                    BeneficesAttendus = TxtBeneficesResult.Text?.Trim(),
                    Type = (TypeDemande)CmbTypeResult.SelectedValue,
                    Criticite = (Criticite)CmbCriticiteResult.SelectedValue,
                    DateCreation = DateTime.Now,
                    Statut = StatutDemande.EnAttenteSpecification,
                    DemandeurId = _authService.CurrentUser?.Id ?? 0,
                    EstArchivee = false
                };

                // Parser le chiffrage
                if (double.TryParse(TxtChiffrageResult.Text, out double chiffrage))
                {
                    DemandeCreee.ChiffrageEstimeJours = chiffrage;
                }

                // Enregistrer dans la base de données
                DemandeCreee = _database.AddOrUpdateDemande(DemandeCreee);

                MessageBox.Show(
                    $"✅ Demande créée avec succès !\n\n" +
                    $"Titre : {DemandeCreee.Titre}\n" +
                    $"Type : {DemandeCreee.Type}\n" +
                    $"Criticité : {DemandeCreee.Criticite}\n\n" +
                    $"Statut : En attente de spécification\n\n" +
                    $"Le processus de validation habituel (Business Analyst, Chef de Projet, chiffrage) s'appliquera normalement.",
                    "Succès",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erreur lors de la création de la demande :\n\n{ex.Message}",
                    "Erreur",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void BtnAnnuler_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}

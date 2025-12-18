using System;
using System.Collections.Generic;
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
        private List<string> _champsIncertains = new List<string>();

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
            
            // Utilisateurs - initialisation vide, sera rempli par FiltrerUtilisateursParEquipes
            CmbBusinessAnalystResult.ItemsSource = new[] { new { Id = 0, Nom = "Non assigné" } };
            CmbBusinessAnalystResult.DisplayMemberPath = "Nom";
            CmbBusinessAnalystResult.SelectedValuePath = "Id";
            CmbBusinessAnalystResult.SelectedIndex = 0;

            CmbDevChiffreurResult.ItemsSource = new[] { new { Id = 0, Nom = "Non assigné" } };
            CmbDevChiffreurResult.DisplayMemberPath = "Nom";
            CmbDevChiffreurResult.SelectedValuePath = "Id";
            CmbDevChiffreurResult.SelectedIndex = 0;
            
            // PHASE 2: Programmes
            var programmes = _database.GetAllProgrammes().Where(p => p.Actif).ToList();
            var programmesCombo = programmes.Select(p => new { Id = p.Id, Display = string.Format("{0} - {1}", p.Code, p.Nom) }).ToList();
            programmesCombo.Insert(0, new { Id = 0, Display = "-- Aucun programme --" });
            CmbProgrammeResult.ItemsSource = programmesCombo;
            CmbProgrammeResult.DisplayMemberPath = "Display";
            CmbProgrammeResult.SelectedValuePath = "Id";
            CmbProgrammeResult.SelectedIndex = 0;
            
            // PHASE 2: Priorité
            var priorites = new[] { "Top High", "High", "Medium", "Low" };
            CmbPrioriteResult.ItemsSource = priorites;
            CmbPrioriteResult.SelectedIndex = 2; // Medium par défaut
            
            // PHASE 2: Type Projet
            var typesProjets = new[] { "Data", "Digital", "Regulatory", "Run", "Transformation", "" };
            CmbTypeProjetResult.ItemsSource = typesProjets;
            CmbTypeProjetResult.SelectedIndex = 5; // Vide par défaut
            
            // PHASE 2: Catégorie
            var categories = new[] { "BAU", "TRANSFO", "" };
            CmbCategorieResult.ItemsSource = categories;
            CmbCategorieResult.SelectedIndex = 2; // Vide par défaut
            
            // PHASE 2: Lead Projet
            var leads = new[] { "GTTO", "CCI", "Autre", "" };
            CmbLeadProjetResult.ItemsSource = leads;
            CmbLeadProjetResult.SelectedIndex = 3; // Vide par défaut
            
            // PHASE 2: Ambition
            var ambitions = new[] { "Automation Rate Increase", "Pricing Alignment", "Workload Gain", "Workload Reduction", "N/A", "" };
            CmbAmbitionResult.ItemsSource = ambitions;
            CmbAmbitionResult.SelectedIndex = 5; // Vide par défaut
            
            // PHASE 2: Équipes (multi-sélection via CheckBoxes dynamiques)
            var equipes = _database.GetAllEquipes().Where(e => e.Actif).ToList();
            PanelEquipesResult.Children.Clear();
            foreach (var equipe in equipes)
            {
                var chk = new System.Windows.Controls.CheckBox
                {
                    Content = equipe.Nom,
                    Tag = equipe.Id,
                    Margin = new Thickness(0, 3, 0, 3)
                };
                // Ajouter un gestionnaire pour filtrer les BA/Dev quand une équipe est cochée/décochée
                chk.Checked += (s, e) => FiltrerUtilisateursParEquipes();
                chk.Unchecked += (s, e) => FiltrerUtilisateursParEquipes();
                PanelEquipesResult.Children.Add(chk);
            }
            
            // Initialiser les managers
            MettreAJourManagersResult();
        }
        
        private void FiltrerUtilisateursParEquipes()
        {
            // Récupérer les équipes sélectionnées
            var equipesSelectionnees = new List<int>();
            foreach (System.Windows.Controls.CheckBox chk in PanelEquipesResult.Children)
            {
                if (chk.IsChecked == true && chk.Tag is int equipeId)
                {
                    equipesSelectionnees.Add(equipeId);
                }
            }

            var utilisateurs = _database.GetUtilisateurs().Where(u => u.Actif).ToList();
            var roles = _database.GetRoles();
            
            // Mettre à jour les managers
            MettreAJourManagersResult();

            // Si aucune équipe sélectionnée, afficher tous les utilisateurs
            if (equipesSelectionnees.Count == 0)
            {
                // Business Analysts - tous
                var tousBas = utilisateurs.Where(u =>
                {
                    var role = roles.FirstOrDefault(r => r.Id == u.RoleId);
                    return role?.Type == RoleType.BusinessAnalyst;
                }).Select(u => new { Id = u.Id, Nom = string.Format("{0} {1}", u.Prenom, u.Nom) }).ToList();
                tousBas.Insert(0, new { Id = 0, Nom = "Non assigné" });
                
                var selectedBaId = CmbBusinessAnalystResult.SelectedValue;
                CmbBusinessAnalystResult.ItemsSource = tousBas;
                CmbBusinessAnalystResult.SelectedValue = selectedBaId;

                // Développeurs - tous
                var tousDevs = utilisateurs.Where(u =>
                {
                    var role = roles.FirstOrDefault(r => r.Id == u.RoleId);
                    return role?.Type == RoleType.Developpeur;
                }).Select(u => new { Id = u.Id, Nom = string.Format("{0} {1}", u.Prenom, u.Nom) }).ToList();
                tousDevs.Insert(0, new { Id = 0, Nom = "Non assigné" });
                
                var selectedDevId = CmbDevChiffreurResult.SelectedValue;
                CmbDevChiffreurResult.ItemsSource = tousDevs;
                CmbDevChiffreurResult.SelectedValue = selectedDevId;
            }
            else
            {
                // Filtrer les BA par équipes sélectionnées
                var basFiltres = utilisateurs.Where(u =>
                {
                    var role = roles.FirstOrDefault(r => r.Id == u.RoleId);
                    return role?.Type == RoleType.BusinessAnalyst && 
                           u.EquipeId.HasValue && 
                           equipesSelectionnees.Contains(u.EquipeId.Value);
                }).Select(u => new { Id = u.Id, Nom = string.Format("{0} {1}", u.Prenom, u.Nom) }).ToList();
                basFiltres.Insert(0, new { Id = 0, Nom = "Non assigné" });
                
                var selectedBaId = CmbBusinessAnalystResult.SelectedValue;
                CmbBusinessAnalystResult.ItemsSource = basFiltres;
                if (selectedBaId != null && basFiltres.Any(b => b.Id == (int)selectedBaId))
                    CmbBusinessAnalystResult.SelectedValue = selectedBaId;
                else
                    CmbBusinessAnalystResult.SelectedIndex = 0;

                // Filtrer les Devs par équipes sélectionnées
                var devsFiltres = utilisateurs.Where(u =>
                {
                    var role = roles.FirstOrDefault(r => r.Id == u.RoleId);
                    return role?.Type == RoleType.Developpeur && 
                           u.EquipeId.HasValue && 
                           equipesSelectionnees.Contains(u.EquipeId.Value);
                }).Select(u => new { Id = u.Id, Nom = string.Format("{0} {1}", u.Prenom, u.Nom) }).ToList();
                devsFiltres.Insert(0, new { Id = 0, Nom = "Non assigné" });
                
                var selectedDevId = CmbDevChiffreurResult.SelectedValue;
                CmbDevChiffreurResult.ItemsSource = devsFiltres;
                if (selectedDevId != null && devsFiltres.Any(d => d.Id == (int)selectedDevId))
                    CmbDevChiffreurResult.SelectedValue = selectedDevId;
                else
                    CmbDevChiffreurResult.SelectedIndex = 0;
            }
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
                    // Remplir les champs basiques avec les résultats (sécurisé)
                    TxtTitreResult.Text = analysisResult.Titre ?? "";
                    TxtDescriptionResult.Text = analysisResult.Description ?? "";
                    TxtSpecificationsResult.Text = analysisResult.Specifications ?? "";
                    TxtContexteResult.Text = analysisResult.ContexteMetier ?? "";
                    TxtBeneficesResult.Text = analysisResult.BeneficesAttendus ?? "";
                    TxtChiffrageResult.Text = analysisResult.ChiffrageEstimeJours?.ToString("F1") ?? "0";

                    // Sélectionner le type (sécurisé)
                    try
                    {
                        var typeItem = CmbTypeResult.Items.Cast<dynamic>()
                            .FirstOrDefault(i => i.Value == analysisResult.Type);
                        if (typeItem != null)
                            CmbTypeResult.SelectedItem = typeItem;
                    }
                    catch { }

                    // Sélectionner la criticité (sécurisé)
                    try
                    {
                        CmbCriticiteResult.SelectedItem = analysisResult.Criticite;
                    }
                    catch { }
                    
                    // PHASE 2: Remplir les nouveaux champs
                    if (!string.IsNullOrEmpty(analysisResult.Priorite))
                        CmbPrioriteResult.SelectedItem = analysisResult.Priorite;
                    
                    if (!string.IsNullOrEmpty(analysisResult.TypeProjet))
                        CmbTypeProjetResult.SelectedItem = analysisResult.TypeProjet;
                    
                    if (!string.IsNullOrEmpty(analysisResult.Categorie))
                        CmbCategorieResult.SelectedItem = analysisResult.Categorie;
                    
                    if (!string.IsNullOrEmpty(analysisResult.LeadProjet))
                        CmbLeadProjetResult.SelectedItem = analysisResult.LeadProjet;
                    
                    if (!string.IsNullOrEmpty(analysisResult.Ambition))
                        CmbAmbitionResult.SelectedItem = analysisResult.Ambition;
                    
                    ChkEstImplementeResult.IsChecked = analysisResult.EstImplemente;
                    
                    TxtGainsTempsResult.Text = analysisResult.GainsTemps ?? "";
                    TxtGainsFinanciersResult.Text = analysisResult.GainsFinanciers ?? "";
                    
                    // Drivers
                    if (!string.IsNullOrEmpty(analysisResult.Drivers))
                    {
                        try
                        {
                            var drivers = System.Text.Json.JsonSerializer.Deserialize<List<string>>(analysisResult.Drivers);
                            if (drivers != null)
                            {
                                ChkDriverAutomationResult.IsChecked = drivers.Contains("Automation");
                                ChkDriverEfficiencyResult.IsChecked = drivers.Contains("Efficiency Gains");
                                ChkDriverOptimizationResult.IsChecked = drivers.Contains("Process Optimization");
                                ChkDriverStandardizationResult.IsChecked = drivers.Contains("Standardization");
                                ChkDriverAucunResult.IsChecked = drivers.Contains("Aucun");
                            }
                        }
                        catch { }
                    }
                    
                    // Bénéficiaires
                    if (!string.IsNullOrEmpty(analysisResult.Beneficiaires))
                    {
                        try
                        {
                            var beneficiaires = System.Text.Json.JsonSerializer.Deserialize<List<string>>(analysisResult.Beneficiaires);
                            if (beneficiaires != null)
                            {
                                ChkBenefSGIResult.IsChecked = beneficiaires.Contains("SGI");
                                ChkBenefTFSCResult.IsChecked = beneficiaires.Contains("TFSC");
                                ChkBenefTransversalResult.IsChecked = beneficiaires.Contains("Transversal");
                            }
                        }
                        catch { }
                    }

                    // Afficher les résultats
                    PanelResultats.Visibility = Visibility.Visible;
                    BtnCreerDemande.IsEnabled = true;
                    
                    // Mettre en surbrillance les champs à vérifier
                    SurlignerChampsIncertains();

                    // Message avec avertissement sur les champs incertains
                    string message = "✅ Analyse IA terminée !\n\n";
                    
                    if (_champsIncertains.Count > 0)
                    {
                        message += "⚠️ Les champs surlignés en jaune nécessitent une vérification ou doivent être complétés :\n\n";
                        foreach (var champ in _champsIncertains)
                        {
                            string champAffichage = TraduireNomChamp(champ);
                            message += $"  • {champAffichage}\n";
                        }
                        message += "\n";
                    }
                    
                    message += "⚠️ Le chiffrage est laissé à 0 - seul le développeur peut l'estimer.\n" +
                               "N'oubliez pas de sélectionner les équipes assignées et les personnes responsables !";

                    MessageBox.Show(
                        message,
                        _champsIncertains.Count > 0 ? "Analyse réussie - Vérification requise" : "Succès",
                        MessageBoxButton.OK,
                        _champsIncertains.Count > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);
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

**Ton rôle** : Analyser l'email fourni et extraire les informations clés pour pré-remplir une demande complète.

**RÈGLE IMPORTANTE - GESTION DE L'INCERTITUDE** :
Si tu n'es pas certain d'une information ou si elle n'est pas clairement mentionnée dans l'email :
- Pour les champs texte : laisse VIDE (chaîne vide """")
- Pour les enums/choix : laisse VIDE (chaîne vide """")
- Pour les listes : laisse vide []
- NE PAS inventer ou supposer des informations non présentes dans l'email
- AJOUTE tous les champs incertains dans le tableau ""ChampsIncertains""

**Champs à extraire** :
1. **Titre** : Résumé court et clair du sujet (max 100 caractères) - OBLIGATOIRE
2. **Type** : Dev, Run, Support - SI INCERTAIN : ""Dev""
3. **Criticite** : Basse, Moyenne, Haute, Bloquante - SI INCERTAIN : ""Moyenne""
4. **Priorite** : Top High, High, Medium, Low - SI INCERTAIN : ""Medium""
5. **Categorie** : BAU, TRANSFO, ou vide - SI INCERTAIN : laisser VIDE et ajouter dans ChampsIncertains
6. **TypeProjet** : Data, Digital, Regulatory, Run, Transformation, ou vide - SI INCERTAIN : laisser VIDE
7. **LeadProjet** : GTTO, CCI, Autre, ou vide - SI INCERTAIN : laisser VIDE
8. **Ambition** : Automation Rate Increase, Pricing Alignment, Workload Gain, Workload Reduction, N/A, ou vide - SI INCERTAIN : laisser VIDE
9. **Description** : Résumé du problème (2-3 phrases) - OBLIGATOIRE
10. **Specifications** : Détails techniques UNIQUEMENT SI MENTIONNÉS - sinon laisser vide
11. **ContexteMetier** : Contexte business UNIQUEMENT SI MENTIONNÉ - sinon laisser vide
12. **BeneficesAttendus** : Bénéfices UNIQUEMENT SI MENTIONNÉS - sinon laisser vide
13. **GainsTemps** : Gains en temps UNIQUEMENT SI MENTIONNÉS (ex: '15h/semaine') - sinon vide
14. **GainsFinanciers** : Gains financiers UNIQUEMENT SI MENTIONNÉS (ex: '45000€/an') - sinon vide
15. **Drivers** : Liste vide [] SI INCERTAIN
16. **Beneficiaires** : Liste vide [] SI INCERTAIN
17. **EstImplemente** : false par défaut
18. **ChampsIncertains** : OBLIGATOIRE - Liste des noms de champs incertains ou non mentionnés

**IMPORTANT** : NE PAS estimer le chiffrage. Laisse ChiffrageEstimeJours à 0.

**Format de réponse** : JSON strict avec cette structure exacte :
```json
{
  ""Titre"": ""..."",
  ""Type"": ""Dev"",
  ""Criticite"": ""Moyenne"",
  ""Priorite"": ""Medium"",
  ""Categorie"": """",
  ""TypeProjet"": """",
  ""LeadProjet"": """",
  ""Ambition"": """",
  ""Description"": ""..."",
  ""Specifications"": """",
  ""ContexteMetier"": """",
  ""BeneficesAttendus"": """",
  ""GainsTemps"": """",
  ""GainsFinanciers"": """",
  ""Drivers"": [],
  ""Beneficiaires"": [],
  ""EstImplemente"": false,
  ""ChiffrageEstimeJours"": 0,
  ""ChampsIncertains"": [""Categorie"", ""TypeProjet"", ""Beneficiaires"", ""GainsTemps""]
}
```

**Valeurs exactes attendues** :
- Type: ""Dev"", ""Run"", ""Support""
- Criticite: ""Basse"", ""Moyenne"", ""Haute"", ""Bloquante""
- Priorite: ""Top High"", ""High"", ""Medium"", ""Low""
- Categorie: ""BAU"", ""TRANSFO"", """"
- TypeProjet: ""Data"", ""Digital"", ""Regulatory"", ""Run"", ""Transformation"", """"
- LeadProjet: ""GTTO"", ""CCI"", ""Autre"", """"
- Ambition: ""Automation Rate Increase"", ""Pricing Alignment"", ""Workload Gain"", ""Workload Reduction"", ""N/A"", """"
- Drivers: tableau avec ""Automation"", ""Efficiency Gains"", ""Process Optimization"", ""Standardization"", ""Aucun""
- Beneficiaires: tableau avec ""SGI"", ""TFSC"", ""Transversal""
- ChampsIncertains: tableau avec les noms des champs (""Categorie"", ""TypeProjet"", ""Drivers"", etc.)

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
                
                // Parser la réponse de l'API de manière sécurisée
                string aiResponse = null;
                try
                {
                    var jsonResponse = JsonDocument.Parse(responseBody);
                    aiResponse = jsonResponse.RootElement
                        .GetProperty("choices")[0]
                        .GetProperty("message")
                        .GetProperty("content")
                        .GetString();
                }
                catch (Exception ex)
                {
                    throw new Exception($"Format de réponse API invalide: {ex.Message}\n\nRéponse brute: {responseBody}");
                }

                if (string.IsNullOrWhiteSpace(aiResponse))
                {
                    throw new Exception("L'API a retourné une réponse vide.");
                }

                // Parser la réponse JSON de l'IA
                return ParseAIResponse(aiResponse);
            }
        }

        private Demande ParseAIResponse(string aiResponse)
        {
            _champsIncertains.Clear(); // Réinitialiser la liste

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

                // Si le JSON est incomplet, essayer de le compléter
                if (!aiResponse.EndsWith("}"))
                {
                    aiResponse += "}";
                }

                using (var doc = JsonDocument.Parse(aiResponse))
                {
                    var root = doc.RootElement;

                    var demande = new Demande
                    {
                        Titre = TryGetString(root, "Titre", ""),
                        Description = TryGetString(root, "Description", ""),
                        Specifications = TryGetString(root, "Specifications", ""),
                        ContexteMetier = TryGetString(root, "ContexteMetier", ""),
                        BeneficesAttendus = TryGetString(root, "BeneficesAttendus", ""),
                        ChiffrageEstimeJours = 0, // Laissé à 0 - le dev chiffrera lui-même
                        Type = ParseType(TryGetString(root, "Type", "Dev")),
                        Criticite = ParseCriticite(TryGetString(root, "Criticite", "Moyenne")),
                        DateCreation = DateTime.Now,
                        Statut = StatutDemande.EnAttenteSpecification,
                        DemandeurId = _authService.CurrentUser?.Id ?? 0,
                        
                        // PHASE 2: Nouveaux champs
                        Priorite = TryGetString(root, "Priorite", "Medium"),
                        Categorie = TryGetString(root, "Categorie", ""),
                        TypeProjet = TryGetString(root, "TypeProjet", ""),
                        LeadProjet = TryGetString(root, "LeadProjet", ""),
                        Ambition = TryGetString(root, "Ambition", ""),
                        GainsTemps = TryGetString(root, "GainsTemps", ""),
                        GainsFinanciers = TryGetString(root, "GainsFinanciers", ""),
                        EstImplemente = TryGetBoolean(root, "EstImplemente", false)
                    };
                    
                    // Marquer les champs vides comme incertains
                    if (string.IsNullOrWhiteSpace(demande.Titre)) _champsIncertains.Add("Titre");
                    if (string.IsNullOrWhiteSpace(demande.Description)) _champsIncertains.Add("Description");
                    if (string.IsNullOrWhiteSpace(demande.Categorie)) _champsIncertains.Add("Categorie");
                    if (string.IsNullOrWhiteSpace(demande.TypeProjet)) _champsIncertains.Add("TypeProjet");
                    
                    // Drivers (array -> JSON string) - sécurisé
                    try
                    {
                        if (root.TryGetProperty("Drivers", out var driversElement) && driversElement.ValueKind == JsonValueKind.Array)
                        {
                            var driversList = new List<string>();
                            foreach (var driver in driversElement.EnumerateArray())
                            {
                                try
                                {
                                    var driverValue = driver.GetString();
                                    if (!string.IsNullOrEmpty(driverValue))
                                        driversList.Add(driverValue);
                                }
                                catch { }
                            }
                            demande.Drivers = driversList.Count > 0 ? System.Text.Json.JsonSerializer.Serialize(driversList) : null;
                        }
                    }
                    catch { }
                    
                    // Bénéficiaires (array -> JSON string) - sécurisé
                    try
                    {
                        if (root.TryGetProperty("Beneficiaires", out var benefElement) && benefElement.ValueKind == JsonValueKind.Array)
                        {
                            var benefList = new List<string>();
                            foreach (var benef in benefElement.EnumerateArray())
                            {
                                try
                                {
                                    var benefValue = benef.GetString();
                                    if (!string.IsNullOrEmpty(benefValue))
                                        benefList.Add(benefValue);
                                }
                                catch { }
                            }
                            demande.Beneficiaires = benefList.Count > 0 ? System.Text.Json.JsonSerializer.Serialize(benefList) : null;
                        }
                    }
                    catch { }

                    // Récupération des champs incertains signalés par l'IA - sécurisé
                    try
                    {
                        if (root.TryGetProperty("ChampsIncertains", out var champsIncertainsElement) && champsIncertainsElement.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var champ in champsIncertainsElement.EnumerateArray())
                            {
                                try
                                {
                                    string champNom = champ.GetString();
                                    if (!string.IsNullOrEmpty(champNom) && !_champsIncertains.Contains(champNom))
                                    {
                                        _champsIncertains.Add(champNom);
                                    }
                                }
                                catch { }
                            }
                        }
                    }
                    catch { }

                    return demande;
                }
            }
            catch (Exception ex)
            {
                // En cas d'erreur, retourner une demande vide avec tous les champs à compléter
                MessageBox.Show($"L'IA n'a pas pu analyser complètement l'email.\nVeuillez remplir les champs manuellement.\n\nDétail: {ex.Message}", 
                    "Analyse partielle", MessageBoxButton.OK, MessageBoxImage.Information);
                
                _champsIncertains.AddRange(new[] { "Titre", "Description", "Type", "Criticite", "ContexteMetier", 
                    "BeneficesAttendus", "Categorie", "TypeProjet", "Priorite" });
                
                return new Demande
                {
                    Titre = "",
                    Description = "",
                    Type = TypeDemande.Dev,
                    Criticite = Criticite.Moyenne,
                    DateCreation = DateTime.Now,
                    Statut = StatutDemande.EnAttenteSpecification,
                    DemandeurId = _authService.CurrentUser?.Id ?? 0
                };
            }
        }

        private string TryGetString(JsonElement root, string propertyName, string defaultValue)
        {
            try
            {
                if (root.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String)
                {
                    var value = prop.GetString();
                    return string.IsNullOrWhiteSpace(value) ? defaultValue : value;
                }
            }
            catch { }
            return defaultValue;
        }

        private bool TryGetBoolean(JsonElement root, string propertyName, bool defaultValue)
        {
            try
            {
                if (root.TryGetProperty(propertyName, out var prop))
                {
                    if (prop.ValueKind == JsonValueKind.True) return true;
                    if (prop.ValueKind == JsonValueKind.False) return false;
                    // Si c'est une string "true" ou "false"
                    if (prop.ValueKind == JsonValueKind.String)
                    {
                        var strValue = prop.GetString()?.ToLower();
                        if (strValue == "true") return true;
                        if (strValue == "false") return false;
                    }
                }
            }
            catch { }
            return defaultValue;
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

                // Créer la demande avec toutes les valeurs
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
                
                // PHASE 2: Enregistrer les nouveaux champs
                var progId = (int)CmbProgrammeResult.SelectedValue;
                DemandeCreee.ProgrammeId = progId != 0 ? (int?)progId : null;
                
                DemandeCreee.Priorite = CmbPrioriteResult.SelectedItem?.ToString();
                DemandeCreee.TypeProjet = CmbTypeProjetResult.SelectedItem?.ToString();
                DemandeCreee.Categorie = CmbCategorieResult.SelectedItem?.ToString();
                DemandeCreee.LeadProjet = CmbLeadProjetResult.SelectedItem?.ToString();
                DemandeCreee.Ambition = CmbAmbitionResult.SelectedItem?.ToString();
                DemandeCreee.EstImplemente = ChkEstImplementeResult.IsChecked == true;
                
                DemandeCreee.GainsTemps = TxtGainsTempsResult.Text?.Trim();
                DemandeCreee.GainsFinanciers = TxtGainsFinanciersResult.Text?.Trim();
                
                // Assignations
                var baId = (int)CmbBusinessAnalystResult.SelectedValue;
                DemandeCreee.BusinessAnalystId = baId != 0 ? (int?)baId : null;

                var devId = (int)CmbDevChiffreurResult.SelectedValue;
                DemandeCreee.DevChiffreurId = devId != 0 ? (int?)devId : null;
                
                // Drivers (multi-sélection -> JSON)
                var driversSelectionnes = new List<string>();
                if (ChkDriverAutomationResult.IsChecked == true) driversSelectionnes.Add("Automation");
                if (ChkDriverEfficiencyResult.IsChecked == true) driversSelectionnes.Add("Efficiency Gains");
                if (ChkDriverOptimizationResult.IsChecked == true) driversSelectionnes.Add("Process Optimization");
                if (ChkDriverStandardizationResult.IsChecked == true) driversSelectionnes.Add("Standardization");
                if (ChkDriverAucunResult.IsChecked == true) driversSelectionnes.Add("Aucun");
                DemandeCreee.Drivers = driversSelectionnes.Count > 0 ? System.Text.Json.JsonSerializer.Serialize(driversSelectionnes) : null;
                
                // Bénéficiaires (multi-sélection -> JSON)
                var beneficiairesSelectionnes = new List<string>();
                if (ChkBenefSGIResult.IsChecked == true) beneficiairesSelectionnes.Add("SGI");
                if (ChkBenefTFSCResult.IsChecked == true) beneficiairesSelectionnes.Add("TFSC");
                if (ChkBenefTransversalResult.IsChecked == true) beneficiairesSelectionnes.Add("Transversal");
                DemandeCreee.Beneficiaires = beneficiairesSelectionnes.Count > 0 ? System.Text.Json.JsonSerializer.Serialize(beneficiairesSelectionnes) : null;
                
                // Équipes Assignées (multi-sélection -> List<int>)
                var equipesSelectionnees = new List<int>();
                foreach (System.Windows.Controls.CheckBox chk in PanelEquipesResult.Children)
                {
                    if (chk.IsChecked == true && chk.Tag is int equipeId)
                    {
                        equipesSelectionnees.Add(equipeId);
                    }
                }
                DemandeCreee.EquipesAssigneesIds = equipesSelectionnees;

                // Enregistrer dans la base de données
                DemandeCreee = _database.AddOrUpdateDemande(DemandeCreee);

                MessageBox.Show(
                    $"✅ Demande créée avec succès !\n\n" +
                    $"Titre : {DemandeCreee.Titre}\n" +
                    $"Type : {DemandeCreee.Type}\n" +
                    $"Criticité : {DemandeCreee.Criticite}\n" +
                    $"Priorité : {DemandeCreee.Priorite}\n\n" +
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

        private string TraduireNomChamp(string nomChampTechnique)
        {
            var traductions = new Dictionary<string, string>
            {
                { "Titre", "Titre" },
                { "Type", "Type" },
                { "Criticite", "Criticité" },
                { "Priorite", "Priorité" },
                { "Categorie", "Catégorie" },
                { "TypeProjet", "Type de projet" },
                { "LeadProjet", "Lead projet" },
                { "Ambition", "Ambition" },
                { "Description", "Description" },
                { "Specifications", "Spécifications" },
                { "ContexteMetier", "Contexte métier" },
                { "BeneficesAttendus", "Bénéfices attendus" },
                { "GainsTemps", "Gains de temps" },
                { "GainsFinanciers", "Gains financiers" },
                { "Drivers", "Drivers" },
                { "Beneficiaires", "Bénéficiaires" },
                { "EstImplemente", "Est déjà implémenté" },
                { "Equipes", "Équipes assignées" },
                { "BusinessAnalyst", "Business Analyst" },
                { "ChefProjet", "Chef de projet" },
                { "DevChiffreur", "Développeur" },
                { "Programme", "Programme" }
            };

            return traductions.ContainsKey(nomChampTechnique) 
                ? traductions[nomChampTechnique] 
                : nomChampTechnique;
        }

        private void MettreAJourManagersResult()
        {
            var equipesSelectionnees = new List<int>();
            foreach (System.Windows.Controls.CheckBox chk in PanelEquipesResult.Children)
            {
                if (chk.IsChecked == true && chk.Tag is int equipeId)
                {
                    equipesSelectionnees.Add(equipeId);
                }
            }
            
            if (equipesSelectionnees.Count == 0)
            {
                TxtManagersResult.Text = "Sélectionnez les équipes pour voir les managers";
                return;
            }
            
            var equipes = _database.GetAllEquipes();
            var utilisateurs = _database.GetUtilisateurs();
            var managers = new List<string>();
            
            foreach (var equipeId in equipesSelectionnees)
            {
                var equipe = equipes.FirstOrDefault(e => e.Id == equipeId);
                if (equipe != null && equipe.ManagerId.HasValue)
                {
                    var manager = utilisateurs.FirstOrDefault(u => u.Id == equipe.ManagerId.Value);
                    if (manager != null)
                    {
                        var managerNom = string.Format("{0} {1}", manager.Prenom, manager.Nom);
                        if (!managers.Contains(managerNom))
                        {
                            managers.Add(managerNom);
                        }
                    }
                }
            }
            
            TxtManagersResult.Text = managers.Count > 0 ? string.Join(", ", managers) : "Aucun manager assigné aux équipes sélectionnées";
        }

        private void SurlignerChampsIncertains()
        {
            var couleurSurbrillance = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 200)); // Jaune pâle
            var couleurNormale = System.Windows.Media.Brushes.White;

            // Titre
            TxtTitreResult.Background = _champsIncertains.Contains("Titre") ? couleurSurbrillance : couleurNormale;
            
            // Description
            TxtDescriptionResult.Background = _champsIncertains.Contains("Description") ? couleurSurbrillance : couleurNormale;
            
            // Spécifications
            TxtSpecificationsResult.Background = _champsIncertains.Contains("Specifications") ? couleurSurbrillance : couleurNormale;
            
            // Contexte Métier
            TxtContexteResult.Background = _champsIncertains.Contains("ContexteMetier") ? couleurSurbrillance : couleurNormale;
            
            // Bénéfices
            TxtBeneficesResult.Background = _champsIncertains.Contains("BeneficesAttendus") ? couleurSurbrillance : couleurNormale;
            
            // Type
            CmbTypeResult.Background = _champsIncertains.Contains("Type") ? couleurSurbrillance : couleurNormale;
            
            // Criticité
            CmbCriticiteResult.Background = _champsIncertains.Contains("Criticite") ? couleurSurbrillance : couleurNormale;
            
            // Priorité
            CmbPrioriteResult.Background = _champsIncertains.Contains("Priorite") ? couleurSurbrillance : couleurNormale;
            
            // Type Projet
            CmbTypeProjetResult.Background = _champsIncertains.Contains("TypeProjet") ? couleurSurbrillance : couleurNormale;
            
            // Catégorie
            CmbCategorieResult.Background = _champsIncertains.Contains("Categorie") ? couleurSurbrillance : couleurNormale;
            
            // Lead Projet
            CmbLeadProjetResult.Background = _champsIncertains.Contains("LeadProjet") ? couleurSurbrillance : couleurNormale;
            
            // Ambition
            CmbAmbitionResult.Background = _champsIncertains.Contains("Ambition") ? couleurSurbrillance : couleurNormale;
            
            // Gains Temps
            TxtGainsTempsResult.Background = _champsIncertains.Contains("GainsTemps") ? couleurSurbrillance : couleurNormale;
            
            // Gains Financiers
            TxtGainsFinanciersResult.Background = _champsIncertains.Contains("GainsFinanciers") ? couleurSurbrillance : couleurNormale;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using BacklogManager.Domain;
using BacklogManager.Converters;
using BacklogManager.Services;

namespace BacklogManager.Views
{
    public partial class AnalyseProgrammeIAWindow : Window
    {
        private const string API_URL = "https://genfactory-ai.analytics.cib.echonet/genai/api/v2/chat/completions";
        private const string MODEL = "gpt-oss-120b";
        
        private readonly Programme _programme;
        private string _apiToken;

        public AnalyseProgrammeIAWindow(Programme programme)
        {
            InitializeComponent();
            
            _programme = programme;
            
            // Initialize localized texts
            InitializeLocalizedTexts();
            
            // Afficher le nom du programme
            TxtNomProgramme.Text = $"{LocalizationService.Instance.GetString("ProgramAIAnalysis_ProgramLabel")} {programme.Nom}";
            
            // Charger le token API
            _apiToken = Properties.Settings.Default.AgentChatToken;
            
            // Générer l'analyse en arrière-plan
            _ = GenererAnalyseAsync();
        }
        
        private void InitializeLocalizedTexts()
        {
            var loc = LocalizationService.Instance;
            
            TxtTitle.Text = loc.GetString("ProgramAIAnalysis_Title");
            TxtAgentName.Text = loc.GetString("ProgramAIAnalysis_AgentName");
            TxtLoading.Text = loc.GetString("ProgramAIAnalysis_Loading");
            TxtLoadingDetails.Text = loc.GetString("ProgramAIAnalysis_LoadingDetails");
            BtnCopier.Content = loc.GetString("ProgramAIAnalysis_Copy");
            BtnClose.Content = loc.GetString("ProgramAIAnalysis_Close");
        }

        private async Task GenererAnalyseAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_apiToken))
                {
                    AfficherErreur(LocalizationService.Instance.GetString("ProgramAIAnalysis_TokenNotConfigured"));
                    return;
                }

                // Récupérer les données du programme
                var database = new SqliteDatabase();
                var backlogService = new BacklogService(database);
                
                var projets = backlogService.GetAllProjets()
                    .Where(p => p.ProgrammeId == _programme.Id && p.Actif)
                    .ToList();
                
                var toutesLesTaches = backlogService.GetAllBacklogItemsIncludingArchived()
                    .Where(t => projets.Any(p => p.Id == t.ProjetId) &&
                                t.TypeDemande != TypeDemande.Conges &&
                                t.TypeDemande != TypeDemande.NonTravaille)
                    .ToList();

                var nbTachesTotal = toutesLesTaches.Count;
                var nbTachesTerminees = toutesLesTaches.Count(t => t.Statut == Statut.Termine || t.EstArchive);
                var pourcentageAvancement = nbTachesTotal > 0 ? (int)((double)nbTachesTerminees / nbTachesTotal * 100) : 0;
                
                var nbGreen = projets.Count(p => p.StatutRAG == "Green");
                var nbAmber = projets.Count(p => p.StatutRAG == "Amber");
                var nbRed = projets.Count(p => p.StatutRAG == "Red");

                // Construire le prompt pour l'IA
                var prompt = $@"Tu es Agent Program Management, expert en gestion de programmes multi-projets et gouvernance de portefeuille.

PROGRAMME ANALYSÉ: {_programme.Nom}
Code: {_programme.Code}
Description: {_programme.Description ?? "Non renseignée"}
Statut global: {_programme.StatutGlobal ?? "Non défini"}
Date début: {_programme.DateDebut?.ToString("dd/MM/yyyy") ?? "Non définie"}
Date fin cible: {_programme.DateFinCible?.ToString("dd/MM/yyyy") ?? "Non définie"}

STATISTIQUES DU PROGRAMME:
- Nombre de projets: {projets.Count}
  • Projets Green (On Track): {nbGreen}
  • Projets Amber (At Risk): {nbAmber}
  • Projets Red (Off Track): {nbRed}
- Total de tâches (tous projets): {nbTachesTotal}
- Tâches terminées: {nbTachesTerminees} ({pourcentageAvancement}%)

PROJETS DU PROGRAMME:
{string.Join("\n", projets.Select(p => 
    $"• {p.Nom} - Statut RAG: {p.StatutRAG ?? "Non défini"} - Date fin: {p.DateFinPrevue?.ToString("dd/MM/yyyy") ?? "Non définie"}"))}

MISSION:
Analyse ce programme et fournis une évaluation stratégique détaillée avec:

1. **ÉTAT GLOBAL** - Vue d'ensemble du programme (3-4 phrases)
   - Santé globale du programme
   - Alignement avec les objectifs
   - Tendances observées

2. **ANALYSE DES RISQUES**
   - Risques identifiés par projet
   - Interdépendances critiques
   - Points de vigilance

3. **RECOMMANDATIONS STRATÉGIQUES**
   - Actions à prendre au niveau du programme
   - Réorganisation éventuelle des ressources
   - Ajustements de gouvernance

4. **PLAN D'ACTION PRIORITAIRE**
   - 5 actions concrètes et prioritaires
   - Focus sur les projets Red et Amber
   - Mesures de mitigation des risques

Sois stratégique, orienté décision et propose des actions concrètes au niveau programme.
Utilise des sections claires avec des titres en MAJUSCULES suivis de deux-points";

                // Appeler l'IA
                var reponse = await AppelerIAAsync(prompt);

                // Afficher les résultats
                Dispatcher.Invoke(() =>
                {
                    PanelChargement.Visibility = Visibility.Collapsed;
                    PanelResultat.Visibility = Visibility.Visible;
                    BtnCopier.Visibility = Visibility.Visible;
                    
                    // Utiliser le convertisseur Markdown pour formatter le texte
                    var converter = new MarkdownToFormattedTextConverter();
                    TxtAnalyse.Document = converter.Convert(reponse, typeof(FlowDocument), null, null) as FlowDocument ?? new FlowDocument();
                });
            }
            catch (Exception ex)
            {
                AfficherErreur(string.Format(LocalizationService.Instance.GetString("ProgramAIAnalysis_AnalysisError"), ex.Message));
            }
        }

        private async Task<string> AppelerIAAsync(string prompt)
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiToken}");

                var requestBody = new
                {
                    model = MODEL,
                    messages = new[]
                    {
                        new { role = "user", content = prompt }
                    },
                    temperature = 0.7,
                    max_tokens = 2000
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(API_URL, content);
                var responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Erreur API: {response.StatusCode} - {responseString}");
                }

                using (var document = JsonDocument.Parse(responseString))
                {
                    var root = document.RootElement;
                    if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                    {
                        var firstChoice = choices[0];
                        if (firstChoice.TryGetProperty("message", out var message) &&
                            message.TryGetProperty("content", out var contentProp))
                        {
                            return contentProp.GetString() ?? "Aucune réponse de l'IA";
                        }
                    }
                }

                return "Réponse IA invalide";
            }
        }

        private void AfficherErreur(string message)
        {
            Dispatcher.Invoke(() =>
            {
                PanelChargement.Visibility = Visibility.Collapsed;
                PanelResultat.Visibility = Visibility.Visible;
                
                var errorDoc = new FlowDocument();
                errorDoc.Blocks.Add(new Paragraph(new Run($"❌ {message}") { Foreground = System.Windows.Media.Brushes.Red }));
                TxtAnalyse.Document = errorDoc;
            });
        }

        private void BtnCopier_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Extraire le texte du FlowDocument
                var textRange = new TextRange(TxtAnalyse.Document.ContentStart, TxtAnalyse.Document.ContentEnd);
                Clipboard.SetText(textRange.Text);
                MessageBox.Show(LocalizationService.Instance.GetString("ProgramAIAnalysis_CopiedToClipboard"), 
                    LocalizationService.Instance.GetString("Common_Success"), 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la copie: {ex.Message}", "Erreur", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnFermer_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

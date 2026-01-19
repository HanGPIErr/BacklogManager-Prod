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
    public partial class AnalyseProjetIAWindow : Window
    {
        private const string API_URL = "https://genfactory-ai.analytics.cib.echonet/genai/api/v2/chat/completions";
        private const string MODEL = "gpt-oss-120b";
        
        private readonly Projet _projet;
        private readonly List<BacklogItem> _taches;
        private string _apiToken;

        public AnalyseProjetIAWindow(Projet projet, List<BacklogItem> taches)
        {
            InitializeComponent();
            
            _projet = projet;
            _taches = taches;
            
            // Initialize localized texts
            InitializeLocalizedTexts();
            
            // Afficher le nom du projet
            TxtNomProjet.Text = $"{LocalizationService.Instance.GetString("ProjectAIAnalysis_ProjectLabel")} {projet.Nom}";
            
            // Charger le token API
            _apiToken = Properties.Settings.Default.AgentChatToken;
            
            // Générer l'analyse en arrière-plan
            _ = GenererAnalyseAsync();
        }
        
        private void InitializeLocalizedTexts()
        {
            var loc = LocalizationService.Instance;
            
            TxtTitle.Text = loc.GetString("ProjectAIAnalysis_Title");
            TxtAgentName.Text = loc.GetString("ProjectAIAnalysis_AgentName");
            TxtScoreLabel.Text = loc.GetString("ProjectAIAnalysis_ScoreLabel");
            TxtLoading.Text = loc.GetString("ProjectAIAnalysis_Loading");
            TxtLoadingDetails.Text = loc.GetString("ProjectAIAnalysis_LoadingDetails");
            TxtOverview.Text = loc.GetString("ProjectAIAnalysis_Overview");
            TxtDeadlineAnalysis.Text = loc.GetString("ProjectAIAnalysis_DeadlineAnalysis");
            TxtRecommendations.Text = loc.GetString("ProjectAIAnalysis_Recommendations");
            TxtProposedActions.Text = loc.GetString("ProjectAIAnalysis_ProposedActions");
            BtnClose.Content = loc.GetString("ProjectAIAnalysis_Close");
        }

        private async Task GenererAnalyseAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_apiToken))
                {
                    AfficherErreur(LocalizationService.Instance.GetString("ProjectAIAnalysis_TokenNotConfigured"));
                    return;
                }

                // Analyser les données du projet
                var stats = AnalyserProjet();
                
                // Construire le prompt pour l'IA
                var prompt = $@"Tu es Agent Project & Change, expert en gestion de projet agile et analyse de performance.

PROJET ANALYSÉ: {_projet.Nom}
Description: {_projet.Description ?? "Non renseignée"}

STATISTIQUES DU PROJET:
- Total de tâches: {stats.TotalTaches}
- Tâches terminées: {stats.TachesTerminees} ({stats.PourcentageTermine:F0}%)
- Tâches en cours: {stats.TachesEnCours}
- Tâches en retard: {stats.TachesEnRetard}
- Délai moyen de retard: {stats.DelaiMoyenRetard:F1} jours

{(stats.TachesAvecDeadline.Any() ? 
$@"DEADLINES DES TÂCHES:
{string.Join("\n", stats.TachesAvecDeadline.Select(t => 
    $"• {t.Titre} - Deadline: {t.DateFinAttendue:dd/MM/yyyy} - Statut: {t.Statut}"))}
" : "Aucune deadline configurée")}

{(stats.TachesEnRetardDetail.Any() ? 
$@"TÂCHES EN RETARD:
{string.Join("\n", stats.TachesEnRetardDetail.Select(t => 
    $"• {t.Item1} - Retard: {t.Item2} jours"))}" : "")}

MISSION:
Analyse ce projet et fournis une évaluation détaillée avec:

1. Un SCORE de 0 à 100 basé sur:
   - Respect des deadlines (40%)
   - Avancement du projet (30%)
   - Gestion des retards (30%)

2. Une VUE D'ENSEMBLE (2-3 phrases) sur l'état global du projet

3. Une ANALYSE DES DEADLINES avec identification des risques

4. Des RECOMMANDATIONS concrètes pour améliorer la situation

5. Des ACTIONS PROPOSÉES (3-5 actions prioritaires) pour réajuster le projet si nécessaire

FORMAT DE RÉPONSE:
[SCORE: XX]
[VUE_ENSEMBLE]
Ton analyse en 2-3 phrases sur l'état global
[/VUE_ENSEMBLE]
[DEADLINES]
Ton analyse des deadlines et risques
[/DEADLINES]
[RECOMMANDATIONS]
Tes recommandations détaillées
[/RECOMMANDATIONS]
[ACTIONS]
• Action 1
• Action 2
• Action 3
[/ACTIONS]

Sois constructif, précis et propose des solutions concrètes.";

                // Appeler l'IA
                var reponse = await AppelerIAAsync(prompt);

                // Parser la réponse
                var resultat = ParserReponse(reponse);

                // Afficher les résultats sur le thread UI
                Dispatcher.Invoke(() =>
                {
                    PanelChargement.Visibility = Visibility.Collapsed;
                    PanelResultat.Visibility = Visibility.Visible;
                    
                    if (resultat.Score > 0)
                    {
                        TxtScore.Text = resultat.Score.ToString();
                        BorderScore.Visibility = Visibility.Visible;
                        
                        // Couleur du score
                        if (resultat.Score >= 80)
                            TxtScore.Foreground = new System.Windows.Media.SolidColorBrush(
                                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#4CAF50"));
                        else if (resultat.Score >= 60)
                            TxtScore.Foreground = new System.Windows.Media.SolidColorBrush(
                                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FF9800"));
                        else
                            TxtScore.Foreground = new System.Windows.Media.SolidColorBrush(
                                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#E53935"));
                    }
                    
                    // Utiliser le convertisseur Markdown pour formatter le texte
                    var converter = new MarkdownToFormattedTextConverter();
                    TxtVueEnsemble.Document = converter.Convert(resultat.VueEnsemble, typeof(FlowDocument), null, null) as FlowDocument ?? new FlowDocument();
                    TxtDeadlines.Document = converter.Convert(resultat.Deadlines, typeof(FlowDocument), null, null) as FlowDocument ?? new FlowDocument();
                    TxtRecommandations.Document = converter.Convert(resultat.Recommandations, typeof(FlowDocument), null, null) as FlowDocument ?? new FlowDocument();
                    TxtActions.Document = converter.Convert(resultat.Actions, typeof(FlowDocument), null, null) as FlowDocument ?? new FlowDocument();
                });
            }
            catch (Exception ex)
            {
                AfficherErreur(string.Format(LocalizationService.Instance.GetString("ProjectAIAnalysis_AnalysisError"), ex.Message));
            }
        }

        private class StatistiquesProjet
        {
            public int TotalTaches { get; set; }
            public int TachesTerminees { get; set; }
            public int TachesEnCours { get; set; }
            public int TachesEnRetard { get; set; }
            public double PourcentageTermine { get; set; }
            public double DelaiMoyenRetard { get; set; }
            public List<BacklogItem> TachesAvecDeadline { get; set; }
            public List<(string, int)> TachesEnRetardDetail { get; set; }
        }

        private StatistiquesProjet AnalyserProjet()
        {
            var stats = new StatistiquesProjet
            {
                TotalTaches = _taches.Count,
                TachesTerminees = _taches.Count(t => t.Statut == Statut.Termine),
                TachesEnCours = _taches.Count(t => t.Statut == Statut.EnCours),
                TachesAvecDeadline = _taches.Where(t => t.DateFinAttendue.HasValue).ToList(),
                TachesEnRetardDetail = new List<(string, int)>()
            };

            stats.PourcentageTermine = stats.TotalTaches > 0 
                ? (stats.TachesTerminees * 100.0 / stats.TotalTaches) 
                : 0;

            // Calculer les retards
            var retards = new List<int>();
            var aujourdhui = DateTime.Now.Date;

            foreach (var tache in stats.TachesAvecDeadline)
            {
                if (tache.DateFinAttendue.HasValue)
                {
                    var deadline = tache.DateFinAttendue.Value.Date;
                    
                    if (tache.Statut == Statut.Termine)
                    {
                        // Vérifier si terminé après deadline (à implémenter avec date de fin réelle)
                        // Pour l'instant on considère comme OK si terminé
                    }
                    else if (aujourdhui > deadline)
                    {
                        var joursRetard = (aujourdhui - deadline).Days;
                        retards.Add(joursRetard);
                        stats.TachesEnRetardDetail.Add((tache.Titre, joursRetard));
                    }
                }
            }

            stats.TachesEnRetard = retards.Count;
            stats.DelaiMoyenRetard = retards.Any() ? retards.Average() : 0;

            return stats;
        }

        private class ResultatAnalyse
        {
            public int Score { get; set; }
            public string VueEnsemble { get; set; }
            public string Deadlines { get; set; }
            public string Recommandations { get; set; }
            public string Actions { get; set; }
        }

        private ResultatAnalyse ParserReponse(string reponse)
        {
            var resultat = new ResultatAnalyse();

            try
            {
                // Extraire le score
                if (reponse.Contains("[SCORE:"))
                {
                    var scoreStart = reponse.IndexOf("[SCORE:") + 7;
                    var scoreEnd = reponse.IndexOf("]", scoreStart);
                    if (scoreEnd > scoreStart)
                    {
                        var scoreStr = reponse.Substring(scoreStart, scoreEnd - scoreStart).Trim();
                        int.TryParse(scoreStr, out int score);
                        resultat.Score = score;
                    }
                }

                // Extraire les sections
                resultat.VueEnsemble = ExtraireSection(reponse, "VUE_ENSEMBLE");
                resultat.Deadlines = ExtraireSection(reponse, "DEADLINES");
                resultat.Recommandations = ExtraireSection(reponse, "RECOMMANDATIONS");
                resultat.Actions = ExtraireSection(reponse, "ACTIONS");

                // Si les sections sont vides, utiliser la réponse brute
                if (string.IsNullOrWhiteSpace(resultat.VueEnsemble))
                {
                    resultat.VueEnsemble = reponse;
                }
            }
            catch
            {
                resultat.VueEnsemble = reponse;
            }

            return resultat;
        }

        private string ExtraireSection(string texte, string nomSection)
        {
            var debut = $"[{nomSection}]";
            var fin = $"[/{nomSection}]";

            var indexDebut = texte.IndexOf(debut);
            var indexFin = texte.IndexOf(fin);

            if (indexDebut >= 0 && indexFin > indexDebut)
            {
                indexDebut += debut.Length;
                return texte.Substring(indexDebut, indexFin - indexDebut).Trim();
            }

            return "";
        }

        private async Task<string> AppelerIAAsync(string prompt)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiToken}");
                
                var requestBody = new
                {
                    model = MODEL,
                    messages = new[]
                    {
                        new { role = "system", content = "Tu es Agent Project & Change, expert en gestion de projet." },
                        new { role = "user", content = prompt }
                    },
                    temperature = 0.7
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(API_URL, content);
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync();
                var jsonDoc = JsonDocument.Parse(responseBody);
                
                return jsonDoc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();
            }
        }

        private void AfficherErreur(string message)
        {
            Dispatcher.Invoke(() =>
            {
                PanelChargement.Visibility = Visibility.Collapsed;
                PanelResultat.Visibility = Visibility.Visible;
                
                // Créer un FlowDocument avec le message d'erreur
                var errorDoc = new FlowDocument();
                var errorPara = new Paragraph(new Run($"❌ {message}"))
                {
                    Foreground = new System.Windows.Media.SolidColorBrush(
                        (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#E53935"))
                };
                errorDoc.Blocks.Add(errorPara);
                TxtVueEnsemble.Document = errorDoc;
            });
        }

        private void BtnFermer_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}

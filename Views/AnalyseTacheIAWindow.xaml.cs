using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using BacklogManager.Domain;

namespace BacklogManager.Views
{
    public partial class AnalyseTacheIAWindow : Window
    {
        private const string API_URL = "https://genfactory-ai.analytics.cib.echonet/genai/api/v2/chat/completions";
        private const string MODEL = "gpt-oss-120b";
        
        private readonly BacklogItem _tache;
        private readonly double _tempsReelHeures;
        private readonly double _progressionPourcentage;
        private string _apiToken;

        public AnalyseTacheIAWindow(BacklogItem tache, double tempsReelHeures, double progressionPourcentage)
        {
            InitializeComponent();
            
            _tache = tache;
            _tempsReelHeures = tempsReelHeures;
            _progressionPourcentage = progressionPourcentage;
            
            // Charger le token API
            _apiToken = Properties.Settings.Default.AgentChatToken;
            
            // Afficher le titre
            TxtTitreTache.Text = tache.Titre;
            
            // Générer l'analyse en arrière-plan
            _ = GenererAnalyseAsync();
        }

        private async Task GenererAnalyseAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_apiToken))
                {
                    AfficherErreur("Token API non configuré. Configurez-le dans la section Chat.");
                    return;
                }

                // Construire le prompt pour l'IA
                var chiffrageJours = _tache.ChiffrageHeures.HasValue ? _tache.ChiffrageHeures.Value / 8.0 : 0;
                var tempsReelJours = _tempsReelHeures / 8.0;
                var ecartJours = tempsReelJours - chiffrageJours;
                var ecartPourcentage = chiffrageJours > 0 ? (ecartJours / chiffrageJours) * 100 : 0;
                
                var prompt = $@"Tu es Agent Project & Change, expert en gestion de projet agile et analyse de performance des tâches.

TÂCHE ANALYSÉE: {_tache.Titre}

INFORMATIONS DE LA TÂCHE:
- Description: {_tache.Description ?? "Non renseignée"}
- Type: {_tache.TypeDemande}
- Priorité: {_tache.Priorite}
- Statut: {_tache.Statut}
- Complexité: {(_tache.Complexite.HasValue ? _tache.Complexite.Value.ToString() : "Non évaluée")}
- Date de création: {_tache.DateCreation:dd/MM/yyyy}
- Date de début: {(_tache.DateDebut.HasValue ? _tache.DateDebut.Value.ToString("dd/MM/yyyy") : "Non commencée")}
- Date fin attendue: {(_tache.DateFinAttendue.HasValue ? _tache.DateFinAttendue.Value.ToString("dd/MM/yyyy") : "Non définie")}

MÉTRIQUES DE PERFORMANCE:
- Chiffrage estimé: {chiffrageJours:F1} jours ({_tache.ChiffrageHeures:F1}h)
- Temps réel passé: {tempsReelJours:F1} jours ({_tempsReelHeures:F1}h)
- Écart: {(ecartJours >= 0 ? "+" : "")}{ecartJours:F1} jours ({(ecartPourcentage >= 0 ? "+" : "")}{ecartPourcentage:F0}%)
- Progression: {_progressionPourcentage:F0}%
- Est en retard: {(_tempsReelHeures > (_tache.ChiffrageHeures ?? 0) ? "Oui" : "Non")}

MISSION:
Analyse cette tâche en profondeur et fournis une évaluation détaillée avec:

1. Un SCORE de 0 à 100 basé sur:
   - Respect de l'estimation (40%)
   - Progression par rapport au temps passé (30%)
   - Respect de la deadline si applicable (30%)

2. Un BILAN GÉNÉRAL (2-3 phrases) sur l'état et la performance de cette tâche

3. Une ANALYSE DÉTAILLÉE sur:
   - La cohérence entre le temps passé et la progression
   - Le respect de l'estimation initiale
   - Les éventuels écarts et leurs implications

4. Des POINTS D'ATTENTION spécifiques à surveiller

5. Des RECOMMANDATIONS concrètes pour:
   - Améliorer la performance sur cette tâche
   - Mieux estimer les prochaines tâches similaires
   - Ajuster la planification si nécessaire

6. Des ACTIONS PRIORITAIRES (2-3 actions) à mettre en place immédiatement

FORMAT DE RÉPONSE:
[SCORE: XX]

[BILAN]
Ton bilan en 2-3 phrases
[/BILAN]

[ANALYSE]
Ton analyse détaillée
[/ANALYSE]

[POINTS_ATTENTION]
• Point 1
• Point 2
• Point 3
[/POINTS_ATTENTION]

[RECOMMANDATIONS]
• Recommandation 1
• Recommandation 2
• Recommandation 3
[/RECOMMANDATIONS]

[ACTIONS]
• Action 1
• Action 2
• Action 3
[/ACTIONS]

Sois constructif, précis et propose des solutions concrètes basées sur les données.";

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
                        
                        // Couleur du score
                        if (resultat.Score >= 80)
                            TxtScore.Foreground = new System.Windows.Media.SolidColorBrush(
                                System.Windows.Media.Color.FromRgb(76, 175, 80)); // Vert
                        else if (resultat.Score >= 60)
                            TxtScore.Foreground = new System.Windows.Media.SolidColorBrush(
                                System.Windows.Media.Color.FromRgb(255, 152, 0)); // Orange
                        else
                            TxtScore.Foreground = new System.Windows.Media.SolidColorBrush(
                                System.Windows.Media.Color.FromRgb(229, 57, 53)); // Rouge
                    }

                    TxtBilan.Text = resultat.Bilan;
                    TxtAnalyse.Text = resultat.Analyse;
                    TxtPointsAttention.Text = resultat.PointsAttention;
                    TxtRecommandations.Text = resultat.Recommandations;
                    TxtActions.Text = resultat.Actions;
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    AfficherErreur($"Erreur lors de l'analyse IA :\n{ex.Message}");
                });
            }
        }

        private class ResultatAnalyse
        {
            public int Score { get; set; }
            public string Bilan { get; set; }
            public string Analyse { get; set; }
            public string PointsAttention { get; set; }
            public string Recommandations { get; set; }
            public string Actions { get; set; }
        }

        private ResultatAnalyse ParserReponse(string reponse)
        {
            var resultat = new ResultatAnalyse();

            // Extraire le score
            var scoreMatch = System.Text.RegularExpressions.Regex.Match(reponse, @"\[SCORE:\s*(\d+)\]");
            if (scoreMatch.Success && int.TryParse(scoreMatch.Groups[1].Value, out int score))
            {
                resultat.Score = score;
            }

            // Extraire les sections
            resultat.Bilan = ExtraireSection(reponse, "BILAN");
            resultat.Analyse = ExtraireSection(reponse, "ANALYSE");
            resultat.PointsAttention = ExtraireSection(reponse, "POINTS_ATTENTION");
            resultat.Recommandations = ExtraireSection(reponse, "RECOMMANDATIONS");
            resultat.Actions = ExtraireSection(reponse, "ACTIONS");

            return resultat;
        }

        private string ExtraireSection(string texte, string nomSection)
        {
            var debut = texte.IndexOf($"[{nomSection}]");
            var fin = texte.IndexOf($"[/{nomSection}]");

            if (debut >= 0 && fin > debut)
            {
                debut += nomSection.Length + 2;
                return texte.Substring(debut, fin - debut).Trim();
            }

            // Si pas trouvé avec balises fermantes, essayer de trouver jusqu'à la prochaine section
            debut = texte.IndexOf($"[{nomSection}]");
            if (debut >= 0)
            {
                debut += nomSection.Length + 2;
                var prochaineSection = texte.IndexOf("[", debut);
                if (prochaineSection > debut)
                {
                    return texte.Substring(debut, prochaineSection - debut).Trim();
                }
            }

            return "Non disponible";
        }

        private async Task<string> AppelerIAAsync(string prompt)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiToken}");
                client.Timeout = TimeSpan.FromMinutes(2);

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
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Erreur API (Code {(int)response.StatusCode}) : {errorContent}");
                }

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
            PanelChargement.Visibility = Visibility.Collapsed;
            PanelResultat.Visibility = Visibility.Visible;
            
            TxtBilan.Text = message;
            TxtAnalyse.Text = "Impossible de générer l'analyse.";
            TxtPointsAttention.Text = "Erreur d'analyse";
            TxtRecommandations.Text = "Veuillez vérifier votre configuration API.";
            TxtActions.Text = "Réessayez plus tard.";
        }

        private void BtnFermer_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

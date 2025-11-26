using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using BacklogManager.Domain;

namespace BacklogManager.Views
{
    public partial class AnalyseDevIAWindow : Window
    {
        private readonly dynamic _statsData;
        private readonly string _periodeDescription;

        public AnalyseDevIAWindow(dynamic statsData, string periodeDescription)
        {
            InitializeComponent();
            _statsData = statsData;
            _periodeDescription = periodeDescription;
            
            Dev dev = statsData.dev;
            TxtDevNom.Text = $"Analyse de {dev.Nom}";
            TxtPeriode.Text = periodeDescription;
            
            Loaded += async (s, e) => await AnalyserDeveloppeur();
        }

        private async Task AnalyserDeveloppeur()
        {
            try
            {
                LoadingOverlay.Visibility = Visibility.Visible;

                Dev dev = _statsData.dev;
                var taches = _statsData.taches as List<TacheDevViewModel>;
                var cras = _statsData.cras as List<CRA>;

                // Calculer des métriques détaillées
                int tachesEnRetard = 0; // Pas de statut EnRetard dans l'enum
                int tachesTermineesAvantDeadline = taches?.Count(t => t.StatutOriginal == Domain.Statut.Termine) ?? 0;
                var heuresCRA = cras?.Sum(c => c.HeuresTravaillees) ?? 0;
                var joursCRA = Math.Round(heuresCRA / 7.0, 1);

                var prompt = $@"Tu es Agent Project & Change, expert en management et analyse de performance individuelle.

Analyse la performance de **{dev.Nom}** pour la période ""{_periodeDescription}"" :

**STATISTIQUES TÂCHES**
- Total tâches assignées : {_statsData.totalTaches}
- En cours : {_statsData.enCours}
- Terminées : {_statsData.terminees}
- Terminées dans les délais : {tachesTermineesAvantDeadline}
- En retard : {tachesEnRetard}

**CHARGE & TEMPS**
- Charge estimée : {_statsData.charge} jours
- Temps réel passé : {_statsData.tempsReel} jours
- Taux de réalisation : {_statsData.tauxRealisation}
- CRA : {joursCRA}j saisis ({heuresCRA}h)

**DÉTAIL DES TÂCHES**
{(taches != null && taches.Any() ? string.Join("\n", taches.Take(10).Select(t => 
    $"- {t.Titre} [{t.Statut}] Charge:{t.ChiffrageJours}j Réel:{t.TempsReelJours}j"
)) : "Aucune tâche")}
{(taches != null && taches.Count > 10 ? $"\n... et {taches.Count - 10} autres tâches" : "")}

Fournis une analyse RH/managériale structurée avec ces sections (utilise EXACTEMENT ces marqueurs) :

[SCORE]
Un score sur 100 évaluant la performance globale basé sur :
- Taux de complétion (25%)
- Respect des délais (25%)
- Respect des estimations (20%)
- Productivité CRA (15%)
- Qualité (stabilité, pas de retours) (15%)
Réponds uniquement par le nombre, exemple: 82

[BILAN]
Un paragraphe de bilan général : niveau de performance, engagement, fiabilité.

[POINTS_FORTS]
3-4 points forts identifiés avec exemples concrets si possible.

[AMELIORATIONS]
2-3 axes d'amélioration constructifs et bienveillants.

[RECOMMANDATIONS]
3-4 recommandations managériales pour accompagner le développeur (formation, mentoring, ajustement de charge, etc.).

[ACTIONS]
2-3 actions concrètes à mettre en place dans les prochaines semaines.";

                var response = await AppelerIA(prompt);
                AfficherResultats(response);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'analyse IA :\n{ex.Message}", 
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private async Task<string> AppelerIA(string prompt)
        {
            try
            {
                var apiKey = BacklogManager.Properties.Settings.Default["AgentChatToken"]?.ToString()?.Trim();
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    throw new Exception("La clé API OpenAI n'est pas configurée. Configurez-la dans la section Chat.");
                }

                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                    httpClient.Timeout = TimeSpan.FromMinutes(2);

                    var requestBody = new
                    {
                        model = "gpt-4o-mini",
                        messages = new[]
                        {
                            new { role = "system", content = "Tu es Agent Project & Change, expert en analyse RH et management." },
                            new { role = "user", content = prompt }
                        },
                        temperature = 0.7,
                        max_tokens = 2000
                    };

                    var json = JsonSerializer.Serialize(requestBody);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);
                    response.EnsureSuccessStatusCode();

                    var responseBody = await response.Content.ReadAsStringAsync();
                    using (var document = JsonDocument.Parse(responseBody))
                    {
                        var root = document.RootElement;
                        return root.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Erreur API OpenAI : {ex.Message}", ex);
            }
        }

        private void AfficherResultats(string response)
        {
            try
            {
                // Parser le score
                var scoreMatch = System.Text.RegularExpressions.Regex.Match(response, @"\[SCORE\]\s*(\d+)");
                if (scoreMatch.Success && int.TryParse(scoreMatch.Groups[1].Value, out int score))
                {
                    TxtScore.Text = score.ToString();
                    
                    // Couleur selon le score
                    if (score >= 80)
                    {
                        BorderScore.Background = new SolidColorBrush(Color.FromRgb(76, 175, 80)); // Vert
                        TxtScoreDescription.Text = "Performance excellente ! Développeur clé de l'équipe.";
                    }
                    else if (score >= 60)
                    {
                        BorderScore.Background = new SolidColorBrush(Color.FromRgb(255, 193, 7)); // Orange
                        TxtScoreDescription.Text = "Performance solide avec du potentiel d'amélioration.";
                    }
                    else if (score >= 40)
                    {
                        BorderScore.Background = new SolidColorBrush(Color.FromRgb(255, 152, 0)); // Orange foncé
                        TxtScoreDescription.Text = "Performance à améliorer. Accompagnement nécessaire.";
                    }
                    else
                    {
                        BorderScore.Background = new SolidColorBrush(Color.FromRgb(244, 67, 54)); // Rouge
                        TxtScoreDescription.Text = "Difficultés importantes. Plan d'action urgent requis.";
                    }
                }

                // Parser les sections
                TxtBilan.Text = ExtraireSection(response, "BILAN");
                TxtPointsForts.Text = ExtraireSection(response, "POINTS_FORTS");
                TxtAmeliorations.Text = ExtraireSection(response, "AMELIORATIONS");
                TxtRecommandations.Text = ExtraireSection(response, "RECOMMANDATIONS");
                TxtActions.Text = ExtraireSection(response, "ACTIONS");

                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du parsing des résultats :\n{ex.Message}\n\nRéponse brute:\n{response}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private string ExtraireSection(string texte, string section)
        {
            var pattern = $@"\[{section}\]\s*(.+?)(?=\[|$)";
            var match = System.Text.RegularExpressions.Regex.Match(texte, pattern, 
                System.Text.RegularExpressions.RegexOptions.Singleline);
            
            if (match.Success)
            {
                return match.Groups[1].Value.Trim();
            }
            
            return $"Section {section} non trouvée dans la réponse.";
        }
    }
}

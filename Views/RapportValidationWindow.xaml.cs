using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using BacklogManager.Services;

namespace BacklogManager.Views
{
    public partial class RapportValidationWindow : Window
    {
        public class TacheRapport
        {
            public string Nom { get; set; }
            public string Detail { get; set; }
        }

        private readonly int _nombreValidations;
        private readonly int _nombreRetards;
        private readonly int _nombreTemps;

        public RapportValidationWindow(int nombreValidations, List<TacheRapport> tachesRetard, List<TacheRapport> tachesTemps)
        {
            InitializeComponent();
            
            _nombreValidations = nombreValidations;
            _nombreRetards = tachesRetard?.Count ?? 0;
            _nombreTemps = tachesTemps?.Count ?? 0;
            
            // Afficher le nombre de validations
            TxtNombreValidations.Text = $"{nombreValidations} CRA";

            // Afficher les tâches en retard
            if (tachesRetard != null && tachesRetard.Count > 0)
            {
                BorderRetard.Visibility = Visibility.Visible;
                ListeTachesRetard.ItemsSource = tachesRetard;
            }

            // Afficher les tâches dans les temps
            if (tachesTemps != null && tachesTemps.Count > 0)
            {
                BorderTemps.Visibility = Visibility.Visible;
                ListeTachesTemps.ItemsSource = tachesTemps;
            }

            // Si aucune donnée à afficher
            if ((tachesRetard == null || tachesRetard.Count == 0) && 
                (tachesTemps == null || tachesTemps.Count == 0))
            {
                BorderAucuneDonnee.Visibility = Visibility.Visible;
            }

            // Le token est maintenant centralisé dans AIConfigService
            BorderAnalyseIA.Visibility = Visibility.Visible;
            
            // Générer l'analyse en arrière-plan
                _ = GenererAnalyseIAAsync(tachesRetard, tachesTemps);
        }

        private async Task GenererAnalyseIAAsync(List<TacheRapport> tachesRetard, List<TacheRapport> tachesTemps)
        {
            try
            {
                // Construire le prompt pour l'IA
                var prompt = $@"Tu es l'assistant BacklogManager, un expert en gestion de projet et analyse de productivité. 
Tu viens d'analyser une validation de {_nombreValidations} CRA (Comptes Rendus d'Activité).

RÉSULTATS DE L'ANALYSE:
- ✅ Tâches dans les temps: {_nombreTemps}
- ⚠️ Tâches en retard: {_nombreRetards}

{(tachesRetard != null && tachesRetard.Any() ? 
$@"DÉTAILS DES RETARDS:
{string.Join("\n", tachesRetard.Select(t => $"• {t.Nom}: {t.Detail}"))}" : "")}

MISSION:
1. Analyse la conformité de ces validations par rapport aux bonnes pratiques de gestion de projet
2. Attribue un score de 0 à 100 basé sur:
   - Respect des délais (60% du score)
   - Nombre de tâches validées (20% du score)
   - Régularité et cohérence (20% du score)
3. Donne ton avis professionnel et des recommandations constructives

FORMAT DE RÉPONSE:
[SCORE: XX]
[Ton analyse personnalisée en 3-5 phrases avec ton style amical et professionnel]

Sois encourageant même en cas de retards, propose des solutions concrètes.";

                // Appeler l'IA
                var reponse = await AppelerIAAsync(prompt);

                // Parser la réponse pour extraire le score
                int score = 0;
                string analyse = reponse;
                
                if (reponse.Contains("[SCORE:"))
                {
                    var scoreStart = reponse.IndexOf("[SCORE:") + 7;
                    var scoreEnd = reponse.IndexOf("]", scoreStart);
                    if (scoreEnd > scoreStart)
                    {
                        var scoreStr = reponse.Substring(scoreStart, scoreEnd - scoreStart).Trim();
                        int.TryParse(scoreStr, out score);
                        
                        // Extraire l'analyse sans le tag de score
                        analyse = reponse.Substring(scoreEnd + 1).Trim();
                    }
                }

                // Afficher sur le thread UI
                Dispatcher.Invoke(() =>
                {
                    // Masquer le chargement
                    PanelChargementIA.Visibility = Visibility.Collapsed;
                    
                    // Afficher le score
                    if (score > 0)
                    {
                        TxtScore.Text = score.ToString();
                        BorderScore.Visibility = Visibility.Visible;
                        
                        // Couleur du score selon la valeur
                        if (score >= 80)
                            TxtScore.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#4CAF50"));
                        else if (score >= 60)
                            TxtScore.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FF9800"));
                        else
                            TxtScore.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#E53935"));
                    }
                    
                    // Afficher l'analyse
                    TxtAnalyseIA.Text = analyse;
                    TxtAnalyseIA.Visibility = Visibility.Visible;
                });
            }
            catch
            {
                // En cas d'erreur, afficher un message par défaut
                Dispatcher.Invoke(() =>
                {
                    PanelChargementIA.Visibility = Visibility.Collapsed;
                    TxtAnalyseIA.Text = "🤖 L'analyse IA n'est pas disponible pour le moment. Vérifiez votre configuration API.";
                    TxtAnalyseIA.Visibility = Visibility.Visible;
                });
            }
        }

        private async Task<string> AppelerIAAsync(string prompt)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {AIConfigService.GetToken()}");
                
                var langCode = LocalizationService.Instance.CurrentLanguageCode;
                string langInstruction;
                switch (langCode)
                {
                    case "fr":
                        langInstruction = "français";
                        break;
                    case "es":
                        langInstruction = "español";
                        break;
                    default:
                        langInstruction = "English";
                        break;
                }

                string systemContent = $"Tu es l'assistant BacklogManager, expert en gestion de projet. Réponds en {langInstruction}.";

                var requestBody = new
                {
                    model = AIConfigService.MODEL,
                    messages = new[]
                    {
                        new { role = "system", content = systemContent },
                        new { role = "user", content = prompt }
                    },
                    temperature = 0.7
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(AIConfigService.API_URL, content);
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

        private void BtnOK_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}

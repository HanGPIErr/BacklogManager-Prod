using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using BacklogManager.ViewModels;

namespace BacklogManager.Views
{
    public partial class AnalyseStatistiquesIAWindow : Window
    {
        private readonly StatistiquesViewModel _viewModel;
        private readonly string _periodeDescription;

        public AnalyseStatistiquesIAWindow(StatistiquesViewModel viewModel, string periodeDescription)
        {
            InitializeComponent();
            _viewModel = viewModel;
            _periodeDescription = periodeDescription;
            
            TxtPeriode.Text = periodeDescription;
            
            Loaded += async (s, e) => await AnalyserStatistiques();
        }

        private async Task AnalyserStatistiques()
        {
            try
            {
                LoadingOverlay.Visibility = Visibility.Visible;

                // Préparer les données statistiques
                var stats = new
                {
                    periode = _periodeDescription,
                    taches = new
                    {
                        total = _viewModel.TotalTaches,
                        terminees = _viewModel.TachesTerminees,
                        enCours = _viewModel.TachesEnCours,
                        pourcentageTerminees = _viewModel.PourcentageTerminees
                    },
                    cra = new
                    {
                        totalHeures = _viewModel.TotalHeuresCRA,
                        totalJours = _viewModel.TotalJoursCRA,
                        joursSaisisMoisCourant = _viewModel.JoursSaisisMoisCourant,
                        tauxCompletionMois = _viewModel.TauxCompletionMois,
                        tauxRealisation = _viewModel.TauxRealisation
                    },
                    projets = new
                    {
                        actifs = _viewModel.ProjetsActifs
                    },
                    developpeurs = new
                    {
                        actifs = _viewModel.DevsActifs,
                        details = _viewModel.ChargeParDev.Select(d => new
                        {
                            nom = d.NomDev,
                            nombreTaches = d.Total,
                            aFaire = d.AFaire,
                            enCours = d.EnCours,
                            terminees = d.Terminees
                        }).ToList()
                    }
                };

                var prompt = $@"Tu es Agent Project & Change, expert en analyse de données et gestion de projet agile.

Analyse les statistiques suivantes pour la période ""{_periodeDescription}"" :

**TÂCHES**
- Total : {stats.taches.total}
- Terminées : {stats.taches.terminees} ({stats.taches.pourcentageTerminees})
- En cours : {stats.taches.enCours}

**CRA (Comptes Rendus d'Activité)**
- Total heures saisies : {stats.cra.totalHeures} ({stats.cra.totalJours})
- Jours saisis ce mois : {stats.cra.joursSaisisMoisCourant}
- Taux complétion mois : {stats.cra.tauxCompletionMois}
- Taux réalisation : {stats.cra.tauxRealisation}

**PROJETS & ÉQUIPE**
- Projets actifs : {stats.projets.actifs}
- Développeurs actifs : {stats.developpeurs.actifs}

**PERFORMANCE PAR DÉVELOPPEUR**
{string.Join("\n", stats.developpeurs.details.Select(d => 
    $"- {d.nom}: {d.nombreTaches} tâches ({d.terminees} terminées, {d.enCours} en cours, {d.aFaire} à faire)"))}

Fournis une analyse structurée avec les sections suivantes (utilise EXACTEMENT ces marqueurs) :

[SCORE]
Un score sur 100 évaluant la performance globale basé sur :
- Taux de complétion des tâches (30%)
- Respect des estimations (30%)
- Productivité CRA (20%)
- Distribution de charge (20%)
Réponds uniquement par le nombre, exemple: 78

[VUE_ENSEMBLE]
Un paragraphe résumant l'état général, les chiffres clés et la santé du projet.

[PERFORMANCE_DEV]
Analyse de la performance par développeur : qui excelle, qui a besoin de soutien, patterns d'occupation.

[TENDANCES]
Patterns observés : surcharge, sous-utilisation, dépassements d'estimations, vélocité, etc.

[RECOMMANDATIONS]
3-4 recommandations concrètes pour améliorer la performance et l'organisation.

[ACTIONS]
3-4 actions prioritaires à mettre en place immédiatement.";

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
                    httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
                    httpClient.Timeout = TimeSpan.FromMinutes(2);

                    var requestBody = new
                    {
                        model = "gpt-oss-120b",
                        messages = new[]
                        {
                            new { role = "system", content = "Tu es Agent Project & Change, expert en analyse de données projet." },
                            new { role = "user", content = prompt }
                        },
                        temperature = 0.7,
                        max_tokens = 2000
                    };

                    var json = JsonSerializer.Serialize(requestBody);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await httpClient.PostAsync("https://genfactory-ai.analytics.cib.echonet/genai/api/v2/chat/completions", content);
                    
                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized || 
                            response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                        {
                            throw new Exception($"Erreur d'authentification API (Code {(int)response.StatusCode}).\n\n" +
                                              "Votre token API est invalide ou a expiré.\n\n" +
                                              "Veuillez vérifier votre token dans la section Chat avec l'IA.\n\n" +
                                              $"Détails : {errorContent}");
                        }
                        throw new Exception($"Erreur API OpenAI (Code {(int)response.StatusCode}) : {errorContent}");
                    }

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
                        TxtScoreDescription.Text = "Excellente performance ! L'équipe atteint ses objectifs.";
                    }
                    else if (score >= 60)
                    {
                        BorderScore.Background = new SolidColorBrush(Color.FromRgb(255, 193, 7)); // Orange
                        TxtScoreDescription.Text = "Performance correcte avec des axes d'amélioration.";
                    }
                    else
                    {
                        BorderScore.Background = new SolidColorBrush(Color.FromRgb(244, 67, 54)); // Rouge
                        TxtScoreDescription.Text = "Performance à améliorer. Actions correctrices nécessaires.";
                    }
                }

                // Parser les sections
                TxtVueEnsemble.Text = ExtraireSection(response, "VUE_ENSEMBLE");
                TxtPerformanceDev.Text = ExtraireSection(response, "PERFORMANCE_DEV");
                TxtTendances.Text = ExtraireSection(response, "TENDANCES");
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

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
using BacklogManager.Services;

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
            
            InitialiserTextes();
            TxtPeriode.Text = periodeDescription;
            
            Loaded += async (s, e) => await AnalyserStatistiques();
        }

        private void InitialiserTextes()
        {
            var loc = LocalizationService.Instance;

            // Titres de header
            TxtAgentTitle.Text = loc["StatsAIAnalysis_AgentName"];
            TxtAgentSubtitle.Text = loc["StatsAIAnalysis_StatsAndTrends"];

            // Titres des sections
            TxtTitlePeriode.Text = loc["StatsAIAnalysis_AnalyzedPeriod"];
            TxtTitlePerformanceScore.Text = loc["StatsAIAnalysis_PerformanceScore"];
            TxtTitleOverview.Text = loc["StatsAIAnalysis_Overview"];
            TxtTitleDevPerformance.Text = loc["StatsAIAnalysis_DevPerformance"];
            TxtTitleTrends.Text = loc["StatsAIAnalysis_TrendsAndPatterns"];
            TxtTitleRecommendations.Text = loc["StatsAIAnalysis_Recommendations"];
            TxtTitlePriorityActions.Text = loc["StatsAIAnalysis_PriorityActions"];
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
                    },
                    equipes = new
                    {
                        total = _viewModel.RessourcesParEquipe.Count,
                        details = _viewModel.RessourcesParEquipe.Select(e => new
                        {
                            nom = e.NomEquipe,
                            membres = e.NbMembres,
                            projets = e.NbProjets,
                            chargeParMembre = e.ChargeParMembre
                        }).ToList(),
                        charge = _viewModel.ChargeParEquipe.Select(e => new
                        {
                            nom = e.NomEquipe,
                            projetsActifs = e.NbProjets
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

**PROJETS & RESSOURCES**
- Projets actifs : {stats.projets.actifs}
- Développeurs actifs : {stats.developpeurs.actifs}
- Équipes actives : {stats.equipes.total}

**PERFORMANCE PAR DÉVELOPPEUR**
{string.Join("\n", stats.developpeurs.details.Select(d => 
    $"- {d.nom}: {d.nombreTaches} tâches ({d.terminees} terminées, {d.enCours} en cours, {d.aFaire} à faire)"))}

**RÉPARTITION PAR ÉQUIPE**
{string.Join("\n", stats.equipes.details.Select(e => 
    $"- {e.nom}: {e.membres} membres, {e.projets} projets ({e.chargeParMembre:F1} projets/membre)"))}

**ANALYSE CRITIQUE - Points d'attention équipes :**
{(stats.equipes.details.Any(e => e.membres == 0 && e.projets > 0) ? 
    $"⚠️ CRITIQUE : Équipes sans membres assignés détectées !\n{string.Join("\n", stats.equipes.details.Where(e => e.membres == 0 && e.projets > 0).Select(e => $"  - {e.nom}: {e.projets} projet(s) sans ressource"))}" : "")}
{(stats.equipes.charge.Any(e => e.projetsActifs > 6) ? 
    $"🔥 SURCHARGE : Équipes avec plus de 6 projets simultanés !\n{string.Join("\n", stats.equipes.charge.Where(e => e.projetsActifs > 6).Select(e => $"  - {e.nom}: {e.projetsActifs} projets"))}" : "")}
{(stats.equipes.details.Any(e => e.chargeParMembre > 3) ? 
    $"⚖️ DÉSÉQUILIBRE : Charge excessive par membre (>3 projets/personne)\n{string.Join("\n", stats.equipes.details.Where(e => e.chargeParMembre > 3).Select(e => $"  - {e.nom}: {e.chargeParMembre:F1} projets/membre"))}" : "")}
{(stats.equipes.details.Any(e => e.membres >= 3 && e.projets <= 1) ? 
    $"💡 CAPACITÉ DISPONIBLE : Équipes sous-utilisées\n{string.Join("\n", stats.equipes.details.Where(e => e.membres >= 3 && e.projets <= 1).Select(e => $"  - {e.nom}: {e.membres} membres, seulement {e.projets} projet(s)"))}" : "")}

Fournis une analyse structurée avec les sections suivantes (utilise EXACTEMENT ces marqueurs) :

[SCORE]
Un score sur 100 évaluant la performance globale basé sur :
- Taux de complétion des tâches (25%)
- Respect des estimations (25%)
- Productivité CRA (20%)
- Distribution de charge équipes (20%)
- Équilibre ressources/projets (10%)
Réponds uniquement par le nombre, exemple: 78

[VUE_ENSEMBLE]
Un paragraphe résumant l'état général : chiffres clés, santé du projet, et surtout l'équilibre entre équipes et projets.

[PERFORMANCE_DEV]
Analyse de la performance par développeur : qui excelle, qui a besoin de soutien, patterns d'occupation.

[TENDANCES]
Patterns observés : surcharge équipes, sous-utilisation, dépassements d'estimations, vélocité, déséquilibres ressources/projets.
**Analyse spécifique des équipes et de leur charge.**

[RECOMMANDATIONS]
4-5 recommandations concrètes pour améliorer :
- La répartition de charge entre équipes
- L'allocation des ressources
- La performance globale
- L'organisation des équipes et programmes
**Mettre l'accent sur les aspects équipes et répartition des projets.**

[ACTIONS]
4-5 actions prioritaires à mettre en place immédiatement :
- Actions sur les équipes critiques (sans membres, surchargées)
- Rééquilibrage de charge si nécessaire
- Optimisations ressources/projets
- Autres améliorations urgentes";

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
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {AIConfigService.GetToken()}");
                    httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
                    httpClient.Timeout = TimeSpan.FromMinutes(2);

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

                    string systemContent = $"Tu es Agent Project & Change, expert en analyse de données projet. " +
                                           $"Réponds en {langInstruction} et conserve EXACTEMENT les marqueurs entre crochets suivants : " +
                                           "[SCORE], [VUE_ENSEMBLE], [PERFORMANCE_DEV], [TENDANCES], [RECOMMANDATIONS], [ACTIONS].";

                    var requestBody = new
                    {
                        model = AIConfigService.MODEL,
                        messages = new[]
                        {
                            new { role = "system", content = systemContent },
                            new { role = "user", content = prompt }
                        },
                        temperature = 0.7,
                        max_tokens = 2000
                    };

                    var json = JsonSerializer.Serialize(requestBody);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await httpClient.PostAsync(AIConfigService.API_URL, content);
                    
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

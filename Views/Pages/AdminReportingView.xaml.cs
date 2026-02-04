using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;
using BacklogManager.Domain;
using BacklogManager.Services;

namespace BacklogManager.Views.Pages
{
    public partial class AdminReportingView : UserControl
    {
        private readonly IDatabase _database;
        private readonly AuthenticationService _authService;
        private string _modeAffichage = "programme"; // "programme" ou "projet"
        private DateTime? _dateDebutFiltre;
        private DateTime? _dateFinFiltre;
        private bool _isLoading = true;
        private List<Projet> _tousLesProjets = new List<Projet>();
        
        // Pour la génération IA
        private Programme _currentProgramme;
        private List<Projet> _currentProjets;
        private List<BacklogItem> _currentTaches;

        public AdminReportingView(IDatabase database, AuthenticationService authService)
        {
            InitializeComponent();
            _database = database;
            _authService = authService;
            
            // Initialiser les textes traduits
            InitialiserTextes();
            
            try
            {
                ChargerDonnees();
                _isLoading = false;
            }
            catch (Exception ex)
            {
                _isLoading = false;
                MessageBox.Show(string.Format(LocalizationService.Instance.GetString("Reporting_DataLoadError"), ex.Message),
                    LocalizationService.Instance.GetString("Reporting_Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void InitialiserTextes()
        {
            // Header principal
            TxtReportingTitle.Text = LocalizationService.Instance.GetString("Reporting_Title");
            TxtReportingDescription.Text = LocalizationService.Instance.GetString("Reporting_Description");
            
            // Boutons Vue Programme/Projet
            TxtProgramView.Text = LocalizationService.Instance.GetString("Reporting_ProgramView");
            TxtProjectView.Text = LocalizationService.Instance.GetString("Reporting_ProjectView");
            
            // Labels des formulaires
            LblProgramme.Text = LocalizationService.Instance.GetString("Reporting_ProgramLabel");
            LblProjet.Text = LocalizationService.Instance.GetString("Reporting_ProjectLabel");
            LblEquipe.Text = LocalizationService.Instance.GetString("Reporting_TeamLabel");
            LblPeriode.Text = LocalizationService.Instance.GetString("Reporting_AnalysisPeriod");
            
            // Bouton et messages
            TxtApplyButton.Text = LocalizationService.Instance.GetString("Reporting_ApplyButton");
            TxtSelectMessage.Text = LocalizationService.Instance.GetString("Reporting_SelectMessage");
            TxtSelectSubMessage.Text = LocalizationService.Instance.GetString("Reporting_SelectSubMessage");
            
            // Labels KPIs
            TxtLabelEquipes.Text = LocalizationService.Instance.GetString("Reporting_Teams");
            TxtDescriptionEquipes.Text = LocalizationService.Instance.GetString("Reporting_TeamsDescription");
            LblWorkloadDays1.Text = LocalizationService.Instance.GetString("Reporting_WorkloadDays");
            LblWorkloadDays2.Text = LocalizationService.Instance.GetString("Reporting_WorkloadDays");
            LblEstimatedWorkloadDays.Text = LocalizationService.Instance.GetString("Reporting_EstimatedWorkloadDays");

            // Nouveaux KPIs
            LblProgress.Text = LocalizationService.Instance.GetString("Reporting_Progress");
            LblTasks.Text = LocalizationService.Instance.GetString("Reporting_Tasks");
            LblDelivery.Text = LocalizationService.Instance.GetString("Reporting_Delivery");
            LblVelocity.Text = LocalizationService.Instance.GetString("Reporting_Velocity");
            LblPriorities.Text = LocalizationService.Instance.GetString("Reporting_Priorities");
            LblHealthStatus.Text = LocalizationService.Instance.GetString("Reporting_HealthStatus");
            LblDelayed.Text = LocalizationService.Instance.GetString("Reporting_Delayed");
            LblComplexity.Text = LocalizationService.Instance.GetString("Reporting_Complexity");
            LblComparativeView.Text = "📈 " + LocalizationService.Instance.GetString("Reporting_ComparativeView");
            LblRemainingTasksComp.Text = LocalizationService.Instance.GetString("Reporting_RemainingTasks");
            
            // Métriques détaillées
            LblTasksPerMonth.Text = LocalizationService.Instance.GetString("Reporting_TasksPerMonth");
            LblActualVsEstimated.Text = LocalizationService.Instance.GetString("Reporting_ActualVsEstimated");
            LblOnTrack.Text = LocalizationService.Instance.GetString("Reporting_OnTrack");
            LblRetardTasks.Text = LocalizationService.Instance.GetString("Reporting_TasksLabel");
            LblAverageComplexity.Text = LocalizationService.Instance.GetString("Reporting_Average");
            
            // Section Contributions des Ressources
            LblContributionsTitle.Text = LocalizationService.Instance.GetString("Reporting_ContributionsTitle");
            
            // Vue Comparative
            LblBeforePeriod1.Text = LocalizationService.Instance.GetString("Reporting_BeforePeriod");
            LblBeforePeriod2.Text = LocalizationService.Instance.GetString("Reporting_BeforePeriod");
            LblSelectedPeriod1.Text = LocalizationService.Instance.GetString("Reporting_SelectedPeriod");
            LblSelectedPeriod2.Text = LocalizationService.Instance.GetString("Reporting_SelectedPeriod");
            LblToDoRemaining.Text = LocalizationService.Instance.GetString("Reporting_ToDoRemaining");
            LblCompletedTasksLabel1.Text = LocalizationService.Instance.GetString("Reporting_CompletedTasks");
            LblCompletedTasksLabel2.Text = LocalizationService.Instance.GetString("Reporting_CompletedTasks");
            LblTasksLabel1.Text = LocalizationService.Instance.GetString("Reporting_Tasks");
            LblTasksLabel2.Text = LocalizationService.Instance.GetString("Reporting_Tasks");
            LblWorkloadDays3.Text = LocalizationService.Instance.GetString("Reporting_WorkloadDays");
            LblWorkloadDays4.Text = LocalizationService.Instance.GetString("Reporting_WorkloadDays");
            LblAllCompleted.Text = LocalizationService.Instance.GetString("Reporting_AllCompleted");
            
            // Section Programme détaillée
            LblPhasesJalons.Text = LocalizationService.Instance.GetString("Reporting_PhasesAndMilestones");
            LblChangeManagement.Text = LocalizationService.Instance.GetString("Reporting_ChangeManagement");
            LblEvolvingScope.Text = LocalizationService.Instance.GetString("Reporting_EvolvingScope");
            LblEvolvingScopeDesc.Text = LocalizationService.Instance.GetString("Reporting_EvolvingScopeDesc");
            LblTimelineProjects.Text = LocalizationService.Instance.GetString("Reporting_TimelineProjects");
            
            // Mettre à jour les en-têtes des colonnes du DataGrid
            MettreAJourEntetes();

            // S'abonner aux changements de langue
            LocalizationService.Instance.PropertyChanged += (s, e) =>
            {
                TxtReportingTitle.Text = LocalizationService.Instance.GetString("Reporting_Title");
                TxtReportingDescription.Text = LocalizationService.Instance.GetString("Reporting_Description");
                TxtProgramView.Text = LocalizationService.Instance.GetString("Reporting_ProgramView");
                TxtProjectView.Text = LocalizationService.Instance.GetString("Reporting_ProjectView");
                LblProgramme.Text = LocalizationService.Instance.GetString("Reporting_ProgramLabel");
                LblProjet.Text = LocalizationService.Instance.GetString("Reporting_ProjectLabel");
                LblEquipe.Text = LocalizationService.Instance.GetString("Reporting_TeamLabel");
                LblPeriode.Text = LocalizationService.Instance.GetString("Reporting_AnalysisPeriod");
                TxtApplyButton.Text = LocalizationService.Instance.GetString("Reporting_ApplyButton");
                TxtSelectMessage.Text = LocalizationService.Instance.GetString("Reporting_SelectMessage");
                TxtSelectSubMessage.Text = LocalizationService.Instance.GetString("Reporting_SelectSubMessage");
                TxtLabelEquipes.Text = LocalizationService.Instance.GetString("Reporting_Teams");
                TxtDescriptionEquipes.Text = LocalizationService.Instance.GetString("Reporting_TeamsDescription");
                LblWorkloadDays1.Text = LocalizationService.Instance.GetString("Reporting_WorkloadDays");
                LblWorkloadDays2.Text = LocalizationService.Instance.GetString("Reporting_WorkloadDays");
                LblEstimatedWorkloadDays.Text = LocalizationService.Instance.GetString("Reporting_EstimatedWorkloadDays");
                
                // Nouveaux KPIs
                LblProgress.Text = LocalizationService.Instance.GetString("Reporting_Progress");
                LblTasks.Text = LocalizationService.Instance.GetString("Reporting_Tasks");
                LblDelivery.Text = LocalizationService.Instance.GetString("Reporting_Delivery");
                LblVelocity.Text = LocalizationService.Instance.GetString("Reporting_Velocity");
                LblPriorities.Text = LocalizationService.Instance.GetString("Reporting_Priorities");
                LblHealthStatus.Text = LocalizationService.Instance.GetString("Reporting_HealthStatus");
                LblDelayed.Text = LocalizationService.Instance.GetString("Reporting_Delayed");
                LblComplexity.Text = LocalizationService.Instance.GetString("Reporting_Complexity");
                LblComparativeView.Text = "📈 " + LocalizationService.Instance.GetString("Reporting_ComparativeView");
                LblRemainingTasksComp.Text = LocalizationService.Instance.GetString("Reporting_RemainingTasks");
                
                // Métriques détaillées
                LblTasksPerMonth.Text = LocalizationService.Instance.GetString("Reporting_TasksPerMonth");
                LblActualVsEstimated.Text = LocalizationService.Instance.GetString("Reporting_ActualVsEstimated");
                LblOnTrack.Text = LocalizationService.Instance.GetString("Reporting_OnTrack");
                LblRetardTasks.Text = LocalizationService.Instance.GetString("Reporting_TasksLabel");
                LblAverageComplexity.Text = LocalizationService.Instance.GetString("Reporting_Average");
                
                // Section Contributions des Ressources
                LblContributionsTitle.Text = LocalizationService.Instance.GetString("Reporting_ContributionsTitle");
                
                // Vue Comparative
                LblBeforePeriod1.Text = LocalizationService.Instance.GetString("Reporting_BeforePeriod");
                LblBeforePeriod2.Text = LocalizationService.Instance.GetString("Reporting_BeforePeriod");
                LblSelectedPeriod1.Text = LocalizationService.Instance.GetString("Reporting_SelectedPeriod");
                LblSelectedPeriod2.Text = LocalizationService.Instance.GetString("Reporting_SelectedPeriod");
                LblToDoRemaining.Text = LocalizationService.Instance.GetString("Reporting_ToDoRemaining");
                LblCompletedTasksLabel1.Text = LocalizationService.Instance.GetString("Reporting_CompletedTasks");
                LblCompletedTasksLabel2.Text = LocalizationService.Instance.GetString("Reporting_CompletedTasks");
                LblTasksLabel1.Text = LocalizationService.Instance.GetString("Reporting_Tasks");
                LblTasksLabel2.Text = LocalizationService.Instance.GetString("Reporting_Tasks");
                LblWorkloadDays3.Text = LocalizationService.Instance.GetString("Reporting_WorkloadDays");
                LblWorkloadDays4.Text = LocalizationService.Instance.GetString("Reporting_WorkloadDays");
                LblAllCompleted.Text = LocalizationService.Instance.GetString("Reporting_AllCompleted");
                
                // Section Programme détaillée
                LblPhasesJalons.Text = LocalizationService.Instance.GetString("Reporting_PhasesAndMilestones");
                LblChangeManagement.Text = LocalizationService.Instance.GetString("Reporting_ChangeManagement");
                LblEvolvingScope.Text = LocalizationService.Instance.GetString("Reporting_EvolvingScope");
                LblEvolvingScopeDesc.Text = LocalizationService.Instance.GetString("Reporting_EvolvingScopeDesc");
                LblTimelineProjects.Text = LocalizationService.Instance.GetString("Reporting_TimelineProjects");
                
                // Mettre à jour les en-têtes des colonnes du DataGrid
                MettreAJourEntetes();
            };
        }

        private void ChargerDonnees()
        {
            try
            {
                // Charger les programmes
                var programmes = _database.GetAllProgrammes().Where(p => p.Actif).OrderBy(p => p.Nom).ToList();
                if (ComboProgramme != null)
                {
                    ComboProgramme.ItemsSource = programmes;
                }
                
                // Charger tous les projets et les stocker
                _tousLesProjets = _database.GetProjets().Where(p => p.Actif).OrderBy(p => p.Nom).ToList();
                
                // Charger les équipes (sans "Toutes les équipes")
                var equipes = _database.GetAllEquipes().Where(e => e.Actif).OrderBy(e => e.Nom).ToList();
                
                if (ComboEquipe != null)
                {
                    ComboEquipe.ItemsSource = equipes;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Erreur dans ChargerDonnees: {ex.Message}", ex);
            }
        }

        private void BtnModeProgramme_Click(object sender, RoutedEventArgs e)
        {
            if (_modeAffichage == "programme") return;
            
            _modeAffichage = "programme";
            
            // Style des boutons
            BtnModeProgramme.Background = new SolidColorBrush(Color.FromRgb(0, 145, 90));
            BtnModeProgramme.Foreground = new SolidColorBrush(Colors.White);
            BtnModeProgramme.FontWeight = FontWeights.SemiBold;
            
            BtnModeProjet.Background = new SolidColorBrush(Colors.Transparent);
            BtnModeProjet.Foreground = new SolidColorBrush(Color.FromRgb(109, 109, 109));
            BtnModeProjet.FontWeight = FontWeights.Normal;
            
            // Affichage des panneaux
            PanelProgramme.Visibility = Visibility.Visible;
            PanelProjet.Visibility = Visibility.Collapsed;
            PanelEquipe.Visibility = Visibility.Collapsed;
            
            // Réinitialiser la sélection
            ComboProjet.SelectedItem = null;
            ComboEquipe.SelectedItem = null;
            
            // Masquer les KPIs
            MasquerKPIs();
        }

        private void BtnModeProjet_Click(object sender, RoutedEventArgs e)
        {
            if (_modeAffichage == "projet") return;
            
            _modeAffichage = "projet";
            
            // Style des boutons
            BtnModeProjet.Background = new SolidColorBrush(Color.FromRgb(0, 145, 90));
            BtnModeProjet.Foreground = new SolidColorBrush(Colors.White);
            BtnModeProjet.FontWeight = FontWeights.SemiBold;
            
            BtnModeProgramme.Background = new SolidColorBrush(Colors.Transparent);
            BtnModeProgramme.Foreground = new SolidColorBrush(Color.FromRgb(109, 109, 109));
            BtnModeProgramme.FontWeight = FontWeights.Normal;
            
            // Affichage des panneaux
            PanelProgramme.Visibility = Visibility.Collapsed;
            PanelEquipe.Visibility = Visibility.Visible;
            PanelProjet.Visibility = Visibility.Visible;
            
            // Réinitialiser les sélections
            ComboProgramme.SelectedItem = null;
            ComboEquipe.SelectedItem = null;
            ComboProjet.SelectedItem = null;
            ComboProjet.ItemsSource = null;
            
            // Masquer les KPIs
            MasquerKPIs();
        }

        private void ComboProgramme_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoading) return;
            
            if (ComboProgramme.SelectedItem is Programme programme)
            {
                ChargerStatistiquesProgramme(programme);
            }
        }

        private void ComboProjet_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoading) return;
            
            if (ComboProjet.SelectedItem is Projet projet)
            {
                // Récupérer l'équipe sélectionnée
                var equipe = ComboEquipe.SelectedItem as Equipe;
                ChargerStatistiquesProjet(projet, equipe);
            }
        }

        private void ComboEquipe_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoading) return;
            
            if (ComboEquipe.SelectedItem is Equipe equipe)
            {
                // Filtrer les projets selon l'équipe sélectionnée
                var projetsFiltres = _tousLesProjets
                    .Where(p => p.EquipesAssigneesIds != null && p.EquipesAssigneesIds.Contains(equipe.Id))
                    .OrderBy(p => p.Nom)
                    .ToList();
                
                if (ComboProjet != null)
                {
                    ComboProjet.ItemsSource = projetsFiltres;
                    ComboProjet.SelectedItem = null;
                }
                
                // Masquer les KPIs jusqu'à ce qu'un projet soit sélectionné
                MasquerKPIs();
            }
        }

        private void ComboPeriode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoading) return;
            
            if (ComboPeriode.SelectedIndex == 2) // Période personnalisée
            {
                PanelPeriodePersonnalisee.Visibility = Visibility.Visible;
            }
            else
            {
                PanelPeriodePersonnalisee.Visibility = Visibility.Collapsed;
                
                // Appliquer les filtres automatiquement
                AppliquerFiltrePeriode();
            }
        }

        private void BtnAppliquerPeriode_Click(object sender, RoutedEventArgs e)
        {
            AppliquerFiltrePeriode();
        }

        private void AppliquerFiltrePeriode()
        {
            var selectedIndex = ComboPeriode.SelectedIndex;
            
            switch (selectedIndex)
            {
                case 0: // Dernier mois
                    _dateDebutFiltre = DateTime.Now.AddMonths(-1);
                    _dateFinFiltre = DateTime.Now;
                    break;
                    
                case 1: // 2 derniers mois
                    _dateDebutFiltre = DateTime.Now.AddMonths(-2);
                    _dateFinFiltre = DateTime.Now;
                    break;
                    
                case 2: // Période personnalisée
                    if (DateDebutPeriode.SelectedDate.HasValue && DateFinPeriode.SelectedDate.HasValue)
                    {
                        var dateDebut = DateDebutPeriode.SelectedDate.Value;
                        var dateFin = DateFinPeriode.SelectedDate.Value;
                        
                        // Valider que la date de fin est postérieure à la date de début
                        if (dateFin < dateDebut)
                        {
                            MessageBox.Show("La date de fin doit être postérieure à la date de début.", 
                                "Période invalide", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                        
                        // Normaliser au début du mois pour date début
                        _dateDebutFiltre = new DateTime(dateDebut.Year, dateDebut.Month, 1);
                        
                        // Normaliser à la fin du mois pour date fin
                        _dateFinFiltre = new DateTime(dateFin.Year, dateFin.Month, DateTime.DaysInMonth(dateFin.Year, dateFin.Month), 23, 59, 59);
                    }
                    else
                    {
                        MessageBox.Show("Veuillez sélectionner une date de début et de fin.", 
                            "Période invalide", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    break;
                    
                case 3: // Vue globale
                    _dateDebutFiltre = null;
                    _dateFinFiltre = null;
                    break;
            }
            
            // Recharger les statistiques avec le nouveau filtre
            if (_modeAffichage == "programme" && ComboProgramme.SelectedItem is Programme programme)
            {
                ChargerStatistiquesProgramme(programme);
            }
            else if (_modeAffichage == "projet" && ComboProjet.SelectedItem is Projet projet)
            {
                var equipe = ComboEquipe.SelectedItem as Equipe;
                ChargerStatistiquesProjet(projet, equipe);
            }
        }

        private void ChargerStatistiquesProgramme(Programme programme)
        {
            try
            {
                // Remplir les informations du programme
                RemplirInformationsProgramme(programme);
                
                var backlogService = new BacklogService(_database);
                var toutesLesTaches = backlogService.GetAllBacklogItemsIncludingArchived();
                
                // Récupérer tous les projets du programme
                var projets = _database.GetProjets().Where(p => p.ProgrammeId == programme.Id && p.Actif).ToList();
                
                if (projets.Count == 0)
                {
                    MessageBox.Show(LocalizationService.Instance.GetString("Reporting_NoActiveProjects"), 
                        LocalizationService.Instance.GetString("Reporting_Information"), MessageBoxButton.OK, MessageBoxImage.Information);
                    MasquerKPIs();
                    return;
                }
                
                // Remplir la timeline des projets
                RemplirTimelineProjets(projets, toutesLesTaches);
                
                // Remplir le tableau Progress Status
                RemplirProgressStatus(projets, toutesLesTaches);
                
                // Remplir Dashboard KPIs et Actions
                RemplirDashboardKPIs(projets, toutesLesTaches);
                
                // Récupérer toutes les tâches de tous les projets du programme
                var tachesToutes = toutesLesTaches.Where(t => projets.Any(p => p.Id == t.ProjetId)).ToList();
                
                // Appliquer le filtre de période si nécessaire
                var taches = FiltrerTachesParPeriode(tachesToutes);
                
                // Calculer les statistiques consolidées
                int totalTaches = taches.Count;
                int tachesCompletes = taches.Count(t => t.Statut == Statut.Termine || t.EstArchive);
                double avancement = totalTaches > 0 ? Math.Round((double)tachesCompletes / totalTaches * 100, 0) : 0;
                
                // KPIs
                TxtAvancement.Text = avancement.ToString("0");
                BarAvancement.Value = avancement;
                
                TxtTachesCompletes.Text = tachesCompletes.ToString();
                TxtTachesTotal.Text = totalTaches.ToString();
                int tachesRestantes = totalTaches - tachesCompletes;
                TxtTachesRestantes.Text = string.Format("{0} " + LocalizationService.Instance.GetString("Reporting_RemainingTasksCount"), tachesRestantes);
                
                // Équipes impliquées (unique)
                var equipesIds = new HashSet<int>();
                foreach (var projet in projets)
                {
                    if (projet.EquipesAssigneesIds != null)
                    {
                        foreach (var equipeId in projet.EquipesAssigneesIds)
                        {
                            equipesIds.Add(equipeId);
                        }
                    }
                }
                TxtNbEquipes.Text = equipesIds.Count.ToString();
                TxtDescriptionEquipes.Text = LocalizationService.Instance.GetString("Reporting_TeamsInvolved");
                
                // Livraison - calculer la date de fin la plus tardive
                DateTime? dateFinMax = null;
                foreach (var projet in projets)
                {
                    if (projet.DateFinPrevue.HasValue)
                    {
                        if (!dateFinMax.HasValue || projet.DateFinPrevue.Value > dateFinMax.Value)
                        {
                            dateFinMax = projet.DateFinPrevue.Value;
                        }
                    }
                }
                
                if (dateFinMax.HasValue)
                {
                    TxtTargetDelivery.Text = dateFinMax.Value.ToString("dd/MM/yyyy");
                    var joursRestants = (dateFinMax.Value - DateTime.Now).Days;
                    if (joursRestants > 0)
                    {
                        TxtDeadlineInfo.Text = string.Format(LocalizationService.Instance.GetString("Reporting_DaysRemaining"), joursRestants);
                    }
                    else if (joursRestants == 0)
                    {
                        TxtDeadlineInfo.Text = LocalizationService.Instance.GetString("Reporting_Today");
                    }
                    else
                    {
                        TxtDeadlineInfo.Text = string.Format(LocalizationService.Instance.GetString("Reporting_DaysDelayed"), Math.Abs(joursRestants));
                        TxtDeadlineInfo.Foreground = new SolidColorBrush(Color.FromRgb(244, 67, 54));
                    }
                }
                else
                {
                    TxtTargetDelivery.Text = LocalizationService.Instance.GetString("Reporting_NotDefined");
                    TxtDeadlineInfo.Text = "";
                }
                
                // Timeline
                DateTime? dateDebutMin = projets.Where(p => p.DateDebut.HasValue).Min(p => p.DateDebut);
                TxtDateDebut.Text = dateDebutMin?.ToString("dd/MM/yyyy") ?? "Non définie";
                TxtDateFin.Text = dateFinMax?.ToString("dd/MM/yyyy") ?? "Non définie";
                
                // Progression temporelle
                if (dateDebutMin.HasValue && dateFinMax.HasValue)
                {
                    var maintenant = DateTime.Now;
                    if (maintenant >= dateDebutMin.Value && maintenant <= dateFinMax.Value)
                    {
                        var dureeTotal = (dateFinMax.Value - dateDebutMin.Value).TotalDays;
                        var dureeEcoulee = (maintenant - dateDebutMin.Value).TotalDays;
                        var progressionTemps = Math.Round((dureeEcoulee / dureeTotal) * 100, 0);
                        
                        ProgressTemporel.Width = (progressionTemps / 100.0) * 800;
                        TxtProgressionTemps.Text = string.Format("{0}% du temps écoulé", progressionTemps);
                    }
                    else if (maintenant < dateDebutMin.Value)
                    {
                        ProgressTemporel.Width = 0;
                        TxtProgressionTemps.Text = "Pas encore commencé";
                    }
                    else
                    {
                        ProgressTemporel.Width = 800;
                        TxtProgressionTemps.Text = "Programme terminé (temps)";
                    }
                }
                else
                {
                    TxtProgressionTemps.Text = "Dates non définies";
                }
                
                // Gains - Consolider les gains de tous les projets
                var gainsTemps = new List<string>();
                var gainsFinanciers = new List<string>();
                
                foreach (var projet in projets)
                {
                    if (!string.IsNullOrEmpty(projet.GainsTemps) && projet.GainsTemps != "Non spécifié")
                    {
                        gainsTemps.Add($"{projet.Nom}: {projet.GainsTemps}");
                    }
                    if (!string.IsNullOrEmpty(projet.GainsFinanciers) && projet.GainsFinanciers != "Non spécifié")
                    {
                        gainsFinanciers.Add($"{projet.Nom}: {projet.GainsFinanciers}");
                    }
                }
                
                TxtGainsTemps.Text = gainsTemps.Count > 0 
                    ? string.Join("\n", gainsTemps) 
                    : "Non spécifié";
                    
                TxtGainsFinanciers.Text = gainsFinanciers.Count > 0 
                    ? string.Join("\n", gainsFinanciers) 
                    : "Non spécifié";
                
                // Contributions des ressources - Agrégées sur tous les projets
                ChargerContributionsRessources(taches);
                
                // Calculer et afficher les stats comparatives (avant / période / reste)
                CalculerEtAfficherStatsComparatives(toutesLesTaches);
                
                // Calculer les statistiques supplémentaires
                CalculerStatistiquesSupplementaires(taches, tachesToutes, projets);
                
                // Afficher les KPIs
                AfficherKPIs();
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(LocalizationService.Instance.GetString("Reporting_StatisticsLoadError"), ex.Message), 
                    LocalizationService.Instance.GetString("Reporting_Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ChargerStatistiquesProjet(Projet projet, Equipe equipe)
        {
            try
            {
                var backlogService = new BacklogService(_database);
                var toutesLesTaches = backlogService.GetAllBacklogItemsIncludingArchived();
                
                // Filtrer par projet
                var tachesProjet = toutesLesTaches.Where(t => t.ProjetId == projet.Id).ToList();
                
                // Filtrer par équipe si sélectionnée (toutes les tâches de l'équipe pour ce projet)
                if (equipe != null)
                {
                    var utilisateursEquipe = _database.GetUtilisateurs()
                        .Where(u => u.EquipeId == equipe.Id)
                        .Select(u => u.Id)
                        .ToList();
                    
                    tachesProjet = tachesProjet.Where(t => 
                        t.DevAssigneId.HasValue && utilisateursEquipe.Contains(t.DevAssigneId.Value)
                    ).ToList();
                }
                
                // Appliquer le filtre de période
                var taches = FiltrerTachesParPeriode(tachesProjet);
                
                // Calculer les statistiques
                int totalTaches = taches.Count;
                int tachesCompletes = taches.Count(t => t.Statut == Statut.Termine || t.EstArchive);
                double avancement = totalTaches > 0 ? Math.Round((double)tachesCompletes / totalTaches * 100, 0) : 0;
                
                // KPIs
                TxtAvancement.Text = avancement.ToString("0");
                BarAvancement.Value = avancement;
                
                TxtTachesCompletes.Text = tachesCompletes.ToString();
                TxtTachesTotal.Text = totalTaches.ToString();
                int tachesRestantes = totalTaches - tachesCompletes;
                TxtTachesRestantes.Text = string.Format("{0} " + LocalizationService.Instance.GetString("Reporting_RemainingTasksCount"), tachesRestantes);
                
                // Équipes
                if (equipe != null)
                {
                    TxtNbEquipes.Text = "1";
                    TxtDescriptionEquipes.Text = equipe.Nom;
                }
                else
                {
                    int nbEquipes = projet.EquipesAssigneesIds != null ? projet.EquipesAssigneesIds.Count : 0;
                    TxtNbEquipes.Text = nbEquipes.ToString();
                    TxtDescriptionEquipes.Text = LocalizationService.Instance.GetString("Reporting_TeamsInvolved");
                }
                
                // Livraison
                if (projet.DateFinPrevue.HasValue)
                {
                    TxtTargetDelivery.Text = projet.DateFinPrevue.Value.ToString("dd/MM/yyyy");
                    var joursRestants = (projet.DateFinPrevue.Value - DateTime.Now).Days;
                    if (joursRestants > 0)
                    {
                        TxtDeadlineInfo.Text = string.Format(LocalizationService.Instance.GetString("Reporting_DaysRemaining"), joursRestants);
                    }
                    else if (joursRestants == 0)
                    {
                        TxtDeadlineInfo.Text = LocalizationService.Instance.GetString("Reporting_Today");
                    }
                    else
                    {
                        TxtDeadlineInfo.Text = string.Format(LocalizationService.Instance.GetString("Reporting_DaysDelayed"), Math.Abs(joursRestants));
                        TxtDeadlineInfo.Foreground = new SolidColorBrush(Color.FromRgb(244, 67, 54));
                    }
                }
                else if (!string.IsNullOrEmpty(projet.TargetDelivery))
                {
                    TxtTargetDelivery.Text = projet.TargetDelivery;
                    TxtDeadlineInfo.Text = "";
                }
                else
                {
                    TxtTargetDelivery.Text = "Non définie";
                    TxtDeadlineInfo.Text = "";
                }
                
                // Timeline
                TxtDateDebut.Text = projet.DateDebut?.ToString("dd/MM/yyyy") ?? "Non définie";
                TxtDateFin.Text = projet.DateFinPrevue?.ToString("dd/MM/yyyy") ?? "Non définie";
                
                // Progression temporelle
                if (projet.DateDebut.HasValue && projet.DateFinPrevue.HasValue)
                {
                    var debut = projet.DateDebut.Value;
                    var fin = projet.DateFinPrevue.Value;
                    var maintenant = DateTime.Now;
                    
                    if (maintenant >= debut && maintenant <= fin)
                    {
                        var dureeTotal = (fin - debut).TotalDays;
                        var dureeEcoulee = (maintenant - debut).TotalDays;
                        var progressionTemps = Math.Round((dureeEcoulee / dureeTotal) * 100, 0);
                        
                        ProgressTemporel.Width = (progressionTemps / 100.0) * 800;
                        TxtProgressionTemps.Text = string.Format("{0}% du temps écoulé", progressionTemps);
                    }
                    else if (maintenant < debut)
                    {
                        ProgressTemporel.Width = 0;
                        TxtProgressionTemps.Text = "Pas encore commencé";
                    }
                    else
                    {
                        ProgressTemporel.Width = 800;
                        TxtProgressionTemps.Text = "Projet terminé (temps)";
                    }
                }
                else
                {
                    TxtProgressionTemps.Text = "Dates non définies";
                }
                
                // Gains
                TxtGainsTemps.Text = projet.GainsTemps ?? "Non spécifié";
                TxtGainsFinanciers.Text = projet.GainsFinanciers ?? "Non spécifié";
                
                // Contributions des ressources
                ChargerContributionsRessources(taches);
                
                // Calculer et afficher les stats comparatives (avant / période / reste)
                CalculerEtAfficherStatsComparatives(tachesProjet);
                
                // Calculer les statistiques supplémentaires
                CalculerStatistiquesSupplementaires(taches, tachesProjet, new List<Projet> { projet });
                
                // Afficher les KPIs
                AfficherKPIs();
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(LocalizationService.Instance.GetString("Reporting_StatisticsLoadError"), ex.Message), 
                    LocalizationService.Instance.GetString("Reporting_Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private List<BacklogItem> FiltrerTachesParPeriode(List<BacklogItem> taches)
        {
            if (!_dateDebutFiltre.HasValue || !_dateFinFiltre.HasValue)
            {
                // Pas de filtre - retourner toutes les tâches
                return taches;
            }
            
            // Filtrer les tâches qui sont actives ou se terminent dans la période
            return taches.Where(t =>
            {
                // Date de début de la tâche (date de création ou date de début si spécifiée)
                var tacheDebut = t.DateDebut ?? t.DateCreation;
                
                // Date de fin de la tâche (date de fin attendue ou maintenant si pas spécifiée)
                var tacheFin = t.DateFinAttendue ?? DateTime.Now;
                
                // Si la tâche est terminée, utiliser la date de création comme référence max
                if (t.Statut == Statut.Termine || t.EstArchive)
                {
                    // Pour les tâches terminées, on regarde si elles ont été actives pendant la période
                    // ou si elles ont été terminées pendant la période
                    return (tacheDebut <= _dateFinFiltre.Value && tacheFin >= _dateDebutFiltre.Value);
                }
                
                // Pour les tâches en cours ou à faire
                // Inclure si la tâche a été créée avant la fin de la période
                // ET (pas de date de fin OU date de fin après le début de la période)
                return (tacheDebut <= _dateFinFiltre.Value && 
                       (!t.DateFinAttendue.HasValue || t.DateFinAttendue.Value >= _dateDebutFiltre.Value));
                
            }).ToList();
        }

        private void ChargerContributionsRessources(List<BacklogItem> taches)
        {
            var contributions = new Dictionary<string, ReportingContributionInfo>();
            
            foreach (var tache in taches)
            {
                if (tache.DevAssigneId.HasValue)
                {
                    var dev = _database.GetUtilisateurs().FirstOrDefault(u => u.Id == tache.DevAssigneId.Value);
                    if (dev != null)
                    {
                        string devKey = dev.Nom;
                        
                        if (!contributions.ContainsKey(devKey))
                        {
                            // Récupérer le nom de l'équipe du développeur
                            string nomEquipe = "-";
                            if (dev.EquipeId.HasValue)
                            {
                                var equipe = _database.GetAllEquipes().FirstOrDefault(e => e.Id == dev.EquipeId.Value);
                                if (equipe != null)
                                {
                                    nomEquipe = equipe.Nom;
                                }
                            }
                            
                            contributions[devKey] = new ReportingContributionInfo
                            {
                                NomDeveloppeur = devKey,
                                NomEquipe = nomEquipe,
                                TachesTotal = 0,
                                TachesCompletes = 0,
                                HeuresEstimees = 0
                            };
                        }
                        
                        contributions[devKey].TachesTotal++;
                        if (tache.Statut == Statut.Termine || tache.EstArchive)
                        {
                            contributions[devKey].TachesCompletes++;
                        }
                        contributions[devKey].HeuresEstimees += tache.ChiffrageHeures ?? 0;
                    }
                }
            }
            
            // Calculer les pourcentages
            double totalHeures = contributions.Values.Sum(c => c.HeuresEstimees);
            foreach (var contrib in contributions.Values)
            {
                contrib.PourcentageContribution = totalHeures > 0 
                    ? Math.Round((contrib.HeuresEstimees / totalHeures) * 100, 1) 
                    : 0;
            }
            
            // Trier par contribution décroissante
            var contributionsTriees = contributions.Values.OrderByDescending(c => c.PourcentageContribution).ToList();
            
            // Afficher dans la liste
            ListeContributions.ItemsSource = contributionsTriees;
        }

        private void AfficherKPIs()
        {
            PanelAucuneSelection.Visibility = Visibility.Collapsed;
            PanelKPIs.Visibility = Visibility.Visible;
            PanelComparatif.Visibility = Visibility.Visible;
            PanelTimelineGains.Visibility = Visibility.Visible;
            PanelContributions.Visibility = Visibility.Visible;
            
            // Afficher la section Programme détaillée uniquement en mode Programme
            if (_modeAffichage == "programme")
            {
                PanelProgrammeDetails.Visibility = Visibility.Visible;
            }
            else
            {
                PanelProgrammeDetails.Visibility = Visibility.Collapsed;
            }

            // Mettre à jour les en-têtes des colonnes du DataGrid
            MettreAJourEntetes();
        }
        
        private void MettreAJourEntetes()
        {
            // Mettre à jour les en-têtes des colonnes du DataGrid Contributions
            if (ListeContributions?.Columns != null && ListeContributions.Columns.Count >= 6)
            {
                ((DataGridTextColumn)ListeContributions.Columns[0]).Header = LocalizationService.Instance.GetString("Reporting_Members");
                ((DataGridTextColumn)ListeContributions.Columns[1]).Header = LocalizationService.Instance.GetString("Reporting_Team");
                ((DataGridTextColumn)ListeContributions.Columns[2]).Header = LocalizationService.Instance.GetString("Reporting_CompletedTasks");
                ((DataGridTextColumn)ListeContributions.Columns[3]).Header = LocalizationService.Instance.GetString("Reporting_CompletionRate");
                ((DataGridTextColumn)ListeContributions.Columns[4]).Header = LocalizationService.Instance.GetString("Reporting_EstimatedDays");
                ((DataGridTemplateColumn)ListeContributions.Columns[5]).Header = LocalizationService.Instance.GetString("Reporting_Contribution");
            }
        }

        private void MasquerKPIs()
        {
            PanelAucuneSelection.Visibility = Visibility.Visible;
            PanelKPIs.Visibility = Visibility.Collapsed;
            PanelComparatif.Visibility = Visibility.Collapsed;
            PanelTimelineGains.Visibility = Visibility.Collapsed;
            PanelContributions.Visibility = Visibility.Collapsed;
            PanelProgrammeDetails.Visibility = Visibility.Collapsed;
        }

        private void CalculerEtAfficherStatsComparatives(List<BacklogItem> toutesLesTaches)
        {
            // Calculer les stats pour les 3 périodes
            var statsAvant = CalculerStatsPeriode(toutesLesTaches, null, _dateDebutFiltre);
            var statsPeriode = CalculerStatsPeriode(toutesLesTaches, _dateDebutFiltre, _dateFinFiltre);
            var statsReste = CalculerStatsPeriode(toutesLesTaches, _dateFinFiltre, null, true);

            // Si tout est terminé (rien à faire), afficher en 2 colonnes
            bool toutEstFait = statsReste.NbTaches == 0;

            if (toutEstFait)
            {
                GridComparatif3Col.Visibility = Visibility.Collapsed;
                GridComparatif2Col.Visibility = Visibility.Visible;

                // Remplir la version 2 colonnes
                TxtAvantTaches2Col.Text = statsAvant.NbTaches.ToString();
                TxtAvantCharge2Col.Text = string.Format("{0:0.#}j", statsAvant.ChargeJours);

                TxtPeriodeTaches2Col.Text = statsPeriode.NbTaches.ToString();
                TxtPeriodeCharge2Col.Text = string.Format("{0:0.#}j", statsPeriode.ChargeJours);
            }
            else
            {
                GridComparatif3Col.Visibility = Visibility.Visible;
                GridComparatif2Col.Visibility = Visibility.Collapsed;

                // Remplir la version 3 colonnes
                TxtAvantTaches.Text = statsAvant.NbTaches.ToString();
                TxtAvantCharge.Text = string.Format("{0:0.#}j", statsAvant.ChargeJours);

                TxtPeriodeTaches.Text = statsPeriode.NbTaches.ToString();
                TxtPeriodeCharge.Text = string.Format("{0:0.#}j", statsPeriode.ChargeJours);

                TxtResteTaches.Text = statsReste.NbTaches.ToString();
                TxtResteCharge.Text = string.Format("{0:0.#}j", statsReste.ChargeJours);
            }
        }

        private class StatsPeriode
        {
            public int NbTaches { get; set; }
            public double ChargeJours { get; set; }
        }

        private StatsPeriode CalculerStatsPeriode(List<BacklogItem> taches, DateTime? dateDebut, DateTime? dateFin, bool sontTachesRestantes = false)
        {
            var stats = new StatsPeriode();

            List<BacklogItem> tachesFiltrees;

            if (sontTachesRestantes)
            {
                // Tâches restantes = tâches non terminées après la période
                tachesFiltrees = taches.Where(t => 
                {
                    bool estNonTerminee = t.Statut != Statut.Termine && !t.EstArchive;
                    
                    if (!dateFin.HasValue)
                    {
                        return estNonTerminee;
                    }

                    // Doit être non terminée ET avoir été créée après la période OU toujours en cours après la période
                    var tacheDebut = t.DateDebut ?? t.DateCreation;
                    return estNonTerminee && (tacheDebut > dateFin.Value || 
                           !t.DateFinAttendue.HasValue || t.DateFinAttendue.Value > dateFin.Value);
                }).ToList();
            }
            else if (!dateDebut.HasValue && dateFin.HasValue)
            {
                // Avant la période : tâches terminées avant la date de début
                tachesFiltrees = taches.Where(t => 
                {
                    bool estTerminee = t.Statut == Statut.Termine || t.EstArchive;
                    var tacheDebut = t.DateDebut ?? t.DateCreation;
                    var tacheFin = t.DateFinAttendue ?? DateTime.Now;
                    
                    // Tâche terminée ET commencée et finie avant le début de la période
                    return estTerminee && tacheFin < dateFin.Value;
                }).ToList();
            }
            else if (dateDebut.HasValue && dateFin.HasValue)
            {
                // Pendant la période : tâches actives pendant cette période
                tachesFiltrees = taches.Where(t =>
                {
                    var tacheDebut = t.DateDebut ?? t.DateCreation;
                    var tacheFin = t.DateFinAttendue ?? DateTime.Now;
                    
                    // Si terminée, vérifier qu'elle était active pendant la période
                    if (t.Statut == Statut.Termine || t.EstArchive)
                    {
                        return tacheDebut <= dateFin.Value && tacheFin >= dateDebut.Value;
                    }
                    
                    // Si en cours, vérifier qu'elle a été créée avant la fin de la période
                    return tacheDebut <= dateFin.Value && 
                           (!t.DateFinAttendue.HasValue || t.DateFinAttendue.Value >= dateDebut.Value);
                }).ToList();
            }
            else
            {
                // Toutes les tâches
                tachesFiltrees = taches;
            }

            stats.NbTaches = tachesFiltrees.Count;
            stats.ChargeJours = tachesFiltrees.Sum(t => t.ChiffrageHeures.HasValue ? t.ChiffrageHeures.Value / 8.0 : 0);

            return stats;
        }

        private void CalculerStatistiquesSupplementaires(List<BacklogItem> tachesPeriode, List<BacklogItem> toutesLesTaches, List<Projet> projets)
        {
            // 1. Vélocité (tâches terminées par mois dans la période)
            if (_dateDebutFiltre.HasValue && _dateFinFiltre.HasValue)
            {
                var tachesTerminees = tachesPeriode.Count(t => t.Statut == Statut.Termine || t.EstArchive);
                var dureeEnMois = Math.Max(1, (_dateFinFiltre.Value - _dateDebutFiltre.Value).TotalDays / 30.0);
                var velocite = Math.Round(tachesTerminees / dureeEnMois, 1);
                TxtVelocite.Text = velocite.ToString("0.#");
            }
            else
            {
                // Vue globale - calculer sur toute la durée du projet/programme
                var tachesTerminees = tachesPeriode.Count(t => t.Statut == Statut.Termine || t.EstArchive);
                var dateMin = toutesLesTaches.Min(t => t.DateCreation);
                var dureeEnMois = Math.Max(1, (DateTime.Now - dateMin).TotalDays / 30.0);
                var velocite = Math.Round(tachesTerminees / dureeEnMois, 1);
                TxtVelocite.Text = velocite.ToString("0.#");
            }

            // 2. Écart Charge (Réel vs Estimé)
            var tachesAvecEstimationEtReel = tachesPeriode.Where(t => 
                t.ChiffrageHeures.HasValue && t.TempsReelHeures.HasValue).ToList();
            
            if (tachesAvecEstimationEtReel.Any())
            {
                var totalEstime = tachesAvecEstimationEtReel.Sum(t => t.ChiffrageHeures.Value);
                var totalReel = tachesAvecEstimationEtReel.Sum(t => t.TempsReelHeures.Value);
                var ecartPourcent = totalEstime > 0 ? Math.Round(((totalReel - totalEstime) / totalEstime) * 100, 0) : 0;
                
                TxtEcartCharge.Text = ecartPourcent >= 0 ? $"+{ecartPourcent}%" : $"{ecartPourcent}%";
                
                // Changer la couleur selon l'écart
                if (ecartPourcent > 20)
                {
                    TxtEcartCharge.Foreground = new SolidColorBrush(Color.FromRgb(244, 67, 54)); // Rouge
                }
                else if (ecartPourcent > 0)
                {
                    TxtEcartCharge.Foreground = new SolidColorBrush(Color.FromRgb(255, 152, 0)); // Orange
                }
                else
                {
                    TxtEcartCharge.Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80)); // Vert
                }
            }
            else
            {
                TxtEcartCharge.Text = "-";
                TxtEcartCharge.Foreground = new SolidColorBrush(Color.FromRgb(153, 153, 153));
            }

            // 3. Répartition par Priorité
            int prioUrgent = tachesPeriode.Count(t => t.Priorite == Priorite.Urgent);
            int prioHaute = tachesPeriode.Count(t => t.Priorite == Priorite.Haute);
            int prioMoyenne = tachesPeriode.Count(t => t.Priorite == Priorite.Moyenne);
            int prioBasse = tachesPeriode.Count(t => t.Priorite == Priorite.Basse);
            
            TxtPrioriteHaute.Text = (prioUrgent + prioHaute).ToString();
            TxtPrioriteMoyenne.Text = prioMoyenne.ToString();
            TxtPrioriteBasse.Text = prioBasse.ToString();

            // 4. Statut RAG (si disponible dans les projets)
            // Toujours afficher le RAG
            BorderStatutRAG.Visibility = Visibility.Visible;
            
            if (projets.Count == 1)
            {
                string statutRAG = projets[0].StatutRAG;
                
                // Si pas de statut défini, calculer automatiquement
                if (string.IsNullOrEmpty(statutRAG))
                {
                    // Calculer basé sur avancement et retards
                    int totalTaches = tachesPeriode.Count;
                    int tachesCompletes = tachesPeriode.Count(t => t.Statut == Statut.Termine || t.EstArchive);
                    double avancement = totalTaches > 0 ? (double)tachesCompletes / totalTaches * 100 : 0;
                    
                    var maintenant = DateTime.Now;
                    var nbEnRetard = tachesPeriode.Count(t => 
                        t.Statut != Statut.Termine && 
                        !t.EstArchive && 
                        t.DateFinAttendue.HasValue && 
                        t.DateFinAttendue.Value < maintenant);
                    
                    var tauxRetard = totalTaches > 0 ? (nbEnRetard * 100.0 / totalTaches) : 0;
                    
                    if (tauxRetard > 30 || (avancement < 30 && nbEnRetard > 0))
                        statutRAG = "Red";
                    else if (tauxRetard > 15 || (avancement < 60 && nbEnRetard > 3))
                        statutRAG = "Amber";
                    else
                        statutRAG = "Green";
                }
                
                switch (statutRAG.ToLower())
                {
                    case "green":
                        BadgeRAG.Background = new SolidColorBrush(Color.FromRgb(76, 175, 80)); // #4CAF50
                        TxtStatutRAG.Text = "GREEN";
                        LblOnTrack.Text = LocalizationService.Instance.GetString("Reporting_OnTime");
                        LblOnTrack.Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                        break;
                    case "amber":
                    case "orange":
                        BadgeRAG.Background = new SolidColorBrush(Color.FromRgb(255, 152, 0)); // #FF9800
                        TxtStatutRAG.Text = "AMBER";
                        LblOnTrack.Text = LocalizationService.Instance.GetString("Reporting_AtRisk");
                        LblOnTrack.Foreground = new SolidColorBrush(Color.FromRgb(255, 152, 0));
                        break;
                    case "red":
                        BadgeRAG.Background = new SolidColorBrush(Color.FromRgb(244, 67, 54)); // #F44336
                        TxtStatutRAG.Text = "RED";
                        LblOnTrack.Text = LocalizationService.Instance.GetString("Reporting_DelayedStatus");
                        LblOnTrack.Foreground = new SolidColorBrush(Color.FromRgb(244, 67, 54));
                        break;
                    default:
                        BadgeRAG.Background = new SolidColorBrush(Color.FromRgb(158, 158, 158)); // Grey
                        TxtStatutRAG.Text = "N/A";
                        LblOnTrack.Text = LocalizationService.Instance.GetString("Reporting_Undefined");
                        LblOnTrack.Foreground = new SolidColorBrush(Color.FromRgb(153, 153, 153));
                        break;
                }
            }
            else if (projets.Count > 1)
            {
                // Pour un programme, calculer le pire statut
                bool hasRed = projets.Any(p => p.StatutRAG?.ToLower() == "red");
                bool hasAmber = projets.Any(p => p.StatutRAG?.ToLower() == "amber" || p.StatutRAG?.ToLower() == "orange");
                
                if (hasRed)
                {
                    BadgeRAG.Background = new SolidColorBrush(Color.FromRgb(244, 67, 54));
                    TxtStatutRAG.Text = "RED";
                    LblOnTrack.Text = LocalizationService.Instance.GetString("Reporting_ProjectsDelayed");
                    LblOnTrack.Foreground = new SolidColorBrush(Color.FromRgb(244, 67, 54));
                }
                else if (hasAmber)
                {
                    BadgeRAG.Background = new SolidColorBrush(Color.FromRgb(255, 152, 0));
                    TxtStatutRAG.Text = "AMBER";
                    LblOnTrack.Text = LocalizationService.Instance.GetString("Reporting_ProjectsAtRisk");
                    LblOnTrack.Foreground = new SolidColorBrush(Color.FromRgb(255, 152, 0));
                }
                else
                {
                    BadgeRAG.Background = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                    TxtStatutRAG.Text = "GREEN";
                    LblOnTrack.Text = LocalizationService.Instance.GetString("Reporting_AllOnTime");
                    LblOnTrack.Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                }
            }

            // 5. Tâches en retard
            var now = DateTime.Now;
            var tachesEnRetard = tachesPeriode.Count(t => 
                t.Statut != Statut.Termine && 
                !t.EstArchive && 
                t.DateFinAttendue.HasValue && 
                t.DateFinAttendue.Value < now);
            
            TxtTachesRetard.Text = tachesEnRetard.ToString();
            
            if (tachesEnRetard > 0)
            {
                LblRetardTasks.Text = tachesEnRetard == 1 ? "tâche en retard" : "tâches en retard";
                TxtTachesRetard.Foreground = new SolidColorBrush(Color.FromRgb(244, 67, 54));
            }
            else
            {
                LblRetardTasks.Text = LocalizationService.Instance.GetString("Reporting_NoDelay");
                TxtTachesRetard.Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80));
            }

            // 6. Complexité moyenne
            var tachesAvecComplexite = tachesPeriode.Where(t => t.Complexite.HasValue).ToList();
            if (tachesAvecComplexite.Any())
            {
                var complexiteMoyenne = Math.Round(tachesAvecComplexite.Average(t => t.Complexite.Value), 1);
                TxtComplexiteMoyenne.Text = complexiteMoyenne.ToString("0.#");
            }
            else
            {
                TxtComplexiteMoyenne.Text = "-";
            }
        }
        
        private void RemplirInformationsProgramme(Programme programme)
        {
            // Nom du programme
            TxtNomProgramme.Text = programme.Nom;
            
            // Récupérer les projets du programme
            var projets = _database.GetProjets().Where(p => p.ProgrammeId == programme.Id && p.Actif)
                .OrderBy(p => p.DateDebut ?? DateTime.MaxValue)
                .ToList();
            
            // Récupérer toutes les tâches pour avoir plus d'infos
            var backlogService = new BacklogService(_database);
            var toutesLesTaches = backlogService.GetAllBacklogItemsIncludingArchived();
            
            // Stocker pour usage dans la génération IA
            _currentProgramme = programme;
            _currentProjets = projets;
            _currentTaches = toutesLesTaches;
            
            // Afficher les phases projet par projet (dynamiquement)
            AfficherPhasesDynamiques(projets, toutesLesTaches);
            
            // Pré-remplir Change Management avec des informations pertinentes de gestion du changement
            TxtChangeManagement.Children.Clear();
            
            // Analyse dynamique des changements dans la période
            var dateAnalyse = _dateDebutFiltre ?? DateTime.Now.AddMonths(-3);
            var tachesPeriode = toutesLesTaches
                .Where(t => projets.Any(p => p.Id == t.ProjetId) && 
                           t.DateCreation >= dateAnalyse)
                .ToList();
            
            var changementsStatut = toutesLesTaches
                .Where(t => projets.Any(p => p.Id == t.ProjetId) && 
                           t.DateDerniereMaj >= dateAnalyse &&
                           t.DateDerniereMaj != t.DateCreation)
                .Count();
            
            var termineesCount = toutesLesTaches.Count(t => projets.Any(p => p.Id == t.ProjetId) && t.Statut == Statut.Termine && t.DateFin >= dateAnalyse);
            
            // Statistiques compactes
            var statsBlock = new TextBlock { Margin = new Thickness(5, 5, 5, 8), FontSize = 11, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666")) };
            statsBlock.Inlines.Add(new Run("• ") { FontWeight = FontWeights.Bold });
            statsBlock.Inlines.Add(new Run(tachesPeriode.Count.ToString()) { FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00915A")) });
            statsBlock.Inlines.Add(new Run($" {LocalizationService.Instance.GetString("Reporting_NewRequestsCreated")}"));
            TxtChangeManagement.Children.Add(statsBlock);
            
            var modifsBlock = new TextBlock { Margin = new Thickness(5, 2, 5, 2), FontSize = 11, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666")) };
            modifsBlock.Inlines.Add(new Run("• ") { FontWeight = FontWeights.Bold });
            modifsBlock.Inlines.Add(new Run(changementsStatut.ToString()) { FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF9800")) });
            modifsBlock.Inlines.Add(new Run($" {LocalizationService.Instance.GetString("Reporting_RecentModifications")}"));
            TxtChangeManagement.Children.Add(modifsBlock);
            
            var termBlock = new TextBlock { Margin = new Thickness(5, 2, 5, 15), FontSize = 11, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666")) };
            termBlock.Inlines.Add(new Run("• ") { FontWeight = FontWeights.Bold });
            termBlock.Inlines.Add(new Run(termineesCount.ToString()) { FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2196F3")) });
            termBlock.Inlines.Add(new Run($" {LocalizationService.Instance.GetString("Reporting_CompletedTasks")}"));
            TxtChangeManagement.Children.Add(termBlock);
            
            // Plan d'action compact basé UNIQUEMENT sur les données de la période sélectionnée
            var planTitreBlock = new TextBlock
            {
                Text = LocalizationService.Instance.GetString("Reporting_ActionPlan"),
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333")),
                Margin = new Thickness(5, 10, 5, 8)
            };
            TxtChangeManagement.Children.Add(planTitreBlock);
            
            // 1. Analyser les types de tâches créées dans la période
            var typesUniques = tachesPeriode.Select(t => t.TypeDemande).Distinct().Count();
            var guide = new TextBlock
            {
                FontSize = 11,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#555")),
                Margin = new Thickness(5, 3, 5, 2),
                TextWrapping = TextWrapping.Wrap
            };
            guide.Inlines.Add(new Run("1   ") { FontWeight = FontWeights.Bold });
            guide.Inlines.Add(new Run(LocalizationService.Instance.GetString("Reporting_UserGuideAndDoc")) { FontWeight = FontWeights.SemiBold });
            
            if (tachesPeriode.Any())
            {
                guide.Inlines.Add(new Run($"{tachesPeriode.Count} {LocalizationService.Instance.GetString("Reporting_RequiringDoc")} ({typesUniques} {LocalizationService.Instance.GetString("Reporting_DifferentTypes")})"));
            }
            else
            {
                guide.Inlines.Add(new Run(LocalizationService.Instance.GetString("Reporting_NoNewRequests")));
            }
            TxtChangeManagement.Children.Add(guide);
            
            // 2. Analyser les développeurs assignés dans la période
            var devsAssignes = tachesPeriode.Where(t => t.DevAssigneId.HasValue)
                                            .Select(t => t.DevAssigneId.Value)
                                            .Distinct()
                                            .ToList();
            var users = _database.GetUtilisateurs();
            var devsDetails = devsAssignes.Select(devId => users.FirstOrDefault(u => u.Id == devId))
                                          .Where(u => u != null)
                                          .ToList();
            
            var training = new TextBlock
            {
                FontSize = 11,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#555")),
                Margin = new Thickness(5, 8, 5, 2),
                TextWrapping = TextWrapping.Wrap
            };
            training.Inlines.Add(new Run("2   ") { FontWeight = FontWeights.Bold });
            training.Inlines.Add(new Run(LocalizationService.Instance.GetString("Reporting_TrainingUsers")) { FontWeight = FontWeights.SemiBold });
            
            if (devsDetails.Any())
            {
                var devsNoms = string.Join(", ", devsDetails.Take(2).Select(d => d.Nom));
                if (devsDetails.Count > 2) devsNoms += $" +{devsDetails.Count - 2}";
                training.Inlines.Add(new Run($"{devsDetails.Count} {LocalizationService.Instance.GetString("Reporting_ActiveDevelopers")} ({devsNoms}) - {LocalizationService.Instance.GetString("Reporting_ContinuousTraining")}"));
            }
            else
            {
                training.Inlines.Add(new Run(LocalizationService.Instance.GetString("Reporting_NoDevAssigned")));
            }
            TxtChangeManagement.Children.Add(training);
            
            // 3. Analyser les jalons dans la période
            var dateDebut = _dateDebutFiltre ?? DateTime.MinValue;
            var dateFin = _dateFinFiltre ?? DateTime.MaxValue;
            var projetsActivePeriode = projets.Where(p => 
                (!p.DateDebut.HasValue || p.DateDebut.Value <= dateFin) &&
                (!p.DateFinPrevue.HasValue || p.DateFinPrevue.Value >= dateDebut)
            ).ToList();
            
            var rollout = new TextBlock
            {
                FontSize = 11,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#555")),
                Margin = new Thickness(5, 8, 5, 2),
                TextWrapping = TextWrapping.Wrap
            };
            rollout.Inlines.Add(new Run("3   ") { FontWeight = FontWeights.Bold });
            rollout.Inlines.Add(new Run(LocalizationService.Instance.GetString("Reporting_RolloutStrategy")) { FontWeight = FontWeights.SemiBold });
            
            if (projetsActivePeriode.Any())
            {
                var projetsNoms = string.Join(", ", projetsActivePeriode.Take(2).Select(p => p.Nom));
                if (projetsActivePeriode.Count > 2) projetsNoms += $" +{projetsActivePeriode.Count - 2}";
                rollout.Inlines.Add(new Run($"{projetsActivePeriode.Count} {LocalizationService.Instance.GetString("Reporting_ActiveProjects")} ({projetsNoms})"));
            }
            else
            {
                rollout.Inlines.Add(new Run(LocalizationService.Instance.GetString("Reporting_NoActiveProjects")));
            }
            TxtChangeManagement.Children.Add(rollout);
            
            // 4. Analyser l'activité réelle dans la période
            var tachesEnCours = tachesPeriode.Count(t => t.Statut == Statut.EnCours);
            var tachesHautePrio = tachesPeriode.Count(t => t.Priorite == Priorite.Haute);
            
            var support = new TextBlock
            {
                FontSize = 11,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#555")),
                Margin = new Thickness(5, 8, 5, 2),
                TextWrapping = TextWrapping.Wrap
            };
            support.Inlines.Add(new Run("4   ") { FontWeight = FontWeights.Bold });
            support.Inlines.Add(new Run(LocalizationService.Instance.GetString("Reporting_SupportAndFeedback")) { FontWeight = FontWeights.SemiBold });
            
            if (tachesPeriode.Any())
            {
                support.Inlines.Add(new Run($"{tachesEnCours} {LocalizationService.Instance.GetString("Reporting_StartedTasks")}"));
                if (tachesHautePrio > 0)
                {
                    support.Inlines.Add(new Run($", {tachesHautePrio} {LocalizationService.Instance.GetString("Reporting_HighPriority")}") { FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E53935")) });
                }
                support.Inlines.Add(new Run($" - {LocalizationService.Instance.GetString("Reporting_ActiveTracking")}"));
            }
            else
            {
                support.Inlines.Add(new Run(LocalizationService.Instance.GetString("Reporting_NoActivityOnPeriod")));
            }
            TxtChangeManagement.Children.Add(support);
            
            // Pré-remplir Evolving Scope avec les tâches récentes ajoutées
            TxtEvolvingScope.Children.Clear();
            
            var dateRecente = _dateDebutFiltre ?? DateTime.Now.AddMonths(-3);
            var tachesRecentes = toutesLesTaches
                .Where(t => projets.Any(p => p.Id == t.ProjetId) && 
                           t.DateCreation >= dateRecente)
                .OrderByDescending(t => t.DateCreation)
                .Take(10)
                .ToList();
            
            // Header
            var evolvingHeaderBlock = new TextBlock
            {
                Text = LocalizationService.Instance.GetString("Reporting_EvolvingScope"),
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2196F3")),
                Margin = new Thickness(5, 5, 5, 10)
            };
            TxtEvolvingScope.Children.Add(evolvingHeaderBlock);
            
            if (tachesRecentes.Any())
            {
                var dateInfoBlock = new TextBlock
                {
                    Text = $"{LocalizationService.Instance.GetString("Reporting_NewRequestsSince")} {dateRecente:dd/MM/yyyy}",
                    FontSize = 12,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333")),
                    Margin = new Thickness(5, 0, 5, 15)
                };
                TxtEvolvingScope.Children.Add(dateInfoBlock);
                
                var groupeesParProjet = tachesRecentes.GroupBy(t => t.ProjetId);
                foreach (var groupe in groupeesParProjet)
                {
                    var projetNom = projets.FirstOrDefault(p => p.Id == groupe.Key)?.Nom ?? LocalizationService.Instance.GetString("Reporting_UnknownProject");
                    var projetBlock = new TextBlock
                    {
                        Text = $"📁 {projetNom}",
                        FontSize = 12,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#555")),
                        Margin = new Thickness(5, 10, 5, 5)
                    };
                    TxtEvolvingScope.Children.Add(projetBlock);
                    
                    foreach (var tache in groupe)
                    {
                        var tacheBlock = new TextBlock { Margin = new Thickness(10, 2, 5, 2), FontSize = 11 };
                        
                        var priorite = tache.Priorite == Priorite.Haute ? "🔴" : 
                                      tache.Priorite == Priorite.Moyenne ? "🟠" : "🟢";
                        tacheBlock.Inlines.Add(new Run($"{priorite} "));
                        tacheBlock.Inlines.Add(new Run($"[{tache.DateCreation:dd/MM}] ") { Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#999")) });
                        tacheBlock.Inlines.Add(new Run(tache.Titre) { Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666")) });
                        
                        TxtEvolvingScope.Children.Add(tacheBlock);
                    }
                }
                
                // Total et statistiques
                var totalBlock = new TextBlock
                {
                    FontSize = 13,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333")),
                    Margin = new Thickness(5, 20, 5, 5)
                };
                totalBlock.Inlines.Add(new Run(LocalizationService.Instance.GetString("Reporting_Total")));
                totalBlock.Inlines.Add(new Run($"{tachesRecentes.Count}") { Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2196F3")) });
                totalBlock.Inlines.Add(new Run($" {LocalizationService.Instance.GetString("Reporting_NewRequestsCreated")}"));
                TxtEvolvingScope.Children.Add(totalBlock);
                
                // Statistiques par priorité
                var nbHaute = tachesRecentes.Count(t => t.Priorite == Priorite.Haute);
                var nbMoyenne = tachesRecentes.Count(t => t.Priorite == Priorite.Moyenne);
                var nbBasse = tachesRecentes.Count(t => t.Priorite == Priorite.Basse);
                
                var priTitreBlock = new TextBlock
                {
                    Text = LocalizationService.Instance.GetString("Reporting_DistributionByPriority"),
                    FontSize = 12,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#555")),
                    Margin = new Thickness(5, 10, 5, 5)
                };
                TxtEvolvingScope.Children.Add(priTitreBlock);
                
                if (nbHaute > 0)
                {
                    var hauteBlock = new TextBlock { Margin = new Thickness(10, 2, 5, 2), FontSize = 11 };
                    hauteBlock.Inlines.Add(new Run($"🔴 {LocalizationService.Instance.GetString("Reporting_High")}"));
                    hauteBlock.Inlines.Add(new Run(nbHaute.ToString()) { FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E53935")) });
                    TxtEvolvingScope.Children.Add(hauteBlock);
                }
                
                if (nbMoyenne > 0)
                {
                    var moyenneBlock = new TextBlock { Margin = new Thickness(10, 2, 5, 2), FontSize = 11 };
                    moyenneBlock.Inlines.Add(new Run($"🟠 {LocalizationService.Instance.GetString("Reporting_Medium")}"));
                    moyenneBlock.Inlines.Add(new Run(nbMoyenne.ToString()) { FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF9800")) });
                    TxtEvolvingScope.Children.Add(moyenneBlock);
                }
                
                if (nbBasse > 0)
                {
                    var basseBlock = new TextBlock { Margin = new Thickness(10, 2, 5, 2), FontSize = 11 };
                    basseBlock.Inlines.Add(new Run($"🟢 {LocalizationService.Instance.GetString("Reporting_Low")}"));
                    basseBlock.Inlines.Add(new Run(nbBasse.ToString()) { FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50")) });
                    TxtEvolvingScope.Children.Add(basseBlock);
                }
            }
            else
            {
                var aucuneBlock = new TextBlock
                {
                    Text = LocalizationService.Instance.GetString("Reporting_NoNewRequests2"),
                    FontSize = 13,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#999")),
                    Margin = new Thickness(5, 10, 5, 10)
                };
                TxtEvolvingScope.Children.Add(aucuneBlock);
                
                var stableBlock = new TextBlock
                {
                    Text = LocalizationService.Instance.GetString("Reporting_StableScope"),
                    FontSize = 12,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666")),
                    Margin = new Thickness(5, 5, 5, 15),
                    TextWrapping = TextWrapping.Wrap
                };
                TxtEvolvingScope.Children.Add(stableBlock);
                
                var conseilTitreBlock = new TextBlock
                {
                    Text = LocalizationService.Instance.GetString("Reporting_Tip"),
                    FontSize = 12,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF9800")),
                    Margin = new Thickness(5, 10, 5, 5)
                };
                TxtEvolvingScope.Children.Add(conseilTitreBlock);
                
                var conseilBlock = new TextBlock
                {
                    Text = LocalizationService.Instance.GetString("Reporting_AdjustPeriod"),
                    FontSize = 11,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666")),
                    Margin = new Thickness(5, 0, 5, 5),
                    TextWrapping = TextWrapping.Wrap
                };
                TxtEvolvingScope.Children.Add(conseilBlock);
            }
        }
        
        private void RemplirTimelineProjets(List<Projet> projets, List<BacklogItem> toutesLesTaches)
        {
            TimelineHeader.Children.Clear();
            TimelineProjects.Children.Clear();
            
            if (!projets.Any())
            {
                var emptyBlock = new TextBlock
                {
                    Text = "Aucun projet dans ce programme",
                    FontSize = 13,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#999")),
                    FontStyle = FontStyles.Italic,
                    Margin = new Thickness(0, 10, 0, 10),
                    TextAlignment = TextAlignment.Center
                };
                TimelineProjects.Children.Add(emptyBlock);
                return;
            }
            
            // Trouver la plage de dates
            var projetsAvecDates = projets.Where(p => p.DateDebut.HasValue && p.DateFinPrevue.HasValue).ToList();
            if (!projetsAvecDates.Any())
            {
                var emptyBlock = new TextBlock
                {
                    Text = LocalizationService.Instance.GetString("Reporting_NoDates"),
                    FontSize = 13,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#999")),
                    FontStyle = FontStyles.Italic,
                    Margin = new Thickness(0, 10, 0, 10),
                    TextAlignment = TextAlignment.Center
                };
                TimelineProjects.Children.Add(emptyBlock);
                return;
            }
            
            var dateMin = projetsAvecDates.Min(p => p.DateDebut.Value);
            var dateMax = projetsAvecDates.Max(p => p.DateFinPrevue.Value);
            
            // Arrondir au début et à la fin du mois
            dateMin = new DateTime(dateMin.Year, dateMin.Month, 1);
            dateMax = new DateTime(dateMax.Year, dateMax.Month, DateTime.DaysInMonth(dateMax.Year, dateMax.Month));
            
            // Calculer le nombre total de jours
            var totalJours = (dateMax - dateMin).TotalDays;
            
            // Créer l'en-tête des mois
            TimelineHeader.ColumnDefinitions.Clear();
            var currentDate = dateMin;
            var moisIndex = 0;
            var moisList = new List<(DateTime date, int jours)>();
            
            while (currentDate <= dateMax)
            {
                var joursDansMois = DateTime.DaysInMonth(currentDate.Year, currentDate.Month);
                var dernierJourMois = new DateTime(currentDate.Year, currentDate.Month, joursDansMois);
                
                if (dernierJourMois > dateMax)
                    joursDansMois = (dateMax - currentDate).Days + 1;
                
                moisList.Add((currentDate, joursDansMois));
                
                // Créer une colonne proportionnelle
                TimelineHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(joursDansMois, GridUnitType.Star) });
                
                // Ajouter le texte du mois
                var borderMois = new Border
                {
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F5F5F5")),
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0")),
                    BorderThickness = new Thickness(0, 0, 1, 1),
                    Padding = new Thickness(5)
                };
                Grid.SetColumn(borderMois, moisIndex);
                
                var textMois = new TextBlock
                {
                    Text = currentDate.ToString("MMM yyyy", LocalizationService.Instance.CurrentCulture).ToUpper(),
                    FontSize = 11,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666")),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                borderMois.Child = textMois;
                TimelineHeader.Children.Add(borderMois);
                
                currentDate = currentDate.AddMonths(1);
                moisIndex++;
            }
            
            // Créer les barres de projet
            foreach (var projet in projetsAvecDates.OrderBy(p => p.DateDebut))
            {
                var tachesProjet = toutesLesTaches.Where(t => t.ProjetId == projet.Id).ToList();
                int totalTaches = tachesProjet.Count;
                int tachesCompletes = tachesProjet.Count(t => t.Statut == Statut.Termine || t.EstArchive);
                
                double pourcentage = totalTaches > 0 
                    ? Math.Round((double)tachesCompletes / totalTaches * 100, 0) 
                    : 0;
                
                // Calculer la position et la largeur de la barre
                var joursDebut = (projet.DateDebut.Value - dateMin).TotalDays;
                var joursTotal = (projet.DateFinPrevue.Value - projet.DateDebut.Value).TotalDays;
                
                // Créer le conteneur pour ce projet
                var projetContainer = new Grid { Margin = new Thickness(0, 0, 0, 15) };
                projetContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });
                projetContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                
                // Nom du projet avec nombre de tâches
                var nomProjet = new TextBlock
                {
                    Text = $"{projet.Nom} {tachesCompletes}/{totalTaches} tâches",
                    FontSize = 12,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333")),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 10, 0),
                    TextWrapping = TextWrapping.Wrap
                };
                Grid.SetColumn(nomProjet, 0);
                projetContainer.Children.Add(nomProjet);
                
                // Timeline Grid synchronisé avec les mois
                var timelineGrid = new Grid { Height = 35 };
                Grid.SetColumn(timelineGrid, 1);
                
                // Copier les mêmes colonnes que l'en-tête
                foreach (var colDef in TimelineHeader.ColumnDefinitions)
                {
                    timelineGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = colDef.Width });
                }
                
                // Canvas par dessus le Grid pour positionner librement la barre
                var timelineCanvas = new Canvas
                {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch
                };
                timelineGrid.Children.Add(timelineCanvas);
                
                // Binding qui sera calculé après le rendu
                timelineGrid.Loaded += (s, e) =>
                {
                    var largeurDisponible = timelineGrid.ActualWidth;
                    if (largeurDisponible <= 0) return;
                    
                    var pixelsParJour = largeurDisponible / totalJours;
                    var leftPosition = joursDebut * pixelsParJour;
                    var barreWidth = Math.Max(joursTotal * pixelsParJour, 30);
                    
                    // Barre de fond (orange clair)
                    var barreFond = new Border
                    {
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFE0B2")),
                        Height = 28,
                        Width = barreWidth,
                        CornerRadius = new CornerRadius(4)
                    };
                    Canvas.SetLeft(barreFond, leftPosition);
                    Canvas.SetTop(barreFond, 3);
                    timelineCanvas.Children.Add(barreFond);
                    
                    // Barre de progression (orange)
                    var barreProgression = new Border
                    {
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF9800")),
                        Height = 28,
                        Width = barreWidth * (pourcentage / 100.0),
                        CornerRadius = new CornerRadius(4),
                        HorizontalAlignment = HorizontalAlignment.Left
                    };
                    Canvas.SetLeft(barreProgression, leftPosition);
                    Canvas.SetTop(barreProgression, 3);
                    timelineCanvas.Children.Add(barreProgression);
                    
                    // Texte du pourcentage
                    var textPourcentage = new TextBlock
                    {
                        Text = $"{pourcentage}%",
                        FontSize = 11,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Colors.White),
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center
                    };
                    Canvas.SetLeft(textPourcentage, leftPosition + (barreWidth / 2) - 15);
                    Canvas.SetTop(textPourcentage, 8);
                    timelineCanvas.Children.Add(textPourcentage);
                };
                
                projetContainer.Children.Add(timelineGrid);
                TimelineProjects.Children.Add(projetContainer);
            }
            
            // Ajouter l'indicateur "Today" après le premier projet pour qu'il soit au-dessus de toutes les barres
            if (projetsAvecDates.Any())
            {
                var todayContainer = new Grid { Margin = new Thickness(0, -(projetsAvecDates.Count * 50 + 40), 0, 0) };
                todayContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });
                todayContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                
                // Timeline Grid synchronisé avec les mois
                var todayTimelineGrid = new Grid();
                Grid.SetColumn(todayTimelineGrid, 1);
                
                // Copier les mêmes colonnes que l'en-tête
                foreach (var colDef in TimelineHeader.ColumnDefinitions)
                {
                    todayTimelineGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = colDef.Width });
                }
                
                // Canvas pour positionner l'indicateur
                var todayCanvas = new Canvas
                {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch
                };
                todayTimelineGrid.Children.Add(todayCanvas);
                
                // Binding après le rendu
                todayTimelineGrid.Loaded += (s, e) =>
                {
                    var largeurDisponible = todayTimelineGrid.ActualWidth;
                    if (largeurDisponible <= 0) return;
                    
                    var today = DateTime.Now;
                    if (today >= dateMin && today <= dateMax)
                    {
                        var pixelsParJour = largeurDisponible / totalJours;
                        var joursDepuisDebut = (today - dateMin).TotalDays;
                        var todayPosition = joursDepuisDebut * pixelsParJour;
                        
                        // Ligne verticale rouge avec effet d'ombre
                        var ligneOmbre = new Border
                        {
                            Background = new SolidColorBrush(Color.FromArgb(40, 255, 0, 0)),
                            Width = 3,
                            Height = projetsAvecDates.Count * 50 + 30
                        };
                        Canvas.SetLeft(ligneOmbre, todayPosition - 1.5);
                        Canvas.SetTop(ligneOmbre, 25);
                        todayCanvas.Children.Add(ligneOmbre);
                        
                        var ligne = new Border
                        {
                            Background = new SolidColorBrush(Color.FromRgb(239, 83, 80)),
                            Width = 2,
                            Height = projetsAvecDates.Count * 50 + 30
                        };
                        Canvas.SetLeft(ligne, todayPosition - 1);
                        Canvas.SetTop(ligne, 25);
                        todayCanvas.Children.Add(ligne);
                        
                        // Triangle pointant vers le bas (flèche) avec bordure
                        var triangleOmbre = new Polygon
                        {
                            Fill = new SolidColorBrush(Color.FromArgb(60, 0, 0, 0)),
                            Points = new PointCollection
                            {
                                new System.Windows.Point(-7, 0),
                                new System.Windows.Point(7, 0),
                                new System.Windows.Point(0, 12)
                            }
                        };
                        Canvas.SetLeft(triangleOmbre, todayPosition);
                        Canvas.SetTop(triangleOmbre, 14);
                        todayCanvas.Children.Add(triangleOmbre);
                        
                        var triangle = new Polygon
                        {
                            Fill = new SolidColorBrush(Color.FromRgb(239, 83, 80)),
                            Points = new PointCollection
                            {
                                new System.Windows.Point(-6, 0),
                                new System.Windows.Point(6, 0),
                                new System.Windows.Point(0, 10)
                            }
                        };
                        Canvas.SetLeft(triangle, todayPosition);
                        Canvas.SetTop(triangle, 14);
                        todayCanvas.Children.Add(triangle);
                        
                        // Label "Today" avec effet d'ombre
                        var todayLabelOmbre = new Border
                        {
                            Background = new SolidColorBrush(Color.FromArgb(40, 0, 0, 0)),
                            CornerRadius = new CornerRadius(4),
                            Padding = new Thickness(8, 3, 8, 3)
                        };
                        Canvas.SetLeft(todayLabelOmbre, todayPosition - 22);
                        Canvas.SetTop(todayLabelOmbre, -2);
                        todayCanvas.Children.Add(todayLabelOmbre);
                        
                        var todayLabel = new Border
                        {
                            Background = new SolidColorBrush(Color.FromRgb(239, 83, 80)),
                            CornerRadius = new CornerRadius(4),
                            Padding = new Thickness(8, 3, 8, 3),
                            Child = new TextBlock
                            {
                                Text = "Today",
                                FontSize = 10,
                                FontWeight = FontWeights.Bold,
                                Foreground = new SolidColorBrush(Colors.White)
                            }
                        };
                        Canvas.SetLeft(todayLabel, todayPosition - 23);
                        Canvas.SetTop(todayLabel, -4);
                        todayCanvas.Children.Add(todayLabel);
                    }
                };
                
                todayContainer.Children.Add(todayTimelineGrid);
                TimelineProjects.Children.Add(todayContainer);
            }
        }
        
        /*
        private void RemplirProgressStatus(List<Projet> projets, List<BacklogItem> toutesLesTaches)
        {
            if (GridProgressStatus == null || TxtHighPriorityCount == null || BadgeStatutProjet == null || TxtStatutProjet == null)
                return;
                
            var progressData = new List<ProgressStatusRow>();
            
            foreach (var projet in projets)
            {
                var tachesProjet = toutesLesTaches.Where(t => t.ProjetId == projet.Id).ToList();
                int totalTaches = tachesProjet.Count;
                int tachesCompletes = tachesProjet.Count(t => t.Statut == Statut.Termine || t.EstArchive);
                double pourcentage = totalTaches > 0 ? Math.Round((double)tachesCompletes / totalTaches * 100, 0) : 0;
                
                // Extraire les bénéficiaires
                string beneficiaire = "N/A";
                if (!string.IsNullOrEmpty(projet.Beneficiaires))
                {
                    try
                    {
                        var benefs = System.Text.Json.JsonSerializer.Deserialize<List<string>>(projet.Beneficiaires);
                        if (benefs != null && benefs.Any())
                        {
                            beneficiaire = string.Join(", ", benefs);
                        }
                    }
                    catch
                    {
                        beneficiaire = projet.Beneficiaires;
                    }
                }
                
                // Key Highlights - extraire des informations pertinentes
                var highlights = new List<string>();
                
                // Ajouter le nombre de tâches en cours
                var tachesEnCours = tachesProjet.Count(t => t.Statut == Statut.EnCours);
                if (tachesEnCours > 0)
                {
                    highlights.Add($"• {tachesEnCours} tâche(s) en cours");
                }
                
                // Ajouter les tâches hautement prioritaires
                var tachesHautePrio = tachesProjet.Count(t => t.Priorite == Priorite.Haute || t.Priorite == Priorite.Urgent);
                if (tachesHautePrio > 0)
                {
                    highlights.Add($"• {tachesHautePrio} tâche(s) haute priorité");
                }
                
                // Ajouter l'ambition si définie
                if (!string.IsNullOrEmpty(projet.Ambition) && projet.Ambition != "Non spécifiée")
                {
                    highlights.Add($"• {projet.Ambition}");
                }
                
                // Ajouter les prochaines actions si définies
                if (!string.IsNullOrEmpty(projet.NextActions))
                {
                    var nextActionsLines = projet.NextActions.Split('\n').Take(2);
                    foreach (var line in nextActionsLines)
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            highlights.Add($"• {line.Trim()}");
                        }
                    }
                }
                
                string keyHighlights = highlights.Any() ? string.Join("\n", highlights) : LocalizationService.Instance.GetString("Reporting_InDevelopment");
                
                // Déterminer la couleur RAG
                SolidColorBrush ragBrush = new SolidColorBrush(Color.FromRgb(76, 175, 80)); // Vert par défaut
                if (!string.IsNullOrEmpty(projet.StatutRAG))
                {
                    switch (projet.StatutRAG.ToLower())
                    {
                        case "green":
                            ragBrush = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                            break;
                        case "amber":
                        case "orange":
                            ragBrush = new SolidColorBrush(Color.FromRgb(255, 152, 0));
                            break;
                        case "red":
                            ragBrush = new SolidColorBrush(Color.FromRgb(244, 67, 54));
                            break;
                    }
                }
                
                // Initial ETA et Updated ETA
                string initialETA = projet.DateDebut?.ToString("MMM yyyy", LocalizationService.Instance.CurrentCulture) ?? "N/A";
                string updatedETA = projet.DateFinPrevue?.ToString("MMM yyyy", LocalizationService.Instance.CurrentCulture) ?? "N/A";
                
                progressData.Add(new ProgressStatusRow
                {
                    Beneficiaire = beneficiaire,
                    Description = projet.Nom,
                    LeadProjet = projet.LeadProjet ?? "N/A",
                    Phase = projet.Phase ?? "N/A",
                    StatutRAGCouleur = ragBrush,
                    KeyHighlights = keyHighlights,
                    InitialETA = initialETA,
                    UpdatedETA = updatedETA,
                    ProgressPourcentage = $"{pourcentage}%"
                });
            }
            
            GridProgressStatus.ItemsSource = progressData;
            GridProgressStatus.AlternationCount = 2;
            
            // Mettre à jour le badge de priorité
            var totalHighPriority = toutesLesTaches.Count(t => projets.Any(p => p.Id == t.ProjetId) && 
                (t.Priorite == Priorite.Haute || t.Priorite == Priorite.Urgent));
            TxtHighPriorityCount.Text = $"High Priority: {totalHighPriority}";
            
            // Mettre à jour le statut global
            bool hasRed = projets.Any(p => p.StatutRAG?.ToLower() == "red");
            bool hasAmber = projets.Any(p => p.StatutRAG?.ToLower() == "amber" || p.StatutRAG?.ToLower() == "orange");
            
            if (hasRed)
            {
                BadgeStatutProjet.Background = new SolidColorBrush(Color.FromRgb(244, 67, 54));
                TxtStatutProjet.Text = "AT RISK";
            }
            else if (hasAmber)
            {
                BadgeStatutProjet.Background = new SolidColorBrush(Color.FromRgb(255, 152, 0));
                TxtStatutProjet.Text = "CAUTION";
            }
            else
            {
                BadgeStatutProjet.Background = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                TxtStatutProjet.Text = "WIP";
            }
        }
        */
        
        private void RemplirProgressStatus(List<Projet> projets, List<BacklogItem> toutesLesTaches)
        {
            if (ContainerProgressStatus == null || TxtHighPriorityCount == null || BadgeStatutProjet == null || TxtStatutProjet == null)
                return;
                
            ContainerProgressStatus.Children.Clear();
            
            // Créer l'en-tête du tableau
            var headerGrid = new Grid
            {
                Background = new SolidColorBrush(Color.FromRgb(46, 125, 50)),
                Height = 40,
                Margin = new Thickness(0, 0, 0, 0)
            };
            
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(180) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            
            var headers = new[] { 
                LocalizationService.Instance.GetString("Reporting_Beneficiary"),
                LocalizationService.Instance.GetString("Reporting_ProjectDescription"),
                LocalizationService.Instance.GetString("Reporting_ProjectLead"),
                LocalizationService.Instance.GetString("Reporting_ProjectPhase"),
                LocalizationService.Instance.GetString("Reporting_RAG"),
                LocalizationService.Instance.GetString("Reporting_KeyHighlights"),
                LocalizationService.Instance.GetString("Reporting_InitialETA"),
                LocalizationService.Instance.GetString("Reporting_UpdatedETA"),
                LocalizationService.Instance.GetString("Reporting_ProgressPercent")
            };
            for (int i = 0; i < headers.Length; i++)
            {
                var headerText = new TextBlock
                {
                    Text = headers[i],
                    Foreground = new SolidColorBrush(Colors.White),
                    FontWeight = FontWeights.Bold,
                    FontSize = 11,
                    Padding = new Thickness(12),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                Grid.SetColumn(headerText, i);
                headerGrid.Children.Add(headerText);
            }
            
            ContainerProgressStatus.Children.Add(headerGrid);
            
            // Créer les lignes pour chaque projet
            int rowIndex = 0;
            foreach (var projet in projets)
            {
                var tachesProjet = toutesLesTaches.Where(t => t.ProjetId == projet.Id).ToList();
                int totalTaches = tachesProjet.Count;
                int tachesCompletes = tachesProjet.Count(t => t.Statut == Statut.Termine || t.EstArchive);
                double pourcentage = totalTaches > 0 ? Math.Round((double)tachesCompletes / totalTaches * 100, 0) : 0;
                
                var rowGrid = new Grid
                {
                    Background = rowIndex % 2 == 0 ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Color.FromRgb(250, 250, 250)),
                    MinHeight = 80,
                    Margin = new Thickness(0, 0, 0, 0)
                };
                
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(180) });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
                
                // Beneficiary
                string beneficiaire = "N/A";
                if (!string.IsNullOrEmpty(projet.Beneficiaires))
                {
                    try
                    {
                        var benefs = System.Text.Json.JsonSerializer.Deserialize<List<string>>(projet.Beneficiaires);
                        if (benefs != null && benefs.Any())
                            beneficiaire = string.Join(", ", benefs);
                    }
                    catch { beneficiaire = projet.Beneficiaires; }
                }
                
                var txtBenef = new TextBlock { Text = beneficiaire, FontWeight = FontWeights.Bold, FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51)), Padding = new Thickness(10), TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
                Grid.SetColumn(txtBenef, 0);
                rowGrid.Children.Add(txtBenef);
                
                var txtDesc = new TextBlock { Text = projet.Nom, FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51)), Padding = new Thickness(10), TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center };
                Grid.SetColumn(txtDesc, 1);
                rowGrid.Children.Add(txtDesc);
                
                var txtLead = new TextBlock { Text = projet.LeadProjet ?? "N/A", FontWeight = FontWeights.SemiBold, FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102)), Padding = new Thickness(10), VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
                Grid.SetColumn(txtLead, 2);
                rowGrid.Children.Add(txtLead);
                
                var txtPhase = new TextBlock { Text = projet.Phase ?? "N/A", FontSize = 10, Foreground = new SolidColorBrush(Color.FromRgb(85, 85, 85)), Padding = new Thickness(10), TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
                Grid.SetColumn(txtPhase, 3);
                rowGrid.Children.Add(txtPhase);
                
                // RAG Status
                Color ragColor = Color.FromRgb(76, 175, 80);
                if (!string.IsNullOrEmpty(projet.StatutRAG))
                {
                    switch (projet.StatutRAG.ToLower())
                    {
                        case "green": ragColor = Color.FromRgb(76, 175, 80); break;
                        case "amber":
                        case "orange": ragColor = Color.FromRgb(255, 152, 0); break;
                        case "red": ragColor = Color.FromRgb(244, 67, 54); break;
                    }
                }
                
                var ragCircle = new Border { Width = 40, Height = 40, CornerRadius = new CornerRadius(20), Background = new SolidColorBrush(ragColor), BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224)), BorderThickness = new Thickness(2), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                Grid.SetColumn(ragCircle, 4);
                rowGrid.Children.Add(ragCircle);
                
                // Key Highlights - Format WPF avec TextBlocks formatés (sans Expected gains)
                var highlightsContainer = new StackPanel { Margin = new Thickness(10, 8, 10, 8), VerticalAlignment = VerticalAlignment.Top };
                
                // Tâches en cours avec descriptions
                var tachesEnCoursList = tachesProjet.Where(t => t.Statut == Statut.EnCours).OrderByDescending(t => t.Priorite).Take(3).ToList();
                foreach (var tache in tachesEnCoursList)
                {
                    var tacheBlock = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 8), FontSize = 10, LineHeight = 14 };
                    
                    var titre = !string.IsNullOrEmpty(tache.Titre) ? tache.Titre : tache.Description;
                    if (titre.Length > 60) titre = titre.Substring(0, 57) + "...";
                    
                    var runTitre = new Run(titre + ": ") { FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51)) };
                    tacheBlock.Inlines.Add(runTitre);
                    
                    if (!string.IsNullOrEmpty(tache.Description) && tache.Description != tache.Titre)
                    {
                        var desc = tache.Description;
                        if (desc.Length > 120) desc = desc.Substring(0, 117) + "...";
                        var runDesc = new Run(desc) { Foreground = new SolidColorBrush(Color.FromRgb(85, 85, 85)) };
                        tacheBlock.Inlines.Add(runDesc);
                    }
                    else
                    {
                        var runDesc = new Run(LocalizationService.Instance.GetString("Reporting_InDevelopment")) { Foreground = new SolidColorBrush(Color.FromRgb(85, 85, 85)) };
                        tacheBlock.Inlines.Add(runDesc);
                    }
                    
                    highlightsContainer.Children.Add(tacheBlock);
                }
                
                // Section "Ongoing" si des tâches en cours
                if (tachesEnCoursList.Any() && highlightsContainer.Children.Count > 0)
                {
                    var ongoingBlock = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 5, 0, 5), FontSize = 10, LineHeight = 14 };
                    var runOngoing = new Run(LocalizationService.Instance.GetString("Reporting_Ongoing") + " ") { FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(0, 145, 90)) };
                    ongoingBlock.Inlines.Add(runOngoing);
                    
                    var ongoingText = tachesEnCoursList.Count == 1 
                        ? LocalizationService.Instance.GetString("Reporting_OngoingDevelopment") 
                        : string.Format(LocalizationService.Instance.GetString("Reporting_ActiveDevelopments"), tachesEnCoursList.Count);
                    var runOngoingDesc = new Run(ongoingText) { Foreground = new SolidColorBrush(Color.FromRgb(85, 85, 85)) };
                    ongoingBlock.Inlines.Add(runOngoingDesc);
                    
                    highlightsContainer.Children.Add(ongoingBlock);
                }
                
                // Next steps: prochaines actions
                if (!string.IsNullOrEmpty(projet.NextActions))
                {
                    var nextStepsContainer = new StackPanel { Margin = new Thickness(0, 8, 0, 0) };
                    
                    var headerBlock = new TextBlock { FontWeight = FontWeights.Bold, FontSize = 10, Margin = new Thickness(0, 0, 0, 3), Foreground = new SolidColorBrush(Color.FromRgb(33, 150, 243)) };
                    headerBlock.Text = LocalizationService.Instance.GetString("Reporting_NextSteps");
                    nextStepsContainer.Children.Add(headerBlock);
                    
                    var actions = projet.NextActions.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Take(3);
                    foreach (var action in actions)
                    {
                        var actionText = action.Trim();
                        if (actionText.Length > 90) actionText = actionText.Substring(0, 87) + "...";
                        
                        var actionBlock = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 2, 0, 2), FontSize = 10, LineHeight = 14, Foreground = new SolidColorBrush(Color.FromRgb(85, 85, 85)) };
                        actionBlock.Text = actionText;
                        nextStepsContainer.Children.Add(actionBlock);
                    }
                    
                    highlightsContainer.Children.Add(nextStepsContainer);
                }
                
                Grid.SetColumn(highlightsContainer, 5);
                rowGrid.Children.Add(highlightsContainer);
                
                var txtInitialETA = new TextBlock { Text = projet.DateDebut?.ToString("MMM yyyy", LocalizationService.Instance.CurrentCulture) ?? "N/A", FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102)), Padding = new Thickness(10), VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
                Grid.SetColumn(txtInitialETA, 6);
                rowGrid.Children.Add(txtInitialETA);
                
                var txtUpdatedETA = new TextBlock { Text = projet.DateFinPrevue?.ToString("MMM yyyy", LocalizationService.Instance.CurrentCulture) ?? "N/A", FontWeight = FontWeights.SemiBold, FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51)), Padding = new Thickness(10), VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
                Grid.SetColumn(txtUpdatedETA, 7);
                rowGrid.Children.Add(txtUpdatedETA);
                
                var txtProgress = new TextBlock { Text = $"{pourcentage}%", FontWeight = FontWeights.Bold, FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50)), Padding = new Thickness(10), VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
                Grid.SetColumn(txtProgress, 8);
                rowGrid.Children.Add(txtProgress);
                
                var bottomBorder = new Border { BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224)), BorderThickness = new Thickness(0, 0, 0, 1), Height = rowGrid.MinHeight };
                Grid.SetColumnSpan(bottomBorder, 9);
                rowGrid.Children.Add(bottomBorder);
                
                ContainerProgressStatus.Children.Add(rowGrid);
                rowIndex++;
            }
            
            // Mettre à jour le badge de priorité
            var totalHighPriority = toutesLesTaches.Count(t => projets.Any(p => p.Id == t.ProjetId) && (t.Priorite == Priorite.Haute || t.Priorite == Priorite.Urgent));
            TxtHighPriorityCount.Text = $"High Priority: {totalHighPriority}";
            
            // Mettre à jour le statut global
            bool hasRed = projets.Any(p => p.StatutRAG?.ToLower() == "red");
            bool hasAmber = projets.Any(p => p.StatutRAG?.ToLower() == "amber" || p.StatutRAG?.ToLower() == "orange");
            
            if (hasRed) { BadgeStatutProjet.Background = new SolidColorBrush(Color.FromRgb(244, 67, 54)); TxtStatutProjet.Text = LocalizationService.Instance.GetString("Reporting_AtRiskStatus"); }
            else if (hasAmber) { BadgeStatutProjet.Background = new SolidColorBrush(Color.FromRgb(255, 152, 0)); TxtStatutProjet.Text = LocalizationService.Instance.GetString("Reporting_CautionStatus"); }
            else { BadgeStatutProjet.Background = new SolidColorBrush(Color.FromRgb(76, 175, 80)); TxtStatutProjet.Text = LocalizationService.Instance.GetString("Reporting_WIPStatus"); }
        }
        
        // Classe pour le binding du DataGrid
        public class ProgressStatusRow
        {
            public string Beneficiaire { get; set; }
            public string Description { get; set; }
            public string LeadProjet { get; set; }
            public string Phase { get; set; }
            public Brush StatutRAGCouleur { get; set; }
            public string KeyHighlights { get; set; }
            public string InitialETA { get; set; }
            public string UpdatedETA { get; set; }
            public string ProgressPourcentage { get; set; }
        }
        
        private void AfficherPhasesDynamiques(List<Projet> projets, List<BacklogItem> toutesLesTaches)
        {
            ContainerPhases.Children.Clear();
            
            if (!projets.Any())
            {
                var emptyBlock = new TextBlock
                {
                    Text = "Aucun projet dans ce programme",
                    FontSize = 13,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#999")),
                    FontStyle = FontStyles.Italic,
                    Margin = new Thickness(0, 10, 0, 10),
                    TextAlignment = TextAlignment.Center
                };
                ContainerPhases.Children.Add(emptyBlock);
                return;
            }
            
            foreach (var projet in projets)
            {
                var tachesProjet = toutesLesTaches.Where(t => t.ProjetId == projet.Id).ToList();
                int totalTaches = tachesProjet.Count;
                int tachesCompletes = tachesProjet.Count(t => t.Statut == Statut.Termine || t.EstArchive);
                double avancement = totalTaches > 0 ? Math.Round((double)tachesCompletes / totalTaches * 100, 0) : 0;
                
                var phaseBorder = new Border
                {
                    Background = new SolidColorBrush(Colors.White),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(20, 15, 20, 15),
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00915A")),
                    BorderThickness = new Thickness(2),
                    Margin = new Thickness(0, 0, 0, 15)
                };
                
                var phaseContent = new StackPanel();
                
                // Titre du projet
                var titreBlock = new TextBlock
                {
                    Text = projet.Nom,
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333")),
                    Margin = new Thickness(0, 0, 0, 8),
                    TextWrapping = TextWrapping.Wrap
                };
                phaseContent.Children.Add(titreBlock);
                
                // Dates
                if (projet.DateDebut.HasValue && projet.DateFinPrevue.HasValue)
                {
                    var dateBlock = new TextBlock
                    {
                        FontSize = 12,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666")),
                        Margin = new Thickness(0, 0, 0, 5)
                    };
                    dateBlock.Inlines.Add(new Run("📅 "));
                    dateBlock.Inlines.Add(new Run($"{projet.DateDebut.Value:dd/MM/yyyy} → {projet.DateFinPrevue.Value:dd/MM/yyyy}"));
                    phaseContent.Children.Add(dateBlock);
                }
                else if (projet.DateFinPrevue.HasValue)
                {
                    var dateBlock = new TextBlock
                    {
                        Text = $"📅 {LocalizationService.Instance.GetString("Reporting_Delivery2")} {projet.DateFinPrevue.Value:MMM yyyy}".ToUpper(),
                        FontSize = 12,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666")),
                        Margin = new Thickness(0, 0, 0, 5)
                    };
                    phaseContent.Children.Add(dateBlock);
                }
                
                // Avancement
                var avancementBlock = new TextBlock
                {
                    FontSize = 12,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00915A")),
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0, 0, 0, 5)
                };
                avancementBlock.Inlines.Add(new Run($"📊 {LocalizationService.Instance.GetString("Reporting_ProgressLabel")} "));
                avancementBlock.Inlines.Add(new Run($"{avancement}%") { FontWeight = FontWeights.Bold });
                avancementBlock.Inlines.Add(new Run($" ({tachesCompletes}/{totalTaches} {LocalizationService.Instance.GetString("Reporting_TasksUnit")})") { FontWeight = FontWeights.Normal });
                phaseContent.Children.Add(avancementBlock);
                
                // Statut RAG si en retard
                if (projet.DateFinPrevue.HasValue && projet.DateFinPrevue.Value < DateTime.Now && avancement < 100)
                {
                    var alerteBlock = new TextBlock
                    {
                        Text = LocalizationService.Instance.GetString("Reporting_LateStatus"),
                        FontSize = 11,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E53935")),
                        Margin = new Thickness(0, 5, 0, 0)
                    };
                    phaseContent.Children.Add(alerteBlock);
                }
                
                phaseBorder.Child = phaseContent;
                ContainerPhases.Children.Add(phaseBorder);
            }
        }
        
        private void BtnGenererPowerPoint_Click(object sender, RoutedEventArgs e)
        {
            if (_currentProgramme == null || _currentProjets == null || _currentTaches == null)
            {
                MessageBox.Show(LocalizationService.Instance.GetString("Reporting_SelectProgramFirst"), 
                    LocalizationService.Instance.GetString("Reporting_Information"), 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            try
            {
                // Ouvrir un dialogue pour choisir l'emplacement de sauvegarde
                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "PowerPoint (*.pptx)|*.pptx",
                    FileName = $"Reporting_{_currentProgramme.Nom}_{DateTime.Now:yyyyMMdd}.pptx",
                    DefaultExt = ".pptx"
                };
                
                if (saveDialog.ShowDialog() == true)
                {
                    BtnGenererPowerPoint.IsEnabled = false;
                    var loadingStack = new StackPanel { Orientation = Orientation.Horizontal };
                    loadingStack.Children.Add(new TextBlock { Text = "⏳", FontSize = 16, Margin = new Thickness(0, 0, 8, 0) });
                    loadingStack.Children.Add(new TextBlock { Text = LocalizationService.Instance.GetString("Reporting_GeneratingInProgress"), FontSize = 13 });
                    BtnGenererPowerPoint.Content = loadingStack;
                    
                    // Préparer les données du dashboard - FILTRER comme dans le reporting
                    var tachesFiltered = _currentTaches
                        .Where(t => t.TypeDemande != TypeDemande.Conges && 
                                   t.TypeDemande != TypeDemande.NonTravaille && 
                                   t.TypeDemande != TypeDemande.Support)
                        .ToList();
                    
                    // Récupérer les équipes assignées aux projets du programme
                    var equipesIds = _currentProjets
                        .SelectMany(p => p.EquipesAssigneesIds ?? new List<int>())
                        .Distinct()
                        .ToList();
                    
                    var equipes = _database.GetAllEquipes()
                        .Where(e => equipesIds.Contains(e.Id))
                        .Select(e => e.Nom)
                        .ToList();
                    
                    // Calculer les valeurs du dashboard - exclure les tâches archivées des statuts
                    int ongoingCount = tachesFiltered.Count(t => t.Statut == Statut.EnCours && !t.EstArchive);
                    int doneCount = tachesFiltered.Count(t => t.Statut == Statut.Termine && !t.EstArchive);
                    int toStartCount = tachesFiltered.Count(t => (t.Statut == Statut.Afaire || t.Statut == Statut.APrioriser) && !t.EstArchive);
                    int cancelledCount = tachesFiltered.Count(t => t.EstArchive);
                    
                    var donneesDashboard = new Dictionary<string, object>
                    {
                        { "StatusOverview", new { 
                            Ongoing = ongoingCount, 
                            Done = doneCount, 
                            ToStart = toStartCount,
                            Cancelled = cancelledCount
                        } },
                        { "Teams", equipes }
                    };
                    
                    // IMPORTANT: Passer les tâches filtrées au lieu de _currentTaches
                    PowerPointGenerator.GenererPowerPointProgramme(
                        _currentProgramme,
                        _currentProjets,
                        tachesFiltered,  // Utiliser tachesFiltered au lieu de _currentTaches
                        donneesDashboard,
                        saveDialog.FileName
                    );
                    
                    MessageBox.Show($"{LocalizationService.Instance.GetString("Reporting_GenerateSuccess")}\n\n{saveDialog.FileName}", 
                        LocalizationService.Instance.GetString("Reporting_Success"), MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    // Ouvrir le fichier
                    System.Diagnostics.Process.Start(saveDialog.FileName);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(LocalizationService.Instance.GetString("Reporting_PowerPointGenerationError"), ex.Message, ex.StackTrace), 
                    LocalizationService.Instance.GetString("Reporting_Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                BtnGenererPowerPoint.IsEnabled = true;
                var stackPanel = new StackPanel { Orientation = Orientation.Horizontal };
                stackPanel.Children.Add(new TextBlock { Text = "📊", FontSize = 16, Margin = new Thickness(0, 0, 8, 0) });
                stackPanel.Children.Add(new TextBlock { Text = LocalizationService.Instance.GetString("Reporting_GeneratePowerPoint"), FontSize = 13, FontWeight = FontWeights.SemiBold });
                BtnGenererPowerPoint.Content = stackPanel;
            }
        }
        
        private async void BtnGenererAvecIA_Click(object sender, RoutedEventArgs e)
        {
            if (_currentProgramme == null || _currentProjets == null || _currentTaches == null)
            {
                MessageBox.Show(LocalizationService.Instance.GetString("Reporting_SelectProgramFirst"), 
                    LocalizationService.Instance.GetString("Reporting_Information"), 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            var apiKey = BacklogManager.Properties.Settings.Default["AgentChatToken"]?.ToString()?.Trim();
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                MessageBox.Show("La clé API OpenAI n'est pas configurée.\nConfigurez-la dans la section Chat.", 
                    "Configuration requise", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            BtnGenererAvecIA.IsEnabled = false;
            var loadingStack = new StackPanel { Orientation = Orientation.Horizontal };
            loadingStack.Children.Add(new TextBlock { Text = "⏳", FontSize = 16, Margin = new Thickness(0, 0, 8, 0) });
            loadingStack.Children.Add(new TextBlock { Text = LocalizationService.Instance.GetString("Reporting_GeneratingInProgress"), FontSize = 13 });
            BtnGenererAvecIA.Content = loadingStack;
            
            try
            {
                await GenererContenuAvecIA();
                MessageBox.Show(LocalizationService.Instance.GetString("Reporting_ContentGeneratedSuccess"), 
                    LocalizationService.Instance.GetString("Reporting_Success"), 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(LocalizationService.Instance.GetString("Reporting_GenerationError"), ex.Message), 
                    LocalizationService.Instance.GetString("Reporting_Error"), 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                BtnGenererAvecIA.IsEnabled = true;
                var stackPanel = new StackPanel { Orientation = Orientation.Horizontal };
                stackPanel.Children.Add(new TextBlock { Text = "✨", FontSize = 16, Margin = new Thickness(0, 0, 8, 0) });
                stackPanel.Children.Add(new TextBlock { Text = LocalizationService.Instance.GetString("Reporting_GenerateWithAI"), FontSize = 13, FontWeight = FontWeights.SemiBold });
                BtnGenererAvecIA.Content = stackPanel;
            }
        }
        
        private async System.Threading.Tasks.Task GenererContenuAvecIA()
        {
            var dateAnalyse = _dateDebutFiltre ?? DateTime.Now.AddMonths(-3);
            var tachesPeriode = _currentTaches
                .Where(t => _currentProjets.Any(p => p.Id == t.ProjetId) && t.DateCreation >= dateAnalyse)
                .ToList();
            
            var periodeDebut = _dateDebutFiltre?.ToString("dd/MM/yyyy") ?? LocalizationService.Instance.GetString("Reporting_Start");
            var periodeFin = _dateFinFiltre?.ToString("dd/MM/yyyy") ?? LocalizationService.Instance.GetString("Reporting_TodayDate");
            
            // Adapter le prompt selon la langue actuelle
            var currentLanguage = LocalizationService.Instance.CurrentLanguageCode;
            string expertDesc, programContext, projectsHeader, activityPeriod, newRequests, inProgress, completed, highPriority;
            string changeManagementInstructions, evolvingScopeInstructions, beConsise;
            
            if (currentLanguage == "en")
            {
                expertDesc = "You are an expert in project management and change management.";
                programContext = "PROGRAM CONTEXT";
                projectsHeader = "PROJECTS";
                activityPeriod = "PERIOD ACTIVITY";
                newRequests = "new request(s)";
                inProgress = "in progress";
                completed = "completed";
                highPriority = "high priority";
                changeManagementInstructions = @"3-4 lines maximum describing:
- Concrete change management actions based on activity
- Focus on documentation, training, support according to created requests
- Mention of active projects and teams involved";
                evolvingScopeInstructions = @"List of main requests added in the period, formatted as follows:
- Date + brief title of each request (max 5-6 most significant requests)
- Grouped by project if relevant
- Priority statistics";
                beConsise = "Be concise, precise, and base yourself ONLY on the data provided.";
            }
            else if (currentLanguage == "es")
            {
                expertDesc = "Eres un experto en gestión de proyectos y gestión del cambio.";
                programContext = "CONTEXTO DEL PROGRAMA";
                projectsHeader = "PROYECTOS";
                activityPeriod = "ACTIVIDAD DEL PERÍODO";
                newRequests = "nueva(s) solicitud(es)";
                inProgress = "en curso";
                completed = "completada(s)";
                highPriority = "alta prioridad";
                changeManagementInstructions = @"Máximo 3-4 líneas describiendo:
- Acciones concretas de gestión del cambio basadas en la actividad
- Enfoque en documentación, formación, soporte según las solicitudes creadas
- Mención de proyectos activos y equipos involucrados";
                evolvingScopeInstructions = @"Lista de las principales solicitudes añadidas en el período, formateada así:
- Fecha + título breve de cada solicitud (máximo 5-6 solicitudes más significativas)
- Agrupadas por proyecto si es relevante
- Estadísticas de prioridades";
                beConsise = "Sé conciso, preciso y basa tus respuestas ÚNICAMENTE en los datos proporcionados.";
            }
            else // français par défaut
            {
                expertDesc = "Tu es un expert en gestion de projet et change management.";
                programContext = "CONTEXTE DU PROGRAMME";
                projectsHeader = "PROJETS";
                activityPeriod = "ACTIVITÉ PÉRIODE";
                newRequests = "nouvelle(s) demande(s)";
                inProgress = "en cours";
                completed = "terminée(s)";
                highPriority = "haute priorité";
                changeManagementInstructions = @"3-4 lignes maximum décrivant:
- Actions concrètes de change management basées sur l'activité
- Focus sur documentation, training, support selon les demandes créées
- Mention des projets actifs et équipes concernées";
                evolvingScopeInstructions = @"Liste des demandes principales ajoutées dans la période, formatée ainsi:
- Date + titre bref de chaque demande (max 5-6 demandes les plus significatives)
- Regroupées par projet si pertinent
- Statistiques de priorités";
                beConsise = "Sois concis, précis, et base-toi UNIQUEMENT sur les données fournies.";
            }
            
            var prompt = $@"{expertDesc}

**{programContext}: {_currentProgramme.Nom}**

**{projectsHeader} ({_currentProjets.Count}):**
{string.Join("\n", _currentProjets.Select(p => $"- {p.Nom}: {(p.DateDebut.HasValue ? p.DateDebut.Value.ToString("MM/yyyy") : "?")} → {(p.DateFinPrevue.HasValue ? p.DateFinPrevue.Value.ToString("MM/yyyy") : "?")}"))}

**{activityPeriod} ({periodeDebut} → {periodeFin}):**
- {tachesPeriode.Count()} {newRequests}
- {tachesPeriode.Count(t => t.Statut == Statut.EnCours)} {inProgress}
- {tachesPeriode.Count(t => t.Statut == Statut.Termine)} {completed}
- {tachesPeriode.Count(t => t.Priorite == Priorite.Haute)} {highPriority}

Generate structured content for the program reporting with these sections (use EXACTLY these markers):

[CHANGE_MANAGEMENT]
{changeManagementInstructions}

[EVOLVING_SCOPE]
{evolvingScopeInstructions}

{beConsise}";

            var response = await AppelerIA(prompt);
            InterpreterEtAfficherResultat(response, tachesPeriode);
        }
        
        private async System.Threading.Tasks.Task<string> AppelerIA(string prompt)
        {
            using (var httpClient = new System.Net.Http.HttpClient())
            {
                var apiKey = BacklogManager.Properties.Settings.Default["AgentChatToken"]?.ToString()?.Trim();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                httpClient.Timeout = TimeSpan.FromMinutes(2);
                
                // Adapter le message système selon la langue
                var currentLanguage = LocalizationService.Instance.CurrentLanguageCode;
                string systemMessage = currentLanguage == "en" 
                    ? "You are a project management expert. Respond in a concise and structured manner."
                    : currentLanguage == "es"
                    ? "Eres un experto en gestión de proyectos. Responde de manera concisa y estructurada."
                    : "Tu es un expert en gestion de projet. Réponds de manière concise et structurée.";
                
                var requestBody = new
                {
                    model = "gpt-oss-120b",
                    messages = new[]
                    {
                        new { role = "system", content = systemMessage },
                        new { role = "user", content = prompt }
                    },
                    temperature = 0.7,
                    max_tokens = 1500
                };
                
                var json = System.Text.Json.JsonSerializer.Serialize(requestBody);
                var content = new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json");
                
                var response = await httpClient.PostAsync("https://genfactory-ai.analytics.cib.echonet/genai/api/v2/chat/completions", content);
                
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Erreur API (Code {(int)response.StatusCode})");
                }
                
                var responseBody = await response.Content.ReadAsStringAsync();
                using (var document = System.Text.Json.JsonDocument.Parse(responseBody))
                {
                    return document.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
                }
            }
        }
        
        private void InterpreterEtAfficherResultat(string response, List<BacklogItem> tachesPeriode)
        {
            // Extraire Change Management
            var changeMatch = System.Text.RegularExpressions.Regex.Match(response, @"\[CHANGE_MANAGEMENT\](.*?)(?=\[|$)", System.Text.RegularExpressions.RegexOptions.Singleline);
            if (changeMatch.Success)
            {
                var changeText = changeMatch.Groups[1].Value.Trim();
                TxtChangeManagement.Children.Clear();
                
                var lines = changeText.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    var cleanLine = line.Trim().TrimStart('-', '•', '*').Trim();
                    if (!string.IsNullOrWhiteSpace(cleanLine))
                    {
                        var block = new TextBlock
                        {
                            Text = $"• {cleanLine}",
                            FontSize = 11,
                            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#555")),
                            Margin = new Thickness(5, 3, 5, 3),
                            TextWrapping = TextWrapping.Wrap
                        };
                        TxtChangeManagement.Children.Add(block);
                    }
                }
            }
            
            // Extraire Evolving Scope
            var evolvingMatch = System.Text.RegularExpressions.Regex.Match(response, @"\[EVOLVING_SCOPE\](.*?)(?=\[|$)", System.Text.RegularExpressions.RegexOptions.Singleline);
            if (evolvingMatch.Success)
            {
                var evolvingText = evolvingMatch.Groups[1].Value.Trim();
                TxtEvolvingScope.Children.Clear();
                
                var lines = evolvingText.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    var cleanLine = line.Trim();
                    if (!string.IsNullOrWhiteSpace(cleanLine))
                    {
                        var block = new TextBlock
                        {
                            Text = cleanLine.StartsWith("-") || cleanLine.StartsWith("•") ? cleanLine : $"• {cleanLine}",
                            FontSize = 11,
                            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#555")),
                            Margin = new Thickness(5, 2, 5, 2),
                            TextWrapping = TextWrapping.Wrap
                        };
                        TxtEvolvingScope.Children.Add(block);
                    }
                }
            }
        }
        
        private void RemplirDashboardKPIs(List<Projet> projets, List<BacklogItem> toutesLesTaches)
        {
            if (ContainerActionsInitiatives == null || ContainerTeamsFilter == null)
                return;
                
            ContainerActionsInitiatives.Children.Clear();
            ContainerTeamsFilter.Children.Clear();
            
            // Filtrer les tâches (exclure Congé, Non travaillé, Support)
            var tachesFiltered = toutesLesTaches
                .Where(t => t.TypeDemande != TypeDemande.Conges && 
                           t.TypeDemande != TypeDemande.NonTravaille && 
                           t.TypeDemande != TypeDemande.Support)
                .ToList();
            
            // Remplir le filtre TEAM(S) - Récupérer TOUTES les équipes de la DB
            var toutesEquipes = _database.GetAllEquipes().Where(e => e.Actif).OrderBy(e => e.Nom).ToList();
            
            // Identifier les équipes impliquées dans le programme
            var equipesImpliquees = new HashSet<string>();
            foreach (var projet in projets)
            {
                if (projet.EquipesAssigneesIds != null && projet.EquipesAssigneesIds.Any())
                {
                    foreach (var equipeId in projet.EquipesAssigneesIds)
                    {
                        var equipe = toutesEquipes.FirstOrDefault(e => e.Id == equipeId);
                        if (equipe != null)
                            equipesImpliquees.Add(equipe.Nom);
                    }
                }
            }
            
            // Afficher toutes les équipes, cocher celles impliquées
            foreach (var equipe in toutesEquipes)
            {
                var chk = new CheckBox
                {
                    Content = equipe.Nom,
                    IsChecked = equipesImpliquees.Contains(equipe.Nom),
                    Margin = new Thickness(0, 0, 0, 5),
                    FontSize = 10
                };
                ContainerTeamsFilter.Children.Add(chk);
            }
            
            // Remplir les graphiques circulaires
            RemplirStatusOverview(tachesFiltered);
            RemplirWPPerMonth(tachesFiltered);
            
            // Remplir le tableau ACTIONS / INITIATIVES depuis les tâches
            var tachesActives = tachesFiltered
                .Where(t => t.Statut != Statut.Termine && !t.EstArchive)
                .OrderByDescending(t => t.Priorite)
                .Take(10)
                .ToList();
                
            int rowIndex = 0;
            foreach (var tache in tachesActives)
            {
                var rowGrid = new Grid
                {
                    Background = rowIndex % 2 == 0 ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Color.FromRgb(250, 250, 250)),
                    MinHeight = 60,
                    Margin = new Thickness(0, 0, 0, 0)
                };
                
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(250) });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
                
                // Action / Initiative
                var titre = !string.IsNullOrEmpty(tache.Titre) ? tache.Titre : tache.Description;
                var txtAction = new TextBlock
                {
                    Text = titre,
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51)),
                    Padding = new Thickness(15, 10, 10, 10),
                    TextWrapping = TextWrapping.Wrap,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(txtAction, 0);
                rowGrid.Children.Add(txtAction);
                
                // Priority
                Color priorityColor = Color.FromRgb(76, 175, 80); // Vert par défaut
                string priorityText = "Low";
                
                switch (tache.Priorite)
                {
                    case Priorite.Urgent:
                    case Priorite.Haute:
                        priorityColor = Color.FromRgb(244, 67, 54); // Rouge
                        priorityText = LocalizationService.Instance.GetString("Reporting_PriorityHigh");
                        break;
                    case Priorite.Moyenne:
                        priorityColor = Color.FromRgb(255, 193, 7); // Jaune
                        priorityText = LocalizationService.Instance.GetString("Reporting_PriorityMedium");
                        break;
                    case Priorite.Basse:
                        priorityColor = Color.FromRgb(76, 175, 80); // Vert
                        priorityText = LocalizationService.Instance.GetString("Reporting_PriorityLow");
                        break;
                }
                
                var priorityBorder = new Border
                {
                    Background = new SolidColorBrush(priorityColor),
                    Width = 70,
                    Height = 30,
                    CornerRadius = new CornerRadius(4),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                
                var txtPriority = new TextBlock
                {
                    Text = priorityText,
                    FontSize = 11,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Colors.White),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                
                priorityBorder.Child = txtPriority;
                Grid.SetColumn(priorityBorder, 1);
                rowGrid.Children.Add(priorityBorder);
                
                // Next Steps - Description détaillée avec contexte
                var nextSteps = "";
                var projet = projets.FirstOrDefault(p => p.Id == tache.ProjetId);
                
                if (!string.IsNullOrEmpty(tache.Description))
                {
                    nextSteps = tache.Description;
                }
                else if (!string.IsNullOrEmpty(tache.Titre))
                {
                    // Si pas de description mais un titre, utiliser le titre avec contexte
                    nextSteps = $"{tache.Titre} - ";
                    switch (tache.Statut)
                    {
                        case Statut.Afaire:
                            nextSteps += LocalizationService.Instance.GetString("Reporting_ToPlanAndStart");
                            break;
                        case Statut.EnCours:
                            nextSteps += LocalizationService.Instance.GetString("Reporting_DevelopmentInProgress");
                            if (tache.ChiffrageJours.HasValue)
                                nextSteps += " " + string.Format(LocalizationService.Instance.GetString("Reporting_EstimatedDays"), Math.Round(tache.ChiffrageJours.Value, 1));
                            break;
                        case Statut.Test:
                            nextSteps += LocalizationService.Instance.GetString("Reporting_InTestPhase");
                            break;
                        case Statut.EnAttente:
                            nextSteps += LocalizationService.Instance.GetString("Reporting_OnHold");
                            break;
                        default:
                            nextSteps += LocalizationService.Instance.GetString("Reporting_PlanningInProgress");
                            break;
                    }
                }
                else
                {
                    nextSteps = string.Format(LocalizationService.Instance.GetString("Reporting_TaskDefinitionInProgress"), tache.Id);
                }
                
                if (nextSteps.Length > 200)
                    nextSteps = nextSteps.Substring(0, 197) + "...";
                
                // Conteneur pour Next Steps + bouton edit
                var nextStepsContainer = new Grid();
                nextStepsContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                nextStepsContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) });
                
                var txtNextSteps = new TextBlock
                {
                    Text = nextSteps,
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Color.FromRgb(85, 85, 85)),
                    Padding = new Thickness(10),
                    TextWrapping = TextWrapping.Wrap,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(txtNextSteps, 0);
                nextStepsContainer.Children.Add(txtNextSteps);
                
                // Bouton d'édition discret
                var btnEdit = new Button
                {
                    Content = "✏️",
                    FontSize = 12,
                    Width = 24,
                    Height = 24,
                    Background = new SolidColorBrush(Color.FromRgb(240, 240, 240)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                    BorderThickness = new Thickness(1),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    ToolTip = "Éditer les détails du next step",
                    Tag = tache // Stocker la tâche pour pouvoir la récupérer
                };
                
                btnEdit.Click += BtnEditNextStep_Click;
                
                Grid.SetColumn(btnEdit, 1);
                nextStepsContainer.Children.Add(btnEdit);
                
                Grid.SetColumn(nextStepsContainer, 2);
                rowGrid.Children.Add(nextStepsContainer);
                
                // ETA
                var eta = tache.DateFinAttendue?.ToString("dd/MM/yyyy") ?? "N/A";
                var txtETA = new TextBlock
                {
                    Text = eta,
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102)),
                    Padding = new Thickness(10),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                Grid.SetColumn(txtETA, 3);
                rowGrid.Children.Add(txtETA);
                
                // Bordure inférieure
                var bottomBorder = new Border
                {
                    BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                    BorderThickness = new Thickness(0, 0, 0, 1),
                    Height = rowGrid.MinHeight
                };
                Grid.SetColumnSpan(bottomBorder, 4);
                rowGrid.Children.Add(bottomBorder);
                
                ContainerActionsInitiatives.Children.Add(rowGrid);
                rowIndex++;
            }
            
            // Mettre à jour le compteur total d'initiatives
            if (TxtTotalInitiatives != null)
            {
                TxtTotalInitiatives.Text = tachesActives.Count.ToString();
            }
        }
        
        private void BtnEditNextStep_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is BacklogItem tache)
            {
                // Créer une fenêtre modale simple
                var dialog = new Window
                {
                    Title = string.Format(LocalizationService.Instance.GetString("Reporting_EditNextStepTitle"), tache.Titre ?? $"Tâche #{tache.Id}"),
                    Width = 600,
                    Height = 400,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = Window.GetWindow(this),
                    ResizeMode = ResizeMode.NoResize,
                    Background = new SolidColorBrush(Colors.White)
                };
                
                var mainGrid = new Grid { Margin = new Thickness(20) };
                mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(40) });
                mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(50) });
                
                // Label
                var label = new TextBlock
                {
                    Text = LocalizationService.Instance.GetString("Reporting_NextStepDetails"),
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51)),
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetRow(label, 0);
                mainGrid.Children.Add(label);
                
                // TextBox pour éditer la description
                var txtDescription = new TextBox
                {
                    Text = tache.Description ?? "",
                    TextWrapping = TextWrapping.Wrap,
                    AcceptsReturn = true,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    Padding = new Thickness(10),
                    FontSize = 12,
                    BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                    BorderThickness = new Thickness(1)
                };
                Grid.SetRow(txtDescription, 1);
                mainGrid.Children.Add(txtDescription);
                
                // Boutons
                var buttonsPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Center
                };
                
                var btnCancel = new Button
                {
                    Content = LocalizationService.Instance.GetString("Reporting_Cancel"),
                    Width = 100,
                    Height = 32,
                    Margin = new Thickness(0, 0, 10, 0),
                    Background = new SolidColorBrush(Color.FromRgb(240, 240, 240)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                    Cursor = System.Windows.Input.Cursors.Hand
                };
                btnCancel.Click += (s, ev) => dialog.Close();
                
                var btnSave = new Button
                {
                    Content = LocalizationService.Instance.GetString("Reporting_Save"),
                    Width = 120,
                    Height = 32,
                    Background = new SolidColorBrush(Color.FromRgb(0, 145, 90)),
                    Foreground = new SolidColorBrush(Colors.White),
                    BorderThickness = new Thickness(0),
                    FontWeight = FontWeights.Bold,
                    Cursor = System.Windows.Input.Cursors.Hand
                };
                
                btnSave.Click += (s, ev) =>
                {
                    try
                    {
                        tache.Description = txtDescription.Text;
                        tache.DateDerniereMaj = DateTime.Now;
                        _database.AddOrUpdateBacklogItem(tache);
                        
                        MessageBox.Show(LocalizationService.Instance.GetString("Reporting_NextStepUpdated"), 
                            LocalizationService.Instance.GetString("Reporting_Success"), 
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        dialog.Close();
                        
                        // Rafraîchir l'affichage
                        if (_currentProgramme != null)
                        {
                            var projets = _database.GetProjetsByProgramme(_currentProgramme.Id);
                            var taches = _database.GetBacklogItems();
                            RemplirDashboardKPIs(projets, taches);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(string.Format(LocalizationService.Instance.GetString("Reporting_UpdateError"), ex.Message), 
                            LocalizationService.Instance.GetString("Reporting_Error"), 
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                };
                
                buttonsPanel.Children.Add(btnCancel);
                buttonsPanel.Children.Add(btnSave);
                
                Grid.SetRow(buttonsPanel, 2);
                mainGrid.Children.Add(buttonsPanel);
                
                dialog.Content = mainGrid;
                dialog.ShowDialog();
            }
        }
        
        private void RemplirStatusOverview(List<BacklogItem> taches)
        {
            if (CanvasStatusOverview == null)
                return;
                
            CanvasStatusOverview.Children.Clear();
            
            // Calculer les statistiques - le total inclut toutes les tâches filtrées (y compris archivées)
            int total = taches.Count;  // Toutes les tâches filtrées
            int ongoing = taches.Count(t => t.Statut == Statut.EnCours && !t.EstArchive);
            int done = taches.Count(t => t.Statut == Statut.Termine && !t.EstArchive);
            int toStart = taches.Count(t => (t.Statut == Statut.Afaire || t.Statut == Statut.APrioriser) && !t.EstArchive);
            int cancelled = taches.Count(t => t.EstArchive);
            
            if (total == 0)
                return;
            
            // Créer le graphique en donut
            double centerX = 75;
            double centerY = 75;
            double radius = 60;
            double innerRadius = 40;
            
            double currentAngle = -90; // Commencer en haut
            
            // Couleurs
            var colors = new[]
            {
                (ongoing, Color.FromRgb(76, 175, 80), "Ongoing"),      // Vert
                (done, Color.FromRgb(0, 145, 90), "Done"),             // Vert foncé
                (toStart, Color.FromRgb(158, 158, 158), "To be Start"), // Gris
                (cancelled, Color.FromRgb(189, 189, 189), "Cancelled")  // Gris clair
            };
            
            foreach (var (count, color, label) in colors)
            {
                if (count == 0) continue;
                
                double percentage = (double)count / total;
                double angleSize = percentage * 360;
                
                // Créer l'arc
                var path = new System.Windows.Shapes.Path
                {
                    Fill = new SolidColorBrush(color),
                    Data = CreateDonutSegment(centerX, centerY, radius, innerRadius, currentAngle, angleSize)
                };
                
                CanvasStatusOverview.Children.Add(path);
                currentAngle += angleSize;
            }
            
            // Mettre à jour le compteur total
            if (TxtTotalInitiatives != null)
            {
                TxtTotalInitiatives.Text = total.ToString();
            }
            
            // Ajouter la légende en dessous du graphique
            double legendY = 160;
            double legendX = 10;
            
            var legendItems = new[]
            {
                (Color.FromRgb(76, 175, 80), $"Ongoing ({ongoing})", ongoing),
                (Color.FromRgb(0, 145, 90), $"Done ({done})", done),
                (Color.FromRgb(158, 158, 158), $"To be Start ({toStart})", toStart),
                (Color.FromRgb(189, 189, 189), $"Cancelled ({cancelled})", cancelled)
            };
            
            int index = 0;
            foreach (var (color, label, count) in legendItems)
            {
                if (count == 0) continue;
                
                // Carré de couleur
                var rect = new System.Windows.Shapes.Rectangle
                {
                    Width = 12,
                    Height = 12,
                    Fill = new SolidColorBrush(color)
                };
                Canvas.SetLeft(rect, legendX);
                Canvas.SetTop(rect, legendY + (index * 18));
                CanvasStatusOverview.Children.Add(rect);
                
                // Label
                var labelText = new TextBlock
                {
                    Text = label,
                    FontSize = 9,
                    Foreground = new SolidColorBrush(Color.FromRgb(85, 85, 85))
                };
                Canvas.SetLeft(labelText, legendX + 18);
                Canvas.SetTop(labelText, legendY + (index * 18) - 1);
                CanvasStatusOverview.Children.Add(labelText);
                
                index++;
            }
        }
        
        private void RemplirWPPerMonth(List<BacklogItem> taches)
        {
            if (CanvasWPMonth == null)
                return;
                
            CanvasWPMonth.Children.Clear();
            
            if (!taches.Any())
                return;
            
            // Grouper les tâches par mois de création
            var tachesParMois = taches
                .Where(t => t.DateCreation >= DateTime.Now.AddMonths(-12))
                .GroupBy(t => new { t.DateCreation.Year, t.DateCreation.Month })
                .Select(g => new
                {
                    Mois = new DateTime(g.Key.Year, g.Key.Month, 1),
                    Count = g.Count()
                })
                .OrderBy(x => x.Mois)
                .ToList();
            
            if (!tachesParMois.Any())
                return;
            
            // Créer le graphique circulaire
            double centerX = 75;
            double centerY = 75;
            double radius = 60;
            double innerRadius = 40;
            
            double currentAngle = -90;
            int totalTaches = tachesParMois.Sum(x => x.Count);
            
            // Couleurs alternées
            var colors = new[]
            {
                Color.FromRgb(0, 145, 90),   // Vert foncé
                Color.FromRgb(76, 175, 80),  // Vert
                Color.FromRgb(129, 199, 132) // Vert clair
            };
            
            int colorIndex = 0;
            foreach (var mois in tachesParMois)
            {
                double percentage = (double)mois.Count / totalTaches;
                double angleSize = percentage * 360;
                
                if (angleSize < 1) continue; // Skip très petits segments
                
                var path = new System.Windows.Shapes.Path
                {
                    Fill = new SolidColorBrush(colors[colorIndex % colors.Length]),
                    Data = CreateDonutSegment(centerX, centerY, radius, innerRadius, currentAngle, angleSize)
                };
                
                CanvasWPMonth.Children.Add(path);
                currentAngle += angleSize;
                colorIndex++;
            }
            
            // Texte central avec pourcentage moyen
            var avgPerMonth = tachesParMois.Any() ? (int)tachesParMois.Average(x => x.Count) : 0;
            var centerText = new TextBlock
            {
                Text = $"{avgPerMonth}\n/mois",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0, 145, 90)),
                TextAlignment = TextAlignment.Center
            };
            Canvas.SetLeft(centerText, centerX - 25);
            Canvas.SetTop(centerText, centerY - 18);
            CanvasWPMonth.Children.Add(centerText);
        }
        
        private System.Windows.Media.Geometry CreateDonutSegment(double centerX, double centerY, double radius, double innerRadius, double startAngle, double angleSize)
        {
            double startAngleRad = startAngle * Math.PI / 180;
            double endAngleRad = (startAngle + angleSize) * Math.PI / 180;
            
            Point outerStart = new Point(
                centerX + radius * Math.Cos(startAngleRad),
                centerY + radius * Math.Sin(startAngleRad)
            );
            
            Point outerEnd = new Point(
                centerX + radius * Math.Cos(endAngleRad),
                centerY + radius * Math.Sin(endAngleRad)
            );
            
            Point innerStart = new Point(
                centerX + innerRadius * Math.Cos(startAngleRad),
                centerY + innerRadius * Math.Sin(startAngleRad)
            );
            
            Point innerEnd = new Point(
                centerX + innerRadius * Math.Cos(endAngleRad),
                centerY + innerRadius * Math.Sin(endAngleRad)
            );
            
            bool largeArc = angleSize > 180;
            
            var figure = new PathFigure { StartPoint = outerStart };
            
            figure.Segments.Add(new ArcSegment
            {
                Point = outerEnd,
                Size = new Size(radius, radius),
                IsLargeArc = largeArc,
                SweepDirection = SweepDirection.Clockwise
            });
            
            figure.Segments.Add(new LineSegment { Point = innerEnd });
            
            figure.Segments.Add(new ArcSegment
            {
                Point = innerStart,
                Size = new Size(innerRadius, innerRadius),
                IsLargeArc = largeArc,
                SweepDirection = SweepDirection.Counterclockwise
            });
            
            figure.IsClosed = true;
            
            return new PathGeometry { Figures = { figure } };
        }
    }
    
    public class ProjetTimelineItem
    {
        public string NomProjet { get; set; }
        public double Pourcentage { get; set; }
    }

    public class ReportingContributionInfo
    {
        public string NomDeveloppeur { get; set; }
        public string NomEquipe { get; set; }
        public int TachesTotal { get; set; }
        public int TachesCompletes { get; set; }
        public double HeuresEstimees { get; set; }
        public double PourcentageContribution { get; set; }
        
        public string TauxCompletion => TachesTotal > 0 
            ? $"{Math.Round((double)TachesCompletes / TachesTotal * 100, 0)}%" 
            : "0%";
        
        public string JoursEstimes
        {
            get
            {
                double jours = HeuresEstimees / 8.0;
                if (jours == 0) return "0j";
                
                int joursEntiers = (int)Math.Floor(jours);
                double reste = jours - joursEntiers;
                
                if (reste >= 0.75)
                {
                    return $"{joursEntiers + 1}j";
                }
                else if (reste >= 0.25)
                {
                    return $"{joursEntiers}.5j";
                }
                else
                {
                    return $"{joursEntiers}j";
                }
            }
        }
    }

    public class ReportingPercentageToWidthConverter : System.Windows.Data.IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (values.Length == 2 && values[0] is double percentage && values[1] is double maxWidth)
            {
                return (percentage / 100.0) * maxWidth;
            }
            return 0.0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    
    public class ColorStringToBrushConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string colorString && !string.IsNullOrEmpty(colorString))
            {
                try
                {
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorString));
                }
                catch
                {
                    return new SolidColorBrush(Colors.Gray);
                }
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

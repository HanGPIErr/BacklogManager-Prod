using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
                MessageBox.Show($"Erreur lors du chargement des données: {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
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
                var backlogService = new BacklogService(_database);
                var toutesLesTaches = backlogService.GetAllBacklogItemsIncludingArchived();
                
                // Récupérer tous les projets du programme
                var projets = _database.GetProjets().Where(p => p.ProgrammeId == programme.Id && p.Actif).ToList();
                
                if (projets.Count == 0)
                {
                    MessageBox.Show("Aucun projet actif trouvé pour ce programme.", 
                        "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    MasquerKPIs();
                    return;
                }
                
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
                MessageBox.Show($"Erreur lors du chargement des statistiques: {ex.Message}", 
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show($"Erreur lors du chargement des statistiques: {ex.Message}", 
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
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
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BacklogManager.Domain;
using BacklogManager.Services;
using BacklogManager.ViewModels;

namespace BacklogManager.Views.Pages
{
    public partial class DetailProjetSteerCoView : UserControl
    {
        private readonly IDatabase _database;
        private Projet _projet;
        private readonly Action _onNavigateBack;
        private readonly AuthenticationService _authService;

        public DetailProjetSteerCoView(IDatabase database, int projetId, Action onNavigateBack, AuthenticationService authService)
        {
            InitializeComponent();
            _database = database;
            _onNavigateBack = onNavigateBack;
            _authService = authService;
            
            // Vérifier les permissions (Admin uniquement)
            var currentRole = _authService.GetCurrentUserRole();
            if (currentRole == null || currentRole.Type != RoleType.Administrateur)
            {
                MessageBox.Show("Accès refusé. Cette vue est réservée aux administrateurs.", 
                    "Accès refusé", MessageBoxButton.OK, MessageBoxImage.Warning);
                _onNavigateBack?.Invoke();
                return;
            }
            
            // Charger le projet
            var projets = _database.GetProjets();
            _projet = projets.FirstOrDefault(p => p.Id == projetId);
            
            if (_projet != null)
            {
                ChargerDetails();
                ChargerContributionsRessources();
            }
        }

        private void ChargerDetails()
        {
            // Header
            TxtNomProjet.Text = _projet.Nom;
            TxtProjetId.Text = _projet.Id.ToString();
            TxtDescription.Text = _projet.Description ?? "Aucune description";
            
            // Programme
            if (_projet.ProgrammeId.HasValue)
            {
                var programme = _database.GetProgrammeById(_projet.ProgrammeId.Value);
                TxtProgramme.Text = programme != null ? programme.Nom : "Aucun programme";
            }
            else
            {
                TxtProgramme.Text = "Aucun programme";
            }
            
            // Priorité
            TxtPriorite.Text = _projet.Priorite ?? "Non définie";
            
            // Statut RAG avec couleurs
            string statutRAG = _projet.StatutRAG ?? "Green";
            TxtStatutRAG.Text = statutRAG.ToUpper();
            BadgeStatutRAG.Background = GetStatutRAGColor(statutRAG);
            
            // KPIs - Avancement
            var backlogService = new BacklogService(_database);
            var toutesLesTaches = backlogService.GetAllBacklogItemsIncludingArchived();
            var taches = toutesLesTaches.Where(t => t.ProjetId == _projet.Id).ToList();
            
            int totalTaches = taches.Count();
            // Les tâches archivées sont forcément terminées
            int tachesCompletes = taches.Count(t => t.Statut == Statut.Termine || t.EstArchive);
            double avancement = totalTaches > 0 ? Math.Round((double)tachesCompletes / totalTaches * 100, 0) : 0;
            
            TxtAvancement.Text = avancement.ToString("0");
            BarAvancement.Value = avancement;
            
            // KPIs - Tâches
            TxtTachesCompletes.Text = tachesCompletes.ToString();
            TxtTachesTotal.Text = totalTaches.ToString();
            int tachesRestantes = totalTaches - tachesCompletes;
            TxtTachesRestantes.Text = string.Format("{0} tâche(s) restante(s)", tachesRestantes);
            
            // KPIs - Équipes
            int nbEquipes = _projet.EquipesAssigneesIds != null ? _projet.EquipesAssigneesIds.Count : 0;
            TxtNbEquipes.Text = nbEquipes.ToString();
            
            // KPIs - Livraison
            if (_projet.DateFinPrevue.HasValue)
            {
                TxtTargetDelivery.Text = _projet.DateFinPrevue.Value.ToString("dd/MM/yyyy");
            }
            else if (!string.IsNullOrEmpty(_projet.TargetDelivery))
            {
                TxtTargetDelivery.Text = _projet.TargetDelivery;
            }
            else
            {
                TxtTargetDelivery.Text = "Non définie";
            }
            
            // Calcul deadline info
            if (_projet.DateFinPrevue.HasValue)
            {
                var joursRestants = (_projet.DateFinPrevue.Value - DateTime.Now).Days;
                if (joursRestants > 0)
                {
                    TxtDeadlineInfo.Text = string.Format("Dans {0} jour(s)", joursRestants);
                }
                else if (joursRestants == 0)
                {
                    TxtDeadlineInfo.Text = "Aujourd'hui !";
                }
                else
                {
                    TxtDeadlineInfo.Text = string.Format("Retard de {0} jour(s)", Math.Abs(joursRestants));
                    TxtDeadlineInfo.Foreground = new SolidColorBrush(Color.FromRgb(244, 67, 54)); // Red
                }
            }
            else
            {
                TxtDeadlineInfo.Text = "Date non définie";
            }
            
            // Timeline
            TxtDateDebut.Text = _projet.DateDebut?.ToString("dd/MM/yyyy") ?? "Non définie";
            TxtDateFin.Text = _projet.DateFinPrevue?.ToString("dd/MM/yyyy") ?? "Non définie";
            
            // Progression temporelle
            if (_projet.DateDebut.HasValue && _projet.DateFinPrevue.HasValue)
            {
                var debut = _projet.DateDebut.Value;
                var fin = _projet.DateFinPrevue.Value;
                var maintenant = DateTime.Now;
                
                if (maintenant >= debut && maintenant <= fin)
                {
                    var dureeTotal = (fin - debut).TotalDays;
                    var dureeEcoulee = (maintenant - debut).TotalDays;
                    var progressionTemps = Math.Round((dureeEcoulee / dureeTotal) * 100, 0);
                    
                    ProgressTemporel.Width = (progressionTemps / 100.0) * 800; // Largeur approximative
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
            TxtGainsTemps.Text = _projet.GainsTemps ?? "Non spécifié";
            TxtGainsFinanciers.Text = _projet.GainsFinanciers ?? "Non spécifié";
            
            // Note: Détails SteerCo (Lead, Équipes, Périmètre, Next Actions) 
            // peuvent être ajoutés dans une section additionnelle si nécessaire
        }

        private void ChargerContributionsRessources()
        {
            var backlogService = new BacklogService(_database);
            var toutesLesTaches = backlogService.GetAllBacklogItemsIncludingArchived();
            var tachesProjet = toutesLesTaches.Where(t => t.ProjetId == _projet.Id).ToList();
            
            if (tachesProjet.Count == 0)
            {
                return;
            }
            
            // Calculer la contribution de chaque développeur
            var contributions = new Dictionary<string, ContributionInfo>();
            
            foreach (var tache in tachesProjet)
            {
                if (tache.DevAssigneId.HasValue)
                {
                    var dev = _database.GetUtilisateurs().FirstOrDefault(u => u.Id == tache.DevAssigneId.Value);
                    if (dev != null)
                    {
                        string devKey = dev.Nom;
                        
                        if (!contributions.ContainsKey(devKey))
                        {
                            contributions[devKey] = new ContributionInfo
                            {
                                NomDeveloppeur = devKey,
                                TachesTotal = 0,
                                TachesCompletes = 0,
                                HeuresEstimees = 0
                            };
                        }
                        
                        contributions[devKey].TachesTotal++;
                        // Compter comme terminé si Statut=Terminé OU si archivé
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

        private SolidColorBrush GetStatutRAGColor(string statut)
        {
            switch (statut?.ToLower())
            {
                case "green":
                    return new SolidColorBrush(Color.FromRgb(76, 175, 80)); // #4CAF50
                case "amber":
                case "orange":
                    return new SolidColorBrush(Color.FromRgb(255, 152, 0)); // #FF9800
                case "red":
                    return new SolidColorBrush(Color.FromRgb(244, 67, 54)); // #F44336
                default:
                    return new SolidColorBrush(Color.FromRgb(158, 158, 158)); // #9E9E9E (Grey)
            }
        }

        private void BtnRetour_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _onNavigateBack?.Invoke();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du retour: {ex.Message}", "Erreur", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnTimeline_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_projet == null)
                {
                    MessageBox.Show("Aucun projet sélectionné.", "Erreur", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Trouver le MainWindow et son ContentControl
                var mainWindow = Window.GetWindow(this) as MainWindow;
                if (mainWindow == null)
                {
                    MessageBox.Show("Impossible de trouver la fenêtre principale", "Erreur Navigation", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Créer la vue Timeline avec le viewmodel
                var craService = new CRAService(_database);
                var backlogService = new BacklogService(_database);
                var programmeService = new ProgrammeService(_database);
                var currentUser = _authService.CurrentUser;
                var currentRole = _authService.GetCurrentUserRole();
                var permissionService = new PermissionService(currentUser, currentRole);
                var suiviCRAViewModel = new SuiviCRAViewModel(craService, backlogService, programmeService, permissionService);
                
                // Passer en mode Timeline
                suiviCRAViewModel.ModeAffichage = "timeline";
                
                // Sélectionner le projet courant
                suiviCRAViewModel.ProjetSelectionne = _projet;
                
                var suiviCRAView = new Views.SuiviCRAView();
                suiviCRAView.DataContext = suiviCRAViewModel;

                var contentControl = mainWindow.FindName("MainContentControl") as ContentControl;
                if (contentControl != null)
                {
                    contentControl.Content = suiviCRAView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ouverture de la timeline: {ex.Message}", 
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnAnalyseIA_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Récupérer toutes les tâches du projet (incluant archivées)
                var backlogService = new BacklogService(_database);
                var toutesLesTaches = backlogService.GetAllBacklogItemsIncludingArchived();
                var tachesProjet = toutesLesTaches.Where(t => t.ProjetId == _projet.Id).ToList();
                
                // Ouvrir la fenêtre d'analyse IA
                var analyseWindow = new AnalyseProjetIAWindow(_projet, tachesProjet);
                analyseWindow.Owner = Window.GetWindow(this);
                analyseWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ouverture de l'analyse IA: {ex.Message}", "Erreur", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public class ContributionInfo
    {
        public string NomDeveloppeur { get; set; }
        public int TachesTotal { get; set; }
        public int TachesCompletes { get; set; }
        public double HeuresEstimees { get; set; }
        public double PourcentageContribution { get; set; }
        
        public string TauxCompletion => TachesTotal > 0 
            ? $"{Math.Round((double)TachesCompletes / TachesTotal * 100, 0)}%" 
            : "0%";
        
        // Conversion en jours (1j = 8h) avec affichage en jours et demi-jours
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

    public class PercentageToWidthConverter : System.Windows.Data.IMultiValueConverter
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

using System.Linq;
using System.Windows;
using BacklogManager.Services;
using BacklogManager.ViewModels;
using BacklogManager.Views;

namespace BacklogManager
{
    public partial class MainWindow : Window
    {
        private BacklogService _backlogService;
        private readonly AuthenticationService _authService;
        private readonly IDatabase _database;
        private PermissionService _permissionService;
        private NotificationService _notificationService;
        private EmailService _emailService;
        private System.Windows.Threading.DispatcherTimer _notificationTimer;

        public MainWindow(AuthenticationService authService)
        {
            InitializeComponent();
            
            _authService = authService;
            _database = new SqliteDatabase();
            
            // Initialiser le PermissionService
            var currentUser = _authService.CurrentUser;
            var currentRole = _authService.GetCurrentUserRole();
            _permissionService = new PermissionService(currentUser, currentRole);
            
            // Récupérer l'AuditLogService depuis AuthenticationService
            var auditLogService = _authService.GetAuditLogService();
            
            // Initialiser le service avec audit
            _backlogService = new BacklogService(_database, auditLogService);
            var pokerService = new PokerService(_database, _backlogService);
            
            // Initialiser le NotificationService
            _notificationService = new NotificationService(_backlogService, _database);
            
            // Initialiser l'EmailService
            _emailService = new EmailService(_backlogService, _authService);
            
            // Initialiser le CRAService
            var craService = new CRAService(_database);
            
            // Initialiser les ViewModels avec PermissionService et CRAService
            var projetsViewModel = new ProjetsViewModel(_backlogService, _permissionService, craService);
            var backlogViewModel = new BacklogViewModel(_backlogService, _permissionService, craService);
            var kanbanViewModel = new KanbanViewModel(_backlogService, _permissionService, craService);
            var pokerViewModel = new PokerViewModel(_backlogService, pokerService);
            var timelineViewModel = new TimelineViewModel(_backlogService);
            
            // Synchronisation bidirectionnelle Backlog ↔ Kanban
            backlogViewModel.TacheCreated += (s, e) => kanbanViewModel.LoadItems();
            kanbanViewModel.TacheStatutChanged += (s, e) => backlogViewModel.LoadData();
            
            // Initialiser le MainViewModel
            var mainViewModel = new MainViewModel(projetsViewModel, backlogViewModel, kanbanViewModel, pokerViewModel, timelineViewModel);
            this.DataContext = mainViewModel;
            
            // Charger après l'initialisation complète
            Loaded += (s, e) =>
            {
                ChargerInfoProjets();
                AfficherUtilisateurConnecte();
                VerifierPermissions();
                VerifierPermissionsAdmin();
                InitialiserNotifications();
                
                // Afficher le Dashboard par défaut
                BtnDashboard_Click(null, null);
            };
        }

        private void InitialiserNotifications()
        {
            // Générer les notifications initiales
            _notificationService.AnalyserEtGenererNotifications();
            MettreAJourBadgeNotifications();
            
            // Timer pour mettre à jour les notifications toutes les 5 minutes
            _notificationTimer = new System.Windows.Threading.DispatcherTimer();
            _notificationTimer.Interval = System.TimeSpan.FromMinutes(5);
            _notificationTimer.Tick += (s, e) =>
            {
                _notificationService.AnalyserEtGenererNotifications();
                MettreAJourBadgeNotifications();
            };
            _notificationTimer.Start();
        }

        private void MettreAJourBadgeNotifications()
        {
            int count = _notificationService.GetCountNotificationsNonLues();
            if (count > 0)
            {
                BadgeNotifications.Visibility = Visibility.Visible;
                TxtBadgeCount.Text = count > 99 ? "99+" : count.ToString();
            }
            else
            {
                BadgeNotifications.Visibility = Visibility.Collapsed;
            }
        }

        private void AfficherUtilisateurConnecte()
        {
            if (_authService?.CurrentUser != null)
            {
                var user = _authService.CurrentUser;
                var role = _authService.GetCurrentUserRole();
                
                // Format: "👤 Prénom Nom"
                TxtUtilisateurConnecte.Text = string.Format("👤 {0} {1}", user.Prenom, user.Nom);
                
                // Format: "📊 Rôle"
                if (role != null)
                {
                    string iconeRole = role.Type == Domain.RoleType.Administrateur ? "👑" :
                                       role.Type == Domain.RoleType.ChefDeProjet ? "📊" :
                                       role.Type == Domain.RoleType.BusinessAnalyst ? "📋" :
                                       "💻";
                    TxtRoleUtilisateur.Text = string.Format("{0} {1}", iconeRole, role.Nom);
                }
            }
        }

        private void VerifierPermissions()
        {
            // Afficher/masquer les boutons selon les permissions
            var hasAdminAccess = _permissionService.PeutAccederAdministration;
            
            BtnAdmin.Visibility = hasAdminAccess ? Visibility.Visible : Visibility.Collapsed;
            BtnDemandes.Visibility = _permissionService.PeutCreerDemandes ? Visibility.Visible : Visibility.Collapsed;
            BtnStatistiques.Visibility = _permissionService.PeutVoirKPI ? Visibility.Visible : Visibility.Collapsed;
            
            // Le bouton Paramètres est réservé à l'Administrateur uniquement
            BtnParametres.Visibility = _permissionService.EstAdministrateur ? Visibility.Visible : Visibility.Collapsed;
            
            // Masquer toute la section ADMINISTRATION si pas d'accès
            TxtHeaderAdmin.Visibility = hasAdminAccess ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ChargerInfoProjets()
        {
            try
            {
                var projets = _backlogService.GetAllProjets();
                TxtNbProjets.Text = string.Format("{0} projet(s) actif(s)", projets.Count);
            }
            catch
            {
                // Ignorer les erreurs de chargement initial
            }
        }

        private void BtnGererEquipe_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var window = new GestionEquipeWindow(_backlogService);
                window.Owner = this;
                window.ShowDialog();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(string.Format("Erreur lors de l'ouverture de la gestion de l'équipe: {0}", ex.Message), 
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnGererProjets_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var window = new GestionProjetsWindow(_backlogService);
                window.Owner = this;
                window.ShowDialog();
                
                // Rafraîchir après fermeture
                ChargerInfoProjets();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(string.Format("Erreur lors de l'ouverture de la gestion des projets: {0}", ex.Message), 
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnAdmin_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Vérifier les permissions d'administration
                if (!_permissionService.PeutAccederAdministration)
                {
                    MessageBox.Show("Vous n'avez pas les droits pour accéder à l'administration.", 
                        "Accès refusé", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var auditLogService = _authService.GetAuditLogService();
                var adminView = new AdministrationView(_database, auditLogService);
                var contentControl = (System.Windows.Controls.ContentControl)this.FindName("MainContentControl");
                if (contentControl != null)
                {
                    contentControl.Content = adminView;
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(string.Format("Erreur lors de l'ouverture de l'administration: {0}", ex.Message), 
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnStatistiques_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Vérifier les permissions KPI
                if (!_permissionService.PeutVoirKPI)
                {
                    MessageBox.Show("Vous n'avez pas les droits pour accéder aux statistiques.", 
                        "Accès refusé", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Créer le ViewModel avec les services nécessaires
                var craService = new CRAService(_database);
                var statistiquesViewModel = new StatistiquesViewModel(_backlogService, craService, _database);

                // Créer la vue et lier le ViewModel
                var statistiquesView = new Views.StatistiquesView();
                statistiquesView.DataContext = statistiquesViewModel;

                // Afficher dans le ContentControl principal
                var contentControl = (System.Windows.Controls.ContentControl)this.FindName("MainContentControl");
                if (contentControl != null)
                {
                    contentControl.Content = statistiquesView;
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(string.Format("Erreur lors de l'ouverture des statistiques: {0}\n\nDétails: {1}", 
                    ex.Message, ex.StackTrace), 
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnParametres_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Vérifier que seul l'Administrateur peut accéder aux paramètres
                if (!_permissionService.EstAdministrateur)
                {
                    MessageBox.Show("Accès refusé.\n\nSeul le rôle Administrateur peut accéder aux paramètres système.", 
                        "Paramètres - Accès restreint", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var window = new ParametresWindow(_database);
                window.Owner = this;
                window.ShowDialog();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(string.Format("Erreur lors de l'ouverture des paramètres: {0}\n\nDétails: {1}", 
                    ex.Message, ex.StackTrace), 
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnNotifications_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var window = new NotificationsWindow(_notificationService, _emailService);
                window.Owner = this;
                window.ShowDialog();
                
                // Mettre à jour le badge après fermeture
                MettreAJourBadgeNotifications();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(string.Format("Erreur lors de l'ouverture des notifications: {0}\n\nDétails: {1}", 
                    ex.Message, ex.StackTrace), 
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnDashboard_Click(object sender, RoutedEventArgs e)
        {
            var dashboardView = new Views.DashboardView(_backlogService, _notificationService, _authService);
            // Trouver le ContentControl dans le XAML et mettre à jour son contenu
            var contentControl = (System.Windows.Controls.ContentControl)this.FindName("MainContentControl");
            if (contentControl != null)
            {
                contentControl.Content = dashboardView;
            }
        }

        private void BtnBacklog_Click(object sender, RoutedEventArgs e)
        {
            var backlogView = new Views.BacklogView();
            var mainViewModel = (MainViewModel)this.DataContext;
            backlogView.DataContext = mainViewModel.BacklogViewModel;
            var contentControl = (System.Windows.Controls.ContentControl)this.FindName("MainContentControl");
            if (contentControl != null)
            {
                contentControl.Content = backlogView;
            }
        }

        private void BtnKanban_Click(object sender, RoutedEventArgs e)
        {
            var kanbanView = new Views.KanbanView();
            var mainViewModel = (MainViewModel)this.DataContext;
            kanbanView.DataContext = mainViewModel.KanbanViewModel;
            var contentControl = (System.Windows.Controls.ContentControl)this.FindName("MainContentControl");
            if (contentControl != null)
            {
                contentControl.Content = kanbanView;
            }
        }

        private void BtnTimeline_Click(object sender, RoutedEventArgs e)
        {
            // Vérifier les permissions admin
            if (!_permissionService.EstAdministrateur)
            {
                MessageBox.Show(
                    "Seul l'administrateur peut accéder au suivi des CRA.",
                    "Accès refusé",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var craService = new CRAService(_database);
            var suiviCRAViewModel = new SuiviCRAViewModel(craService, _backlogService, _permissionService);
            var suiviCRAView = new Views.SuiviCRAView();
            suiviCRAView.DataContext = suiviCRAViewModel;

            var contentControl = (System.Windows.Controls.ContentControl)this.FindName("MainContentControl");
            if (contentControl != null)
            {
                contentControl.Content = suiviCRAView;
            }
        }

        private void BtnDemandes_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var demandesView = new DemandesView(_authService, _permissionService);
                MainContentControl.Content = demandesView;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(string.Format("Erreur lors de l'ouverture des demandes: {0}", ex.Message), 
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnSaisirCRA_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Créer le ViewModel pour la vue calendrier
                var craService = new CRAService(_database);
                var vm = new ViewModels.CRACalendrierViewModel(craService, _backlogService, _authService, _permissionService);
                
                // Créer et afficher la vue
                var craView = new Views.CRACalendrierView
                {
                    DataContext = vm
                };
                
                // Remplacer le contenu principal
                MainContentControl.Content = craView;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(string.Format("Erreur lors de l'ouverture de la saisie CRA: {0}", ex.Message),
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnHistoriqueCRA_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var currentUser = _authService.CurrentUser;
                var isAdmin = _permissionService.IsAdmin;
                var window = new CRAHistoriqueWindow(_database, currentUser.Id, isAdmin)
                {
                    Owner = this
                };
                window.ShowDialog();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(string.Format("Erreur lors de l'ouverture de l'historique CRA: {0}", ex.Message),
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void VerifierPermissionsAdmin()
        {
            // Masquer la section admin si pas admin
            var adminSection = (System.Windows.Controls.TextBlock)this.FindName("AdminSectionTitle");
            var btnTimeline = (System.Windows.Controls.Button)this.FindName("BtnTimeline");

            if (!_permissionService.EstAdministrateur)
            {
                if (adminSection != null) adminSection.Visibility = Visibility.Collapsed;
                if (btnTimeline != null) btnTimeline.Visibility = Visibility.Collapsed;
            }
        }

        public void AfficherBacklog()
        {
            BtnBacklog_Click(null, null);
        }

        public void AfficherNotifications()
        {
            BtnNotifications_Click(null, null);
        }

        public void AfficherKanban()
        {
            BtnKanban_Click(null, null);
        }

        public void AfficherTimeline()
        {
            BtnTimeline_Click(null, null);
        }
    }
}

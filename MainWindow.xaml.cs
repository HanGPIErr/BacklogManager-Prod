using System.Linq;
using System.Windows;
using BacklogManager.Domain;
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
            
            // Exposer les services dans App pour NotificationsView
            var app = Application.Current as App;
            if (app != null)
            {
                app.NotificationService = _notificationService;
                app.EmailService = _emailService;
            }
            
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
            
            // Le bouton Paramètres est maintenant accessible à tous (préférences personnelles)
            BtnParametres.Visibility = Visibility.Visible;
            
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
                var adminView = new AdministrationView(_database, auditLogService, _authService);
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
                // Maintenant accessible à tous pour les préférences personnelles
                // Les sections sensibles (export/import, maintenance) sont masquées pour les non-admins dans la vue
                var parametresView = new ParametresView(_database, _permissionService);
                var contentControl = (System.Windows.Controls.ContentControl)this.FindName("MainContentControl");
                if (contentControl != null)
                {
                    contentControl.Content = parametresView;
                }
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
                var notificationsView = new Views.NotificationsView();
                var contentControl = (System.Windows.Controls.ContentControl)this.FindName("MainContentControl");
                if (contentControl != null)
                {
                    contentControl.Content = notificationsView;
                }
                
                // Mettre à jour le badge
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
            NaviguerVersDashboard();
        }

        public void NaviguerVersDashboard()
        {
            var dashboardView = new Views.DashboardView(_backlogService, _notificationService, _authService, _permissionService);
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
            var programmeService = new ProgrammeService(_database);
            var suiviCRAViewModel = new SuiviCRAViewModel(craService, _backlogService, programmeService, _permissionService);
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
                var demandesView = new DemandesView(_database, _authService, _permissionService);
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

        public void AfficherGuide()
        {
            try
            {
                var guideView = new Views.GuideUtilisateurView(_authService, _database, this);
                MainContentControl.Content = guideView;
                DeselectAllMenuButtons();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Erreur : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void AfficherChatIA()
        {
            try
            {
                var chatHistoryService = new ChatHistoryService(_database);
                var chatView = new Views.AgentChatView(chatHistoryService, _authService.CurrentUser, this);
                MainContentControl.Content = chatView;
                DeselectAllMenuButtons();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Erreur : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeselectAllMenuButtons()
        {
            BtnDashboard.Background = System.Windows.Media.Brushes.Transparent;
            BtnBacklog.Background = System.Windows.Media.Brushes.Transparent;
            BtnDemandes.Background = System.Windows.Media.Brushes.Transparent;
            BtnKanban.Background = System.Windows.Media.Brushes.Transparent;
            BtnTimeline.Background = System.Windows.Media.Brushes.Transparent;
            BtnSaisirCRA.Background = System.Windows.Media.Brushes.Transparent;
            BtnStatistiques.Background = System.Windows.Media.Brushes.Transparent;
            BtnNotifications.Background = System.Windows.Media.Brushes.Transparent;
        }

        public void NaviguerVersSuiviCRATimeline(Projet projet)
        {
            try
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

                // Créer le ViewModel et la vue
                var craService = new CRAService(_database);
                var programmeService = new ProgrammeService(_database);
                var suiviCRAViewModel = new SuiviCRAViewModel(craService, _backlogService, programmeService, _permissionService);
                var suiviCRAView = new Views.SuiviCRAView();
                suiviCRAView.DataContext = suiviCRAViewModel;

                // Définir le mode timeline et sélectionner le projet
                suiviCRAViewModel.ModeAffichage = "timeline";
                suiviCRAViewModel.ProjetSelectionne = projet;

                // Afficher la vue
                var contentControl = (System.Windows.Controls.ContentControl)this.FindName("MainContentControl");
                if (contentControl != null)
                {
                    contentControl.Content = suiviCRAView;
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(string.Format("Erreur lors de la navigation vers la timeline: {0}", ex.Message),
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void NaviguerVersDetailEquipe(int equipeId)
        {
            try
            {
                var detailView = new Views.Pages.DetailEquipeView(
                    equipeId, 
                    _database, 
                    () => NaviguerVersDashboard(),
                    _authService
                );
                var contentControl = (System.Windows.Controls.ContentControl)this.FindName("MainContentControl");
                if (contentControl != null)
                {
                    contentControl.Content = detailView;
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(string.Format("Erreur lors de la navigation vers l'équipe: {0}", ex.Message),
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnChangerUtilisateur_Click(object sender, RoutedEventArgs e)
        {
            var window = new ChangerUtilisateurWindow(_database);
            window.Owner = this;
            
            if (window.ShowDialog() == true && window.UtilisateurSelectionne != null)
            {
                var role = _database.GetRoles().FirstOrDefault(r => r.Id == window.UtilisateurSelectionne.RoleId);
                
                // Afficher le message avant de redémarrer
                var result = MessageBox.Show(
                    $"Vous allez être connecté en tant que:\n{window.UtilisateurSelectionne.Prenom} {window.UtilisateurSelectionne.Nom}\n\nRôle: {role?.Nom}\n\nL'application va redémarrer pour appliquer les changements.", 
                    "Changement d'utilisateur", 
                    MessageBoxButton.OKCancel, 
                    MessageBoxImage.Information);
                
                if (result == MessageBoxResult.OK)
                {
                    // Sauvegarder l'utilisateur choisi dans un fichier temporaire
                    var tempFile = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, ".switch_user");
                    System.IO.File.WriteAllText(tempFile, window.UtilisateurSelectionne.UsernameWindows);
                    
                    // Redémarrer l'application
                    System.Diagnostics.Process.Start(System.Reflection.Assembly.GetExecutingAssembly().Location);
                    Application.Current.Shutdown();
                }
            }
        }
    }
}

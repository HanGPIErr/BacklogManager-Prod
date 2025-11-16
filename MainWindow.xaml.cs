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
            _notificationService = new NotificationService(_backlogService);
            
            // Initialiser le NotificationService
            _notificationService = new NotificationService(_backlogService);
            
            // Initialiser les ViewModels avec PermissionService
            var projetsViewModel = new ProjetsViewModel(_backlogService, _permissionService);
            var backlogViewModel = new BacklogViewModel(_backlogService, _permissionService);
            var kanbanViewModel = new KanbanViewModel(_backlogService, _permissionService);
            var pokerViewModel = new PokerViewModel(_backlogService, pokerService);
            var timelineViewModel = new TimelineViewModel(_backlogService);
            
            // Initialiser le MainViewModel
            var mainViewModel = new MainViewModel(projetsViewModel, backlogViewModel, kanbanViewModel, pokerViewModel, timelineViewModel);
            this.DataContext = mainViewModel;
            
            // Charger après l'initialisation complète
            Loaded += (s, e) =>
            {
                ChargerEquipe();
                ChargerInfoProjets();
                AfficherUtilisateurConnecte();
                VerifierPermissions();
                InitialiserNotifications();
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
            if (_authService.EstConnecte)
            {
                var user = _authService.CurrentUser;
                var role = _authService.GetCurrentUserRole();
                TxtUtilisateurConnecte.Text = string.Format("{0} {1} - {2}", user.Nom, user.Prenom, role != null ? role.Nom : "");
            }
        }

        private void VerifierPermissions()
        {
            // Afficher/masquer les boutons selon les permissions
            BtnAdmin.Visibility = _permissionService.PeutAccederAdministration ? Visibility.Visible : Visibility.Collapsed;
            BtnGererEquipe.Visibility = _permissionService.PeutGererEquipe ? Visibility.Visible : Visibility.Collapsed;
            BtnDemandes.Visibility = _permissionService.PeutCreerDemandes ? Visibility.Visible : Visibility.Collapsed;
            BtnStatistiques.Visibility = _permissionService.PeutVoirKPI ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ChargerEquipe()
        {
            try
            {
                var devs = _backlogService.GetAllDevs();
                LstEquipe.ItemsSource = devs;
            }
            catch
            {
                // Ignorer les erreurs de chargement initial
            }
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
                
                // Rafraîchir après fermeture
                ChargerEquipe();
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
                var window = new AdministrationWindow(_database, auditLogService);
                window.Owner = this;
                window.ShowDialog();
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

                var window = new StatistiquesWindow(_backlogService, _database);
                window.Owner = this;
                window.ShowDialog();
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
                var window = new NotificationsWindow(_notificationService);
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

        private void BtnDemandes_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var demandesView = new DemandesView(_authService, _permissionService);
                var window = new Window
                {
                    Title = "Gestion des demandes",
                    Content = demandesView,
                    Width = 1200,
                    Height = 800,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = this
                };
                window.ShowDialog();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(string.Format("Erreur lors de l'ouverture des demandes: {0}", ex.Message), 
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

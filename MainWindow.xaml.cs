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

        public MainWindow(AuthenticationService authService)
        {
            InitializeComponent();
            
            _authService = authService;
            _database = new SqliteDatabase();
            
            // Initialiser le service
            _backlogService = new BacklogService(_database);
            var pokerService = new PokerService(_database, _backlogService);
            
            // Initialiser les ViewModels
            var projetsViewModel = new ProjetsViewModel(_backlogService);
            var backlogViewModel = new BacklogViewModel(_backlogService);
            var kanbanViewModel = new KanbanViewModel(_backlogService);
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
            };
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
            if (!_authService.HasPermission("GererUtilisateurs"))
            {
                // Masquer le bouton de gestion utilisateurs si pas admin
                // À implémenter selon vos besoins
            }
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
                if (!_authService.HasPermission("GererUtilisateurs"))
                {
                    MessageBox.Show("Vous n'avez pas les droits pour accéder à l'administration.", 
                        "Accès refusé", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var window = new GestionUtilisateursWindow(_database);
                window.Owner = this;
                window.ShowDialog();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(string.Format("Erreur lors de l'ouverture de l'administration: {0}", ex.Message), 
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnDemandes_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var demandesView = new DemandesView();
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

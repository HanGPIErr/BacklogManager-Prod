using System;
using System.Linq;
using System.Security.Principal;
using System.Windows;
using BacklogManager.Services;
using BacklogManager.Domain;

namespace BacklogManager.Views
{
    public partial class LoginWindow : Window
    {
        private readonly AuthenticationService _authService;
        private readonly InitializationService _initService;

        public LoginWindow()
        {
            InitializeComponent();
            
            var database = new SqliteDatabase();
            _authService = new AuthenticationService(database);
            _initService = new InitializationService(database);
            
            // Initialiser le service de configuration IA
            AIConfigService.Initialize(database);
            
            // Assigner les services à App pour qu'ils soient accessibles partout
            var app = Application.Current as App;
            if (app != null)
            {
                app.AuthService = _authService;
                app.Database = database;
            }
            
            // Initialiser les données par défaut
            _initService.InitializeDefaultData();
            
            // Afficher le username
            AfficherUsername();
            
            // Connexion automatique
            Loaded += (s, e) => TentativeConnexionAutomatique();
            
            // Vérifier les mises à jour en arrière-plan
            Loaded += async (s, e) => await VerifierMisesAJour();
        }

        private async System.Threading.Tasks.Task VerifierMisesAJour()
        {
            try
            {
                var updateService = new UpdateService();
                var versionInfo = await updateService.CheckForUpdatesAsync();

                if (versionInfo != null)
                {
                    // Afficher la fenêtre de mise à jour
                    Dispatcher.Invoke(() =>
                    {
                        var updateWindow = new UpdateWindow(versionInfo, updateService);
                        updateWindow.ShowDialog();
                    });
                }
            }
            catch (Exception ex)
            {
                // Ignorer les erreurs de mise à jour pour ne pas bloquer l'application
                System.Diagnostics.Debug.WriteLine($"Erreur vérification MAJ: {ex.Message}");
            }
        }

        private void TentativeConnexionAutomatique()
        {
            try
            {
                // Authentification Windows pure - récupérer le username Windows de l'utilisateur
                string windowsUsername = WindowsIdentity.GetCurrent().Name;
                if (windowsUsername.Contains("\\"))
                {
                    windowsUsername = windowsUsername.Split('\\')[1];
                }

                // Gestion des droits admin automatiques
                var database = ((App)Application.Current).Database;
                var roleAdmin = database.GetRoles().FirstOrDefault(r => r.Type == RoleType.Administrateur);
                var utilisateurs = database.GetUtilisateurs();
                
                // CAS 1 : Premier utilisateur qui se connecte = auto-admin
                if (!utilisateurs.Any())
                {
                    var premierUtilisateur = new Utilisateur
                    {
                        UsernameWindows = windowsUsername,
                        Nom = "",
                        Prenom = windowsUsername,
                        Email = $"{windowsUsername}@bnpparibas.com",
                        RoleId = roleAdmin?.Id ?? 1,
                        Actif = true,
                        DateCreation = DateTime.Now,
                        Statut = "BAU"
                    };
                    database.AddOrUpdateUtilisateur(premierUtilisateur);
                }
                // CAS 2 : Utilisateur spécial 610506 = toujours admin
                else if (windowsUsername == "610506")
                {
                    var utilisateur = utilisateurs.FirstOrDefault(u => u.UsernameWindows == "610506");
                    
                    if (utilisateur == null)
                    {
                        // Créer le compte admin automatiquement
                        utilisateur = new Utilisateur
                        {
                            UsernameWindows = "610506",
                            Nom = "Admin",
                            Prenom = "Super",
                            Email = "610506@bnpparibas.com",
                            RoleId = roleAdmin?.Id ?? 1,
                            Actif = true,
                            DateCreation = DateTime.Now,
                            Statut = "BAU"
                        };
                        database.AddOrUpdateUtilisateur(utilisateur);
                    }
                    else if (utilisateur.RoleId != roleAdmin?.Id)
                    {
                        // S'assurer qu'il a le rôle admin
                        utilisateur.RoleId = roleAdmin?.Id ?? 1;
                        utilisateur.Actif = true;
                        database.AddOrUpdateUtilisateur(utilisateur);
                    }
                }

                // Tenter la connexion avec le username Windows
                bool success = _authService.LoginWithUsername(windowsUsername);
                if (success)
                {
                    // Ouvrir directement la fenêtre principale
                    var mainWindow = new MainWindow(_authService);
                    mainWindow.Show();
                    this.Close();
                    return;
                }

                // Si la connexion échoue, afficher le message
                Dispatcher.Invoke(() =>
                {
                    TxtUsername.Text = $"Compte non enregistré";
                    TxtStatut.Text = "Veuillez contacter votre administrateur";
                    TxtErreur.Text = $"Votre compte Windows '{windowsUsername}' n'est pas enregistré dans l'application.\n\nContactez votre administrateur pour créer votre compte utilisateur.";
                    TxtErreur.Visibility = Visibility.Visible;
                    BtnConnecter.IsEnabled = true;
                });
            }
            catch (Exception ex)
            {
                // En cas d'erreur, afficher un message
                Dispatcher.Invoke(() =>
                {
                    TxtUsername.Text = "Erreur de connexion";
                    TxtStatut.Text = "Une erreur est survenue";
                    TxtErreur.Text = $"Erreur: {ex.Message}";
                    TxtErreur.Visibility = Visibility.Visible;
                    BtnConnecter.IsEnabled = true;
                });
            }
        }

        private void AfficherUsername()
        {
            try
            {
                TxtUsername.Text = "Connexion automatique en cours...";
                TxtUsernameInput.Text = "";
                TxtStatut.Text = "";
            }
            catch (Exception ex)
            {
                TxtUsername.Text = "Erreur de connexion";
                TxtErreur.Text = ex.Message;
                TxtErreur.Visibility = Visibility.Visible;
            }
        }

        private void BtnConnecter_Click(object sender, RoutedEventArgs e)
        {
            TxtErreur.Visibility = Visibility.Collapsed;
            BtnConnecter.IsEnabled = false;
            TxtStatut.Text = "Connexion en cours...";

            try
            {
                string usernameToUse = TxtUsernameInput.Text.Trim();
                
                if (string.IsNullOrWhiteSpace(usernameToUse))
                {
                    // Utiliser l'authentification Windows automatique
                    usernameToUse = WindowsIdentity.GetCurrent().Name;
                    if (usernameToUse.Contains("\\"))
                    {
                        usernameToUse = usernameToUse.Split('\\')[1];
                    }
                }

                bool success = _authService.LoginWithUsername(usernameToUse);

                if (success)
                {
                    var user = _authService.CurrentUser;
                    var role = _authService.GetCurrentUserRole();

                    TxtStatut.Text = string.Format("Connecté avec succès en tant que {0} ({1})", user.Prenom + " " + user.Nom, role?.Nom);

                    // Ouvrir la fenêtre principale
                    var mainWindow = new MainWindow(_authService);
                    mainWindow.Show();
                    this.Close();
                }
                else
                {
                    TxtErreur.Text = string.Format("Accès refusé: Le compte '{0}' n'est pas enregistré dans l'application.\n\nContactez votre administrateur pour créer votre compte utilisateur.", usernameToUse);
                    TxtErreur.Visibility = Visibility.Visible;
                    TxtStatut.Text = "";
                    BtnConnecter.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                TxtErreur.Text = string.Format("Erreur lors de la connexion: {0}", ex.Message);
                TxtErreur.Visibility = Visibility.Visible;
                TxtStatut.Text = "";
                BtnConnecter.IsEnabled = true;
            }
        }
    }
}

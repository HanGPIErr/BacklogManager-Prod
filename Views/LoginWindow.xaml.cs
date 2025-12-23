using System;
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
            catch
            {
                // Ignorer les erreurs de mise à jour pour ne pas bloquer l'application
            }
        }

        private void TentativeConnexionAutomatique()
        {
            try
            {
                // Vérifier si un changement d'utilisateur est en cours
                var tempFile = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, ".switch_user");
                if (System.IO.File.Exists(tempFile))
                {
                    string switchUsername = System.IO.File.ReadAllText(tempFile).Trim();
                    System.IO.File.Delete(tempFile); // Supprimer le fichier après lecture
                    
                    bool success = _authService.LoginWithUsername(switchUsername);
                    if (success)
                    {
                        var mainWindow = new MainWindow(_authService);
                        mainWindow.Show();
                        this.Close();
                        return;
                    }
                }
                
                // Essayer d'abord avec le compte admin par défaut
                bool success2 = _authService.LoginWithUsername("admin");

                if (success2)
                {
                    var user = _authService.CurrentUser;
                    var role = _authService.GetCurrentUserRole();

                    // Ouvrir directement la fenêtre principale
                    var mainWindow = new MainWindow(_authService);
                    mainWindow.Show();
                    this.Close();
                    return;
                }

                // Fallback: essayer avec le username Windows de l'utilisateur actuel
                string windowsUsername = WindowsIdentity.GetCurrent().Name;
                if (windowsUsername.Contains("\\"))
                {
                    windowsUsername = windowsUsername.Split('\\')[1];
                }

                bool success3 = _authService.LoginWithUsername(windowsUsername);
                if (success3)
                {
                    var user = _authService.CurrentUser;
                    var role = _authService.GetCurrentUserRole();

                    // Ouvrir directement la fenêtre principale
                    var mainWindow = new MainWindow(_authService);
                    mainWindow.Show();
                    this.Close();
                    return;
                }

                // Si aucune connexion automatique ne fonctionne, afficher le message
                Dispatcher.Invoke(() =>
                {
                    TxtUsername.Text = $"Connexion automatique impossible";
                    TxtStatut.Text = "Veuillez saisir votre identifiant ci-dessous";
                    TxtErreur.Text = $"Le compte 'admin' et votre compte Windows '{windowsUsername}' n'ont pas été trouvés.\nVeuillez entrer votre identifiant manuellement.";
                    TxtErreur.Visibility = Visibility.Visible;
                    BtnConnecter.IsEnabled = true;
                });
            }
            catch (Exception ex)
            {
                // En cas d'erreur, afficher un message et laisser l'utilisateur se connecter manuellement
                Dispatcher.Invoke(() =>
                {
                    TxtUsername.Text = "Erreur de connexion automatique";
                    TxtStatut.Text = "Veuillez vous connecter manuellement";
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
                TxtUsernameInput.Text = "admin";
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

using System;
using System.Security.Principal;
using System.Windows;
using BacklogManager.Services;

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
            
            // Initialiser les données par défaut
            _initService.InitializeDefaultData();
            
            // Afficher le username
            AfficherUsername();
        }

        private void AfficherUsername()
        {
            try
            {
                string windowsUsername = WindowsIdentity.GetCurrent().Name;
                if (windowsUsername.Contains("\\"))
                {
                    windowsUsername = windowsUsername.Split('\\')[1];
                }
                TxtUsername.Text = string.Format("Connecté en tant que: {0}", windowsUsername);
                TxtStatut.Text = "Entrez votre identifiant ci-dessus";
            }
            catch (Exception ex)
            {
                TxtUsername.Text = "Impossible de récupérer le username Windows";
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

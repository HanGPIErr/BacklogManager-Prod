using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BacklogManager.Services;
using BacklogManager.Views.Pages;

namespace BacklogManager.Views
{
    public partial class AdministrationView : UserControl
    {
        private readonly IDatabase _database;
        private readonly AuditLogService _auditLogService;
        private readonly AuthenticationService _authService;

        public AdministrationView(IDatabase database, AuditLogService auditLogService = null, AuthenticationService authService = null)
        {
            InitializeComponent();
            _database = database;
            _auditLogService = auditLogService;
            _authService = authService;

            // Initialiser les textes traduits
            InitialiserTextes();

            // Charger les pages dans les frames
            ChargerPages();
        }

        private void InitialiserTextes()
        {
            // Header principal
            TxtAdministrationTitle.Text = LocalizationService.Instance.GetString("Administration_Title");
            TxtAdministrationDescription.Text = LocalizationService.Instance.GetString("Administration_Description");

            // Onglets principaux
            TxtUsersAndRolesTab.Text = LocalizationService.Instance.GetString("Administration_UsersAndRoles");
            TxtProjectsAndTeamTab.Text = LocalizationService.Instance.GetString("Administration_ProjectsAndTeam");
            TxtReportingTab.Text = LocalizationService.Instance.GetString("Administration_Reporting");
            TxtChatHistoryTab.Text = LocalizationService.Instance.GetString("Administration_ChatHistory");
            TxtAuditLogTab.Text = LocalizationService.Instance.GetString("Administration_AuditLog");

            // Sous-onglets Utilisateurs & Rôles
            TxtUsersSubTab.Text = LocalizationService.Instance.GetString("Administration_Users");
            TxtRolesSubTab.Text = LocalizationService.Instance.GetString("Administration_Roles");

            // Sous-onglets Projets & Équipe
            TxtProgramsSubTab.Text = LocalizationService.Instance.GetString("Administration_Programs");
            TxtProjectsSubTab.Text = LocalizationService.Instance.GetString("Administration_Projects");
            TxtTeamsSubTab.Text = LocalizationService.Instance.GetString("Administration_Teams");

            // Bouton Historique Chat
            TxtOpenChatHistory.Text = LocalizationService.Instance.GetString("Administration_OpenChatHistory");

            // S'abonner aux changements de langue
            LocalizationService.Instance.PropertyChanged += (s, e) =>
            {
                TxtAdministrationTitle.Text = LocalizationService.Instance.GetString("Administration_Title");
                TxtAdministrationDescription.Text = LocalizationService.Instance.GetString("Administration_Description");
                TxtUsersAndRolesTab.Text = LocalizationService.Instance.GetString("Administration_UsersAndRoles");
                TxtProjectsAndTeamTab.Text = LocalizationService.Instance.GetString("Administration_ProjectsAndTeam");
                TxtReportingTab.Text = LocalizationService.Instance.GetString("Administration_Reporting");
                TxtChatHistoryTab.Text = LocalizationService.Instance.GetString("Administration_ChatHistory");
                TxtAuditLogTab.Text = LocalizationService.Instance.GetString("Administration_AuditLog");
                TxtUsersSubTab.Text = LocalizationService.Instance.GetString("Administration_Users");
                TxtRolesSubTab.Text = LocalizationService.Instance.GetString("Administration_Roles");
                TxtProgramsSubTab.Text = LocalizationService.Instance.GetString("Administration_Programs");
                TxtProjectsSubTab.Text = LocalizationService.Instance.GetString("Administration_Projects");
                TxtTeamsSubTab.Text = LocalizationService.Instance.GetString("Administration_Teams");
                TxtOpenChatHistory.Text = LocalizationService.Instance.GetString("Administration_OpenChatHistory");
            };
        }

        private void ChargerPages()
        {
            try
            {
                // Charger la première page de chaque groupe par défaut
                if (FrameUtilisateursRoles != null)
                {
                    FrameUtilisateursRoles.Content = new GestionUtilisateursPage(_database, _auditLogService);
                }
                
                if (FrameProjetsEquipe != null)
                {
                    FrameProjetsEquipe.Content = new GestionProgrammesPage(_database);
                }
                
                if (FrameAudit != null)
                {
                    FrameAudit.Content = new AuditLogPage(_database, _auditLogService);
                }
                
                // Charger la page de Reporting seulement si authService est disponible
                if (FrameReporting != null && _authService != null)
                {
                    try
                    {
                        FrameReporting.Content = new AdminReportingView(_database, _authService);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Erreur lors du chargement du Reporting: {ex.Message}\n\nStackTrace: {ex.StackTrace}", 
                            "Erreur Reporting", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des pages: {ex.Message}\n\nStackTrace: {ex.StackTrace}", 
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Gestion des sous-onglets Utilisateurs & Rôles
        private void BtnSousOngletUtilisateurs_Click(object sender, RoutedEventArgs e)
        {
            // Mettre à jour les styles des boutons
            BtnSousOngletUtilisateurs.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00915A"));
            BtnSousOngletUtilisateurs.Foreground = Brushes.White;
            BtnSousOngletUtilisateurs.FontWeight = FontWeights.SemiBold;

            BtnSousOngletRoles.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0"));
            BtnSousOngletRoles.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6D6D6D"));
            BtnSousOngletRoles.FontWeight = FontWeights.Normal;

            // Charger le contenu
            FrameUtilisateursRoles.Content = new GestionUtilisateursPage(_database, _auditLogService);
        }

        private void BtnSousOngletRoles_Click(object sender, RoutedEventArgs e)
        {
            // Mettre à jour les styles des boutons
            BtnSousOngletRoles.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00915A"));
            BtnSousOngletRoles.Foreground = Brushes.White;
            BtnSousOngletRoles.FontWeight = FontWeights.SemiBold;

            BtnSousOngletUtilisateurs.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0"));
            BtnSousOngletUtilisateurs.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6D6D6D"));
            BtnSousOngletUtilisateurs.FontWeight = FontWeights.Normal;

            // Charger le contenu
            FrameUtilisateursRoles.Content = new GestionRolesPage(_database, _auditLogService);
        }

        // Gestion des sous-onglets Projets & Équipe
        private void BtnSousOngletProgrammes_Click(object sender, RoutedEventArgs e)
        {
            // Mettre à jour les styles des boutons
            BtnSousOngletProgrammes.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00915A"));
            BtnSousOngletProgrammes.Foreground = Brushes.White;
            BtnSousOngletProgrammes.FontWeight = FontWeights.SemiBold;

            BtnSousOngletProjets.Background = Brushes.Transparent;
            BtnSousOngletProjets.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6D6D6D"));
            BtnSousOngletProjets.FontWeight = FontWeights.Normal;

            BtnSousOngletEquipes.Background = Brushes.Transparent;
            BtnSousOngletEquipes.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6D6D6D"));
            BtnSousOngletEquipes.FontWeight = FontWeights.Normal;

            // Charger le contenu
            FrameProjetsEquipe.Content = new GestionProgrammesPage(_database);
        }

        private void BtnSousOngletProjets_Click(object sender, RoutedEventArgs e)
        {
            // Mettre à jour les styles des boutons
            BtnSousOngletProjets.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00915A"));
            BtnSousOngletProjets.Foreground = Brushes.White;
            BtnSousOngletProjets.FontWeight = FontWeights.SemiBold;

            BtnSousOngletProgrammes.Background = Brushes.Transparent;
            BtnSousOngletProgrammes.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6D6D6D"));
            BtnSousOngletProgrammes.FontWeight = FontWeights.Normal;

            BtnSousOngletEquipes.Background = Brushes.Transparent;
            BtnSousOngletEquipes.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6D6D6D"));
            BtnSousOngletEquipes.FontWeight = FontWeights.Normal;

            // Charger le contenu
            FrameProjetsEquipe.Content = new GestionProjetsPage(_database, _auditLogService);
        }

        private void BtnSousOngletEquipes_Click(object sender, RoutedEventArgs e)
        {
            // Mettre à jour les styles des boutons
            BtnSousOngletEquipes.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00915A"));
            BtnSousOngletEquipes.Foreground = Brushes.White;
            BtnSousOngletEquipes.FontWeight = FontWeights.SemiBold;

            BtnSousOngletProgrammes.Background = Brushes.Transparent;
            BtnSousOngletProgrammes.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6D6D6D"));
            BtnSousOngletProgrammes.FontWeight = FontWeights.Normal;

            BtnSousOngletProjets.Background = Brushes.Transparent;
            BtnSousOngletProjets.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6D6D6D"));
            BtnSousOngletProjets.FontWeight = FontWeights.Normal;

            // Charger le contenu avec la nouvelle page de gestion des équipes
            FrameProjetsEquipe.Content = new GestionEquipesPage(_database);
        }

        private void BtnOuvrirHistoriqueChat_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var chatHistoryService = new ChatHistoryService(_database);
                var historiqueWindow = new ChatHistoriqueAdminWindow(chatHistoryService)
                {
                    Owner = Window.GetWindow(this)
                };
                historiqueWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ouverture de l'historique des conversations : {ex.Message}", 
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

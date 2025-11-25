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

        public AdministrationView(IDatabase database, AuditLogService auditLogService = null)
        {
            InitializeComponent();
            _database = database;
            _auditLogService = auditLogService;

            // Charger les pages dans les frames
            ChargerPages();
        }

        private void ChargerPages()
        {
            try
            {
                // Charger la première page de chaque groupe par défaut
                FrameUtilisateursRoles.Content = new GestionUtilisateursPage(_database, _auditLogService);
                FrameProjetsEquipe.Content = new GestionProjetsPage(_database, _auditLogService);
                FrameAudit.Content = new AuditLogPage(_database, _auditLogService);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des pages: {ex.Message}", 
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
        private void BtnSousOngletProjets_Click(object sender, RoutedEventArgs e)
        {
            // Mettre à jour les styles des boutons
            BtnSousOngletProjets.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00915A"));
            BtnSousOngletProjets.Foreground = Brushes.White;
            BtnSousOngletProjets.FontWeight = FontWeights.SemiBold;

            BtnSousOngletDevs.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0"));
            BtnSousOngletDevs.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6D6D6D"));
            BtnSousOngletDevs.FontWeight = FontWeights.Normal;

            // Charger le contenu
            FrameProjetsEquipe.Content = new GestionProjetsPage(_database, _auditLogService);
        }

        private void BtnSousOngletDevs_Click(object sender, RoutedEventArgs e)
        {
            // Mettre à jour les styles des boutons
            BtnSousOngletDevs.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00915A"));
            BtnSousOngletDevs.Foreground = Brushes.White;
            BtnSousOngletDevs.FontWeight = FontWeights.SemiBold;

            BtnSousOngletProjets.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0"));
            BtnSousOngletProjets.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6D6D6D"));
            BtnSousOngletProjets.FontWeight = FontWeights.Normal;

            // Charger le contenu (utiliser GestionEquipePage à la place)
            FrameProjetsEquipe.Content = new GestionEquipePage(_database);
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

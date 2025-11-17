using System;
using System.Windows;
using BacklogManager.Services;
using BacklogManager.Views.Pages;

namespace BacklogManager.Views
{
    public partial class AdministrationWindow : Window
    {
        private readonly IDatabase _database;
        private readonly AuditLogService _auditLogService;

        public AdministrationWindow(IDatabase database, AuditLogService auditLogService = null)
        {
            InitializeComponent();
            _database = database;
            _auditLogService = auditLogService;

            // Charger les pages dans les frames
            ChargerPages();
            
            // Charger les statistiques
            ChargerStatistiques();
        }

        private void ChargerPages()
        {
            try
            {
                // Charger la première page de chaque groupe par défaut
                FrameUtilisateursRoles.Content = new GestionUtilisateursPage(_database, _auditLogService);
                FrameProjetsEquipe.Content = new GestionProjetsPage(_database, _auditLogService);
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
            BtnSousOngletUtilisateurs.Background = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#00915A"));
            BtnSousOngletUtilisateurs.Foreground = System.Windows.Media.Brushes.White;
            BtnSousOngletUtilisateurs.FontWeight = FontWeights.SemiBold;

            BtnSousOngletRoles.Background = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#E0E0E0"));
            BtnSousOngletRoles.Foreground = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#6D6D6D"));
            BtnSousOngletRoles.FontWeight = FontWeights.Normal;

            // Charger le contenu
            FrameUtilisateursRoles.Content = new GestionUtilisateursPage(_database, _auditLogService);
        }

        private void BtnSousOngletRoles_Click(object sender, RoutedEventArgs e)
        {
            // Mettre à jour les styles des boutons
            BtnSousOngletRoles.Background = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#00915A"));
            BtnSousOngletRoles.Foreground = System.Windows.Media.Brushes.White;
            BtnSousOngletRoles.FontWeight = FontWeights.SemiBold;

            BtnSousOngletUtilisateurs.Background = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#E0E0E0"));
            BtnSousOngletUtilisateurs.Foreground = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#6D6D6D"));
            BtnSousOngletUtilisateurs.FontWeight = FontWeights.Normal;

            // Charger le contenu
            FrameUtilisateursRoles.Content = new GestionRolesPage(_database, _auditLogService);
        }

        // Gestion des sous-onglets Projets & Équipe
        private void BtnSousOngletProjets_Click(object sender, RoutedEventArgs e)
        {
            // Mettre à jour les styles des boutons
            BtnSousOngletProjets.Background = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#00915A"));
            BtnSousOngletProjets.Foreground = System.Windows.Media.Brushes.White;
            BtnSousOngletProjets.FontWeight = FontWeights.SemiBold;

            BtnSousOngletEquipe.Background = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#E0E0E0"));
            BtnSousOngletEquipe.Foreground = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#6D6D6D"));
            BtnSousOngletEquipe.FontWeight = FontWeights.Normal;

            // Charger le contenu
            FrameProjetsEquipe.Content = new GestionProjetsPage(_database, _auditLogService);
        }

        private void BtnSousOngletEquipe_Click(object sender, RoutedEventArgs e)
        {
            // Mettre à jour les styles des boutons
            BtnSousOngletEquipe.Background = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#00915A"));
            BtnSousOngletEquipe.Foreground = System.Windows.Media.Brushes.White;
            BtnSousOngletEquipe.FontWeight = FontWeights.SemiBold;

            BtnSousOngletProjets.Background = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#E0E0E0"));
            BtnSousOngletProjets.Foreground = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#6D6D6D"));
            BtnSousOngletProjets.FontWeight = FontWeights.Normal;

            // Charger le contenu
            FrameProjetsEquipe.Content = new GestionEquipePage(_database);
        }

        private void ChargerStatistiques()
        {
            try
            {
                var items = _database.GetBacklogItems();
                var projets = _database.GetProjets();
                var devs = _database.GetDevs();
                var utilisateurs = _database.GetUtilisateurs();
                var roles = _database.GetRoles();

                int aFaire = 0;
                int enCours = 0;
                int terminees = 0;
                
                foreach (var item in items)
                {
                    if (item.Statut.ToString() == "AFaire") aFaire++;
                    else if (item.Statut.ToString() == "EnCours") enCours++;
                    else if (item.Statut.ToString() == "Termine") terminees++;
                }
                
                // Compter utilisateurs par rôle
                var usersByRole = new System.Collections.Generic.Dictionary<string, int>();
                foreach (var role in roles)
                {
                    var count = utilisateurs.FindAll(u => u.RoleId == role.Id && u.Actif).Count;
                    if (count > 0)
                    {
                        usersByRole[role.Nom] = count;
                    }
                }
                
                double progression = CalculerProgressionMoyenne(items);

                var stats = "📊 Vue d'ensemble:\n" +
                    $"   • {items.Count} tâches au total\n" +
                    $"   • {aFaire} à faire\n" +
                    $"   • {enCours} en cours\n" +
                    $"   • {terminees} terminées\n\n" +
                    "📁 Projets:\n" +
                    $"   • {projets.Count} projets actifs\n\n" +
                    "👥 Équipe:\n" +
                    $"   • {utilisateurs.FindAll(u => u.Actif).Count} utilisateurs actifs\n" +
                    $"   • {devs.Count} développeurs\n";

                // Ajouter le détail par rôle
                foreach (var kvp in usersByRole)
                {
                    stats += $"   • {kvp.Value} {kvp.Key}(s)\n";
                }
                
                // Calcul des statistiques avancées
                double chargePrevu = 0;
                double chargeReelle = 0;
                int tachesAvecChiffrage = 0;
                
                foreach (var item in items)
                {
                    if (item.ChiffrageHeures.HasValue)
                    {
                        chargePrevu += item.ChiffrageHeures.Value;
                        chargeReelle += item.TempsReelHeures ?? 0;
                        tachesAvecChiffrage++;
                    }
                }
                
                stats += $"\n📊 Charge de travail:\n" +
                         $"   • {chargePrevu / 7:F1} jours estimés\n" +
                         $"   • {chargeReelle:F1} heures réalisées\n" +
                         $"   • {tachesAvecChiffrage} tâches chiffrées\n";
                
                stats += $"\n📈 Progression moyenne: {progression:F1}%";

                TxtStatistiques.Text = stats;
            }
            catch (Exception ex)
            {
                TxtStatistiques.Text = "❌ Erreur: " + ex.Message;
            }
        }

        private double CalculerProgressionMoyenne(System.Collections.Generic.List<Domain.BacklogItem> items)
        {
            if (items.Count == 0) return 0;
            
            // Calculer progression basée sur temps réel vs charge prévue (comme Kanban)
            double chargePrevu = 0;
            double chargeReelle = 0;
            
            foreach (var item in items)
            {
                if (item.ChiffrageHeures.HasValue)
                {
                    chargePrevu += item.ChiffrageHeures.Value;
                    chargeReelle += item.TempsReelHeures ?? 0;
                }
            }
            
            if (chargePrevu == 0) return 0;
            return Math.Min(100, (chargeReelle / chargePrevu) * 100);
        }

        private void BtnOuvrirAuditLog_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var auditLogWindow = new AuditLogWindow(_database);
                auditLogWindow.Owner = this;
                auditLogWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ouverture du journal d'audit :\n{ex.Message}\n\nStackTrace:\n{ex.StackTrace}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

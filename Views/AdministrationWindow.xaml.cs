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
                FrameUtilisateurs.Content = new GestionUtilisateursPage(_database, _auditLogService);
                FrameRoles.Content = new GestionRolesPage(_database, _auditLogService);
                FrameProjets.Content = new GestionProjetsPage(_database, _auditLogService);
                FrameEquipe.Content = new GestionEquipePage(_database);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des pages: {ex.Message}", 
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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

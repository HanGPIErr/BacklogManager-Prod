using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BacklogManager.Services;

namespace BacklogManager.Views.Pages
{
    public partial class GestionRolesPage : Page
    {
        private readonly IDatabase _database;
        private readonly AuditLogService _auditLogService;

        public GestionRolesPage(IDatabase database, AuditLogService auditLogService = null)
        {
            InitializeComponent();
            _database = database;
            _auditLogService = auditLogService;
            ChargerRoles();
        }

        private void ChargerRoles()
        {
            try
            {
                var roles = _database.GetRoles();
                LstRoles.ItemsSource = roles;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des rôles: {ex.Message}", 
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnEnregistrerRole_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                var role = button?.Tag as Domain.Role;
                
                if (role == null) return;

                // Récupérer l'ancien état avant modification
                var oldRole = _database.GetRoles().FirstOrDefault(r => r.Id == role.Id);
                string oldValue = oldRole != null ? 
                    $"PeutCreerDemandes: {oldRole.PeutCreerDemandes}, PeutChiffrer: {oldRole.PeutChiffrer}, " +
                    $"PeutPrioriser: {oldRole.PeutPrioriser}, PeutGererUtilisateurs: {oldRole.PeutGererUtilisateurs}, " +
                    $"PeutVoirKPI: {oldRole.PeutVoirKPI}, PeutGererReferentiels: {oldRole.PeutGererReferentiels}, " +
                    $"PeutModifierTaches: {oldRole.PeutModifierTaches}, PeutSupprimerTaches: {oldRole.PeutSupprimerTaches}" 
                    : "N/A";

                _database.UpdateRole(role);

                // Audit log après modification
                if (_auditLogService != null)
                {
                    string newValue = $"PeutCreerDemandes: {role.PeutCreerDemandes}, PeutChiffrer: {role.PeutChiffrer}, " +
                        $"PeutPrioriser: {role.PeutPrioriser}, PeutGererUtilisateurs: {role.PeutGererUtilisateurs}, " +
                        $"PeutVoirKPI: {role.PeutVoirKPI}, PeutGererReferentiels: {role.PeutGererReferentiels}, " +
                        $"PeutModifierTaches: {role.PeutModifierTaches}, PeutSupprimerTaches: {role.PeutSupprimerTaches}";

                    _auditLogService.LogUpdate("Role", role.Id, $"Rôle: {role.Nom}", oldValue, newValue);
                }

                MessageBox.Show($"Les permissions du rôle '{role.Nom}' ont été mises à jour avec succès.", 
                    "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                
                ChargerRoles();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'enregistrement: {ex.Message}", 
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnActualiser_Click(object sender, RoutedEventArgs e)
        {
            ChargerRoles();
        }
    }
}

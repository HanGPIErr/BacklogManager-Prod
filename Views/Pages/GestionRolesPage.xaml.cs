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
            
            // Initialiser les textes traduits
            InitialiserTextes();
            
            ChargerRoles();
        }

        private void InitialiserTextes()
        {
            // Textes de l'interface
            TxtConfigurationTitle.Text = "🎭 " + LocalizationService.Instance.GetString("Roles_ConfigurationTitle");
            BtnActualiser.Content = "🔄 " + LocalizationService.Instance.GetString("Roles_Refresh");
            BtnResetAdmin.Content = "⚡ " + LocalizationService.Instance.GetString("Roles_ResetAdminPermissions");
            BtnResetAdmin.ToolTip = LocalizationService.Instance.GetString("Roles_ResetAdminTooltip");

            // S'abonner aux changements de langue
            LocalizationService.Instance.PropertyChanged += (s, e) =>
            {
                TxtConfigurationTitle.Text = "🎭 " + LocalizationService.Instance.GetString("Roles_ConfigurationTitle");
                BtnActualiser.Content = "🔄 " + LocalizationService.Instance.GetString("Roles_Refresh");
                BtnResetAdmin.Content = "⚡ " + LocalizationService.Instance.GetString("Roles_ResetAdminPermissions");
                BtnResetAdmin.ToolTip = LocalizationService.Instance.GetString("Roles_ResetAdminTooltip");
            };
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
                MessageBox.Show(string.Format(LocalizationService.Instance.GetString("Roles_ErrorLoading"), ex.Message), 
                    LocalizationService.Instance.GetString("Common_Error"), MessageBoxButton.OK, MessageBoxImage.Error);
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

                MessageBox.Show(string.Format(LocalizationService.Instance.GetString("Roles_PermissionsUpdated"), role.Nom), 
                    LocalizationService.Instance.GetString("Common_Success"), MessageBoxButton.OK, MessageBoxImage.Information);
                
                ChargerRoles();
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(LocalizationService.Instance.GetString("Roles_ErrorSaving"), ex.Message), 
                    LocalizationService.Instance.GetString("Common_Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnActualiser_Click(object sender, RoutedEventArgs e)
        {
            ChargerRoles();
        }

        private void BtnResetAdmin_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show(
                    LocalizationService.Instance.GetString("Roles_ResetConfirmMessage") + "\n\n" +
                    LocalizationService.Instance.GetString("Roles_ResetPermissionsList"),
                    LocalizationService.Instance.GetString("Roles_ResetConfirmTitle"),
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    var roles = _database.GetRoles();
                    var adminRole = roles.FirstOrDefault(r => r.Type == Domain.RoleType.Administrateur);

                    if (adminRole != null)
                    {
                        // Activer toutes les permissions
                        adminRole.PeutCreerDemandes = true;
                        adminRole.PeutChiffrer = true;
                        adminRole.PeutPrioriser = true;
                        adminRole.PeutModifierTaches = true;
                        adminRole.PeutSupprimerTaches = true;
                        adminRole.PeutGererUtilisateurs = true;
                        adminRole.PeutGererReferentiels = true;
                        adminRole.PeutVoirKPI = true;

                        _database.UpdateRole(adminRole);

                        if (_auditLogService != null)
                        {
                            _auditLogService.LogUpdate("Role", adminRole.Id, 
                                "Réinitialisation permissions Administrateur", 
                                "Anciennes permissions", 
                                "Toutes les permissions activées");
                        }

                        MessageBox.Show(
                            LocalizationService.Instance.GetString("Roles_AdminPermissionsReset"),
                            LocalizationService.Instance.GetString("Common_Success"),
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);

                        ChargerRoles();
                    }
                    else
                    {
                        MessageBox.Show("Rôle Administrateur introuvable dans la base de données.",
                            "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Erreur lors de la réinitialisation : {0}", ex.Message),
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}


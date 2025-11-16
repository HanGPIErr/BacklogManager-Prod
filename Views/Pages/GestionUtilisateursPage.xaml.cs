using System;
using System.Windows;
using System.Windows.Controls;
using BacklogManager.Domain;
using BacklogManager.Services;

namespace BacklogManager.Views.Pages
{
    public partial class GestionUtilisateursPage : Page
    {
        private readonly IDatabase _database;
        private readonly AuditLogService _auditLogService;

        public GestionUtilisateursPage(IDatabase database, AuditLogService auditLogService = null)
        {
            InitializeComponent();
            _database = database;
            _auditLogService = auditLogService;
            ChargerUtilisateurs();
        }

        private void ChargerUtilisateurs()
        {
            try
            {
                var utilisateurs = _database.GetUtilisateurs();
                LstUtilisateurs.ItemsSource = utilisateurs;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des utilisateurs: {ex.Message}", 
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnNouvelUtilisateur_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var window = new EditUtilisateurWindow(_database, null, _auditLogService);
                if (window.ShowDialog() == true)
                {
                    ChargerUtilisateurs();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ouverture de la fenêtre: {ex.Message}\n\nDétails: {ex.StackTrace}", 
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnModifierUtilisateur_Click(object sender, RoutedEventArgs e)
        {
            if (LstUtilisateurs.SelectedItem == null)
            {
                MessageBox.Show("Veuillez sélectionner un utilisateur à modifier.", 
                    "Attention", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var utilisateur = (Utilisateur)LstUtilisateurs.SelectedItem;
                var window = new EditUtilisateurWindow(_database, utilisateur, _auditLogService);
                if (window.ShowDialog() == true)
                {
                    ChargerUtilisateurs();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ouverture de la fenêtre: {ex.Message}\n\nDétails: {ex.StackTrace}", 
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnSupprimerUtilisateur_Click(object sender, RoutedEventArgs e)
        {
            if (LstUtilisateurs.SelectedItem == null)
            {
                MessageBox.Show("Veuillez sélectionner un utilisateur à supprimer.", 
                    "Attention", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var utilisateur = (Utilisateur)LstUtilisateurs.SelectedItem;
                
                var result = MessageBox.Show(
                    $"Êtes-vous sûr de vouloir supprimer l'utilisateur '{utilisateur.Nom}' ({utilisateur.UsernameWindows}) ?\n\n" +
                    "Cette action est irréversible.\n\n" +
                    "Les tâches assignées à cet utilisateur ne seront pas supprimées.",
                    "Confirmer la suppression",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    // Log avant suppression
                    if (_auditLogService != null)
                    {
                        try
                        {
                            var oldValue = $"Id: {utilisateur.Id}, Nom: {utilisateur.Nom} {utilisateur.Prenom}, Username: {utilisateur.UsernameWindows}, Role: {utilisateur.RoleId}";
                            _auditLogService.LogDelete("Utilisateur", utilisateur.Id, oldValue, $"Suppression de l'utilisateur {utilisateur.UsernameWindows}");
                        }
                        catch { }
                    }

                    _database.DeleteUtilisateur(utilisateur.Id);
                    MessageBox.Show("Utilisateur supprimé avec succès.", "Succès", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    ChargerUtilisateurs();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la suppression: {ex.Message}\n\nDétails: {ex.StackTrace}", 
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnActualiser_Click(object sender, RoutedEventArgs e)
        {
            ChargerUtilisateurs();
        }
    }
}

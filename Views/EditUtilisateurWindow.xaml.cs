using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using BacklogManager.Domain;
using BacklogManager.Services;

namespace BacklogManager.Views
{
    public partial class EditUtilisateurWindow : Window
    {
        private readonly IDatabase _database;
        private readonly Utilisateur _utilisateur;
        private readonly bool _isNewUser;
        private readonly AuditLogService _auditLogService;

        public EditUtilisateurWindow(IDatabase database, Utilisateur utilisateur = null, AuditLogService auditLogService = null)
        {
            InitializeComponent();
            _database = database;
            _utilisateur = utilisateur ?? new Utilisateur { Actif = true };
            _isNewUser = utilisateur == null;
            _auditLogService = auditLogService;

            ChargerRoles();
            RemplirFormulaire();

            Title = _isNewUser ? "Nouvel Utilisateur" : "Modifier Utilisateur";
        }

        private void ChargerRoles()
        {
            try
            {
                var roles = _database.GetRoles();
                CboRole.ItemsSource = roles;
                
                if (_utilisateur.RoleId > 0)
                {
                    CboRole.SelectedValue = _utilisateur.RoleId;
                }
                else if (roles.Any())
                {
                    CboRole.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des rôles: {ex.Message}", 
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RemplirFormulaire()
        {
            if (!_isNewUser)
            {
                TxtNom.Text = _utilisateur.Nom ?? "";
                TxtPrenom.Text = _utilisateur.Prenom ?? "";
                TxtEmail.Text = _utilisateur.Email ?? "";
                TxtUsernameWindows.Text = _utilisateur.UsernameWindows ?? "";
                ChkActif.IsChecked = _utilisateur.Actif;
                CboRole.SelectedValue = _utilisateur.RoleId;
            }
        }

        private bool ValiderFormulaire()
        {
            TxtErreur.Text = "";
            BrdErreur.Visibility = Visibility.Collapsed;

            // Validation Nom
            if (string.IsNullOrWhiteSpace(TxtNom.Text))
            {
                TxtErreur.Text = "Le nom est obligatoire.";
                BrdErreur.Visibility = Visibility.Visible;
                TxtNom.Focus();
                return false;
            }

            // Validation Username Windows (matricule)
            if (string.IsNullOrWhiteSpace(TxtUsernameWindows.Text))
            {
                TxtErreur.Text = "Le username Windows (matricule) est obligatoire.";
                BrdErreur.Visibility = Visibility.Visible;
                TxtUsernameWindows.Focus();
                return false;
            }

            // Vérifier l'unicité du username
            var utilisateurs = _database.GetUtilisateurs();
            var existingUser = utilisateurs.FirstOrDefault(u => 
                u.UsernameWindows.Equals(TxtUsernameWindows.Text, StringComparison.OrdinalIgnoreCase) && 
                u.Id != _utilisateur.Id);
            
            if (existingUser != null)
            {
                TxtErreur.Text = "Ce username Windows existe déjà dans le système.";
                BrdErreur.Visibility = Visibility.Visible;
                TxtUsernameWindows.Focus();
                return false;
            }

            // Validation Email (si renseigné)
            if (!string.IsNullOrWhiteSpace(TxtEmail.Text))
            {
                var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
                if (!emailRegex.IsMatch(TxtEmail.Text))
                {
                    TxtErreur.Text = "Le format de l'email est invalide.";
                    BrdErreur.Visibility = Visibility.Visible;
                    TxtEmail.Focus();
                    return false;
                }
            }

            // Validation Rôle
            if (CboRole.SelectedValue == null)
            {
                TxtErreur.Text = "Veuillez sélectionner un rôle.";
                BrdErreur.Visibility = Visibility.Visible;
                CboRole.Focus();
                return false;
            }

            return true;
        }

        private void BtnEnregistrer_Click(object sender, RoutedEventArgs e)
        {
            if (!ValiderFormulaire())
                return;

            try
            {
                // Capturer l'état avant modification
                string oldValue = null;
                if (!_isNewUser && _auditLogService != null)
                {
                    oldValue = $"Nom: {_utilisateur.Nom} {_utilisateur.Prenom}, Email: {_utilisateur.Email}, Role: {_utilisateur.RoleId}, Actif: {_utilisateur.Actif}";
                }

                _utilisateur.Nom = TxtNom.Text.Trim();
                _utilisateur.Prenom = TxtPrenom.Text.Trim();
                _utilisateur.Email = TxtEmail.Text.Trim();
                _utilisateur.UsernameWindows = TxtUsernameWindows.Text.Trim();
                _utilisateur.RoleId = (int)CboRole.SelectedValue;
                _utilisateur.Actif = ChkActif.IsChecked ?? true;

                if (_isNewUser)
                {
                    _utilisateur.DateCreation = DateTime.Now;
                    _database.AddUtilisateur(_utilisateur);
                    
                    // Log création
                    if (_auditLogService != null)
                    {
                        try
                        {
                            var newValue = $"Nom: {_utilisateur.Nom} {_utilisateur.Prenom}, Email: {_utilisateur.Email}, Role: {_utilisateur.RoleId}";
                            _auditLogService.LogCreate("Utilisateur", _utilisateur.Id, newValue, $"Création de l'utilisateur {_utilisateur.UsernameWindows}");
                        }
                        catch { }
                    }

                    MessageBox.Show("Utilisateur créé avec succès.", "Succès", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    _database.UpdateUtilisateur(_utilisateur);
                    
                    // Log mise à jour
                    if (_auditLogService != null && oldValue != null)
                    {
                        try
                        {
                            var newValue = $"Nom: {_utilisateur.Nom} {_utilisateur.Prenom}, Email: {_utilisateur.Email}, Role: {_utilisateur.RoleId}, Actif: {_utilisateur.Actif}";
                            _auditLogService.LogUpdate("Utilisateur", _utilisateur.Id, oldValue, newValue, $"Modification de l'utilisateur {_utilisateur.UsernameWindows}");
                        }
                        catch { }
                    }

                    MessageBox.Show("Utilisateur modifié avec succès.", "Succès", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'enregistrement: {ex.Message}\n\nDétails: {ex.StackTrace}", 
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnAnnuler_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}

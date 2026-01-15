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

            InitialiserTextes();
            ChargerRoles();
            RemplirFormulaire();

            Title = _isNewUser ? LocalizationService.Instance.GetString("UserEdit_NewUser") : LocalizationService.Instance.GetString("UserEdit_EditUser");
        }

        private void InitialiserTextes()
        {
            var loc = LocalizationService.Instance;

            // Titre principal
            TxtTitle.Text = loc.GetString("UserEdit_UserInformation");

            // Labels des champs
            TxtLabelLastName.Text = loc.GetString("UserEdit_LastName");
            TxtLabelFirstName.Text = loc.GetString("UserEdit_FirstName");
            TxtLabelEmail.Text = loc.GetString("UserEdit_Email");
            TxtLabelUsername.Text = loc.GetString("UserEdit_Username");
            TxtHintUsername.Text = loc.GetString("UserEdit_UsernameHint");
            TxtLabelRole.Text = loc.GetString("UserEdit_Role");
            TxtLabelTeam.Text = loc.GetString("UserEdit_Team");
            TxtHintTeam.Text = loc.GetString("UserEdit_TeamHint");
            TxtLabelStatus.Text = loc.GetString("UserEdit_Status");
            TxtHintStatus.Text = loc.GetString("UserEdit_StatusHint");
            ChkActif.Content = loc.GetString("UserEdit_ActiveUser");

            // Boutons
            BtnSave.Content = loc.GetString("UserEdit_Save");
            BtnCancel.Content = loc.GetString("UserEdit_Cancel");

            // Écouter les changements de langue
            loc.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "Item[]")
                {
                    InitialiserTextes();
                    Title = _isNewUser ? loc.GetString("UserEdit_NewUser") : loc.GetString("UserEdit_EditUser");
                }
            };
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
                
                // Charger les équipes
                var equipes = _database.GetAllEquipes().Where(e => e.Actif).OrderBy(e => e.Nom).ToList();
                // Ajouter une option "Aucune équipe" au début
                equipes.Insert(0, new Equipe { Id = 0, Nom = LocalizationService.Instance.GetString("UserEdit_NoTeam"), Code = "" });
                CboEquipe.ItemsSource = equipes;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{LocalizationService.Instance.GetString("UserEdit_ErrorLoadingRoles")}: {ex.Message}", 
                    LocalizationService.Instance.GetString("Common_Error"), MessageBoxButton.OK, MessageBoxImage.Error);
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
                CboEquipe.SelectedValue = _utilisateur.EquipeId.HasValue ? _utilisateur.EquipeId.Value : 0;
                
                // Sélectionner le statut
                foreach (System.Windows.Controls.ComboBoxItem item in CboStatut.Items)
                {
                    if (item.Tag.ToString() == _utilisateur.Statut)
                    {
                        CboStatut.SelectedItem = item;
                        break;
                    }
                }
            }
            else
            {
                // Pour un nouvel utilisateur, sélectionner "Aucune équipe" par défaut
                CboEquipe.SelectedValue = 0;
                // Sélectionner BAU par défaut
                CboStatut.SelectedIndex = 0;
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

            // Validation Statut
            if (CboStatut.SelectedItem == null)
            {
                TxtErreur.Text = "Veuillez sélectionner un statut.";
                BrdErreur.Visibility = Visibility.Visible;
                CboStatut.Focus();
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

                var equipeId = CboEquipe.SelectedValue != null && (int)CboEquipe.SelectedValue > 0 
                    ? (int?)CboEquipe.SelectedValue 
                    : null;

                _utilisateur.Nom = TxtNom.Text.Trim();
                _utilisateur.Prenom = TxtPrenom.Text.Trim();
                _utilisateur.Email = TxtEmail.Text.Trim();
                _utilisateur.UsernameWindows = TxtUsernameWindows.Text.Trim();
                _utilisateur.RoleId = (int)CboRole.SelectedValue;
                _utilisateur.EquipeId = equipeId;
                _utilisateur.Statut = ((System.Windows.Controls.ComboBoxItem)CboStatut.SelectedItem).Tag.ToString();
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

                    MessageBox.Show(LocalizationService.Instance.GetString("UserEdit_SuccessCreated"), 
                        LocalizationService.Instance.GetString("Common_Success"), 
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

                    MessageBox.Show(LocalizationService.Instance.GetString("UserEdit_SuccessUpdated"), 
                        LocalizationService.Instance.GetString("Common_Success"), 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{LocalizationService.Instance.GetString("UserEdit_ErrorSaving")}: {ex.Message}\n\n{LocalizationService.Instance.GetString("Common_Details")}: {ex.StackTrace}", 
                    LocalizationService.Instance.GetString("Common_Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnAnnuler_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}

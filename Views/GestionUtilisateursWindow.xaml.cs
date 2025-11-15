using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BacklogManager.Domain;
using BacklogManager.Services;

namespace BacklogManager.Views
{
    public class UtilisateurViewModel
    {
        public int Id { get; set; }
        public string UsernameWindows { get; set; }
        public string Nom { get; set; }
        public string Prenom { get; set; }
        public string Email { get; set; }
        public int RoleId { get; set; }
        public string RoleNom { get; set; }
        public bool Actif { get; set; }
        public string NomComplet { get { return string.Format("{0} {1}", Prenom, Nom); } }
    }

    public partial class GestionUtilisateursWindow : Window
    {
        private readonly IDatabase _database;
        private Utilisateur _utilisateurEnEdition;
        private Role _roleEnEdition;

        public GestionUtilisateursWindow(IDatabase database)
        {
            InitializeComponent();
            _database = database;
            ChargerDonnees();
        }

        private void ChargerDonnees()
        {
            ChargerUtilisateurs();
            ChargerRoles();
            ChargerRolesCombo();
        }

        // ===== UTILISATEURS =====

        private void ChargerUtilisateurs()
        {
            var utilisateurs = _database.GetUtilisateurs();
            var roles = _database.GetRoles();

            var viewModels = utilisateurs.Select(u => new UtilisateurViewModel
            {
                Id = u.Id,
                UsernameWindows = u.UsernameWindows,
                Nom = u.Nom,
                Prenom = u.Prenom,
                Email = u.Email,
                RoleId = u.RoleId,
                RoleNom = roles.FirstOrDefault(r => r.Id == u.RoleId)?.Nom ?? "N/A",
                Actif = u.Actif
            }).ToList();

            DgUtilisateurs.ItemsSource = viewModels;
        }

        private void ChargerRolesCombo()
        {
            var roles = _database.GetRoles().Where(r => r.Actif).ToList();
            CmbRole.ItemsSource = roles;
        }

        private void BtnAjouterUser_Click(object sender, RoutedEventArgs e)
        {
            if (!ValiderFormulaireUtilisateur()) return;

            var utilisateur = new Utilisateur
            {
                UsernameWindows = TxtUsername.Text.Trim(),
                Nom = TxtNom.Text.Trim(),
                Prenom = TxtPrenom.Text.Trim(),
                Email = TxtEmail.Text.Trim(),
                RoleId = (int)CmbRole.SelectedValue,
                Actif = ChkActif.IsChecked ?? true,
                DateCreation = DateTime.Now
            };

            _database.AddOrUpdateUtilisateur(utilisateur);
            ViderFormulaireUtilisateur();
            ChargerUtilisateurs();

            MessageBox.Show(string.Format("Utilisateur {0} {1} ajouté avec succès.", utilisateur.Prenom, utilisateur.Nom), 
                "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnModifierUser_Click(object sender, RoutedEventArgs e)
        {
            if (_utilisateurEnEdition == null) return;
            if (!ValiderFormulaireUtilisateur()) return;

            _utilisateurEnEdition.UsernameWindows = TxtUsername.Text.Trim();
            _utilisateurEnEdition.Nom = TxtNom.Text.Trim();
            _utilisateurEnEdition.Prenom = TxtPrenom.Text.Trim();
            _utilisateurEnEdition.Email = TxtEmail.Text.Trim();
            _utilisateurEnEdition.RoleId = (int)CmbRole.SelectedValue;
            _utilisateurEnEdition.Actif = ChkActif.IsChecked ?? true;

            _database.AddOrUpdateUtilisateur(_utilisateurEnEdition);
            AnnulerEditionUtilisateur();
            ChargerUtilisateurs();

            MessageBox.Show("Utilisateur modifié avec succès!", 
                "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnAnnulerUser_Click(object sender, RoutedEventArgs e)
        {
            AnnulerEditionUtilisateur();
        }

        private void BtnEditUser_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var vm = button?.Tag as UtilisateurViewModel;
            if (vm == null) return;

            var utilisateurs = _database.GetUtilisateurs();
            _utilisateurEnEdition = utilisateurs.FirstOrDefault(u => u.Id == vm.Id);
            if (_utilisateurEnEdition == null) return;

            TxtUsername.Text = _utilisateurEnEdition.UsernameWindows;
            TxtNom.Text = _utilisateurEnEdition.Nom;
            TxtPrenom.Text = _utilisateurEnEdition.Prenom;
            TxtEmail.Text = _utilisateurEnEdition.Email;
            CmbRole.SelectedValue = _utilisateurEnEdition.RoleId;
            ChkActif.IsChecked = _utilisateurEnEdition.Actif;

            BtnAjouterUser.Visibility = Visibility.Collapsed;
            BtnModifierUser.Visibility = Visibility.Visible;
            BtnAnnulerUser.Visibility = Visibility.Visible;
        }

        private void BtnDeleteUser_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var vm = button?.Tag as UtilisateurViewModel;
            if (vm == null) return;

            var result = MessageBox.Show(
                string.Format("Voulez-vous vraiment désactiver l'utilisateur {0} ?", vm.NomComplet),
                "Confirmer la désactivation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                var utilisateurs = _database.GetUtilisateurs();
                var utilisateur = utilisateurs.FirstOrDefault(u => u.Id == vm.Id);
                if (utilisateur != null)
                {
                    utilisateur.Actif = false;
                    _database.AddOrUpdateUtilisateur(utilisateur);
                    ChargerUtilisateurs();
                }
            }
        }

        private void DgUtilisateurs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Optionnel
        }

        private bool ValiderFormulaireUtilisateur()
        {
            if (string.IsNullOrWhiteSpace(TxtUsername.Text))
            {
                MessageBox.Show("Le username Windows est obligatoire.", "Validation", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(TxtNom.Text))
            {
                MessageBox.Show("Le nom est obligatoire.", "Validation", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(TxtEmail.Text))
            {
                MessageBox.Show("L'email est obligatoire.", "Validation", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (CmbRole.SelectedValue == null)
            {
                MessageBox.Show("Le rôle est obligatoire.", "Validation", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private void ViderFormulaireUtilisateur()
        {
            TxtUsername.Clear();
            TxtNom.Clear();
            TxtPrenom.Clear();
            TxtEmail.Clear();
            CmbRole.SelectedIndex = -1;
            ChkActif.IsChecked = true;
            AnnulerEditionUtilisateur();
        }

        private void AnnulerEditionUtilisateur()
        {
            _utilisateurEnEdition = null;
            BtnAjouterUser.Visibility = Visibility.Visible;
            BtnModifierUser.Visibility = Visibility.Collapsed;
            BtnAnnulerUser.Visibility = Visibility.Collapsed;
        }

        // ===== RÔLES =====

        private void ChargerRoles()
        {
            var roles = _database.GetRoles();
            DgRoles.ItemsSource = roles;
        }

        private void BtnAjouterRole_Click(object sender, RoutedEventArgs e)
        {
            if (!ValiderFormulaireRole()) return;

            var role = new Role
            {
                Nom = TxtNomRole.Text.Trim(),
                Type = (RoleType)CmbTypeRole.SelectedItem,
                PeutCreerDemandes = ChkCreerDemandes.IsChecked ?? false,
                PeutChiffrer = ChkChiffrer.IsChecked ?? false,
                PeutPrioriser = ChkPrioriser.IsChecked ?? false,
                PeutGererUtilisateurs = ChkGererUtilisateurs.IsChecked ?? false,
                PeutVoirKPI = ChkVoirKPI.IsChecked ?? false,
                PeutGererReferentiels = ChkGererReferentiels.IsChecked ?? false,
                Actif = true
            };

            _database.AddOrUpdateRole(role);
            ViderFormulaireRole();
            ChargerRoles();
            ChargerRolesCombo();

            MessageBox.Show(string.Format("Rôle {0} ajouté avec succès.", role.Nom), 
                "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnModifierRole_Click(object sender, RoutedEventArgs e)
        {
            if (_roleEnEdition == null) return;
            if (!ValiderFormulaireRole()) return;

            _roleEnEdition.Nom = TxtNomRole.Text.Trim();
            _roleEnEdition.Type = (RoleType)CmbTypeRole.SelectedItem;
            _roleEnEdition.PeutCreerDemandes = ChkCreerDemandes.IsChecked ?? false;
            _roleEnEdition.PeutChiffrer = ChkChiffrer.IsChecked ?? false;
            _roleEnEdition.PeutPrioriser = ChkPrioriser.IsChecked ?? false;
            _roleEnEdition.PeutGererUtilisateurs = ChkGererUtilisateurs.IsChecked ?? false;
            _roleEnEdition.PeutVoirKPI = ChkVoirKPI.IsChecked ?? false;
            _roleEnEdition.PeutGererReferentiels = ChkGererReferentiels.IsChecked ?? false;

            _database.AddOrUpdateRole(_roleEnEdition);
            ViderFormulaireRole();
            ChargerRoles();
            ChargerRolesCombo();

            MessageBox.Show("Rôle modifié avec succès!", 
                "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void DgRoles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DgRoles.SelectedItem is Role role)
            {
                _roleEnEdition = role;
                TxtNomRole.Text = role.Nom;
                CmbTypeRole.SelectedItem = role.Type;
                ChkCreerDemandes.IsChecked = role.PeutCreerDemandes;
                ChkChiffrer.IsChecked = role.PeutChiffrer;
                ChkPrioriser.IsChecked = role.PeutPrioriser;
                ChkGererUtilisateurs.IsChecked = role.PeutGererUtilisateurs;
                ChkVoirKPI.IsChecked = role.PeutVoirKPI;
                ChkGererReferentiels.IsChecked = role.PeutGererReferentiels;

                BtnAjouterRole.Visibility = Visibility.Collapsed;
                BtnModifierRole.Visibility = Visibility.Visible;
            }
        }

        private bool ValiderFormulaireRole()
        {
            if (string.IsNullOrWhiteSpace(TxtNomRole.Text))
            {
                MessageBox.Show("Le nom du rôle est obligatoire.", "Validation", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (CmbTypeRole.SelectedItem == null)
            {
                MessageBox.Show("Le type de rôle est obligatoire.", "Validation", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private void ViderFormulaireRole()
        {
            TxtNomRole.Clear();
            CmbTypeRole.SelectedIndex = -1;
            ChkCreerDemandes.IsChecked = false;
            ChkChiffrer.IsChecked = false;
            ChkPrioriser.IsChecked = false;
            ChkGererUtilisateurs.IsChecked = false;
            ChkVoirKPI.IsChecked = false;
            ChkGererReferentiels.IsChecked = false;
            
            _roleEnEdition = null;
            BtnAjouterRole.Visibility = Visibility.Visible;
            BtnModifierRole.Visibility = Visibility.Collapsed;
        }
    }
}

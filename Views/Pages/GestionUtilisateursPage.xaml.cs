using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BacklogManager.Domain;
using BacklogManager.Services;

namespace BacklogManager.Views.Pages
{
    public class UtilisateurListViewModel
    {
        public Utilisateur Utilisateur { get; set; }
        public int Id => Utilisateur.Id;
        public string Nom => $"{Utilisateur.Prenom} {Utilisateur.Nom}";
        public string UsernameWindows => Utilisateur.UsernameWindows;
        public int RoleId => Utilisateur.RoleId;
        public string RoleNom { get; set; }
        public string Statut => Utilisateur.Statut;
        public bool Actif => Utilisateur.Actif;
        public string ActifTexte => Actif ? LocalizationService.Instance.GetString("Users_Yes") : LocalizationService.Instance.GetString("Users_No");
        
        public string StatutCouleurBadge
        {
            get
            {
                switch (Utilisateur?.Statut)
                {
                    case "BAU":
                        return "#1976D2"; // Bleu
                    case "PROJECTS":
                        return "#7B1FA2"; // Violet
                    case "Temporary":
                        return "#F57C00"; // Orange
                    case "Hiring ongoing":
                        return "#616161"; // Gris
                    default:
                        return "#757575"; // Gris par défaut
                }
            }
        }
    }

    public partial class GestionUtilisateursPage : Page, System.ComponentModel.INotifyPropertyChanged
    {
        private readonly IDatabase _database;
        private readonly AuditLogService _auditLogService;
        private List<UtilisateurListViewModel> _tousLesUtilisateurs;
        private List<UtilisateurListViewModel> _utilisateursFiltres;
        private int _pageActuelle = 1;
        private const int UTILISATEURS_PAR_PAGE = 25;

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        // Propriétés pour les textes bindés
        public string NewUserText => LocalizationService.Instance.GetString("Users_NewUser");
        public string ModifyText => LocalizationService.Instance.GetString("Users_Modify");
        public string DeleteText => LocalizationService.Instance.GetString("Users_Delete");
        public string RefreshText => LocalizationService.Instance.GetString("Users_Refresh");
        public string SearchPlaceholderText => LocalizationService.Instance.GetString("Users_SearchPlaceholder");

        public GestionUtilisateursPage(IDatabase database, AuditLogService auditLogService = null)
        {
            InitializeComponent();
            _database = database;
            _auditLogService = auditLogService;
            
            // Définir le DataContext pour les bindings
            this.DataContext = this;
            
            // Initialiser les textes traduits
            InitialiserTextes();
            
            ChargerUtilisateurs();
        }

        private void InitialiserTextes()
        {
            // En-têtes de colonnes
            ColFullName.Header = LocalizationService.Instance.GetString("Users_FullName");
            ColWindowsUsername.Header = LocalizationService.Instance.GetString("Users_WindowsUsername");
            ColRole.Header = LocalizationService.Instance.GetString("Users_Role");
            ColStatus.Header = LocalizationService.Instance.GetString("Users_Status");
            ColActive.Header = LocalizationService.Instance.GetString("Users_Active");

            // Placeholder de recherche
            TxtSearchPlaceholder.Text = LocalizationService.Instance.GetString("Users_SearchPlaceholder");

            // Textes de pagination
            TxtDisplayingFrom.Text = LocalizationService.Instance.GetString("Users_DisplayingFrom") + " ";
            TxtTo.Text = " " + LocalizationService.Instance.GetString("Users_To") + " ";
            TxtOf.Text = " " + LocalizationService.Instance.GetString("Users_Of") + " ";
            TxtTotalUsersLabel.Text = " " + LocalizationService.Instance.GetString("Users_TotalUsers");
            TxtPageLabel.Text = LocalizationService.Instance.GetString("Users_Page") + " ";
            TxtPrevious.Text = LocalizationService.Instance.GetString("Users_Previous");
            TxtNext.Text = LocalizationService.Instance.GetString("Users_Next");

            // S'abonner aux changements de langue
            LocalizationService.Instance.PropertyChanged += (s, e) =>
            {
                ColFullName.Header = LocalizationService.Instance.GetString("Users_FullName");
                ColWindowsUsername.Header = LocalizationService.Instance.GetString("Users_WindowsUsername");
                ColRole.Header = LocalizationService.Instance.GetString("Users_Role");
                ColStatus.Header = LocalizationService.Instance.GetString("Users_Status");
                ColActive.Header = LocalizationService.Instance.GetString("Users_Active");
                TxtSearchPlaceholder.Text = LocalizationService.Instance.GetString("Users_SearchPlaceholder");
                TxtDisplayingFrom.Text = LocalizationService.Instance.GetString("Users_DisplayingFrom") + " ";
                TxtTo.Text = " " + LocalizationService.Instance.GetString("Users_To") + " ";
                TxtOf.Text = " " + LocalizationService.Instance.GetString("Users_Of") + " ";
                TxtTotalUsersLabel.Text = " " + LocalizationService.Instance.GetString("Users_TotalUsers");
                TxtPageLabel.Text = LocalizationService.Instance.GetString("Users_Page") + " ";
                TxtPrevious.Text = LocalizationService.Instance.GetString("Users_Previous");
                TxtNext.Text = LocalizationService.Instance.GetString("Users_Next");
                
                // Notifier les changements pour les propriétés bindées
                OnPropertyChanged(nameof(NewUserText));
                OnPropertyChanged(nameof(ModifyText));
                OnPropertyChanged(nameof(DeleteText));
                OnPropertyChanged(nameof(RefreshText));
                OnPropertyChanged(nameof(SearchPlaceholderText));
            };
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }

        private void ChargerUtilisateurs()
        {
            try
            {
                var utilisateurs = _database.GetUtilisateurs();
                var roles = _database.GetRoles();
                
                _tousLesUtilisateurs = utilisateurs.Select(u => new UtilisateurListViewModel
                {
                    Utilisateur = u,
                    RoleNom = roles.FirstOrDefault(r => r.Id == u.RoleId)?.Nom ?? "Inconnu"
                }).ToList();
                
                _pageActuelle = 1;
                AfficherPage();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des utilisateurs: {ex.Message}", 
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AfficherPage()
        {
            // Utiliser la liste filtrée si elle existe, sinon la liste complète
            var listeAffichee = _utilisateursFiltres ?? _tousLesUtilisateurs;
            
            if (listeAffichee == null || !listeAffichee.Any())
            {
                LstUtilisateurs.ItemsSource = null;
                TxtTotalUtilisateurs.Text = "0";
                TxtDebutPage.Text = "0";
                TxtFinPage.Text = "0";
                TxtNumeroPage.Text = "0";
                TxtNombrePages.Text = "0";
                BtnPagePrecedente.IsEnabled = false;
                BtnPageSuivante.IsEnabled = false;
                return;
            }

            var totalUtilisateurs = listeAffichee.Count;
            var nombrePages = (int)Math.Ceiling(totalUtilisateurs / (double)UTILISATEURS_PAR_PAGE);
            
            // S'assurer que la page actuelle est valide
            if (_pageActuelle > nombrePages)
                _pageActuelle = nombrePages;
            if (_pageActuelle < 1)
                _pageActuelle = 1;

            var debut = (_pageActuelle - 1) * UTILISATEURS_PAR_PAGE;
            var fin = Math.Min(debut + UTILISATEURS_PAR_PAGE, totalUtilisateurs);
            
            var utilisateursPage = listeAffichee
                .Skip(debut)
                .Take(UTILISATEURS_PAR_PAGE)
                .ToList();
            
            LstUtilisateurs.ItemsSource = utilisateursPage;
            
            // Mettre à jour les infos de pagination
            TxtTotalUtilisateurs.Text = totalUtilisateurs.ToString();
            TxtDebutPage.Text = (debut + 1).ToString();
            TxtFinPage.Text = fin.ToString();
            TxtNumeroPage.Text = _pageActuelle.ToString();
            TxtNombrePages.Text = nombrePages.ToString();
            
            // Activer/désactiver les boutons
            BtnPagePrecedente.IsEnabled = _pageActuelle > 1;
            BtnPageSuivante.IsEnabled = _pageActuelle < nombrePages;
        }

        private void BtnPagePrecedente_Click(object sender, RoutedEventArgs e)
        {
            if (_pageActuelle > 1)
            {
                _pageActuelle--;
                AfficherPage();
            }
        }

        private void BtnPageSuivante_Click(object sender, RoutedEventArgs e)
        {
            var listeAffichee = _utilisateursFiltres ?? _tousLesUtilisateurs;
            var nombrePages = (int)Math.Ceiling(listeAffichee.Count / (double)UTILISATEURS_PAR_PAGE);
            if (_pageActuelle < nombrePages)
            {
                _pageActuelle++;
                AfficherPage();
            }
        }

        private void TxtRecherche_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_tousLesUtilisateurs == null || !_tousLesUtilisateurs.Any())
                return;

            string searchTerm = TxtRecherche.Text.ToLower().Trim();
            
            if (string.IsNullOrEmpty(searchTerm))
            {
                // Afficher tous les utilisateurs
                _utilisateursFiltres = null;
            }
            else
            {
                // Filtrer les utilisateurs
                _utilisateursFiltres = _tousLesUtilisateurs.Where(u =>
                    u.Nom.ToLower().Contains(searchTerm) ||
                    u.UsernameWindows.ToLower().Contains(searchTerm) ||
                    u.RoleNom.ToLower().Contains(searchTerm) ||
                    u.Statut.ToLower().Contains(searchTerm)
                ).ToList();
            }
            
            // Revenir à la première page et rafraîchir
            _pageActuelle = 1;
            AfficherPage();
        }

        private void LstUtilisateurs_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (LstUtilisateurs.SelectedItem is UtilisateurListViewModel selected)
            {
                BtnModifierUtilisateur_Click(sender, e);
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
                var viewModel = (UtilisateurListViewModel)LstUtilisateurs.SelectedItem;
                var utilisateur = viewModel.Utilisateur;
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
                var viewModel = (UtilisateurListViewModel)LstUtilisateurs.SelectedItem;
                var utilisateur = viewModel.Utilisateur;
                
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using BacklogManager.Domain;
using BacklogManager.Services;

namespace BacklogManager.Views
{
    public partial class DemandesView : UserControl
    {
        private readonly IDatabase _database;
        private readonly AuthenticationService _authService;
        private readonly PermissionService _permissionService;
        private List<DemandeViewModel> _toutesLesDemandes;

        public DemandesView(AuthenticationService authService, PermissionService permissionService)
        {
            InitializeComponent();
            _database = new SqliteDatabase();
            _authService = authService;
            _permissionService = permissionService;
            
            InitialiserFiltres();
            ChargerDemandes();
            
            BtnNouvelleDemande.Click += BtnNouvelleDemande_Click;
            BtnRefresh.Click += (s, e) => ChargerDemandes();
            CmbFiltreStatut.SelectionChanged += (s, e) => AppliquerFiltres();
            CmbFiltreCriticite.SelectionChanged += (s, e) => AppliquerFiltres();
            
            // Vérifier permissions
            VerifierPermissions();
        }

        private void InitialiserFiltres()
        {
            // Filtre statut - Utiliser les valeurs formatées
            var statutItems = new List<string> { "Tous" };
            foreach (StatutDemande statut in Enum.GetValues(typeof(StatutDemande)))
            {
                statutItems.Add(FormatStatut(statut));
            }
            CmbFiltreStatut.ItemsSource = statutItems;
            CmbFiltreStatut.SelectedIndex = 0;
            
            // Filtre criticité
            var criticiteItems = new List<string> { "Toutes" };
            criticiteItems.AddRange(Enum.GetNames(typeof(Criticite)));
            CmbFiltreCriticite.ItemsSource = criticiteItems;
            CmbFiltreCriticite.SelectedIndex = 0;
            
            // Configurer les DatePickers
            DpDateDebut.SelectedDateChanged += (s, e) => AppliquerFiltres();
            DpDateFin.SelectedDateChanged += (s, e) => AppliquerFiltres();
            BtnEffacerFiltres.Click += (s, e) => EffacerFiltres();
        }
        
        private void EffacerFiltres()
        {
            CmbFiltreStatut.SelectedIndex = 0;
            CmbFiltreCriticite.SelectedIndex = 0;
            DpDateDebut.SelectedDate = null;
            DpDateFin.SelectedDate = null;
            AppliquerFiltres();
        }

        private void VerifierPermissions()
        {
            try
            {
                // Debug: Afficher les informations de l'utilisateur et du rôle
                var currentUser = _authService.CurrentUser;
                var currentRole = _authService.GetCurrentUserRole();
                
                string logPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "permissions_log.txt");
                using (var writer = new System.IO.StreamWriter(logPath, true))
                {
                    writer.WriteLine("=== VERIFICATION PERMISSIONS DEMANDES " + DateTime.Now.ToString("HH:mm:ss") + " ===");
                    writer.WriteLine(string.Format("User: {0} (ID: {1})", 
                        currentUser?.Prenom ?? "NULL", 
                        currentUser?.Id.ToString() ?? "NULL"));
                    writer.WriteLine(string.Format("Role: {0} (ID: {1}, Type: {2})", 
                        currentRole?.Nom ?? "NULL",
                        currentRole?.Id.ToString() ?? "NULL",
                        currentRole?.Type.ToString() ?? "NULL"));
                    writer.WriteLine(string.Format("IsAdmin: {0}, IsChefDeProjet: {1}, IsBusinessAnalyst: {2}", 
                        _permissionService.IsAdmin, 
                        _permissionService.IsChefDeProjet, 
                        _permissionService.IsBusinessAnalyst));
                    writer.WriteLine(string.Format("PeutCreerDemandes (DB): {0}", 
                        _permissionService.PeutCreerDemandes));
                    
                    // Vérification directe du type de rôle
                    bool isAdminDirect = currentRole != null && currentRole.Type == Domain.RoleType.Administrateur;
                    bool isChefProjetDirect = currentRole != null && currentRole.Type == Domain.RoleType.ChefDeProjet;
                    bool isBADirect = currentRole != null && currentRole.Type == Domain.RoleType.BusinessAnalyst;
                    
                    writer.WriteLine(string.Format("Vérification directe - Admin: {0}, Chef: {1}, BA: {2}", 
                        isAdminDirect, isChefProjetDirect, isBADirect));
                    
                    // FORCE: Si l'utilisateur est Admin, Chef de projet ou BA, activer le bouton
                    bool peutCreer = isAdminDirect || isChefProjetDirect || isBADirect || _permissionService.PeutCreerDemandes;
                    BtnNouvelleDemande.IsEnabled = peutCreer;
                    
                    writer.WriteLine(string.Format("BtnNouvelleDemande.IsEnabled: {0}", peutCreer));
                    writer.WriteLine("==========================================");
                    
                    // Configurer le DataContext pour les bindings
                    bool peutSupprimer = isAdminDirect || isChefProjetDirect;
                    this.DataContext = new
                    {
                        PeutSupprimerDemandes = peutSupprimer
                    };
                    
                    writer.WriteLine(string.Format("PeutSupprimerDemandes: {0}", peutSupprimer));
                }
                
                System.Diagnostics.Debug.WriteLine("=== VERIFICATION PERMISSIONS DEMANDES ===");
                System.Diagnostics.Debug.WriteLine(string.Format("User: {0} (ID: {1})", 
                    currentUser?.Prenom ?? "NULL", 
                    currentUser?.Id.ToString() ?? "NULL"));
                System.Diagnostics.Debug.WriteLine(string.Format("Role: {0} (ID: {1}, Type: {2})", 
                    currentRole?.Nom ?? "NULL",
                    currentRole?.Id.ToString() ?? "NULL",
                    currentRole?.Type.ToString() ?? "NULL"));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("ERREUR VerifierPermissions: {0}", ex.Message));
                MessageBox.Show(string.Format("Erreur lors de la vérification des permissions: {0}", ex.Message),
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ChargerDemandes()
        {
            try
            {
                var demandes = _database.GetDemandes();
                var utilisateurs = _database.GetUtilisateurs();

                _toutesLesDemandes = demandes.Select(d => new DemandeViewModel
                {
                    Id = d.Id,
                    Titre = d.Titre,
                    Description = d.Description,
                    Type = d.Type.ToString(),
                    Statut = FormatStatut(d.Statut),
                    Criticite = d.Criticite.ToString(),
                    Demandeur = ObtenirNomUtilisateur(d.DemandeurId, utilisateurs),
                    BusinessAnalyst = ObtenirNomUtilisateur(d.BusinessAnalystId, utilisateurs),
                    ChefProjet = ObtenirNomUtilisateur(d.ChefProjetId, utilisateurs),
                    DateCreation = d.DateCreation
                }).OrderByDescending(d => d.DateCreation).ToList();

                AppliquerFiltres();
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Erreur lors du chargement des demandes : {0}", ex.Message), 
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AppliquerFiltres()
        {
            if (_toutesLesDemandes == null) return;

            var filtered = _toutesLesDemandes.AsEnumerable();

            // Filtre statut - Comparer avec le statut formaté
            if (CmbFiltreStatut.SelectedIndex > 0)
            {
                var statutSelectionne = CmbFiltreStatut.SelectedItem.ToString();
                filtered = filtered.Where(d => d.Statut == statutSelectionne);
            }

            // Filtre criticité
            if (CmbFiltreCriticite.SelectedIndex > 0)
            {
                var criticiteSelectionnee = CmbFiltreCriticite.SelectedItem.ToString();
                filtered = filtered.Where(d => d.Criticite == criticiteSelectionnee);
            }
            
            // Filtre date début
            if (DpDateDebut.SelectedDate.HasValue)
            {
                var dateDebut = DpDateDebut.SelectedDate.Value.Date;
                filtered = filtered.Where(d => d.DateCreation.Date >= dateDebut);
            }
            
            // Filtre date fin
            if (DpDateFin.SelectedDate.HasValue)
            {
                var dateFin = DpDateFin.SelectedDate.Value.Date;
                filtered = filtered.Where(d => d.DateCreation.Date <= dateFin);
            }

            ListeDemandes.ItemsSource = filtered.ToList();
        }

        private string ObtenirNomUtilisateur(int? userId, List<Utilisateur> utilisateurs)
        {
            if (!userId.HasValue) return "Non assigné";
            var user = utilisateurs.FirstOrDefault(u => u.Id == userId.Value);
            return user != null ? string.Format("{0} {1}", user.Prenom, user.Nom) : "Non assigné";
        }

        private string FormatStatut(StatutDemande statut)
        {
            switch (statut)
            {
                case StatutDemande.EnAttenteSpecification:
                    return "En attente spécification";
                case StatutDemande.EnAttenteChiffrage:
                    return "En attente chiffrage";
                case StatutDemande.EnAttenteArbitrage:
                    return "En attente arbitrage";
                case StatutDemande.Acceptee:
                    return "Acceptée";
                case StatutDemande.PlanifieeEnUS:
                    return "Planifiée en US";
                case StatutDemande.EnCours:
                    return "En cours";
                case StatutDemande.Livree:
                    return "Livrée";
                case StatutDemande.Refusee:
                    return "Refusée";
                default:
                    return statut.ToString();
            }
        }

        private void BtnNouvelleDemande_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var window = new EditionDemandeWindow(_database, _authService);
                window.Owner = Window.GetWindow(this);
                if (window.ShowDialog() == true)
                {
                    ChargerDemandes();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Erreur lors de la création de la demande : {0}", ex.Message), 
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnDetailsClick(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is int demandeId)
            {
                AfficherDetails(demandeId);
            }
        }

        private void BtnModifierClick(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is int demandeId)
            {
                ModifierDemande(demandeId);
            }
        }

        private void BtnCommentairesClick(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is int demandeId)
            {
                AfficherCommentaires(demandeId);
            }
        }

        private void AfficherDetails(int demandeId)
        {
            try
            {
                var window = new DetailsDemandeWindow(demandeId, _database);
                window.Owner = Window.GetWindow(this);
                window.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Erreur lors de l'affichage des détails : {0}", ex.Message), 
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ModifierDemande(int demandeId)
        {
            try
            {
                // Récupérer la demande pour vérifier les permissions
                var demandeDb = _database.GetDemandes().FirstOrDefault(d => d.Id == demandeId);
                if (demandeDb == null)
                {
                    MessageBox.Show("Demande introuvable.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Vérifier les permissions
                if (!_permissionService.PeutModifierDemande(demandeDb))
                {
                    MessageBox.Show("Vous n'avez pas les permissions pour modifier cette demande.",
                        "Accès refusé", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var window = new EditionDemandeWindow(_database, _authService, demandeId);
                window.Owner = Window.GetWindow(this);
                if (window.ShowDialog() == true)
                {
                    ChargerDemandes();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Erreur lors de la modification de la demande : {0}", ex.Message), 
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AfficherCommentaires(int demandeId)
        {
            try
            {
                var window = new CommentairesWindow(demandeId, _database, _authService);
                window.Owner = Window.GetWindow(this);
                window.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Erreur lors de l'affichage des commentaires : {0}", ex.Message), 
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnSupprimerClick(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is int demandeId)
            {
                SupprimerDemande(demandeId);
            }
        }

        private void SupprimerDemande(int demandeId)
        {
            try
            {
                // Vérifier les permissions
                if (!_permissionService.IsAdmin && !_permissionService.IsChefDeProjet)
                {
                    MessageBox.Show("Vous n'avez pas les permissions pour supprimer des demandes.",
                        "Accès refusé", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Récupérer la demande pour afficher ses détails
                var demande = _toutesLesDemandes.FirstOrDefault(d => d.Id == demandeId);
                if (demande == null) return;

                // Confirmation
                var result = MessageBox.Show(
                    string.Format("Êtes-vous sûr de vouloir supprimer cette demande ?\n\nTitre : {0}\nStatut : {1}\n\nCette action est irréversible.",
                        demande.Titre, demande.Statut),
                    "Confirmation de suppression",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning,
                    MessageBoxResult.No);

                if (result == MessageBoxResult.Yes)
                {
                    _database.DeleteDemande(demandeId);
                    MessageBox.Show("Demande supprimée avec succès.", "Suppression", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    ChargerDemandes();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Erreur lors de la suppression de la demande : {0}", ex.Message),
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public class DemandeViewModel
    {
        public int Id { get; set; }
        public string Titre { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
        public string Statut { get; set; }
        public string Criticite { get; set; }
        public string Demandeur { get; set; }
        public string BusinessAnalyst { get; set; }
        public string ChefProjet { get; set; }
        public DateTime DateCreation { get; set; }
    }

    // Convertisseur pour les couleurs de statut
    public class StatutToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string statut)
            {
                if (statut.Contains("Livrée")) return new SolidColorBrush(Color.FromRgb(0, 166, 81)); // Vert
                if (statut.Contains("En cours")) return new SolidColorBrush(Color.FromRgb(0, 120, 215)); // Bleu
                if (statut.Contains("Acceptée")) return new SolidColorBrush(Color.FromRgb(16, 124, 16)); // Vert foncé
                if (statut.Contains("Refusée")) return new SolidColorBrush(Color.FromRgb(232, 17, 35)); // Rouge
                if (statut.Contains("attente")) return new SolidColorBrush(Color.FromRgb(255, 140, 0)); // Orange
                return new SolidColorBrush(Color.FromRgb(102, 102, 102)); // Gris
            }
            return new SolidColorBrush(Color.FromRgb(102, 102, 102));
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // Convertisseur pour les couleurs de criticité
    public class CriticiteToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string criticite)
            {
                switch (criticite)
                {
                    case "Bloquante":
                        return new SolidColorBrush(Color.FromRgb(232, 17, 35)); // Rouge
                    case "Haute":
                        return new SolidColorBrush(Color.FromRgb(255, 140, 0)); // Orange
                    case "Moyenne":
                        return new SolidColorBrush(Color.FromRgb(255, 185, 0)); // Jaune foncé
                    case "Basse":
                        return new SolidColorBrush(Color.FromRgb(0, 120, 215)); // Bleu
                    default:
                        return new SolidColorBrush(Color.FromRgb(102, 102, 102)); // Gris
                }
            }
            return new SolidColorBrush(Color.FromRgb(102, 102, 102));
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

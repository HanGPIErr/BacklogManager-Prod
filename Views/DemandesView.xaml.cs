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
        private bool _afficherArchives = false;

        public DemandesView(IDatabase database, AuthenticationService authService, PermissionService permissionService)
        {
            InitializeComponent();
            _database = database;
            _authService = authService;
            _permissionService = permissionService;
            
            InitialiserFiltres();
            ChargerDemandes();
            
            BtnNouvelleDemande.Click += BtnNouvelleDemande_Click;
            BtnAnalyseEmail.Click += BtnAnalyseEmail_Click;
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
                    bool peutArchiver = isAdminDirect; // Seuls les admins peuvent archiver
                    this.DataContext = new
                    {
                        PeutSupprimerDemandes = peutSupprimer,
                        PeutArchiverDemandes = peutArchiver
                    };
                    
                    writer.WriteLine(string.Format("PeutSupprimerDemandes: {0}", peutSupprimer));
                    writer.WriteLine(string.Format("PeutArchiverDemandes: {0}", peutArchiver));
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
                var projets = _database.GetProjets();

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
                    ProjetNom = d.ProjetId.HasValue ? projets.FirstOrDefault(p => p.Id == d.ProjetId.Value)?.Nom ?? "-" : "-",
                    DateCreation = d.DateCreation,
                    EstAcceptee = d.Statut == StatutDemande.Acceptee,
                    EstArchivee = d.EstArchivee
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

            // Filtre archives/actives
            filtered = filtered.Where(d => d.EstArchivee == _afficherArchives);

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
                case StatutDemande.EnAttenteValidationManager:
                    return "En attente validation manager";
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

        private void BtnAnalyseEmail_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var window = new AnalyseEmailDemandeWindow(_database, _authService);
                window.Owner = Window.GetWindow(this);
                if (window.ShowDialog() == true)
                {
                    ChargerDemandes();
                    
                    // Afficher un message de succès supplémentaire
                    if (window.DemandeCreee != null)
                    {
                        MessageBox.Show(
                            $"La demande a été créée avec succès via l'analyse IA !\n\n" +
                            $"📝 Titre : {window.DemandeCreee.Titre}\n" +
                            $"🏷️ Type : {window.DemandeCreee.Type}\n" +
                            $"⚠️ Criticité : {window.DemandeCreee.Criticite}\n\n" +
                            $"Elle apparaît maintenant dans votre liste de demandes.",
                            "✅ Demande créée",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erreur lors de l'analyse de l'email :\n\n{ex.Message}",
                    "Erreur",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
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

        private void BtnCreerTacheClick(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is int demandeId)
            {
                CreerTacheDepuisDemande(demandeId);
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

        private void CreerTacheDepuisDemande(int demandeId)
        {
            try
            {
                var demande = _database.GetDemandes().FirstOrDefault(d => d.Id == demandeId);
                if (demande == null)
                {
                    MessageBox.Show("Demande introuvable.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Créer une nouvelle tâche pré-remplie avec les infos de la demande
                var nouvelleTache = new BacklogItem
                {
                    Titre = demande.Titre,
                    Description = demande.Description + "\n\n" + 
                                 (string.IsNullOrEmpty(demande.Specifications) ? "" : "Spécifications:\n" + demande.Specifications),
                    TypeDemande = demande.Type,
                    Priorite = demande.Criticite == Criticite.Bloquante ? Priorite.Haute :
                               demande.Criticite == Criticite.Haute ? Priorite.Haute :
                               demande.Criticite == Criticite.Moyenne ? Priorite.Moyenne : Priorite.Basse,
                    Statut = Statut.Afaire,
                    DateCreation = DateTime.Now,
                    ProjetId = demande.ProjetId,
                    DevAssigneId = demande.DevChiffreurId, // Récupérer le dev de la demande
                    DemandeId = demande.Id, // Lien vers la demande d'origine
                    DateFinAttendue = demande.DatePrevisionnelleImplementation,
                    ChiffrageHeures = demande.ChiffrageEstimeJours.HasValue ? demande.ChiffrageEstimeJours.Value * 8.0 : (double?)null
                };

                // Créer les services nécessaires
                var backlogService = new BacklogService(_database);

                // Ouvrir la fenêtre d'édition avec la tâche pré-remplie
                var window = new EditTacheWindow(nouvelleTache, backlogService, _permissionService)
                {
                    Owner = Window.GetWindow(this),
                    Title = "Créer une tâche depuis la demande"
                };

                if (window.ShowDialog() == true)
                {
                    MessageBox.Show(
                        "Tâche créée avec succès !\n\nLa tâche a été ajoutée au Kanban.",
                        "Succès",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Erreur lors de la création de la tâche : {0}", ex.Message),
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void BtnOngletActives_Click(object sender, RoutedEventArgs e)
        {
            _afficherArchives = false;
            
            // Changer les styles des boutons
            BtnOngletActives.Background = new SolidColorBrush(Color.FromRgb(0, 145, 90));
            BtnOngletActives.Foreground = Brushes.White;
            BtnOngletActives.FontWeight = FontWeights.Bold;
            
            BtnOngletArchives.Background = Brushes.Transparent;
            BtnOngletArchives.Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102));
            BtnOngletArchives.FontWeight = FontWeights.SemiBold;
            
            AppliquerFiltres();
        }
        
        private void BtnOngletArchives_Click(object sender, RoutedEventArgs e)
        {
            _afficherArchives = true;
            
            // Changer les styles des boutons
            BtnOngletArchives.Background = new SolidColorBrush(Color.FromRgb(0, 145, 90));
            BtnOngletArchives.Foreground = Brushes.White;
            BtnOngletArchives.FontWeight = FontWeights.Bold;
            
            BtnOngletActives.Background = Brushes.Transparent;
            BtnOngletActives.Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102));
            BtnOngletActives.FontWeight = FontWeights.SemiBold;
            
            AppliquerFiltres();
        }
        
        private void BtnArchiverClick(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is int demandeId)
            {
                ArchiverDemande(demandeId);
            }
        }
        
        private void ArchiverDemande(int demandeId)
        {
            try
            {
                // Vérifier les permissions
                if (!_permissionService.IsAdmin)
                {
                    MessageBox.Show("Seuls les administrateurs peuvent archiver des demandes.",
                        "Accès refusé", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Récupérer la demande
                var demande = _database.GetDemandes().FirstOrDefault(d => d.Id == demandeId);
                if (demande == null)
                {
                    MessageBox.Show("Demande introuvable.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                
                string action = demande.EstArchivee ? "désarchiver" : "archiver";
                string actionPasse = demande.EstArchivee ? "désarchivée" : "archivée";
                
                // Confirmation
                var result = MessageBox.Show(
                    string.Format("Êtes-vous sûr de vouloir {0} cette demande ?\n\nTitre : {1}\nStatut : {2}",
                        action, demande.Titre, FormatStatut(demande.Statut)),
                    "Confirmation",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question,
                    MessageBoxResult.No);

                if (result == MessageBoxResult.Yes)
                {
                    // Basculer le statut archivé
                    bool nouvelEtatArchive = !demande.EstArchivee;
                    demande.EstArchivee = nouvelEtatArchive;
                    _database.AddOrUpdateDemande(demande);
                    
                    MessageBox.Show(string.Format("Demande {0} avec succès.", actionPasse), 
                        "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    // Recharger et basculer automatiquement vers l'onglet approprié
                    ChargerDemandes();
                    
                    // Si on vient d'archiver, basculer vers les archives
                    if (nouvelEtatArchive)
                    {
                        BtnOngletArchives_Click(null, null);
                    }
                    else
                    {
                        // Si on vient de désarchiver, rester sur actives
                        BtnOngletActives_Click(null, null);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Erreur lors de l'archivage de la demande : {0}", ex.Message),
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
        public string ProjetNom { get; set; }
        public DateTime DateCreation { get; set; }
        public bool EstAcceptee { get; set; }
        public bool EstArchivee { get; set; }
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

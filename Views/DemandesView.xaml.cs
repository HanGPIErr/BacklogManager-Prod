using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
        private int _pageActuelle = 1;
        private const int PageSize = 15;

        public DemandesView(IDatabase database, AuthenticationService authService, PermissionService permissionService)
        {
            InitializeComponent();
            _database = database;
            _authService = authService;
            _permissionService = permissionService;
            
            // Configurer les événements AVANT InitialiserFiltres
            CmbFiltreStatut.SelectionChanged += (s, e) => { _pageActuelle = 1; AppliquerFiltres(); };
            CmbFiltreCriticite.SelectionChanged += (s, e) => { _pageActuelle = 1; AppliquerFiltres(); };
            CmbFiltreEquipe.SelectionChanged += (s, e) => { _pageActuelle = 1; AppliquerFiltres(); };
            DpDateDebut.SelectedDateChanged += (s, e) => { _pageActuelle = 1; AppliquerFiltres(); };
            DpDateFin.SelectedDateChanged += (s, e) => { _pageActuelle = 1; AppliquerFiltres(); };
            TxtRecherche.TextChanged += (s, e) => { _pageActuelle = 1; AppliquerFiltres(); };
            BtnEffacerFiltres.Click += (s, e) => EffacerFiltres();
            BtnNouvelleDemande.Click += BtnNouvelleDemande_Click;
            BtnAnalyseEmail.Click += BtnAnalyseEmail_Click;
            BtnRefresh.Click += (s, e) => ChargerDemandes();
            
            InitialiserTextes();
            InitialiserFiltres();
            ChargerDemandes();
            
            // S'abonner aux changements de langue
            LocalizationService.Instance.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "CurrentCulture")
                {
                    InitialiserTextes();
                    InitialiserFiltres();
                    ChargerDemandes();
                }
            };
            
            // Vérifier permissions
            VerifierPermissions();
        }
        
        private void InitialiserTextes()
        {
            // Header
            TxtTitre.Text = LocalizationService.Instance["Requests_Title"];
            TxtSousTitre.Text = LocalizationService.Instance["Requests_Subtitle"];
            
            // Boutons header
            BtnAnalyseEmail.Content = LocalizationService.Instance["Requests_CreateFromEmail"];
            BtnNouvelleDemande.Content = LocalizationService.Instance["Requests_NewRequest"];
            BtnOngletActives.Content = LocalizationService.Instance["Requests_ActiveTab"];
            BtnOngletArchives.Content = LocalizationService.Instance["Requests_ArchivesTab"];
            
            // Labels filtres
            TxtLabelStatut.Text = LocalizationService.Instance["Requests_FilterStatus"];
            TxtLabelCriticite.Text = LocalizationService.Instance["Requests_FilterCriticality"];
            TxtLabelEquipe.Text = LocalizationService.Instance["Requests_FilterTeam"];
            TxtLabelDateDebut.Text = LocalizationService.Instance["Requests_FilterDateFrom"];
            TxtLabelDateFin.Text = LocalizationService.Instance["Requests_FilterDateTo"];
            TxtLabelRecherche.Text = LocalizationService.Instance["Requests_FilterSearch"];
            TxtRecherche.Tag = LocalizationService.Instance["Requests_FilterSearchHint"];
            
            // Boutons filtres
            BtnRefresh.Content = LocalizationService.Instance["Requests_Refresh"];
            BtnEffacerFiltres.Content = LocalizationService.Instance["Requests_Reset"];
            
            // Watermarks des DatePickers
            SetDatePickerWatermark(DpDateDebut, LocalizationService.Instance["Requests_SelectDate"]);
            SetDatePickerWatermark(DpDateFin, LocalizationService.Instance["Requests_SelectDate"]);
        }
        
        private void SetDatePickerWatermark(DatePicker datePicker, string watermark)
        {
            datePicker.Loaded += (s, e) =>
            {
                var textBox = FindVisualChild<DatePickerTextBox>(datePicker);
                if (textBox != null)
                {
                    var watermarkProperty = textBox.GetType().GetProperty("Watermark", 
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                    if (watermarkProperty != null)
                    {
                        watermarkProperty.SetValue(textBox, watermark);
                    }
                }
            };
        }
        
        private T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                {
                    return typedChild;
                }
                
                var result = FindVisualChild<T>(child);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }

        private void InitialiserFiltres()
        {
            // Sauvegarder les sélections actuelles
            int selectedStatutIndex = CmbFiltreStatut?.SelectedIndex ?? 0;
            int selectedCriticiteIndex = CmbFiltreCriticite?.SelectedIndex ?? 0;
            int selectedEquipeIndex = CmbFiltreEquipe?.SelectedIndex ?? 0;
            
            // Filtre statut - Utiliser les valeurs formatées
            var statutItems = new List<string> { LocalizationService.Instance["Requests_FilterAll"] };
            foreach (StatutDemande statut in Enum.GetValues(typeof(StatutDemande)))
            {
                statutItems.Add(FormatStatut(statut));
            }
            CmbFiltreStatut.ItemsSource = statutItems;
            CmbFiltreStatut.SelectedIndex = Math.Min(selectedStatutIndex, statutItems.Count - 1);
            
            // Filtre criticité
            var criticiteItems = new List<string> { LocalizationService.Instance["Requests_FilterAllCriticality"] };
            foreach (Criticite criticite in Enum.GetValues(typeof(Criticite)))
            {
                criticiteItems.Add(TraduireCriticite(criticite));
            }
            CmbFiltreCriticite.ItemsSource = criticiteItems;
            CmbFiltreCriticite.SelectedIndex = Math.Min(selectedCriticiteIndex, criticiteItems.Count - 1);
            
            // Filtre équipe
            var equipes = _database.GetAllEquipes().Where(e => e.Actif).OrderBy(e => e.Nom).ToList();
            var equipesAvecTous = new List<Equipe> { new Equipe { Id = 0, Nom = LocalizationService.Instance["Requests_FilterAllTeams"] } };
            equipesAvecTous.AddRange(equipes);
            CmbFiltreEquipe.ItemsSource = equipesAvecTous;
            CmbFiltreEquipe.SelectedIndex = Math.Min(selectedEquipeIndex, equipesAvecTous.Count - 1);
        }
        
        private void EffacerFiltres()
        {
            CmbFiltreStatut.SelectedIndex = 0;
            CmbFiltreCriticite.SelectedIndex = 0;
            CmbFiltreEquipe.SelectedIndex = 0;
            DpDateDebut.SelectedDate = null;
            DpDateFin.SelectedDate = null;
            TxtRecherche.Text = string.Empty;
            _pageActuelle = 1;
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
                var equipes = _database.GetAllEquipes();

                _toutesLesDemandes = demandes.Select(d => new DemandeViewModel
                {
                    Id = d.Id,
                    Titre = d.Titre,
                    Description = d.Description,
                    Type = d.Type.ToString(),
                    Statut = FormatStatut(d.Statut),
                    Criticite = TraduireCriticite(d.Criticite),
                    Demandeur = ObtenirNomUtilisateur(d.DemandeurId, utilisateurs),
                    BusinessAnalyst = ObtenirNomUtilisateur(d.BusinessAnalystId, utilisateurs),
                    Managers = ObtenirManagersEquipes(d.EquipesAssigneesIds, equipes, utilisateurs),
                    ProjetNom = d.ProjetId.HasValue ? projets.FirstOrDefault(p => p.Id == d.ProjetId.Value)?.Nom ?? "-" : "-",
                    ProjetId = d.ProjetId,
                    DateCreation = d.DateCreation,
                    EstAcceptee = d.Statut == StatutDemande.Acceptee,
                    EstArchivee = d.EstArchivee,
                    EquipesAssigneesIds = d.EquipesAssigneesIds ?? new List<int>(),
                    EquipesNoms = ObtenirNomsEquipes(d.EquipesAssigneesIds, equipes),
                    MembresEquipes = ObtenirMembresAssignes(d, utilisateurs)
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

            // Filtre recherche textuelle
            var recherche = TxtRecherche?.Text?.Trim();
            if (!string.IsNullOrEmpty(recherche))
            {
                filtered = filtered.Where(d =>
                    (d.Titre != null && d.Titre.IndexOf(recherche, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (d.Description != null && d.Description.IndexOf(recherche, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (d.Demandeur != null && d.Demandeur.IndexOf(recherche, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (d.EquipesNoms != null && d.EquipesNoms.IndexOf(recherche, StringComparison.OrdinalIgnoreCase) >= 0));
            }

            // Filtre statut
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
            
            // Filtre équipe
            if (CmbFiltreEquipe.SelectedIndex > 0 && CmbFiltreEquipe.SelectedValue != null)
            {
                var equipeId = (int)CmbFiltreEquipe.SelectedValue;
                filtered = filtered.Where(d => 
                {
                    if (d.EquipesAssigneesIds == null || d.EquipesAssigneesIds.Count == 0)
                        return false;
                    return d.EquipesAssigneesIds.Contains(equipeId);
                });
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

            var liste = filtered.ToList();
            int total = liste.Count;
            int totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));

            // S'assurer que la page courante est valide
            if (_pageActuelle < 1) _pageActuelle = 1;
            if (_pageActuelle > totalPages) _pageActuelle = totalPages;

            var page = liste.Skip((_pageActuelle - 1) * PageSize).Take(PageSize).ToList();
            ListeDemandes.ItemsSource = page;

            // Mettre à jour les contrôles de pagination
            var label = LocalizationService.Instance["Requests_PaginationRequests"];
            var of = LocalizationService.Instance["Requests_PaginationOf"];
            TxtInfoPagination.Text = string.Format("{0} {1}", total, label);
            TxtNumeroPage.Text = string.Format("Page {0} / {1}", _pageActuelle, totalPages);
            TxtTotalDemandes.Text = string.Format("{0}-{1} {2} {3}",
                total == 0 ? 0 : (_pageActuelle - 1) * PageSize + 1,
                Math.Min(_pageActuelle * PageSize, total),
                of,
                total);

            BtnPremierePage.IsEnabled = _pageActuelle > 1;
            BtnPagePrecedente.IsEnabled = _pageActuelle > 1;
            BtnPageSuivante.IsEnabled = _pageActuelle < totalPages;
            BtnDernierePage.IsEnabled = _pageActuelle < totalPages;
        }

        private void BtnPremierePage_Click(object sender, RoutedEventArgs e)
        {
            _pageActuelle = 1;
            AppliquerFiltres();
        }

        private void BtnPagePrecedente_Click(object sender, RoutedEventArgs e)
        {
            if (_pageActuelle > 1) { _pageActuelle--; AppliquerFiltres(); }
        }

        private void BtnPageSuivante_Click(object sender, RoutedEventArgs e)
        {
            _pageActuelle++;
            AppliquerFiltres();
        }

        private void BtnDernierePage_Click(object sender, RoutedEventArgs e)
        {
            if (_toutesLesDemandes == null) return;
            var total = _toutesLesDemandes.Count(d => d.EstArchivee == _afficherArchives);
            _pageActuelle = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
            AppliquerFiltres();
        }

        private string ObtenirNomUtilisateur(int? userId, List<Utilisateur> utilisateurs)
        {
            if (!userId.HasValue) return LocalizationService.Instance["Requests_NotAssigned"];
            var user = utilisateurs.FirstOrDefault(u => u.Id == userId.Value);
            return user != null ? string.Format("{0} {1}", user.Prenom, user.Nom) : LocalizationService.Instance["Requests_NotAssigned"];
        }

        private string FormatStatut(StatutDemande statut)
        {
            switch (statut)
            {
                case StatutDemande.EnAttenteSpecification:
                    return LocalizationService.Instance["Requests_StatusPendingSpec"];
                case StatutDemande.EnAttenteChiffrage:
                    return LocalizationService.Instance["Requests_StatusPendingEstimate"];
                case StatutDemande.EnAttenteValidationManager:
                    return LocalizationService.Instance["Requests_StatusPendingValidation"];
                case StatutDemande.Acceptee:
                    return LocalizationService.Instance["Requests_StatusAccepted"];
                case StatutDemande.PlanifieeEnUS:
                    return LocalizationService.Instance["Requests_StatusPlannedUS"];
                case StatutDemande.EnCours:
                    return LocalizationService.Instance["Requests_StatusInProgress"];
                case StatutDemande.Livree:
                    return LocalizationService.Instance["Requests_StatusDelivered"];
                case StatutDemande.Refusee:
                    return LocalizationService.Instance["Requests_StatusRejected"];
                default:
                    return statut.ToString();
            }
        }

        private string ObtenirManagersEquipes(List<int> equipesIds, List<Equipe> equipes, List<Utilisateur> utilisateurs)
        {
            if (equipesIds == null || equipesIds.Count == 0)
                return LocalizationService.Instance["Requests_NotAssigned"];

            var managers = new List<string>();
            foreach (var equipeId in equipesIds)
            {
                var equipe = equipes.FirstOrDefault(e => e.Id == equipeId);
                if (equipe != null && equipe.ManagerId.HasValue)
                {
                    var managerNom = ObtenirNomUtilisateur(equipe.ManagerId.Value, utilisateurs);
                    if (!string.IsNullOrEmpty(managerNom) && managerNom != LocalizationService.Instance["Requests_NotAssigned"] && !managers.Contains(managerNom))
                    {
                        managers.Add(managerNom);
                    }
                }
            }

            return managers.Count > 0 ? string.Join(", ", managers) : LocalizationService.Instance["Requests_NotAssigned"];
        }

        private string TraduireCriticite(Criticite criticite)
        {
            switch (criticite)
            {
                case Criticite.Haute:
                    return LocalizationService.Instance["Requests_CriticalityHigh"];
                case Criticite.Moyenne:
                    return LocalizationService.Instance["Requests_CriticalityMedium"];
                case Criticite.Basse:
                    return LocalizationService.Instance["Requests_CriticalityLow"];
                default:
                    return criticite.ToString();
            }
        }

        private string ObtenirNomsEquipes(List<int> equipesIds, List<Equipe> equipes)
        {
            if (equipesIds == null || equipesIds.Count == 0)
                return LocalizationService.Instance["Requests_NoTeam"];

            var nomsEquipes = new List<string>();
            foreach (var equipeId in equipesIds)
            {
                var equipe = equipes.FirstOrDefault(e => e.Id == equipeId);
                if (equipe != null)
                {
                    nomsEquipes.Add(equipe.Nom);
                }
            }

            return nomsEquipes.Count > 0 ? string.Join(", ", nomsEquipes) : LocalizationService.Instance["Requests_NoTeam"];
        }

        private string ObtenirMembresEquipes(List<int> equipesIds, List<Equipe> equipes, List<Utilisateur> utilisateurs)
        {
            if (equipesIds == null || equipesIds.Count == 0)
                return LocalizationService.Instance["Requests_NoMembers"];

            var membresIds = new HashSet<int>();
            foreach (var equipeId in equipesIds)
            {
                var membres = utilisateurs.Where(u => u.EquipeId == equipeId && u.Actif).ToList();
                foreach (var membre in membres)
                {
                    membresIds.Add(membre.Id);
                }
            }

            var nomsMembres = membresIds
                .Select(id => utilisateurs.FirstOrDefault(u => u.Id == id))
                .Where(u => u != null)
                .Select(u => string.Format("{0} {1}", u.Prenom, u.Nom))
                .OrderBy(n => n)
                .ToList();

            return nomsMembres.Count > 0 ? string.Join(", ", nomsMembres) : LocalizationService.Instance["Requests_NoMembers"];
        }

        private string ObtenirMembresAssignes(Demande demande, List<Utilisateur> utilisateurs)
        {
            var membresAssignes = new List<string>();

            // Business Analyst
            if (demande.BusinessAnalystId.HasValue)
            {
                var ba = utilisateurs.FirstOrDefault(u => u.Id == demande.BusinessAnalystId.Value);
                if (ba != null)
                {
                    membresAssignes.Add(string.Format("{0} {1} (BA)", ba.Prenom, ba.Nom));
                }
            }

            // Développeur chiffreur
            if (demande.DevChiffreurId.HasValue)
            {
                var dev = utilisateurs.FirstOrDefault(u => u.Id == demande.DevChiffreurId.Value);
                if (dev != null)
                {
                    membresAssignes.Add(string.Format("{0} {1} (Dev)", dev.Prenom, dev.Nom));
                }
            }

            return membresAssignes.Count > 0 ? string.Join(", ", membresAssignes) : "Aucun membre assigné";
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

        private bool _isAnalyseEmailInProgress = false;

        private void BtnAnalyseEmail_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"BtnAnalyseEmail_Click called - _isAnalyseEmailInProgress: {_isAnalyseEmailInProgress}");
            
            if (_isAnalyseEmailInProgress) 
            {
                System.Diagnostics.Debug.WriteLine("Analysis already in progress, returning");
                return;
            }
            
            try
            {
                _isAnalyseEmailInProgress = true;
                System.Diagnostics.Debug.WriteLine("Creating AnalyseEmailDemandeWindow");
                
                var window = new AnalyseEmailDemandeWindow(_database, _authService);
                window.Owner = Window.GetWindow(this);
                
                System.Diagnostics.Debug.WriteLine("About to call ShowDialog");
                var result = window.ShowDialog();
                System.Diagnostics.Debug.WriteLine($"ShowDialog returned: {result}");
                
                if (result == true)
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
                System.Diagnostics.Debug.WriteLine($"Error in BtnAnalyseEmail_Click: {ex.Message}");
                MessageBox.Show(
                    $"Erreur lors de l'analyse de l'email :\n\n{ex.Message}",
                    "Erreur",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                System.Diagnostics.Debug.WriteLine("Setting _isAnalyseEmailInProgress to false");
                _isAnalyseEmailInProgress = false;
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

        private void BtnCreerProjetClick(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is int demandeId)
            {
                CreerProjetPourDemande(demandeId);
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
                    MessageBox.Show(LocalizationService.Instance.GetString("Requests_DeletePermissionDenied"),
                        LocalizationService.Instance.GetString("Common_AccessDenied"), MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Récupérer la demande pour afficher ses détails
                var demande = _toutesLesDemandes.FirstOrDefault(d => d.Id == demandeId);
                if (demande == null) return;

                // Confirmation
                var result = MessageBox.Show(
                    string.Format(LocalizationService.Instance.GetString("Requests_DeleteConfirmation"),
                        demande.Titre, demande.Statut),
                    LocalizationService.Instance.GetString("Common_DeleteConfirmation"),
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning,
                    MessageBoxResult.No);

                if (result == MessageBoxResult.Yes)
                {
                    _database.DeleteDemande(demandeId);
                    MessageBox.Show(LocalizationService.Instance.GetString("Requests_DeleteSuccess"), LocalizationService.Instance.GetString("Common_Deletion"), 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    ChargerDemandes();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(LocalizationService.Instance.GetString("Requests_DeleteError"), ex.Message),
                    LocalizationService.Instance.GetString("Common_Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CreerProjetPourDemande(int demandeId)
        {
            try
            {
                var demande = _database.GetDemandes().FirstOrDefault(d => d.Id == demandeId);
                if (demande == null)
                {
                    MessageBox.Show("Demande introuvable.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Vérifier si la demande a déjà un projet
                if (demande.ProjetId.HasValue)
                {
                    MessageBox.Show("Cette demande est déjà associée à un projet.", "Information", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Réutiliser le BacklogService de l'application
                var backlogService = (Application.Current as App)?.BacklogService ?? new BacklogService(_database);
                var nouveauProjet = new Projet
                {
                    Nom = demande.Titre,
                    Description = demande.Description,
                    Actif = true,
                    DateCreation = DateTime.Now,
                    // Copier les champs Phase 2 de la demande
                    ProgrammeId = demande.ProgrammeId,
                    Priorite = demande.Priorite,
                    TypeProjet = demande.TypeProjet,
                    Categorie = demande.Categorie,
                    Drivers = demande.Drivers,
                    Ambition = demande.Ambition,
                    Beneficiaires = demande.Beneficiaires,
                    GainsTemps = demande.GainsTemps,
                    GainsFinanciers = demande.GainsFinanciers,
                    LeadProjet = demande.LeadProjet,
                    EstImplemente = demande.EstImplemente,
                    EquipesAssigneesIds = demande.EquipesAssigneesIds != null ? new List<int>(demande.EquipesAssigneesIds) : new List<int>()
                };
                
                var window = new EditProjetWindow(backlogService, nouveauProjet);
                window.Owner = Window.GetWindow(this);
                window.Title = string.Format(LocalizationService.Instance["Requests_CreateProjectFor"], demande.Titre);

                if (window.ShowDialog() == true)
                {
                    // Récupérer le dernier projet créé
                    var projets = _database.GetProjets().OrderByDescending(p => p.DateCreation).ToList();
                    if (projets.Any())
                    {
                        var projetCree = projets.First();
                        
                        // Associer le projet à la demande
                        demande.ProjetId = projetCree.Id;
                        _database.AddOrUpdateDemande(demande);

                        // Historique
                        var utilisateur = _authService.CurrentUser;
                        if (utilisateur != null)
                        {
                            var historique = new HistoriqueModification
                            {
                                TypeEntite = "Demande",
                                EntiteId = demande.Id,
                                UtilisateurId = utilisateur.Id,
                                DateModification = DateTime.Now,
                                TypeModification = Domain.TypeModification.Modification,
                                NouvelleValeur = string.Format("Projet '{0}' créé et associé", projetCree.Nom),
                                AncienneValeur = "Aucun projet",
                                ChampModifie = "ProjetId"
                            };
                            _database.AddHistorique(historique);
                        }

                        MessageBox.Show(
                            string.Format("Projet '{0}' créé et associé avec succès !\n\nVous pouvez maintenant créer une tâche.", 
                                nouveauProjet.Nom),
                            "Succès",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);

                        // Recharger les demandes
                        ChargerDemandes();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Erreur lors de la création du projet : {0}", ex.Message),
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

                // Vérifier que la demande a un projet
                if (!demande.ProjetId.HasValue)
                {
                    MessageBox.Show("Cette demande n'a pas de projet associé.\n\nVeuillez d'abord créer un projet.", 
                        "Projet manquant", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                    // ChiffrageHeures laissé vide : le développeur décide du chiffrage lui-même
                    ChiffrageHeures = null
                };

                // Réutiliser les services de l'application
                var backlogService = (Application.Current as App)?.BacklogService ?? new BacklogService(_database);

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
                    MessageBox.Show(LocalizationService.Instance.GetString("Requests_ArchivePermissionDenied"),
                        LocalizationService.Instance.GetString("Common_AccessDenied"), MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Récupérer la demande
                var demande = _database.GetDemandes().FirstOrDefault(d => d.Id == demandeId);
                if (demande == null)
                {
                    MessageBox.Show(LocalizationService.Instance.GetString("Requests_RequestNotFound"), LocalizationService.Instance.GetString("Common_Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                
                string action = demande.EstArchivee ? LocalizationService.Instance.GetString("Requests_Unarchive") : LocalizationService.Instance.GetString("Requests_Archive");
                string actionPasse = demande.EstArchivee ? LocalizationService.Instance.GetString("Requests_Unarchived") : LocalizationService.Instance.GetString("Requests_Archived");
                
                // Confirmation
                var result = MessageBox.Show(
                    string.Format(LocalizationService.Instance.GetString("Requests_ArchiveConfirmation"),
                        action, demande.Titre, FormatStatut(demande.Statut)),
                    LocalizationService.Instance.GetString("Common_Confirmation"),
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question,
                    MessageBoxResult.No);

                if (result == MessageBoxResult.Yes)
                {
                    // Basculer le statut archivé
                    bool nouvelEtatArchive = !demande.EstArchivee;
                    demande.EstArchivee = nouvelEtatArchive;
                    _database.AddOrUpdateDemande(demande);
                    
                    MessageBox.Show(string.Format(LocalizationService.Instance.GetString("Requests_ArchiveSuccess"), actionPasse), 
                        LocalizationService.Instance.GetString("Common_Success"), MessageBoxButton.OK, MessageBoxImage.Information);
                    
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
                MessageBox.Show(string.Format(LocalizationService.Instance.GetString("Requests_ArchiveError"), ex.Message),
                    LocalizationService.Instance.GetString("Common_Error"), MessageBoxButton.OK, MessageBoxImage.Error);
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
        public string Managers { get; set; }
        public string ProjetNom { get; set; }
        public int? ProjetId { get; set; }
        public DateTime DateCreation { get; set; }
        public bool EstAcceptee { get; set; }
        public bool EstArchivee { get; set; }
        public List<int> EquipesAssigneesIds { get; set; }
        public string EquipesNoms { get; set; }
        public string MembresEquipes { get; set; }
        public bool AProjet => ProjetId.HasValue;
        public bool EstAccepteeSansProjet => EstAcceptee && !AProjet;
        public bool EstAccepteeAvecProjet => EstAcceptee && AProjet;
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

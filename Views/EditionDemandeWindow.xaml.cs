using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using BacklogManager.Domain;
using BacklogManager.Services;

namespace BacklogManager.Views
{
    public partial class EditionDemandeWindow : Window
    {
        private readonly IDatabase _database;
        private readonly AuthenticationService _authService;
        private readonly int? _demandeId;
        private Demande _demandeActuelle;

        public EditionDemandeWindow(IDatabase database, AuthenticationService authService, int? demandeId = null)
        {
            InitializeComponent();
            _database = database;
            _authService = authService;
            _demandeId = demandeId;

            InitialiserComboBoxes();
            
            if (_demandeId.HasValue)
            {
                TxtTitre.Text = LocalizationService.Instance["Requests_EditRequest"];
                ChargerDemande();
                PanelChiffrage.Visibility = Visibility.Visible;
                
                // Afficher le panel des spécifications si la demande est en attente de spécification
                if (_demandeActuelle != null && _demandeActuelle.Statut == StatutDemande.EnAttenteSpecification)
                {
                    PanelSpecifications.Visibility = Visibility.Visible;
                }
                
                // Afficher le panel de validation manager si la demande est en attente de validation et que l'utilisateur est Admin ou Chef de Projet
                if (_demandeActuelle != null && _demandeActuelle.Statut == StatutDemande.EnAttenteValidationManager)
                {
                    var currentRole = _authService.GetCurrentUserRole();
                    if (currentRole != null && (currentRole.Type == Domain.RoleType.Administrateur || currentRole.Type == Domain.RoleType.ChefDeProjet))
                    {
                        PanelArbitrage.Visibility = Visibility.Visible;
                    }
                }
            }
            else
            {
                _demandeActuelle = new Demande
                {
                    Id = 0,
                    DateCreation = DateTime.Now,
                    Statut = StatutDemande.EnAttenteSpecification,
                    DemandeurId = _authService.CurrentUser?.Id ?? 0
                };
            }

            BtnAnnuler.Click += (s, e) => { DialogResult = false; Close(); };
            BtnEnregistrer.Click += BtnEnregistrer_Click;
            BtnValiderSpecifications.Click += BtnValiderSpecifications_Click;
            BtnAccepter.Click += BtnAccepter_Click;
            BtnRefuser.Click += BtnRefuser_Click;
            
            LocalizationService.Instance.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(LocalizationService.CurrentCulture))
                {
                    InitialiserTextes();
                    InitialiserComboBoxes();
                }
            };

            // Initialiser les textes au chargement
            InitialiserTextes();
        }

        private void InitialiserTextes()
        {
            // Titre de la fenêtre
            if (_demandeId > 0)
            {
                Title = LocalizationService.Instance["Requests_EditRequest"];
                TxtTitre.Text = LocalizationService.Instance["Requests_EditRequest"];
            }
            else
            {
                Title = LocalizationService.Instance["Requests_NewRequest"];
                TxtTitre.Text = LocalizationService.Instance["Requests_NewRequest"];
            }

            // Sections principales via x:Name
            TxtDetailedSpecsSection.Text = "📋 " + LocalizationService.Instance["Requests_DetailedSpecs"];
            TxtProgramClassificationSection.Text = LocalizationService.Instance["Requests_ProgramClassification"];
            TxtDriversAmbitionSection.Text = LocalizationService.Instance["Requests_DriversAmbition"];
            TxtExpectedGainsSection.Text = LocalizationService.Instance["Requests_ExpectedGains"];
            TxtAssignedTeamsSection.Text = LocalizationService.Instance["Requests_AssignedTeams"];
            TxtEstimationSection.Text = LocalizationService.Instance["Requests_Estimation"];
            TxtRequiredFieldsInstruction.Text = LocalizationService.Instance["Requests_RequiredFields"];
            TxtDefineSpecsInstruction.Text = LocalizationService.Instance["Requests_DefineSpecs"];
            TxtTypeLabel.Text = LocalizationService.Instance["Requests_TypeLabel"] + " *";
            TxtCriticalityLabel.Text = LocalizationService.Instance["Requests_CriticalityLabel"] + " *";
            
            // Labels principaux
            TxtAssociatedProgramLabel.Text = LocalizationService.Instance["Requests_AssociatedProgram"];
            TxtBeneficiariesLabel.Text = LocalizationService.Instance["Requests_Beneficiaries"];
            TxtConcernedTeamsLabel.Text = LocalizationService.Instance["Requests_ConcernedTeams"];
            TxtBusinessContextLabel.Text = LocalizationService.Instance["Requests_BusinessContext"];
            TxtExpectedBenefitsLabel.Text = LocalizationService.Instance["Requests_ExpectedBenefits"];
            TxtBusinessAnalystLabel.Text = LocalizationService.Instance["Requests_BusinessAnalyst"];
            TxtManagersLabel.Text = LocalizationService.Instance["Requests_Managers"];
            TxtEstimatingDeveloperLabel.Text = LocalizationService.Instance["Requests_EstimatingDeveloper"];
            TxtEstimationDaysLabel.Text = LocalizationService.Instance["Requests_EstimationDays"];
            TxtManagerValidationInstruction.Text = LocalizationService.Instance["Requests_AwaitingValidation"];

            // Instructions et exemples
            TxtSelectDriversInstruction.Text = LocalizationService.Instance["Requests_SelectDrivers"];
            TxtWhoBenefitsInstruction.Text = LocalizationService.Instance["Requests_WhoBenefits"];
            TxtTimeGainsExample.Text = LocalizationService.Instance["Requests_TimeGainsExample"];
            TxtFinancialGainsExample.Text = LocalizationService.Instance["Requests_FinancialGainsExample"];
            TxtSelectTeamsInstruction.Text = LocalizationService.Instance["Requests_SelectTeams"];

            // Labels avec Runs - traitement spécial pour les labels avec astérisques
            ((Run)TxtTitleLabel.Inlines.FirstInline).Text = LocalizationService.Instance["Requests_TitleLabel"];
            ((Run)TxtDescriptionLabel.Inlines.FirstInline).Text = LocalizationService.Instance["Requests_DescriptionLabel"];

            // Sous-labels via x:Name
            TxtAssociatedProgramLabel.Text = LocalizationService.Instance["Requests_AssociatedProgram"];
            TxtDriversLabel.Text = LocalizationService.Instance["Requests_Drivers"];
            TxtAmbitionLabel.Text = LocalizationService.Instance["Requests_Ambition"];
            TxtTimeGainsLabel.Text = LocalizationService.Instance["Requests_TimeGains"] + " *";
            TxtFinancialGainsLabel.Text = LocalizationService.Instance["Requests_FinancialGains"] + " (optionnel)";
            TxtPriorityLabel.Text = LocalizationService.Instance["Requests_PriorityLabel"] + " *";
            TxtProjectTypeLabel.Text = LocalizationService.Instance["Requests_ProjectType"];
            TxtCategoryLabel.Text = LocalizationService.Instance["Requests_Category"];
            TxtLeadProjetLabel.Text = LocalizationService.Instance["Requests_ProjectLead"];

            // Labels avec FindVisualChildren pour les textes français dans le XAML
            foreach (var child in FindVisualChildren<TextBlock>(this))
            {
                if (child.Text == "Complétez tous les champs obligatoires marqués d'un astérisque (*)")
                    child.Text = LocalizationService.Instance["Requests_RequiredFields"];
                else if (child.Text == "PROGRAMME ET CLASSIFICATION")
                    child.Text = LocalizationService.Instance["Requests_ProgramClassification"];
                else if (child.Text == "Programme associé")
                    child.Text = LocalizationService.Instance["Requests_AssociatedProgram"];
                else if (child.Text == "Priorité *")
                    child.Text = LocalizationService.Instance["Requests_PriorityLabel"] + " *";
                else if (child.Text == "Type Projet")
                    child.Text = LocalizationService.Instance["Requests_ProjectType"];
                else if (child.Text == "Catégorie")
                    child.Text = LocalizationService.Instance["Requests_Category"];
                else if (child.Text == "Lead Projet")
                    child.Text = LocalizationService.Instance["Requests_ProjectLead"];
                else if (child.Text == "Type *")
                    child.Text = LocalizationService.Instance["Requests_TypeLabel"] + " *";
                else if (child.Text == "Criticité *")
                    child.Text = LocalizationService.Instance["Requests_CriticalityLabel"] + " *";
                else if (child.Text == "Sélectionnez les drivers applicables (plusieurs choix possibles)")
                    child.Text = LocalizationService.Instance["Requests_SelectDrivers"];
                else if (child.Text == "Bénéficiaires")
                    child.Text = LocalizationService.Instance["Requests_Beneficiaries"];
                else if (child.Text == "Qui bénéficie de ce projet ? (plusieurs choix possibles)")
                    child.Text = LocalizationService.Instance["Requests_WhoBenefits"];
                else if (child.Text.StartsWith("Exemples: '15 heures/semaine'"))
                    child.Text = LocalizationService.Instance["Requests_TimeGainsExample"];
                else if (child.Text.StartsWith("Exemples: '45000€ annuels'"))
                    child.Text = LocalizationService.Instance["Requests_FinancialGainsExample"];
                else if (child.Text == "Équipes concernées")
                    child.Text = LocalizationService.Instance["Requests_ConcernedTeams"];
                else if (child.Text == "Sélectionnez une ou plusieurs équipes (plusieurs choix possibles)")
                    child.Text = LocalizationService.Instance["Requests_SelectTeams"];
                else if (child.Text == "Contexte métier")
                    child.Text = LocalizationService.Instance["Requests_BusinessContext"];
                else if (child.Text == "Bénéfices attendus")
                    child.Text = LocalizationService.Instance["Requests_ExpectedBenefits"];
                else if (child.Text == "Business Analyst")
                    child.Text = LocalizationService.Instance["Requests_BusinessAnalyst"];
                else if (child.Text == "Manager(s)")
                    child.Text = LocalizationService.Instance["Requests_Managers"];
                else if (child.Text == LocalizationService.Instance.GetString("Requests_SelectTeamsForManagers"))
                    child.Text = LocalizationService.Instance["Requests_SelectTeamsForManagers"];
                else if (child.Text == "Développeur chiffreur")
                    child.Text = LocalizationService.Instance["Requests_EstimatingDeveloper"];
                else if (child.Text == "Estimation (jours)")
                    child.Text = LocalizationService.Instance["Requests_EstimationDays"];
                else if (child.Text == "Cette demande est en attente de validation manager. Décidez d'accepter ou de refuser le lancement du projet.")
                    child.Text = LocalizationService.Instance["Requests_AwaitingValidation"];
                else if (child.Text == "Annuler")
                    child.Text = LocalizationService.Instance["Cancel"];
                else if (child.Text == "Enregistrer")
                    child.Text = LocalizationService.Instance["Save"];
            }

            // Boutons et CheckBoxes
            BtnValiderSpecifications.Content = "✓ " + LocalizationService.Instance["Requests_ValidateAndEstimate"];
            BtnEnregistrer.Content = LocalizationService.Instance["Save"];
            
            // Initialiser le texte par défaut pour les managers
            TxtManagers.Text = LocalizationService.Instance.GetString("Requests_SelectTeamsForManagers");
            BtnAnnuler.Content = LocalizationService.Instance["Cancel"];
            BtnAccepter.Content = "✅ " + LocalizationService.Instance["Common_Accept"];
            BtnRefuser.Content = "❌ " + LocalizationService.Instance["Common_Refuse"];
            
            // Traduire les CheckBoxes avec Content
            foreach (var chk in FindVisualChildren<CheckBox>(this))
            {
                if (chk.Content?.ToString() == "Déjà implémenté")
                    chk.Content = LocalizationService.Instance["Requests_AlreadyImplemented"];
                else if (chk.Content?.ToString() == "Automation")
                    chk.Content = LocalizationService.Instance["Requests_DriverAutomation"];
                else if (chk.Content?.ToString() == "Efficiency Gains")
                    chk.Content = LocalizationService.Instance["Requests_DriverEfficiency"];
                else if (chk.Content?.ToString() == "Process Optimization")
                    chk.Content = LocalizationService.Instance["Requests_DriverOptimization"];
                else if (chk.Content?.ToString() == "Standardization")
                    chk.Content = LocalizationService.Instance["Requests_DriverStandardization"];
                else if (chk.Content?.ToString() == "Aucun")
                    chk.Content = LocalizationService.Instance["Requests_DriverNone"];
            }

            // Update TextBlocks with Runs (for labels with asterisks)
            foreach (var textBlock in FindVisualChildren<TextBlock>(this))
            {
                if (textBlock.Inlines.Count == 2)
                {
                    var firstRun = textBlock.Inlines.FirstInline as Run;
                    var secondRun = textBlock.Inlines.LastInline as Run;
                    
                    if (firstRun != null && secondRun != null && secondRun.Text == " *")
                    {
                        if (firstRun.Text == "Titre")
                            firstRun.Text = LocalizationService.Instance["Requests_TitleLabel"];
                        else if (firstRun.Text == "Description")
                            firstRun.Text = LocalizationService.Instance["Requests_DescriptionLabel"];
                        else if (firstRun.Text == "Type")
                            firstRun.Text = LocalizationService.Instance["Requests_TypeLabel"];
                        else if (firstRun.Text == "Criticité")
                            firstRun.Text = LocalizationService.Instance["Requests_CriticalityLabel"];
                        else if (firstRun.Text == "Priorité")
                            firstRun.Text = LocalizationService.Instance["Requests_PriorityLabel"];
                    }
                }
            }
        }

        private IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }

        private void InitialiserComboBoxes()
        {
            // Type
            CmbType.ItemsSource = Enum.GetValues(typeof(TypeDemande)).Cast<TypeDemande>()
                .Select(t => new { Value = t, Display = FormatTypeDemande(t) });
            CmbType.DisplayMemberPath = "Display";
            CmbType.SelectedValuePath = "Value";
            CmbType.SelectedIndex = 0;

            // Criticité
            CmbCriticite.ItemsSource = Enum.GetValues(typeof(Criticite)).Cast<Criticite>();
            CmbCriticite.SelectedIndex = 0;

            // Utilisateurs
            var utilisateurs = _database.GetUtilisateurs().Where(u => u.Actif).ToList();
            var roles = _database.GetRoles();

            // Développeurs - initialisation vide, sera rempli par FiltrerUtilisateursParEquipes
            CmbBusinessAnalyst.ItemsSource = new[] { new { Id = 0, Nom = "Non assigné" } };
            CmbBusinessAnalyst.DisplayMemberPath = "Nom";
            CmbBusinessAnalyst.SelectedValuePath = "Id";
            CmbBusinessAnalyst.SelectedIndex = 0;

            CmbDevChiffreur.ItemsSource = new[] { new { Id = 0, Nom = "Non assigné" } };
            CmbDevChiffreur.DisplayMemberPath = "Nom";
            CmbDevChiffreur.SelectedValuePath = "Id";
            CmbDevChiffreur.SelectedIndex = 0;
            
            // PHASE 2: Programmes
            var programmes = _database.GetAllProgrammes().Where(p => p.Actif).ToList();
            var programmesCombo = programmes.Select(p => new { Id = p.Id, Display = string.Format("{0} - {1}", p.Code, p.Nom) }).ToList();
            programmesCombo.Insert(0, new { Id = 0, Display = "-- Aucun programme --" });
            CmbProgramme.ItemsSource = programmesCombo;
            CmbProgramme.DisplayMemberPath = "Display";
            CmbProgramme.SelectedValuePath = "Id";
            CmbProgramme.SelectedIndex = 0;
            
            // PHASE 2: Priorité
            var priorites = new[] { "Top High", "High", "Medium", "Low" };
            CmbPriorite.ItemsSource = priorites;
            CmbPriorite.SelectedIndex = 2; // Medium par défaut
            
            // PHASE 2: Type Projet
            var typesProjets = new[] { "Data", "Digital", "Regulatory", "Run", "Transformation", "" };
            CmbTypeProjet.ItemsSource = typesProjets;
            CmbTypeProjet.SelectedIndex = 5; // Vide par défaut
            
            // PHASE 2: Catégorie
            var categories = new[] { "BAU", "TRANSFO", "" };
            CmbCategorie.ItemsSource = categories;
            CmbCategorie.SelectedIndex = 2; // Vide par défaut
            
            // PHASE 2: Lead Projet
            var leads = new[] { "GTTO", "CCI", "Autre", "" };
            CmbLeadProjet.ItemsSource = leads;
            CmbLeadProjet.SelectedIndex = 3; // Vide par défaut
            
            // PHASE 2: Ambition
            var ambitions = new[] { "Automation Rate Increase", "Pricing Alignment", "Workload Gain", "Workload Reduction", "N/A", "" };
            CmbAmbition.ItemsSource = ambitions;
            CmbAmbition.SelectedIndex = 5; // Vide par défaut
            
            // PHASE 2: Équipes (multi-sélection via CheckBoxes dynamiques)
            var equipes = _database.GetAllEquipes().Where(e => e.Actif).ToList();
            PanelEquipes.Children.Clear();
            foreach (var equipe in equipes)
            {
                var chk = new CheckBox
                {
                    Content = equipe.Nom,
                    Tag = equipe.Id,
                    Margin = new Thickness(0, 3, 0, 3)
                };
                // Ajouter un gestionnaire pour filtrer les BA/Dev quand une équipe est cochée/décochée
                chk.Checked += (s, e) => FiltrerUtilisateursParEquipes();
                chk.Unchecked += (s, e) => FiltrerUtilisateursParEquipes();
                PanelEquipes.Children.Add(chk);
            }
        }

        private void FiltrerUtilisateursParEquipes()
        {
            // Récupérer les équipes sélectionnées
            var equipesSelectionnees = new List<int>();
            foreach (CheckBox chk in PanelEquipes.Children)
            {
                if (chk.IsChecked == true && chk.Tag is int equipeId)
                {
                    equipesSelectionnees.Add(equipeId);
                }
            }

            var utilisateurs = _database.GetUtilisateurs().Where(u => u.Actif).ToList();
            var roles = _database.GetRoles();

            // Si aucune équipe sélectionnée, afficher tous les utilisateurs
            if (equipesSelectionnees.Count == 0)
            {
                // Business Analysts - tous
                var tousBas = utilisateurs.Where(u =>
                {
                    var role = roles.FirstOrDefault(r => r.Id == u.RoleId);
                    return role?.Type == RoleType.BusinessAnalyst;
                }).Select(u => new { Id = u.Id, Nom = string.Format("{0} {1}", u.Prenom, u.Nom) }).ToList();
                tousBas.Insert(0, new { Id = 0, Nom = "Non assigné" });
                
                var selectedBaId = CmbBusinessAnalyst.SelectedValue;
                CmbBusinessAnalyst.ItemsSource = tousBas;
                CmbBusinessAnalyst.SelectedValue = selectedBaId;

                // Développeurs - tous
                var tousDevs = utilisateurs.Where(u =>
                {
                    var role = roles.FirstOrDefault(r => r.Id == u.RoleId);
                    return role?.Type == RoleType.Developpeur;
                }).Select(u => new { Id = u.Id, Nom = string.Format("{0} {1}", u.Prenom, u.Nom) }).ToList();
                tousDevs.Insert(0, new { Id = 0, Nom = "Non assigné" });
                
                var selectedDevId = CmbDevChiffreur.SelectedValue;
                CmbDevChiffreur.ItemsSource = tousDevs;
                CmbDevChiffreur.SelectedValue = selectedDevId;
            }
            else
            {
                // Filtrer les BA par équipes sélectionnées
                var basFiltres = utilisateurs.Where(u =>
                {
                    var role = roles.FirstOrDefault(r => r.Id == u.RoleId);
                    return role?.Type == RoleType.BusinessAnalyst && 
                           u.EquipeId.HasValue && 
                           equipesSelectionnees.Contains(u.EquipeId.Value);
                }).Select(u => new { Id = u.Id, Nom = string.Format("{0} {1}", u.Prenom, u.Nom) }).ToList();
                basFiltres.Insert(0, new { Id = 0, Nom = "Non assigné" });
                
                var selectedBaId = CmbBusinessAnalyst.SelectedValue;
                CmbBusinessAnalyst.ItemsSource = basFiltres;
                // Réappliquer la sélection si elle fait partie des équipes sélectionnées
                if (selectedBaId != null && basFiltres.Any(b => b.Id == (int)selectedBaId))
                    CmbBusinessAnalyst.SelectedValue = selectedBaId;
                else
                    CmbBusinessAnalyst.SelectedIndex = 0;

                // Filtrer les Devs par équipes sélectionnées
                var devsFiltres = utilisateurs.Where(u =>
                {
                    var role = roles.FirstOrDefault(r => r.Id == u.RoleId);
                    return role?.Type == RoleType.Developpeur && 
                           u.EquipeId.HasValue && 
                           equipesSelectionnees.Contains(u.EquipeId.Value);
                }).Select(u => new { Id = u.Id, Nom = string.Format("{0} {1}", u.Prenom, u.Nom) }).ToList();
                devsFiltres.Insert(0, new { Id = 0, Nom = "Non assigné" });
                
                var selectedDevId = CmbDevChiffreur.SelectedValue;
                CmbDevChiffreur.ItemsSource = devsFiltres;
                // Réappliquer la sélection si elle fait partie des équipes sélectionnées
                if (selectedDevId != null && devsFiltres.Any(d => d.Id == (int)selectedDevId))
                    CmbDevChiffreur.SelectedValue = selectedDevId;
                else
                    CmbDevChiffreur.SelectedIndex = 0;
            }
            
            // Mettre à jour les managers
            MettreAJourManagers();
        }

        private void MettreAJourManagers()
        {
            var equipesSelectionnees = new List<int>();
            foreach (CheckBox chk in PanelEquipes.Children)
            {
                if (chk.IsChecked == true && chk.Tag is int equipeId)
                {
                    equipesSelectionnees.Add(equipeId);
                }
            }
            
            if (equipesSelectionnees.Count == 0)
            {
                TxtManagers.Text = LocalizationService.Instance.GetString("Requests_SelectTeamsForManagers");
                return;
            }
            
            var equipes = _database.GetAllEquipes();
            var utilisateurs = _database.GetUtilisateurs();
            var managers = new List<string>();
            
            foreach (var equipeId in equipesSelectionnees)
            {
                var equipe = equipes.FirstOrDefault(e => e.Id == equipeId);
                if (equipe != null && equipe.ManagerId.HasValue)
                {
                    var manager = utilisateurs.FirstOrDefault(u => u.Id == equipe.ManagerId.Value);
                    if (manager != null)
                    {
                        var managerNom = string.Format("{0} {1}", manager.Prenom, manager.Nom);
                        if (!managers.Contains(managerNom))
                        {
                            managers.Add(managerNom);
                        }
                    }
                }
            }
            
            TxtManagers.Text = managers.Count > 0 ? string.Join(", ", managers) : LocalizationService.Instance.GetString("Requests_NoManagersAssigned");
        }

        private string FormatTypeDemande(TypeDemande type)
        {
            switch (type)
            {
                case TypeDemande.Run:
                    return "Run";
                case TypeDemande.Dev:
                    return "Dev";
                default:
                    return type.ToString();
            }
        }

        private void ChargerDemande()
        {
            var demandes = _database.GetDemandes();
            _demandeActuelle = demandes.FirstOrDefault(d => d.Id == _demandeId.Value);

            if (_demandeActuelle == null)
            {
                MessageBox.Show("Demande introuvable.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
                return;
            }

            TxtTitreDemande.Text = _demandeActuelle.Titre;
            TxtDescription.Text = _demandeActuelle.Description;
            TxtSpecifications.Text = _demandeActuelle.Specifications;
            TxtContexte.Text = _demandeActuelle.ContexteMetier;
            TxtBenefices.Text = _demandeActuelle.BeneficesAttendus;

            // Sélectionner les valeurs dans les combos
            CmbType.SelectedValue = _demandeActuelle.Type;
            CmbCriticite.SelectedValue = _demandeActuelle.Criticite;

            TxtChiffrageEstime.Text = _demandeActuelle.ChiffrageEstimeJours?.ToString() ?? "";
            
            // PHASE 2: Charger les nouveaux champs
            if (_demandeActuelle.ProgrammeId.HasValue)
                CmbProgramme.SelectedValue = _demandeActuelle.ProgrammeId.Value;
            
            if (!string.IsNullOrEmpty(_demandeActuelle.Priorite))
                CmbPriorite.SelectedItem = _demandeActuelle.Priorite;
            
            if (!string.IsNullOrEmpty(_demandeActuelle.TypeProjet))
                CmbTypeProjet.SelectedItem = _demandeActuelle.TypeProjet;
            
            if (!string.IsNullOrEmpty(_demandeActuelle.Categorie))
                CmbCategorie.SelectedItem = _demandeActuelle.Categorie;
            
            if (!string.IsNullOrEmpty(_demandeActuelle.LeadProjet))
                CmbLeadProjet.SelectedItem = _demandeActuelle.LeadProjet;
            
            if (!string.IsNullOrEmpty(_demandeActuelle.Ambition))
                CmbAmbition.SelectedItem = _demandeActuelle.Ambition;
            
            ChkEstImplemente.IsChecked = _demandeActuelle.EstImplemente;
            
            TxtGainsTemps.Text = _demandeActuelle.GainsTemps ?? "";
            TxtGainsFinanciers.Text = _demandeActuelle.GainsFinanciers ?? "";
            
            // Drivers (JSON array)
            if (!string.IsNullOrEmpty(_demandeActuelle.Drivers))
            {
                try
                {
                    var drivers = System.Text.Json.JsonSerializer.Deserialize<List<string>>(_demandeActuelle.Drivers);
                    if (drivers != null)
                    {
                        ChkDriverAutomation.IsChecked = drivers.Contains("Automation");
                        ChkDriverEfficiency.IsChecked = drivers.Contains("Efficiency Gains");
                        ChkDriverOptimization.IsChecked = drivers.Contains("Process Optimization");
                        ChkDriverStandardization.IsChecked = drivers.Contains("Standardization");
                        ChkDriverAucun.IsChecked = drivers.Contains("Aucun");
                    }
                }
                catch { }
            }
            
            // Bénéficiaires (JSON array)
            if (!string.IsNullOrEmpty(_demandeActuelle.Beneficiaires))
            {
                try
                {
                    var beneficiaires = System.Text.Json.JsonSerializer.Deserialize<List<string>>(_demandeActuelle.Beneficiaires);
                    if (beneficiaires != null)
                    {
                        ChkBenefSGI.IsChecked = beneficiaires.Contains("SGI");
                        ChkBenefTFSC.IsChecked = beneficiaires.Contains("TFSC");
                        ChkBenefTransversal.IsChecked = beneficiaires.Contains("Transversal");
                    }
                }
                catch { }
            }
            
            // Équipes Assignées (List<int>)
            if (_demandeActuelle.EquipesAssigneesIds != null && _demandeActuelle.EquipesAssigneesIds.Count > 0)
            {
                foreach (CheckBox chk in PanelEquipes.Children)
                {
                    if (chk.Tag is int equipeId && _demandeActuelle.EquipesAssigneesIds.Contains(equipeId))
                    {
                        chk.IsChecked = true;
                    }
                }
            }
            
            // Afficher les managers
            MettreAJourManagers();
            
            // IMPORTANT : Sélectionner BA et Dev APRÈS le filtrage par équipes
            if (_demandeActuelle.BusinessAnalystId.HasValue)
                CmbBusinessAnalyst.SelectedValue = _demandeActuelle.BusinessAnalystId.Value;

            if (_demandeActuelle.DevChiffreurId.HasValue)
                CmbDevChiffreur.SelectedValue = _demandeActuelle.DevChiffreurId.Value;
        }

        private void BtnEnregistrer_Click(object sender, RoutedEventArgs e)
        {
            TxtErreur.Visibility = Visibility.Collapsed;

            // Validation
            if (string.IsNullOrWhiteSpace(TxtTitreDemande.Text))
            {
                TxtErreur.Text = "Le titre est obligatoire.";
                TxtErreur.Visibility = Visibility.Visible;
                return;
            }

            if (string.IsNullOrWhiteSpace(TxtDescription.Text))
            {
                TxtErreur.Text = "La description est obligatoire.";
                TxtErreur.Visibility = Visibility.Visible;
                return;
            }

            if (CmbType.SelectedValue == null)
            {
                TxtErreur.Text = "Le type est obligatoire.";
                TxtErreur.Visibility = Visibility.Visible;
                return;
            }

            try
            {
                // Mise à jour des champs
                _demandeActuelle.Titre = TxtTitreDemande.Text.Trim();
                _demandeActuelle.Description = TxtDescription.Text.Trim();
                _demandeActuelle.Specifications = TxtSpecifications.Text?.Trim();
                _demandeActuelle.ContexteMetier = TxtContexte.Text?.Trim();
                _demandeActuelle.BeneficesAttendus = TxtBenefices.Text?.Trim();
                _demandeActuelle.Type = (TypeDemande)CmbType.SelectedValue;
                _demandeActuelle.Criticite = (Criticite)CmbCriticite.SelectedValue;

                // Assignations
                var baId = (int)CmbBusinessAnalyst.SelectedValue;
                _demandeActuelle.BusinessAnalystId = baId != 0 ? (int?)baId : null;

                // Chiffrage (si visible)
                if (PanelChiffrage.Visibility == Visibility.Visible)
                {
                    var devId = (int)CmbDevChiffreur.SelectedValue;
                    _demandeActuelle.DevChiffreurId = devId != 0 ? (int?)devId : null;

                    if (!string.IsNullOrWhiteSpace(TxtChiffrageEstime.Text) && 
                        decimal.TryParse(TxtChiffrageEstime.Text.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal jours))
                    {
                        _demandeActuelle.ChiffrageEstimeJours = (double)jours;
                        
                        // Si un chiffrage est fourni et qu'on est en attente de chiffrage, passer à En attente validation manager
                        if (_demandeActuelle.Statut == StatutDemande.EnAttenteChiffrage)
                        {
                            _demandeActuelle.Statut = StatutDemande.EnAttenteValidationManager;
                            _demandeActuelle.DateValidationChiffrage = DateTime.Now;
                        }
                    }
                }
                
                // PHASE 2: Enregistrer les nouveaux champs
                var progId = (int)CmbProgramme.SelectedValue;
                _demandeActuelle.ProgrammeId = progId != 0 ? (int?)progId : null;
                
                _demandeActuelle.Priorite = CmbPriorite.SelectedItem?.ToString();
                _demandeActuelle.TypeProjet = CmbTypeProjet.SelectedItem?.ToString();
                _demandeActuelle.Categorie = CmbCategorie.SelectedItem?.ToString();
                _demandeActuelle.LeadProjet = CmbLeadProjet.SelectedItem?.ToString();
                _demandeActuelle.Ambition = CmbAmbition.SelectedItem?.ToString();
                _demandeActuelle.EstImplemente = ChkEstImplemente.IsChecked == true;
                
                _demandeActuelle.GainsTemps = TxtGainsTemps.Text?.Trim();
                _demandeActuelle.GainsFinanciers = TxtGainsFinanciers.Text?.Trim();
                
                // Drivers (multi-sélection -> JSON)
                var driversSelectionnes = new List<string>();
                if (ChkDriverAutomation.IsChecked == true) driversSelectionnes.Add("Automation");
                if (ChkDriverEfficiency.IsChecked == true) driversSelectionnes.Add("Efficiency Gains");
                if (ChkDriverOptimization.IsChecked == true) driversSelectionnes.Add("Process Optimization");
                if (ChkDriverStandardization.IsChecked == true) driversSelectionnes.Add("Standardization");
                if (ChkDriverAucun.IsChecked == true) driversSelectionnes.Add("Aucun");
                _demandeActuelle.Drivers = driversSelectionnes.Count > 0 ? System.Text.Json.JsonSerializer.Serialize(driversSelectionnes) : null;
                
                // Bénéficiaires (multi-sélection -> JSON)
                var beneficiairesSelectionnes = new List<string>();
                if (ChkBenefSGI.IsChecked == true) beneficiairesSelectionnes.Add("SGI");
                if (ChkBenefTFSC.IsChecked == true) beneficiairesSelectionnes.Add("TFSC");
                if (ChkBenefTransversal.IsChecked == true) beneficiairesSelectionnes.Add("Transversal");
                _demandeActuelle.Beneficiaires = beneficiairesSelectionnes.Count > 0 ? System.Text.Json.JsonSerializer.Serialize(beneficiairesSelectionnes) : null;
                
                // Équipes Assignées (multi-sélection -> List<int>)
                var equipesSelectionnees = new List<int>();
                foreach (CheckBox chk in PanelEquipes.Children)
                {
                    if (chk.IsChecked == true && chk.Tag is int equipeId)
                    {
                        equipesSelectionnees.Add(equipeId);
                    }
                }
                _demandeActuelle.EquipesAssigneesIds = equipesSelectionnees;

                // Enregistrement
                _database.AddOrUpdateDemande(_demandeActuelle);

                // Historique
                var utilisateur = _authService.CurrentUser;
                var historique = new HistoriqueModification
                {
                    TypeEntite = "Demande",
                    EntiteId = _demandeActuelle.Id,
                    UtilisateurId = utilisateur != null ? utilisateur.Id : 0,
                    DateModification = DateTime.Now,
                    TypeModification = _demandeId.HasValue ? Domain.TypeModification.Modification : Domain.TypeModification.Creation,
                    NouvelleValeur = _demandeActuelle.Titre,
                    ChampModifie = _demandeId.HasValue ? "Modification complète" : "Création"
                };
                _database.AddHistorique(historique);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                TxtErreur.Text = string.Format("Erreur : {0}", ex.Message);
                TxtErreur.Visibility = Visibility.Visible;
            }
        }

        private void BtnValiderSpecifications_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(TxtSpecifications.Text))
                {
                    MessageBox.Show("Veuillez saisir les spécifications avant de valider.", 
                        "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Sauvegarder les spécifications
                _demandeActuelle.Specifications = TxtSpecifications.Text.Trim();
                
                // Passer au statut suivant
                _demandeActuelle.Statut = StatutDemande.EnAttenteChiffrage;
                
                // Enregistrer
                _database.AddOrUpdateDemande(_demandeActuelle);

                // Historique
                var utilisateur = _authService.CurrentUser;
                var historique = new HistoriqueModification
                {
                    TypeEntite = "Demande",
                    EntiteId = _demandeActuelle.Id,
                    UtilisateurId = utilisateur != null ? utilisateur.Id : 0,
                    DateModification = DateTime.Now,
                    TypeModification = Domain.TypeModification.Modification,
                    NouvelleValeur = "En attente de chiffrage",
                    AncienneValeur = "En attente de spécification",
                    ChampModifie = "Statut"
                };
                _database.AddHistorique(historique);

                MessageBox.Show("Les spécifications ont été validées.\nLa demande passe en attente de chiffrage.", 
                    "Succès", MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Erreur lors de la validation : {0}", ex.Message), 
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnAccepter_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show(
                    "Voulez-vous accepter cette demande ?\n\nElle passera en statut 'Acceptée' et pourra être planifiée en User Story.",
                    "Accepter la demande",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _demandeActuelle.Statut = StatutDemande.Acceptee;
                    _demandeActuelle.DateAcceptation = DateTime.Now;
                    
                    _database.AddOrUpdateDemande(_demandeActuelle);

                    // Historique
                    var utilisateur = _authService.CurrentUser;
                    var historique = new HistoriqueModification
                    {
                        TypeEntite = "Demande",
                        EntiteId = _demandeActuelle.Id,
                        UtilisateurId = utilisateur != null ? utilisateur.Id : 0,
                        DateModification = DateTime.Now,
                        TypeModification = Domain.TypeModification.Modification,
                        NouvelleValeur = "Acceptée",
                        AncienneValeur = "En attente validation manager",
                        ChampModifie = "Statut"
                    };
                    _database.AddHistorique(historique);

                    MessageBox.Show("La demande a été acceptée avec succès.", 
                        "Succès", MessageBoxButton.OK, MessageBoxImage.Information);

                    DialogResult = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Erreur lors de l'acceptation : {0}", ex.Message), 
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnRefuser_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Demander une justification
                var justificationWindow = new Window
                {
                    Title = "Refuser la demande",
                    Width = 500,
                    Height = 300,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = this,
                    ResizeMode = ResizeMode.NoResize
                };

                var grid = new Grid();
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var stackPanel = new StackPanel { Margin = new Thickness(20) };
                
                var lblTitre = new TextBlock 
                { 
                    Text = "Justification du refus *", 
                    FontWeight = FontWeights.Bold, 
                    Margin = new Thickness(0, 0, 0, 5) 
                };
                
                var txtJustification = new TextBox 
                { 
                    Height = 150, 
                    TextWrapping = TextWrapping.Wrap, 
                    AcceptsReturn = true, 
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    Padding = new Thickness(8)
                };

                stackPanel.Children.Add(lblTitre);
                stackPanel.Children.Add(txtJustification);
                
                Grid.SetRow(stackPanel, 0);
                grid.Children.Add(stackPanel);

                var btnPanel = new StackPanel 
                { 
                    Orientation = Orientation.Horizontal, 
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(20)
                };

                var btnAnnuler = new Button 
                { 
                    Content = "Annuler", 
                    Width = 100, 
                    Height = 35,
                    Margin = new Thickness(0, 0, 10, 0)
                };
                btnAnnuler.Click += (s, ev) => justificationWindow.DialogResult = false;

                var btnValider = new Button 
                { 
                    Content = "Refuser", 
                    Width = 100, 
                    Height = 35,
                    Background = new SolidColorBrush(Color.FromRgb(244, 67, 54)),
                    Foreground = Brushes.White
                };
                btnValider.Click += (s, ev) =>
                {
                    if (string.IsNullOrWhiteSpace(txtJustification.Text))
                    {
                        MessageBox.Show("Veuillez saisir une justification.", "Validation", 
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    justificationWindow.Tag = txtJustification.Text;
                    justificationWindow.DialogResult = true;
                };

                btnPanel.Children.Add(btnAnnuler);
                btnPanel.Children.Add(btnValider);
                
                Grid.SetRow(btnPanel, 1);
                grid.Children.Add(btnPanel);

                justificationWindow.Content = grid;

                if (justificationWindow.ShowDialog() == true)
                {
                    _demandeActuelle.Statut = StatutDemande.Refusee;
                    _demandeActuelle.JustificationRefus = justificationWindow.Tag.ToString();
                    
                    _database.AddOrUpdateDemande(_demandeActuelle);

                    // Historique
                    var utilisateur = _authService.CurrentUser;
                    var historique = new HistoriqueModification
                    {
                        TypeEntite = "Demande",
                        EntiteId = _demandeActuelle.Id,
                        UtilisateurId = utilisateur != null ? utilisateur.Id : 0,
                        DateModification = DateTime.Now,
                        TypeModification = Domain.TypeModification.Modification,
                        NouvelleValeur = "Refusée: " + _demandeActuelle.JustificationRefus,
                        AncienneValeur = "En attente validation manager",
                        ChampModifie = "Statut"
                    };
                    _database.AddHistorique(historique);

                    MessageBox.Show("La demande a été refusée.", 
                        "Refus", MessageBoxButton.OK, MessageBoxImage.Information);

                    DialogResult = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Erreur lors du refus : {0}", ex.Message), 
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

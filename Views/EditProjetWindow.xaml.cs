using System;
using System.Windows;
using BacklogManager.Domain;
using BacklogManager.Services;

namespace BacklogManager.Views
{
    public partial class EditProjetWindow : Window
    {
        private readonly BacklogService _backlogService;
        private readonly Projet _projet;
        private readonly bool _isNewProjet;

        public EditProjetWindow(BacklogService backlogService, Projet projet = null)
        {
            InitializeComponent();
            _backlogService = backlogService;
            _projet = projet ?? new Projet { Actif = true };
            _isNewProjet = projet == null;

            InitialiserComboBoxes();
            RemplirFormulaire();
            TxtTitre.Text = _isNewProjet ? "Nouveau Projet" : "Modifier Projet";
        }
        
        private void InitialiserComboBoxes()
        {
            var database = _backlogService.Database;
            
            // PHASE 2: Programmes
            var programmes = database.GetAllProgrammes().FindAll(p => p.Actif);
            var programmesCombo = new System.Collections.Generic.List<object>();
            programmesCombo.Add(new { Id = 0, Display = "-- Aucun programme --" });
            foreach (var p in programmes)
            {
                programmesCombo.Add(new { Id = p.Id, Display = string.Format("{0} - {1}", p.Code, p.Nom) });
            }
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
            var equipes = database.GetAllEquipes().FindAll(e => e.Actif);
            PanelEquipes.Children.Clear();
            foreach (var equipe in equipes)
            {
                var chk = new System.Windows.Controls.CheckBox
                {
                    Content = equipe.Nom,
                    Tag = equipe.Id,
                    Margin = new Thickness(0, 3, 0, 3)
                };
                PanelEquipes.Children.Add(chk);
            }
        }

        private void RemplirFormulaire()
        {
            // Remplir le formulaire soit pour un projet existant (édition), soit pour un nouveau projet pré-rempli
            TxtNomProjet.Text = _projet.Nom ?? "";
            TxtDescription.Text = _projet.Description ?? "";
            DpDateDebut.SelectedDate = _projet.DateDebut;
            DpDateFinPrevue.SelectedDate = _projet.DateFinPrevue;
            ChkActif.IsChecked = _projet.Actif;
            
            // PHASE 2: Charger les nouveaux champs
            if (_projet.ProgrammeId.HasValue)
                CmbProgramme.SelectedValue = _projet.ProgrammeId.Value;
            
            if (!string.IsNullOrEmpty(_projet.Priorite))
                CmbPriorite.SelectedItem = _projet.Priorite;
            
            if (!string.IsNullOrEmpty(_projet.TypeProjet))
                CmbTypeProjet.SelectedItem = _projet.TypeProjet;
            
            if (!string.IsNullOrEmpty(_projet.Categorie))
                CmbCategorie.SelectedItem = _projet.Categorie;
            
            if (!string.IsNullOrEmpty(_projet.LeadProjet))
                CmbLeadProjet.SelectedItem = _projet.LeadProjet;
            
            if (!string.IsNullOrEmpty(_projet.Ambition))
                CmbAmbition.SelectedItem = _projet.Ambition;
            
            ChkEstImplemente.IsChecked = _projet.EstImplemente;
            
            TxtGainsTemps.Text = _projet.GainsTemps ?? "";
            TxtGainsFinanciers.Text = _projet.GainsFinanciers ?? "";
            
            // Drivers (JSON array)
            if (!string.IsNullOrEmpty(_projet.Drivers))
            {
                try
                {
                    var drivers = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.List<string>>(_projet.Drivers);
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
            if (!string.IsNullOrEmpty(_projet.Beneficiaires))
            {
                try
                {
                    var beneficiaires = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.List<string>>(_projet.Beneficiaires);
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
            if (_projet.EquipesAssigneesIds != null && _projet.EquipesAssigneesIds.Count > 0)
            {
                foreach (System.Windows.Controls.CheckBox chk in PanelEquipes.Children)
                {
                    if (chk.Tag is int equipeId && _projet.EquipesAssigneesIds.Contains(equipeId))
                    {
                        chk.IsChecked = true;
                    }
                }
            }
        }

        private bool ValiderFormulaire()
        {
            TxtErreur.Text = "";
            BrdErreur.Visibility = Visibility.Collapsed;

            // Validation Nom
            if (string.IsNullOrWhiteSpace(TxtNomProjet.Text))
            {
                TxtErreur.Text = "Le nom du projet est obligatoire.";
                BrdErreur.Visibility = Visibility.Visible;
                TxtNomProjet.Focus();
                return false;
            }

            // Vérifier l'unicité du nom
            var projets = _backlogService.GetAllProjets();
            var existingProjet = projets.Find(p => 
                p.Nom.Equals(TxtNomProjet.Text.Trim(), StringComparison.OrdinalIgnoreCase) && 
                p.Id != _projet.Id);
            
            if (existingProjet != null)
            {
                TxtErreur.Text = "Un projet avec ce nom existe déjà.";
                BrdErreur.Visibility = Visibility.Visible;
                TxtNomProjet.Focus();
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
                _projet.Nom = TxtNomProjet.Text.Trim();
                _projet.Description = TxtDescription.Text.Trim();
                _projet.DateDebut = DpDateDebut.SelectedDate;
                _projet.DateFinPrevue = DpDateFinPrevue.SelectedDate;
                _projet.Actif = ChkActif.IsChecked ?? true;

                // PHASE 2: Enregistrer les nouveaux champs
                var progId = (int)CmbProgramme.SelectedValue;
                _projet.ProgrammeId = progId != 0 ? (int?)progId : null;
                
                _projet.Priorite = CmbPriorite.SelectedItem?.ToString();
                _projet.TypeProjet = CmbTypeProjet.SelectedItem?.ToString();
                _projet.Categorie = CmbCategorie.SelectedItem?.ToString();
                _projet.LeadProjet = CmbLeadProjet.SelectedItem?.ToString();
                _projet.Ambition = CmbAmbition.SelectedItem?.ToString();
                _projet.EstImplemente = ChkEstImplemente.IsChecked == true;
                
                _projet.GainsTemps = TxtGainsTemps.Text?.Trim();
                _projet.GainsFinanciers = TxtGainsFinanciers.Text?.Trim();
                
                // Drivers (multi-sélection -> JSON)
                var driversSelectionnes = new System.Collections.Generic.List<string>();
                if (ChkDriverAutomation.IsChecked == true) driversSelectionnes.Add("Automation");
                if (ChkDriverEfficiency.IsChecked == true) driversSelectionnes.Add("Efficiency Gains");
                if (ChkDriverOptimization.IsChecked == true) driversSelectionnes.Add("Process Optimization");
                if (ChkDriverStandardization.IsChecked == true) driversSelectionnes.Add("Standardization");
                if (ChkDriverAucun.IsChecked == true) driversSelectionnes.Add("Aucun");
                _projet.Drivers = driversSelectionnes.Count > 0 ? System.Text.Json.JsonSerializer.Serialize(driversSelectionnes) : null;
                
                // Bénéficiaires (multi-sélection -> JSON)
                var beneficiairesSelectionnes = new System.Collections.Generic.List<string>();
                if (ChkBenefSGI.IsChecked == true) beneficiairesSelectionnes.Add("SGI");
                if (ChkBenefTFSC.IsChecked == true) beneficiairesSelectionnes.Add("TFSC");
                if (ChkBenefTransversal.IsChecked == true) beneficiairesSelectionnes.Add("Transversal");
                _projet.Beneficiaires = beneficiairesSelectionnes.Count > 0 ? System.Text.Json.JsonSerializer.Serialize(beneficiairesSelectionnes) : null;
                
                // Équipes Assignées (multi-sélection -> List<int>)
                var equipesSelectionnees = new System.Collections.Generic.List<int>();
                foreach (System.Windows.Controls.CheckBox chk in PanelEquipes.Children)
                {
                    if (chk.IsChecked == true && chk.Tag is int equipeId)
                    {
                        equipesSelectionnees.Add(equipeId);
                    }
                }
                _projet.EquipesAssigneesIds = equipesSelectionnees;

                if (_isNewProjet)
                {
                    _projet.DateCreation = DateTime.Now;
                    // Assigner une couleur aléatoire depuis la palette
                    var random = new Random();
                    _projet.CouleurHex = Projet.CouleursPalette[random.Next(Projet.CouleursPalette.Length)];
                }

                _backlogService.SaveProjet(_projet);

                MessageBox.Show(_isNewProjet ? "Projet créé avec succès." : "Projet modifié avec succès.", 
                    "Succès", MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'enregistrement: {ex.Message}", 
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

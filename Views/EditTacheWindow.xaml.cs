using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using BacklogManager.Domain;
using BacklogManager.Services;

namespace BacklogManager.Views
{
    public partial class EditTacheWindow : Window
    {
        private readonly BacklogItem _tache;
        private readonly BacklogService _backlogService;
        private readonly PermissionService _permissionService;
        public bool Saved { get; private set; }

        public EditTacheWindow(BacklogItem tache, BacklogService backlogService, PermissionService permissionService)
        {
            InitializeComponent();
            _tache = tache;
            _backlogService = backlogService;
            _permissionService = permissionService;

            LoadData();
            ApplyPermissions();
        }

        private void LoadData()
        {
            // Charger les données dans les contrôles
            TitreTextBox.Text = _tache.Titre;
            DescriptionTextBox.Text = _tache.Description;
            TypeDemandeComboBox.SelectedItem = _tache.TypeDemande;
            PrioriteComboBox.SelectedItem = _tache.Priorite;
            StatutComboBox.SelectedItem = _tache.Statut;

            // Charger les devs
            var utilisateurs = _backlogService.GetAllUtilisateurs();
            DevComboBox.ItemsSource = utilisateurs;
            DevComboBox.SelectedValue = _tache.DevAssigneId;

            // Charger les projets
            var projets = _backlogService.GetAllProjets();
            ProjetComboBox.ItemsSource = projets;
            ProjetComboBox.SelectedValue = _tache.ProjetId;

            // Complexité
            ComplexiteComboBox.ItemsSource = new List<string> { "1", "2", "3", "5", "8", "13", "21" };
            ComplexiteComboBox.SelectedItem = _tache.Complexite?.ToString();

            // Chiffrage (convertir heures en jours: 7h = 1j)
            ChiffrageTextBox.Text = _tache.ChiffrageHeures.HasValue ? (_tache.ChiffrageHeures.Value / 7.0).ToString("0.#") : "";

            // Temps réel passé (en heures)
            TempsReelTextBox.Text = _tache.TempsReelHeures.HasValue ? _tache.TempsReelHeures.Value.ToString("0.#") : "0";
            
            // Calculer et afficher la progression
            UpdateProgression();

            // Événements pour recalculer la progression en temps réel
            ChiffrageTextBox.TextChanged += (s, e) => UpdateProgression();
            TempsReelTextBox.TextChanged += (s, e) => UpdateProgression();

            // Date fin attendue
            DateFinDatePicker.SelectedDate = _tache.DateFinAttendue;
        }

        private void ApplyPermissions()
        {
            if (_permissionService == null) return;

            // Vérifier si l'utilisateur peut modifier cette tâche
            bool peutModifier = _permissionService.PeutModifierTache(_tache);

            // Désactiver les champs si pas de permission de modification
            TitreTextBox.IsReadOnly = !peutModifier;
            DescriptionTextBox.IsReadOnly = !peutModifier;
            TypeDemandeComboBox.IsEnabled = peutModifier;
            StatutComboBox.IsEnabled = peutModifier;

            // Priorité seulement si PeutPrioriser
            PrioriteComboBox.IsEnabled = peutModifier && _permissionService.PeutPrioriser;

            // Dev assigné seulement si PeutAssignerDev
            DevComboBox.IsEnabled = peutModifier && _permissionService.PeutAssignerDev;

            // Complexité seulement si PeutChiffrer
            ComplexiteComboBox.IsEnabled = peutModifier && _permissionService.PeutChiffrer;
            ChiffrageTextBox.IsReadOnly = !peutModifier || !_permissionService.PeutChiffrer;
            TempsReelTextBox.IsReadOnly = !peutModifier;

            // Autres champs
            ProjetComboBox.IsEnabled = peutModifier;
            DateFinDatePicker.IsEnabled = peutModifier;

            // Changer le titre si lecture seule
            if (!peutModifier)
            {
                Title = "Consultation de tâche (lecture seule)";
            }
        }

        private void UpdateProgression()
        {
            if (double.TryParse(ChiffrageTextBox.Text, out double chiffrageJours) && chiffrageJours > 0)
            {
                double chiffrageHeures = chiffrageJours * 7.0; // Convertir jours en heures
                
                if (double.TryParse(TempsReelTextBox.Text, out double tempsReelHeures))
                {
                    double progression = Math.Min(100, (tempsReelHeures / chiffrageHeures) * 100);
                    double restantHeures = Math.Max(0, chiffrageHeures - tempsReelHeures);
                    double restantJours = restantHeures / 7.0;
                    
                    ProgressionTextBlock.Text = string.Format("Progression: {0:F0}% | Reste: {1:F1}j", progression, restantJours);
                    
                    // Changer la couleur selon la progression
                    if (progression >= 100)
                        ProgressionTextBlock.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(76, 175, 80)); // Vert
                    else if (progression >= 75)
                        ProgressionTextBlock.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 145, 90)); // BNP Green
                    else if (progression >= 50)
                        ProgressionTextBlock.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 124, 0)); // Orange
                    else
                        ProgressionTextBlock.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(211, 47, 47)); // Rouge
                }
                else
                {
                    ProgressionTextBlock.Text = "Progression: 0%";
                }
            }
            else
            {
                ProgressionTextBlock.Text = "Non estimé";
                ProgressionTextBlock.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(102, 102, 102)); // Gris
            }
        }

        private void Enregistrer_Click(object sender, RoutedEventArgs e)
        {
            // Vérifier les permissions avant d'enregistrer
            if (_permissionService != null && !_permissionService.PeutModifierTache(_tache))
            {
                MessageBox.Show("Vous n'avez pas les droits pour modifier cette tâche.",
                    "Accès refusé", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(TitreTextBox.Text))
            {
                MessageBox.Show("Le titre est obligatoire.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Mettre à jour la tâche
            _tache.Titre = TitreTextBox.Text;
            _tache.Description = DescriptionTextBox.Text;
            _tache.TypeDemande = (TypeDemande)TypeDemandeComboBox.SelectedItem;
            _tache.Statut = (Statut)StatutComboBox.SelectedItem;

            // Priorité (si autorisé)
            if (_permissionService == null || _permissionService.PeutPrioriser)
            {
                _tache.Priorite = (Priorite)PrioriteComboBox.SelectedItem;
            }

            // Dev assigné (si autorisé)
            if (_permissionService == null || _permissionService.PeutAssignerDev)
            {
                _tache.DevAssigneId = (int?)DevComboBox.SelectedValue;
            }

            _tache.ProjetId = (int?)ProjetComboBox.SelectedValue;

            // Complexité (si autorisé)
            if (_permissionService == null || _permissionService.PeutChiffrer)
            {
                if (ComplexiteComboBox.SelectedItem != null && int.TryParse(ComplexiteComboBox.SelectedItem.ToString(), out int complexite))
                {
                    _tache.Complexite = complexite;
                }

                if (double.TryParse(ChiffrageTextBox.Text, out double chiffrageJours))
                {
                    _tache.ChiffrageHeures = chiffrageJours * 7; // Convertir jours en heures (7h par jour)
                }
            }

            // Temps réel passé (en heures)
            if (double.TryParse(TempsReelTextBox.Text, out double tempsReelHeures))
            {
                _tache.TempsReelHeures = tempsReelHeures;
            }
            else
            {
                _tache.TempsReelHeures = 0;
            }

            _tache.DateFinAttendue = DateFinDatePicker.SelectedDate;
            _tache.DateDerniereMaj = DateTime.Now;

            // Sauvegarder
            _backlogService.SaveBacklogItem(_tache);

            Saved = true;
            DialogResult = true;
            Close();
        }

        private void Annuler_Click(object sender, RoutedEventArgs e)
        {
            Saved = false;
            DialogResult = false;
            Close();
        }
    }
}

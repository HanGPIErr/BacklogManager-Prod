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
        private readonly CRAService _craService;
        private readonly PermissionService _permissionService;
        public bool Saved { get; private set; }

        public EditTacheWindow(BacklogItem tache, BacklogService backlogService, PermissionService permissionService, CRAService craService = null)
        {
            InitializeComponent();
            _tache = tache;
            _backlogService = backlogService;
            _permissionService = permissionService;
            _craService = craService;

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

            // Chiffrage (en jours: 1j = 8h)
            ChiffrageTextBox.Text = _tache.ChiffrageHeures.HasValue ? (_tache.ChiffrageHeures.Value / 8.0).ToString("0.#") : "";

            // Date de début (visible uniquement si statut >= En cours)
            bool enCours = _tache.Statut == Statut.EnCours || _tache.Statut == Statut.Test || _tache.Statut == Statut.Termine;
            DateDebutLabel.Visibility = enCours ? Visibility.Visible : Visibility.Collapsed;
            DateDebutDatePicker.Visibility = enCours ? Visibility.Visible : Visibility.Collapsed;
            DateDebutDatePicker.SelectedDate = _tache.DateDebut;

            // Temps réel passé (calculé automatiquement depuis les CRA, affiché en jours)
            double tempsReelHeures = 0;
            if (_craService != null && _tache.Id > 0)
            {
                tempsReelHeures = _craService.GetTempsReelTache(_tache.Id);
            }
            double tempsReelJours = tempsReelHeures / 8.0;
            TempsReelTextBox.Text = tempsReelJours > 0 ? tempsReelJours.ToString("0.#") : "0";
            
            // Calculer et afficher la progression
            UpdateProgression();

            // Événement pour recalculer la progression
            ChiffrageTextBox.TextChanged += (s, e) => UpdateProgression();
            StatutComboBox.SelectionChanged += (s, e) => 
            {
                bool estEnCours = StatutComboBox.SelectedItem != null && 
                                  ((Statut)StatutComboBox.SelectedItem == Statut.EnCours || 
                                   (Statut)StatutComboBox.SelectedItem == Statut.Test || 
                                   (Statut)StatutComboBox.SelectedItem == Statut.Termine);
                DateDebutLabel.Visibility = estEnCours ? Visibility.Visible : Visibility.Collapsed;
                DateDebutDatePicker.Visibility = estEnCours ? Visibility.Visible : Visibility.Collapsed;
            };

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

            // Chiffrage seulement si PeutChiffrer
            ChiffrageTextBox.IsReadOnly = !peutModifier || !_permissionService.PeutChiffrer;

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
                if (double.TryParse(TempsReelTextBox.Text, out double tempsReelJours))
                {
                    double progression = Math.Min(100, (tempsReelJours / chiffrageJours) * 100);
                    double restantJours = Math.Max(0, chiffrageJours - tempsReelJours);
                    
                    ProgressionTextBlock.Text = string.Format("Progression: {0:F0}% | Reste: {1:F1}j", progression, restantJours);
                    
                    // Changer la couleur selon la progression
                    if (progression >= 100)
                        ProgressionTextBlock.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(76, 175, 80)); // Vert
                    else if (progression >= 90)
                        ProgressionTextBlock.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 124, 0)); // Orange (en risque)
                    else if (progression >= 75)
                        ProgressionTextBlock.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 145, 90)); // BNP Green
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

            // Chiffrage (si autorisé) - convertir jours en heures (1j = 8h)
            if (_permissionService == null || _permissionService.PeutChiffrer)
            {
                if (double.TryParse(ChiffrageTextBox.Text, out double chiffrageJours))
                {
                    _tache.ChiffrageHeures = chiffrageJours * 8.0; // Convertir jours -> heures
                }
            }

            // Date de début
            _tache.DateDebut = DateDebutDatePicker.SelectedDate;

            // Note: Le temps réel est calculé automatiquement depuis les CRA, pas saisi manuellement

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

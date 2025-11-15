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
        public bool Saved { get; private set; }

        public EditTacheWindow(BacklogItem tache, BacklogService backlogService)
        {
            InitializeComponent();
            _tache = tache;
            _backlogService = backlogService;

            LoadData();
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

            // Date fin attendue
            DateFinDatePicker.SelectedDate = _tache.DateFinAttendue;
        }

        private void Enregistrer_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TitreTextBox.Text))
            {
                MessageBox.Show("Le titre est obligatoire.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Mettre à jour la tâche
            _tache.Titre = TitreTextBox.Text;
            _tache.Description = DescriptionTextBox.Text;
            _tache.TypeDemande = (TypeDemande)TypeDemandeComboBox.SelectedItem;
            _tache.Priorite = (Priorite)PrioriteComboBox.SelectedItem;
            _tache.Statut = (Statut)StatutComboBox.SelectedItem;
            _tache.DevAssigneId = (int?)DevComboBox.SelectedValue;
            _tache.ProjetId = (int?)ProjetComboBox.SelectedValue;

            if (ComplexiteComboBox.SelectedItem != null && int.TryParse(ComplexiteComboBox.SelectedItem.ToString(), out int complexite))
            {
                _tache.Complexite = complexite;
            }

            if (double.TryParse(ChiffrageTextBox.Text, out double chiffrageJours))
            {
                _tache.ChiffrageHeures = chiffrageJours * 7; // Convertir jours en heures (7h par jour)
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

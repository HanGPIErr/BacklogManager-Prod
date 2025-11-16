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

            RemplirFormulaire();
            TxtTitre.Text = _isNewProjet ? "Nouveau Projet" : "Modifier Projet";
        }

        private void RemplirFormulaire()
        {
            if (!_isNewProjet)
            {
                TxtNomProjet.Text = _projet.Nom ?? "";
                TxtDescription.Text = _projet.Description ?? "";
                ChkActif.IsChecked = _projet.Actif;
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
                _projet.Actif = ChkActif.IsChecked ?? true;

                if (_isNewProjet)
                {
                    _projet.DateCreation = DateTime.Now;
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

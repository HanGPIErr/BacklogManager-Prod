using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BacklogManager.Domain;
using BacklogManager.Services;

namespace BacklogManager.Views
{
    public partial class GestionProjetsWindow : Window
    {
        private readonly BacklogService _backlogService;
        private Projet _projetEnEdition = null;

        public GestionProjetsWindow(BacklogService backlogService)
        {
            InitializeComponent();
            _backlogService = backlogService;
            ChargerProjets();
        }

        private void ChargerProjets()
        {
            var projets = _backlogService.GetAllProjets();
            LstProjets.ItemsSource = projets;
            TxtCountProjets.Text = string.Format("{0} projet(s)", projets.Count);
        }

        private void BtnAjouter_Click(object sender, RoutedEventArgs e)
        {
            var nom = TxtNomProjet.Text.Trim();
            if (string.IsNullOrEmpty(nom))
            {
                MessageBox.Show("Veuillez saisir un nom de projet.", "Validation", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var newProjet = new Projet
            {
                Id = 0,  // Laisser 0 pour auto-increment
                Nom = nom,
                Description = TxtDescriptionProjet.Text.Trim(),
                DateCreation = DateTime.Now,
                Actif = ChkActif.IsChecked ?? true
            };

            _backlogService.SaveProjet(newProjet);
            ViderFormulaire();
            ChargerProjets();

            MessageBox.Show(string.Format("Projet '{0}' ajouté avec succès!", nom), "Succès", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnModifier_Click(object sender, RoutedEventArgs e)
        {
            if (_projetEnEdition == null) return;

            var nom = TxtNomProjet.Text.Trim();
            if (string.IsNullOrEmpty(nom))
            {
                MessageBox.Show("Veuillez saisir un nom de projet.", "Validation", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _projetEnEdition.Nom = nom;
            _projetEnEdition.Description = TxtDescriptionProjet.Text.Trim();
            _projetEnEdition.Actif = ChkActif.IsChecked ?? true;

            _backlogService.SaveProjet(_projetEnEdition);

            AnnulerEdition();
            ChargerProjets();

            MessageBox.Show("Projet modifié avec succès!", "Succès", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnAnnuler_Click(object sender, RoutedEventArgs e)
        {
            AnnulerEdition();
        }

        private void AnnulerEdition()
        {
            _projetEnEdition = null;
            ViderFormulaire();
            LstProjets.SelectedItem = null;

            BtnAjouter.Visibility = Visibility.Visible;
            BtnModifier.Visibility = Visibility.Collapsed;
            BtnAnnuler.Visibility = Visibility.Collapsed;
        }

        private void ViderFormulaire()
        {
            TxtNomProjet.Clear();
            TxtDescriptionProjet.Clear();
            ChkActif.IsChecked = true;
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var projet = button?.Tag as Projet;
            if (projet == null) return;

            _projetEnEdition = projet;
            TxtNomProjet.Text = projet.Nom;
            TxtDescriptionProjet.Text = projet.Description;
            ChkActif.IsChecked = projet.Actif;

            BtnAjouter.Visibility = Visibility.Collapsed;
            BtnModifier.Visibility = Visibility.Visible;
            BtnAnnuler.Visibility = Visibility.Visible;
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var projet = button?.Tag as Projet;
            if (projet == null) return;

            var result = MessageBox.Show(
                string.Format("Êtes-vous sûr de vouloir supprimer le projet '{0}' ?\n\nAttention: Les tâches liées à ce projet ne seront pas supprimées.", projet.Nom),
                "Confirmer la suppression",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                _backlogService.DeleteProjet(projet.Id);
                AnnulerEdition();
                ChargerProjets();

                MessageBox.Show("Projet supprimé avec succès!", "Succès", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnFermer_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}

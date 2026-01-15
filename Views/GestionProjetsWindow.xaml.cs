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

        public GestionProjetsWindow(BacklogService backlogService)
        {
            InitializeComponent();
            _backlogService = backlogService;
            
            // Initialiser les textes traduits
            InitialiserTextes();
            
            ChargerProjets();
        }

        private void InitialiserTextes()
        {
            // Textes de l'interface
            TxtProjectsTitle.Text = LocalizationService.Instance.GetString("Modal_Project_Title");
            BtnAjouter.Content = "➕ " + LocalizationService.Instance.GetString("Modal_Project_NewProject");

            // S'abonner aux changements de langue
            LocalizationService.Instance.PropertyChanged += (s, e) =>
            {
                TxtProjectsTitle.Text = LocalizationService.Instance.GetString("Modal_Project_Title");
                BtnAjouter.Content = "➕ " + LocalizationService.Instance.GetString("Modal_Project_NewProject");
                // Recharger pour mettre à jour le compteur
                ChargerProjets();
            };
        }

        private void ChargerProjets()
        {
            var projets = _backlogService.GetAllProjets();
            LstProjets.ItemsSource = projets;
            TxtCountProjets.Text = string.Format(LocalizationService.Instance.GetString("Modal_Project_Count"), projets.Count);
        }

        private void BtnAjouter_Click(object sender, RoutedEventArgs e)
        {
            var window = new EditProjetWindow(_backlogService);
            if (window.ShowDialog() == true)
            {
                ChargerProjets();
            }
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var projet = button?.Tag as Projet;
            if (projet == null) return;

            var window = new EditProjetWindow(_backlogService, projet);
            if (window.ShowDialog() == true)
            {
                ChargerProjets();
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var projet = button?.Tag as Projet;
            if (projet == null) return;

            var result = MessageBox.Show(
                $"Êtes-vous sûr de vouloir supprimer le projet '{projet.Nom}' ?\n\nAttention: Les tâches liées à ce projet ne seront pas supprimées.",
                "Confirmer la suppression",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                _backlogService.DeleteProjet(projet.Id);
                ChargerProjets();

                MessageBox.Show("Projet supprimé avec succès!", "Succès", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnFermer_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true; // Indiquer que des modifications ont pu être faites
            Close();
        }
    }
}

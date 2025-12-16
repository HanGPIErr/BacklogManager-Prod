using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BacklogManager.Domain;
using BacklogManager.ViewModels;

namespace BacklogManager.Views
{
    public partial class ProjetsView : UserControl
    {
        public ProjetsView()
        {
            InitializeComponent();
        }

        private void Border_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var border = sender as Border;
            if (border?.DataContext is ProjetItemViewModel projet)
            {
                var viewModel = border.Tag as ProjetsViewModel;
                if (viewModel != null)
                {
                    viewModel.SelectedProjet = projet;
                }
            }
        }

        private void ModifierTache_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                if (button == null)
                {
                    MessageBox.Show("Sender is not a button", "Debug");
                    return;
                }

                if (!(button.Tag is TacheEnrichie tacheEnrichie))
                {
                    MessageBox.Show($"Tag is not TacheEnrichie: {button.Tag?.GetType().Name ?? "null"}", "Debug");
                    return;
                }

                var viewModel = DataContext as ProjetsViewModel;
                if (viewModel == null)
                {
                    MessageBox.Show("DataContext is not ProjetsViewModel", "Debug");
                    return;
                }

                viewModel.ModifierTache(tacheEnrichie.Tache);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Erreur: {ex.Message}\n\n{ex.StackTrace}", "Erreur");
            }
        }

        private void SupprimerTache_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is TacheEnrichie tacheEnrichie)
            {
                var result = MessageBox.Show(
                    $"Êtes-vous sûr de vouloir supprimer la tâche '{tacheEnrichie.Titre}' ?",
                    "Confirmation de suppression",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    var viewModel = DataContext as ProjetsViewModel;
                    if (viewModel != null)
                    {
                        viewModel.SupprimerTache(tacheEnrichie.Tache);
                    }
                }
            }
        }

        private void BtnInverserTri_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as ProjetsViewModel;
            viewModel?.InverserTriTaches();
        }

        private void BtnModifierProjet_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var projet = button?.Tag as Projet;
            
            if (projet != null)
            {
                var viewModel = DataContext as ProjetsViewModel;
                viewModel?.ModifierProjet(projet);
            }
            
            // Empêcher la propagation du clic vers la Border parente
            e.Handled = true;
        }

        private void BtnSupprimerProjet_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var projet = button?.Tag as Projet;
            
            if (projet != null)
            {
                var result = MessageBox.Show(
                    $"Êtes-vous sûr de vouloir supprimer le projet '{projet.Nom}' ?\n\nToutes les tâches associées perdront leur lien avec ce projet.",
                    "Confirmation de suppression",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    var viewModel = DataContext as ProjetsViewModel;
                    viewModel?.SupprimerProjet(projet);
                }
            }
            
            // Empêcher la propagation du clic vers la Border parente
            e.Handled = true;
        }

        private void BtnDetailsProjet_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var projet = button?.Tag as Projet;
            
            if (projet != null)
            {
                var viewModel = DataContext as ProjetsViewModel;
                viewModel?.VoirDetailsProjet(projet);
            }
            
            // Empêcher la propagation du clic vers la Border parente
            e.Handled = true;
        }

        private void BtnDetailsTache_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is BacklogItem tache)
            {
                var viewModel = DataContext as ProjetsViewModel;
                viewModel?.VoirDetailsTache(tache);
            }
            
            // Empêcher la propagation du clic vers la Border parente
            e.Handled = true;
        }
    }
}

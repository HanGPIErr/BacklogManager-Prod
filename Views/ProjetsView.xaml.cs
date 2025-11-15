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

                if (!(button.Tag is BacklogItem tache))
                {
                    MessageBox.Show($"Tag is not BacklogItem: {button.Tag?.GetType().Name ?? "null"}", "Debug");
                    return;
                }

                var viewModel = DataContext as ProjetsViewModel;
                if (viewModel == null)
                {
                    MessageBox.Show("DataContext is not ProjetsViewModel", "Debug");
                    return;
                }

                viewModel.ModifierTache(tache);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Erreur: {ex.Message}\n\n{ex.StackTrace}", "Erreur");
            }
        }

        private void SupprimerTache_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is BacklogItem tache)
            {
                var result = MessageBox.Show(
                    $"Êtes-vous sûr de vouloir supprimer la tâche '{tache.Titre}' ?",
                    "Confirmation de suppression",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    var viewModel = DataContext as ProjetsViewModel;
                    if (viewModel != null)
                    {
                        viewModel.SupprimerTache(tache);
                    }
                }
            }
        }
    }
}

using System.Windows.Controls;
using System.Windows;
using BacklogManager.ViewModels;
using System.Linq;
using System.Windows.Input;

namespace BacklogManager.Views
{
    public partial class SuiviCRAView : UserControl
    {
        public SuiviCRAView()
        {
            InitializeComponent();
        }

        private void TimelineScroll_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (sender is ScrollViewer scrollViewer && TimelineHeaderScroll != null)
            {
                TimelineHeaderScroll.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset);
            }
        }

        private void TacheTimeline_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is TacheTimelineViewModel tache)
            {
                // Ouvrir la fenêtre d'aperçu de la tâche
                var apercuWindow = new AperçuTacheTimelineWindow(tache);
                apercuWindow.Owner = Window.GetWindow(this);
                apercuWindow.ShowDialog();
            }
        }

        private void BtnAnalyserProjetIA_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as SuiviCRAViewModel;
            if (viewModel == null || viewModel.ProjetSelectionne == null)
            {
                MessageBox.Show(
                    "Veuillez sélectionner un projet avant de lancer l'analyse IA.",
                    "Aucun projet sélectionné",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (!viewModel.TachesProjetTimeline.Any())
            {
                MessageBox.Show(
                    "Ce projet ne contient aucune tâche à analyser.",
                    "Aucune tâche",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            // Ouvrir la fenêtre d'analyse IA
            var tachesBacklogItems = viewModel.TachesProjetTimeline
                .Select(t => t.BacklogItem)
                .ToList();
                
            var analyseWindow = new AnalyseProjetIAWindow(
                viewModel.ProjetSelectionne,
                tachesBacklogItems);
            analyseWindow.Owner = Window.GetWindow(this);
            analyseWindow.ShowDialog();
        }
    }
}


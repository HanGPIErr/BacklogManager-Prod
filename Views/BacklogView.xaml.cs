using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BacklogManager.ViewModels;

namespace BacklogManager.Views
{
    public partial class BacklogView : UserControl
    {
        private ProjetsView _projetsView;

        public BacklogView()
        {
            InitializeComponent();
            Loaded += BacklogView_Loaded;
        }

        private void BacklogView_Loaded(object sender, RoutedEventArgs e)
        {
            // Initialiser la vue projets si nécessaire
            if (_projetsView == null && DataContext is BacklogViewModel viewModel)
            {
                _projetsView = new ProjetsView();
                var projetsViewModel = new ProjetsViewModel(viewModel.BacklogService, viewModel.PermissionService, viewModel.CRAService);
                _projetsView.DataContext = projetsViewModel;
                // S'abonner à l'événement de création de projet
                viewModel.ProjetCreated += OnProjetCreated;
            }
        }

        private void OnProjetCreated(object sender, EventArgs e)
        {
            // Rafraîchir la vue des projets si elle est active
            if (_projetsView != null && MainContentFrame?.Visibility == Visibility.Visible)
            {
                var viewModel = _projetsView.DataContext as ProjetsViewModel;
                viewModel?.LoadData();
            }
        }

        private void BtnVueTaches_Click(object sender, RoutedEventArgs e)
        {
            // Activer le bouton Tâches
            BtnVueTaches.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00915A"));
            BtnVueTaches.Foreground = Brushes.White;
            BtnVueTaches.FontWeight = FontWeights.SemiBold;

            // Désactiver le bouton Projets
            BtnVueProjets.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0"));
            BtnVueProjets.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6D6D6D"));
            BtnVueProjets.FontWeight = FontWeights.Normal;

            // Afficher les boutons de tâches, cacher Nouveau Projet
            BtnNouvelleTache.Visibility = Visibility.Visible;
            BtnTacheSpeciale.Visibility = Visibility.Visible;
            BtnNouveauProjet.Visibility = Visibility.Collapsed;

            // Afficher la vue des tâches, cacher projets
            if (TachesScrollViewer != null)
            {
                TachesScrollViewer.Visibility = Visibility.Visible;
            }
            if (MainContentFrame != null)
            {
                MainContentFrame.Visibility = Visibility.Collapsed;
            }
            
            // Rafraîchir les données du Backlog
            if (DataContext is BacklogViewModel viewModel)
            {
                viewModel.LoadData();
            }
        }

        private void BtnVueProjets_Click(object sender, RoutedEventArgs e)
        {
            // Activer le bouton Projets
            BtnVueProjets.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00915A"));
            BtnVueProjets.Foreground = Brushes.White;
            BtnVueProjets.FontWeight = FontWeights.SemiBold;

            // Désactiver le bouton Tâches
            BtnVueTaches.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0"));
            BtnVueTaches.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6D6D6D"));
            BtnVueTaches.FontWeight = FontWeights.Normal;

            // Afficher le bouton Nouveau Projet, cacher les boutons de tâches
            BtnNouveauProjet.Visibility = Visibility.Visible;
            BtnNouvelleTache.Visibility = Visibility.Collapsed;
            BtnTacheSpeciale.Visibility = Visibility.Collapsed;

            // Cacher la vue des tâches
            if (TachesScrollViewer != null)
            {
                TachesScrollViewer.Visibility = Visibility.Collapsed;
            }

            // Afficher la vue des projets
            if (MainContentFrame != null && _projetsView != null)
            {
                MainContentFrame.Content = _projetsView;
                MainContentFrame.Visibility = Visibility.Visible;
                var viewModel = _projetsView.DataContext as ProjetsViewModel;
                viewModel?.LoadData();
            }
            else if (DataContext is BacklogViewModel viewModel)
            {
                _projetsView = new ProjetsView();
                var projetsViewModel = new ProjetsViewModel(viewModel.BacklogService, viewModel.PermissionService, viewModel.CRAService);
                _projetsView.DataContext = projetsViewModel;
                if (MainContentFrame != null)
                {
                    MainContentFrame.Content = _projetsView;
                    MainContentFrame.Visibility = Visibility.Visible;
                }
            }
        }

        private void BtnTacheSpeciale_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is BacklogViewModel viewModel)
            {
                // Créer une fenêtre de sélection du type de tâche spéciale
                var window = new TacheSpecialeWindow(viewModel.BacklogService, viewModel.PermissionService);
                if (window.ShowDialog() == true)
                {
                    // Rafraîchir le backlog
                    viewModel.LoadData();
                }
            }
        }

        private void BtnVueArchives_Click(object sender, RoutedEventArgs e)
        {
            // Activer le bouton Archives
            BtnVueArchives.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00915A"));
            BtnVueArchives.Foreground = Brushes.White;
            BtnVueArchives.FontWeight = FontWeights.SemiBold;

            // Désactiver les autres boutons
            BtnVueTaches.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0"));
            BtnVueTaches.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6D6D6D"));
            BtnVueTaches.FontWeight = FontWeights.Normal;
            
            BtnVueProjets.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0"));
            BtnVueProjets.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6D6D6D"));
            BtnVueProjets.FontWeight = FontWeights.Normal;

            // Cacher tous les boutons d'action
            BtnNouveauProjet.Visibility = Visibility.Collapsed;
            BtnNouvelleTache.Visibility = Visibility.Collapsed;
            BtnTacheSpeciale.Visibility = Visibility.Collapsed;

            // Cacher la vue des tâches
            if (TachesScrollViewer != null)
            {
                TachesScrollViewer.Visibility = Visibility.Collapsed;
            }

            // Afficher la vue des archives
            if (MainContentFrame != null && DataContext is BacklogViewModel backlogViewModel)
            {
                // Créer l'ArchivesViewModel avec les services du BacklogViewModel
                var archivesViewModel = new ArchivesViewModel(
                    backlogViewModel.BacklogService,
                    backlogViewModel.PermissionService,
                    backlogViewModel.CRAService);
                
                var archivesView = new ArchivesView();
                archivesView.DataContext = archivesViewModel;
                
                MainContentFrame.Content = archivesView;
                MainContentFrame.Visibility = Visibility.Visible;
            }
        }

        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.ContextMenu != null)
            {
                button.ContextMenu.PlacementTarget = button;
                button.ContextMenu.IsOpen = true;
            }
        }

        private void EditMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            if (menuItem?.Tag != null && DataContext is BacklogViewModel viewModel)
            {
                // Vérification de permission supplémentaire
                if (viewModel.PermissionService?.PeutModifierTaches == true)
                {
                    viewModel.EditCommand?.Execute(menuItem.Tag);
                }
            }
        }

        private void DeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            if (menuItem?.Tag != null && DataContext is BacklogViewModel viewModel)
            {
                // Vérification de permission supplémentaire
                if (viewModel.PermissionService?.PeutSupprimerTaches == true)
                {
                    viewModel.DeleteCommand?.Execute(menuItem.Tag);
                }
            }
        }
    }
}

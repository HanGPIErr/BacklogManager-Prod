using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BacklogManager.ViewModels;

namespace BacklogManager.Views
{
    public partial class BacklogView : UserControl
    {
        private Pages.ProjetsListPage _projetsPage;

        public BacklogView()
        {
            InitializeComponent();
            Loaded += BacklogView_Loaded;
        }

        private void BacklogView_Loaded(object sender, RoutedEventArgs e)
        {
            // Initialiser la page projets si nécessaire
            if (_projetsPage == null && DataContext is BacklogViewModel viewModel)
            {
                _projetsPage = new Pages.ProjetsListPage(viewModel.BacklogService, viewModel.PermissionService);
                // S'abonner à l'événement de création de projet
                viewModel.ProjetCreated += OnProjetCreated;
            }
        }

        private void OnProjetCreated(object sender, EventArgs e)
        {
            // Rafraîchir la page des projets si elle est active
            if (_projetsPage != null && MainContentFrame?.Visibility == Visibility.Visible)
            {
                _projetsPage.Refresh();
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

            // Afficher le bouton Nouvelle Tâche, cacher Nouveau Projet
            BtnNouvelleTache.Visibility = Visibility.Visible;
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

            // Afficher le bouton Nouveau Projet, cacher Nouvelle Tâche
            BtnNouveauProjet.Visibility = Visibility.Visible;
            BtnNouvelleTache.Visibility = Visibility.Collapsed;

            // Cacher la vue des tâches
            if (TachesScrollViewer != null)
            {
                TachesScrollViewer.Visibility = Visibility.Collapsed;
            }

            // Afficher la vue des projets
            if (MainContentFrame != null && _projetsPage != null)
            {
                MainContentFrame.Content = _projetsPage;
                MainContentFrame.Visibility = Visibility.Visible;
                _projetsPage.Refresh();
            }
            else if (DataContext is BacklogViewModel viewModel)
            {
                _projetsPage = new Pages.ProjetsListPage(viewModel.BacklogService, viewModel.PermissionService);
                if (MainContentFrame != null)
                {
                    MainContentFrame.Content = _projetsPage;
                    MainContentFrame.Visibility = Visibility.Visible;
                }
            }
        }
    }
}

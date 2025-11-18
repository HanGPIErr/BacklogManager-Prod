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
                // S'abonner à l'événement de clic sur un projet
                _projetsPage.ProjetClicked += OnProjetClicked;
            }
        }

        private void OnProjetClicked(object sender, Domain.Projet projet)
        {
            if (DataContext is BacklogViewModel viewModel)
            {
                // Définir le filtre de projet
                viewModel.SelectedProjetId = projet.Id;
                
                // Basculer vers l'onglet Tâches
                BtnVueTaches_Click(null, null);
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
    }
}

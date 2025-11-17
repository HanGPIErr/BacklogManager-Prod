using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using BacklogManager.Domain;
using BacklogManager.Services;

namespace BacklogManager.Views.Pages
{
    public partial class ProjetsListPage : Page
    {
        private readonly BacklogService _backlogService;
        private readonly PermissionService _permissionService;

        // Événement pour notifier qu'un projet a été cliqué
        public event EventHandler<Projet> ProjetClicked;

        public ProjetsListPage(BacklogService backlogService, PermissionService permissionService = null)
        {
            InitializeComponent();
            _backlogService = backlogService;
            _permissionService = permissionService;
            LoadProjets();
        }

        private void LoadProjets()
        {
            var projets = _backlogService.GetAllProjets();
            var projetsList = new ObservableCollection<Projet>(projets);
            ProjetsItemsControl.ItemsSource = projetsList;
        }

        private void BtnMenu_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var projet = button?.Tag as Projet;
            
            if (projet == null) return;

            var contextMenu = new ContextMenu();
            
            // Modifier
            var editItem = new MenuItem { Header = "✏️ Modifier" };
            editItem.Click += (s, args) => EditProjet(projet);
            contextMenu.Items.Add(editItem);
            
            // Archiver/Réactiver
            if (projet.Actif)
            {
                var archiveItem = new MenuItem { Header = "📦 Archiver" };
                archiveItem.Click += (s, args) => ToggleProjetStatus(projet);
                contextMenu.Items.Add(archiveItem);
            }
            else
            {
                var activateItem = new MenuItem { Header = "✓ Réactiver" };
                activateItem.Click += (s, args) => ToggleProjetStatus(projet);
                contextMenu.Items.Add(activateItem);
            }
            
            // Separator
            contextMenu.Items.Add(new Separator());
            
            // Supprimer
            var deleteItem = new MenuItem { Header = "🗑️ Supprimer", Foreground = System.Windows.Media.Brushes.Red };
            deleteItem.Click += (s, args) => DeleteProjet(projet);
            contextMenu.Items.Add(deleteItem);
            
            contextMenu.PlacementTarget = button;
            contextMenu.IsOpen = true;
        }

        private void EditProjet(Projet projet)
        {
            var editWindow = new EditProjetWindow(_backlogService, projet);
            
            var mainWindow = Application.Current.MainWindow;
            if (mainWindow != null && mainWindow != editWindow)
            {
                editWindow.Owner = mainWindow;
            }
            
            if (editWindow.ShowDialog() == true)
            {
                LoadProjets();
            }
        }

        private void ToggleProjetStatus(Projet projet)
        {
            projet.Actif = !projet.Actif;
            _backlogService.SaveProjet(projet);
            LoadProjets();
        }

        private void DeleteProjet(Projet projet)
        {
            var result = MessageBox.Show(
                $"Êtes-vous sûr de vouloir supprimer le projet '{projet.Nom}' ?\n\nCette action est irréversible.",
                "Confirmation de suppression",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                _backlogService.DeleteProjet(projet.Id);
                LoadProjets();
            }
        }

        public void Refresh()
        {
            LoadProjets();
        }

        private void ProjetCard_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var border = sender as System.Windows.Controls.Border;
            var projet = border?.Tag as Projet;
            
            if (projet != null)
            {
                // Déclencher l'événement pour notifier BacklogView
                ProjetClicked?.Invoke(this, projet);
            }
        }
    }
}

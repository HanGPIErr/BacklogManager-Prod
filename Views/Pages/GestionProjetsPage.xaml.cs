using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BacklogManager.Services;

namespace BacklogManager.Views.Pages
{
    public partial class GestionProjetsPage : Page
    {
        private readonly IDatabase _database;
        private readonly BacklogService _backlogService;
        private readonly AuditLogService _auditLogService;

        public GestionProjetsPage(IDatabase database, AuditLogService auditLogService = null)
        {
            InitializeComponent();
            _database = database;
            _auditLogService = auditLogService;
            _backlogService = new BacklogService(_database, _auditLogService);
            
            // Initialiser les textes traduits
            InitialiserTextes();
            
            ChargerProjets();
        }

        private void InitialiserTextes()
        {
            // Textes de l'interface
            TxtShow.Text = LocalizationService.Instance.GetString("Projects_Show");
            CmbItemActifs.Content = "📂 " + LocalizationService.Instance.GetString("Projects_ActiveProjects");
            CmbItemArchives.Content = "📦 " + LocalizationService.Instance.GetString("Projects_ArchivedProjects");
            CmbItemTous.Content = "📁 " + LocalizationService.Instance.GetString("Projects_AllProjects");

            // S'abonner aux changements de langue
            LocalizationService.Instance.PropertyChanged += (s, e) =>
            {
                TxtShow.Text = LocalizationService.Instance.GetString("Projects_Show");
                CmbItemActifs.Content = "📂 " + LocalizationService.Instance.GetString("Projects_ActiveProjects");
                CmbItemArchives.Content = "📦 " + LocalizationService.Instance.GetString("Projects_ArchivedProjects");
                CmbItemTous.Content = "📁 " + LocalizationService.Instance.GetString("Projects_AllProjects");
            };
        }

        private void ChargerProjets()
        {
            try
            {
                var projets = _backlogService.GetProjetsActifs(); // Par défaut, afficher les projets actifs
                LstProjets.ItemsSource = projets;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des projets: {ex.Message}", 
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CmbFiltreStatut_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbFiltreStatut == null || CmbFiltreStatut.SelectedIndex == -1)
                return;

            if (_backlogService == null || LstProjets == null)
                return;

            try
            {
                System.Collections.Generic.List<Domain.Projet> projets = null;
                
                switch (CmbFiltreStatut.SelectedIndex)
                {
                    case 0: // Projets actifs
                        projets = _backlogService.GetProjetsActifs();
                        break;
                    case 1: // Projets archivés
                        projets = _backlogService.GetProjetsArchives();
                        break;
                    case 2: // Tous les projets
                        projets = _backlogService.GetAllProjets();
                        break;
                }
                
                LstProjets.ItemsSource = null;
                LstProjets.ItemsSource = projets;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du filtrage des projets: {ex.Message}", 
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnNouveauProjet_Click(object sender, RoutedEventArgs e)
        {
            var window = new GestionProjetsWindow(_backlogService);
            if (window.ShowDialog() == true)
            {
                BtnActualiser_Click(sender, e); // Rafraîchir avec le filtre actuel
            }
        }

        private void BtnModifierProjet_Click(object sender, RoutedEventArgs e)
        {
            if (LstProjets.SelectedItem == null)
            {
                MessageBox.Show("Veuillez sélectionner un projet", "Attention", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            var projet = LstProjets.SelectedItem as Domain.Projet;
            var window = new EditProjetWindow(_backlogService, projet);
            window.ShowDialog();
            BtnActualiser_Click(sender, e); // Rafraîchir avec le filtre actuel
        }

        private void BtnActualiser_Click(object sender, RoutedEventArgs e)
        {
            // Recharger en fonction du filtre actuel
            if (CmbFiltreStatut != null && CmbFiltreStatut.SelectedIndex >= 0)
            {
                CmbFiltreStatut_SelectionChanged(CmbFiltreStatut, null);
            }
            else
            {
                ChargerProjets();
            }
        }

        private void LstProjets_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (LstProjets.SelectedItem == null)
                return;

            var projet = LstProjets.SelectedItem as Domain.Projet;
            if (projet != null)
            {
                try
                {
                    var detailsWindow = new ProjetDetailsWindow(projet, _backlogService);
                    detailsWindow.ShowDialog();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de l'ouverture des détails du projet :\n{ex.Message}",
                        "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}

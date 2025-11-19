using System;
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
            ChargerProjets();
        }

        private void ChargerProjets()
        {
            try
            {
                var projets = _database.GetProjets();
                LstProjets.ItemsSource = projets;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des projets: {ex.Message}", 
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnNouveauProjet_Click(object sender, RoutedEventArgs e)
        {
            var window = new GestionProjetsWindow(_backlogService);
            window.ShowDialog();
            ChargerProjets();
        }

        private void BtnModifierProjet_Click(object sender, RoutedEventArgs e)
        {
            if (LstProjets.SelectedItem == null)
            {
                MessageBox.Show("Veuillez sélectionner un projet", "Attention", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            var projet = LstProjets.SelectedItem as Domain.Projet;
            var window = new GestionProjetsWindow(_backlogService);
            window.ShowDialog();
            ChargerProjets();
        }

        private void BtnActualiser_Click(object sender, RoutedEventArgs e)
        {
            ChargerProjets();
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

using System;
using System.Windows;
using System.Windows.Controls;
using BacklogManager.Services;

namespace BacklogManager.Views.Pages
{
    public partial class GestionEquipePage : Page
    {
        private readonly IDatabase _database;
        private readonly BacklogService _backlogService;

        public GestionEquipePage(IDatabase database)
        {
            InitializeComponent();
            _database = database;
            _backlogService = new BacklogService(_database);
            ChargerEquipe();
        }

        private void ChargerEquipe()
        {
            try
            {
                var devs = _database.GetDevs();
                LstEquipe.ItemsSource = devs;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement de l'équipe: {ex.Message}", 
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnGererEquipe_Click(object sender, RoutedEventArgs e)
        {
            var window = new GestionEquipeWindow(_backlogService);
            window.ShowDialog();
            ChargerEquipe();
        }

        private void BtnActualiser_Click(object sender, RoutedEventArgs e)
        {
            ChargerEquipe();
        }
    }
}

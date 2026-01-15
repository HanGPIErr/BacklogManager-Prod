using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BacklogManager.Domain;
using BacklogManager.Services;

namespace BacklogManager.Views.Pages
{
    public partial class GestionEquipesPage : Page
    {
        private readonly IDatabase _database;
        private readonly EquipeService _equipeService;

        public GestionEquipesPage(IDatabase database)
        {
            InitializeComponent();
            _database = database;
            _equipeService = new EquipeService(_database);
            
            // Initialiser les textes traduits
            InitialiserTextes();
            
            ChargerEquipes();
        }

        private void InitialiserTextes()
        {
            // Textes des boutons avec icônes
            BtnNouvelleEquipe.Content = "➕ " + LocalizationService.Instance.GetString("Teams_NewTeam");
            BtnActualiserEquipes.Content = "🔄 " + LocalizationService.Instance.GetString("Teams_Refresh");

            // S'abonner aux changements de langue
            LocalizationService.Instance.PropertyChanged += (s, e) =>
            {
                BtnNouvelleEquipe.Content = "➕ " + LocalizationService.Instance.GetString("Teams_NewTeam");
                BtnActualiserEquipes.Content = "🔄 " + LocalizationService.Instance.GetString("Teams_Refresh");
                
                // Recharger les équipes pour mettre à jour "Aucun manager"
                ChargerEquipes();
            };
        }

        private void ChargerEquipes()
        {
            try
            {
                var equipes = _equipeService.GetAllEquipes();
                
                if (!equipes.Any())
                {
                    LstEquipes.Visibility = Visibility.Collapsed;
                    PanelAucuneEquipe.Visibility = Visibility.Visible;
                    return;
                }

                LstEquipes.Visibility = Visibility.Visible;
                PanelAucuneEquipe.Visibility = Visibility.Collapsed;

                // Récupérer tous les utilisateurs pour les managers
                var utilisateurs = _database.GetUtilisateurs();

                // Créer les ViewModels avec statistiques
                var equipesViewModel = equipes.Select(e => new EquipeViewModel
                {
                    Id = e.Id,
                    Nom = e.Nom,
                    Code = e.Code,
                    Description = !string.IsNullOrWhiteSpace(e.Description) ? e.Description : "Aucune description",
                    ManagerNom = e.ManagerId.HasValue 
                        ? utilisateurs.FirstOrDefault(u => u.Id == e.ManagerId.Value)?.Prenom + " " + utilisateurs.FirstOrDefault(u => u.Id == e.ManagerId.Value)?.Nom
                        : LocalizationService.Instance.GetString("Teams_NoManager"),
                    NombreMembres = _equipeService.GetNombreMembres(e.Id),
                    NombreProjets = _equipeService.GetNombreProjetsActifs(e.Id)
                }).ToList();

                LstEquipes.ItemsSource = equipesViewModel;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des équipes: {ex.Message}", 
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnNouvelleEquipe_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var window = new EquipeEditionWindow(_database);
                window.Owner = Window.GetWindow(this);
                if (window.ShowDialog() == true)
                {
                    ChargerEquipes();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ouverture de la fenêtre d'édition: {ex.Message}", 
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnActualiser_Click(object sender, RoutedEventArgs e)
        {
            ChargerEquipes();
        }

        private void Equipe_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var border = sender as Border;
                var equipe = border?.DataContext as EquipeViewModel;
                if (equipe != null)
                {
                    var window = new EquipeEditionWindow(_database, equipe.Id);
                    window.Owner = Window.GetWindow(this);
                    if (window.ShowDialog() == true)
                    {
                        ChargerEquipes();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ouverture des détails de l'équipe: {ex.Message}", 
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ViewModel pour affichage
        public class EquipeViewModel
        {
            public int Id { get; set; }
            public string Nom { get; set; }
            public string Code { get; set; }
            public string Description { get; set; }
            public string ManagerNom { get; set; }
            public int NombreMembres { get; set; }
            public int NombreProjets { get; set; }
        }
    }
}

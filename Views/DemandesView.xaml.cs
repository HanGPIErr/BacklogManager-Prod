using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using BacklogManager.Domain;
using BacklogManager.Services;

namespace BacklogManager.Views
{
    public partial class DemandesView : UserControl
    {
        private readonly IDatabase _database;
        private readonly AuthenticationService _authService;
        private List<DemandeViewModel> _toutesLesDemandes;

        public DemandesView()
        {
            InitializeComponent();
            _database = new SqliteDatabase();
            _authService = new AuthenticationService(_database);
            
            InitialiserFiltres();
            ChargerDemandes();
            
            BtnNouvelleDemande.Click += BtnNouvelleDemande_Click;
            BtnRefresh.Click += (s, e) => ChargerDemandes();
            CmbFiltreStatut.SelectionChanged += (s, e) => AppliquerFiltres();
            CmbFiltreCriticite.SelectionChanged += (s, e) => AppliquerFiltres();
            
            // Vérifier permissions
            VerifierPermissions();
        }

        private void InitialiserFiltres()
        {
            // Filtre statut
            var statutItems = new List<string> { "Tous" };
            statutItems.AddRange(Enum.GetNames(typeof(StatutDemande)));
            CmbFiltreStatut.ItemsSource = statutItems;
            CmbFiltreStatut.SelectedIndex = 0;
            
            // Filtre criticité
            var criticiteItems = new List<string> { "Toutes" };
            criticiteItems.AddRange(Enum.GetNames(typeof(Criticite)));
            CmbFiltreCriticite.ItemsSource = criticiteItems;
            CmbFiltreCriticite.SelectedIndex = 0;
        }

        private void VerifierPermissions()
        {
            var utilisateur = _authService.CurrentUser;
            if (utilisateur == null) return;

            var role = _authService.GetCurrentUserRole();
            if (role == null) return;

            // Seuls BA et Chef de Projet peuvent créer des demandes
            BtnNouvelleDemande.IsEnabled = role.PeutCreerDemandes;
        }

        private void ChargerDemandes()
        {
            try
            {
                var demandes = _database.GetDemandes();
                var utilisateurs = _database.GetUtilisateurs();

                _toutesLesDemandes = demandes.Select(d => new DemandeViewModel
                {
                    Id = d.Id,
                    Titre = d.Titre,
                    Description = d.Description,
                    Type = d.Type.ToString(),
                    Statut = FormatStatut(d.Statut),
                    Criticite = d.Criticite.ToString(),
                    Demandeur = ObtenirNomUtilisateur(d.DemandeurId, utilisateurs),
                    BusinessAnalyst = ObtenirNomUtilisateur(d.BusinessAnalystId, utilisateurs),
                    ChefProjet = ObtenirNomUtilisateur(d.ChefProjetId, utilisateurs),
                    DateCreation = d.DateCreation
                }).OrderByDescending(d => d.DateCreation).ToList();

                AppliquerFiltres();
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Erreur lors du chargement des demandes : {0}", ex.Message), 
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AppliquerFiltres()
        {
            if (_toutesLesDemandes == null) return;

            var filtered = _toutesLesDemandes.AsEnumerable();

            // Filtre statut
            if (CmbFiltreStatut.SelectedIndex > 0)
            {
                var statutSelectionne = CmbFiltreStatut.SelectedItem.ToString();
                filtered = filtered.Where(d => d.Statut.Contains(statutSelectionne));
            }

            // Filtre criticité
            if (CmbFiltreCriticite.SelectedIndex > 0)
            {
                var criticiteSelectionnee = CmbFiltreCriticite.SelectedItem.ToString();
                filtered = filtered.Where(d => d.Criticite == criticiteSelectionnee);
            }

            ListeDemandes.ItemsSource = filtered.ToList();
        }

        private string ObtenirNomUtilisateur(int? userId, List<Utilisateur> utilisateurs)
        {
            if (!userId.HasValue) return "Non assigné";
            var user = utilisateurs.FirstOrDefault(u => u.Id == userId.Value);
            return user != null ? string.Format("{0} {1}", user.Prenom, user.Nom) : "Non assigné";
        }

        private string FormatStatut(StatutDemande statut)
        {
            switch (statut)
            {
                case StatutDemande.EnAttenteSpecification:
                    return "En attente spécification";
                case StatutDemande.EnAttenteChiffrage:
                    return "En attente chiffrage";
                case StatutDemande.EnAttenteArbitrage:
                    return "En attente arbitrage";
                case StatutDemande.Acceptee:
                    return "Acceptée";
                case StatutDemande.PlanifieeEnUS:
                    return "Planifiée en US";
                case StatutDemande.EnCours:
                    return "En cours";
                case StatutDemande.Livree:
                    return "Livrée";
                case StatutDemande.Refusee:
                    return "Refusée";
                default:
                    return statut.ToString();
            }
        }

        private void BtnNouvelleDemande_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var window = new EditionDemandeWindow(_database, _authService);
                window.Owner = Window.GetWindow(this);
                if (window.ShowDialog() == true)
                {
                    ChargerDemandes();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Erreur lors de la création de la demande : {0}", ex.Message), 
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnDetailsClick(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is int demandeId)
            {
                AfficherDetails(demandeId);
            }
        }

        private void BtnModifierClick(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is int demandeId)
            {
                ModifierDemande(demandeId);
            }
        }

        private void BtnCommentairesClick(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is int demandeId)
            {
                AfficherCommentaires(demandeId);
            }
        }

        private void AfficherDetails(int demandeId)
        {
            try
            {
                var window = new DetailsDemandeWindow(demandeId, _database);
                window.Owner = Window.GetWindow(this);
                window.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Erreur lors de l'affichage des détails : {0}", ex.Message), 
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ModifierDemande(int demandeId)
        {
            try
            {
                var window = new EditionDemandeWindow(_database, _authService, demandeId);
                window.Owner = Window.GetWindow(this);
                if (window.ShowDialog() == true)
                {
                    ChargerDemandes();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Erreur lors de la modification de la demande : {0}", ex.Message), 
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AfficherCommentaires(int demandeId)
        {
            try
            {
                var window = new CommentairesWindow(demandeId, _database, _authService);
                window.Owner = Window.GetWindow(this);
                window.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Erreur lors de l'affichage des commentaires : {0}", ex.Message), 
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public class DemandeViewModel
    {
        public int Id { get; set; }
        public string Titre { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
        public string Statut { get; set; }
        public string Criticite { get; set; }
        public string Demandeur { get; set; }
        public string BusinessAnalyst { get; set; }
        public string ChefProjet { get; set; }
        public DateTime DateCreation { get; set; }
    }

    // Convertisseur pour les couleurs de statut
    public class StatutToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string statut)
            {
                if (statut.Contains("Livrée")) return new SolidColorBrush(Color.FromRgb(0, 166, 81)); // Vert
                if (statut.Contains("En cours")) return new SolidColorBrush(Color.FromRgb(0, 120, 215)); // Bleu
                if (statut.Contains("Acceptée")) return new SolidColorBrush(Color.FromRgb(16, 124, 16)); // Vert foncé
                if (statut.Contains("Refusée")) return new SolidColorBrush(Color.FromRgb(232, 17, 35)); // Rouge
                if (statut.Contains("attente")) return new SolidColorBrush(Color.FromRgb(255, 140, 0)); // Orange
                return new SolidColorBrush(Color.FromRgb(102, 102, 102)); // Gris
            }
            return new SolidColorBrush(Color.FromRgb(102, 102, 102));
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // Convertisseur pour les couleurs de criticité
    public class CriticiteToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string criticite)
            {
                switch (criticite)
                {
                    case "Bloquante":
                        return new SolidColorBrush(Color.FromRgb(232, 17, 35)); // Rouge
                    case "Haute":
                        return new SolidColorBrush(Color.FromRgb(255, 140, 0)); // Orange
                    case "Moyenne":
                        return new SolidColorBrush(Color.FromRgb(255, 185, 0)); // Jaune foncé
                    case "Basse":
                        return new SolidColorBrush(Color.FromRgb(0, 120, 215)); // Bleu
                    default:
                        return new SolidColorBrush(Color.FromRgb(102, 102, 102)); // Gris
                }
            }
            return new SolidColorBrush(Color.FromRgb(102, 102, 102));
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

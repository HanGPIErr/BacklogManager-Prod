using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BacklogManager.Domain;
using BacklogManager.Services;

namespace BacklogManager.Views.Pages
{
    public partial class DetailEquipeView : UserControl
    {
        private readonly IDatabase _database;
        private readonly int _equipeId;
        private readonly Action _retourCallback;
        private readonly AuthenticationService _authService;
        private Equipe _equipe;

        public DetailEquipeView(int equipeId, IDatabase database, Action retourCallback, AuthenticationService authService)
        {
            InitializeComponent();
            _equipeId = equipeId;
            _database = database;
            _retourCallback = retourCallback;
            _authService = authService;
            
            ChargerDetailsEquipe();
        }

        private void BtnRetour_Click(object sender, RoutedEventArgs e)
        {
            _retourCallback?.Invoke();
        }

        private void ChargerDetailsEquipe()
        {
            try
            {
                var equipes = _database.GetAllEquipes();
                var equipe = equipes.FirstOrDefault(eq => eq.Id == _equipeId);
                
                if (equipe == null)
                {
                    MessageBox.Show("Équipe introuvable.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    _retourCallback?.Invoke();
                    return;
                }

                _equipe = equipe;

                // Informations générales
                TxtNomEquipe.Text = equipe.Nom;
                TxtCodeEquipe.Text = string.Format("Code: {0}", equipe.Code);
                TxtDescriptionEquipe.Text = !string.IsNullOrWhiteSpace(equipe.Description) ? 
                    equipe.Description : "Aucune description";
                TxtPerimetre.Text = !string.IsNullOrWhiteSpace(equipe.PerimetreFonctionnel) ? 
                    equipe.PerimetreFonctionnel : "Non défini";
                TxtContact.Text = !string.IsNullOrWhiteSpace(equipe.Contact) ? 
                    equipe.Contact : "Non défini";

                // Afficher le bouton Planning VM uniquement pour Tactical Solutions
                if (equipe.Code == "TACTICAL_SOLUTIONS")
                {
                    BtnPlanningVM.Visibility = Visibility.Visible;
                }

                // Manager
                if (equipe.ManagerId.HasValue)
                {
                    var utilisateurs = _database.GetUtilisateurs();
                    var manager = utilisateurs.FirstOrDefault(u => u.Id == equipe.ManagerId.Value);
                    TxtManager.Text = manager != null ? 
                        string.Format("{0} {1}", manager.Prenom, manager.Nom) : "Non assigné";
                }
                else
                {
                    TxtManager.Text = "Non assigné";
                }

                // Membres de l'équipe
                var membres = _database.GetUtilisateurs().Where(u => u.EquipeId == _equipeId).ToList();
                TxtNbMembres.Text = membres.Count.ToString();
                
                if (membres.Any())
                {
                    var roles = _database.GetRoles();
                    var membresViewModel = membres.Select(m => {
                        var role = roles.FirstOrDefault(r => r.Id == m.RoleId);
                        return new MembreViewModel
                        {
                            Nom = m.Nom,
                            Prenom = m.Prenom,
                            Role = role != null ? role.Nom : "Non défini",
                            Email = m.Email,
                            Statut = m.Actif ? "Actif" : "Inactif"
                        };
                    }).ToList();
                    
                    GridMembres.ItemsSource = membresViewModel;
                    GridMembres.Visibility = Visibility.Visible;
                    TxtAucunMembre.Visibility = Visibility.Collapsed;
                }
                else
                {
                    GridMembres.Visibility = Visibility.Collapsed;
                    TxtAucunMembre.Visibility = Visibility.Visible;
                }

                // Projets associés
                var tousProjets = _database.GetProjets();
                var projetsEquipe = new List<Projet>();
                
                foreach (var projet in tousProjets.Where(p => p.Actif))
                {
                    // Vérifier si l'équipe est assignée à ce projet
                    if (projet.EquipesAssigneesIds != null && projet.EquipesAssigneesIds.Any(id => id == _equipeId))
                    {
                        projetsEquipe.Add(projet);
                    }
                }

                TxtNbProjets.Text = projetsEquipe.Count.ToString();
                
                if (projetsEquipe.Any())
                {
                    var projetsViewModel = projetsEquipe.Select(p => new ProjetViewModel
                    {
                        Id = p.Id,
                        Nom = p.Nom,
                        Description = !string.IsNullOrWhiteSpace(p.Description) ? 
                            (p.Description.Length > 100 ? p.Description.Substring(0, 100) + "..." : p.Description) : 
                            "Aucune description",
                        Statut = p.Actif ? "Actif" : "Inactif"
                    }).ToList();
                    
                    ListeProjets.ItemsSource = projetsViewModel;
                    ListeProjets.Visibility = Visibility.Visible;
                    TxtAucunProjet.Visibility = Visibility.Collapsed;
                }
                else
                {
                    ListeProjets.Visibility = Visibility.Collapsed;
                    TxtAucunProjet.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Erreur lors du chargement des détails : {0}", ex.Message), 
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ViewModels internes
        public class MembreViewModel
        {
            public string Nom { get; set; }
            public string Prenom { get; set; }
            public string Role { get; set; }
            public string Email { get; set; }
            public string Statut { get; set; }
        }

        public class ProjetViewModel
        {
            public int Id { get; set; }
            public string Nom { get; set; }
            public string Description { get; set; }
            public string Statut { get; set; }
        }

        private void Projet_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                var border = sender as Border;
                if (border == null)
                {
                    MessageBox.Show("Sender n'est pas un Border", "Debug", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                
                var projet = border.DataContext as ProjetViewModel;
                if (projet == null)
                {
                    MessageBox.Show("DataContext n'est pas un ProjetViewModel", "Debug", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                
                // Trouver le MainWindow et son ContentControl
                var mainWindow = Window.GetWindow(this) as MainWindow;
                if (mainWindow == null)
                {
                    MessageBox.Show("Impossible de trouver la fenêtre principale", "Erreur Navigation", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                
                var contentControl = mainWindow.FindName("MainContentControl") as ContentControl;
                if (contentControl == null)
                {
                    MessageBox.Show("Impossible de trouver le contrôle de contenu principal", "Erreur Navigation", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                
                // Capture des variables pour le callback
                var capturedEquipeId = _equipeId;
                var capturedDatabase = _database;
                var capturedRetourCallback = _retourCallback;
                var capturedAuthService = _authService;
                var capturedContentControl = contentControl;
                
                // Navigation vers le détail du projet (SteerCo View)
                var detailProjet = new DetailProjetSteerCoView(_database, projet.Id, () =>
                {
                    // Retour vers la page équipe - recréer une nouvelle instance
                    var nouvellePageEquipe = new DetailEquipeView(capturedEquipeId, capturedDatabase, capturedRetourCallback, capturedAuthService);
                    capturedContentControl.Content = nouvellePageEquipe;
                }, capturedAuthService);

                // Afficher la vue détail projet
                contentControl.Content = detailProjet;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la navigation: {ex.Message}\n\n{ex.StackTrace}", "Erreur", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnPlanningVM_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Trouver le MainWindow et son ContentControl
                var mainWindow = Window.GetWindow(this) as MainWindow;
                if (mainWindow == null)
                {
                    MessageBox.Show("Impossible de trouver la fenêtre principale", "Erreur", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var contentControl = mainWindow.FindName("MainContentControl") as ContentControl;
                if (contentControl == null)
                {
                    MessageBox.Show("Impossible de trouver le contrôle de contenu principal", "Erreur Navigation", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Créer le NotificationService
                var backlogService = new BacklogService(_database);
                var notificationService = new NotificationService(backlogService, _database);

                // Capture des variables pour le callback
                var capturedEquipeId = _equipeId;
                var capturedDatabase = _database;
                var capturedRetourCallback = _retourCallback;
                var capturedAuthService = _authService;
                var capturedContentControl = contentControl;

                // Navigation vers le planning VM
                var planningPage = new PlanningVMPage(_database, notificationService, _authService, _equipeId, () =>
                {
                    // Retour vers la page équipe - recréer une nouvelle instance
                    var nouvellePageEquipe = new DetailEquipeView(capturedEquipeId, capturedDatabase, capturedRetourCallback, capturedAuthService);
                    capturedContentControl.Content = nouvellePageEquipe;
                });

                // Afficher la page de planning VM
                contentControl.Content = planningPage;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ouverture du planning VM : {ex.Message}", 
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

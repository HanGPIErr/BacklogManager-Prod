using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BacklogManager.Domain;
using BacklogManager.Services;
using BacklogManager.ViewModels;

namespace BacklogManager.Views
{
    public partial class RessourcesEquipeDetailWindow : Window
    {
        private readonly IDatabase _database;
        private readonly EquipeService _equipeService;
        private List<RessourceDetailViewModel> _toutesLesRessources;
        private int? _equipeIdFiltre = null;

        public RessourcesEquipeDetailWindow(IDatabase database, int? equipeId = null)
        {
            InitializeComponent();
            _database = database;
            _equipeService = (Application.Current as App)?.EquipeService ?? new EquipeService(database);
            _equipeIdFiltre = equipeId;
            
            Loaded += RessourcesEquipeDetailWindow_Loaded;
        }

        private void RessourcesEquipeDetailWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ChargerDonnees();
        }

        private void ChargerDonnees()
        {
            try
            {
                // Charger toutes les ressources
                _toutesLesRessources = new List<RessourceDetailViewModel>();
                var utilisateurs = _database.GetUtilisateurs().Where(u => u.Actif).ToList();
                var equipes = _database.GetAllEquipes().Where(eq => eq.Actif).ToList();
                var projets = _database.GetProjets();
                var taches = _database.GetBacklogItems();
                var roles = _database.GetRoles();

                foreach (var user in utilisateurs)
                {
                    // Ignorer les utilisateurs sans équipe
                    if (user.EquipeId == null)
                        continue;

                    var equipe = equipes.FirstOrDefault(e => e.Id == user.EquipeId.Value);
                    var role = roles.FirstOrDefault(r => r.Id == user.RoleId);
                    
                    // Récupérer le manager de l'équipe
                    string nomManager = "Aucun";
                    if (equipe?.ManagerId != null)
                    {
                        var manager = utilisateurs.FirstOrDefault(u => u.Id == equipe.ManagerId.Value);
                        if (manager != null)
                        {
                            nomManager = $"{manager.Prenom} {manager.Nom}";
                        }
                    }
                    
                    // Projets où l'utilisateur a des tâches assignées
                    var projetsDev = projets.Where(p => 
                        taches.Any(t => t.ProjetId == p.Id && t.DevAssigneId == user.Id)
                    ).ToList();
                    
                    // Tâches actives (non terminées, non archivées)
                    var tachesActives = taches.Where(t => 
                        t.DevAssigneId == user.Id && 
                        t.Statut != Statut.Termine && 
                        !t.EstArchive
                    ).ToList();

                    // Calculer la charge par projet
                    var detailsProjets = new List<ProjetDetailViewModel>();
                    foreach (var projet in projetsDev)
                    {
                        var nbTachesProjet = taches.Where(t => 
                            t.ProjetId == projet.Id && 
                            t.DevAssigneId == user.Id &&
                            t.Statut != Statut.Termine &&
                            !t.EstArchive
                        ).Count();
                        
                        detailsProjets.Add(new ProjetDetailViewModel
                        {
                            Nom = projet.Nom,
                            NbTaches = nbTachesProjet
                        });
                    }

                    // Calculer niveau de charge
                    int nbProjets = projetsDev.Count;
                    string niveauCharge;
                    Color couleurCharge;
                    
                    if (nbProjets <= 3)
                    {
                        niveauCharge = "Faible";
                        couleurCharge = Color.FromRgb(76, 175, 80); // Vert
                    }
                    else if (nbProjets <= 6)
                    {
                        niveauCharge = "Moyenne";
                        couleurCharge = Color.FromRgb(255, 152, 0); // Orange
                    }
                    else
                    {
                        niveauCharge = "Élevée";
                        couleurCharge = Color.FromRgb(244, 67, 54); // Rouge
                    }

                    // Largeur barre proportionnelle (max 10 projets = 100%)
                    double largeurBarre = Math.Min((double)nbProjets / 10.0 * 380, 380);

                    _toutesLesRessources.Add(new RessourceDetailViewModel
                    {
                        Id = user.Id,
                        Nom = $"{user.Prenom} {user.Nom}",
                        Role = role?.Nom ?? "N/A",
                        EquipeId = user.EquipeId,
                        NomEquipe = equipe?.Nom ?? "Aucune équipe",
                        CodeEquipe = equipe?.Code ?? "N/A",
                        NomManager = nomManager,
                        NbProjets = nbProjets,
                        NbTachesActives = tachesActives.Count(),
                        NiveauCharge = niveauCharge,
                        CouleurCharge = couleurCharge,
                        CouleurChargeBrush = new SolidColorBrush(couleurCharge),
                        LargeurBarreCharge = largeurBarre,
                        ListeProjets = detailsProjets,
                        AucunProjet = nbProjets == 0
                    });
                }

                // Trier par équipe puis par nom
                _toutesLesRessources = _toutesLesRessources
                    .OrderBy(r => r.NomEquipe)
                    .ThenBy(r => r.Nom)
                    .ToList();

                // Créer les boutons de filtre par équipe
                CreerFiltresEquipes(equipes);

                // Appliquer le filtre initial si spécifié
                if (_equipeIdFiltre.HasValue)
                {
                    var equipe = equipes.FirstOrDefault(e => e.Id == _equipeIdFiltre.Value);
                    if (equipe != null)
                    {
                        TxtTitre.Text = $"👥 Ressources - {equipe.Nom}";
                        TxtSousTitre.Text = $"Vue des membres de l'équipe {equipe.Nom}";
                    }
                    FiltrerParEquipe(_equipeIdFiltre);
                }
                else
                {
                    AfficherToutesRessources();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des données : {ex.Message}", 
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CreerFiltresEquipes(List<Equipe> equipes)
        {
            PanelFiltresEquipes.Children.Clear();

            // Bouton "Toutes"
            var btnToutes = new Button
            {
                Content = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Children =
                    {
                        new TextBlock { Text = "🌐 ", FontSize = 12 },
                        new TextBlock 
                        { 
                            Text = "Toutes les équipes", 
                            FontSize = 12, 
                            FontWeight = FontWeights.SemiBold 
                        }
                    }
                },
                Style = _equipeIdFiltre == null ? 
                    (Style)FindResource("EquipeFilterButtonActive") : 
                    (Style)FindResource("EquipeFilterButton"),
                Tag = (int?)null
            };
            btnToutes.Click += BtnFiltreEquipe_Click;
            PanelFiltresEquipes.Children.Add(btnToutes);

            // Boutons par équipe
            foreach (var equipe in equipes.OrderBy(e => e.Nom))
            {
                var nbMembres = _toutesLesRessources.Count(r => r.EquipeId == equipe.Id);
                
                var btnEquipe = new Button
                {
                    Content = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Children =
                        {
                            new TextBlock { Text = "🏢 ", FontSize = 12 },
                            new TextBlock 
                            { 
                                Text = equipe.Nom, 
                                FontSize = 12, 
                                FontWeight = FontWeights.SemiBold,
                                Margin = new Thickness(0, 0, 5, 0)
                            },
                            new Border
                            {
                                Background = new SolidColorBrush(Color.FromRgb(200, 230, 201)),
                                CornerRadius = new CornerRadius(10),
                                Padding = new Thickness(6, 2, 6, 2),
                                Child = new TextBlock
                                {
                                    Text = nbMembres.ToString(),
                                    FontSize = 10,
                                    FontWeight = FontWeights.Bold,
                                    Foreground = new SolidColorBrush(Color.FromRgb(27, 94, 32))
                                }
                            }
                        }
                    },
                    Style = _equipeIdFiltre == equipe.Id ? 
                        (Style)FindResource("EquipeFilterButtonActive") : 
                        (Style)FindResource("EquipeFilterButton"),
                    Tag = equipe.Id
                };
                btnEquipe.Click += BtnFiltreEquipe_Click;
                PanelFiltresEquipes.Children.Add(btnEquipe);
            }
        }

        private void BtnFiltreEquipe_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn?.Tag == null)
            {
                AfficherToutesRessources();
            }
            else
            {
                int equipeId = (int)btn.Tag;
                FiltrerParEquipe(equipeId);
            }

            // Mettre à jour les styles des boutons
            foreach (var child in PanelFiltresEquipes.Children)
            {
                if (child is Button button)
                {
                    if ((button.Tag == null && _equipeIdFiltre == null) ||
                        (button.Tag != null && _equipeIdFiltre.HasValue && (int)button.Tag == _equipeIdFiltre.Value))
                    {
                        button.Style = (Style)FindResource("EquipeFilterButtonActive");
                    }
                    else
                    {
                        button.Style = (Style)FindResource("EquipeFilterButton");
                    }
                }
            }
        }

        private void AfficherToutesRessources()
        {
            _equipeIdFiltre = null;
            TxtTitre.Text = "👥 Détail des Ressources";
            TxtSousTitre.Text = "Vue complète des membres, charges et projets assignés";
            
            ListeRessources.ItemsSource = _toutesLesRessources;
            MettreAJourStatistiques(_toutesLesRessources);
            
            PanelAucuneRessource.Visibility = _toutesLesRessources.Count == 0 ? 
                Visibility.Visible : Visibility.Collapsed;
        }

        private void FiltrerParEquipe(int? equipeId)
        {
            _equipeIdFiltre = equipeId;
            
            var ressourcesFiltrees = _toutesLesRessources
                .Where(r => r.EquipeId == equipeId)
                .ToList();

            // Mettre à jour le titre
            if (equipeId.HasValue)
            {
                var equipe = _database.GetAllEquipes().FirstOrDefault(e => e.Id == equipeId.Value);
                if (equipe != null)
                {
                    TxtTitre.Text = $"👥 Ressources - {equipe.Nom}";
                    TxtSousTitre.Text = $"Membres de l'équipe {equipe.Nom} avec leurs charges et projets";
                }
            }

            ListeRessources.ItemsSource = ressourcesFiltrees;
            MettreAJourStatistiques(ressourcesFiltrees);
            
            PanelAucuneRessource.Visibility = ressourcesFiltrees.Count == 0 ? 
                Visibility.Visible : Visibility.Collapsed;
        }

        private void MettreAJourStatistiques(List<RessourceDetailViewModel> ressources)
        {
            TxtTotalRessources.Text = ressources.Count.ToString();
            
            var equipesUniques = ressources
                .Where(r => r.EquipeId.HasValue)
                .Select(r => r.EquipeId.Value)
                .Distinct()
                .Count();
            TxtTotalEquipes.Text = equipesUniques.ToString();

            var projetsUniques = ressources
                .SelectMany(r => r.ListeProjets.Select(p => p.Nom))
                .Distinct()
                .Count();
            TxtTotalProjets.Text = projetsUniques.ToString();

            var chargeMoyenne = ressources.Count > 0 ? 
                ressources.Average(r => r.NbProjets) : 0;
            TxtChargeMoyenne.Text = chargeMoyenne.ToString("F1");
        }

        private void BtnFermer_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

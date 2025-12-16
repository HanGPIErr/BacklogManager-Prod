using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Collections.Generic;
using System.Linq;
using BacklogManager.ViewModels;
using BacklogManager.Services;
using BacklogManager.Domain;
using System.Windows;

namespace BacklogManager.Views
{
    public partial class StatistiquesView : UserControl
    {
        private IDatabase _database;
        private EquipeService _equipeService;
        private List<RessourceDetailViewModel> _toutesLesRessources;
        private int? _equipeIdFiltre = null;

        public StatistiquesView()
        {
            InitializeComponent();
            Loaded += StatistiquesView_Loaded;
        }

        private void StatistiquesView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (CboPeriode != null)
            {
                CboPeriode.SelectedIndex = 5; // "Tout afficher" par défaut
            }

            // Récupérer la base de données depuis le DataContext
            if (DataContext is StatistiquesViewModel viewModel)
            {
                _database = viewModel.Database;
                _equipeService = new EquipeService(_database);
            }
        }

        private void CboPeriode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Vérifier que le DataContext est initialisé
            if (DataContext == null) return;

            // Afficher/masquer les DatePickers pour période personnalisée
            if (CboPeriode.SelectedIndex == 4) // Période personnalisée
            {
                PanelDatesCustom.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                PanelDatesCustom.Visibility = System.Windows.Visibility.Collapsed;
                
                // Appliquer directement le filtre pour les périodes prédéfinies
                if (DataContext is StatistiquesViewModel viewModel)
                {
                    viewModel.AppliquerFiltrePeriode(CboPeriode.SelectedIndex);
                }
            }
        }

        private void BtnAppliquerPeriode_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is StatistiquesViewModel viewModel)
            {
                viewModel.AppliquerFiltrePeriode(CboPeriode.SelectedIndex);
            }
        }

        private void GridChargeParDev_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is StatistiquesViewModel viewModel && ((DataGrid)sender).SelectedItem is ChargeDevViewModel devStats)
            {
                viewModel.AfficherDetailsDevCommand?.Execute(devStats);
            }
        }

        private void GridCRAParDev_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is StatistiquesViewModel viewModel && ((DataGrid)sender).SelectedItem is CRADevViewModel craDevStats)
            {
                viewModel.AfficherDetailsCRADevCommand?.Execute(craDevStats);
            }
        }

        private void BtnAnalyserIA_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            // Vérifier que le token API est configuré
            var apiToken = BacklogManager.Properties.Settings.Default["AgentChatToken"]?.ToString()?.Trim();
            if (string.IsNullOrWhiteSpace(apiToken))
            {
                var result = System.Windows.MessageBox.Show(
                    "Pour utiliser l'analyse IA, vous devez d'abord configurer votre token API OpenAI.\n\n" +
                    "Voulez-vous le configurer maintenant ?\n\n" +
                    "Note : Rendez-vous dans la section 💬 Chat avec l'IA pour configurer votre token.",
                    "Token API requis",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Information);
                
                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    System.Windows.MessageBox.Show(
                        "Allez dans la section '💬 Chat avec l'IA' du menu principal pour configurer votre token API.",
                        "Configuration",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information);
                }
                return;
            }

            if (DataContext is StatistiquesViewModel viewModel)
            {
                // Déterminer la description de la période
                string periodeDescription = "Toutes les données";
                
                if (CboPeriode.SelectedIndex == 0)
                    periodeDescription = "Année en cours";
                else if (CboPeriode.SelectedIndex == 1)
                    periodeDescription = "6 derniers mois";
                else if (CboPeriode.SelectedIndex == 2)
                    periodeDescription = "Mois en cours";
                else if (CboPeriode.SelectedIndex == 3)
                    periodeDescription = "3 derniers mois";
                else if (CboPeriode.SelectedIndex == 4 && viewModel.DateDebutFiltre.HasValue && viewModel.DateFinFiltre.HasValue)
                    periodeDescription = $"Du {viewModel.DateDebutFiltre.Value:dd/MM/yyyy} au {viewModel.DateFinFiltre.Value:dd/MM/yyyy}";

                var analyseWindow = new AnalyseStatistiquesIAWindow(viewModel, periodeDescription);
                analyseWindow.ShowDialog();
            }
        }

        // Event handlers pour les effets hover sur les cartes
        private void DevCard_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                border.Background = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                border.BorderBrush = new SolidColorBrush(Color.FromRgb(0, 145, 90));
                border.BorderThickness = new System.Windows.Thickness(2);
            }
        }

        private void DevCard_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                border.Background = new SolidColorBrush(Color.FromRgb(249, 249, 249));
                border.BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224));
                border.BorderThickness = new System.Windows.Thickness(1);
            }
        }

        private void BtnVoirToutesRessources_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_database == null)
            {
                System.Windows.MessageBox.Show("Base de données non disponible.", "Erreur", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            // Charger toutes les ressources et afficher la page
            _equipeIdFiltre = null;
            ChargerDonneesRessources();
            AfficherPageRessources(null);
        }

        private void BtnVoirEquipeDetail_Click(object sender, MouseButtonEventArgs e)
        {
            if (_database == null)
            {
                System.Windows.MessageBox.Show("Base de données non disponible.", "Erreur", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            if (sender is Border border && border.Tag is int equipeId)
            {
                _equipeIdFiltre = equipeId;
                ChargerDonneesRessources();
                
                var equipe = _database.GetAllEquipes().FirstOrDefault(e => e.Id == equipeId);
                AfficherPageRessources(equipe?.Nom);
            }
        }

        private void BtnRetourStatistiques_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            // Retour à la page principale
            if (DataContext is StatistiquesViewModel viewModel)
            {
                viewModel.AfficherPagePrincipale = true;
                viewModel.AfficherPageRessources = false;
            }
        }

        private void AfficherPageRessources(string nomEquipe)
        {
            if (DataContext is StatistiquesViewModel viewModel)
            {
                viewModel.AfficherPagePrincipale = false;
                viewModel.AfficherPageRessources = true;

                // Mettre à jour les titres
                if (nomEquipe != null)
                {
                    TxtTitrePageRessources.Text = $"🏢 Ressources de l'équipe : {nomEquipe}";
                    TxtSousTitrePageRessources.Text = $"Vue détaillée des membres de l'équipe {nomEquipe}";
                }
                else
                {
                    TxtTitrePageRessources.Text = "🏢 Détail des ressources par équipe";
                    TxtSousTitrePageRessources.Text = "Vue détaillée de la charge et des projets par ressource";
                }
            }
        }

        private void ChargerDonneesRessources()
        {
            try
            {
                _toutesLesRessources = new List<RessourceDetailViewModel>();
                var utilisateurs = _database.GetUtilisateurs().Where(u => u.Actif).ToList();
                var equipes = _database.GetAllEquipes().Where(eq => eq.Actif).ToList();
                var projets = _database.GetProjets();
                var taches = _database.GetBacklogItems();
                var roles = _database.GetRoles();

                foreach (var user in utilisateurs)
                {
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
                    
                    var projetsDev = projets.Where(p => 
                        taches.Any(t => t.ProjetId == p.Id && t.DevAssigneId == user.Id)
                    ).ToList();
                    
                    var tachesActives = taches.Where(t => 
                        t.DevAssigneId == user.Id && 
                        t.Statut != Statut.Termine && 
                        !t.EstArchive
                    ).ToList();

                    var detailsProjets = new List<ProjetDetailViewModel>();
                    double heuresReelles = 0;
                    double heuresDisponibles = 0;
                    
                    // Calculer les heures CRA réelles du développeur
                    var craService = new CRAService(_database);
                    
                    // Définir la période de calcul (filtrée ou toutes les données)
                    System.DateTime? dateDebut = null;
                    System.DateTime? dateFin = null;
                    
                    var viewModel = DataContext as StatistiquesViewModel;
                    if (viewModel?.DateDebutFiltre != null && viewModel?.DateFinFiltre != null)
                    {
                        dateDebut = viewModel.DateDebutFiltre.Value;
                        dateFin = viewModel.DateFinFiltre.Value;
                    }
                    
                    // Récupérer tous les CRA du dev (filtrés par période si définie)
                    var crasDev = dateDebut.HasValue && dateFin.HasValue 
                        ? craService.GetCRAsByDev(user.Id, dateDebut.Value, dateFin.Value)
                        : craService.GetCRAsByDev(user.Id);
                    
                    // Filtrer pour exclure congés et non-travaillé comme dans les stats d'équipe
                    var backlogItems = _database.GetBacklogItems();
                    heuresReelles = crasDev
                        .Where(c => {
                            var tache = backlogItems.FirstOrDefault(t => t.Id == c.BacklogItemId);
                            return tache != null && 
                                   tache.TypeDemande != TypeDemande.Conges && 
                                   tache.TypeDemande != TypeDemande.NonTravaille;
                        })
                        .Sum(c => c.HeuresTravaillees);
                    
                    // Calculer les heures disponibles (8h/jour × jours ouvrés)
                    const double HEURES_PAR_JOUR = 8.0;
                    double joursOuvres;
                    
                    if (dateDebut.HasValue && dateFin.HasValue)
                    {
                        var totalJours = (dateFin.Value - dateDebut.Value).Days + 1;
                        joursOuvres = totalJours * 5.0 / 7.0; // Estimation jours ouvrés (5 sur 7)
                    }
                    else
                    {
                        // Si pas de filtre, estimer sur base du nombre de CRA (moyenne 22 jours/mois)
                        var nbJoursCRA = crasDev.Select(c => c.Date.Date).Distinct().Count();
                        joursOuvres = nbJoursCRA > 0 ? nbJoursCRA : 22;
                    }
                    
                    heuresDisponibles = HEURES_PAR_JOUR * joursOuvres;
                    
                    foreach (var projet in projetsDev)
                    {
                        var tachesProjet = taches.Where(t => 
                            t.ProjetId == projet.Id && 
                            t.DevAssigneId == user.Id &&
                            t.Statut != Statut.Termine &&
                            !t.EstArchive
                        ).ToList();
                        
                        var nbTachesProjet = tachesProjet.Count;
                        
                        detailsProjets.Add(new ProjetDetailViewModel
                        {
                            Nom = projet.Nom,
                            NbTaches = nbTachesProjet
                        });
                    }

                    int nbProjets = projetsDev.Count;
                    string niveauCharge;
                    Color couleurCharge;
                    
                    // Calcul basé sur les heures CRA réelles vs disponibles
                    double pourcentageCharge = heuresDisponibles > 0 ? (heuresReelles / heuresDisponibles) * 100.0 : 0;
                    
                    if (pourcentageCharge < 50)
                    {
                        niveauCharge = $"Faible ({pourcentageCharge:F0}%)";
                        couleurCharge = Color.FromRgb(76, 175, 80); // Vert
                    }
                    else if (pourcentageCharge < 100)
                    {
                        niveauCharge = $"Normale ({pourcentageCharge:F0}%)";
                        couleurCharge = Color.FromRgb(33, 150, 243); // Bleu
                    }
                    else if (pourcentageCharge < 150)
                    {
                        niveauCharge = $"Élevée ({pourcentageCharge:F0}%)";
                        couleurCharge = Color.FromRgb(255, 152, 0); // Orange
                    }
                    else
                    {
                        niveauCharge = $"Surcharge ({pourcentageCharge:F0}%)";
                        couleurCharge = Color.FromRgb(244, 67, 54); // Rouge
                    }

                    // Largeur barre proportionnelle au pourcentage (100% = 100% de la barre, peut dépasser)
                    double largeurBarre = System.Math.Min((pourcentageCharge / 100.0) * 380, 380);

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

                // Créer les filtres d'équipes
                CreerFiltresEquipes(equipes);

                // Filtrer et afficher
                if (_equipeIdFiltre.HasValue)
                    FiltrerParEquipe(_equipeIdFiltre);
                else
                    AfficherToutesRessources();

                MettreAJourStatistiques();
            }
            catch (System.Exception ex)
            {
                System.Windows.MessageBox.Show($"Erreur lors du chargement des ressources : {ex.Message}", 
                    "Erreur", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void CreerFiltresEquipes(List<Equipe> equipes)
        {
            WrapPanelFiltresEquipes.Children.Clear();

            // Bouton "Toutes"
            var btnToutes = new Button
            {
                Content = "🌐 Toutes les équipes",
                Padding = new System.Windows.Thickness(12, 6, 12, 6),
                Margin = new System.Windows.Thickness(5),
                Background = new SolidColorBrush(Color.FromRgb(0, 145, 90)),
                Foreground = new SolidColorBrush(Colors.White),
                BorderThickness = new System.Windows.Thickness(0),
                Cursor = Cursors.Hand,
                Tag = (int?)null
            };
            btnToutes.Click += BtnFiltreEquipe_Click;
            WrapPanelFiltresEquipes.Children.Add(btnToutes);

            // Boutons par équipe
            foreach (var equipe in equipes.OrderBy(e => e.Nom))
            {
                var nbMembres = _toutesLesRessources.Count(r => r.EquipeId == equipe.Id);
                if (nbMembres == 0) continue;

                var btn = new Button
                {
                    Content = $"🏢 {equipe.Code} ({nbMembres})",
                    Padding = new System.Windows.Thickness(12, 6, 12, 6),
                    Margin = new System.Windows.Thickness(5),
                    Background = new SolidColorBrush(Color.FromRgb(245, 245, 245)),
                    Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                    BorderThickness = new System.Windows.Thickness(1),
                    Cursor = Cursors.Hand,
                    Tag = equipe.Id
                };
                btn.Click += BtnFiltreEquipe_Click;
                WrapPanelFiltresEquipes.Children.Add(btn);
            }
        }

        private void BtnFiltreEquipe_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                _equipeIdFiltre = btn.Tag as int?;
                
                // Mise à jour visuelle des boutons
                foreach (var child in WrapPanelFiltresEquipes.Children)
                {
                    if (child is Button otherBtn)
                    {
                        if (otherBtn == btn)
                        {
                            otherBtn.Background = new SolidColorBrush(Color.FromRgb(0, 145, 90));
                            otherBtn.Foreground = new SolidColorBrush(Colors.White);
                        }
                        else
                        {
                            otherBtn.Background = new SolidColorBrush(Color.FromRgb(245, 245, 245));
                            otherBtn.Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51));
                        }
                    }
                }

                if (_equipeIdFiltre.HasValue)
                    FiltrerParEquipe(_equipeIdFiltre);
                else
                    AfficherToutesRessources();

                MettreAJourStatistiques();
            }
        }

        private void AfficherToutesRessources()
        {
            ItemsControlRessources.ItemsSource = _toutesLesRessources.OrderBy(r => r.NomEquipe).ThenBy(r => r.Nom);
        }

        private void FiltrerParEquipe(int? equipeId)
        {
            if (equipeId.HasValue)
            {
                ItemsControlRessources.ItemsSource = _toutesLesRessources
                    .Where(r => r.EquipeId == equipeId.Value)
                    .OrderBy(r => r.Nom);
            }
            else
            {
                AfficherToutesRessources();
            }
        }

        private void MettreAJourStatistiques()
        {
            var ressourcesAffichees = ItemsControlRessources.ItemsSource as IEnumerable<RessourceDetailViewModel>;
            if (ressourcesAffichees == null) return;

            var liste = ressourcesAffichees.ToList();
            
            TxtNbTotalRessources.Text = liste.Count.ToString();
            TxtNbEquipesActives.Text = liste.Select(r => r.EquipeId).Distinct().Count().ToString();
            TxtNbProjetsActifs.Text = liste.Sum(r => r.NbProjets).ToString();
            
            // Extraire le pourcentage de charge du texte NiveauCharge (format: "Faible (XX%)")
            double chargeMoyenne = 0;
            if (liste.Any())
            {
                var chargesValides = liste.Select(r =>
                {
                    var match = System.Text.RegularExpressions.Regex.Match(r.NiveauCharge, @"\((\d+)%\)");
                    if (match.Success && double.TryParse(match.Groups[1].Value, out double charge))
                        return charge;
                    return 0.0;
                }).Where(c => c > 0);
                
                chargeMoyenne = chargesValides.Any() ? chargesValides.Average() : 0;
            }
            
            TxtChargeMoyenne.Text = $"{chargeMoyenne:F0}%";
        }
    }
}

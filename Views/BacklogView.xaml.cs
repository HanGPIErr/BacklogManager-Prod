using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BacklogManager.Domain;
using BacklogManager.ViewModels;

namespace BacklogManager.Views
{
    public partial class BacklogView : UserControl
    {
        private ProjetsView _projetsView;

        public BacklogView()
        {
            InitializeComponent();
            Loaded += BacklogView_Loaded;
        }

        private void BacklogView_Loaded(object sender, RoutedEventArgs e)
        {
            // Initialiser la vue projets si nécessaire
            if (_projetsView == null && DataContext is BacklogViewModel viewModel)
            {
                _projetsView = new ProjetsView();
                var projetsViewModel = new ProjetsViewModel(viewModel.BacklogService, viewModel.PermissionService, viewModel.CRAService);
                _projetsView.DataContext = projetsViewModel;
                // S'abonner à l'événement de création de projet
                viewModel.ProjetCreated += OnProjetCreated;
            }
            
            // Afficher directement l'onglet Programmes au chargement
            BtnVueProgrammes_Click(null, null);
        }

        private void OnProjetCreated(object sender, EventArgs e)
        {
            // Rafraîchir la vue des projets si elle est active
            if (_projetsView != null && MainContentFrame?.Visibility == Visibility.Visible)
            {
                var viewModel = _projetsView.DataContext as ProjetsViewModel;
                viewModel?.LoadData();
            }
        }

        private void BtnVueProgrammes_Click(object sender, RoutedEventArgs e)
        {
            // Activer le bouton Programmes
            BtnVueProgrammes.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00915A"));
            BtnVueProgrammes.Foreground = Brushes.White;
            BtnVueProgrammes.FontWeight = FontWeights.SemiBold;

            // Désactiver le bouton Projets
            BtnVueProjets.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0"));
            BtnVueProjets.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6D6D6D"));
            BtnVueProjets.FontWeight = FontWeights.Normal;

            // Cacher tous les boutons d'action
            BtnNouveauProjet.Visibility = Visibility.Collapsed;
            BtnNouvelleTache.Visibility = Visibility.Collapsed;
            BtnTacheSpeciale.Visibility = Visibility.Collapsed;

            // Cacher la barre de filtres et réduire la largeur de colonne
            if (FiltersSidebar != null)
            {
                FiltersSidebar.Visibility = Visibility.Collapsed;
                FiltersColumn.Width = new GridLength(0);
                FiltersColumn.MinWidth = 0;
                FiltersColumn.MaxWidth = 0;
            }

            // Cacher la vue des tâches
            if (TachesScrollViewer != null)
            {
                TachesScrollViewer.Visibility = Visibility.Collapsed;
            }

            // Afficher la vue des programmes
            if (MainContentFrame != null && DataContext is BacklogViewModel viewModel)
            {
                // Créer une vue pour afficher les programmes
                var programmesList = CreateProgrammesView();
                MainContentFrame.Content = programmesList;
                MainContentFrame.Visibility = Visibility.Visible;
            }
        }

        private UIElement CreateProgrammesView()
        {
            var viewModel = DataContext as BacklogViewModel;
            if (viewModel == null) return new Grid();

            var programmes = viewModel.BacklogService.Database.GetAllProgrammes().FindAll(p => p.Actif);
            var projets = viewModel.BacklogService.GetAllProjets();

            var scrollViewer = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Padding = new Thickness(20) };
            var stackPanel = new StackPanel { Margin = new Thickness(0) };

            if (programmes.Count == 0)
            {
                var emptyText = new TextBlock
                {
                    Text = "Aucun programme actif. Créez des programmes depuis l'onglet Administration.",
                    FontSize = 16,
                    Foreground = new SolidColorBrush(Colors.Gray),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 40, 0, 0)
                };
                stackPanel.Children.Add(emptyText);
            }
            else
            {
                foreach (var programme in programmes.OrderBy(p => p.Code))
                {
                    var projetsAssocies = projets.Where(pr => pr.ProgrammeId == programme.Id && pr.Actif).ToList();
                    var card = CreateProgrammeCard(programme, projetsAssocies, viewModel);
                    stackPanel.Children.Add(card);
                }
            }

            scrollViewer.Content = stackPanel;
            return scrollViewer;
        }

        private Border CreateProgrammeCard(Programme programme, System.Collections.Generic.List<Projet> projetsAssocies, BacklogViewModel backlogViewModel)
        {
            var border = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(25),
                Margin = new Thickness(0, 0, 0, 15),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0")),
                BorderThickness = new Thickness(1),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            border.Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                BlurRadius = 12,
                ShadowDepth = 0,
                Opacity = 0.1,
                Color = Colors.Black
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var leftPanel = new StackPanel();
            
            var titlePanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
            titlePanel.Children.Add(new TextBlock { Text = "🎯", FontSize = 24, Margin = new Thickness(0, 0, 12, 0), VerticalAlignment = VerticalAlignment.Center });
            var titleBlock = new TextBlock
            {
                Text = $"{programme.Code} - {programme.Nom}",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00915A")),
                VerticalAlignment = VerticalAlignment.Center
            };
            titlePanel.Children.Add(titleBlock);
            leftPanel.Children.Add(titlePanel);

            if (!string.IsNullOrEmpty(programme.Description))
            {
                leftPanel.Children.Add(new TextBlock
                {
                    Text = programme.Description,
                    FontSize = 14,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666666")),
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 10)
                });
            }

            // Pastilles d'informations
            var infosPanel = new WrapPanel { Margin = new Thickness(0, 0, 0, 15) };

            // Responsable
            if (programme.ResponsableId.HasValue)
            {
                var devs = backlogViewModel.BacklogService.GetAllDevs();
                var responsable = devs.FirstOrDefault(d => d.Id == programme.ResponsableId.Value);
                if (responsable != null)
                {
                    var responsableBadge = new Border
                    {
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E3F2FD")),
                        CornerRadius = new CornerRadius(8),
                        Padding = new Thickness(8, 4, 8, 4),
                        Margin = new Thickness(0, 0, 8, 4)
                    };
                    responsableBadge.Child = new TextBlock
                    {
                        Text = $"👤 {responsable.Nom}",
                        FontSize = 12,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1976D2"))
                    };
                    infosPanel.Children.Add(responsableBadge);
                }
            }

            // Dates
            if (programme.DateDebut.HasValue && programme.DateFinCible.HasValue)
            {
                var datesBadge = new Border
                {
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF3E0")),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(8, 4, 8, 4),
                    Margin = new Thickness(0, 0, 8, 4)
                };
                datesBadge.Child = new TextBlock
                {
                    Text = $"📅 {programme.DateDebut.Value:dd/MM/yyyy} → {programme.DateFinCible.Value:dd/MM/yyyy}",
                    FontSize = 12,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F57C00"))
                };
                infosPanel.Children.Add(datesBadge);
            }

            // Statut global
            if (!string.IsNullOrEmpty(programme.StatutGlobal))
            {
                var statutColor = programme.StatutGlobal == "On Track" ? "#4CAF50" :
                                  programme.StatutGlobal == "At Risk" ? "#FF9800" : "#F44336";
                var statutIcon = programme.StatutGlobal == "On Track" ? "✓" :
                                 programme.StatutGlobal == "At Risk" ? "⚠" : "✗";

                var statutBadge = new Border
                {
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(statutColor)),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(8, 4, 8, 4),
                    Margin = new Thickness(0, 0, 8, 4)
                };
                statutBadge.Child = new TextBlock
                {
                    Text = $"{statutIcon} {programme.StatutGlobal}",
                    FontSize = 12,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = Brushes.White
                };
                infosPanel.Children.Add(statutBadge);
            }

            // Indicateurs RAG compacts (nombre de projets par statut)
            if (projetsAssocies.Count > 0)
            {
                // Calculer le StatutRAG automatiquement pour chaque projet s'il n'est pas défini
                var toutesLesTaches = backlogViewModel.BacklogService.GetAllBacklogItemsIncludingArchived();
                
                foreach (var projet in projetsAssocies)
                {
                    if (string.IsNullOrEmpty(projet.StatutRAG))
                    {
                        projet.StatutRAG = CalculerStatutRAGAutomatique(projet, toutesLesTaches);
                    }
                }
                
                var nbGreen = projetsAssocies.Count(p => !string.IsNullOrEmpty(p.StatutRAG) && p.StatutRAG.Equals("Green", StringComparison.OrdinalIgnoreCase));
                var nbAmber = projetsAssocies.Count(p => !string.IsNullOrEmpty(p.StatutRAG) && p.StatutRAG.Equals("Amber", StringComparison.OrdinalIgnoreCase));
                var nbRed = projetsAssocies.Count(p => !string.IsNullOrEmpty(p.StatutRAG) && p.StatutRAG.Equals("Red", StringComparison.OrdinalIgnoreCase));
                var nbNonDefini = projetsAssocies.Count(p => string.IsNullOrEmpty(p.StatutRAG));

                // Afficher un badge par statut RAG (comme dans Dashboard)
                if (nbGreen > 0)
                {
                    var greenBadge = new Border
                    {
                        Background = new SolidColorBrush(Color.FromRgb(76, 175, 80)), // #4CAF50
                        CornerRadius = new CornerRadius(8),
                        Padding = new Thickness(8, 4, 8, 4),
                        Margin = new Thickness(0, 0, 5, 4)
                    };
                    greenBadge.Child = new TextBlock
                    {
                        Text = nbGreen == 1 ? "GREEN" : $"GREEN ({nbGreen})",
                        FontSize = 11,
                        FontWeight = FontWeights.Bold,
                        Foreground = Brushes.White
                    };
                    infosPanel.Children.Add(greenBadge);
                }

                if (nbAmber > 0)
                {
                    var amberBadge = new Border
                    {
                        Background = new SolidColorBrush(Color.FromRgb(255, 152, 0)), // #FF9800
                        CornerRadius = new CornerRadius(8),
                        Padding = new Thickness(8, 4, 8, 4),
                        Margin = new Thickness(0, 0, 5, 4)
                    };
                    amberBadge.Child = new TextBlock
                    {
                        Text = nbAmber == 1 ? "AMBER" : $"AMBER ({nbAmber})",
                        FontSize = 11,
                        FontWeight = FontWeights.Bold,
                        Foreground = Brushes.White
                    };
                    infosPanel.Children.Add(amberBadge);
                }

                if (nbRed > 0)
                {
                    var redBadge = new Border
                    {
                        Background = new SolidColorBrush(Color.FromRgb(244, 67, 54)), // #F44336
                        CornerRadius = new CornerRadius(8),
                        Padding = new Thickness(8, 4, 8, 4),
                        Margin = new Thickness(0, 0, 5, 4)
                    };
                    redBadge.Child = new TextBlock
                    {
                        Text = nbRed == 1 ? "RED" : $"RED ({nbRed})",
                        FontSize = 11,
                        FontWeight = FontWeights.Bold,
                        Foreground = Brushes.White
                    };
                    infosPanel.Children.Add(redBadge);
                }

                if (nbNonDefini > 0)
                {
                    var nonDefiniBadge = new Border
                    {
                        Background = new SolidColorBrush(Color.FromRgb(158, 158, 158)), // #9E9E9E (Grey)
                        CornerRadius = new CornerRadius(8),
                        Padding = new Thickness(8, 4, 8, 4),
                        Margin = new Thickness(0, 0, 5, 4)
                    };
                    nonDefiniBadge.Child = new TextBlock
                    {
                        Text = nbNonDefini == 1 ? "N/A" : $"N/A ({nbNonDefini})",
                        FontSize = 11,
                        FontWeight = FontWeights.Bold,
                        Foreground = Brushes.White
                    };
                    infosPanel.Children.Add(nonDefiniBadge);
                }
            }

            leftPanel.Children.Add(infosPanel);

            // Afficher les badges des projets
            if (projetsAssocies.Count > 0)
            {
                // Calculer les statistiques du programme (comme dans StatistiquesViewModel)
                // Récupérer TOUTES les tâches (y compris archivées) sauf Congés et NonTravaillé
                var toutesLesTaches = backlogViewModel.BacklogService.GetAllBacklogItemsIncludingArchived()
                    .Where(t => projetsAssocies.Any(p => p.Id == t.ProjetId) &&
                                t.TypeDemande != TypeDemande.Conges && 
                                t.TypeDemande != TypeDemande.NonTravaille)
                    .ToList();
                
                var nbTachesTotal = toutesLesTaches.Count;
                // Terminées = tâches actives avec Statut.Termine + tâches archivées
                var nbTachesTerminees = toutesLesTaches.Count(t => t.Statut == Statut.Termine || t.EstArchive);
                var pourcentageAvancement = nbTachesTotal > 0 ? (int)((double)nbTachesTerminees / nbTachesTotal * 100) : 0;

                // Compter les projets par statut RAG
                var nbGreen = projetsAssocies.Count(p => p.StatutRAG == "Green");
                var nbAmber = projetsAssocies.Count(p => p.StatutRAG == "Amber");
                var nbRed = projetsAssocies.Count(p => p.StatutRAG == "Red");

                // Panel d'indicateurs (barre de progression + RAG)
                var indicateursPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 12) };
                
                // Barre de progression
                var progressContainer = new Border
                {
                    Width = 200,
                    Height = 24,
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F0F0F0")),
                    CornerRadius = new CornerRadius(12),
                    Margin = new Thickness(0, 0, 15, 0)
                };

                var progressGrid = new Grid();
                var progressBar = new Border
                {
                    Width = 200 * pourcentageAvancement / 100,
                    Height = 24,
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00915A")),
                    CornerRadius = new CornerRadius(12),
                    HorizontalAlignment = HorizontalAlignment.Left
                };
                progressGrid.Children.Add(progressBar);

                var progressText = new TextBlock
                {
                    Text = $"{pourcentageAvancement}%",
                    FontSize = 12,
                    FontWeight = FontWeights.Bold,
                    Foreground = pourcentageAvancement > 50 ? Brushes.White : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666666")),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                progressGrid.Children.Add(progressText);
                progressContainer.Child = progressGrid;
                indicateursPanel.Children.Add(progressContainer);

                // Indicateurs RAG
                if (nbGreen > 0)
                {
                    var greenIndicator = new Border
                    {
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50")),
                        CornerRadius = new CornerRadius(10),
                        Padding = new Thickness(8, 4, 8, 4),
                        Margin = new Thickness(0, 0, 5, 0)
                    };
                    greenIndicator.Child = new TextBlock
                    {
                        Text = $"🟢 {nbGreen}",
                        FontSize = 12,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = Brushes.White
                    };
                    indicateursPanel.Children.Add(greenIndicator);
                }

                if (nbAmber > 0)
                {
                    var amberIndicator = new Border
                    {
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF9800")),
                        CornerRadius = new CornerRadius(10),
                        Padding = new Thickness(8, 4, 8, 4),
                        Margin = new Thickness(0, 0, 5, 0)
                    };
                    amberIndicator.Child = new TextBlock
                    {
                        Text = $"🟠 {nbAmber}",
                        FontSize = 12,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = Brushes.White
                    };
                    indicateursPanel.Children.Add(amberIndicator);
                }

                if (nbRed > 0)
                {
                    var redIndicator = new Border
                    {
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F44336")),
                        CornerRadius = new CornerRadius(10),
                        Padding = new Thickness(8, 4, 8, 4),
                        Margin = new Thickness(0, 0, 5, 0)
                    };
                    redIndicator.Child = new TextBlock
                    {
                        Text = $"🔴 {nbRed}",
                        FontSize = 12,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = Brushes.White
                    };
                    indicateursPanel.Children.Add(redIndicator);
                }

                leftPanel.Children.Add(indicateursPanel);

                // Label des projets
                var projetsLabel = new TextBlock
                {
                    Text = $"📁 {projetsAssocies.Count} projet{(projetsAssocies.Count > 1 ? "s" : "")} :",
                    FontSize = 14,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00915A")),
                    Margin = new Thickness(0, 0, 0, 8)
                };
                leftPanel.Children.Add(projetsLabel);
                
                var badgesPanel = new WrapPanel { Margin = new Thickness(0, 0, 0, 0) };
                foreach (var projet in projetsAssocies)
                {
                    // Calculer les stats du projet
                    var tachesProjet = toutesLesTaches.Where(t => t.ProjetId == projet.Id).ToList();
                    var nbTotal = tachesProjet.Count;
                    var nbTermine = tachesProjet.Count(t => t.Statut == Statut.Termine || t.EstArchive);
                    var progression = nbTotal > 0 ? (int)((double)nbTermine / nbTotal * 100) : 0;
                    
                    // Calculer le RAG si non défini
                    if (string.IsNullOrEmpty(projet.StatutRAG))
                    {
                        projet.StatutRAG = CalculerStatutRAGAutomatique(projet, toutesLesTaches);
                    }

                    var badge = new Border
                    {
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E8F5E9")),
                        BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00915A")),
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(12),
                        Padding = new Thickness(12, 6, 12, 6),
                        Margin = new Thickness(0, 0, 8, 8),
                        Cursor = System.Windows.Input.Cursors.Hand
                    };

                    // Contenu du badge avec nom + progression + RAG
                    var badgeStack = new StackPanel { Orientation = Orientation.Horizontal };
                    
                    // Nom du projet
                    var nomText = new TextBlock
                    {
                        Text = projet.Nom,
                        FontSize = 13,
                        FontWeight = FontWeights.Medium,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00915A")),
                        Margin = new Thickness(0, 0, 8, 0)
                    };
                    badgeStack.Children.Add(nomText);

                    // Badge progression
                    var progressBadge = new Border
                    {
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00915A")),
                        CornerRadius = new CornerRadius(4),
                        Padding = new Thickness(5, 2, 5, 2),
                        Margin = new Thickness(0, 0, 5, 0)
                    };
                    progressBadge.Child = new TextBlock
                    {
                        Text = $"{progression}%",
                        FontSize = 10,
                        FontWeight = FontWeights.Bold,
                        Foreground = Brushes.White
                    };
                    badgeStack.Children.Add(progressBadge);

                    // Badge RAG
                    var ragColor = projet.StatutRAG?.ToLower() == "green" ? Color.FromRgb(76, 175, 80) :
                                   projet.StatutRAG?.ToLower() == "amber" ? Color.FromRgb(255, 152, 0) : 
                                   projet.StatutRAG?.ToLower() == "red" ? Color.FromRgb(244, 67, 54) :
                                   Color.FromRgb(158, 158, 158);
                    
                    var ragBadge = new Border
                    {
                        Background = new SolidColorBrush(ragColor),
                        CornerRadius = new CornerRadius(4),
                        Padding = new Thickness(5, 2, 5, 2)
                    };
                    ragBadge.Child = new TextBlock
                    {
                        Text = (projet.StatutRAG ?? "N/A").ToUpper(),
                        FontSize = 10,
                        FontWeight = FontWeights.Bold,
                        Foreground = Brushes.White
                    };
                    badgeStack.Children.Add(ragBadge);

                    badge.Child = badgeStack;

                    // Empêcher la propagation du clic vers la carte parent
                    badge.MouseLeftButtonUp += (s, e) =>
                    {
                        e.Handled = true;
                        ShowProjetDetails(projet.Id);
                    };

                    badge.MouseEnter += (s, e) =>
                    {
                        badge.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C8E6C9"));
                    };

                    badge.MouseLeave += (s, e) =>
                    {
                        badge.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E8F5E9"));
                    };

                    badgesPanel.Children.Add(badge);
                }
                leftPanel.Children.Add(badgesPanel);
            }

            Grid.SetColumn(leftPanel, 0);
            grid.Children.Add(leftPanel);

            // Panel de droite avec bouton détails
            var rightPanel = new StackPanel { VerticalAlignment = VerticalAlignment.Center };

            var detailsButton = new Button
            {
                Content = "ℹ Détails",
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00915A")),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(16, 8, 16, 8),
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Cursor = System.Windows.Input.Cursors.Hand,
                Margin = new Thickness(0, 0, 0, 10)
            };
            detailsButton.Click += (s, e) =>
            {
                e.Handled = true;
                ShowProgrammeDetails(programme);
            };
            rightPanel.Children.Add(detailsButton);

            var arrow = new TextBlock
            {
                Text = "→",
                FontSize = 32,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00915A")),
                VerticalAlignment = VerticalAlignment.Center
            };
            rightPanel.Children.Add(arrow);

            Grid.SetColumn(rightPanel, 1);
            grid.Children.Add(rightPanel);

            border.Child = grid;

            // Événement clic pour afficher les projets du programme
            border.MouseLeftButtonUp += (s, e) =>
            {
                ShowProgrammeProjects(programme.Id);
            };

            border.MouseEnter += (s, e) =>
            {
                border.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F0F9F4"));
            };

            border.MouseLeave += (s, e) =>
            {
                border.Background = Brushes.White;
            };

            return border;
        }

        private void ShowProgrammeProjects(int programmeId)
        {
            // Basculer vers l'onglet Projets et filtrer par programme
            BtnVueProjets_Click(this, new RoutedEventArgs());
            
            // Filtrer les projets par programme
            if (_projetsView != null && _projetsView.DataContext is ProjetsViewModel projetsViewModel)
            {
                // Recharger les données puis filtrer
                projetsViewModel.LoadData();
                // Note: Il faudra ajouter une méthode de filtrage dans ProjetsViewModel si elle n'existe pas
            }
        }

        private void ShowProgrammeDetails(Programme programme)
        {
            if (DataContext is BacklogViewModel viewModel)
            {
                var detailsWindow = new ProgrammeDetailsWindow(programme, viewModel.BacklogService);
                detailsWindow.Owner = Window.GetWindow(this);
                detailsWindow.ShowDialog();
            }
        }

        private void ShowProjetDetails(int projetId)
        {
            // Basculer vers l'onglet Projets
            BtnVueProjets_Click(this, new RoutedEventArgs());
            
            // Sélectionner le projet dans la vue Projets
            if (_projetsView != null && _projetsView.DataContext is ProjetsViewModel projetsViewModel && DataContext is BacklogViewModel backlogViewModel)
            {
                // Trouver le ProjetItemViewModel correspondant au projet
                var projetItem = projetsViewModel.ProjetsFiltres.FirstOrDefault(p => p.Projet.Id == projetId);
                if (projetItem != null)
                {
                    // Définir le projet sélectionné, ce qui va déclencher l'affichage de ses tâches
                    projetsViewModel.SelectedProjet = projetItem;
                }
            }
        }
        private void BtnVueProjets_Click(object sender, RoutedEventArgs e)
        {
            // Activer le bouton Projets
            BtnVueProjets.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00915A"));
            BtnVueProjets.Foreground = Brushes.White;
            BtnVueProjets.FontWeight = FontWeights.SemiBold;

            // Désactiver le bouton Programmes
            BtnVueProgrammes.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0"));
            BtnVueProgrammes.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6D6D6D"));
            BtnVueProgrammes.FontWeight = FontWeights.Normal;

            // Afficher le bouton Nouveau Projet, cacher les boutons de tâches
            BtnNouveauProjet.Visibility = Visibility.Visible;
            BtnNouvelleTache.Visibility = Visibility.Collapsed;
            BtnTacheSpeciale.Visibility = Visibility.Collapsed;

            // Cacher la barre de filtres (pour tâches uniquement) et réduire la largeur de colonne
            if (FiltersSidebar != null)
            {
                FiltersSidebar.Visibility = Visibility.Collapsed;
                FiltersColumn.Width = new GridLength(0);
                FiltersColumn.MinWidth = 0;
                FiltersColumn.MaxWidth = 0;
            }

            // Cacher la vue des tâches
            if (TachesScrollViewer != null)
            {
                TachesScrollViewer.Visibility = Visibility.Collapsed;
            }

            // Afficher la vue des projets
            if (MainContentFrame != null && _projetsView != null)
            {
                MainContentFrame.Content = _projetsView;
                MainContentFrame.Visibility = Visibility.Visible;
                var viewModel = _projetsView.DataContext as ProjetsViewModel;
                viewModel?.LoadData();
            }
            else if (DataContext is BacklogViewModel viewModel)
            {
                _projetsView = new ProjetsView();
                var projetsViewModel = new ProjetsViewModel(viewModel.BacklogService, viewModel.PermissionService, viewModel.CRAService);
                _projetsView.DataContext = projetsViewModel;
                if (MainContentFrame != null)
                {
                    MainContentFrame.Content = _projetsView;
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
            BtnVueProgrammes.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0"));
            BtnVueProgrammes.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6D6D6D"));
            BtnVueProgrammes.FontWeight = FontWeights.Normal;
            
            BtnVueProjets.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0"));
            BtnVueProjets.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6D6D6D"));
            BtnVueProjets.FontWeight = FontWeights.Normal;

            // Cacher tous les boutons d'action
            BtnNouveauProjet.Visibility = Visibility.Collapsed;
            BtnNouvelleTache.Visibility = Visibility.Collapsed;
            BtnTacheSpeciale.Visibility = Visibility.Collapsed;

            // Cacher la barre de filtres (pour tâches uniquement) et réduire la largeur de colonne
            if (FiltersSidebar != null)
            {
                FiltersSidebar.Visibility = Visibility.Collapsed;
                FiltersColumn.Width = new GridLength(0);
                FiltersColumn.MinWidth = 0;
                FiltersColumn.MaxWidth = 0;
            }

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

        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.ContextMenu != null)
            {
                button.ContextMenu.PlacementTarget = button;
                button.ContextMenu.IsOpen = true;
            }
        }

        private void EditMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            if (menuItem?.Tag != null && DataContext is BacklogViewModel viewModel)
            {
                // Vérification de permission supplémentaire
                if (viewModel.PermissionService?.PeutModifierTaches == true)
                {
                    viewModel.EditCommand?.Execute(menuItem.Tag);
                }
            }
        }

        private void DeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            if (menuItem?.Tag != null && DataContext is BacklogViewModel viewModel)
            {
                // Vérification de permission supplémentaire
                if (viewModel.PermissionService?.PeutSupprimerTaches == true)
                {
                    viewModel.DeleteCommand?.Execute(menuItem.Tag);
                }
            }
        }

        private string CalculerStatutRAGAutomatique(Projet projet, System.Collections.Generic.List<BacklogItem> toutesLesTaches)
        {
            var tachesProjet = toutesLesTaches
                .Where(t => t.ProjetId == projet.Id && 
                            t.TypeDemande != TypeDemande.Conges && 
                            t.TypeDemande != TypeDemande.NonTravaille)
                .ToList();

            if (tachesProjet.Count == 0)
                return "Green"; // Pas de tâches = projet sain

            var nbTotal = tachesProjet.Count;
            var nbTerminees = tachesProjet.Count(t => t.Statut == Statut.Termine || t.EstArchive);
            var progression = nbTotal > 0 ? (nbTerminees * 100.0 / nbTotal) : 0;

            // Compter les tâches en retard (non terminées avec date dépassée)
            var nbEnRetard = tachesProjet.Count(t => 
                t.Statut != Statut.Termine && 
                !t.EstArchive &&
                t.DateFinAttendue.HasValue && 
                t.DateFinAttendue.Value < DateTime.Now);

            var tauxRetard = nbTotal > 0 ? (nbEnRetard * 100.0 / nbTotal) : 0;

            // Logique de calcul RAG:
            // Red: > 30% de tâches en retard OU progression < 30% avec des tâches en retard
            // Amber: > 15% de tâches en retard OU progression entre 30-60% avec retards
            // Green: < 15% de tâches en retard ET progression correcte

            if (tauxRetard > 30 || (progression < 30 && nbEnRetard > 0))
                return "Red";
            
            if (tauxRetard > 15 || (progression < 60 && nbEnRetard > 3))
                return "Amber";
            
            return "Green";
        }
    }
}

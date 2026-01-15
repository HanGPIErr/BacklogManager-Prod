using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using BacklogManager.Domain;
using BacklogManager.Services;

namespace BacklogManager.Views.Pages
{
    public partial class PlanningVMPage : UserControl, INotifyPropertyChanged
    {
        private readonly IDatabase _database;
        private readonly NotificationService _notificationService;
        private readonly AuthenticationService _authService;
        private readonly int _equipeId;
        private readonly Action _retourCallback;
        private DateTime _moisCourant;
        private List<Utilisateur> _membresEquipe;
        public ICommand JourClickCommand { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Propriétés traduites
        public string BackText => LocalizationService.Instance["TeamDetail_Back"];
        public string PlanningVMText => LocalizationService.Instance["PlanningVM_Title"];
        public string TodayText => LocalizationService.Instance["PlanningVM_Today"];
        public string MondayText => LocalizationService.Instance["Day_Monday"];
        public string TuesdayText => LocalizationService.Instance["Day_Tuesday"];
        public string WednesdayText => LocalizationService.Instance["Day_Wednesday"];
        public string ThursdayText => LocalizationService.Instance["Day_Thursday"];
        public string FridayText => LocalizationService.Instance["Day_Friday"];
        public string SaturdayText => LocalizationService.Instance["Day_Saturday"];
        public string SundayText => LocalizationService.Instance["Day_Sunday"];

        public PlanningVMPage(IDatabase database, NotificationService notificationService, 
            AuthenticationService authService, int equipeId, Action retourCallback)
        {
            InitializeComponent();
            _database = database;
            _notificationService = notificationService;
            _authService = authService;
            _equipeId = equipeId;
            _retourCallback = retourCallback;
            _moisCourant = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            
            DataContext = this;
            
            // Initialiser le Command
            JourClickCommand = new RelayCommand<JourCalendrierVM>(OnJourClick);

            ChargerMembresEquipe();
            AfficherCalendrier();
            
            // S'abonner aux changements de langue
            LocalizationService.Instance.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "Item[]")
                {
                    InitialiserTextes();
                }
            };
        }

        private void InitialiserTextes()
        {
            OnPropertyChanged(nameof(BackText));
            OnPropertyChanged(nameof(PlanningVMText));
            OnPropertyChanged(nameof(TodayText));
            OnPropertyChanged(nameof(MondayText));
            OnPropertyChanged(nameof(TuesdayText));
            OnPropertyChanged(nameof(WednesdayText));
            OnPropertyChanged(nameof(ThursdayText));
            OnPropertyChanged(nameof(FridayText));
            OnPropertyChanged(nameof(SaturdayText));
            OnPropertyChanged(nameof(SundayText));
        }

        private void BtnRetour_Click(object sender, RoutedEventArgs e)
        {
            _retourCallback?.Invoke();
        }

        private void ChargerMembresEquipe()
        {
            _membresEquipe = _database.GetUtilisateurs()
                .Where(u => u.EquipeId == _equipeId && u.Actif)
                .ToList();
        }

        private void AfficherCalendrier()
        {
            // Mettre à jour le titre du mois avec la culture appropriée
            var culture = LocalizationService.Instance.CurrentCulture;
            TxtMoisAnnee.Text = _moisCourant.ToString("MMMM yyyy", culture);
            TxtMoisAnnee.Text = char.ToUpper(TxtMoisAnnee.Text[0]) + TxtMoisAnnee.Text.Substring(1);

            // Générer les jours du calendrier
            var jours = new List<JourCalendrierVM>();
            
            // Premier jour du mois
            var premierJour = _moisCourant;
            
            // Trouver le premier lundi à afficher (peut être du mois précédent)
            var premierLundi = premierJour;
            while (premierLundi.DayOfWeek != DayOfWeek.Monday)
            {
                premierLundi = premierLundi.AddDays(-1);
            }

            // Récupérer tous les plannings du mois et des semaines adjacentes
            var plannings = _database.GetPlanningsVM()
                .Where(p => p.EquipeId == _equipeId && 
                            p.Date >= premierLundi && 
                            p.Date < premierLundi.AddDays(42))
                .ToList();

            // Récupérer les demandes d'échange en attente
            var demandesEchange = _database.GetDemandesEchangeVM()
                .Where(d => d.Statut == "EN_ATTENTE")
                .ToList();
            
            var utilisateurConnecteId = _authService.CurrentUser.Id;

            // Générer 42 jours (6 semaines)
            for (int i = 0; i < 42; i++)
            {
                var date = premierLundi.AddDays(i);
                var planning = plannings.FirstOrDefault(p => p.Date.Date == date.Date);
                var demandeEnAttente = planning != null ? 
                    demandesEchange.FirstOrDefault(d => d.PlanningVMJourId == planning.Id && 
                        (d.UtilisateurDemandeurId == utilisateurConnecteId || d.UtilisateurCibleId == utilisateurConnecteId)) : null;

                var jourVM = new JourCalendrierVM
                {
                    Date = date,
                    Numero = date.Day.ToString(),
                    EstHorsMois = date.Month != _moisCourant.Month,
                    EstAujourdhui = date.Date == DateTime.Today,
                    EstWeekend = JoursFeriesService.EstWeekend(date),
                    EstJourFerie = JoursFeriesService.EstJourFerie(date),
                    NomJourFerie = JoursFeriesService.GetNomJourFerie(date),
                    IconeJourFerie = GetIconeJourFerie(date),
                    PlanningId = planning?.Id,
                    UtilisateurId = planning?.UtilisateurId,
                    NomMembre = planning?.UtilisateurId != null ? 
                        GetNomUtilisateur(planning.UtilisateurId.Value) : null,
                    ADemandeEchange = demandeEnAttente != null
                };

                jours.Add(jourVM);
            }

            // Désactiver l'ItemsControl pendant la mise à jour pour éviter les conflits
            CalendrierItems.IsEnabled = false;
            CalendrierItems.ItemsSource = null;
            
            // Forcer le rafraîchissement visuel
            CalendrierItems.UpdateLayout();
            
            // Réassigner la nouvelle source
            CalendrierItems.ItemsSource = jours;
            
            // Réactiver après un court délai
            Dispatcher.BeginInvoke(new Action(() => 
            {
                CalendrierItems.UpdateLayout();
                CalendrierItems.IsEnabled = true;
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private string GetIconeJourFerie(DateTime date)
        {
            if (!JoursFeriesService.EstJourFerie(date))
                return null;

            // Utiliser la même icône pour tous les jours fériés
            return "/Images/jour-ferie.png";
        }

        private string GetNomUtilisateur(int utilisateurId)
        {
            var utilisateur = _membresEquipe.FirstOrDefault(u => u.Id == utilisateurId);
            if (utilisateur == null)
            {
                // Charger depuis la base si pas dans la liste
                utilisateur = _database.GetUtilisateurs().FirstOrDefault(u => u.Id == utilisateurId);
            }
            return utilisateur != null ? $"{utilisateur.Prenom} {utilisateur.Nom}" : "Inconnu";
        }

        private void BtnMoisPrecedent_Click(object sender, RoutedEventArgs e)
        {
            _moisCourant = _moisCourant.AddMonths(-1);
            Dispatcher.BeginInvoke(new Action(() => AfficherCalendrier()), 
                System.Windows.Threading.DispatcherPriority.ContextIdle);
        }

        private void BtnMoisSuivant_Click(object sender, RoutedEventArgs e)
        {
            _moisCourant = _moisCourant.AddMonths(1);
            Dispatcher.BeginInvoke(new Action(() => AfficherCalendrier()), 
                System.Windows.Threading.DispatcherPriority.ContextIdle);
        }

        private void BtnAujourdhui_Click(object sender, RoutedEventArgs e)
        {
            _moisCourant = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            Dispatcher.BeginInvoke(new Action(() => AfficherCalendrier()), 
                System.Windows.Threading.DispatcherPriority.ContextIdle);
        }

        private void BtnAssignerSemaine_Click(object sender, RoutedEventArgs e)
        {
            var utilisateurConnecte = _authService.CurrentUser;
            
            // Créer un dialogue personnalisé pour choisir la plage de dates
            var dialog = new Window
            {
                Title = "S'assigner plusieurs jours - Planning VM",
                Width = 500,
                Height = 380,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F5F5F5"))
            };

            var mainStack = new StackPanel { Margin = new Thickness(20) };
            
            // Titre
            var titre = new TextBlock
            {
                Text = "📋 Sélection de la période",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00915A")),
                Margin = new Thickness(0, 0, 0, 20)
            };
            mainStack.Children.Add(titre);

            // Option 1: Semaine en cours
            var btnSemaineCourante = new Button
            {
                Content = "📅 Semaine en cours (lundi - vendredi)",
                Height = 45,
                Margin = new Thickness(0, 0, 0, 10),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00915A")),
                Foreground = Brushes.White,
                FontWeight = FontWeights.SemiBold,
                Cursor = Cursors.Hand,
                Tag = "semaine_courante"
            };
            mainStack.Children.Add(btnSemaineCourante);

            // Séparateur
            var separateur = new Border
            {
                Height = 1,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0")),
                Margin = new Thickness(0, 15, 0, 15)
            };
            mainStack.Children.Add(separateur);

            // Option 2: Plage personnalisée
            var labelPersonnalise = new TextBlock
            {
                Text = "🗓️ Plage personnalisée (jours ouvrables uniquement)",
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 10)
            };
            mainStack.Children.Add(labelPersonnalise);

            // Date de début
            var stackDebut = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
            stackDebut.Children.Add(new TextBlock { Text = "Du :", Width = 80, VerticalAlignment = VerticalAlignment.Center, FontWeight = FontWeights.Medium });
            var dateDebut = new DatePicker { Width = 200, SelectedDate = DateTime.Now };
            stackDebut.Children.Add(dateDebut);
            mainStack.Children.Add(stackDebut);

            // Date de fin
            var stackFin = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 20) };
            stackFin.Children.Add(new TextBlock { Text = "Au :", Width = 80, VerticalAlignment = VerticalAlignment.Center, FontWeight = FontWeights.Medium });
            var dateFin = new DatePicker { Width = 200, SelectedDate = DateTime.Now.AddDays(7) };
            stackFin.Children.Add(dateFin);
            mainStack.Children.Add(stackFin);

            // Bouton valider plage personnalisée
            var btnPersonnalise = new Button
            {
                Content = "✓ Valider la plage personnalisée",
                Height = 45,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#007A4D")),
                Foreground = Brushes.White,
                FontWeight = FontWeights.SemiBold,
                Cursor = Cursors.Hand,
                Tag = "personnalise"
            };
            mainStack.Children.Add(btnPersonnalise);

            // Bouton annuler
            var btnAnnuler = new Button
            {
                Content = "✕ Annuler",
                Height = 40,
                Margin = new Thickness(0, 10, 0, 0),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0")),
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333")),
                FontWeight = FontWeights.Medium,
                Cursor = Cursors.Hand
            };
            mainStack.Children.Add(btnAnnuler);

            dialog.Content = mainStack;

            DateTime? dateDebutSelectionnee = null;
            DateTime? dateFinSelectionnee = null;

            btnSemaineCourante.Click += (s, ev) =>
            {
                // Semaine en cours
                var aujourdhui = DateTime.Now.Date;
                var jourSemaine = (int)aujourdhui.DayOfWeek;
                var offsetVersLundi = jourSemaine == 0 ? -6 : 1 - jourSemaine;
                dateDebutSelectionnee = aujourdhui.AddDays(offsetVersLundi);
                dateFinSelectionnee = dateDebutSelectionnee.Value.AddDays(4); // Vendredi
                dialog.DialogResult = true;
                dialog.Close();
            };

            btnPersonnalise.Click += (s, ev) =>
            {
                if (!dateDebut.SelectedDate.HasValue || !dateFin.SelectedDate.HasValue)
                {
                    MessageBox.Show("Veuillez sélectionner une date de début et une date de fin.", "Planning VM", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (dateDebut.SelectedDate.Value > dateFin.SelectedDate.Value)
                {
                    MessageBox.Show("La date de début doit être antérieure à la date de fin.", "Planning VM", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                dateDebutSelectionnee = dateDebut.SelectedDate.Value.Date;
                dateFinSelectionnee = dateFin.SelectedDate.Value.Date;
                dialog.DialogResult = true;
                dialog.Close();
            };

            btnAnnuler.Click += (s, ev) =>
            {
                dialog.DialogResult = false;
                dialog.Close();
            };

            var result = dialog.ShowDialog();
            
            if (result != true || !dateDebutSelectionnee.HasValue || !dateFinSelectionnee.HasValue)
                return;

            // Appeler la méthode d'assignation avec la plage sélectionnée
            AssignerSemaineComplete(dateDebutSelectionnee.Value, dateFinSelectionnee.Value, utilisateurConnecte.Id);
        }

        private void AssignerSemaineComplete(DateTime dateDebut, DateTime dateFin, int utilisateurId)
        {
            // Collecter tous les jours ouvrables dans la plage (lundi à vendredi)
            var joursASsigner = new List<DateTime>();
            var dateActuelle = dateDebut;
            while (dateActuelle <= dateFin)
            {
                // Exclure les weekends
                if (dateActuelle.DayOfWeek != DayOfWeek.Saturday && dateActuelle.DayOfWeek != DayOfWeek.Sunday)
                {
                    joursASsigner.Add(dateActuelle);
                }
                dateActuelle = dateActuelle.AddDays(1);
            }
            
            // Vérifier quels jours sont déjà assignés
            var planningsExistants = _database.GetPlanningsVM()
                .Where(p => p.EquipeId == _equipeId)
                .ToList();
            var joursDisponibles = new List<DateTime>();
            var joursDejaAssignes = new List<string>();
            
            foreach (var jour in joursASsigner)
            {
                var planningExistant = planningsExistants.FirstOrDefault(p => p.Date.Date == jour.Date);
                
                if (planningExistant != null)
                {
                    // Jour déjà assigné
                    if (planningExistant.UtilisateurId.HasValue)
                    {
                        var nomAssigne = GetNomUtilisateur(planningExistant.UtilisateurId.Value);
                        joursDejaAssignes.Add($"{jour:dddd dd/MM} → {nomAssigne}");
                    }
                }
                else
                {
                    // Vérifier si c'est un jour férié
                    if (!JoursFeriesService.EstJourFerie(jour))
                    {
                        joursDisponibles.Add(jour);
                    }
                }
            }
            
            // Afficher le résumé
            var message = $"Assignation pour la période du {dateDebut:dd/MM/yyyy} au {dateFin:dd/MM/yyyy}:\n\n";
            
            if (joursDisponibles.Count > 0)
            {
                message += $"✅ Jours disponibles ({joursDisponibles.Count}):\n";
                foreach (var jour in joursDisponibles)
                {
                    message += $"   • {jour:dddd dd/MM}\n";
                }
            }
            
            if (joursDejaAssignes.Count > 0)
            {
                message += $"\n❌ Jours déjà assignés ({joursDejaAssignes.Count}):\n";
                foreach (var info in joursDejaAssignes)
                {
                    message += $"   • {info}\n";
                }
            }
            
            if (joursDisponibles.Count == 0)
            {
                message += "\n⚠️ Aucun jour disponible dans cette période.";
                MessageBox.Show(message, "Planning VM", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            message += $"\n\nVoulez-vous vous assigner aux {joursDisponibles.Count} jour(s) disponible(s) ?";
            
            var resultatConfirmation = MessageBox.Show(message, "Planning VM", MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (resultatConfirmation == MessageBoxResult.Yes)
            {
                int nbAssignations = 0;
                try
                {
                    foreach (var jour in joursDisponibles)
                    {
                        var planning = new PlanningVMJour
                        {
                            EquipeId = _equipeId,
                            Date = jour.Date,
                            UtilisateurId = utilisateurId,
                            DateAssignation = DateTime.Now
                        };
                        
                        _database.AjouterPlanningVM(planning);
                        nbAssignations++;
                    }
                    
                    var loc = LocalizationService.Instance;
                    MessageBox.Show(string.Format(loc["PlanningVM_AssignedSuccess"], nbAssignations), 
                        loc["PlanningVM_Success"], MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    // Rafraîchir le calendrier
                    Dispatcher.BeginInvoke(new Action(() => AfficherCalendrier()), 
                        System.Windows.Threading.DispatcherPriority.ContextIdle);
                }
                catch (Exception ex)
                {
                    var loc = LocalizationService.Instance;
                    MessageBox.Show(string.Format(loc["PlanningVM_ErrorAfterAssign"], nbAssignations, ex.Message), 
                        loc["PlanningVM_Error"], MessageBoxButton.OK, MessageBoxImage.Error);
                    
                    // Rafraîchir quand même pour voir ce qui a été fait
                    Dispatcher.BeginInvoke(new Action(() => AfficherCalendrier()), 
                        System.Windows.Threading.DispatcherPriority.ContextIdle);
                }
            }
        }

        private void OnJourClick(JourCalendrierVM jour)
        {
            if (jour != null)
            {
                // Ne pas permettre de cliquer sur les weekends ou jours fériés
                if (jour.EstWeekend || jour.EstJourFerie)
                {
                    MessageBox.Show("Impossible d'assigner un membre sur un weekend ou un jour férié.", 
                        "Planning VM", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Ne pas permettre de cliquer sur les jours hors du mois
                if (jour.EstHorsMois)
                {
                    return;
                }

                // Afficher le menu contextuel
                AfficherMenuJour(jour);
            }
        }

        private void AfficherMenuJour(JourCalendrierVM jour)
        {
            var utilisateurConnecte = _authService.CurrentUser;
            
            // Vérifier s'il y a une demande d'échange pour ce jour
            var demandesEnAttente = _database.GetDemandesEchangeVMEnAttentePourUtilisateur(utilisateurConnecte.Id);
            var demandesPourCeJour = demandesEnAttente.Where(d => d.PlanningVMJourId == jour.PlanningId).ToList();
            
            if (demandesPourCeJour.Any())
            {
                // Il y a une demande d'échange pour ce jour
                var demande = demandesPourCeJour.First();
                var nomDemandeur = GetNomUtilisateur(demande.UtilisateurDemandeurId);
                
                var result = MessageBox.Show(
                    $"🔄 {nomDemandeur} vous demande de prendre sa place pour gérer la VM le {jour.Date:dddd dd MMMM yyyy}.\n\n" +
                    $"Voulez-vous accepter cet échange ?",
                    "Demande d'échange - Planning VM",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    AccepterEchange(demande, jour);
                }
                return;
            }
            
            if (jour.UtilisateurId == null)
            {
                // Jour libre - proposer de s'assigner
                AfficherDialogueAssignation(jour);
            }
            else if (jour.UtilisateurId == utilisateurConnecte.Id)
            {
                // L'utilisateur est déjà assigné - proposer de se désister ou demander un échange
                AfficherMenuUtilisateurAssigne(jour);
            }
            else
            {
                // Un autre membre est assigné - afficher l'info
                var nomMembre = GetNomUtilisateur(jour.UtilisateurId.Value);
                var loc = LocalizationService.Instance;
                MessageBox.Show(string.Format(loc["PlanningVM_AlreadyAssigned"], nomMembre, nomMembre), 
                    loc["PlanningVM_Title"], MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void AfficherDialogueAssignation(JourCalendrierVM jour)
        {
            var utilisateurConnecte = _authService.CurrentUser;
            var loc = LocalizationService.Instance;
            var culture = loc.CurrentCulture;
            
            // Créer une fenêtre de choix
            var dialog = new Window
            {
                Title = $"{loc["PlanningVM_Title"]} - {jour.Date.ToString("dddd dd MMMM yyyy", culture)}",
                Width = 450,
                Height = 230,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F5F5F5"))
            };

            var mainStack = new StackPanel { Margin = new Thickness(20) };
            
            // Message
            var message = new TextBlock
            {
                Text = loc["PlanningVM_AssignQuestion"],
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 20),
                FontWeight = FontWeights.Medium
            };
            mainStack.Children.Add(message);

            // Option 1: Ce jour uniquement
            var btnJourUniquement = new Button
            {
                Content = string.Format(loc["PlanningVM_OnlyThisDay"], jour.Date.ToString("dddd dd/MM", culture)),
                Height = 45,
                Margin = new Thickness(0, 0, 0, 10),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00915A")),
                Foreground = Brushes.White,
                FontWeight = FontWeights.SemiBold,
                FontSize = 13,
                Cursor = Cursors.Hand,
                Tag = "jour_unique"
            };
            mainStack.Children.Add(btnJourUniquement);

            // Option 2: Toute la semaine
            var lundiDeLaSemaine = jour.Date.AddDays(-(int)jour.Date.DayOfWeek + (int)DayOfWeek.Monday);
            if (jour.Date.DayOfWeek == DayOfWeek.Sunday) lundiDeLaSemaine = lundiDeLaSemaine.AddDays(-7);
            var vendrediDeLaSemaine = lundiDeLaSemaine.AddDays(4);
            
            var btnTouteLaSemaine = new Button
            {
                Content = string.Format(loc["PlanningVM_WholeWeek"], lundiDeLaSemaine.ToString("dd/MM"), vendrediDeLaSemaine.ToString("dd/MM")),
                Height = 45,
                Margin = new Thickness(0, 0, 0, 10),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#007A4D")),
                Foreground = Brushes.White,
                FontWeight = FontWeights.SemiBold,
                FontSize = 13,
                Cursor = Cursors.Hand,
                Tag = "semaine_complete"
            };
            mainStack.Children.Add(btnTouteLaSemaine);

            // Bouton annuler
            var btnAnnuler = new Button
            {
                Content = "✕ Annuler",
                Height = 35,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0")),
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333")),
                FontWeight = FontWeights.Medium,
                Cursor = Cursors.Hand
            };
            mainStack.Children.Add(btnAnnuler);

            dialog.Content = mainStack;

            string choix = null;

            btnJourUniquement.Click += (s, ev) =>
            {
                choix = "jour_unique";
                dialog.Close();
            };

            btnTouteLaSemaine.Click += (s, ev) =>
            {
                choix = "semaine_complete";
                dialog.Close();
            };

            btnAnnuler.Click += (s, ev) =>
            {
                dialog.Close();
            };

            dialog.ShowDialog();

            if (choix == "jour_unique")
            {
                // Assigner ce jour uniquement
                AssignerUtilisateur(jour.Date, utilisateurConnecte.Id);
            }
            else if (choix == "semaine_complete")
            {
                // Assigner toute la semaine
                AssignerSemaineComplete(lundiDeLaSemaine, vendrediDeLaSemaine, utilisateurConnecte.Id);
            }
        }

        private void AfficherMenuUtilisateurAssigne(JourCalendrierVM jour)
        {
            var dialog = new AssignationMenuDialog(jour.Date, _membresEquipe, _authService.CurrentUser);
            if (dialog.ShowDialog() == true && dialog.Action == "DEMANDER_ECHANGE" && dialog.MembreSelectionne != null)
            {
                DemanderEchange(jour, dialog.MembreSelectionne);
            }
        }

        private void AssignerUtilisateur(DateTime date, int utilisateurId)
        {
            try
            {
                var planning = new PlanningVMJour
                {
                    EquipeId = _equipeId,
                    Date = date.Date,
                    UtilisateurId = utilisateurId,
                    DateAssignation = DateTime.Now
                };

                _database.AjouterPlanningVM(planning);
                var loc = LocalizationService.Instance;
                
                MessageBox.Show(loc["PlanningVM_AssignSuccess"], 
                    loc["PlanningVM_Title"], MessageBoxButton.OK, MessageBoxImage.Information);
                
                // Rafraîchir le calendrier
                Dispatcher.BeginInvoke(new Action(() => AfficherCalendrier()), 
                    System.Windows.Threading.DispatcherPriority.ContextIdle);
            }
            catch (Exception ex)
            {
                var loc = LocalizationService.Instance;
                MessageBox.Show($"{loc["PlanningVM_AssignError"]}:\n\n{ex.GetType().Name}\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}", 
                    loc["PlanningVM_Error"], MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DemanderEchange(JourCalendrierVM jour, Utilisateur membreCible)
        {
            try
            {
                if (!jour.PlanningId.HasValue)
                    return;

                var demande = new DemandeEchangeVM
                {
                    PlanningVMJourId = jour.PlanningId.Value,
                    UtilisateurDemandeurId = _authService.CurrentUser.Id,
                    UtilisateurCibleId = membreCible.Id,
                    DateDemande = DateTime.Now,
                    Statut = "EN_ATTENTE",
                    Message = $"Demande d'échange pour le {jour.Date:dd/MM/yyyy}"
                };

                _database.AjouterDemandeEchangeVM(demande);

                // L'ID est maintenant dans demande.Id après l'insertion
                int demandeId = demande.Id;
                
                if (demandeId <= 0)
                {
                    throw new Exception("L'ID de la demande n'a pas été retourné correctement.");
                }

                // Créer une notification pour le membre cible
                var utilisateurDemandeur = _authService.CurrentUser;
                var loc = LocalizationService.Instance;
                var culture = loc.CurrentCulture;
                
                var notification = new Notification
                {
                    Titre = loc["PlanningVM_NotifExchangeTitle"],
                    Message = string.Format(loc["PlanningVM_NotifExchangeMessage"], 
                        utilisateurDemandeur.Prenom, utilisateurDemandeur.Nom, 
                        jour.Date.ToString("dddd dd MMMM yyyy", culture)),
                    Type = NotificationType.Info,
                    DateCreation = DateTime.Now,
                    EstLue = false,
                    DemandeEchangeVMId = demandeId
                };

                try
                {
                    _database.AjouterNotification(notification, membreCible.Id);
                    LoggingService.Instance.LogInfo($"Notification créée pour utilisateur {membreCible.Id}, demande {demandeId}");
                }
                catch (Exception exNotif)
                {
                    LoggingService.Instance.LogError($"Erreur création notification pour cible: {exNotif.Message}", exNotif);
                    throw;
                }
                
                // Créer une notification pour le demandeur (pour qu'il puisse annuler)
                var notificationDemandeur = new Notification
                {
                    Titre = loc["PlanningVM_NotifSentTitle"],
                    Message = string.Format(loc["PlanningVM_NotifSentMessage"], 
                        membreCible.Prenom, membreCible.Nom, 
                        jour.Date.ToString("dddd dd MMMM yyyy", culture)),
                    Type = NotificationType.Info,
                    DateCreation = DateTime.Now,
                    EstLue = false,
                    DemandeEchangeVMId = demandeId
                };
                
                try
                {
                    _database.AjouterNotification(notificationDemandeur, utilisateurDemandeur.Id);
                    LoggingService.Instance.LogInfo($"Notification créée pour demandeur {utilisateurDemandeur.Id}, demande {demandeId}");
                }
                catch (Exception exNotif)
                {
                    LoggingService.Instance.LogError($"Erreur création notification pour demandeur: {exNotif.Message}", exNotif);
                    throw;
                }

                MessageBox.Show(string.Format(loc["PlanningVM_ExchangeSent"], membreCible.Prenom, membreCible.Nom), 
                    loc["PlanningVM_RequestSent"], MessageBoxButton.OK, MessageBoxImage.Information);

                // Différer le rafraîchissement pour éviter les conflits avec l'événement de clic
                Dispatcher.BeginInvoke(new Action(() => AfficherCalendrier()), 
                    System.Windows.Threading.DispatcherPriority.ContextIdle);
            }
            catch (Exception ex)
            {
                var loc = LocalizationService.Instance;
                MessageBox.Show(string.Format(loc["PlanningVM_ExchangeError"], ex.Message), 
                    loc["PlanningVM_Error"], MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void AccepterEchange(DemandeEchangeVM demande, JourCalendrierVM jour)
        {
            try
            {
                // Accepter l'échange : l'utilisateur cible prend la place du demandeur
                _database.AccepterEchangeVM(
                    demande.Id, 
                    demande.PlanningVMJourId, 
                    demande.UtilisateurDemandeurId, 
                    demande.UtilisateurCibleId);
                
                // Créer une notification pour le demandeur
                var utilisateurCible = _authService.CurrentUser;
                var utilisateurDemandeur = _database.GetUtilisateurs().FirstOrDefault(u => u.Id == demande.UtilisateurDemandeurId);
                var loc = LocalizationService.Instance;
                var culture = loc.CurrentCulture;
                
                if (utilisateurDemandeur != null)
                {
                    var notificationConfirmation = new Notification
                    {
                        Titre = loc["PlanningVM_NotifAcceptedTitle"],
                        Message = string.Format(loc["PlanningVM_NotifAcceptedMessage"], 
                            utilisateurCible.Prenom, utilisateurCible.Nom, 
                            jour.Date.ToString("dddd dd MMMM yyyy", culture)),
                        Type = NotificationType.Success,
                        DateCreation = DateTime.Now,
                        EstLue = false
                    };
                    
                    _database.AjouterNotification(notificationConfirmation, demande.UtilisateurDemandeurId);
                }
                
                MessageBox.Show(string.Format(loc["PlanningVM_AcceptExchange"], jour.Date.ToString("dddd dd MMMM yyyy", culture)), 
                    loc["PlanningVM_Confirmation"], MessageBoxButton.OK, MessageBoxImage.Information);
                
                // Rafraîchir le calendrier
                Dispatcher.BeginInvoke(new Action(() => AfficherCalendrier()), 
                    System.Windows.Threading.DispatcherPriority.ContextIdle);
            }
            catch (Exception ex)
            {
                var loc = LocalizationService.Instance;
                MessageBox.Show(string.Format(loc["PlanningVM_AssignError"], ex.Message), 
                    loc["PlanningVM_Error"], MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    // ViewModel pour un jour du calendrier
    public class JourCalendrierVM : INotifyPropertyChanged
    {
        public DateTime Date { get; set; }
        public string Numero { get; set; }
        public bool EstHorsMois { get; set; }
        public bool EstAujourdhui { get; set; }
        public bool EstWeekend { get; set; }
        public bool EstJourFerie { get; set; }
        public string NomJourFerie { get; set; }
        public string IconeJourFerie { get; set; }
        public int? PlanningId { get; set; }
        public int? UtilisateurId { get; set; }
        public string NomMembre { get; set; }
        public bool ADemandeEchange { get; set; }

        public Visibility VisibiliteJourFerie => 
            EstJourFerie ? Visibility.Visible : Visibility.Collapsed;
        
        public Visibility VisibiliteAssignation => 
            !string.IsNullOrEmpty(NomMembre) && !EstJourFerie ? Visibility.Visible : Visibility.Collapsed;
        
        public Visibility VisibiliteDemandeEchange => 
            ADemandeEchange ? Visibility.Visible : Visibility.Collapsed;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Dialogue pour le menu d'assignation
    public class AssignationMenuDialog : Window
    {
        public string Action { get; private set; }
        public Utilisateur MembreSelectionne { get; private set; }

        public AssignationMenuDialog(DateTime date, List<Utilisateur> membresEquipe, Utilisateur utilisateurConnecte)
        {
            var loc = LocalizationService.Instance;
            var culture = loc.CurrentCulture;
            
            Title = loc["PlanningVM_ExchangeRequest"];
            Width = 400;
            Height = 280;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = System.Windows.Media.Brushes.White;

            var grid = new Grid { Margin = new Thickness(20) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Titre
            var titre = new TextBlock
            {
                Text = string.Format(loc["PlanningVM_AssignedOn"], date.ToString("dddd dd MMMM yyyy", culture)),
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 20)
            };
            Grid.SetRow(titre, 0);
            grid.Children.Add(titre);

            // Liste des membres pour l'échange
            var panel = new StackPanel { Margin = new Thickness(0, 0, 0, 15) };
            var label = new TextBlock
            {
                Text = loc["PlanningVM_RequestExchangeLabel"],
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 10)
            };
            panel.Children.Add(label);

            var comboBox = new ComboBox
            {
                ItemsSource = membresEquipe.Where(m => m.Id != utilisateurConnecte.Id).ToList(),
                DisplayMemberPath = "NomComplet",
                Height = 35,
                FontSize = 14
            };
            panel.Children.Add(comboBox);

            Grid.SetRow(panel, 1);
            grid.Children.Add(panel);

            // Grille pour les boutons (côte à côte)
            var boutonsGrid = new Grid { Margin = new Thickness(0, 20, 0, 0) };
            boutonsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            boutonsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) }); // Espacement
            boutonsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Bouton annuler (a gauche)
            var btnAnnuler = new Button
            {
                Content = loc["PlanningVM_Cancel"],
                Height = 40,
                Background = System.Windows.Media.Brushes.LightGray,
                Cursor = Cursors.Hand,
                BorderThickness = new Thickness(0),
                FontSize = 14
            };
            btnAnnuler.Click += (s, e) =>
            {
                DialogResult = false;
                Close();
            };
            Grid.SetColumn(btnAnnuler, 0);
            boutonsGrid.Children.Add(btnAnnuler);

            // Bouton valider (a droite)
            var btnDemanderEchange = new Button
            {
                Content = loc["PlanningVM_Validate"],
                Height = 40,
                Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0, 145, 90)),
                Foreground = System.Windows.Media.Brushes.White,
                FontWeight = FontWeights.SemiBold,
                Cursor = Cursors.Hand,
                BorderThickness = new Thickness(0),
                FontSize = 14
            };
            btnDemanderEchange.Click += (s, e) =>
            {
                if (comboBox.SelectedItem != null)
                {
                    Action = "DEMANDER_ECHANGE";
                    MembreSelectionne = (Utilisateur)comboBox.SelectedItem;
                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show(loc["PlanningVM_SelectMember"], loc["PlanningVM_Warning"], 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            };
            Grid.SetColumn(btnDemanderEchange, 2);
            boutonsGrid.Children.Add(btnDemanderEchange);

            Grid.SetRow(boutonsGrid, 2);
            grid.Children.Add(boutonsGrid);

            Content = grid;
        }
    }

    // RelayCommand pour gérer les clics sur les jours
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Predicate<T> _canExecute;

        public RelayCommand(Action<T> execute, Predicate<T> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute((T)parameter);
        }

        public void Execute(object parameter)
        {
            _execute((T)parameter);
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}

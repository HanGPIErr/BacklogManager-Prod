using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Threading;
using BacklogManager.Domain;
using BacklogManager.Services;
using BacklogManager.Shared;

namespace BacklogManager.ViewModels
{
    public class KanbanItemViewModel : INotifyPropertyChanged
    {
        private BacklogItem _item;
        private string _assignedDevName;
        private int _daysRemaining;
        private string _alertLevel;
        private int _daysSinceCreation;
        private double _chargeRestante;
        private double _chargePrevu;
        private double _chargeReelle;
        private double _avancement;
        private string _projetCouleur;

        public BacklogItem Item
        {
            get { return _item; }
            set 
            { 
                _item = value; 
                OnPropertyChanged();
                UpdateMetrics();
            }
        }

        public string AssignedDevName
        {
            get { return _assignedDevName; }
            set { _assignedDevName = value; OnPropertyChanged(); }
        }

        public string ProjetCouleur
        {
            get { return _projetCouleur; }
            set { _projetCouleur = value; OnPropertyChanged(); }
        }

        public int DaysRemaining
        {
            get { return _daysRemaining; }
            set { _daysRemaining = value; OnPropertyChanged(); OnPropertyChanged(nameof(TimeRemainingDisplay)); }
        }

        public string TimeRemainingDisplay
        {
            get
            {
                if (Item == null || !Item.DateFinAttendue.HasValue) return "";
                
                TimeSpan timeRemaining = Item.DateFinAttendue.Value - DateTime.Now;
                
                if (timeRemaining.TotalDays < 0)
                {
                    int hoursLate = (int)Math.Abs(timeRemaining.TotalHours);
                    return string.Format("-{0}h", hoursLate);
                }
                else if (timeRemaining.TotalDays < 1)
                {
                    int hoursRemaining = (int)Math.Ceiling(timeRemaining.TotalHours);
                    return string.Format("{0}h", hoursRemaining);
                }
                else
                {
                    return string.Format("{0}j", DaysRemaining);
                }
            }
        }

        public string AlertLevel
        {
            get { return _alertLevel; }
            set { _alertLevel = value; OnPropertyChanged(); }
        }

        public int DaysSinceCreation
        {
            get { return _daysSinceCreation; }
            set { _daysSinceCreation = value; OnPropertyChanged(); }
        }

        public double ChargeRestante
        {
            get { return _chargeRestante; }
            set { _chargeRestante = value; OnPropertyChanged(); }
        }

        public double ChargePrevu
        {
            get { return _chargePrevu; }
            set { _chargePrevu = value; OnPropertyChanged(); }
        }

        public double ChargeReelle
        {
            get { return _chargeReelle; }
            set { _chargeReelle = value; OnPropertyChanged(); }
        }

        public double Avancement
        {
            get { return _avancement; }
            set { _avancement = value; OnPropertyChanged(); }
        }

        public string AlertColor
        {
            get
            {
                if (AlertLevel == "URGENT") return "#D32F2F";
                if (AlertLevel == "ATTENTION") return "#F57C00";
                return "#4CAF50";
            }
        }

        public string DevDisplayName
        {
            get { return string.IsNullOrEmpty(AssignedDevName) ? "Non assigné" : AssignedDevName; }
        }

        public int DaysInStatus
        {
            get
            {
                if (Item == null) return 0;
                return (DateTime.Now - Item.DateDerniereMaj).Days;
            }
        }

        public string ChargeInfo
        {
            get
            {
                if (Item == null || !Item.ChiffrageHeures.HasValue) return "Non estimé";
                double joursRestants = ChargeRestante / 7.0;
                if (joursRestants >= 1)
                    return string.Format("{0:F1}j restant sur {1:F1}j", joursRestants, ChargePrevu);
                else if (joursRestants >= 0.5)
                    return string.Format("½j restant sur {0:F1}j", ChargePrevu);
                else if (joursRestants > 0)
                    return string.Format("< ½j restant sur {0:F1}j", ChargePrevu);
                else
                    return string.Format("Terminé ({0:F1}j)", ChargeReelle);
            }
        }

        public string ChargeDetailInfo
        {
            get
            {
                if (Item == null || !Item.ChiffrageHeures.HasValue) return "";
                return string.Format("Prévu: {0:F1}j | Passé: {1:F1}j | Restant: {2:F1}j", 
                    ChargePrevu, ChargeReelle, ChargeRestante / 8.0); // 8h = 1j
            }
        }

        public string AvancementInfo
        {
            get { return string.Format("{0:F0}%", Avancement); }
        }

        private void UpdateMetrics()
        {
            if (Item == null) return;

            DaysSinceCreation = (DateTime.Now - Item.DateCreation).Days;

            const double HEURES_PAR_JOUR = 8.0; // 1 jour = 8 heures

            // Calcul de la charge en jours (8h = 1j)
            if (Item.ChiffrageHeures.HasValue)
            {
                ChargePrevu = Item.ChiffrageHeures.Value / HEURES_PAR_JOUR;
                double tempsPasséHeures = Item.TempsReelHeures ?? 0;
                ChargeReelle = tempsPasséHeures / HEURES_PAR_JOUR;
                ChargeRestante = Math.Max(0, Item.ChiffrageHeures.Value - tempsPasséHeures);
                
                // Calcul de l'avancement
                if (Item.ChiffrageHeures.Value > 0)
                {
                    Avancement = (tempsPasséHeures / Item.ChiffrageHeures.Value) * 100;
                    Avancement = Math.Min(100, Avancement);
                }
            }
            else
            {
                ChargePrevu = 0;
                ChargeReelle = 0;
                ChargeRestante = 0;
                Avancement = Item.Statut == Statut.Termine ? 100 : 0;
            }

            // Alertes basées sur la deadline ou la complexité
            if (Item.DateFinAttendue.HasValue)
            {
                TimeSpan timeRemaining = Item.DateFinAttendue.Value - DateTime.Now;
                DaysRemaining = (int)timeRemaining.TotalDays;
                
                if (timeRemaining.TotalHours < 0)
                    AlertLevel = "URGENT";
                else if (timeRemaining.TotalHours <= 48)
                    AlertLevel = "ATTENTION";
                else
                    AlertLevel = "OK";
            }
            else if (Item.Complexite.HasValue)
            {
                double estimatedDays = Item.Complexite.Value * 1.25;
                double elapsedDays = (DateTime.Now - Item.DateCreation).Days;
                DaysRemaining = (int)(estimatedDays - elapsedDays);

                if (DaysRemaining < 0)
                    AlertLevel = "URGENT";
                else if (DaysRemaining <= 2)
                    AlertLevel = "ATTENTION";
                else
                    AlertLevel = "OK";
            }
            else
            {
                DaysRemaining = 0;
                AlertLevel = DaysSinceCreation > 7 ? "ATTENTION" : "OK";
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class KanbanViewModel : INotifyPropertyChanged
    {
        private readonly BacklogService _backlogService;
        private readonly PermissionService _permissionService;
        private readonly CRAService _craService;
        private int? _selectedDevId;
        private int? _selectedProjetId;
        private bool _isLoading;

        public ObservableCollection<KanbanItemViewModel> ItemsEnAttente { get; set; }
        public ObservableCollection<KanbanItemViewModel> ItemsAPrioriser { get; set; }
        public ObservableCollection<KanbanItemViewModel> ItemsAfaire { get; set; }
        public ObservableCollection<KanbanItemViewModel> ItemsEnCours { get; set; }
        public ObservableCollection<KanbanItemViewModel> ItemsEnTest { get; set; }
        public ObservableCollection<KanbanItemViewModel> ItemsTermine { get; set; }
        public ObservableCollection<Dev> Devs { get; set; }
        public ObservableCollection<Projet> Projets { get; set; }

        // Limites WIP (Work In Progress)
        private const int LIMITE_WIP_ENCOURS = 5;
        private const int LIMITE_WIP_ENTEST = 3;

        // Compteurs WIP avec alertes
        private int _countEnCours;
        public int CountEnCours
        {
            get { return _countEnCours; }
            set { _countEnCours = value; OnPropertyChanged(); OnPropertyChanged(nameof(AlerteWipEnCours)); }
        }

        private int _countEnTest;
        public int CountEnTest
        {
            get { return _countEnTest; }
            set { _countEnTest = value; OnPropertyChanged(); OnPropertyChanged(nameof(AlerteWipEnTest)); }
        }

        public bool AlerteWipEnCours => CountEnCours > LIMITE_WIP_ENCOURS;
        public bool AlerteWipEnTest => CountEnTest > LIMITE_WIP_ENTEST;

        // Recherche
        private string _rechercheTexte;
        public string RechercheTexte
        {
            get { return _rechercheTexte; }
            set
            {
                if (_rechercheTexte != value)
                {
                    _rechercheTexte = value;
                    OnPropertyChanged();
                    System.Windows.Application.Current.Dispatcher.BeginInvoke(
                        DispatcherPriority.Background,
                        new Action(() => LoadItems()));
                }
            }
        }

        public bool PeutSupprimerTaches => _permissionService.PeutSupprimerTaches;
        public bool EstAdministrateur => _permissionService.EstAdministrateur;

        public int? SelectedDevId
        {
            get { return _selectedDevId; }
            set
            {
                if (_selectedDevId != value)
                {
                    _selectedDevId = value;
                    OnPropertyChanged();
                    System.Windows.Application.Current.Dispatcher.BeginInvoke(
                        DispatcherPriority.Background,
                        new Action(() => LoadItems()));
                }
            }
        }

        public int? SelectedProjetId
        {
            get { return _selectedProjetId; }
            set
            {
                if (_selectedProjetId != value)
                {
                    _selectedProjetId = value;
                    OnPropertyChanged();
                    System.Windows.Application.Current.Dispatcher.BeginInvoke(
                        DispatcherPriority.Background,
                        new Action(() => LoadItems()));
                }
            }
        }

        public ICommand MoveLeftCommand { get; }
        public ICommand MoveRightCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ClearFiltersCommand { get; }
        public ICommand MettreEnAttenteCommand { get; }
        public ICommand ReactiverTacheCommand { get; }
        public ICommand ArchiverTacheCommand { get; }

        // Événement pour notifier les changements de statut
        public event EventHandler TacheStatutChanged;

        public void OuvrirDetailsTache(BacklogItem tache)
        {
            var editWindow = new Views.EditTacheWindow(tache, _backlogService, _permissionService, _craService);
            
            var mainWindow = System.Windows.Application.Current.MainWindow;
            if (mainWindow != null && mainWindow != editWindow)
            {
                editWindow.Owner = mainWindow;
            }
            
            if (editWindow.ShowDialog() == true && editWindow.Saved)
            {
                LoadItems();
            }
        }

        public void ChangerStatutTache(BacklogItem tache, Statut nouveauStatut)
        {
            if (tache != null)
            {
                tache.Statut = nouveauStatut;
                tache.DateDerniereMaj = System.DateTime.Now;
                
                // Sauvegarder dans la base de données
                _backlogService.SaveBacklogItem(tache);
                
                // Rafraîchir l'affichage
                LoadItems();
                
                // Notifier les autres vues (Backlog)
                TacheStatutChanged?.Invoke(this, System.EventArgs.Empty);
            }
        }

        public KanbanViewModel(BacklogService backlogService, PermissionService permissionService = null, CRAService craService = null)
        {
            _backlogService = backlogService;
            _permissionService = permissionService;
            _craService = craService;
            
            ItemsEnAttente = new ObservableCollection<KanbanItemViewModel>();
            ItemsAPrioriser = new ObservableCollection<KanbanItemViewModel>();
            ItemsAfaire = new ObservableCollection<KanbanItemViewModel>();
            ItemsEnCours = new ObservableCollection<KanbanItemViewModel>();
            ItemsEnTest = new ObservableCollection<KanbanItemViewModel>();
            ItemsTermine = new ObservableCollection<KanbanItemViewModel>();
            Devs = new ObservableCollection<Dev>();
            Projets = new ObservableCollection<Projet>();

            MoveLeftCommand = new RelayCommand(item => MoveItemLeft((item as KanbanItemViewModel)?.Item ?? item as BacklogItem));
            MoveRightCommand = new RelayCommand(item => MoveItemRight((item as KanbanItemViewModel)?.Item ?? item as BacklogItem));
            RefreshCommand = new RelayCommand(_ => LoadItems());
            ClearFiltersCommand = new RelayCommand(_ => ClearFilters());
            MettreEnAttenteCommand = new RelayCommand(item => MettreEnAttente((item as KanbanItemViewModel)?.Item ?? item as BacklogItem));
            ReactiverTacheCommand = new RelayCommand(item => ReactiverTache(item as BacklogItem));
            ArchiverTacheCommand = new RelayCommand(item => ArchiverTache(item as BacklogItem), _ => EstAdministrateur);

            LoadFilterLists(); // Charger les listes une seule fois
            LoadItems();
        }

        private void LoadFilterLists()
        {
            var devs = _backlogService.GetAllDevs();
            var projets = _backlogService.GetAllProjets();

            Devs.Clear();
            // Ajouter une option "Tous" pour permettre de désélectionner
            Devs.Add(new Dev { Id = 0, Nom = "-- Tous les développeurs --" });
            foreach (var dev in devs)
            {
                Devs.Add(dev);
            }

            Projets.Clear();
            // Ajouter une option "Tous" pour permettre de désélectionner
            Projets.Add(new Projet { Id = 0, Nom = "-- Tous les projets --" });
            foreach (var projet in projets)
            {
                Projets.Add(projet);
            }
        }

        public void LoadItems()
        {
            var allItems = _backlogService.GetAllBacklogItems()
                .Where(i => i.EstVisibleDansKanban && !i.EstArchive) // Filtrer les tâches non-Kanban ET archivées
                .ToList();
            var devs = _backlogService.GetAllDevs();
            var projets = _backlogService.GetAllProjets();

            // Debug: afficher les données avant filtre
            System.Diagnostics.Debug.WriteLine($"[KANBAN] Items avant filtre: {allItems.Count}");
            System.Diagnostics.Debug.WriteLine($"[KANBAN] SelectedDevId: {_selectedDevId}");
            System.Diagnostics.Debug.WriteLine($"[KANBAN] SelectedProjetId: {_selectedProjetId}");
            
            // Debug: afficher combien de tâches ont un dev assigné
            var itemsAvecDev = allItems.Count(i => i.DevAssigneId.HasValue);
            var itemsAvecProjet = allItems.Count(i => i.ProjetId.HasValue);
            System.Diagnostics.Debug.WriteLine($"[KANBAN] Items avec DevAssigneId: {itemsAvecDev}/{allItems.Count}");
            System.Diagnostics.Debug.WriteLine($"[KANBAN] Items avec ProjetId: {itemsAvecProjet}/{allItems.Count}");

            // Filter by dev if selected (ignore 0 = "Tous")
            // IMPORTANT: On inclut aussi les tâches SANS dev assigné pour permettre de les voir et de les assigner
            if (_selectedDevId.HasValue && _selectedDevId.Value > 0)
            {
                allItems = allItems.Where(i => !i.DevAssigneId.HasValue || i.DevAssigneId == _selectedDevId.Value).ToList();
                System.Diagnostics.Debug.WriteLine($"[KANBAN] Items après filtre Dev (incluant non-assignés): {allItems.Count}");
            }

            // Filter by project if selected (ignore 0 = "Tous")
            // IMPORTANT: On inclut aussi les tâches SANS projet assigné pour permettre de les voir et de les assigner
            if (_selectedProjetId.HasValue && _selectedProjetId.Value > 0)
            {
                allItems = allItems.Where(i => !i.ProjetId.HasValue || i.ProjetId == _selectedProjetId.Value).ToList();
                System.Diagnostics.Debug.WriteLine($"[KANBAN] Items après filtre Projet (incluant non-assignés): {allItems.Count}");
            }

            // Filter by search text
            if (!string.IsNullOrWhiteSpace(_rechercheTexte))
            {
                string rechercheLower = _rechercheTexte.ToLower();
                allItems = allItems.Where(i => 
                    (i.Titre != null && i.Titre.ToLower().Contains(rechercheLower)) ||
                    (i.Description != null && i.Description.ToLower().Contains(rechercheLower))
                ).ToList();
                System.Diagnostics.Debug.WriteLine($"[KANBAN] Items après recherche '{_rechercheTexte}': {allItems.Count}");
            }

            ItemsEnAttente.Clear();
            ItemsAPrioriser.Clear();
            ItemsAfaire.Clear();
            ItemsEnCours.Clear();
            ItemsEnTest.Clear();
            ItemsTermine.Clear();

            foreach (var item in allItems.OrderByDescending(i => i.Priorite).ThenBy(i => i.DateCreation))
            {
                var dev = item.DevAssigneId.HasValue ? devs.FirstOrDefault(d => d.Id == item.DevAssigneId.Value) : null;
                
                // Calculer le temps réel depuis les CRA
                if (_craService != null)
                {
                    double tempsReelHeures = _craService.GetTempsReelTache(item.Id);
                    item.TempsReelHeures = tempsReelHeures;
                }

                // Récupérer la couleur du projet
                var projet = Projets.FirstOrDefault(p => p.Id == item.ProjetId);
                string couleurProjet = projet?.CouleurHex ?? "#E0E0E0"; // Gris par défaut
                
                var viewModel = new KanbanItemViewModel
                {
                    Item = item,
                    AssignedDevName = dev?.Nom,
                    ProjetCouleur = couleurProjet
                };

                switch (item.Statut)
                {
                    case Statut.EnAttente:
                        ItemsEnAttente.Add(viewModel);
                        break;
                    case Statut.APrioriser:
                        ItemsAPrioriser.Add(viewModel);
                        break;
                    case Statut.Afaire:
                        ItemsAfaire.Add(viewModel);
                        break;
                    case Statut.EnCours:
                        ItemsEnCours.Add(viewModel);
                        break;
                    case Statut.Test:
                        ItemsEnTest.Add(viewModel);
                        break;
                    case Statut.Termine:
                        ItemsTermine.Add(viewModel);
                        break;
                }
            }

            // Mettre à jour les compteurs WIP
            CountEnCours = ItemsEnCours.Count;
            CountEnTest = ItemsEnTest.Count;
        }

        private void MoveItemLeft(BacklogItem item)
        {
            if (item == null) return;

            var newStatus = item.Statut;
            switch (item.Statut)
            {
                case Statut.APrioriser:
                    newStatus = Statut.EnAttente;
                    break;
                case Statut.Afaire:
                    newStatus = Statut.APrioriser;
                    break;
                case Statut.EnCours:
                    newStatus = Statut.Afaire;
                    break;
                case Statut.Test:
                    newStatus = Statut.EnCours;
                    break;
                case Statut.Termine:
                    newStatus = Statut.Test;
                    break;
            }

            if (newStatus != item.Statut)
            {
                _backlogService.UpdateBacklogItemStatus(item.Id, newStatus);
                LoadItems();
            }
        }

        private void MoveItemRight(BacklogItem item)
        {
            if (item == null) return;

            var newStatus = item.Statut;
            switch (item.Statut)
            {
                case Statut.EnAttente:
                    newStatus = Statut.APrioriser;
                    break;
                case Statut.APrioriser:
                    newStatus = Statut.Afaire;
                    break;
                case Statut.Afaire:
                    newStatus = Statut.EnCours;
                    break;
                case Statut.EnCours:
                    newStatus = Statut.Test;
                    break;
                case Statut.Test:
                    newStatus = Statut.Termine;
                    break;
            }

            if (newStatus != item.Statut)
            {
                _backlogService.UpdateBacklogItemStatus(item.Id, newStatus);
                LoadItems();
            }
        }

        private void ClearFilters()
        {
            SelectedDevId = 0;  // 0 = "Tous"
            SelectedProjetId = 0;  // 0 = "Tous"
            LoadItems();
        }

        private void MettreEnAttente(BacklogItem item)
        {
            if (item == null) return;
            
            // Seul l'administrateur peut mettre une tâche en attente
            if (!_permissionService.EstAdministrateur)
            {
                System.Windows.MessageBox.Show(
                    "Seul l'administrateur peut mettre une tâche en attente.",
                    "Accès refusé",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

            _backlogService.UpdateBacklogItemStatus(item.Id, Statut.EnAttente);
            LoadItems();
        }

        private void ReactiverTache(BacklogItem item)
        {
            if (item == null) return;
            
            // Seul l'administrateur peut réactiver une tâche en attente ou à prioriser
            if (!_permissionService.EstAdministrateur)
            {
                System.Windows.MessageBox.Show(
                    "Seul l'administrateur peut réactiver cette tâche.",
                    "Accès refusé",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

            // Déterminer le nouveau statut selon l'état actuel
            Statut nouveauStatut = Statut.Afaire;
            
            if (item.Statut == Statut.EnAttente)
            {
                // Depuis "En attente", on peut aller vers "A prioriser" ou "A faire"
                var result = System.Windows.MessageBox.Show(
                    "Souhaitez-vous marquer cette tâche comme 'A prioriser' ?\n\nOui = A prioriser\nNon = A faire",
                    "Réactivation de la tâche",
                    System.Windows.MessageBoxButton.YesNoCancel,
                    System.Windows.MessageBoxImage.Question);
                
                if (result == System.Windows.MessageBoxResult.Cancel)
                    return;
                
                nouveauStatut = result == System.Windows.MessageBoxResult.Yes ? Statut.APrioriser : Statut.Afaire;
            }
            else if (item.Statut == Statut.APrioriser)
            {
                // Depuis "A prioriser", on va vers "A faire"
                nouveauStatut = Statut.Afaire;
            }

            _backlogService.UpdateBacklogItemStatus(item.Id, nouveauStatut);
            LoadItems();
        }

        public void SupprimerTache(BacklogItem task)
        {
            if (task == null) return;
            
            if (!_permissionService.PeutSupprimerTache(task))
            {
                throw new UnauthorizedAccessException("Vous n'avez pas les permissions pour supprimer cette tâche.");
            }

            _backlogService.DeleteBacklogItem(task.Id);
            LoadItems();
        }

        private void ArchiverTache(BacklogItem item)
        {
            if (item == null || !EstAdministrateur) return;

            var result = System.Windows.MessageBox.Show(
                $"Voulez-vous archiver la tâche \"{item.Titre}\" ?\n\nElle ne sera plus visible dans le Kanban et le Backlog.",
                "Archivage de tâche",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                item.EstArchive = true;
                item.DateDerniereMaj = DateTime.Now;
                _backlogService.SaveBacklogItem(item);
                LoadItems();
                
                System.Windows.MessageBox.Show(
                    "Tâche archivée avec succès !",
                    "Archivage",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

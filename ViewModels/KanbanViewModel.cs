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

        public int DaysRemaining
        {
            get { return _daysRemaining; }
            set { _daysRemaining = value; OnPropertyChanged(); }
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
                    ChargePrevu, ChargeReelle, ChargeRestante / 7.0);
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

            const double HEURES_PAR_JOUR = 7.0;

            // Calcul de la charge en jours (7h = 1j)
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
                DaysRemaining = (Item.DateFinAttendue.Value - DateTime.Now).Days;
                
                if (DaysRemaining < 0)
                    AlertLevel = "URGENT";
                else if (DaysRemaining <= 2)
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
        private int? _selectedDevId;
        private int? _selectedProjetId;
        private bool _isLoading;

        public ObservableCollection<KanbanItemViewModel> ItemsAfaire { get; set; }
        public ObservableCollection<KanbanItemViewModel> ItemsEnCours { get; set; }
        public ObservableCollection<KanbanItemViewModel> ItemsEnTest { get; set; }
        public ObservableCollection<KanbanItemViewModel> ItemsTermine { get; set; }
        public ObservableCollection<Dev> Devs { get; set; }
        public ObservableCollection<Projet> Projets { get; set; }

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

        public void OuvrirDetailsTache(BacklogItem tache)
        {
            var editWindow = new Views.EditTacheWindow(tache, _backlogService);
            
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

        public KanbanViewModel(BacklogService backlogService)
        {
            _backlogService = backlogService;
            
            ItemsAfaire = new ObservableCollection<KanbanItemViewModel>();
            ItemsEnCours = new ObservableCollection<KanbanItemViewModel>();
            ItemsEnTest = new ObservableCollection<KanbanItemViewModel>();
            ItemsTermine = new ObservableCollection<KanbanItemViewModel>();
            Devs = new ObservableCollection<Dev>();
            Projets = new ObservableCollection<Projet>();

            MoveLeftCommand = new RelayCommand(item => MoveItemLeft((item as KanbanItemViewModel)?.Item));
            MoveRightCommand = new RelayCommand(item => MoveItemRight((item as KanbanItemViewModel)?.Item));
            RefreshCommand = new RelayCommand(_ => LoadItems());
            ClearFiltersCommand = new RelayCommand(_ => ClearFilters());

            LoadItems();
        }

        public void LoadItems()
        {
            var allItems = _backlogService.GetAllBacklogItems();
            var devs = _backlogService.GetAllDevs();
            var projets = _backlogService.GetAllProjets();

            Devs.Clear();
            foreach (var dev in devs)
            {
                Devs.Add(dev);
            }

            Projets.Clear();
            foreach (var projet in projets)
            {
                Projets.Add(projet);
            }

            // Filter by dev if selected
            if (_selectedDevId.HasValue)
            {
                allItems = allItems.Where(i => i.DevAssigneId == _selectedDevId.Value).ToList();
            }

            // Filter by project if selected
            if (_selectedProjetId.HasValue)
            {
                allItems = allItems.Where(i => i.ProjetId == _selectedProjetId.Value).ToList();
            }

            ItemsAfaire.Clear();
            ItemsEnCours.Clear();
            ItemsEnTest.Clear();
            ItemsTermine.Clear();

            foreach (var item in allItems.OrderByDescending(i => i.Priorite).ThenBy(i => i.DateCreation))
            {
                var dev = item.DevAssigneId.HasValue ? devs.FirstOrDefault(d => d.Id == item.DevAssigneId.Value) : null;
                var viewModel = new KanbanItemViewModel
                {
                    Item = item,
                    AssignedDevName = dev?.Nom
                };

                switch (item.Statut)
                {
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
        }

        private void MoveItemLeft(BacklogItem item)
        {
            if (item == null) return;

            var newStatus = item.Statut;
            switch (item.Statut)
            {
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
            SelectedDevId = null;
            SelectedProjetId = null;
            LoadItems();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

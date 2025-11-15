using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using BacklogManager.Domain;
using BacklogManager.Services;
using BacklogManager.Shared;

namespace BacklogManager.ViewModels
{
    public class TimelineItemViewModel : INotifyPropertyChanged
    {
        private BacklogItem _item;
        private Dev _assignedDev;
        private Projet _projet;

        public BacklogItem Item
        {
            get { return _item; }
            set { _item = value; OnPropertyChanged(); }
        }

        public Dev AssignedDev
        {
            get { return _assignedDev; }
            set { _assignedDev = value; OnPropertyChanged(); }
        }

        public Projet Projet
        {
            get { return _projet; }
            set { _projet = value; OnPropertyChanged(); }
        }

        public string DevName
        {
            get { return AssignedDev != null ? AssignedDev.Nom : "Non assigné"; }
        }

        public string ProjetName
        {
            get { return Projet != null ? Projet.Nom : "Aucun projet"; }
        }

        public int DaysTotal
        {
            get
            {
                if (Item == null || Item.DateFinAttendue == null) return 0;
                return (Item.DateFinAttendue.Value - Item.DateCreation).Days;
            }
        }

        public int DaysElapsed
        {
            get
            {
                if (Item == null) return 0;
                return (DateTime.Now - Item.DateCreation).Days;
            }
        }

        public int DaysRemaining
        {
            get
            {
                if (Item == null || Item.DateFinAttendue == null) return 0;
                return (Item.DateFinAttendue.Value - DateTime.Now).Days;
            }
        }

        public double ProgressPercentage
        {
            get
            {
                if (DaysTotal <= 0) return 0;
                var progress = (double)DaysElapsed / DaysTotal * 100;
                return Math.Min(100, Math.Max(0, progress));
            }
        }

        public string ProgressStatus
        {
            get
            {
                if (Item == null || Item.DateFinAttendue == null) return "Pas de deadline";
                if (DaysRemaining < 0) return "EN RETARD";
                if (DaysRemaining == 0) return "AUJOURD'HUI";
                if (DaysRemaining <= 2) return "URGENT";
                return string.Format("{0} jours restants", DaysRemaining);
            }
        }

        public string ProgressColor
        {
            get
            {
                if (Item == null || Item.DateFinAttendue == null) return "#9E9E9E";
                if (DaysRemaining < 0) return "#D32F2F";
                if (DaysRemaining <= 2) return "#F57C00";
                return "#00915A";
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class TimelineViewModel : INotifyPropertyChanged
    {
        private readonly BacklogService _backlogService;
        private int? _selectedDevId;
        private int? _selectedProjetId;
        private Statut? _selectedStatut;

        public ObservableCollection<TimelineItemViewModel> TimelineItems { get; set; }
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
                    LoadItems();
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
                    LoadItems();
                }
            }
        }

        public Statut? SelectedStatut
        {
            get { return _selectedStatut; }
            set
            {
                if (_selectedStatut != value)
                {
                    _selectedStatut = value;
                    OnPropertyChanged();
                    LoadItems();
                }
            }
        }

        public ICommand RefreshCommand { get; }
        public ICommand ClearFiltersCommand { get; }

        public TimelineViewModel(BacklogService backlogService)
        {
            _backlogService = backlogService;

            TimelineItems = new ObservableCollection<TimelineItemViewModel>();
            Devs = new ObservableCollection<Dev>();
            Projets = new ObservableCollection<Projet>();

            RefreshCommand = new RelayCommand(_ => LoadItems());
            ClearFiltersCommand = new RelayCommand(_ => ClearFilters());

            LoadItems();
        }

        public void LoadItems()
        {
            var allItems = _backlogService.GetAllBacklogItems();
            var devs = _backlogService.GetAllDevs();
            var projets = _backlogService.GetAllProjets();

            // Charger les devs seulement si la collection est vide (éviter les doublons)
            if (Devs.Count == 0)
            {
                foreach (var dev in devs)
                {
                    Devs.Add(dev);
                }
            }

            // Charger les projets seulement si la collection est vide (éviter les doublons)
            if (Projets.Count == 0)
            {
                foreach (var projet in projets)
                {
                    Projets.Add(projet);
                }
            }

            // Apply filters
            if (_selectedDevId.HasValue)
            {
                allItems = allItems.Where(i => i.DevAssigneId == _selectedDevId.Value).ToList();
            }

            if (_selectedProjetId.HasValue)
            {
                allItems = allItems.Where(i => i.ProjetId == _selectedProjetId.Value).ToList();
            }

            if (_selectedStatut.HasValue)
            {
                allItems = allItems.Where(i => i.Statut == _selectedStatut.Value).ToList();
            }

            // Only show items with DateFinAttendue set
            allItems = allItems.Where(i => i.DateFinAttendue.HasValue).ToList();

            TimelineItems.Clear();
            foreach (var item in allItems.OrderBy(i => i.DateFinAttendue))
            {
                var dev = item.DevAssigneId.HasValue ? devs.FirstOrDefault(d => d.Id == item.DevAssigneId.Value) : null;
                var projet = item.ProjetId.HasValue ? projets.FirstOrDefault(p => p.Id == item.ProjetId.Value) : null;

                var viewModel = new TimelineItemViewModel
                {
                    Item = item,
                    AssignedDev = dev,
                    Projet = projet
                };

                TimelineItems.Add(viewModel);
            }
        }

        private void ClearFilters()
        {
            SelectedDevId = null;
            SelectedProjetId = null;
            SelectedStatut = null;
            LoadItems();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

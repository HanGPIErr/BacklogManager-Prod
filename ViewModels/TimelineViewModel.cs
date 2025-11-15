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

    public class TimelineProjetViewModel : INotifyPropertyChanged
    {
        public Projet Projet { get; set; }
        public DateTime DateDebut { get; set; }
        public DateTime DateFin { get; set; }
        public int NbTaches { get; set; }
        public int NbTachesTerminees { get; set; }
        public double ProgressionPct { get; set; }
        public double ChargeJours { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public string ProgressionInfo => $"{NbTachesTerminees}/{NbTaches} tâches ({ProgressionPct:F0}%)";
        public string ChargeInfo => $"{ChargeJours:F1}j";
        public string PeriodeInfo => $"{DateDebut:dd/MM/yyyy} → {DateFin:dd/MM/yyyy}";

        public double BarreLeft
        {
            get
            {
                var totalDays = (EndDate - StartDate).TotalDays;
                if (totalDays <= 0) return 0;
                var offsetDays = (DateDebut - StartDate).TotalDays;
                return Math.Max(0, Math.Min(100, (offsetDays / totalDays) * 100));
            }
        }

        public double BarreWidth
        {
            get
            {
                var totalDays = (EndDate - StartDate).TotalDays;
                if (totalDays <= 0) return 0;
                var projetDays = (DateFin - DateDebut).TotalDays;
                return Math.Max(0, Math.Min(100 - BarreLeft, (projetDays / totalDays) * 100));
            }
        }

        public string BarreColor
        {
            get
            {
                if (ProgressionPct >= 100) return "#4CAF50";
                if (ProgressionPct >= 75) return "#8BC34A";
                if (ProgressionPct >= 50) return "#FFC107";
                if (ProgressionPct >= 25) return "#FF9800";
                return "#F44336";
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class TimelineMonthViewModel
    {
        public DateTime Month { get; set; }
        public string Label { get; set; }
    }

    public class TimelineViewModel : INotifyPropertyChanged
    {
        private readonly BacklogService _backlogService;
        private int? _selectedDevId;
        private int? _selectedProjetId;
        private Statut? _selectedStatut;
        private DateTime _startDate;
        private DateTime _endDate;
        private string _selectedPeriode;

        public ObservableCollection<TimelineItemViewModel> TimelineItems { get; set; }
        public ObservableCollection<TimelineProjetViewModel> ProjetsTimeline { get; set; }
        public ObservableCollection<TimelineMonthViewModel> Months { get; set; }
        public ObservableCollection<Dev> Devs { get; set; }
        public ObservableCollection<Projet> Projets { get; set; }
        public ObservableCollection<string> Periodes { get; set; }
        public ObservableCollection<int> Annees { get; set; }
        
        private int _selectedAnnee;
        public int SelectedAnnee
        {
            get { return _selectedAnnee; }
            set 
            { 
                _selectedAnnee = value; 
                OnPropertyChanged();
                UpdateFromAnnee();
            }
        }
        
        private string _searchText;
        public string SearchText
        {
            get { return _searchText; }
            set { _searchText = value; OnPropertyChanged(); LoadProjetsTimeline(); }
        }
        
        public int TotalProjets => ProjetsTimeline.Count;
        public string ProjetsInfo => $"{TotalProjets} projet(s) affiché(s)";
        
        private bool _groupByMonth = true;
        public bool GroupByMonth
        {
            get { return _groupByMonth; }
            set { _groupByMonth = value; OnPropertyChanged(); LoadProjetsTimeline(); }
        }

        public DateTime StartDate
        {
            get { return _startDate; }
            set { _startDate = value; OnPropertyChanged(); LoadProjetsTimeline(); }
        }

        public DateTime EndDate
        {
            get { return _endDate; }
            set { _endDate = value; OnPropertyChanged(); LoadProjetsTimeline(); }
        }

        public string SelectedPeriode
        {
            get { return _selectedPeriode; }
            set 
            { 
                _selectedPeriode = value; 
                OnPropertyChanged();
                UpdatePeriode();
            }
        }

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
            ProjetsTimeline = new ObservableCollection<TimelineProjetViewModel>();
            Months = new ObservableCollection<TimelineMonthViewModel>();
            Devs = new ObservableCollection<Dev>();
            Projets = new ObservableCollection<Projet>();
            Annees = new ObservableCollection<int>();
            Periodes = new ObservableCollection<string>
            {
                "Trimestre actuel",
                "6 derniers mois",
                "Année complète",
                "Personnalisé"
            };

            // Générer les années (2020 à 2030)
            for (int year = 2020; year <= 2030; year++)
            {
                Annees.Add(year);
            }

            RefreshCommand = new RelayCommand(_ => { LoadItems(); LoadProjetsTimeline(); });
            ClearFiltersCommand = new RelayCommand(_ => ClearFilters());

            _selectedAnnee = DateTime.Now.Year;
            SelectedPeriode = "Année complète";
            LoadItems();
        }

        private void UpdateFromAnnee()
        {
            if (SelectedPeriode == "Année complète")
            {
                StartDate = new DateTime(SelectedAnnee, 1, 1);
                EndDate = new DateTime(SelectedAnnee, 12, 31);
            }
            else if (SelectedPeriode == "Trimestre actuel")
            {
                var quarter = (DateTime.Now.Month - 1) / 3;
                StartDate = new DateTime(SelectedAnnee, quarter * 3 + 1, 1);
                EndDate = StartDate.AddMonths(3).AddDays(-1);
            }
        }

        private void UpdatePeriode()
        {
            var today = DateTime.Now;
            switch (SelectedPeriode)
            {
                case "Trimestre actuel":
                    var quarter = (today.Month - 1) / 3;
                    StartDate = new DateTime(SelectedAnnee, quarter * 3 + 1, 1);
                    EndDate = StartDate.AddMonths(3).AddDays(-1);
                    break;
                case "6 derniers mois":
                    StartDate = new DateTime(SelectedAnnee, 1, 1);
                    EndDate = new DateTime(SelectedAnnee, 6, 30);
                    break;
                case "Année complète":
                    StartDate = new DateTime(SelectedAnnee, 1, 1);
                    EndDate = new DateTime(SelectedAnnee, 12, 31);
                    break;
                case "Personnalisé":
                    // Ne rien changer, l'utilisateur définira manuellement
                    break;
            }
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

        public void LoadProjetsTimeline()
        {
            var allProjets = _backlogService.GetAllProjets();
            var allTaches = _backlogService.GetAllBacklogItems();

            // Générer les mois
            Months.Clear();
            var currentMonth = new DateTime(StartDate.Year, StartDate.Month, 1);
            while (currentMonth <= EndDate)
            {
                Months.Add(new TimelineMonthViewModel
                {
                    Month = currentMonth,
                    Label = currentMonth.ToString("MMM yyyy")
                });
                currentMonth = currentMonth.AddMonths(1);
            }

            // Filtrer les projets
            var projets = allProjets.Where(p => p.Actif).ToList();

            if (SelectedProjetId.HasValue && SelectedProjetId.Value > 0)
            {
                projets = projets.Where(p => p.Id == SelectedProjetId.Value).ToList();
            }

            // Filtre par recherche textuelle
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                projets = projets.Where(p => p.Nom.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            }

            // Créer les ViewModels des projets
            ProjetsTimeline.Clear();
            foreach (var projet in projets)
            {
                var taches = allTaches.Where(t => t.ProjetId == projet.Id && !t.EstArchive).ToList();

                if (SelectedDevId.HasValue && SelectedDevId.Value > 0)
                {
                    taches = taches.Where(t => t.DevAssigneId == SelectedDevId.Value).ToList();
                }

                if (!taches.Any()) continue;

                var dateDebut = taches.Min(t => t.DateCreation);
                var dateFin = taches.Where(t => t.DateFinAttendue.HasValue)
                                    .Select(t => t.DateFinAttendue.Value)
                                    .DefaultIfEmpty(DateTime.Now.AddMonths(3))
                                    .Max();

                var nbTachesTotal = taches.Count;
                var nbTachesTerminees = taches.Count(t => t.Statut == Statut.Termine);
                var progressionPct = nbTachesTotal > 0 ? (nbTachesTerminees * 100.0 / nbTachesTotal) : 0;

                var chargeTotal = taches.Where(t => t.ChiffrageHeures.HasValue).Sum(t => t.ChiffrageHeures.Value) / 7.0;

                ProjetsTimeline.Add(new TimelineProjetViewModel
                {
                    Projet = projet,
                    DateDebut = dateDebut,
                    DateFin = dateFin,
                    NbTaches = nbTachesTotal,
                    NbTachesTerminees = nbTachesTerminees,
                    ProgressionPct = progressionPct,
                    ChargeJours = chargeTotal,
                    StartDate = StartDate,
                    EndDate = EndDate
                });
            }

            OnPropertyChanged(nameof(TotalProjets));
            OnPropertyChanged(nameof(ProjetsInfo));
        }

        private void ClearFilters()
        {
            SelectedDevId = null;
            SelectedProjetId = null;
            SelectedStatut = null;
            SearchText = "";
            SelectedAnnee = DateTime.Now.Year;
            SelectedPeriode = "Année complète";
            LoadItems();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

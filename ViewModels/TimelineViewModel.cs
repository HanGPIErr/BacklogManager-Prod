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
        private TimelineViewModel _parentViewModel;

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

        public TimelineViewModel ParentViewModel
        {
            get { return _parentViewModel; }
            set { _parentViewModel = value; OnPropertyChanged(); }
        }

        public DateTime StartDate 
        { 
            get { return _parentViewModel?.StartDate ?? DateTime.Now; }
        }
        
        public DateTime EndDate 
        { 
            get { return _parentViewModel?.EndDate ?? DateTime.Now.AddMonths(6); }
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
                var dateDebut = Item.DateDebut ?? Item.DateCreation;
                return (Item.DateFinAttendue.Value - dateDebut).Days;
            }
        }

        public int DaysElapsed
        {
            get
            {
                if (Item == null) return 0;
                var dateDebut = Item.DateDebut ?? Item.DateCreation;
                return (DateTime.Now - dateDebut).Days;
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
                if (Item == null) return 0;
                // Progression basée sur temps réel vs charge prévue (comme Kanban)
                if (Item.ChiffrageHeures.HasValue && Item.ChiffrageHeures.Value > 0)
                {
                    double tempsReel = Item.TempsReelHeures ?? 0;
                    return Math.Min(100, (tempsReel / Item.ChiffrageHeures.Value) * 100);
                }
                return 0;
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

        public double BarreLeft
        {
            get
            {
                if (Item == null) return 0;
                var dateDebut = Item.DateDebut ?? Item.DateCreation;
                var totalDays = (EndDate - StartDate).TotalDays;
                if (totalDays <= 0) return 0;
                var offsetDays = (dateDebut - StartDate).TotalDays;
                return Math.Max(0, Math.Min(100, (offsetDays / totalDays) * 100));
            }
        }

        public double BarreWidth
        {
            get
            {
                if (Item == null || Item.DateFinAttendue == null) return 0;
                var dateDebut = Item.DateDebut ?? Item.DateCreation;
                var dateFin = Item.DateFinAttendue.Value;
                var totalDays = (EndDate - StartDate).TotalDays;
                if (totalDays <= 0) return 0;
                var tacheDays = (dateFin - dateDebut).TotalDays;
                return Math.Max(0, Math.Min(100 - BarreLeft, (tacheDays / totalDays) * 100));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string propertyName = null)
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
        public double ChargeReelleHeures { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public string ProgressionInfo => $"{ChargeReelleHeures:F1}h / {ChargeJours * 7:F1}h ({ProgressionPct:F0}%)";
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
            set 
            { 
                _startDate = value; 
                OnPropertyChanged();
                // Notifier tous les TimelineItems que les dates ont changé
                foreach (var item in TimelineItems)
                {
                    item.OnPropertyChanged(nameof(item.StartDate));
                    item.OnPropertyChanged(nameof(item.BarreLeft));
                    item.OnPropertyChanged(nameof(item.BarreWidth));
                }
                LoadProjetsTimeline();
            }
        }

        public DateTime EndDate
        {
            get { return _endDate; }
            set 
            { 
                _endDate = value; 
                OnPropertyChanged();
                // Notifier tous les TimelineItems que les dates ont changé
                foreach (var item in TimelineItems)
                {
                    item.OnPropertyChanged(nameof(item.EndDate));
                    item.OnPropertyChanged(nameof(item.BarreLeft));
                    item.OnPropertyChanged(nameof(item.BarreWidth));
                }
                LoadProjetsTimeline();
            }
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

            // Initialiser avec l'année qui contient des tâches planifiées
            var allItems = _backlogService.GetAllBacklogItems();
            var itemsAvecDates = allItems.Where(i => i.DateDebut.HasValue || i.DateFinAttendue.HasValue).ToList();
            
            if (itemsAvecDates.Any())
            {
                // Prendre l'année de la plus proche date de début ou fin
                var dates = itemsAvecDates
                    .SelectMany(i => new[] { i.DateDebut, i.DateFinAttendue })
                    .Where(d => d.HasValue)
                    .Select(d => d.Value)
                    .ToList();
                    
                if (dates.Any())
                {
                    var minDate = dates.Min();
                    var maxDate = dates.Max();
                    // Prendre l'année qui contient le plus de tâches, ou la plus proche
                    _selectedAnnee = (minDate > DateTime.Now) ? minDate.Year : DateTime.Now.Year;
                }
                else
                {
                    _selectedAnnee = DateTime.Now.Year;
                }
            }
            else
            {
                _selectedAnnee = DateTime.Now.Year;
            }
            
            SelectedPeriode = "6 derniers mois";
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
                    // Calculer les 6 derniers mois depuis aujourd'hui
                    EndDate = new DateTime(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month));
                    StartDate = EndDate.AddMonths(-6).AddDays(1);
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
            var devs = _backlogService.GetAllDevs();
            var projets = _backlogService.GetAllProjets();
            
            // Recalculer les DateDebut depuis les CRA pour toutes les tâches
            var craService = new CRAService(_backlogService.Database);
            var allItemsForUpdate = _backlogService.GetAllBacklogItems();
            foreach (var item in allItemsForUpdate.Where(i => i.Id > 0))
            {
                var cras = craService.GetCRAsByBacklogItem(item.Id);
                if (cras.Any())
                {
                    // Prendre la date du PREMIER CRA (le plus ancien)
                    var premierCRA = cras.OrderBy(c => c.Date).First();
                    if (item.DateDebut != premierCRA.Date)
                    {
                        item.DateDebut = premierCRA.Date;
                        _backlogService.SaveBacklogItem(item);
                    }
                }
            }
            
            // Recharger APRÈS les mises à jour pour avoir les bonnes dates
            var allItems = _backlogService.GetAllBacklogItems();

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
                    Projet = projet,
                    ParentViewModel = this
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

                // Utiliser les dates du projet si disponibles, sinon calculer depuis les tâches
                var dateDebut = projet.DateDebut ?? 
                               (taches.Where(t => t.DateDebut.HasValue).Any()
                                   ? taches.Where(t => t.DateDebut.HasValue).Min(t => t.DateDebut.Value)
                                   : taches.Min(t => t.DateCreation));
                                   
                var dateFin = projet.DateFinPrevue ?? 
                             (taches.Where(t => t.DateFinAttendue.HasValue).Any() 
                                ? taches.Where(t => t.DateFinAttendue.HasValue).Max(t => t.DateFinAttendue.Value)
                                : DateTime.Now.AddMonths(3));

                var nbTachesTotal = taches.Count;
                var nbTachesTerminees = taches.Count(t => t.Statut == Statut.Termine);
                
                // Calculer progression basée sur temps réel vs charge prévue (comme Kanban)
                var chargePrevu = taches.Where(t => t.ChiffrageHeures.HasValue).Sum(t => t.ChiffrageHeures.Value);
                var chargeReelle = taches.Sum(t => t.TempsReelHeures ?? 0);
                var progressionPct = chargePrevu > 0 ? Math.Min(100, (chargeReelle / chargePrevu) * 100) : 0;

                var chargeTotal = chargePrevu / 7.0;

                ProjetsTimeline.Add(new TimelineProjetViewModel
                {
                    Projet = projet,
                    DateDebut = dateDebut,
                    DateFin = dateFin,
                    NbTaches = nbTachesTotal,
                    NbTachesTerminees = nbTachesTerminees,
                    ProgressionPct = progressionPct,
                    ChargeJours = chargeTotal,
                    ChargeReelleHeures = chargeReelle,
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

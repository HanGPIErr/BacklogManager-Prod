using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using BacklogManager.Domain;
using BacklogManager.Services;
using BacklogManager.Shared;

namespace BacklogManager.ViewModels
{
    public class CRADisplay : INotifyPropertyChanged
    {
        private int _id;
        private string _date;
        private string _devNom;
        private string _projetNom;
        private string _tacheNom;
        private double _heures;
        private string _commentaire;

        public int Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        public string Date
        {
            get => _date;
            set { _date = value; OnPropertyChanged(); }
        }

        public string DevNom
        {
            get => _devNom;
            set { _devNom = value; OnPropertyChanged(); }
        }

        public string ProjetNom
        {
            get => _projetNom;
            set { _projetNom = value; OnPropertyChanged(); }
        }

        public string TacheNom
        {
            get => _tacheNom;
            set { _tacheNom = value; OnPropertyChanged(); }
        }

        public double Heures
        {
            get => _heures;
            set { _heures = value; OnPropertyChanged(); OnPropertyChanged(nameof(Jours)); }
        }

        public double Jours => _heures / 8.0; // Conversion heures -> jours (1j = 8h)

        public string Commentaire
        {
            get => _commentaire;
            set { _commentaire = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class CRAHistoriqueViewModel : INotifyPropertyChanged
    {
        private readonly CRAService _craService;
        private readonly IDatabase _db;
        private readonly int _currentUserId;
        private readonly bool _isAdmin;

        private DateTime? _dateDebut;
        private DateTime? _dateFin;
        private Utilisateur _devFiltre;
        private Projet _projetFiltre;
        private BacklogItem _tacheFiltre;

        public ObservableCollection<CRADisplay> CRAs { get; set; }
        public ObservableCollection<Utilisateur> Devs { get; set; }
        public ObservableCollection<Projet> Projets { get; set; }
        public ObservableCollection<BacklogItem> Taches { get; set; }

        public DateTime? DateDebut
        {
            get => _dateDebut;
            set
            {
                if (_dateDebut != value)
                {
                    _dateDebut = value;
                    OnPropertyChanged();
                }
            }
        }

        public DateTime? DateFin
        {
            get => _dateFin;
            set
            {
                if (_dateFin != value)
                {
                    _dateFin = value;
                    OnPropertyChanged();
                }
            }
        }

        public Utilisateur DevFiltre
        {
            get => _devFiltre;
            set
            {
                if (_devFiltre != value)
                {
                    _devFiltre = value;
                    OnPropertyChanged();
                }
            }
        }

        public Projet ProjetFiltre
        {
            get => _projetFiltre;
            set
            {
                if (_projetFiltre != value)
                {
                    _projetFiltre = value;
                    OnPropertyChanged();
                }
            }
        }

        public BacklogItem TacheFiltre
        {
            get => _tacheFiltre;
            set
            {
                if (_tacheFiltre != value)
                {
                    _tacheFiltre = value;
                    OnPropertyChanged();
                }
            }
        }

        public int TotalCRA => CRAs?.Count ?? 0;
        public double TotalHeures => CRAs?.Sum(c => c.Heures) ?? 0;
        public double TotalJours => TotalHeures / 8.0; // Conversion en jours

        public bool CanFilterAllDevs => _isAdmin;

        public ICommand LoadCRAsCommand { get; }
        public ICommand DeleteCRACommand { get; }
        public ICommand FilterTodayCommand { get; }
        public ICommand FilterWeekCommand { get; }
        public ICommand FilterMonthCommand { get; }
        public ICommand FilterAllCommand { get; }
        public ICommand ExportCommand { get; }

        public CRAHistoriqueViewModel(IDatabase db, int currentUserId, bool isAdmin)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _craService = new CRAService(db);
            _currentUserId = currentUserId;
            _isAdmin = isAdmin;

            CRAs = new ObservableCollection<CRADisplay>();
            Devs = new ObservableCollection<Utilisateur>();
            Projets = new ObservableCollection<Projet>();
            Taches = new ObservableCollection<BacklogItem>();

            LoadCRAsCommand = new RelayCommand(_ => LoadCRAs(null));
            DeleteCRACommand = new RelayCommand(param => DeleteCRA((int)param));
            FilterTodayCommand = new RelayCommand(FilterToday);
            FilterWeekCommand = new RelayCommand(FilterWeek);
            FilterMonthCommand = new RelayCommand(FilterMonth);
            FilterAllCommand = new RelayCommand(FilterAll);
            ExportCommand = new RelayCommand(ExportCSV);

            LoadFilters();
            FilterMonth(null); // Défaut : mois en cours
        }

        private void LoadFilters()
        {
            // Devs
            Devs.Clear();
            var utilisateurs = _db.GetUtilisateurs()
                .Where(u => u.Actif)
                .OrderBy(u => u.Nom)
                .ToList();

            foreach (var user in utilisateurs)
            {
                Devs.Add(user);
            }

            // Projets
            Projets.Clear();
            var projets = _db.GetProjets().OrderBy(p => p.Nom).ToList();
            foreach (var projet in projets)
            {
                Projets.Add(projet);
            }

            // Taches
            Taches.Clear();
            var taches = _db.GetBacklogItems().OrderBy(t => t.Titre).ToList();
            foreach (var tache in taches)
            {
                Taches.Add(tache);
            }

            // Si pas admin, pré-sélection du dev courant
            if (!_isAdmin)
            {
                DevFiltre = Devs.FirstOrDefault(d => d.Id == _currentUserId);
            }
        }

        private void LoadCRAs(object parameter = null)
        {
            CRAs.Clear();

            try
            {
                // Récupération des CRA selon les filtres
                int? devId = null;
                if (!_isAdmin)
                {
                    devId = _currentUserId; // Dev ne voit que ses CRA
                }
                else if (DevFiltre != null)
                {
                    devId = DevFiltre.Id;
                }

                List<CRA> cras;
                if (TacheFiltre != null)
                {
                    cras = _craService.GetCRAsByBacklogItem(TacheFiltre.Id);
                }
                else
                {
                    cras = _craService.GetAllCRAs(DateDebut, DateFin);
                }

                // Filtrage
                if (devId.HasValue)
                {
                    cras = cras.Where(c => c.DevId == devId.Value).ToList();
                }

                if (ProjetFiltre != null)
                {
                    var tachesProjet = _db.GetBacklogItems()
                        .Where(b => b.ProjetId == ProjetFiltre.Id)
                        .Select(b => b.Id)
                        .ToList();
                    cras = cras.Where(c => tachesProjet.Contains(c.BacklogItemId)).ToList();
                }

                // Mapping vers CRADisplay
                var backlogItems = _db.GetBacklogItems().ToDictionary(b => b.Id);
                var devs = _db.GetUtilisateurs().ToDictionary(u => u.Id);
                var projets = _db.GetProjets().ToDictionary(p => p.Id);

                foreach (var cra in cras.OrderByDescending(c => c.Date).ThenByDescending(c => c.Id))
                {
                    var backlogItem = backlogItems.ContainsKey(cra.BacklogItemId) ? backlogItems[cra.BacklogItemId] : null;
                    var dev = devs.ContainsKey(cra.DevId) ? devs[cra.DevId] : null;
                    var projet = backlogItem != null && backlogItem.ProjetId.HasValue && projets.ContainsKey(backlogItem.ProjetId.Value) ? projets[backlogItem.ProjetId.Value] : null;

                    CRAs.Add(new CRADisplay
                    {
                        Id = cra.Id,
                        Date = cra.Date.ToString("dd/MM/yyyy"),
                        DevNom = dev?.Nom ?? "Inconnu",
                        ProjetNom = projet?.Nom ?? "Inconnu",
                        TacheNom = backlogItem?.Titre ?? "Inconnu",
                        Heures = cra.HeuresTravaillees,
                        Commentaire = cra.Commentaire ?? string.Empty
                    });
                }

                OnPropertyChanged(nameof(TotalCRA));
                OnPropertyChanged(nameof(TotalHeures));
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erreur lors du chargement des CRA : {ex.Message}",
                    "Erreur",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void DeleteCRA(int craId)
        {
            try
            {
                var result = MessageBox.Show(
                    "Voulez-vous vraiment supprimer ce CRA ?",
                    "Confirmation",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _craService.DeleteCRA(craId, _currentUserId, _isAdmin);
                    LoadCRAs(); // Recharger la liste

                    MessageBox.Show(
                        "CRA supprimé avec succès",
                        "Succès",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erreur lors de la suppression : {ex.Message}",
                    "Erreur",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void FilterToday(object parameter)
        {
            DateDebut = DateTime.Today;
            DateFin = DateTime.Today;
            LoadCRAs();
        }

        private void FilterWeek(object parameter)
        {
            var today = DateTime.Today;
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday);
            DateDebut = startOfWeek;
            DateFin = startOfWeek.AddDays(6);
            LoadCRAs();
        }

        private void FilterMonth(object parameter)
        {
            var today = DateTime.Today;
            DateDebut = new DateTime(today.Year, today.Month, 1);
            DateFin = DateDebut.Value.AddMonths(1).AddDays(-1);
            LoadCRAs();
        }

        private void FilterAll(object parameter)
        {
            DateDebut = null;
            DateFin = null;
            LoadCRAs();
        }

        private void ExportCSV(object parameter)
        {
            try
            {
                // Récupérer les CRA actuellement affichés
                int? devId = DevFiltre?.Id;
                if (!_isAdmin)
                {
                    devId = _currentUserId;
                }

                var cras = _craService.GetAllCRAs(DateDebut, DateFin);
                if (devId.HasValue)
                {
                    cras = cras.Where(c => c.DevId == devId.Value).ToList();
                }

                var csv = _craService.ExportToCSV(cras);

                // Sauvegarder le fichier
                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Fichiers CSV (*.csv)|*.csv",
                    FileName = $"CRA_Export_{DateTime.Now:yyyyMMdd}.csv"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    System.IO.File.WriteAllText(saveDialog.FileName, csv, System.Text.Encoding.UTF8);
                    MessageBox.Show(
                        $"Export réussi : {cras.Count} CRA exportés",
                        "Succès",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erreur lors de l'export : {ex.Message}",
                    "Erreur",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

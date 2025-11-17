using System;
using System.Collections.Generic;
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
    public class JourCRAViewModel
    {
        public DateTime Date { get; set; }
        public int Jour { get; set; }
        public bool EstDansMois { get; set; }
        public bool EstAujourdhui { get; set; }
        public bool EstWeekend { get; set; }
        public bool EstJourFerie { get; set; }
        public string NomJourFerie { get; set; }
        public double TotalHeuresDev { get; set; }
        public string TotalJoursAffiche => TotalHeuresDev > 0 ? $"{TotalHeuresDev / 8.0:F1}j" : "";
    }

    public class DevCRAInfoViewModel
    {
        public Utilisateur Dev { get; set; }
        public string Nom { get; set; }
        public double TotalHeuresPeriode { get; set; }
        public double TotalJoursPeriode => TotalHeuresPeriode / 8.0;
        public int NombreJoursSaisis { get; set; }
        public List<CRA> CRAs { get; set; }
    }

    public class CRADetailViewModel
    {
        public CRA CRA { get; set; }
        public string TacheNom { get; set; }
        public double Jours => CRA?.HeuresTravaillees / 8.0 ?? 0;
        public double Heures => CRA?.HeuresTravaillees ?? 0;
        public string Commentaire => CRA?.Commentaire ?? "";
    }

    public class SuiviCRAViewModel : INotifyPropertyChanged
    {
        private readonly CRAService _craService;
        private readonly BacklogService _backlogService;
        private readonly PermissionService _permissionService;

        private DateTime _moisCourant;
        private string _modeAffichage; // "mois" ou "semaine"
        private Utilisateur _devSelectionne;
        private JourCRAViewModel _jourSelectionne;
        private DateTime _semaineDebut;

        public ObservableCollection<JourCRAViewModel> JoursCalendrier { get; set; }
        public ObservableCollection<Utilisateur> Devs { get; set; }
        public ObservableCollection<DevCRAInfoViewModel> StatsDev { get; set; }
        public ObservableCollection<CRADetailViewModel> CRAsJourSelectionne { get; set; }

        public DateTime MoisCourant
        {
            get => _moisCourant;
            set
            {
                _moisCourant = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(MoisAnneeAffichage));
                OnPropertyChanged(nameof(PeriodeAffichage));
                ChargerCalendrier();
                ChargerStatsDev();
            }
        }

        public string MoisAnneeAffichage => MoisCourant.ToString("MMMM yyyy").ToUpper();

        public string PeriodeAffichage
        {
            get
            {
                if (ModeAffichage == "semaine")
                {
                    var fin = SemaineDebut.AddDays(6);
                    return $"Semaine du {SemaineDebut:dd/MM} au {fin:dd/MM/yyyy}";
                }
                return MoisAnneeAffichage;
            }
        }

        public string ModeAffichage
        {
            get => _modeAffichage;
            set
            {
                _modeAffichage = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(EstModeMois));
                OnPropertyChanged(nameof(EstModeSemaine));
                OnPropertyChanged(nameof(PeriodeAffichage));
                ChargerCalendrier();
                ChargerStatsDev();
            }
        }

        public bool EstModeMois => ModeAffichage == "mois";
        public bool EstModeSemaine => ModeAffichage == "semaine";

        public DateTime SemaineDebut
        {
            get => _semaineDebut;
            set
            {
                _semaineDebut = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PeriodeAffichage));
            }
        }

        public Utilisateur DevSelectionne
        {
            get => _devSelectionne;
            set
            {
                _devSelectionne = value;
                OnPropertyChanged();
                ChargerCalendrier();
                ChargerStatsDev();
            }
        }

        public JourCRAViewModel JourSelectionne
        {
            get => _jourSelectionne;
            set
            {
                _jourSelectionne = value;
                OnPropertyChanged();
                ChargerCRAsJour();
            }
        }

        public ICommand PeriodePrecedenteCommand { get; }
        public ICommand PeriodeSuivanteCommand { get; }
        public ICommand AujourdhuiCommand { get; }
        public ICommand ChangerModeCommand { get; }
        public ICommand JourSelectionnCommand { get; }

        public SuiviCRAViewModel(CRAService craService, BacklogService backlogService, PermissionService permissionService)
        {
            _craService = craService;
            _backlogService = backlogService;
            _permissionService = permissionService;

            JoursCalendrier = new ObservableCollection<JourCRAViewModel>();
            Devs = new ObservableCollection<Utilisateur>();
            StatsDev = new ObservableCollection<DevCRAInfoViewModel>();
            CRAsJourSelectionne = new ObservableCollection<CRADetailViewModel>();

            MoisCourant = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            ModeAffichage = "mois";
            SemaineDebut = GetLundiDeLaSemaine(DateTime.Now);

            PeriodePrecedenteCommand = new RelayCommand(_ => 
            {
                if (ModeAffichage == "mois")
                    MoisCourant = MoisCourant.AddMonths(-1);
                else
                {
                    SemaineDebut = SemaineDebut.AddDays(-7);
                    ChargerCalendrier();
                    ChargerStatsDev();
                }
            });

            PeriodeSuivanteCommand = new RelayCommand(_ => 
            {
                if (ModeAffichage == "mois")
                    MoisCourant = MoisCourant.AddMonths(1);
                else
                {
                    SemaineDebut = SemaineDebut.AddDays(7);
                    ChargerCalendrier();
                    ChargerStatsDev();
                }
            });

            AujourdhuiCommand = new RelayCommand(_ => 
            {
                if (ModeAffichage == "mois")
                    MoisCourant = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                else
                {
                    SemaineDebut = GetLundiDeLaSemaine(DateTime.Now);
                    ChargerCalendrier();
                    ChargerStatsDev();
                }
            });

            ChangerModeCommand = new RelayCommand(param => ModeAffichage = param.ToString());
            JourSelectionnCommand = new RelayCommand(param => JourSelectionne = (JourCRAViewModel)param);

            ChargerDevs();
            ChargerCalendrier();
            ChargerStatsDev();
        }

        private DateTime GetLundiDeLaSemaine(DateTime date)
        {
            int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
            return date.AddDays(-1 * diff).Date;
        }

        private void ChargerDevs()
        {
            Devs.Clear();
            Devs.Add(new Utilisateur { Id = 0, Nom = "Tous les développeurs" });

            var users = _backlogService.GetAllUtilisateurs();
            foreach (var user in users)
            {
                Devs.Add(user);
            }

            DevSelectionne = Devs.FirstOrDefault();
        }

        private void ChargerCalendrier()
        {
            JoursCalendrier.Clear();

            DateTime dateDebut, dateFin;

            if (ModeAffichage == "semaine")
            {
                dateDebut = SemaineDebut;
                dateFin = SemaineDebut.AddDays(6);
            }
            else // mois
            {
                dateDebut = MoisCourant;
                dateFin = MoisCourant.AddMonths(1).AddDays(-1);

                // Ajuster pour commencer un lundi
                int premierJourSemaine = (int)dateDebut.DayOfWeek;
                premierJourSemaine = premierJourSemaine == 0 ? 6 : premierJourSemaine - 1;
                dateDebut = dateDebut.AddDays(-premierJourSemaine);
            }

            // Récupérer les CRAs de la période
            var devId = DevSelectionne != null && DevSelectionne.Id > 0 ? DevSelectionne.Id : (int?)null;
            var cras = devId.HasValue 
                ? _craService.GetCRAsByDev(devId.Value, dateDebut, dateFin.AddDays(ModeAffichage == "mois" ? 41 : 0))
                : _craService.GetCRAsByPeriod(dateDebut, dateFin.AddDays(ModeAffichage == "mois" ? 41 : 0));

            int nombreJours = ModeAffichage == "semaine" ? 7 : 42;

            for (int i = 0; i < nombreJours; i++)
            {
                var date = dateDebut.AddDays(i);
                var totalHeures = cras.Where(c => c.Date.Date == date.Date).Sum(c => c.HeuresTravaillees);

                var jourVM = new JourCRAViewModel
                {
                    Date = date,
                    Jour = date.Day,
                    EstDansMois = date.Month == MoisCourant.Month,
                    EstAujourdhui = date.Date == DateTime.Now.Date,
                    EstWeekend = JoursFeriesService.EstWeekend(date),
                    EstJourFerie = JoursFeriesService.EstJourFerie(date),
                    NomJourFerie = JoursFeriesService.GetNomJourFerie(date),
                    TotalHeuresDev = totalHeures
                };

                JoursCalendrier.Add(jourVM);
            }
        }

        private void ChargerStatsDev()
        {
            StatsDev.Clear();

            DateTime dateDebut, dateFin;

            if (ModeAffichage == "semaine")
            {
                dateDebut = SemaineDebut;
                dateFin = SemaineDebut.AddDays(6);
            }
            else
            {
                dateDebut = MoisCourant;
                dateFin = MoisCourant.AddMonths(1).AddDays(-1);
            }

            var devs = DevSelectionne != null && DevSelectionne.Id > 0 
                ? new List<Utilisateur> { DevSelectionne }
                : _backlogService.GetAllUtilisateurs();

            foreach (var dev in devs)
            {
                var cras = _craService.GetCRAsByDev(dev.Id, dateDebut, dateFin);
                
                if (cras.Count == 0 && DevSelectionne != null && DevSelectionne.Id > 0)
                    continue; // Ne pas afficher si aucun CRA et un dev spécifique sélectionné

                var joursUniques = cras.Select(c => c.Date.Date).Distinct().Count();

                StatsDev.Add(new DevCRAInfoViewModel
                {
                    Dev = dev,
                    Nom = dev.Nom,
                    TotalHeuresPeriode = cras.Sum(c => c.HeuresTravaillees),
                    NombreJoursSaisis = joursUniques,
                    CRAs = cras
                });
            }
        }

        private void ChargerCRAsJour()
        {
            CRAsJourSelectionne.Clear();

            if (JourSelectionne == null) return;

            var devId = DevSelectionne != null && DevSelectionne.Id > 0 ? DevSelectionne.Id : (int?)null;
            var cras = devId.HasValue
                ? _craService.GetCRAsByDev(devId.Value, JourSelectionne.Date, JourSelectionne.Date)
                : _craService.GetCRAsByPeriod(JourSelectionne.Date, JourSelectionne.Date);

            var taches = _backlogService.GetAllBacklogItems();
            var users = _backlogService.GetAllUtilisateurs();

            foreach (var cra in cras.OrderBy(c => c.DevId).ThenBy(c => c.DateCreation))
            {
                var tache = taches.FirstOrDefault(t => t.Id == cra.BacklogItemId);
                var dev = users.FirstOrDefault(u => u.Id == cra.DevId);
                
                CRAsJourSelectionne.Add(new CRADetailViewModel
                {
                    CRA = cra,
                    TacheNom = $"[{dev?.Nom}] {tache?.Titre ?? "Tâche supprimée"}"
                });
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

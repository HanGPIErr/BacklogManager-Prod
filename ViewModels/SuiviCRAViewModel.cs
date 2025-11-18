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
        public string IconeJourFerie { get; set; }
        public bool EstDansPasse { get; set; }
        public bool EstDansFutur { get; set; }
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

    public class MoisCRAViewModel
    {
        public int Mois { get; set; }
        public int Annee { get; set; }
        public string NomMois { get; set; }
        public double TotalHeures { get; set; }
        public double TotalJours => TotalHeures / 8.0;
        public int NombreJoursSaisis { get; set; }
        public string TotalAffiche => TotalHeures > 0 ? $"{TotalJours:F1}j" : "-";
    }

    public class SuiviCRAViewModel : INotifyPropertyChanged
    {
        private readonly CRAService _craService;
        private readonly BacklogService _backlogService;
        private readonly PermissionService _permissionService;

        private DateTime _moisCourant;
        private string _modeAffichage; // "mois" ou "liste"
        private Utilisateur _devSelectionne;
        private JourCRAViewModel _jourSelectionne;
        private DateTime _semaineDebut;
        private int _anneeCourante;

        public ObservableCollection<JourCRAViewModel> JoursCalendrier { get; set; }
        public ObservableCollection<MoisCRAViewModel> MoisAnnee { get; set; }
        public ObservableCollection<Utilisateur> Devs { get; set; }
        public ObservableCollection<DevCRAInfoViewModel> StatsDev { get; set; }
        public ObservableCollection<CRADetailViewModel> CRAsJourSelectionne { get; set; }

        public int AnneeCourante
        {
            get => _anneeCourante;
            set
            {
                _anneeCourante = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PeriodeAffichage));
                if (ModeAffichage == "liste")
                {
                    ChargerMoisAnnee();
                }
            }
        }

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
                if (ModeAffichage == "liste")
                {
                    return $"ANNÉE {AnneeCourante}";
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
                OnPropertyChanged(nameof(EstModeListe));
                OnPropertyChanged(nameof(PeriodeAffichage));
                
                if (value == "liste")
                {
                    ChargerMoisAnnee();
                }
                else
                {
                    ChargerCalendrier();
                    ChargerStatsDev();
                }
            }
        }

        public bool EstModeMois => ModeAffichage == "mois";
        public bool EstModeListe => ModeAffichage == "liste";

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
        public ICommand MoisSelectionnCommand { get; }

        public SuiviCRAViewModel(CRAService craService, BacklogService backlogService, PermissionService permissionService)
        {
            _craService = craService;
            _backlogService = backlogService;
            _permissionService = permissionService;

            JoursCalendrier = new ObservableCollection<JourCRAViewModel>();
            MoisAnnee = new ObservableCollection<MoisCRAViewModel>();
            Devs = new ObservableCollection<Utilisateur>();
            StatsDev = new ObservableCollection<DevCRAInfoViewModel>();
            CRAsJourSelectionne = new ObservableCollection<CRADetailViewModel>();

            MoisCourant = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            AnneeCourante = DateTime.Now.Year;
            ModeAffichage = "mois";
            SemaineDebut = GetLundiDeLaSemaine(DateTime.Now);

            PeriodePrecedenteCommand = new RelayCommand(_ => 
            {
                if (ModeAffichage == "mois")
                    MoisCourant = MoisCourant.AddMonths(-1);
                else if (ModeAffichage == "liste")
                    AnneeCourante--;
            });

            PeriodeSuivanteCommand = new RelayCommand(_ => 
            {
                if (ModeAffichage == "mois")
                    MoisCourant = MoisCourant.AddMonths(1);
                else if (ModeAffichage == "liste")
                    AnneeCourante++;
            });

            AujourdhuiCommand = new RelayCommand(_ => 
            {
                if (ModeAffichage == "mois")
                    MoisCourant = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                else if (ModeAffichage == "liste")
                    AnneeCourante = DateTime.Now.Year;
            });

            ChangerModeCommand = new RelayCommand(param => ModeAffichage = param.ToString());
            JourSelectionnCommand = new RelayCommand(param => JourSelectionne = (JourCRAViewModel)param);
            MoisSelectionnCommand = new RelayCommand(param =>
            {
                var moisVM = param as MoisCRAViewModel;
                if (moisVM != null)
                {
                    MoisCourant = new DateTime(moisVM.Annee, moisVM.Mois, 1);
                    ModeAffichage = "mois"; // Basculer vers la vue mois
                }
            });

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

            if (ModeAffichage == "liste")
            {
                // Mode liste: afficher tout le mois en une seule ligne
                dateDebut = new DateTime(MoisCourant.Year, MoisCourant.Month, 1);
                dateFin = dateDebut.AddMonths(1).AddDays(-1);
            }
            else // mois (calendrier)
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

            int nombreJours = ModeAffichage == "liste" ? (dateFin - dateDebut).Days + 1 : 42;

            for (int i = 0; i < nombreJours; i++)
            {
                var date = dateDebut.AddDays(i);
                var totalHeures = cras.Where(c => c.Date.Date == date.Date).Sum(c => c.HeuresTravaillees);

                var estJourFerie = JoursFeriesService.EstJourFerie(date);
                var nomJourFerie = JoursFeriesService.GetNomJourFerie(date);
                var aujourdhui = DateTime.Now.Date;

                var jourVM = new JourCRAViewModel
                {
                    Date = date,
                    Jour = date.Day,
                    EstDansMois = date.Month == MoisCourant.Month,
                    EstAujourdhui = date.Date == aujourdhui,
                    EstWeekend = JoursFeriesService.EstWeekend(date),
                    EstJourFerie = estJourFerie,
                    NomJourFerie = nomJourFerie,
                    IconeJourFerie = estJourFerie ? "/Images/jour-ferie.png" : null,
                    EstDansPasse = date < aujourdhui,
                    EstDansFutur = date > aujourdhui,
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

            var taches = _backlogService.GetAllBacklogItemsIncludingArchived();
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

        private void ChargerMoisAnnee()
        {
            MoisAnnee.Clear();

            var devId = DevSelectionne != null && DevSelectionne.Id > 0 ? DevSelectionne.Id : (int?)null;
            
            for (int mois = 1; mois <= 12; mois++)
            {
                var debutMois = new DateTime(AnneeCourante, mois, 1);
                var finMois = debutMois.AddMonths(1).AddDays(-1);

                var cras = devId.HasValue
                    ? _craService.GetCRAsByDev(devId.Value, debutMois, finMois)
                    : _craService.GetCRAsByPeriod(debutMois, finMois);

                var totalHeures = cras.Sum(c => c.HeuresTravaillees);
                var joursUniques = cras.Select(c => c.Date.Date).Distinct().Count();

                MoisAnnee.Add(new MoisCRAViewModel
                {
                    Mois = mois,
                    Annee = AnneeCourante,
                    NomMois = new DateTime(AnneeCourante, mois, 1).ToString("MMMM").ToUpper(),
                    TotalHeures = totalHeures,
                    NombreJoursSaisis = joursUniques
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

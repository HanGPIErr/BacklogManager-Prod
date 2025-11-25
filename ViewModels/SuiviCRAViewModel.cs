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
        public double HeuresTachesTravail { get; set; } // Heures de vraies tâches uniquement (sans congés)
        public string TotalJoursAffiche => HeuresTachesTravail > 0 ? $"{HeuresTachesTravail / 8.0:F1}j" : "";
        public bool EstConges { get; set; }
        public bool EstNonTravaille { get; set; }
    }

    public class DevCRAInfoViewModel
    {
        public Utilisateur Dev { get; set; }
        public string Nom { get; set; }
        public double TotalHeuresPeriode { get; set; }
        public double TotalJoursPeriode => TotalHeuresPeriode / 8.0;
        public int NombreJoursSaisis { get; set; }
        public List<CRA> CRAs { get; set; }
        public double HeuresConges { get; set; }
        public double JoursConges => HeuresConges / 8.0;
        public double HeuresNonTravaille { get; set; }
        public double JoursNonTravaille => HeuresNonTravaille / 8.0;
        public double HeuresTravail { get; set; }
        public double JoursTravail => HeuresTravail / 8.0;
    }

    public class CRADetailViewModel
    {
        public CRA CRA { get; set; }
        public string TacheNom { get; set; }
        public double Jours => CRA?.HeuresTravaillees / 8.0 ?? 0;
        public double Heures => CRA?.HeuresTravaillees ?? 0;
        public string Commentaire => CRA?.Commentaire ?? "";
        public bool EstConges { get; set; }
        public bool EstNonTravaille { get; set; }
        public bool EstTacheNormale => !EstConges && !EstNonTravaille;
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

    public class TacheTimelineViewModel
    {
        public string Titre { get; set; }
        public Statut Statut { get; set; }
        public string DevAssigneNom { get; set; }
        public int? Complexite { get; set; }
        public DateTime DateCreation { get; set; }
        public DateTime? DateFinAttendue { get; set; }
        public double? ChiffrageJours { get; set; }
        public double TempsReelHeures { get; set; }
        public double TempsReelJours { get; set; }
        public double ProgressionPourcentage { get; set; }
        public double ProgressionLargeur { get; set; }
        public bool EstEnRetard { get; set; }
        
        // Pour la timeline visuelle
        public double PositionX { get; set; }
        public double PositionY { get; set; }
        public double LargeurBarre { get; set; }
    }

    public class SuiviCRAViewModel : INotifyPropertyChanged
    {
        private readonly CRAService _craService;
        private readonly BacklogService _backlogService;
        private readonly PermissionService _permissionService;

        private DateTime _moisCourant;
        private string _modeAffichage; // "mois", "liste" ou "timeline"
        private Utilisateur _devSelectionne;
        private JourCRAViewModel _jourSelectionne;
        private DateTime _semaineDebut;
        private int _anneeCourante;
        private Projet _projetSelectionne;

        public ObservableCollection<JourCRAViewModel> JoursCalendrier { get; set; }
        public ObservableCollection<MoisCRAViewModel> MoisAnnee { get; set; }
        public ObservableCollection<Utilisateur> Devs { get; set; }
        public ObservableCollection<DevCRAInfoViewModel> StatsDev { get; set; }
        public ObservableCollection<CRADetailViewModel> CRAsJourSelectionne { get; set; }
        public ObservableCollection<Projet> Projets { get; set; }
        public ObservableCollection<TacheTimelineViewModel> TachesProjetTimeline { get; set; }
        public ObservableCollection<MoisCRAViewModel> MoisTimeline { get; set; }
        
        public double HauteurTimeline => TachesProjetTimeline.Count * 90 + 20; // 90px par tâche + marge

        private double _positionDebutProjet;
        public double PositionDebutProjet
        {
            get => _positionDebutProjet;
            set { _positionDebutProjet = value; OnPropertyChanged(); }
        }

        private double _positionFinProjet;
        public double PositionFinProjet
        {
            get => _positionFinProjet;
            set { _positionFinProjet = value; OnPropertyChanged(); }
        }

        private bool _afficherMarqueursProjet;
        public bool AfficherMarqueursProjet
        {
            get => _afficherMarqueursProjet;
            set { _afficherMarqueursProjet = value; OnPropertyChanged(); }
        }

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
                OnPropertyChanged(nameof(EstModeTimeline));
                OnPropertyChanged(nameof(PeriodeAffichage));
                
                if (value == "liste")
                {
                    ChargerMoisAnnee();
                }
                else if (value == "timeline")
                {
                    ChargerTachesTimeline();
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
        public bool EstModeTimeline => ModeAffichage == "timeline";

        public Projet ProjetSelectionne
        {
            get => _projetSelectionne;
            set
            {
                _projetSelectionne = value;
                OnPropertyChanged();
                if (EstModeTimeline)
                {
                    ChargerTachesTimeline();
                }
            }
        }

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
            Projets = new ObservableCollection<Projet>();
            TachesProjetTimeline = new ObservableCollection<TacheTimelineViewModel>();
            MoisTimeline = new ObservableCollection<MoisCRAViewModel>();

            MoisCourant = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            AnneeCourante = DateTime.Now.Year;
            ModeAffichage = "mois";
            SemaineDebut = GetLundiDeLaSemaine(DateTime.Now);

            ChargerProjets();

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
                var crasJour = cras.Where(c => c.Date.Date == date.Date).ToList();
                var totalHeures = crasJour.Sum(c => c.HeuresTravaillees);

                var estJourFerie = JoursFeriesService.EstJourFerie(date);
                var nomJourFerie = JoursFeriesService.GetNomJourFerie(date);
                var aujourdhui = DateTime.Now.Date;

                // Déterminer si c'est un jour de congés/non-travaillé et calculer heures de travail réel
                var estConges = false;
                var estNonTravaille = false;
                var heuresTravail = 0.0;
                if (crasJour.Any())
                {
                    var tachesIds = crasJour.Select(c => c.BacklogItemId).Distinct();
                    var taches = tachesIds.Select(id => _backlogService.GetBacklogItemById(id)).Where(t => t != null).ToList();
                    estConges = taches.Any(t => t.TypeDemande == TypeDemande.Conges);
                    estNonTravaille = taches.Any(t => t.TypeDemande == TypeDemande.NonTravaille);
                    
                    // Calculer heures de vraies tâches (exclure congés et non-travaillé)
                    foreach (var cra in crasJour)
                    {
                        var tache = _backlogService.GetBacklogItemById(cra.BacklogItemId);
                        if (tache != null && tache.TypeDemande != TypeDemande.Conges && tache.TypeDemande != TypeDemande.NonTravaille)
                        {
                            heuresTravail += cra.HeuresTravaillees;
                        }
                    }
                }

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
                    TotalHeuresDev = totalHeures,
                    HeuresTachesTravail = heuresTravail,
                    EstConges = estConges,
                    EstNonTravaille = estNonTravaille
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

                // Calculer les heures par type
                var heuresConges = 0.0;
                var heuresNonTravaille = 0.0;
                var heuresTravail = 0.0;
                
                foreach (var cra in cras)
                {
                    var tache = _backlogService.GetBacklogItemById(cra.BacklogItemId);
                    if (tache != null)
                    {
                        if (tache.TypeDemande == TypeDemande.Conges)
                            heuresConges += cra.HeuresTravaillees;
                        else if (tache.TypeDemande == TypeDemande.NonTravaille)
                            heuresNonTravaille += cra.HeuresTravaillees;
                        else
                            heuresTravail += cra.HeuresTravaillees;
                    }
                }

                StatsDev.Add(new DevCRAInfoViewModel
                {
                    Dev = dev,
                    Nom = dev.Nom,
                    TotalHeuresPeriode = cras.Sum(c => c.HeuresTravaillees),
                    NombreJoursSaisis = joursUniques,
                    CRAs = cras,
                    HeuresConges = heuresConges,
                    HeuresNonTravaille = heuresNonTravaille,
                    HeuresTravail = heuresTravail
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
                    TacheNom = $"[{dev?.Nom}] {tache?.Titre ?? "Tâche supprimée"}",
                    EstConges = tache?.TypeDemande == TypeDemande.Conges,
                    EstNonTravaille = tache?.TypeDemande == TypeDemande.NonTravaille
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

        private void ChargerProjets()
        {
            Projets.Clear();
            var projets = _backlogService.GetAllProjets().Where(p => p.Actif).ToList();
            foreach (var projet in projets)
            {
                Projets.Add(projet);
            }
        }

        private void ChargerTachesTimeline()
        {
            TachesProjetTimeline.Clear();
            MoisTimeline.Clear();
            
            if (ProjetSelectionne == null)
                return;

            // Utiliser les dates du projet comme plage principale
            DateTime dateMin, dateMax;
            
            if (ProjetSelectionne.DateDebut.HasValue)
            {
                dateMin = ProjetSelectionne.DateDebut.Value;
            }
            else
            {
                // Fallback: utiliser la DateDebut des tâches, sinon DateCreation
                var tachesTemp = _backlogService.GetAllBacklogItemsIncludingArchived()
                    .Where(t => t.ProjetId == ProjetSelectionne.Id)
                    .ToList();
                dateMin = tachesTemp.Any() 
                    ? tachesTemp.Min(t => t.DateDebut ?? t.DateCreation) 
                    : DateTime.Now;
            }

            if (ProjetSelectionne.DateFinPrevue.HasValue)
            {
                dateMax = ProjetSelectionne.DateFinPrevue.Value;
            }
            else
            {
                // Fallback: utiliser la date max des tâches + 3 mois
                var tachesTemp = _backlogService.GetAllBacklogItemsIncludingArchived()
                    .Where(t => t.ProjetId == ProjetSelectionne.Id)
                    .ToList();
                dateMax = tachesTemp.Any() 
                    ? tachesTemp.Max(t => t.DateFinAttendue ?? t.DateDebut ?? t.DateCreation).AddMonths(3)
                    : DateTime.Now.AddMonths(6);
            }

            // Normaliser au début du mois pour dateMin seulement
            dateMin = new DateTime(dateMin.Year, dateMin.Month, 1);
            // Pour dateMax, garder le mois complet (aller jusqu'à la fin du mois)
            dateMax = new DateTime(dateMax.Year, dateMax.Month, DateTime.DaysInMonth(dateMax.Year, dateMax.Month));
            
            // Pas de marge avant, petite marge après
            dateMax = dateMax.AddMonths(1);

            // Générer les mois pour toute la durée du projet
            var dateCourante = dateMin;
            while (dateCourante <= dateMax)
            {
                MoisTimeline.Add(new MoisCRAViewModel
                {
                    Mois = dateCourante.Month,
                    Annee = dateCourante.Year,
                    NomMois = dateCourante.ToString("MMM").ToUpper()
                });
                dateCourante = dateCourante.AddMonths(1);
            }

            // Charger les tâches
            var taches = _backlogService.GetAllBacklogItemsIncludingArchived()
                .Where(t => t.ProjetId == ProjetSelectionne.Id)
                .OrderBy(t => t.DateCreation)
                .ToList();

            if (!taches.Any())
            {
                OnPropertyChanged(nameof(HauteurTimeline));
                return;
            }

            // Largeur par mois en pixels
            const double largeurMois = 120;
            var devs = _backlogService.GetAllUtilisateurs().ToDictionary(d => d.Id, d => d.Nom);

            // Calculer les positions
            int indexLigne = 0;
            foreach (var tache in taches)
            {
                var tempsReel = _craService.GetTempsReelTache(tache.Id);
                var progression = tache.ChiffrageHeures.HasValue && tache.ChiffrageHeures > 0
                    ? (tempsReel / tache.ChiffrageHeures.Value) * 100
                    : 0;

                // Utiliser DateDebut si disponible, sinon DateCreation
                var dateDebut = tache.DateDebut ?? tache.DateCreation;
                var dateFin = tache.DateFinAttendue ?? dateDebut.AddDays(tache.ChiffrageJours.HasValue ? tache.ChiffrageJours.Value * 7 : 7);

                // Calculer position X (début de la tâche)
                var moisDepuisDebut = (dateDebut.Year - dateMin.Year) * 12 + (dateDebut.Month - dateMin.Month);
                var jourDansMois = dateDebut.Day - 1;
                var joursDansMois = DateTime.DaysInMonth(dateDebut.Year, dateDebut.Month);
                var positionX = moisDepuisDebut * largeurMois + (jourDansMois / (double)joursDansMois * largeurMois);

                // Calculer largeur (durée)
                var dureeJours = (dateFin - dateDebut).TotalDays;
                var largeurBarre = Math.Max(largeurMois / 4, (dureeJours / 30.0) * largeurMois); // Min 1/4 mois

                var tacheVM = new TacheTimelineViewModel
                {
                    Titre = tache.Titre,
                    Statut = tache.Statut,
                    DevAssigneNom = tache.DevAssigneId.HasValue && devs.ContainsKey(tache.DevAssigneId.Value)
                        ? devs[tache.DevAssigneId.Value]
                        : null,
                    Complexite = tache.Complexite,
                    DateCreation = tache.DateCreation,
                    DateFinAttendue = tache.DateFinAttendue,
                    ChiffrageJours = tache.ChiffrageJours,
                    TempsReelHeures = tempsReel,
                    TempsReelJours = tempsReel / 8.0,
                    ProgressionPourcentage = Math.Min(progression, 100),
                    ProgressionLargeur = Math.Min(progression * 3, 300),
                    EstEnRetard = tache.ChiffrageHeures.HasValue && tempsReel > tache.ChiffrageHeures.Value,
                    PositionX = positionX,
                    PositionY = indexLigne * 90 + 10,
                    LargeurBarre = largeurBarre
                };

                TachesProjetTimeline.Add(tacheVM);
                indexLigne++;
            }

            // Calculer les positions des marqueurs de début et fin du projet
            if (ProjetSelectionne.DateDebut.HasValue)
            {
                var dateDebutProjet = ProjetSelectionne.DateDebut.Value;
                var moisDepuisDebut = (dateDebutProjet.Year - dateMin.Year) * 12 + (dateDebutProjet.Month - dateMin.Month);
                var jourDansMois = dateDebutProjet.Day - 1;
                var joursDansMois = DateTime.DaysInMonth(dateDebutProjet.Year, dateDebutProjet.Month);
                PositionDebutProjet = moisDepuisDebut * largeurMois + (jourDansMois / (double)joursDansMois * largeurMois);
            }

            if (ProjetSelectionne.DateFinPrevue.HasValue)
            {
                var dateFinProjet = ProjetSelectionne.DateFinPrevue.Value;
                var moisDepuisDebut = (dateFinProjet.Year - dateMin.Year) * 12 + (dateFinProjet.Month - dateMin.Month);
                var jourDansMois = dateFinProjet.Day - 1;
                var joursDansMois = DateTime.DaysInMonth(dateFinProjet.Year, dateFinProjet.Month);
                PositionFinProjet = moisDepuisDebut * largeurMois + (jourDansMois / (double)joursDansMois * largeurMois);
            }

            AfficherMarqueursProjet = ProjetSelectionne.DateDebut.HasValue || ProjetSelectionne.DateFinPrevue.HasValue;
            
            OnPropertyChanged(nameof(HauteurTimeline));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

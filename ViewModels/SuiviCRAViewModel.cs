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
        public BacklogItem BacklogItem { get; set; } // Référence à l'item d'origine
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

        private bool _extensionProjetDetectee;
        public bool ExtensionProjetDetectee
        {
            get => _extensionProjetDetectee;
            set { _extensionProjetDetectee = value; OnPropertyChanged(); }
        }

        private string _messageExtension;
        public string MessageExtension
        {
            get => _messageExtension;
            set { _messageExtension = value; OnPropertyChanged(); }
        }

        private DateTime? _dateDebutExtension;
        public DateTime? DateDebutExtension
        {
            get => _dateDebutExtension;
            set { _dateDebutExtension = value; OnPropertyChanged(); }
        }

        private DateTime? _dateFinExtension;
        public DateTime? DateFinExtension
        {
            get => _dateFinExtension;
            set { _dateFinExtension = value; OnPropertyChanged(); }
        }

        private List<BacklogItem> _tachesExtension; // Tâches à migrer vers le nouveau projet

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
        public ICommand ValiderExtensionProjetCommand { get; }

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
            
            ValiderExtensionProjetCommand = new RelayCommand(_ => ValiderExtensionProjet());

            ChargerDevs();
            ChargerCalendrier();
            ChargerStatsDev();
        }

        private void ValiderExtensionProjet()
        {
            if (ProjetSelectionne == null || !DateDebutExtension.HasValue || !DateFinExtension.HasValue || _tachesExtension == null || !_tachesExtension.Any())
            {
                System.Windows.MessageBox.Show(
                    "Impossible de valider l'extension : données manquantes.",
                    "Erreur",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

            try
            {
                // 1. DÉTECTION DE VERSION
                string nomProjetActuel = ProjetSelectionne.Nom ?? "Projet";
                if (string.IsNullOrWhiteSpace(nomProjetActuel))
                    nomProjetActuel = "Projet";

                string nomNouveauProjet;
                int versionActuelle = 1;

                // Extraire version si elle existe (format : "Nom V2", "Nom V3", etc.)
                var regexVersion = new System.Text.RegularExpressions.Regex(@"\s+V(\d+)$");
                var match = regexVersion.Match(nomProjetActuel);
                
                if (match.Success)
                {
                    versionActuelle = int.Parse(match.Groups[1].Value);
                    nomNouveauProjet = regexVersion.Replace(nomProjetActuel, $" V{versionActuelle + 1}");
                }
                else
                {
                    nomNouveauProjet = $"{nomProjetActuel} V2";
                }

                // 2. PRÉPARER LE NOUVEAU PROJET (sans le sauvegarder encore)
                var nouveauProjet = new Projet
                {
                    Nom = nomNouveauProjet,
                    Description = string.IsNullOrEmpty(ProjetSelectionne.Description) 
                        ? $"Extension de {nomProjetActuel}"
                        : $"Extension de {nomProjetActuel} - {ProjetSelectionne.Description}",
                    DateDebut = DateDebutExtension.Value,
                    DateFinPrevue = DateFinExtension.Value,
                    DateCreation = DateTime.Now,
                    Actif = true,
                    CouleurHex = string.IsNullOrEmpty(ProjetSelectionne.CouleurHex) ? "#00915A" : ProjetSelectionne.CouleurHex
                };

                // 3. OUVRIR LA FENÊTRE D'ÉDITION POUR PERMETTRE À L'UTILISATEUR D'AJUSTER
                var editWindow = new Views.EditProjetWindow(_backlogService, nouveauProjet);
                if (editWindow.ShowDialog() == true)
                {
                    // L'utilisateur a validé, le projet est déjà sauvegardé par EditProjetWindow
                    // Récupérer le projet créé
                    var projetCree = _backlogService.GetAllProjets().FirstOrDefault(p => p.Nom == nomNouveauProjet);
                    if (projetCree == null)
                    {
                        System.Windows.MessageBox.Show(
                            "Erreur : le projet n'a pas pu être créé.",
                            "Erreur",
                            System.Windows.MessageBoxButton.OK,
                            System.Windows.MessageBoxImage.Error);
                        return;
                    }

                    // 4. MIGRER LES TÂCHES VERS LE NOUVEAU PROJET
                    var utilisateurs = _backlogService.Database.GetUtilisateurs();
                    var windowsUsername = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
                    if (windowsUsername.Contains(@"\"))
                        windowsUsername = windowsUsername.Split('\\')[1];
                    var utilisateurCourant = utilisateurs.FirstOrDefault(u => 
                        u.UsernameWindows.Equals(windowsUsername, StringComparison.OrdinalIgnoreCase)) ?? utilisateurs.FirstOrDefault();
                    int userId = utilisateurCourant?.Id ?? 1;

                    int nbTachesMigrees = 0;
                    foreach (var tache in _tachesExtension)
                    {
                        var ancienProjetId = tache.ProjetId;
                        tache.ProjetId = projetCree.Id;
                        _backlogService.SaveBacklogItem(tache);

                        // Historique pour la migration
                        _backlogService.Database.AddHistorique(new HistoriqueModification
                        {
                            TypeEntite = "BacklogItem",
                            EntiteId = tache.Id,
                            UtilisateurId = userId,
                            DateModification = DateTime.Now,
                            TypeModification = TypeModification.Modification,
                            ChampModifie = "ProjetId",
                            AncienneValeur = $"{ProjetSelectionne.Nom} (ID: {ancienProjetId})",
                            NouvelleValeur = $"{nomNouveauProjet} (ID: {projetCree.Id})"
                        });

                        nbTachesMigrees++;
                    }

                    // 5. HISTORIQUE GLOBAL POUR L'EXTENSION
                    _backlogService.Database.AddHistorique(new HistoriqueModification
                    {
                        TypeEntite = "Projet",
                        EntiteId = projetCree.Id,
                        UtilisateurId = userId,
                        DateModification = DateTime.Now,
                        TypeModification = TypeModification.Creation,
                        ChampModifie = "Extension",
                        AncienneValeur = ProjetSelectionne.Nom,
                        NouvelleValeur = $"{nomNouveauProjet} - {nbTachesMigrees} tâches migrées"
                    });

                    // 6. RAFRAÎCHIR LA VUE
                    ChargerProjets();
                    ProjetSelectionne = projetCree; // Sélectionner le nouveau projet
                    ChargerTachesTimeline();

                    System.Windows.MessageBox.Show(
                        $"✅ Extension validée !\n\n" +
                        $"Nouveau projet créé : {projetCree.Nom}\n" +
                        $"Période : {projetCree.DateDebut?.ToString("dd/MM/yyyy") ?? "N/A"} - {projetCree.DateFinPrevue?.ToString("dd/MM/yyyy") ?? "N/A"}\n" +
                        $"Tâches migrées : {nbTachesMigrees}",
                        "Succès",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information);
                }
                // Sinon l'utilisateur a annulé, on ne fait rien
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Erreur lors de l'extension du projet :\n{ex.Message}",
                    "Erreur",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
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
            ExtensionProjetDetectee = false;
            MessageExtension = string.Empty;
            DateDebutExtension = null;
            DateFinExtension = null;
            
            if (ProjetSelectionne == null)
            {
                OnPropertyChanged(nameof(HauteurTimeline));
                return;
            }

            // Récupérer toutes les tâches du projet
            var tachesTemp = _backlogService.GetAllBacklogItemsIncludingArchived()
                .Where(t => t.ProjetId == ProjetSelectionne.Id)
                .ToList();

            if (!tachesTemp.Any())
            {
                OnPropertyChanged(nameof(HauteurTimeline));
                return;
            }

            // Définir les dates de base (période initiale du projet)
            DateTime dateMin, dateMax;

            if (ProjetSelectionne.DateDebut.HasValue)
            {
                dateMin = ProjetSelectionne.DateDebut.Value;
            }
            else
            {
                dateMin = tachesTemp.Min(t => t.DateDebut ?? t.DateCreation);
            }

            if (ProjetSelectionne.DateFinPrevue.HasValue)
            {
                dateMax = ProjetSelectionne.DateFinPrevue.Value;
            }
            else
            {
                dateMax = tachesTemp.Max(t => t.DateFinAttendue ?? t.DateDebut ?? t.DateCreation).AddMonths(3);
            }

            // Normaliser au début du mois
            dateMin = new DateTime(dateMin.Year, dateMin.Month, 1);
            dateMax = new DateTime(dateMax.Year, dateMax.Month, DateTime.DaysInMonth(dateMax.Year, dateMax.Month));

            // DÉTECTION D'EXTENSION : Vérifier s'il y a des tâches après la date de fin du projet avec un gap significatif
            const int JOURS_GAP_MINIMUM = 30; // 1 mois minimum pour considérer une extension
            var tachesApresFinProjet = tachesTemp
                .Where(t => (t.DateDebut ?? t.DateCreation) > dateMax.AddDays(JOURS_GAP_MINIMUM))
                .OrderBy(t => t.DateDebut ?? t.DateCreation)
                .ToList();

            bool hasExtension = tachesApresFinProjet.Any();
            DateTime? dateDebutExtensionCalc = null;
            DateTime? dateMaxExtensionCalc = null;

            if (hasExtension)
            {
                // Calculer la période d'extension
                dateDebutExtensionCalc = tachesApresFinProjet.Min(t => t.DateDebut ?? t.DateCreation);
                dateMaxExtensionCalc = tachesApresFinProjet.Max(t => t.DateFinAttendue ?? t.DateDebut ?? t.DateCreation);

                // Normaliser
                dateDebutExtensionCalc = new DateTime(dateDebutExtensionCalc.Value.Year, dateDebutExtensionCalc.Value.Month, 1);
                dateMaxExtensionCalc = new DateTime(dateMaxExtensionCalc.Value.Year, dateMaxExtensionCalc.Value.Month, 
                    DateTime.DaysInMonth(dateMaxExtensionCalc.Value.Year, dateMaxExtensionCalc.Value.Month));

                // Calculer le gap en mois
                int gapMois = ((dateDebutExtensionCalc.Value.Year - dateMax.Year) * 12) + 
                              (dateDebutExtensionCalc.Value.Month - dateMax.Month);

                // Stocker les tâches de l'extension pour la migration
                _tachesExtension = tachesApresFinProjet;

                ExtensionProjetDetectee = true;
                MessageExtension = $"⚠️ {tachesApresFinProjet.Count} tâche(s) détectée(s) {gapMois} mois après la fin du projet. " +
                                   $"Extension suggérée : {dateDebutExtensionCalc.Value:dd/MM/yyyy} - {dateMaxExtensionCalc.Value:dd/MM/yyyy}";
                DateDebutExtension = dateDebutExtensionCalc;
                DateFinExtension = dateMaxExtensionCalc;
            }

            // Générer les mois UNIQUEMENT pour les périodes actives (sans le gap)
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

            // Si extension, ajouter les mois de l'extension (séparés visuellement)
            if (hasExtension && dateDebutExtensionCalc.HasValue && dateMaxExtensionCalc.HasValue)
            {
                dateCourante = dateDebutExtensionCalc.Value;
                while (dateCourante <= dateMaxExtensionCalc.Value)
                {
                    MoisTimeline.Add(new MoisCRAViewModel
                    {
                        Mois = dateCourante.Month,
                        Annee = dateCourante.Year,
                        NomMois = dateCourante.ToString("MMM").ToUpper()
                    });
                    dateCourante = dateCourante.AddMonths(1);
                }
            }

            // Charger les tâches avec calcul de position ajusté
            var taches = tachesTemp.OrderBy(t => t.DateCreation).ToList();

            const double largeurMois = 120;
            var devs = _backlogService.GetAllUtilisateurs().ToDictionary(d => d.Id, d => d.Nom);

            // Calculer combien de mois dans la période initiale
            int nbMoisPeriode1 = ((dateMax.Year - dateMin.Year) * 12) + (dateMax.Month - dateMin.Month) + 1;

            int indexLigne = 0;
            foreach (var tache in taches)
            {
                var tempsReel = _craService.GetTempsReelTache(tache.Id);
                var progression = tache.ChiffrageHeures.HasValue && tache.ChiffrageHeures > 0
                    ? (tempsReel / tache.ChiffrageHeures.Value) * 100
                    : 0;

                var dateDebut = tache.DateDebut ?? tache.DateCreation;
                var dateFin = tache.DateFinAttendue ?? dateDebut.AddDays(tache.ChiffrageJours.HasValue ? tache.ChiffrageJours.Value * 7 : 7);

                // Calculer position X en fonction de si la tâche est dans la période 1 ou l'extension
                double positionX;
                bool estDansExtension = hasExtension && dateDebut > dateMax.AddDays(JOURS_GAP_MINIMUM);

                if (estDansExtension && dateDebutExtensionCalc.HasValue)
                {
                    // Tâche dans l'extension : calculer depuis le début de l'extension
                    // Offset = largeur de la période 1 (on colle directement après)
                    var offsetExtension = nbMoisPeriode1 * largeurMois;
                    var moisDepuisDebutExt = (dateDebut.Year - dateDebutExtensionCalc.Value.Year) * 12 + 
                                             (dateDebut.Month - dateDebutExtensionCalc.Value.Month);
                    var jourDansMois = dateDebut.Day - 1;
                    var joursDansMois = DateTime.DaysInMonth(dateDebut.Year, dateDebut.Month);
                    positionX = offsetExtension + moisDepuisDebutExt * largeurMois + (jourDansMois / (double)joursDansMois * largeurMois);
                }
                else
                {
                    // Tâche dans la période initiale
                    var moisDepuisDebut = (dateDebut.Year - dateMin.Year) * 12 + (dateDebut.Month - dateMin.Month);
                    var jourDansMois = dateDebut.Day - 1;
                    var joursDansMois = DateTime.DaysInMonth(dateDebut.Year, dateDebut.Month);
                    positionX = moisDepuisDebut * largeurMois + (jourDansMois / (double)joursDansMois * largeurMois);
                }

                var dureeJours = (dateFin - dateDebut).TotalDays;
                var largeurBarre = Math.Max(largeurMois / 4, (dureeJours / 30.0) * largeurMois);

                var tacheVM = new TacheTimelineViewModel
                {
                    BacklogItem = tache, // Référence à l'item d'origine
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

        /// <summary>
        /// Valide tous les CRA non validés de tous les développeurs
        /// </summary>
        public int ValiderTousLesCRA()
        {
            int nombreValidations = 0;

            // Récupérer tous les CRA non validés
            var tousLesCRA = _craService.GetAllCRAs();
            var craAValider = tousLesCRA.Where(c => !c.EstValide).ToList();

            foreach (var cra in craAValider)
            {
                _craService.ValiderCRA(cra.Id);
                nombreValidations++;
            }

            // Rafraîchir l'affichage
            if (ModeAffichage == "mois")
            {
                ChargerCalendrier();
            }
            else if (ModeAffichage == "liste")
            {
                ChargerMoisAnnee();
            }

            return nombreValidations;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

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

    public class ProjetTimelineViewModel
    {
        public Projet Projet { get; set; }
        public string Nom { get; set; }
        public string Description { get; set; }
        public int NbTaches { get; set; }
        public int NbTachesTerminees { get; set; }
        public double ProgressionPourcentage { get; set; }
        public DateTime? DateDebut { get; set; }
        public DateTime? DateFin { get; set; }
        public bool EstEnCours { get; set; }
        
        // Pour la timeline visuelle
        public double PositionX { get; set; }
        public double PositionY { get; set; }
        public double LargeurBarre { get; set; }
        public double LargeurProgression => LargeurBarre * (ProgressionPourcentage / 100.0);
        public string CouleurBarre { get; set; }
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
        private readonly ProgrammeService _programmeService;
        private readonly PermissionService _permissionService;

        private DateTime _moisCourant;
        private string _modeAffichage; // "mois", "liste" ou "timeline"
        private Utilisateur _devSelectionne;
        private JourCRAViewModel _jourSelectionne;
        private DateTime _semaineDebut;
        private int _anneeCourante;
        private Projet _projetSelectionne;
        private Equipe _equipeSelectionnee;
        private Programme _programmeSelectionne;

        public ObservableCollection<JourCRAViewModel> JoursCalendrier { get; set; }
        public ObservableCollection<MoisCRAViewModel> MoisAnnee { get; set; }
        public ObservableCollection<Utilisateur> Devs { get; set; }
        public ObservableCollection<Equipe> Equipes { get; set; }
        public ObservableCollection<DevCRAInfoViewModel> StatsDev { get; set; }
        public ObservableCollection<CRADetailViewModel> CRAsJourSelectionne { get; set; }
        public ObservableCollection<Projet> Projets { get; set; }
        public ObservableCollection<TacheTimelineViewModel> TachesProjetTimeline { get; set; }
        public ObservableCollection<MoisCRAViewModel> MoisTimeline { get; set; }
        public ObservableCollection<Programme> Programmes { get; set; }
        public ObservableCollection<ProjetTimelineViewModel> ProjetsTimelineProgramme { get; set; }
        public ObservableCollection<Equipe> EquipesProgramme { get; set; }
        
        public bool AfficherEquipesProgramme => EquipesProgramme != null && EquipesProgramme.Count > 0;
        public int NbEquipesProgramme => EquipesProgramme?.Count ?? 0;
        
        public double HauteurTimeline => TachesProjetTimeline.Count * 90 + 20; // 90px par tâche + marge

        // Statistiques du programme
        private int _nbTotalTachesProgramme;
        public int NbTotalTachesProgramme
        {
            get => _nbTotalTachesProgramme;
            set { _nbTotalTachesProgramme = value; OnPropertyChanged(); }
        }

        private int _nbTachesTermineesProgramme;
        public int NbTachesTermineesProgramme
        {
            get => _nbTachesTermineesProgramme;
            set { _nbTachesTermineesProgramme = value; OnPropertyChanged(); }
        }

        private double _progressionGlobaleProgramme;
        public double ProgressionGlobaleProgramme
        {
            get => _progressionGlobaleProgramme;
            set { _progressionGlobaleProgramme = value; OnPropertyChanged(); OnPropertyChanged(nameof(ProgressionGlobaleProgrammeTexte)); OnPropertyChanged(nameof(LargeurProgressionGlobale)); }
        }

        public string ProgressionGlobaleProgrammeTexte => $"{ProgressionGlobaleProgramme:F0}%";
        
        public double LargeurProgressionGlobale => ProgressionGlobaleProgramme * 10; // 100% = 1000px

        private double _budgetTempsEstime;
        public double BudgetTempsEstime
        {
            get => _budgetTempsEstime;
            set { _budgetTempsEstime = value; OnPropertyChanged(); OnPropertyChanged(nameof(BudgetTempsEstimeJours)); }
        }

        public string BudgetTempsEstimeJours => $"{BudgetTempsEstime:F1}j";

        private double _tempsReel;
        public double TempsReel
        {
            get => _tempsReel;
            set { _tempsReel = value; OnPropertyChanged(); OnPropertyChanged(nameof(TempsReelJours)); }
        }

        public string TempsReelJours => $"{TempsReel:F1}j";

        // Indicateurs RAG
        private int _nbProjetsGreen;
        public int NbProjetsGreen
        {
            get => _nbProjetsGreen;
            set { _nbProjetsGreen = value; OnPropertyChanged(); OnPropertyChanged(nameof(AfficherGreen)); }
        }

        private int _nbProjetsAmber;
        public int NbProjetsAmber
        {
            get => _nbProjetsAmber;
            set { _nbProjetsAmber = value; OnPropertyChanged(); OnPropertyChanged(nameof(AfficherAmber)); }
        }

        private int _nbProjetsRed;
        public int NbProjetsRed
        {
            get => _nbProjetsRed;
            set { _nbProjetsRed = value; OnPropertyChanged(); OnPropertyChanged(nameof(AfficherRed)); }
        }

        public bool AfficherGreen => NbProjetsGreen > 0;
        public bool AfficherAmber => NbProjetsAmber > 0;
        public bool AfficherRed => NbProjetsRed > 0;

        // Dates du programme
        public string DateDebutProgramme => ProgrammeSelectionne?.DateDebut?.ToString("dd/MM/yyyy") ?? "Non définie";
        public string DateFinCibleProgramme => ProgrammeSelectionne?.DateFinCible?.ToString("dd/MM/yyyy") ?? "Non définie";
        
        // Statut global du programme
        public string StatutGlobalProgramme => ProgrammeSelectionne?.StatutGlobal ?? "";
        public bool AfficherStatutGlobal => !string.IsNullOrEmpty(StatutGlobalProgramme);
        
        public string CouleurStatutGlobal
        {
            get
            {
                if (StatutGlobalProgramme == "On Track") return "#4CAF50";
                if (StatutGlobalProgramme == "At Risk") return "#FF9800";
                if (StatutGlobalProgramme == "Off Track") return "#F44336";
                return "#999999";
            }
        }
        
        public string IconeStatutGlobal
        {
            get
            {
                if (StatutGlobalProgramme == "On Track") return "✓";
                if (StatutGlobalProgramme == "At Risk") return "⚠";
                if (StatutGlobalProgramme == "Off Track") return "✗";
                return "";
            }
        }

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
                OnPropertyChanged(nameof(EstModeTimelineProgramme));
                OnPropertyChanged(nameof(PeriodeAffichage));
                
                if (value == "liste")
                {
                    ChargerMoisAnnee();
                }
                else if (value == "timeline")
                {
                    ChargerTachesTimeline();
                }
                else if (value == "timeline_programme")
                {
                    ChargerTimelineProgrammes();
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
        public bool EstModeTimelineProgramme => ModeAffichage == "timeline_programme";

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

        public Programme ProgrammeSelectionne
        {
            get => _programmeSelectionne;
            set
            {
                _programmeSelectionne = value;
                OnPropertyChanged();
                if (EstModeTimelineProgramme)
                {
                    ChargerTimelineProgrammes();
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

        public Equipe EquipeSelectionnee
        {
            get => _equipeSelectionnee;
            set
            {
                _equipeSelectionnee = value;
                OnPropertyChanged();
                ChargerDevs(); // Recharger la liste des membres filtrée par équipe
                
                // Si un membre était déjà sélectionné, charger les données
                if (DevSelectionne != null)
                {
                    ChargerCalendrier();
                    ChargerStatsDev();
                }
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
        public ICommand OuvrirTimelineProjetCommand { get; }
        public ICommand ValiderExtensionProjetCommand { get; }

        public SuiviCRAViewModel(CRAService craService, BacklogService backlogService, ProgrammeService programmeService, PermissionService permissionService)
        {
            _craService = craService;
            _backlogService = backlogService;
            _programmeService = programmeService;
            _permissionService = permissionService;

            JoursCalendrier = new ObservableCollection<JourCRAViewModel>();
            MoisAnnee = new ObservableCollection<MoisCRAViewModel>();
            Devs = new ObservableCollection<Utilisateur>();
            Equipes = new ObservableCollection<Equipe>();
            StatsDev = new ObservableCollection<DevCRAInfoViewModel>();
            CRAsJourSelectionne = new ObservableCollection<CRADetailViewModel>();
            Projets = new ObservableCollection<Projet>();
            TachesProjetTimeline = new ObservableCollection<TacheTimelineViewModel>();
            MoisTimeline = new ObservableCollection<MoisCRAViewModel>();
            Programmes = new ObservableCollection<Programme>();
            ProjetsTimelineProgramme = new ObservableCollection<ProjetTimelineViewModel>();
            EquipesProgramme = new ObservableCollection<Equipe>();

            MoisCourant = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            AnneeCourante = DateTime.Now.Year;
            ModeAffichage = "mois";
            SemaineDebut = GetLundiDeLaSemaine(DateTime.Now);

            ChargerProjets();
            ChargerProgrammes();

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
            
            OuvrirTimelineProjetCommand = new RelayCommand(param =>
            {
                if (param is Projet projet)
                {
                    OuvrirTimelineProjet(projet);
                }
            });
            
            ValiderExtensionProjetCommand = new RelayCommand(_ => ValiderExtensionProjet());

            ChargerEquipes();
            // ChargerDevs() sera appelé automatiquement quand une équipe est sélectionnée
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
            Devs.Add(new Utilisateur { Id = 0, Nom = "Tous les membres" });

            var users = _backlogService.GetAllUtilisateurs();
            
            // Filtrer par équipe si une équipe est sélectionnée
            if (EquipeSelectionnee != null && EquipeSelectionnee.Id > 0)
            {
                users = users.Where(u => u.EquipeId == EquipeSelectionnee.Id).ToList();
            }

            foreach (var user in users)
            {
                Devs.Add(user);
            }

            DevSelectionne = Devs.FirstOrDefault();
        }

        private void ChargerEquipes()
        {
            Equipes.Clear();
            Equipes.Add(new Equipe { Id = 0, Nom = "-- Toutes les équipes --" });

            var equipes = _backlogService.GetAllEquipes();
            foreach (var equipe in equipes.OrderBy(e => e.Nom))
            {
                Equipes.Add(equipe);
            }

            EquipeSelectionnee = Equipes.FirstOrDefault();
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

        private void ChargerProgrammes()
        {
            Programmes.Clear();
            var programmes = _programmeService.GetAllProgrammes().Where(p => p.Actif).OrderBy(p => p.Nom).ToList();
            
            foreach (var programme in programmes)
            {
                Programmes.Add(programme);
            }
        }

        private void ChargerTimelineProgrammes()
        {
            ProjetsTimelineProgramme.Clear();
            EquipesProgramme.Clear();

            if (ProgrammeSelectionne == null)
                return;

            var projets = _backlogService.GetAllProjets()
                .Where(p => p.Actif && p.ProgrammeId == ProgrammeSelectionne.Id)
                .ToList();

            if (!projets.Any())
            {
                // Réinitialiser les stats
                NbTotalTachesProgramme = 0;
                NbTachesTermineesProgramme = 0;
                ProgressionGlobaleProgramme = 0;
                BudgetTempsEstime = 0;
                TempsReel = 0;
                NbProjetsGreen = 0;
                NbProjetsAmber = 0;
                NbProjetsRed = 0;
                return;
            }

            // Récupérer toutes les équipes des projets du programme
            var equipesIds = new HashSet<int>();
            foreach (var projet in projets)
            {
                if (projet.EquipesAssigneesIds != null && projet.EquipesAssigneesIds.Count > 0)
                {
                    foreach (var equipeId in projet.EquipesAssigneesIds)
                    {
                        equipesIds.Add(equipeId);
                    }
                }
            }

            var toutesLesEquipes = _backlogService.Database.GetAllEquipes();
            foreach (var equipeId in equipesIds)
            {
                var equipe = toutesLesEquipes.FirstOrDefault(e => e.Id == equipeId);
                if (equipe != null && equipe.Actif)
                {
                    EquipesProgramme.Add(equipe);
                }
            }
            
            OnPropertyChanged(nameof(AfficherEquipesProgramme));
            OnPropertyChanged(nameof(NbEquipesProgramme));

            // Calculer la période globale
            var dateMiniGlobale = DateTime.Now.AddMonths(-6);
            var dateMaxGlobale = DateTime.Now.AddMonths(6);

            // Générer les mois de la timeline
            MoisTimeline.Clear();
            var dateCourante = new DateTime(dateMiniGlobale.Year, dateMiniGlobale.Month, 1);
            var cultureInfo = new System.Globalization.CultureInfo("fr-FR");
            while (dateCourante <= dateMaxGlobale)
            {
                MoisTimeline.Add(new MoisCRAViewModel
                {
                    Mois = dateCourante.Month,
                    Annee = dateCourante.Year,
                    NomMois = cultureInfo.DateTimeFormat.GetAbbreviatedMonthName(dateCourante.Month).ToUpper()
                });
                dateCourante = dateCourante.AddMonths(1);
            }

            // Calculer les statistiques globales du programme
            int totalTaches = 0;
            int totalTerminees = 0;
            double totalBudget = 0;
            double totalTempsReel = 0;
            int nbGreen = 0;
            int nbAmber = 0;
            int nbRed = 0;

            // Calculer les positions des projets
            double yPosition = 0;
            int index = 0;

            foreach (var projet in projets)
            {
                var taches = _backlogService.GetAllBacklogItemsIncludingArchived()
                    .Where(t => t.ProjetId == projet.Id && 
                                t.TypeDemande != TypeDemande.Conges &&
                                t.TypeDemande != TypeDemande.NonTravaille)
                    .ToList();
                
                DateTime? dateDebut = null;
                DateTime? dateFin = null;
                
                if (taches.Any())
                {
                    dateDebut = taches.Min(t => t.DateCreation);
                    var tachesAvecEcheance = taches.Where(t => t.DateFinAttendue != null).ToList();
                    if (tachesAvecEcheance.Any())
                    {
                        dateFin = tachesAvecEcheance.Max(t => t.DateFinAttendue);
                    }
                }

                int nbTaches = taches.Count();
                int nbTerminees = taches.Count(t => t.Statut == Statut.Termine || t.EstArchive);
                double progression = nbTaches > 0 ? (double)nbTerminees / nbTaches * 100 : 0;

                // Accumuler les statistiques
                totalTaches += nbTaches;
                totalTerminees += nbTerminees;
                
                // Compter les statuts RAG
                if (projet.StatutRAG == "Green") nbGreen++;
                else if (projet.StatutRAG == "Amber") nbAmber++;
                else if (projet.StatutRAG == "Red") nbRed++;
                
                // Budget temps (somme des chiffrages)
                double budgetProjet = taches.Where(t => t.ChiffrageJours.HasValue).Sum(t => t.ChiffrageJours.Value);
                totalBudget += budgetProjet;
                
                // Temps réel (somme des CRA)
                var cras = _craService.GetAllCRAs().Where(c => taches.Select(t => t.Id).Contains(c.BacklogItemId));
                double tempsReelProjet = cras.Sum(c => c.HeuresTravaillees) / 8.0; // Convertir en jours
                totalTempsReel += tempsReelProjet;

                double positionX = 0;
                double largeurBarre = 0;

                if (dateDebut.HasValue)
                {
                    var debutMois = dateDebut.Value;
                    var moisDepuisDebut = (debutMois.Year - dateMiniGlobale.Year) * 12 + debutMois.Month - dateMiniGlobale.Month;
                    positionX = moisDepuisDebut * 100.0;

                    if (dateFin.HasValue)
                    {
                        var dureeEnMois = (dateFin.Value.Year - debutMois.Year) * 12 + dateFin.Value.Month - debutMois.Month + 1;
                        largeurBarre = dureeEnMois * 100.0;
                    }
                    else
                    {
                        largeurBarre = 200.0; // Largeur par défaut
                    }
                }

                // Couleurs selon progression
                string couleur = "#4CAF50"; // Vert par défaut
                if (progression < 30)
                    couleur = "#F44336"; // Rouge
                else if (progression < 70)
                    couleur = "#FF9800"; // Orange

                ProjetsTimelineProgramme.Add(new ProjetTimelineViewModel
                {
                    Projet = projet,
                    Nom = projet.Nom,
                    Description = projet.Description,
                    NbTaches = nbTaches,
                    NbTachesTerminees = nbTerminees,
                    ProgressionPourcentage = progression,
                    DateDebut = dateDebut,
                    DateFin = dateFin,
                    EstEnCours = nbTaches > 0 && nbTerminees < nbTaches,
                    PositionX = positionX,
                    PositionY = yPosition,
                    LargeurBarre = largeurBarre,
                    CouleurBarre = couleur
                });

                yPosition += 80; // Espacement vertical entre projets
                index++;
            }

            // Mettre à jour les statistiques globales
            NbTotalTachesProgramme = totalTaches;
            NbTachesTermineesProgramme = totalTerminees;
            ProgressionGlobaleProgramme = totalTaches > 0 ? (double)totalTerminees / totalTaches * 100 : 0;
            BudgetTempsEstime = totalBudget;
            TempsReel = totalTempsReel;
            NbProjetsGreen = nbGreen;
            NbProjetsAmber = nbAmber;
            NbProjetsRed = nbRed;
            
            // Notifier les changements de dates
            OnPropertyChanged(nameof(DateDebutProgramme));
            OnPropertyChanged(nameof(DateFinCibleProgramme));
            OnPropertyChanged(nameof(StatutGlobalProgramme));
            OnPropertyChanged(nameof(AfficherStatutGlobal));
            OnPropertyChanged(nameof(CouleurStatutGlobal));
            OnPropertyChanged(nameof(IconeStatutGlobal));
        }

        public void OuvrirTimelineProjet(Projet projet)
        {
            ProjetSelectionne = projet;
            ModeAffichage = "timeline";
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

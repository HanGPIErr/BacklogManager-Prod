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
using System.Windows;

namespace BacklogManager.ViewModels
{
    public class CRADisplayViewModel : INotifyPropertyChanged
    {
        public CRA CRA { get; set; }
        public string TacheNom { get; set; }
        public double Jours => CRA.HeuresTravaillees / 8.0;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class TacheJourViewModel
    {
        public string NomTache { get; set; }
        public double Heures { get; set; }
        public string Couleur { get; set; }
        public double Pourcentage { get; set; } // Pour le dégradé visuel
    }

    public class JourCalendrierViewModel : INotifyPropertyChanged
    {
        public DateTime Date { get; set; }
        public int Jour { get; set; }
        public bool EstDansMois { get; set; }
        public bool EstAujourdhui { get; set; }
        public bool EstWeekend { get; set; }
        public bool EstJourFerie { get; set; }
        public string NomJourFerie { get; set; }
        public string IconeJourFerie { get; set; } // Chemin vers l'icône personnalisée
        public double TotalHeuresSaisies { get; set; }
        public double TotalHeuresPrevisionnelles { get; set; } // CRA futurs
        public bool ADesCRAs => TotalHeuresSaisies > 0;
        public bool ADesCRAsPrevisionnels => TotalHeuresPrevisionnelles > 0;
        
        // Distinction temporelle pour couleurs
        public bool EstDansPasse { get; set; }
        public bool EstDansFutur { get; set; }
        
        // Nouveaux indicateurs pour tâches spéciales
        public bool EstConges { get; set; }
        public bool EstNonTravaille { get; set; }
        
        // Validation CRA
        public bool ADesCRAsAValider { get; set; } // True si le jour a des CRA non validés dans le passé
        public int NombreCRAsAValider { get; set; } // Nombre de CRA à valider pour ce jour
        
        // Liste des tâches travaillées ce jour (pour affichage détaillé)
        public ObservableCollection<TacheJourViewModel> TachesDuJour { get; set; }
        
        // Afficher les détails des tâches normales (pas congés/non travaillé)
        public bool AfficherDetailsTaches => TachesDuJour != null && TachesDuJour.Count > 0 && !EstConges && !EstNonTravaille;
        
        public string TotalJoursAffiche => TotalHeuresSaisies > 0 ? $"{TotalHeuresSaisies / 8.0:F1}j" : "";

        public JourCalendrierViewModel()
        {
            TachesDuJour = new ObservableCollection<TacheJourViewModel>();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class CRACalendrierViewModel : INotifyPropertyChanged
    {
        private readonly CRAService _craService;
        private readonly BacklogService _backlogService;
        private readonly AuthenticationService _authService;
        private readonly PermissionService _permissionService;
        
        private DateTime _moisCourant;
        private Utilisateur _devSelectionne;
        private bool _afficherToutesLesTaches;
        private JourCalendrierViewModel _jourSelectionne;
        private BacklogItem _tacheSelectionnee;
        private double _joursASaisir;
        private string _commentaire;
        private bool _saisirSurPeriode;
        private DateTime? _dateFinPeriode;
        private Equipe _equipeSelectionnee;

        public ObservableCollection<JourCalendrierViewModel> JoursCalendrier { get; set; }
        public ObservableCollection<Utilisateur> Devs { get; set; }
        public ObservableCollection<Equipe> Equipes { get; set; }
        public ObservableCollection<BacklogItem> TachesDisponibles { get; set; }
        public ObservableCollection<CRADisplayViewModel> CRAsJourSelectionne { get; set; }

        public DateTime MoisCourant
        {
            get => _moisCourant;
            set
            {
                _moisCourant = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(MoisAnneeAffichage));
                ChargerCalendrier();
            }
        }

        public string MoisAnneeAffichage => MoisCourant.ToString("MMMM yyyy").ToUpper();

        public Utilisateur DevSelectionne
        {
            get => _devSelectionne;
            set
            {
                _devSelectionne = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(AfficherBoutonsValidation)); // Notifier changement boutons validation
                ChargerTachesDisponibles();
                ChargerCalendrier();
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
            }
        }

        public bool AfficherToutesLesTaches
        {
            get => _afficherToutesLesTaches;
            set
            {
                _afficherToutesLesTaches = value;
                OnPropertyChanged();
                ChargerTachesDisponibles();
            }
        }

        public JourCalendrierViewModel JourSelectionne
        {
            get => _jourSelectionne;
            set
            {
                _jourSelectionne = value;
                OnPropertyChanged();
                ChargerCRAsJour();
            }
        }

        public BacklogItem TacheSelectionnee
        {
            get => _tacheSelectionnee;
            set
            {
                _tacheSelectionnee = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(JoursRestants));
                OnPropertyChanged(nameof(AfficherAllocationAuto));
                OnPropertyChanged(nameof(ProposeAutoAllocation));
                OnPropertyChanged(nameof(AfficherSaisiePeriode)); // Afficher saisie période pour congés
            }
        }

        public double JoursASaisir
        {
            get => _joursASaisir;
            set
            {
                _joursASaisir = value;
                OnPropertyChanged();
            }
        }

        public string Commentaire
        {
            get => _commentaire;
            set
            {
                _commentaire = value;
                OnPropertyChanged();
            }
        }

        public bool SaisirSurPeriode
        {
            get => _saisirSurPeriode;
            set
            {
                _saisirSurPeriode = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(AfficherDateFin));
            }
        }

        public DateTime? DateFinPeriode
        {
            get => _dateFinPeriode;
            set
            {
                _dateFinPeriode = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(NombreJoursOuvresPeriode));
            }
        }

        public bool AfficherDateFin => SaisirSurPeriode;
        
        public bool AfficherSelecteurDev => Devs.Count > 1; // Afficher uniquement si plusieurs devs (admin)
        
        public bool AfficherBoutonsValidation => AfficherSelecteurDev && DevSelectionne != null; // Boutons visibles si admin ET dev sélectionné

        // Afficher la saisie sur période uniquement pour les congés et jours non travaillés
        public bool AfficherSaisiePeriode => TacheSelectionnee != null && 
                                             (TacheSelectionnee.TypeDemande == TypeDemande.Conges || 
                                              TacheSelectionnee.TypeDemande == TypeDemande.NonTravaille);

        public string NombreJoursOuvresPeriode
        {
            get
            {
                if (!SaisirSurPeriode || JourSelectionne == null || !DateFinPeriode.HasValue)
                    return string.Empty;

                var dateDebut = JourSelectionne.Date;
                var dateFin = DateFinPeriode.Value;

                if (dateFin < dateDebut)
                    return "⚠️ La date de fin doit être après la date de début";

                var joursOuvres = JoursFeriesService.CompterJoursOuvres(dateDebut, dateFin);
                return $"📊 {joursOuvres} jour(s) ouvré(s) sur la période";
            }
        }

        /// <summary>
        /// Calcule le nombre de jours restants à allouer pour la tâche sélectionnée
        /// </summary>
        public double JoursRestants
        {
            get
            {
                if (TacheSelectionnee == null || !TacheSelectionnee.ChiffrageJours.HasValue)
                    return 0;

                // Utiliser GetTempsTotalTache pour compter TOUS les CRA (validés + prévisionnels)
                // afin d'éviter la double allocation
                var tempsTotalHeures = _craService.GetTempsTotalTache(TacheSelectionnee.Id);
                var tempsTotalJours = tempsTotalHeures / 8.0;
                var restant = TacheSelectionnee.ChiffrageJours.Value - tempsTotalJours;
                return Math.Max(0, restant); // Ne pas retourner de valeur négative
            }
        }

        /// <summary>
        /// Indique si on doit afficher le bouton d'allocation automatique
        /// </summary>
        public bool AfficherAllocationAuto => TacheSelectionnee != null && 
                                               TacheSelectionnee.ChiffrageJours.HasValue && 
                                               JoursRestants > 0 &&
                                               JourSelectionne != null &&
                                               DevSelectionne != null &&
                                               TacheSelectionnee.TypeDemande != TypeDemande.Conges &&
                                               TacheSelectionnee.TypeDemande != TypeDemande.NonTravaille;

        /// <summary>
        /// Message proposant l'allocation automatique
        /// </summary>
        public string ProposeAutoAllocation
        {
            get
            {
                if (!AfficherAllocationAuto)
                    return string.Empty;

                var message = $"💡 {JoursRestants:F1} jour(s) restant(s) à allouer";
                if (TacheSelectionnee.DateFinAttendue.HasValue)
                {
                    message += $" (cible: {TacheSelectionnee.DateFinAttendue.Value:dd/MM/yyyy})";
                }
                return message;
            }
        }

        public ICommand MoisPrecedentCommand { get; }
        public ICommand MoisSuivantCommand { get; }
        public ICommand AujourdhuiCommand { get; }
        public ICommand SaisirCRACommand { get; }
        public ICommand SupprimerCRACommand { get; }
        public ICommand RepositionnerCRACommand { get; }
        public ICommand SetJoursRapideCommand { get; }
        public ICommand JourSelectionnCommand { get; }
        public ICommand AllocationAutomatiqueCommand { get; }
        public ICommand ValiderJourneeCommand { get; }

        public CRACalendrierViewModel(CRAService craService, BacklogService backlogService, 
            AuthenticationService authService, PermissionService permissionService)
        {
            _craService = craService;
            _backlogService = backlogService;
            _authService = authService;
            _permissionService = permissionService;

            JoursCalendrier = new ObservableCollection<JourCalendrierViewModel>();
            Devs = new ObservableCollection<Utilisateur>();
            Equipes = new ObservableCollection<Equipe>();
            TachesDisponibles = new ObservableCollection<BacklogItem>();
            CRAsJourSelectionne = new ObservableCollection<CRADisplayViewModel>();

            MoisCourant = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            SaisirSurPeriode = false;
            DateFinPeriode = DateTime.Now;
            SaisirSurPeriode = false;
            DateFinPeriode = DateTime.Now;
            
            MoisPrecedentCommand = new RelayCommand(_ => MoisCourant = MoisCourant.AddMonths(-1));
            MoisSuivantCommand = new RelayCommand(_ => MoisCourant = MoisCourant.AddMonths(1));
            AujourdhuiCommand = new RelayCommand(_ => MoisCourant = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1));
            SaisirCRACommand = new RelayCommand(_ => SaisirCRA(), _ => PeutSaisirCRA());
            SupprimerCRACommand = new RelayCommand(param => SupprimerCRA((CRADisplayViewModel)param));
            RepositionnerCRACommand = new RelayCommand(param => RepositionnerCRA((CRADisplayViewModel)param));
            SetJoursRapideCommand = new RelayCommand(param => JoursASaisir = double.Parse(param.ToString(), System.Globalization.CultureInfo.InvariantCulture));
            JourSelectionnCommand = new RelayCommand(param => JourSelectionne = (JourCalendrierViewModel)param);
            AllocationAutomatiqueCommand = new RelayCommand(_ => AllouerAutomatiquement(), _ => AfficherAllocationAuto);
            ValiderJourneeCommand = new RelayCommand(param => ValiderJournee((JourCalendrierViewModel)param));
            ValiderJourneeCommand = new RelayCommand(param => ValiderJournee((JourCalendrierViewModel)param));

            ChargerEquipes();
            // ChargerDevs() sera appelé après sélection équipe si admin, sinon maintenant
            if (!_permissionService.EstAdministrateur)
            {
                ChargerDevs();
            }
            ChargerTachesDisponibles();
            ChargerCalendrier();
        }

        private void ChargerDevs()
        {
            Devs.Clear();
            
            // Si l'utilisateur est admin, montrer uniquement les Dev et BA (ceux qui saisissent des CRA), sinon seulement lui-même
            if (_permissionService.EstAdministrateur)
            {
                // Récupérer tous les utilisateurs actifs avec leurs rôles
                var users = _backlogService.Database.GetUtilisateurs()
                    .Where(u => u.Actif)
                    .ToList();
                
                var roles = _backlogService.Database.GetRoles();
                
                // Filtrer uniquement les Développeurs et Business Analysts
                var devsFiltered = new List<Utilisateur>();
                foreach (var user in users)
                {
                    var role = roles.FirstOrDefault(r => r.Id == user.RoleId);
                    if (role != null && (role.Type == RoleType.Developpeur || role.Type == RoleType.BusinessAnalyst))
                    {
                        devsFiltered.Add(user);
                    }
                }
                
                // Filtrer par équipe si une équipe est sélectionnée
                if (EquipeSelectionnee != null && EquipeSelectionnee.Id > 0)
                {
                    devsFiltered = devsFiltered.Where(u => u.EquipeId == EquipeSelectionnee.Id).ToList();
                }
                
                // Trier par nom
                var devsList = devsFiltered.OrderBy(d => d.Nom).ToList();
                Devs.Clear();
                foreach (var dev in devsList)
                {
                    Devs.Add(dev);
                }
            }
            else
            {
                // Pour un dev normal, ajouter uniquement lui-même
                Devs.Add(_authService.CurrentUser);
            }

            // Sélectionner l'utilisateur connecté par défaut UNIQUEMENT si pas admin (un seul dev dans la liste)
            if (Devs.Count == 1)
            {
                DevSelectionne = Devs.First();
            }
            else
            {
                // Admin: ne rien sélectionner par défaut, forcer le choix
                DevSelectionne = null;
            }
            
            OnPropertyChanged(nameof(AfficherSelecteurDev));
        }

        private void ChargerEquipes()
        {
            Equipes.Clear();
            
            // Pour admin uniquement
            if (_permissionService.EstAdministrateur)
            {
                Equipes.Add(new Equipe { Id = 0, Nom = "-- Toutes les équipes --" });

                var equipes = _backlogService.GetAllEquipes();
                foreach (var equipe in equipes.OrderBy(e => e.Nom))
                {
                    Equipes.Add(equipe);
                }

                EquipeSelectionnee = Equipes.FirstOrDefault();
            }
        }

        private void ChargerTachesDisponibles()
        {
            // Toujours vider la liste au début pour éviter les doublons
            TachesDisponibles.Clear();
            
            if (DevSelectionne == null)
            {
                return;
            }

            // Pour la saisie CRA, seulement les tâches non-archivées
            var toutesLesTaches = _backlogService.GetAllBacklogItems();

            // Séparer les tâches normales et spéciales
            // Les tâches spéciales (Congés, Non travaillé, Support, Run) sont TOUJOURS disponibles pour tous
            // MAIS on ne garde qu'UNE SEULE tâche par type spécial pour éviter les doublons
            var tachesSpecialesParType = toutesLesTaches
                .Where(t => 
                    t.TypeDemande == TypeDemande.Conges || 
                    t.TypeDemande == TypeDemande.NonTravaille || 
                    t.TypeDemande == TypeDemande.Support || 
                    t.TypeDemande == TypeDemande.Run)
                .GroupBy(t => t.TypeDemande)
                .Select(g => g.First())
                .ToList();

            List<BacklogItem> taches;
            if (_afficherToutesLesTaches)
            {
                // Toutes les tâches de "À faire" à "En test" (pas terminées) SAUF les spéciales déjà dans la liste
                var tachesNormales = toutesLesTaches.Where(t => 
                    t.Statut >= Statut.Afaire && 
                    t.Statut < Statut.Termine &&
                    t.TypeDemande != TypeDemande.Conges &&
                    t.TypeDemande != TypeDemande.NonTravaille &&
                    t.TypeDemande != TypeDemande.Support &&
                    t.TypeDemande != TypeDemande.Run).ToList();
                taches = tachesNormales.Concat(tachesSpecialesParType).ToList();
            }
            else
            {
                // Tâches assignées au dev (à faire/en cours/test) SAUF les spéciales + TOUTES les tâches spéciales
                var tachesNormales = toutesLesTaches.Where(t => 
                    t.DevAssigneId == DevSelectionne.Id && 
                    (t.Statut == Statut.Afaire || t.Statut == Statut.EnCours || t.Statut == Statut.Test) &&
                    t.TypeDemande != TypeDemande.Conges &&
                    t.TypeDemande != TypeDemande.NonTravaille &&
                    t.TypeDemande != TypeDemande.Support &&
                    t.TypeDemande != TypeDemande.Run).ToList();
                // Les tâches spéciales sont disponibles pour tous les devs (pas de filtre DevAssigneId)
                taches = tachesNormales.Concat(tachesSpecialesParType).ToList();
            }

            // Filtrer les tâches qui ont encore des jours à saisir
            var tachesAvecChiffrage = new List<BacklogItem>();
            foreach (var tache in taches)
            {
                // Les tâches spéciales sont toujours disponibles (pas de limite de chiffrage)
                bool estTacheSpeciale = tache.TypeDemande == TypeDemande.Conges || 
                                       tache.TypeDemande == TypeDemande.NonTravaille || 
                                       tache.TypeDemande == TypeDemande.Support || 
                                       tache.TypeDemande == TypeDemande.Run;
                
                if (estTacheSpeciale)
                {
                    tachesAvecChiffrage.Add(tache);
                    continue;
                }
                
                // Pour les tâches normales, vérifier s'il reste du chiffrage
                if (tache.ChiffrageHeures.HasValue && tache.ChiffrageHeures.Value > 0)
                {
                    // Calculer les heures déjà saisies dans le CRA
                    var heuresSaisies = _craService.GetHeuresSaisiesPourTache(tache.Id);
                    var heuresRestantes = tache.ChiffrageHeures.Value - heuresSaisies;
                    
                    // DEBUG: Log pour diagnostiquer le problème
                    System.Diagnostics.Debug.WriteLine($"Tâche #{tache.Id} '{tache.Titre}': Chiffrage={tache.ChiffrageHeures}h, Saisies={heuresSaisies}h, Restantes={heuresRestantes}h");
                    
                    // Si il reste au moins 0.5h à saisir, afficher la tâche
                    if (heuresRestantes >= 0.5)
                    {
                        tachesAvecChiffrage.Add(tache);
                    }
                }
                else
                {
                    // Pas de chiffrage défini = toujours disponible
                    tachesAvecChiffrage.Add(tache);
                }
            }

            // Dédupliquer STRICTEMENT : 
            // - Pour les tâches spéciales : une seule par TypeDemande
            // - Pour les tâches normales : une seule par Id
            var tachesFinales = new List<BacklogItem>();
            var typesSpeciauxVus = new HashSet<TypeDemande>();
            var idsVus = new HashSet<int>();

            foreach (var tache in tachesAvecChiffrage.OrderByDescending(t => t.Priorite).ThenBy(t => t.Titre))
            {
                bool estTacheSpeciale = tache.TypeDemande == TypeDemande.Conges || 
                                       tache.TypeDemande == TypeDemande.NonTravaille || 
                                       tache.TypeDemande == TypeDemande.Support || 
                                       tache.TypeDemande == TypeDemande.Run;

                if (estTacheSpeciale)
                {
                    // Pour les tâches spéciales : une seule par type
                    if (!typesSpeciauxVus.Contains(tache.TypeDemande))
                    {
                        typesSpeciauxVus.Add(tache.TypeDemande);
                        tachesFinales.Add(tache);
                    }
                }
                else
                {
                    // Pour les tâches normales : une seule par Id
                    if (!idsVus.Contains(tache.Id))
                    {
                        idsVus.Add(tache.Id);
                        tachesFinales.Add(tache);
                    }
                }
            }

            foreach (var tache in tachesFinales)
            {
                TachesDisponibles.Add(tache);
            }
        }

        private void ChargerCalendrier()
        {
            JoursCalendrier.Clear();

            // Premier jour du mois
            var premierJour = new DateTime(MoisCourant.Year, MoisCourant.Month, 1);
            
            // Dernier jour du mois
            var dernierJour = premierJour.AddMonths(1).AddDays(-1);

            // Jour de la semaine du premier jour (0 = dimanche, 1 = lundi, etc.)
            int premierJourSemaine = (int)premierJour.DayOfWeek;
            // Ajuster pour que lundi = 0
            premierJourSemaine = premierJourSemaine == 0 ? 6 : premierJourSemaine - 1;

            // Charger les CRAs du mois pour le dev sélectionné
            var cras = DevSelectionne != null ? 
                _craService.GetCRAsByDev(DevSelectionne.Id, premierJour, dernierJour) : 
                new System.Collections.Generic.List<CRA>();

            // Charger toutes les tâches pour détecter les types spéciaux (y compris archivées)
            var toutesLesTaches = _backlogService.GetAllBacklogItemsIncludingArchived();

            // Ajouter les jours du mois précédent pour compléter la première semaine
            var jourDebut = premierJour.AddDays(-premierJourSemaine);
            
            // Générer 42 jours (6 semaines) pour avoir une grille complète
            for (int i = 0; i < 42; i++)
            {
                var date = jourDebut.AddDays(i);
                var crasDuJour = cras.Where(c => c.Date.Date == date.Date).ToList();
                var totalHeures = crasDuJour.Sum(c => c.HeuresTravaillees);

                // Détecter si le jour contient des tâches spéciales et créer la liste des tâches
                var estConges = false;
                var estNonTravaille = false;
                var tachesDuJour = new ObservableCollection<TacheJourViewModel>();
                
                // Palette de couleurs pour différencier les tâches
                var couleurs = new[] { "#00915A", "#1976D2", "#7B1FA2", "#D32F2F", "#F57C00", "#388E3C", "#0097A7", "#C2185B" };
                int indexCouleur = 0;
                
                foreach (var cra in crasDuJour.OrderByDescending(c => c.HeuresTravaillees))
                {
                    var tache = toutesLesTaches.FirstOrDefault(t => t.Id == cra.BacklogItemId);
                    if (tache != null)
                    {
                        if (tache.TypeDemande == TypeDemande.Conges)
                            estConges = true;
                        else if (tache.TypeDemande == TypeDemande.NonTravaille)
                            estNonTravaille = true;
                        else
                        {
                            // Tâche normale : ajouter à la liste avec une couleur
                            var pourcentage = totalHeures > 0 ? (cra.HeuresTravaillees / totalHeures) * 100 : 0;
                            tachesDuJour.Add(new TacheJourViewModel
                            {
                                NomTache = tache.Titre.Length > 25 ? tache.Titre.Substring(0, 22) + "..." : tache.Titre,
                                Heures = cra.HeuresTravaillees,
                                Couleur = couleurs[indexCouleur % couleurs.Length],
                                Pourcentage = pourcentage
                            });
                            indexCouleur++;
                        }
                    }
                }

                var aujourdhui = DateTime.Now.Date;
                var estJourFerie = JoursFeriesService.EstJourFerie(date);
                var nomJourFerie = JoursFeriesService.GetNomJourFerie(date);
                
                // Détecter les CRA à valider (prévisionnels dans le passé non validés)
                var crasAValider = crasDuJour.Where(c => c.EstAValider).ToList();
                var aDesCRAsAValider = crasAValider.Any();
                var nombreCRAsAValider = crasAValider.Count;
                
                var jourVM = new JourCalendrierViewModel
                {
                    Date = date,
                    Jour = date.Day,
                    EstDansMois = date.Month == MoisCourant.Month,
                    EstAujourdhui = date.Date == aujourdhui,
                    EstWeekend = JoursFeriesService.EstWeekend(date),
                    EstJourFerie = estJourFerie,
                    NomJourFerie = nomJourFerie,
                    IconeJourFerie = estJourFerie ? GetIconeJourFerie(nomJourFerie) : null,
                    EstDansPasse = date < aujourdhui,
                    EstDansFutur = date > aujourdhui,
                    TotalHeuresSaisies = totalHeures,
                    TotalHeuresPrevisionnelles = 0, // Sera calculé séparément
                    EstConges = estConges,
                    EstNonTravaille = estNonTravaille,
                    TachesDuJour = tachesDuJour,
                    ADesCRAsAValider = aDesCRAsAValider,
                    NombreCRAsAValider = nombreCRAsAValider
                };

                JoursCalendrier.Add(jourVM);
            }
        }

        private void ChargerCRAsJour()
        {
            CRAsJourSelectionne.Clear();

            if (JourSelectionne == null || DevSelectionne == null) return;

            var cras = _craService.GetCRAsByDev(DevSelectionne.Id, JourSelectionne.Date, JourSelectionne.Date);
            var taches = _backlogService.GetAllBacklogItemsIncludingArchived();
            
            foreach (var cra in cras.OrderBy(c => c.DateCreation))
            {
                var tache = taches.FirstOrDefault(t => t.Id == cra.BacklogItemId);
                CRAsJourSelectionne.Add(new CRADisplayViewModel
                {
                    CRA = cra,
                    TacheNom = tache?.Titre ?? "Tâche supprimée"
                });
            }
        }

        private bool PeutSaisirCRA()
        {
            return JourSelectionne != null && 
                   TacheSelectionnee != null && 
                   JoursASaisir > 0 &&
                   DevSelectionne != null;
        }

        private void SaisirCRA()
        {
            if (JourSelectionne == null || TacheSelectionnee == null || JoursASaisir <= 0 || DevSelectionne == null)
            {
                System.Windows.MessageBox.Show(
                    "Veuillez remplir tous les champs obligatoires.",
                    "Validation",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

            // Saisie sur période pour les congés, sinon mode journalier
            if (SaisirSurPeriode && AfficherSaisiePeriode)
            {
                SaisirCRAPeriode();
            }
            else
            {
                SaisirCRAJournalier();
            }
        }

        private void SaisirCRAJournalier()
        {
            try
            {
                // Les congés et jours non travaillés ne sont pas limités par le chiffrage
                bool estCongesOuNonTravaille = TacheSelectionnee.TypeDemande == TypeDemande.Conges || 
                                                TacheSelectionnee.TypeDemande == TypeDemande.NonTravaille;

                // Vérifier qu'il reste du temps à allouer pour cette tâche (sauf congés/non travaillé)
                if (!estCongesOuNonTravaille && TacheSelectionnee.ChiffrageJours.HasValue && JoursRestants <= 0)
                {
                    System.Windows.MessageBox.Show(
                        $"⚠️ Il ne reste plus de temps à allouer pour cette tâche !\\n\\n" +
                        $"Chiffrage: {TacheSelectionnee.ChiffrageJours.Value:F1}j\\n" +
                        $"Déjà alloué: {TacheSelectionnee.ChiffrageJours.Value:F1}j (validé + prévisionnel)\\n\\n" +
                        $"Si vous devez ajouter plus de temps, augmentez d'abord le chiffrage de la tâche.",
                        "Tâche complète",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning);
                    return;
                }

                // Vérifier que la saisie ne dépasse pas le temps restant (sauf congés/non travaillé)
                if (!estCongesOuNonTravaille && TacheSelectionnee.ChiffrageJours.HasValue && JoursASaisir > JoursRestants)
                {
                    var result = System.Windows.MessageBox.Show(
                        $"⚠️ Vous essayez de saisir {JoursASaisir:F1}j mais il ne reste que {JoursRestants:F1}j à allouer.\\n\\n" +
                        $"Voulez-vous saisir uniquement {JoursRestants:F1}j ?",
                        "Dépassement",
                        System.Windows.MessageBoxButton.YesNo,
                        System.Windows.MessageBoxImage.Question);

                    if (result == System.Windows.MessageBoxResult.Yes)
                    {
                        JoursASaisir = JoursRestants;
                    }
                    else
                    {
                        return;
                    }
                }

                // Vérification jour férié / weekend
                if (!JoursFeriesService.EstJourOuvre(JourSelectionne.Date))
                {
                    var nomJour = JoursFeriesService.EstWeekend(JourSelectionne.Date) ? "week-end" : 
                                 "jour férié (" + JoursFeriesService.GetNomJourFerie(JourSelectionne.Date) + ")";
                    var result = System.Windows.MessageBox.Show(
                        $"Le {JourSelectionne.Date:dd/MM/yyyy} est un {nomJour}.\\n\\nVoulez-vous quand même saisir un CRA ?",
                        "Confirmation",
                        System.Windows.MessageBoxButton.YesNo,
                        System.Windows.MessageBoxImage.Question);

                    if (result != System.Windows.MessageBoxResult.Yes)
                        return;
                }

                // Pour les congés/non travaillé, proposer le décalage des tâches existantes
                if (estCongesOuNonTravaille)
                {
                    // Vérifier d'abord s'il y a déjà un congé/non-travaillé ce jour
                    var crasExistantsCeJour = _craService.GetCRAsByDev(DevSelectionne.Id, JourSelectionne.Date, JourSelectionne.Date);
                    var aDejaCongesOuNonTravaille = crasExistantsCeJour.Any(c => {
                        var tache = _backlogService.GetBacklogItemById(c.BacklogItemId);
                        return tache != null && 
                               (tache.TypeDemande == TypeDemande.Conges || 
                                tache.TypeDemande == TypeDemande.NonTravaille);
                    });

                    // Si un congé/non-travaillé existe déjà ce jour, ne pas en créer un autre
                    if (aDejaCongesOuNonTravaille)
                    {
                        System.Windows.MessageBox.Show(
                            $"⚠️ Un congé ou jour non-travaillé existe déjà le {JourSelectionne.Date:dd/MM/yyyy}.\n\n" +
                            $"Vous ne pouvez pas ajouter un autre congé sur ce jour.",
                            "Congé existant",
                            System.Windows.MessageBoxButton.OK,
                            System.Windows.MessageBoxImage.Warning);
                        return;
                    }

                    // Vérifier s'il y a des tâches à décaler ce jour-là
                    var crasExistants = crasExistantsCeJour
                        .Where(c => c.BacklogItemId != TacheSelectionnee.Id) // Exclure la tâche de congés elle-même
                        .ToList();

                    if (crasExistants.Any())
                    {
                        var result = System.Windows.MessageBox.Show(
                            $"⚠️ Il y a déjà {crasExistants.Count} CRA existant(s) le {JourSelectionne.Date:dd/MM/yyyy}.\n\n" +
                            $"Voulez-vous décaler automatiquement ces tâches ?\n\n" +
                            $"✅ Oui : Les tâches seront décalées au prochain jour disponible\n" +
                            $"❌ Non : Les tâches resteront en place (superposition)",
                            "Décalage automatique",
                            System.Windows.MessageBoxButton.YesNo,
                            System.Windows.MessageBoxImage.Question);

                        if (result == System.Windows.MessageBoxResult.Yes)
                        {
                            DecalerCRAsExistants(crasExistants, JourSelectionne.Date, JourSelectionne.Date);
                        }
                    }
                }

                // Convertir jours en heures (1j = 8h)
                double heures = JoursASaisir * 8.0;

                // Vérifier la charge maximale journalière (sauf pour congés déjà décalés)
                double chargeActuelle = _craService.GetChargeParJour(DevSelectionne.Id, JourSelectionne.Date);
                double chargeTotal = chargeActuelle + heures;

                if (chargeTotal > 24)
                {
                    var result = System.Windows.MessageBox.Show(
                        $"Le total des heures pour cette journée sera de {chargeTotal}h (max recommandé: 24h).\\n\\nVoulez-vous continuer ?",
                        "Charge élevée",
                        System.Windows.MessageBoxButton.YesNo,
                        System.Windows.MessageBoxImage.Warning);
                    
                    if (result == System.Windows.MessageBoxResult.No)
                        return;
                }

                // Créer le CRA
                var cra = new CRA
                {
                    BacklogItemId = TacheSelectionnee.Id,
                    DevId = DevSelectionne.Id,
                    Date = JourSelectionne.Date,
                    HeuresTravaillees = heures,
                    Commentaire = Commentaire,
                    DateCreation = DateTime.Now,
                    EstPrevisionnel = true, // Tous les CRA sont prévisionnels à la création
                    EstValide = false // À valider manuellement
                };

                try
                {
                    Mouse.OverrideCursor = Cursors.Wait;
                    _craService.SaveCRA(cra);

                    // Réinitialiser le formulaire
                    JoursASaisir = 0;
                    Commentaire = "";
                    TacheSelectionnee = null;

                    // Rafraîchir l'affichage
                    ChargerCalendrier();
                    ChargerCRAsJour();
                    ChargerTachesDisponibles();
                }
                finally
                {
                    Mouse.OverrideCursor = null;
                }

                System.Windows.MessageBox.Show(
                    "CRA enregistré avec succès !",
                    "Succès",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Erreur lors de l'enregistrement : {ex.Message}",
                    "Erreur",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }

        private void SaisirCRAPeriode()
        {
            if (!DateFinPeriode.HasValue)
            {
                System.Windows.MessageBox.Show(
                    "Veuillez sélectionner une date de fin.",
                    "Validation",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

            var dateDebut = JourSelectionne.Date;
            var dateFin = DateFinPeriode.Value.Date;

            // Validations
            if (dateFin < dateDebut)
            {
                System.Windows.MessageBox.Show(
                    "La date de fin doit être après ou égale à la date de début.",
                    "Validation",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

            // Pour les congés/non travaillé, proposer le décalage des tâches existantes
            bool estCongesOuNonTravaille = TacheSelectionnee.TypeDemande == TypeDemande.Conges || 
                                            TacheSelectionnee.TypeDemande == TypeDemande.NonTravaille;

            if (estCongesOuNonTravaille)
            {
                // Vérifier s'il y a des tâches à décaler
                var crasExistants = _craService.GetCRAsByDev(DevSelectionne.Id, dateDebut, dateFin)
                    .Where(c => c.BacklogItemId != TacheSelectionnee.Id) // Exclure la tâche de congés elle-même
                    .ToList();

                if (crasExistants.Any())
                {
                    var result = System.Windows.MessageBox.Show(
                        $"⚠️ Il y a {crasExistants.Count} CRA existant(s) sur cette période.\n\n" +
                        $"Voulez-vous décaler automatiquement ces tâches après vos congés ?\n\n" +
                        $"✅ Oui : Les tâches seront décalées après le {dateFin:dd/MM/yyyy}\n" +
                        $"❌ Non : Les tâches resteront en place (superposition)",
                        "Décalage automatique",
                        System.Windows.MessageBoxButton.YesNo,
                        System.Windows.MessageBoxImage.Question);

                    if (result == System.Windows.MessageBoxResult.Yes)
                    {
                        DecalerCRAsExistants(crasExistants, dateDebut, dateFin);
                    }
                }
            }

            // Calculer le nombre de jours ouvrés sur la période
            // Les congés ne doivent être posés QUE sur les jours ouvrés (pas week-end ni jours fériés)
            var joursOuvres = new List<DateTime>();
            for (var date = dateDebut; date <= dateFin; date = date.AddDays(1))
            {
                // Toujours vérifier que c'est un jour ouvré (même pour les congés)
                if (JoursFeriesService.EstJourOuvre(date))
                {
                    joursOuvres.Add(date);
                }
            }

            if (joursOuvres.Count == 0)
            {
                System.Windows.MessageBox.Show(
                    "Aucun jour ouvré trouvé sur cette période.",
                    "Validation",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

            // Confirmer la saisie
            var heuresParJour = JoursASaisir * 8.0;
            var totalHeures = heuresParJour * joursOuvres.Count;
            
            var confirmResult = System.Windows.MessageBox.Show(
                $"💾 Créer des CRA sur {joursOuvres.Count} jour(s)\n\n" +
                $"📅 Du {joursOuvres.First():dd/MM/yyyy} au {joursOuvres.Last():dd/MM/yyyy}\n" +
                $"⏱️ {JoursASaisir:F1}j ({heuresParJour:F1}h) par jour\n" +
                $"📊 Total : {joursOuvres.Count * JoursASaisir:F1}j ({totalHeures:F1}h)\n\n" +
                $"Continuer ?",
                "Confirmation",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            if (confirmResult != System.Windows.MessageBoxResult.Yes)
                return;

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                
                int nombreCRAsCrees = 0;
                int nombreCRAsIgnores = 0;
                var aujourdhui = DateTime.Now.Date;

                foreach (var jour in joursOuvres)
                {
                    // Si c'est des congés/non-travaillé, vérifier s'il n'y a pas déjà un CRA du même type ce jour
                    if (estCongesOuNonTravaille)
                    {
                        var crasExistantsCeJour = _craService.GetCRAsByDev(DevSelectionne.Id, jour, jour);
                        var aDejaCongesOuNonTravaille = crasExistantsCeJour.Any(c => {
                            var tache = _backlogService.GetBacklogItemById(c.BacklogItemId);
                            return tache != null && 
                                   (tache.TypeDemande == TypeDemande.Conges || 
                                    tache.TypeDemande == TypeDemande.NonTravaille);
                        });

                        // Si un congé/non-travaillé existe déjà ce jour, ne pas en créer un autre
                        if (aDejaCongesOuNonTravaille)
                        {
                            nombreCRAsIgnores++;
                            continue;
                        }
                    }

                    var cra = new CRA
                    {
                        DevId = DevSelectionne.Id,
                        BacklogItemId = TacheSelectionnee.Id,
                        Date = jour,
                        HeuresTravaillees = heuresParJour,
                        Commentaire = Commentaire,
                        DateCreation = DateTime.Now,
                        EstPrevisionnel = true, // Tous les CRA sont prévisionnels à la création
                        EstValide = false // À valider manuellement
                    };

                    _craService.SaveCRA(cra);
                    nombreCRAsCrees++;
                }

                // Réinitialiser le formulaire
                TacheSelectionnee = null;
                JoursASaisir = 0;
                Commentaire = string.Empty;
                SaisirSurPeriode = false;
                DateFinPeriode = null;
                
                ChargerCalendrier();
                ChargerCRAsJour();
                ChargerTachesDisponibles();

                // Message de succès avec détails
                string message = $"✅ {nombreCRAsCrees} CRA(s) enregistré(s) avec succès !";
                if (nombreCRAsIgnores > 0)
                {
                    message += $"\n\n⚠️ {nombreCRAsIgnores} jour(s) ignoré(s) car un congé/non-travaillé existait déjà.";
                }

                System.Windows.MessageBox.Show(
                    message,
                    "Succès",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Erreur lors de l'enregistrement : {ex.Message}",
                    "Erreur",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        /// <summary>
        /// Décale les CRA existants après une période de congés
        /// </summary>
        private void DecalerCRAsExistants(List<CRA> crasADecaler, DateTime debutConges, DateTime finConges)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                
                // IMPORTANT : Ne décaler QUE les tâches de travail, pas les congés/non travaillé
                var crasATravailADecaler = crasADecaler
                    .Where(c => {
                        var tache = _backlogService.GetBacklogItemById(c.BacklogItemId);
                        return tache != null && 
                               tache.TypeDemande != TypeDemande.Conges && 
                               tache.TypeDemande != TypeDemande.NonTravaille;
                    })
                    .OrderBy(c => c.Date)
                    .ToList();
                
                // Si aucune tâche de travail à décaler, terminé
                if (!crasATravailADecaler.Any())
                    return;
                
                // Point de départ pour le décalage : jour suivant la fin des congés
                var dateDecalage = finConges.AddDays(1);
                
                // Dictionnaire pour suivre la charge ajoutée à chaque jour pendant le décalage
                var chargeAjoutee = new Dictionary<DateTime, double>();
                
                // Décaler chaque CRA qui est dans la période de congés
                foreach (var cra in crasATravailADecaler)
                {
                    // Si le CRA est dans la période de congés
                    if (cra.Date >= debutConges && cra.Date <= finConges)
                    {
                        // Trouver le prochain jour ouvré disponible (sans férié, weekend ET sans congé existant)
                        while (!JoursFeriesService.EstJourOuvre(dateDecalage) || ADejaCongesCeJour(cra.DevId, dateDecalage))
                        {
                            dateDecalage = dateDecalage.AddDays(1);
                        }
                        
                        // Calculer la charge du jour (existante + ce qu'on a déjà ajouté)
                        // MAIS en excluant les congés/non-travaillé de la charge existante
                        var chargeExistante = GetChargeJourSansCongés(cra.DevId, dateDecalage);
                        var chargeDejaAjoutee = chargeAjoutee.ContainsKey(dateDecalage) ? chargeAjoutee[dateDecalage] : 0;
                        var chargeTotal = chargeExistante + chargeDejaAjoutee + cra.HeuresTravaillees;
                        
                        // Si le jour serait trop chargé (> 8h), passer au jour suivant
                        while (chargeTotal > 8.0)
                        {
                            dateDecalage = dateDecalage.AddDays(1);
                            while (!JoursFeriesService.EstJourOuvre(dateDecalage) || ADejaCongesCeJour(cra.DevId, dateDecalage))
                            {
                                dateDecalage = dateDecalage.AddDays(1);
                            }
                            chargeExistante = GetChargeJourSansCongés(cra.DevId, dateDecalage);
                            chargeDejaAjoutee = chargeAjoutee.ContainsKey(dateDecalage) ? chargeAjoutee[dateDecalage] : 0;
                            chargeTotal = chargeExistante + chargeDejaAjoutee + cra.HeuresTravaillees;
                        }
                        
                        // Enregistrer la charge ajoutée à ce jour
                        if (chargeAjoutee.ContainsKey(dateDecalage))
                            chargeAjoutee[dateDecalage] += cra.HeuresTravaillees;
                        else
                            chargeAjoutee[dateDecalage] = cra.HeuresTravaillees;
                        
                        // Décaler le CRA
                        cra.Date = dateDecalage;
                        
                        // Mettre à jour EstPrevisionnel et EstValide selon la nouvelle date
                        var aujourdhui = DateTime.Now.Date;
                        cra.EstPrevisionnel = dateDecalage >= aujourdhui;
                        cra.EstValide = dateDecalage < aujourdhui;
                        
                        _craService.SaveCRA(cra);
                        
                        // Passer au jour suivant pour le prochain CRA
                        dateDecalage = dateDecalage.AddDays(1);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Erreur lors du décalage des CRA : {ex.Message}",
                    "Erreur",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        /// <summary>
        /// Calcule la charge d'un jour en excluant les congés et jours non travaillés
        /// </summary>
        private double GetChargeJourSansCongés(int devId, DateTime date)
        {
            var crasDuJour = _craService.GetCRAsByDev(devId, date, date);
            
            // Ne compter que les CRA de vraies tâches (pas congés ni non-travaillé)
            double charge = 0;
            foreach (var cra in crasDuJour)
            {
                var tache = _backlogService.GetBacklogItemById(cra.BacklogItemId);
                if (tache != null && 
                    tache.TypeDemande != TypeDemande.Conges && 
                    tache.TypeDemande != TypeDemande.NonTravaille)
                {
                    charge += cra.HeuresTravaillees;
                }
            }
            
            return charge;
        }

        /// <summary>
        /// Vérifie si le dev a déjà un congé ou jour non travaillé à cette date
        /// </summary>
        private bool ADejaCongesCeJour(int devId, DateTime date)
        {
            var crasDuJour = _craService.GetCRAsByDev(devId, date, date);
            
            return crasDuJour.Any(c => {
                var tache = _backlogService.GetBacklogItemById(c.BacklogItemId);
                return tache != null && 
                       (tache.TypeDemande == TypeDemande.Conges || 
                        tache.TypeDemande == TypeDemande.NonTravaille);
            });
        }

        /// <summary>
        /// Trouve les jours disponibles avec décalage automatique si nécessaire
        /// </summary>
        private System.Collections.Generic.List<DateTime> TrouverJoursDisponibles(
            DateTime dateDebut, 
            DateTime dateFin, 
            int devId, 
            double heuresParJour, 
            int nombreJoursNecessaires)
        {
            var joursDisponibles = new System.Collections.Generic.List<DateTime>();
            var dateActuelle = dateDebut;
            var maxRecherche = dateFin.AddMonths(3); // Limite de recherche : 3 mois après dateFin

            while (joursDisponibles.Count < nombreJoursNecessaires && dateActuelle <= maxRecherche)
            {
                // Vérifier si le jour est ouvré (pas weekend, pas férié)
                if (!JoursFeriesService.EstWeekend(dateActuelle) && 
                    !JoursFeriesService.EstJourFerie(dateActuelle))
                {
                    // Vérifier la charge déjà saisie
                    var chargeJour = _craService.GetChargeParJour(devId, dateActuelle);

                    // Vérifier s'il reste de la capacité (max 8h/jour = 1j)
                    if (chargeJour + heuresParJour <= 8.0)
                    {
                        joursDisponibles.Add(dateActuelle);
                    }
                }

                dateActuelle = dateActuelle.AddDays(1);
            }

            return joursDisponibles;
        }

        private void SupprimerCRA(CRADisplayViewModel craVM)
        {
            if (craVM == null) return;

            var result = System.Windows.MessageBox.Show(
                $"Êtes-vous sûr de vouloir supprimer ce CRA ?\n\n{craVM.Jours:F1}j sur {craVM.TacheNom}",
                "Confirmation",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                try
                {
                    Mouse.OverrideCursor = Cursors.Wait;
                    _craService.DeleteCRA(craVM.CRA.Id, _authService.CurrentUser.Id, _permissionService.EstAdministrateur);
                    
                    // Force le rechargement complet des données
                    ChargerTachesDisponibles();  // D'abord les tâches disponibles
                    ChargerCalendrier();         // Puis le calendrier
                    ChargerCRAsJour();           // Et enfin les CRAs du jour

                    System.Windows.MessageBox.Show(
                        "CRA supprimé avec succès !",
                        "Succès",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(
                        $"Erreur lors de la suppression : {ex.Message}",
                        "Erreur",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Error);
                }
                finally
                {
                    Mouse.OverrideCursor = null;
                }
            }
        }

        /// <summary>
        /// Déplace un CRA d'un jour à un autre (drag and drop)
        /// </summary>
        public void DeplacerCRA(CRADisplayViewModel craDisplay, JourCalendrierViewModel jourCible)
        {
            if (craDisplay == null || jourCible == null || DevSelectionne == null) return;
            
            // Ne pas permettre de déplacer hors du mois
            if (!jourCible.EstDansMois) return;

            var cra = craDisplay.CRA;
            var dateOrigine = cra.Date;
            var dateDestination = jourCible.Date;

            // Si c'est le même jour, ne rien faire
            if (dateOrigine.Date == dateDestination.Date) return;

            // Vérifier qu'on ne dépasse pas 8h sur le jour de destination
            var chargeDestination = _craService.GetChargeParJour(DevSelectionne.Id, dateDestination);
            if (chargeDestination + cra.HeuresTravaillees > 8.0)
            {
                var joursDisponibles = chargeDestination / 8.0;
                System.Windows.MessageBox.Show(
                    $"Impossible de déplacer ce CRA :\n\n" +
                    $"Le {dateDestination:dd/MM/yyyy} est déjà chargé à {joursDisponibles:F1}j\n" +
                    $"Il reste seulement {(8.0 - chargeDestination) / 8.0:F1}j disponible(s).\n\n" +
                    $"Ce CRA nécessite {cra.HeuresTravaillees / 8.0:F1}j.",
                    "Jour trop chargé",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

            // Demander confirmation
            var result = System.Windows.MessageBox.Show(
                $"Déplacer ce CRA ?\n\n" +
                $"Tâche : {craDisplay.TacheNom}\n" +
                $"Temps : {craDisplay.Jours:F1}j\n\n" +
                $"Du {dateOrigine:dddd dd/MM/yyyy}\n" +
                $"Vers {dateDestination:dddd dd/MM/yyyy}",
                "Confirmation de déplacement",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                try
                {
                    // Déplacer le CRA
                    cra.Date = dateDestination;
                    
                    // Mettre à jour EstPrevisionnel et EstValide selon la nouvelle date
                    var aujourdhui = DateTime.Now.Date;
                    cra.EstPrevisionnel = dateDestination >= aujourdhui;
                    cra.EstValide = dateDestination < aujourdhui;
                    
                    _craService.SaveCRA(cra);
                    
                    // Rafraîchir l'affichage
                    ChargerCalendrier();
                    
                    // Si le jour sélectionné est l'origine ou la destination, recharger les CRA affichés
                    if (JourSelectionne != null && 
                        (JourSelectionne.Date.Date == dateOrigine.Date || JourSelectionne.Date.Date == dateDestination.Date))
                    {
                        ChargerCRAsJour();
                    }
                    
                    System.Windows.MessageBox.Show(
                        $"✓ CRA déplacé avec succès vers le {dateDestination:dd/MM/yyyy}",
                        "Succès",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(
                        $"Erreur lors du déplacement : {ex.Message}",
                        "Erreur",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// Repositionne un CRA au prochain jour disponible à partir d'aujourd'hui
        /// </summary>
        private void RepositionnerCRA(CRADisplayViewModel craDisplay)
        {
            if (craDisplay == null || DevSelectionne == null) return;

            var cra = craDisplay.CRA;
            var dateOrigine = cra.Date;
            var aujourdhui = DateTime.Now.Date;

            // Chercher le prochain jour disponible à partir d'aujourd'hui
            DateTime dateRecherche = aujourdhui;
            DateTime? dateDisponible = null;
            int joursRecherches = 0;
            const int maxJoursRecherche = 90; // Chercher max 3 mois

            while (joursRecherches < maxJoursRecherche)
            {
                // Vérifier si c'est un jour ouvré
                if (JoursFeriesService.EstJourOuvre(dateRecherche))
                {
                    // Vérifier la charge du jour
                    var chargeJour = _craService.GetChargeParJour(DevSelectionne.Id, dateRecherche);
                    
                    // Si le jour a de la place pour ce CRA
                    if (chargeJour + cra.HeuresTravaillees <= 8.0)
                    {
                        dateDisponible = dateRecherche;
                        break;
                    }
                }
                
                dateRecherche = dateRecherche.AddDays(1);
                joursRecherches++;
            }

            if (!dateDisponible.HasValue)
            {
                System.Windows.MessageBox.Show(
                    "Aucun jour disponible trouvé dans les 3 prochains mois.\n\n" +
                    "Tous les jours ouvrés sont déjà chargés à 8h.",
                    "Aucun créneau disponible",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

            var dateDestination = dateDisponible.Value;

            // Si c'est le même jour, rien à faire
            if (dateOrigine.Date == dateDestination.Date)
            {
                System.Windows.MessageBox.Show(
                    "Ce CRA est déjà au prochain jour disponible.",
                    "Information",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
                return;
            }

            // Calculer le nombre de jours de décalage
            int joursDecalage = (int)(dateDestination - aujourdhui).TotalDays;
            
            // Demander confirmation
            var result = System.Windows.MessageBox.Show(
                $"📍 Repositionner ce CRA ?\n\n" +
                $"Tâche : {craDisplay.TacheNom}\n" +
                $"Temps : {craDisplay.Jours:F1}j\n\n" +
                $"Date actuelle : {dateOrigine:dddd dd/MM/yyyy}\n" +
                $"➜ Prochain jour disponible : {dateDestination:dddd dd/MM/yyyy}\n" +
                $"   (dans {joursDecalage} jour{(joursDecalage > 1 ? "s" : "")})",
                "Repositionner au prochain créneau",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                try
                {
                    // Déplacer le CRA
                    cra.Date = dateDestination;
                    
                    // Mettre à jour EstPrevisionnel et EstValide selon la nouvelle date
                    cra.EstPrevisionnel = dateDestination >= aujourdhui;
                    cra.EstValide = dateDestination < aujourdhui;
                    
                    _craService.SaveCRA(cra);
                    
                    // Rafraîchir l'affichage
                    ChargerCalendrier();
                    
                    // Si le jour sélectionné est l'origine ou la destination, recharger les CRA affichés
                    if (JourSelectionne != null && 
                        (JourSelectionne.Date.Date == dateOrigine.Date || JourSelectionne.Date.Date == dateDestination.Date))
                    {
                        ChargerCRAsJour();
                    }
                    
                    System.Windows.MessageBox.Show(
                        $"✓ CRA repositionné avec succès au {dateDestination:dd/MM/yyyy}",
                        "Succès",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(
                        $"Erreur lors du repositionnement : {ex.Message}",
                        "Erreur",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// Valide tous les CRA d'une journée
        /// </summary>
        private void ValiderJournee(JourCalendrierViewModel jour)
        {
            if (jour == null || DevSelectionne == null) return;

            // Vérifier les permissions
            if (!_permissionService.PeutValiderCRA)
            {
                System.Windows.MessageBox.Show(
                    "Vous n'avez pas les droits pour valider des CRA.\n\n" +
                    "Seuls les Administrateurs et Chefs de Projet peuvent valider les CRA.",
                    "Permission refusée",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

            var result = System.Windows.MessageBox.Show(
                $"Valider tous les CRA du {jour.Date:dd/MM/yyyy} ?\n\n" +
                $"Cela confirmera que les {jour.NombreCRAsAValider} CRA prévisionnel(s) correspondent à la réalité.\n" +
                $"Les CRA validés compteront dans le temps réel des tâches.",
                "Validation CRA",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                try
                {
                    _craService.ValiderJournee(DevSelectionne.Id, jour.Date);
                    ChargerCalendrier();
                    ChargerCRAsJour();

                    System.Windows.MessageBox.Show(
                        $"✅ {jour.NombreCRAsAValider} CRA(s) validé(s) avec succès !",
                        "Succès",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(
                        $"Erreur lors de la validation : {ex.Message}",
                        "Erreur",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// Alloue automatiquement le temps restant de la tâche sur les jours disponibles
        /// </summary>
        private void AllouerAutomatiquement()
        {
            if (JourSelectionne == null || TacheSelectionnee == null || DevSelectionne == null)
                return;

            var joursRestants = JoursRestants;
            if (joursRestants <= 0)
            {
                System.Windows.MessageBox.Show(
                    "Cette tâche n'a plus de temps restant à allouer.",
                    "Information",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
                return;
            }

            // Calculer la date de début (le jour sélectionné)
            var dateDebut = JourSelectionne.Date;
            var heuresParJour = 8.0; // 1 jour complet par défaut
            var nombreJoursNecessaires = (int)Math.Ceiling(joursRestants);

            // Utiliser la date de livraison attendue si disponible, sinon 3 mois
            var dateFin = TacheSelectionnee.DateFinAttendue ?? dateDebut.AddMonths(3);
            
            // Si la date de fin est avant la date de début, étendre la recherche
            if (dateFin < dateDebut)
            {
                dateFin = dateDebut.AddMonths(3);
            }

            // Trouver les jours disponibles
            var joursDisponibles = TrouverJoursDisponibles(
                dateDebut, 
                dateFin, 
                DevSelectionne.Id, 
                heuresParJour, 
                nombreJoursNecessaires);

            if (joursDisponibles.Count == 0)
            {
                System.Windows.MessageBox.Show(
                    "Aucun jour disponible trouvé dans les 3 prochains mois.\n\n" +
                    "Tous les jours sont soit:\n" +
                    "- Week-ends ou jours fériés\n" +
                    "- Déjà chargés à 100% (1j = 8h max/jour)",
                    "Allocation impossible",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

            // Calculer la répartition intelligente
            var heuresRestantes = joursRestants * 8.0;
            var joursAUtiliser = Math.Min(joursDisponibles.Count, nombreJoursNecessaires);
            
            // Séparer jours passés/présents et futurs
            var aujourdhui = DateTime.Now.Date;
            var joursPassesEtPresent = joursDisponibles.Where(j => j <= aujourdhui).Take(joursAUtiliser).ToList();
            var joursFuturs = joursDisponibles.Where(j => j > aujourdhui).Take(Math.Max(0, joursAUtiliser - joursPassesEtPresent.Count)).ToList();
            var tousLesJours = joursPassesEtPresent.Concat(joursFuturs).Take(joursAUtiliser).ToList();

            // Préparer le message de confirmation
            var premierJour = tousLesJours.First();
            var dernierJour = tousLesJours.Last();
            var totalHeures = Math.Min(heuresRestantes, tousLesJours.Count * 8.0);
            var totalJours = totalHeures / 8.0;

            string message = $"🤖 ALLOCATION AUTOMATIQUE\n\n";
            message += $"📋 Tâche : {TacheSelectionnee.Titre}\n";
            message += $"⏱️ Temps restant : {joursRestants:F1} jour(s)\n";
            if (TacheSelectionnee.DateFinAttendue.HasValue)
            {
                message += $"🎯 Livraison cible : {TacheSelectionnee.DateFinAttendue.Value:dd/MM/yyyy}\n";
            }
            message += $"📅 Période planifiée : {premierJour:dd/MM/yyyy} → {dernierJour:dd/MM/yyyy}\n";
            message += $"📊 Distribution : {totalJours:F1}j sur {tousLesJours.Count} jour(s) ouvré(s)\n\n";

            if (joursPassesEtPresent.Count > 0)
            {
                message += $"✅ {joursPassesEtPresent.Count} jour(s) comptabilisés (passé/présent)\n";
            }
            if (joursFuturs.Count > 0)
            {
                message += $"📆 {joursFuturs.Count} jour(s) en prévisionnel (futur)\n";
            }

            message += $"\n💡 Le système a trouvé les premiers jours disponibles\nen sautant les week-ends, jours fériés et jours pleins.\n\n";
            message += "Voulez-vous créer ces CRA automatiquement ?";

            var resultat = System.Windows.MessageBox.Show(
                message,
                "Confirmer l'allocation automatique",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            if (resultat != System.Windows.MessageBoxResult.Yes)
                return;

            // Créer les CRA automatiquement
            try
            {
                int nombreCRAsCrees = 0;
                double heuresAllouees = 0;

                foreach (var jour in tousLesJours)
                {
                    // Calculer les heures à allouer ce jour
                    double heuresAAllouer = Math.Min(8.0, heuresRestantes - heuresAllouees);
                    
                    if (heuresAAllouer <= 0)
                        break;

                    var cra = new CRA
                    {
                        DevId = DevSelectionne.Id,
                        BacklogItemId = TacheSelectionnee.Id,
                        Date = jour,
                        HeuresTravaillees = heuresAAllouer,
                        Commentaire = "Allocation automatique",
                        DateCreation = DateTime.Now,
                        EstPrevisionnel = true, // Tous les CRA sont prévisionnels à la création
                        EstValide = false // À valider manuellement
                    };

                    _craService.SaveCRA(cra);
                    nombreCRAsCrees++;
                    heuresAllouees += heuresAAllouer;
                }

                // Réinitialiser le formulaire
                TacheSelectionnee = null;
                JoursASaisir = 0;
                Commentaire = string.Empty;

                ChargerCalendrier();
                ChargerCRAsJour();

                string messageSucces = $"✅ {nombreCRAsCrees} CRA(s) créé(s) automatiquement !\n\n";
                messageSucces += $"⏱️ Total alloué : {heuresAllouees / 8.0:F1} jour(s)\n";
                if (joursPassesEtPresent.Count > 0)
                {
                    messageSucces += $"📊 {joursPassesEtPresent.Count} jour(s) comptabilisés\n";
                }
                if (joursFuturs.Count > 0)
                {
                    messageSucces += $"📆 {joursFuturs.Count} jour(s) en prévisionnel";
                }

                System.Windows.MessageBox.Show(
                    messageSucces,
                    "Allocation réussie",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Erreur lors de l'allocation automatique : {ex.Message}",
                    "Erreur",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }

        private string GetIconeJourFerie(string nomJourFerie)
        {
            if (string.IsNullOrEmpty(nomJourFerie)) return null;

            // Icône unique pour tous les jours fériés
            return "/Images/jour-ferie.png";
        }

        /// <summary>
        /// Valide tous les CRA non validés du développeur sélectionné
        /// </summary>
        public int ValiderTousLesCRADuDev()
        {
            if (DevSelectionne == null) return 0;

            int nombreValidations = 0;

            // Récupérer tous les CRA non validés du développeur sélectionné
            var tousLesCRA = _craService.GetAllCRAs();
            var craAValider = tousLesCRA.Where(c => 
                !c.EstValide && 
                c.DevId == DevSelectionne.Id).ToList();

            foreach (var cra in craAValider)
            {
                _craService.ValiderCRA(cra.Id);
                nombreValidations++;
            }

            // Rafraîchir l'affichage du calendrier
            ChargerCalendrier();

            return nombreValidations;
        }

        /// <summary>
        /// Valide uniquement les CRA "à valider" (orange) du développeur sélectionné
        /// Ce sont les CRA prévisionnels avant la date du jour et non validés
        /// </summary>
        public int ValiderCRAAValiderDuDev()
        {
            if (DevSelectionne == null) return 0;

            int nombreValidations = 0;

            // Récupérer tous les CRA du développeur sélectionné
            var tousLesCRA = _craService.GetAllCRAs();
            var craAValider = tousLesCRA.Where(c => 
                c.DevId == DevSelectionne.Id && 
                c.EstAValider).ToList(); // EstAValider = prévisionnels avant aujourd'hui et non validés

            foreach (var cra in craAValider)
            {
                _craService.ValiderCRA(cra.Id);
                nombreValidations++;
            }

            // Rafraîchir l'affichage du calendrier
            ChargerCalendrier();

            return nombreValidations;
        }

        /// <summary>
        /// Expose GetAllCRAs pour les rapports
        /// </summary>
        public List<CRA> GetAllCRAs()
        {
            return _craService.GetAllCRAs();
        }

        /// <summary>
        /// Génère un rapport sur le respect des dates de fin des tâches
        /// Retourne des listes structurées pour affichage dans une fenêtre dédiée
        /// </summary>
        public (List<(string Nom, string Detail)> TachesRetard, List<(string Nom, string Detail)> TachesTemps) GenererRapportRespectDates(List<CRA> crasValides)
        {
            var tachesRetard = new List<(string Nom, string Detail)>();
            var tachesTemps = new List<(string Nom, string Detail)>();

            if (crasValides == null || !crasValides.Any()) 
                return (tachesRetard, tachesTemps);

            // Grouper les CRA par tâche
            var craParTache = crasValides.GroupBy(c => c.BacklogItemId);

            foreach (var groupe in craParTache)
            {
                var tache = _backlogService.GetBacklogItemById(groupe.Key);
                if (tache == null || !tache.DateFinAttendue.HasValue) continue;

                var dernierCRA = groupe.OrderByDescending(c => c.Date).First();
                var dateFin = tache.DateFinAttendue.Value;
                var dateFinTravail = dernierCRA.Date;

                if (dateFinTravail > dateFin)
                {
                    var ecart = (dateFinTravail - dateFin).Days;
                    tachesRetard.Add((
                        tache.Titre,
                        $"Écart: {ecart} jour{(ecart > 1 ? "s" : "")} • Attendu: {dateFin:dd/MM/yyyy} • Terminé: {dateFinTravail:dd/MM/yyyy}"
                    ));
                }
                else
                {
                    tachesTemps.Add((
                        tache.Titre,
                        $"Terminé le: {dateFinTravail:dd/MM/yyyy} (Attendu: {dateFin:dd/MM/yyyy})"
                    ));
                }
            }

            return (tachesRetard, tachesTemps);
        }

        /// <summary>
        /// Annule la validation de tous les CRA validés du développeur sélectionné
        /// </summary>
        public int AnnulerValidationCRADuDev()
        {
            if (DevSelectionne == null) return 0;

            int nombreAnnulations = 0;

            // Récupérer tous les CRA validés du développeur sélectionné
            var tousLesCRA = _craService.GetAllCRAs();
            var craValides = tousLesCRA.Where(c => 
                c.EstValide && 
                c.DevId == DevSelectionne.Id).ToList();

            foreach (var cra in craValides)
            {
                // Remettre le CRA en mode non validé
                cra.EstValide = false;
                // IMPORTANT: Remettre EstPrevisionnel à true pour que les CRAs passés redeviennent orange (EstAValider)
                cra.EstPrevisionnel = true;
                _craService.SaveCRA(cra);
                nombreAnnulations++;
            }

            // Rafraîchir l'affichage du calendrier
            ChargerCalendrier();

            return nombreAnnulations;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

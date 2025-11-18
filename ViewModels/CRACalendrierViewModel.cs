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

        public ObservableCollection<JourCalendrierViewModel> JoursCalendrier { get; set; }
        public ObservableCollection<Utilisateur> Devs { get; set; }
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
                ChargerTachesDisponibles();
                ChargerCalendrier();
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
                                               DevSelectionne != null;

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
            SetJoursRapideCommand = new RelayCommand(param => JoursASaisir = double.Parse(param.ToString(), System.Globalization.CultureInfo.InvariantCulture));
            JourSelectionnCommand = new RelayCommand(param => JourSelectionne = (JourCalendrierViewModel)param);
            AllocationAutomatiqueCommand = new RelayCommand(_ => AllouerAutomatiquement(), _ => AfficherAllocationAuto);
            ValiderJourneeCommand = new RelayCommand(param => ValiderJournee((JourCalendrierViewModel)param));
            ValiderJourneeCommand = new RelayCommand(param => ValiderJournee((JourCalendrierViewModel)param));

            ChargerDevs();
            ChargerTachesDisponibles();
            ChargerCalendrier();
        }

        private void ChargerDevs()
        {
            Devs.Clear();
            
            // Si l'utilisateur est admin, montrer tous les devs, sinon seulement lui-même
            if (_permissionService.EstAdministrateur)
            {
                var users = _backlogService.GetAllUtilisateurs();
                foreach (var user in users)
                {
                    Devs.Add(user);
                }
            }
            else
            {
                // Pour un dev normal, ajouter uniquement lui-même
                Devs.Add(_authService.CurrentUser);
            }

            // Sélectionner l'utilisateur connecté par défaut
            DevSelectionne = Devs.FirstOrDefault(d => d.Id == _authService.CurrentUser.Id);
            OnPropertyChanged(nameof(AfficherSelecteurDev));
        }

        private void ChargerTachesDisponibles()
        {
            TachesDisponibles.Clear();
            
            if (DevSelectionne == null) return;

            // Pour la saisie CRA, seulement les tâches non-archivées
            var taches = _backlogService.GetAllBacklogItems();

            // Séparer les tâches normales et spéciales
            var tachesSpeciales = taches.Where(t => 
                t.TypeDemande == TypeDemande.Conges || 
                t.TypeDemande == TypeDemande.NonTravaille || 
                t.TypeDemande == TypeDemande.Support || 
                t.TypeDemande == TypeDemande.Run).ToList();

            if (_afficherToutesLesTaches)
            {
                // Toutes les tâches de "À faire" à "En test" (pas terminées) + tâches spéciales
                var tachesNormales = taches.Where(t => t.Statut >= Statut.Afaire && t.Statut < Statut.Termine).ToList();
                taches = tachesNormales.Concat(tachesSpeciales).ToList();
            }
            else
            {
                // Tâches assignées au dev (en cours/test) + ses tâches spéciales
                var tachesNormales = taches.Where(t => t.DevAssigneId == DevSelectionne.Id && 
                                          (t.Statut == Statut.EnCours || t.Statut == Statut.Test)).ToList();
                var mesTachesSpeciales = tachesSpeciales.Where(t => t.DevAssigneId == DevSelectionne.Id).ToList();
                taches = tachesNormales.Concat(mesTachesSpeciales).ToList();
            }

            foreach (var tache in taches.OrderByDescending(t => t.Priorite).ThenBy(t => t.Titre))
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

            // Toujours saisir en mode journalier (la saisie sur période a été remplacée par l'allocation auto)
            SaisirCRAJournalier();
        }

        private void SaisirCRAJournalier()
        {
            try
            {
                // Vérifier qu'il reste du temps à allouer pour cette tâche
                if (TacheSelectionnee.ChiffrageJours.HasValue && JoursRestants <= 0)
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

                // Vérifier que la saisie ne dépasse pas le temps restant
                if (TacheSelectionnee.ChiffrageJours.HasValue && JoursASaisir > JoursRestants)
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

                // Convertir jours en heures (1j = 8h)
                double heures = JoursASaisir * 8.0;

                // Vérifier la charge maximale journalière
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
                    EstPrevisionnel = JourSelectionne.Date >= DateTime.Now.Date, // Prévisionnel si aujourd'hui ou futur
                    EstValide = JourSelectionne.Date < DateTime.Now.Date // Validé automatiquement si dans le passé
                };

                _craService.SaveCRA(cra);

                // Réinitialiser le formulaire
                JoursASaisir = 0;
                Commentaire = "";
                TacheSelectionnee = null;

                // Rafraîchir l'affichage
                ChargerCalendrier();
                ChargerCRAsJour();

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

            // Calculer le nombre de jours à saisir
            var heuresParJour = JoursASaisir * 8.0;
            int joursADistribuer = (int)Math.Ceiling(JoursASaisir);

            // Trouver les jours disponibles avec décalage automatique
            var joursDisponibles = TrouverJoursDisponibles(dateDebut, dateFin, DevSelectionne.Id, heuresParJour, joursADistribuer);

            if (joursDisponibles.Count == 0)
            {
                System.Windows.MessageBox.Show(
                    "Aucun jour disponible trouvé sur cette période.\n\n" +
                    "Tous les jours sont soit:\n" +
                    "- Week-ends ou jours fériés\n" +
                    "- Déjà chargés à 100% (1j = 8h max/jour)",
                    "Validation",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

            // Séparer jours passés et futurs
            var aujourdhui = DateTime.Now.Date;
            var joursPassesEtPresent = joursDisponibles.Where(j => j <= aujourdhui).ToList();
            var joursFuturs = joursDisponibles.Where(j => j > aujourdhui).ToList();

            // Message de confirmation avec détails
            var totalJours = joursDisponibles.Count;
            var totalHeures = heuresParJour * totalJours;
            var premierJour = joursDisponibles.First();
            var dernierJour = joursDisponibles.Last();

            string message = $"💾 Saisie CRA sur {totalJours} jour(s) disponible(s)\n\n";
            message += $"📅 Période effective : {premierJour:dd/MM/yyyy} → {dernierJour:dd/MM/yyyy}\n";
            message += $"⏱️ Charge : {JoursASaisir:F1}j ({heuresParJour:F1}h) par jour\n";
            message += $"📊 Total : {totalJours * JoursASaisir:F1}j ({totalHeures:F1}h)\n\n";

            if (joursPassesEtPresent.Count > 0)
            {
                message += $"✅ Jours passés/actuels : {joursPassesEtPresent.Count} jour(s)\n";
                message += "   → Comptés immédiatement dans l'avancement\n\n";
            }

            if (joursFuturs.Count > 0)
            {
                message += $"📆 Jours futurs (prévisionnel) : {joursFuturs.Count} jour(s)\n";
                message += "   → Ne seront PAS comptés dans l'avancement actuel\n";
                message += "   → S'ajouteront automatiquement au fur et à mesure\n\n";
            }

            if (dernierJour > dateFin)
            {
                message += $"⚠️ Décalage appliqué jusqu'au {dernierJour:dd/MM/yyyy}\n";
                message += $"   (certains jours entre {dateDebut:dd/MM/yyyy} et {dateFin:dd/MM/yyyy} n'étaient pas disponibles)\n\n";
            }

            message += "Continuer ?";

            var result = System.Windows.MessageBox.Show(
                message,
                "Confirmation saisie CRA",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            if (result != System.Windows.MessageBoxResult.Yes)
                return;

            try
            {
                int nombreCRAsCrees = 0;

                foreach (var jour in joursDisponibles)
                {
                    var cra = new CRA
                    {
                        DevId = DevSelectionne.Id,
                        BacklogItemId = TacheSelectionnee.Id,
                        Date = jour,
                        HeuresTravaillees = heuresParJour,
                        Commentaire = Commentaire,
                        DateCreation = DateTime.Now,
                        EstPrevisionnel = jour > aujourdhui // Marquer comme prévisionnel si futur
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

                string messageSucces = $"✅ {nombreCRAsCrees} CRA(s) enregistré(s) !\n\n";
                if (joursPassesEtPresent.Count > 0)
                {
                    messageSucces += $"📊 {joursPassesEtPresent.Count} jour(s) comptés dans l'avancement\n";
                }
                if (joursFuturs.Count > 0)
                {
                    messageSucces += $"📆 {joursFuturs.Count} jour(s) en prévisionnel (ajoutés au fur et à mesure)";
                }

                System.Windows.MessageBox.Show(
                    messageSucces,
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
                    _craService.DeleteCRA(craVM.CRA.Id, _authService.CurrentUser.Id, _permissionService.EstAdministrateur);
                    ChargerCalendrier();
                    ChargerCRAsJour();

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
            }
        }

        /// <summary>
        /// Valide tous les CRA d'une journée
        /// </summary>
        private void ValiderJournee(JourCalendrierViewModel jour)
        {
            if (jour == null || DevSelectionne == null) return;

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
                        EstPrevisionnel = jour >= aujourdhui, // Prévisionnel si aujourd'hui ou futur
                        EstValide = jour < aujourdhui // Validé automatiquement si dans le passé
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

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

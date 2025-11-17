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

    public class JourCalendrierViewModel : INotifyPropertyChanged
    {
        public DateTime Date { get; set; }
        public int Jour { get; set; }
        public bool EstDansMois { get; set; }
        public bool EstAujourdhui { get; set; }
        public bool EstWeekend { get; set; }
        public bool EstJourFerie { get; set; }
        public string NomJourFerie { get; set; }
        public double TotalHeuresSaisies { get; set; }
        public bool ADesCRAs => TotalHeuresSaisies > 0;
        
        public string TotalJoursAffiche => TotalHeuresSaisies > 0 ? $"{TotalHeuresSaisies / 8.0:F1}j" : "";

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

        public ICommand MoisPrecedentCommand { get; }
        public ICommand MoisSuivantCommand { get; }
        public ICommand AujourdhuiCommand { get; }
        public ICommand SaisirCRACommand { get; }
        public ICommand SupprimerCRACommand { get; }
        public ICommand SetJoursRapideCommand { get; }
        public ICommand JourSelectionnCommand { get; }

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
            SetJoursRapideCommand = new RelayCommand(param => JoursASaisir = double.Parse(param.ToString()));
            JourSelectionnCommand = new RelayCommand(param => JourSelectionne = (JourCalendrierViewModel)param);

            ChargerDevs();
            ChargerTachesDisponibles();
            ChargerCalendrier();
        }

        private void ChargerDevs()
        {
            Devs.Clear();
            var users = _backlogService.GetAllUtilisateurs();
            
            foreach (var user in users)
            {
                Devs.Add(user);
            }

            // Sélectionner l'utilisateur connecté par défaut
            DevSelectionne = Devs.FirstOrDefault(d => d.Id == _authService.CurrentUser.Id);
        }

        private void ChargerTachesDisponibles()
        {
            TachesDisponibles.Clear();
            
            if (DevSelectionne == null) return;

            var taches = _backlogService.GetAllBacklogItems();

            if (_afficherToutesLesTaches)
            {
                // Toutes les tâches de "À faire" à "En test" (pas terminées)
                taches = taches.Where(t => t.Statut >= Statut.Afaire && t.Statut < Statut.Termine).ToList();
            }
            else
            {
                // Seulement les tâches assignées au dev et en cours/test
                taches = taches.Where(t => t.DevAssigneId == DevSelectionne.Id && 
                                          (t.Statut == Statut.EnCours || t.Statut == Statut.Test)).ToList();
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

            // Ajouter les jours du mois précédent pour compléter la première semaine
            var jourDebut = premierJour.AddDays(-premierJourSemaine);
            
            // Générer 42 jours (6 semaines) pour avoir une grille complète
            for (int i = 0; i < 42; i++)
            {
                var date = jourDebut.AddDays(i);
                var totalHeures = cras.Where(c => c.Date.Date == date.Date).Sum(c => c.HeuresTravaillees);

                var jourVM = new JourCalendrierViewModel
                {
                    Date = date,
                    Jour = date.Day,
                    EstDansMois = date.Month == MoisCourant.Month,
                    EstAujourdhui = date.Date == DateTime.Now.Date,
                    EstWeekend = JoursFeriesService.EstWeekend(date),
                    EstJourFerie = JoursFeriesService.EstJourFerie(date),
                    NomJourFerie = JoursFeriesService.GetNomJourFerie(date),
                    TotalHeuresSaisies = totalHeures
                };

                JoursCalendrier.Add(jourVM);
            }
        }

        private void ChargerCRAsJour()
        {
            CRAsJourSelectionne.Clear();

            if (JourSelectionne == null || DevSelectionne == null) return;

            var cras = _craService.GetCRAsByDev(DevSelectionne.Id, JourSelectionne.Date, JourSelectionne.Date);
            var taches = _backlogService.GetAllBacklogItems();
            
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

            if (SaisirSurPeriode)
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
                    DateCreation = DateTime.Now
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

            if (dateFin > DateTime.Now.Date)
            {
                var resultFutur = System.Windows.MessageBox.Show(
                    "Certaines dates de la période sont dans le futur.\n\nVoulez-vous quand même continuer ?",
                    "Dates futures",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Question);
                
                if (resultFutur != System.Windows.MessageBoxResult.Yes)
                    return;
            }

            // Compter les jours ouvrés
            var joursOuvres = JoursFeriesService.GetJoursOuvres(dateDebut, dateFin);

            if (joursOuvres.Count == 0)
            {
                System.Windows.MessageBox.Show(
                    "Aucun jour ouvré trouvé sur cette période (uniquement des week-ends et jours fériés).",
                    "Validation",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

            // Confirmation
            var heuresParJour = JoursASaisir * 8.0;
            var totalJours = JoursASaisir * joursOuvres.Count;
            var totalHeures = heuresParJour * joursOuvres.Count;

            var result = System.Windows.MessageBox.Show(
                $"Voulez-vous saisir {JoursASaisir:F1}j ({heuresParJour:F1}h) par jour ouvré sur {joursOuvres.Count} jour(s) ?\\n\\n" +
                $"Période : {dateDebut:dd/MM/yyyy} → {dateFin:dd/MM/yyyy}\\n" +
                $"Total : {totalJours:F1}j ({totalHeures:F1}h)",
                "Confirmation",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            if (result != System.Windows.MessageBoxResult.Yes)
                return;

            try
            {
                int nombreCRAsCrees = 0;

                foreach (var jour in joursOuvres)
                {
                    // Vérification charge quotidienne
                    var chargeJour = _craService.GetChargeParJour(DevSelectionne.Id, jour);

                    if (chargeJour + heuresParJour > 24)
                    {
                        var resultDepasse = System.Windows.MessageBox.Show(
                            $"Le {jour:dd/MM/yyyy} dépasserait 24h ({chargeJour:F1}h déjà saisi).\\n\\nIgnorer ce jour et continuer ?",
                            "Dépassement",
                            System.Windows.MessageBoxButton.YesNo,
                            System.Windows.MessageBoxImage.Warning);

                        if (resultDepasse == System.Windows.MessageBoxResult.Yes)
                            continue;
                        else
                            break;
                    }

                    var cra = new CRA
                    {
                        DevId = DevSelectionne.Id,
                        BacklogItemId = TacheSelectionnee.Id,
                        Date = jour,
                        HeuresTravaillees = heuresParJour,
                        Commentaire = Commentaire,
                        DateCreation = DateTime.Now
                    };

                    _craService.SaveCRA(cra);
                    nombreCRAsCrees++;
                }

                // Réinitialiser le formulaire
                TacheSelectionnee = null;
                JoursASaisir = 0;
                Commentaire = string.Empty;
                SaisirSurPeriode = false;
                
                ChargerCalendrier();
                ChargerCRAsJour();

                System.Windows.MessageBox.Show(
                    $"{nombreCRAsCrees} CRA(s) enregistré(s) avec succès !",
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

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

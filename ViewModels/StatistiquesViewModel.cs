using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Media;
using BacklogManager.Domain;
using BacklogManager.Services;
using BacklogManager.Shared;

namespace BacklogManager.ViewModels
{
    public class StatistiquesViewModel : INotifyPropertyChanged
    {
        private readonly BacklogService _backlogService;
        private readonly CRAService _craService;
        private readonly IDatabase _database;

        public event PropertyChangedEventHandler PropertyChanged;

        // Propriétés principales
        private int _totalTaches;
        public int TotalTaches
        {
            get => _totalTaches;
            set { _totalTaches = value; OnPropertyChanged(nameof(TotalTaches)); }
        }

        private int _tachesTerminees;
        public int TachesTerminees
        {
            get => _tachesTerminees;
            set { _tachesTerminees = value; OnPropertyChanged(nameof(TachesTerminees)); }
        }

        private string _pourcentageTerminees;
        public string PourcentageTerminees
        {
            get => _pourcentageTerminees;
            set { _pourcentageTerminees = value; OnPropertyChanged(nameof(PourcentageTerminees)); }
        }

        private int _tachesEnCours;
        public int TachesEnCours
        {
            get => _tachesEnCours;
            set { _tachesEnCours = value; OnPropertyChanged(nameof(TachesEnCours)); }
        }

        private int _projetsActifs;
        public int ProjetsActifs
        {
            get => _projetsActifs;
            set { _projetsActifs = value; OnPropertyChanged(nameof(ProjetsActifs)); }
        }

        // Statistiques CRA
        private string _totalHeuresCRA;
        public string TotalHeuresCRA
        {
            get => _totalHeuresCRA;
            set { _totalHeuresCRA = value; OnPropertyChanged(nameof(TotalHeuresCRA)); }
        }

        private string _totalJoursCRA;
        public string TotalJoursCRA
        {
            get => _totalJoursCRA;
            set { _totalJoursCRA = value; OnPropertyChanged(nameof(TotalJoursCRA)); }
        }

        private int _joursSaisisMoisCourant;
        public int JoursSaisisMoisCourant
        {
            get => _joursSaisisMoisCourant;
            set { _joursSaisisMoisCourant = value; OnPropertyChanged(nameof(JoursSaisisMoisCourant)); }
        }

        private string _tauxCompletionMois;
        public string TauxCompletionMois
        {
            get => _tauxCompletionMois;
            set { _tauxCompletionMois = value; OnPropertyChanged(nameof(TauxCompletionMois)); }
        }

        private int _devsActifs;
        public int DevsActifs
        {
            get => _devsActifs;
            set { _devsActifs = value; OnPropertyChanged(nameof(DevsActifs)); }
        }

        private string _tauxRealisation;
        public string TauxRealisation
        {
            get => _tauxRealisation;
            set { _tauxRealisation = value; OnPropertyChanged(nameof(TauxRealisation)); }
        }

        // Collections
        public ObservableCollection<StatutStatsViewModel> TachesParStatut { get; set; }
        public ObservableCollection<ChargeDevViewModel> ChargeParDev { get; set; }
        public ObservableCollection<CRADevViewModel> CRAParDev { get; set; }
        public ObservableCollection<CompletionProjetViewModel> CompletionParProjet { get; set; }

        // Filtres de période
        private int _periodeSelectionneeIndex = 5; // Tout afficher par défaut
        public int PeriodeSelectionneeIndex
        {
            get => _periodeSelectionneeIndex;
            set { _periodeSelectionneeIndex = value; OnPropertyChanged(nameof(PeriodeSelectionneeIndex)); }
        }

        private DateTime? _dateDebutFiltre;
        public DateTime? DateDebutFiltre
        {
            get => _dateDebutFiltre;
            set { _dateDebutFiltre = value; OnPropertyChanged(nameof(DateDebutFiltre)); }
        }

        private DateTime? _dateFinFiltre;
        public DateTime? DateFinFiltre
        {
            get => _dateFinFiltre;
            set { _dateFinFiltre = value; OnPropertyChanged(nameof(DateFinFiltre)); }
        }

        // Commandes
        public RelayCommand RafraichirCommand { get; set; }
        public RelayCommand AfficherDetailsDevCommand { get; set; }
        public RelayCommand AfficherDetailsCRADevCommand { get; set; }
        public RelayCommand ExporterPDFCommand { get; set; }

        public StatistiquesViewModel(BacklogService backlogService, CRAService craService, IDatabase database)
        {
            _backlogService = backlogService;
            _craService = craService;
            _database = database;

            TachesParStatut = new ObservableCollection<StatutStatsViewModel>();
            ChargeParDev = new ObservableCollection<ChargeDevViewModel>();
            CRAParDev = new ObservableCollection<CRADevViewModel>();
            CompletionParProjet = new ObservableCollection<CompletionProjetViewModel>();

            RafraichirCommand = new RelayCommand(param => ChargerStatistiques());
            AfficherDetailsDevCommand = new RelayCommand(param => AfficherDetailsDev(param as ChargeDevViewModel));
            AfficherDetailsCRADevCommand = new RelayCommand(param => AfficherDetailsCRADev(param as CRADevViewModel));
            ExporterPDFCommand = new RelayCommand(param => ExporterEnPDF());

            ChargerStatistiques();
        }

        public void AppliquerFiltrePeriode(int periodeIndex)
        {
            DateTime now = DateTime.Now;
            
            switch (periodeIndex)
            {
                case 0: // Année en cours
                    DateDebutFiltre = new DateTime(now.Year, 1, 1);
                    DateFinFiltre = new DateTime(now.Year, 12, 31, 23, 59, 59);
                    break;
                case 1: // 6 derniers mois
                    DateDebutFiltre = now.AddMonths(-6);
                    DateFinFiltre = now;
                    break;
                case 2: // Mois en cours
                    DateDebutFiltre = new DateTime(now.Year, now.Month, 1);
                    DateFinFiltre = new DateTime(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month), 23, 59, 59);
                    break;
                case 3: // 3 derniers mois
                    DateDebutFiltre = now.AddMonths(-3);
                    DateFinFiltre = now;
                    break;
                case 4: // Période personnalisée (dates saisies par l'utilisateur)
                    // Les dates sont déjà définies via binding, ne rien faire
                    break;
                case 5: // Tout afficher
                default:
                    DateDebutFiltre = null;
                    DateFinFiltre = null;
                    break;
            }

            ChargerStatistiques();
        }

        private void ChargerStatistiques()
        {
            try
            {
                // Charger toutes les tâches SAUF congés et non-travaillé
                var taches = _backlogService.GetAllBacklogItems()
                    .Where(t => t.TypeDemande != TypeDemande.Conges && t.TypeDemande != TypeDemande.NonTravaille)
                    .ToList();
                var projets = _backlogService.GetAllProjets();
                var devs = _backlogService.GetAllDevs();

                // Appliquer le filtre de période sur les tâches
                if (DateDebutFiltre.HasValue && DateFinFiltre.HasValue)
                {
                    taches = taches.Where(t => t.DateCreation >= DateDebutFiltre.Value && 
                                               t.DateCreation <= DateFinFiltre.Value).ToList();
                }

                // Cartes rapides - Tâches
                // Compter les tâches archivées comme terminées (SAUF congés et non-travaillé)
                var tachesArchivees = _backlogService.GetAllBacklogItemsIncludingArchived()
                    .Count(t => t.EstArchive && t.TypeDemande != TypeDemande.Conges && t.TypeDemande != TypeDemande.NonTravaille);
                
                TotalTaches = taches.Count + tachesArchivees; // Total = actives + archivées
                TachesTerminees = taches.Count(t => t.Statut == Statut.Termine) + tachesArchivees; // Terminées = statut Terminé + archivées
                TachesEnCours = taches.Count(t => t.Statut == Statut.EnCours);
                ProjetsActifs = projets.Count(p => p.Actif);

                if (TotalTaches > 0)
                {
                    var pourcentage = (TachesTerminees * 100.0 / TotalTaches);
                    PourcentageTerminees = $"{pourcentage:F1}%";
                }
                else
                {
                    PourcentageTerminees = "0%";
                }

                // Cartes rapides - CRA
                ChargerStatistiquesCRA(devs);

                // Graphique: Tâches par statut
                ChargerTachesParStatut(taches);

                // Charge par développeur (tâches)
                ChargerChargeParDev(taches, devs);

                // CRA par développeur (heures)
                ChargerCRAParDev(devs);

                // Complétion par projet
                ChargerCompletionParProjet(taches, projets);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Erreur lors du chargement des statistiques: {ex.Message}",
                    "Erreur", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void ChargerStatistiquesCRA(List<Dev> devs)
        {
            try
            {
                const double HEURES_PAR_JOUR = 8.0;

                // Total de toutes les heures CRA
                var tousLesCRAs = _craService.GetAllCRAs();
                
                // Appliquer le filtre de période sur les CRA
                if (DateDebutFiltre.HasValue && DateFinFiltre.HasValue)
                {
                    tousLesCRAs = tousLesCRAs.Where(c => c.Date >= DateDebutFiltre.Value && 
                                                          c.Date <= DateFinFiltre.Value).ToList();
                }

                var totalHeures = tousLesCRAs.Sum(c => c.HeuresTravaillees);
                var totalJours = totalHeures / HEURES_PAR_JOUR;

                TotalHeuresCRA = totalHeures.ToString("F0");
                TotalJoursCRA = $"({totalJours:F1} jours)";

                // Jours saisis ce mois
                var maintenant = DateTime.Now;
                var crasMoisCourant = tousLesCRAs.Where(c => c.Date.Year == maintenant.Year && c.Date.Month == maintenant.Month).ToList();
                var joursSaisisUniques = crasMoisCourant.Select(c => c.Date.Date).Distinct().Count();
                JoursSaisisMoisCourant = joursSaisisUniques;

                // Calcul du taux de complétion du mois (nombre de jours ouvrés écoulés)
                var joursOuvresMois = CalculerJoursOuvresMois(maintenant.Year, maintenant.Month, maintenant.Day);
                if (joursOuvresMois > 0)
                {
                    var tauxCompMois = (joursSaisisUniques * 100.0 / joursOuvresMois);
                    TauxCompletionMois = $"{tauxCompMois:F0}% du mois";
                }
                else
                {
                    TauxCompletionMois = "0% du mois";
                }

                // Devs actifs (ayant saisi au moins un CRA ce mois)
                var devsActifsCeMois = crasMoisCourant.Select(c => c.DevId).Distinct().Count();
                DevsActifs = devsActifsCeMois;

                // Taux de réalisation (temps réel vs temps estimé sur les tâches terminées)
                ChargerTauxRealisation();
            }
            catch
            {
                TotalHeuresCRA = "0";
                TotalJoursCRA = "(0 jours)";
                JoursSaisisMoisCourant = 0;
                TauxCompletionMois = "0%";
                DevsActifs = 0;
                TauxRealisation = "N/A";
            }
        }

        private int CalculerJoursOuvresMois(int annee, int mois, int jourActuel)
        {
            int joursOuvres = 0;
            var joursFeries = JoursFeriesService.GetJoursFeries(annee);

            for (int jour = 1; jour <= jourActuel; jour++)
            {
                var date = new DateTime(annee, mois, jour);
                if (date.DayOfWeek != DayOfWeek.Saturday && 
                    date.DayOfWeek != DayOfWeek.Sunday && 
                    !joursFeries.Contains(date.Date))
                {
                    joursOuvres++;
                }
            }

            return joursOuvres;
        }

        private void ChargerTauxRealisation()
        {
            try
            {
                var taches = _backlogService.GetAllBacklogItems();
                
                // Appliquer le filtre de période sur les tâches
                if (DateDebutFiltre.HasValue && DateFinFiltre.HasValue)
                {
                    taches = taches.Where(t => t.DateCreation >= DateDebutFiltre.Value && 
                                               t.DateCreation <= DateFinFiltre.Value).ToList();
                }
                
                // Total estimé : somme du chiffrage de toutes les tâches (en heures)
                var tachesAvecEstimation = taches.Where(t =>
                    t.ChiffrageHeures.HasValue &&
                    t.ChiffrageHeures.Value > 0).ToList();

                // Total réel : somme de TOUS les CRA saisis (toutes tâches confondues)
                var tousLesCRAs = _craService.GetAllCRAs();
                
                // Appliquer le filtre de période sur les CRA
                if (DateDebutFiltre.HasValue && DateFinFiltre.HasValue)
                {
                    tousLesCRAs = tousLesCRAs.Where(c => c.Date >= DateDebutFiltre.Value && 
                                                          c.Date <= DateFinFiltre.Value).ToList();
                }
                
                var totalReelHeures = tousLesCRAs.Sum(c => c.HeuresTravaillees);

                // Debug
                var nbTaches = taches.Count;
                var nbTachesChiffrees = tachesAvecEstimation.Count;
                var nbCRAs = tousLesCRAs.Count;
                System.Diagnostics.Debug.WriteLine($"[STATS] Tâches totales: {nbTaches}, Tâches chiffrées: {nbTachesChiffrees}, CRAs: {nbCRAs}, Heures CRA: {totalReelHeures}");

                if (tachesAvecEstimation.Any() && totalReelHeures > 0)
                {
                    // Somme des heures estimées
                    var totalEstimeHeures = tachesAvecEstimation.Sum(t => t.ChiffrageHeures.Value);
                    
                    // Taux = temps réel / temps estimé * 100
                    // Si > 100% : on a pris plus de temps que prévu
                    // Si < 100% : on a été plus rapide que prévu
                    var taux = (totalReelHeures / totalEstimeHeures) * 100.0;
                    TauxRealisation = $"{taux:F0}%";
                    System.Diagnostics.Debug.WriteLine($"[STATS] Taux calculé: {taux:F2}% (Réel: {totalReelHeures}h / Estimé: {totalEstimeHeures}h)");
                }
                else if (!tachesAvecEstimation.Any() && totalReelHeures > 0)
                {
                    // CRA saisis mais pas de tâches chiffrées
                    TauxRealisation = "Chiffrer tâches";
                    System.Diagnostics.Debug.WriteLine("[STATS] N/A - CRA saisis mais pas de tâches chiffrées");
                }
                else if (tachesAvecEstimation.Any() && totalReelHeures == 0)
                {
                    // Tâches chiffrées mais pas de CRA
                    TauxRealisation = "Saisir CRA";
                    System.Diagnostics.Debug.WriteLine("[STATS] 0% - Tâches chiffrées mais pas de CRA");
                }
                else
                {
                    // Ni tâches chiffrées, ni CRA
                    TauxRealisation = "Aucune donnée";
                    System.Diagnostics.Debug.WriteLine("[STATS] N/A - Ni tâches chiffrées, ni CRA");
                }
            }
            catch (Exception ex)
            {
                TauxRealisation = $"Erreur";
                System.Diagnostics.Debug.WriteLine($"Erreur calcul taux: {ex.Message}");
            }
        }

        private void ChargerTachesParStatut(List<BacklogItem> taches)
        {
            TachesParStatut.Clear();

            var stats = new List<StatutStatsViewModel>
            {
                new StatutStatsViewModel
                {
                    Statut = "À faire",
                    Nombre = taches.Count(t => t.Statut == Statut.Afaire),
                    Couleur = new SolidColorBrush(Color.FromRgb(255, 152, 0))
                },
                new StatutStatsViewModel
                {
                    Statut = "En cours",
                    Nombre = taches.Count(t => t.Statut == Statut.EnCours),
                    Couleur = new SolidColorBrush(Color.FromRgb(33, 150, 243))
                },
                new StatutStatsViewModel
                {
                    Statut = "En test",
                    Nombre = taches.Count(t => t.Statut == Statut.Test),
                    Couleur = new SolidColorBrush(Color.FromRgb(156, 39, 176))
                },
                new StatutStatsViewModel
                {
                    Statut = "Terminé",
                    Nombre = taches.Count(t => t.Statut == Statut.Termine) + 
                             _backlogService.GetAllBacklogItemsIncludingArchived()
                                 .Count(t => t.EstArchive && t.TypeDemande != TypeDemande.Conges && t.TypeDemande != TypeDemande.NonTravaille),
                    Couleur = new SolidColorBrush(Color.FromRgb(76, 175, 80))
                }
            };

            var maxTaches = stats.Max(s => s.Nombre);
            if (maxTaches > 0)
            {
                foreach (var stat in stats)
                {
                    stat.LargeurBarre = (stat.Nombre * 600.0 / maxTaches);
                    if (TotalTaches > 0)
                    {
                        var pct = (stat.Nombre * 100.0 / TotalTaches);
                        stat.Pourcentage = $"{pct:F1}%";
                    }
                }
            }

            foreach (var stat in stats)
            {
                TachesParStatut.Add(stat);
            }
        }

        private void ChargerChargeParDev(List<BacklogItem> taches, List<Dev> devs)
        {
            ChargeParDev.Clear();

            foreach (var dev in devs)
            {
                var tachesDev = taches.Where(t => t.DevAssigneId == dev.Id).ToList();
                var tachesArchiveesDev = _backlogService.GetAllBacklogItemsIncludingArchived()
                    .Count(t => t.EstArchive && t.DevAssigneId == dev.Id && 
                                t.TypeDemande != TypeDemande.Conges && t.TypeDemande != TypeDemande.NonTravaille);
                var total = tachesDev.Count + tachesArchiveesDev;

                if (total > 0)
                {
                    ChargeParDev.Add(new ChargeDevViewModel
                    {
                        Dev = dev,
                        NomDev = dev.Nom,
                        AFaire = tachesDev.Count(t => t.Statut == Statut.Afaire),
                        EnCours = tachesDev.Count(t => t.Statut == Statut.EnCours),
                        Terminees = tachesDev.Count(t => t.Statut == Statut.Termine) + tachesArchiveesDev,
                        Total = total
                    });
                }
            }

            // Trier par charge décroissante
            var sortedList = ChargeParDev.OrderByDescending(c => c.Total).ToList();
            ChargeParDev.Clear();
            foreach (var item in sortedList)
            {
                ChargeParDev.Add(item);
            }
        }

        private void ChargerCRAParDev(List<Dev> devs)
        {
            CRAParDev.Clear();

            const double HEURES_PAR_JOUR = 8.0;
            var maintenant = DateTime.Now;
            var listTemp = new List<CRADevViewModel>();

            foreach (var dev in devs)
            {
                var crasDev = _craService.GetCRAsByDev(dev.Id);
                
                // Appliquer le filtre de période
                if (DateDebutFiltre.HasValue && DateFinFiltre.HasValue)
                {
                    crasDev = crasDev.Where(c => c.Date >= DateDebutFiltre.Value && 
                                                 c.Date <= DateFinFiltre.Value).ToList();
                }

                if (crasDev.Any())
                {
                    // Heures ce mois
                    var heuresMois = crasDev
                        .Where(c => c.Date.Year == maintenant.Year && c.Date.Month == maintenant.Month)
                        .Sum(c => c.HeuresTravaillees);

                    // Heures total
                    var heuresTotal = crasDev.Sum(c => c.HeuresTravaillees);
                    var joursTotal = heuresTotal / HEURES_PAR_JOUR;

                    listTemp.Add(new CRADevViewModel
                    {
                        DevId = dev.Id,
                        NomDev = dev.Nom,
                        HeuresMois = heuresMois.ToString("F0"),
                        HeuresTotal = heuresTotal.ToString("F0"),
                        JoursTotal = joursTotal.ToString("F1")
                    });
                }
            }

            // Calculer le max pour les barres de progression
            if (listTemp.Any())
            {
                var maxHeures = listTemp.Max(c => double.Parse(c.HeuresTotal));
                CRADevViewModel.SetMaxHeures(maxHeures);
            }

            // Trier par heures totales décroissantes
            foreach (var item in listTemp.OrderByDescending(c => double.Parse(c.HeuresTotal)))
            {
                CRAParDev.Add(item);
            }
        }

        private void ChargerCompletionParProjet(List<BacklogItem> taches, List<Projet> projets)
        {
            CompletionParProjet.Clear();

            foreach (var projet in projets.Where(p => p.Actif && p.Nom != "Tâches administratives"))
            {
                // Récupérer toutes les tâches du projet (y compris archivées) SAUF congés et non-travaillé
                var toutesLesTachesProjet = _backlogService.GetAllBacklogItemsIncludingArchived()
                    .Where(t => t.ProjetId == projet.Id && 
                                t.TypeDemande != TypeDemande.Conges && 
                                t.TypeDemande != TypeDemande.NonTravaille)
                    .ToList();
                
                var tachesActivesProjet = toutesLesTachesProjet.Where(t => !t.EstArchive).ToList();
                var tachesArchiveesProjet = toutesLesTachesProjet.Count(t => t.EstArchive);
                
                var totalProjet = toutesLesTachesProjet.Count; // Total = actives + archivées

                if (totalProjet > 0)
                {
                    // Terminées = statut Terminé (actives) + archivées
                    var termineesProjet = tachesActivesProjet.Count(t => t.Statut == Statut.Termine) + tachesArchiveesProjet;
                    var tauxCompletion = (termineesProjet * 100.0 / totalProjet);

                    // Calculer les heures CRA sur le projet
                    var heuresCRAProjet = CalculerHeuresCRAProjet(projet.Id);
                    var joursCRAProjet = heuresCRAProjet / 8.0;

                    CompletionProjetViewModel completion;
                    if (heuresCRAProjet > 0)
                    {
                        // Afficher en jours si >= 1j, sinon en heures
                        string affichageCRA;
                        if (joursCRAProjet >= 1)
                            affichageCRA = $"{joursCRAProjet:F1}j";
                        else
                            affichageCRA = $"{heuresCRAProjet:F1}h";

                        completion = new CompletionProjetViewModel
                        {
                            Projet = projet,
                            NomProjet = projet.Nom,
                            TotalTaches = totalProjet,
                            TachesTerminees = termineesProjet,
                            TauxCompletion = $"{tauxCompletion:F1}%",
                            HeuresCRA = affichageCRA
                        };
                    }
                    else
                    {
                        completion = new CompletionProjetViewModel
                        {
                            Projet = projet,
                            NomProjet = projet.Nom,
                            TotalTaches = totalProjet,
                            TachesTerminees = termineesProjet,
                            TauxCompletion = $"{tauxCompletion:F1}%",
                            HeuresCRA = "-"
                        };
                    }

                    CompletionParProjet.Add(completion);
                }
            }

            // Trier par nombre de tâches décroissant
            var sortedList = CompletionParProjet.OrderByDescending(c => c.TotalTaches).ToList();
            CompletionParProjet.Clear();
            foreach (var item in sortedList)
            {
                CompletionParProjet.Add(item);
            }
        }

        private double CalculerHeuresCRAProjet(int projetId)
        {
            try
            {
                // Récupérer toutes les tâches du projet SAUF congés et non-travaillé
                var tachesProjet = _backlogService.GetAllBacklogItems()
                    .Where(t => t.ProjetId == projetId && 
                                t.TypeDemande != TypeDemande.Conges && 
                                t.TypeDemande != TypeDemande.NonTravaille)
                    .Select(t => t.Id)
                    .ToList();

                if (!tachesProjet.Any())
                    return 0;

                // Récupérer tous les CRA pour ces tâches
                double totalHeures = 0;
                
                foreach (var tacheId in tachesProjet)
                {
                    var crasTache = _craService.GetCRAsByBacklogItem(tacheId);
                    
                    // Appliquer le filtre de période si défini
                    if (DateDebutFiltre.HasValue && DateFinFiltre.HasValue)
                    {
                        crasTache = crasTache.Where(c => c.Date >= DateDebutFiltre.Value && 
                                                         c.Date <= DateFinFiltre.Value).ToList();
                    }
                    
                    totalHeures += crasTache.Sum(c => c.HeuresTravaillees);
                }

                return totalHeures;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[STATS] Erreur calcul heures CRA projet {projetId}: {ex.Message}");
                return 0;
            }
        }

        private void AfficherDetailsDev(ChargeDevViewModel devStats)
        {
            if (devStats == null)
            {
                System.Windows.MessageBox.Show("devStats est null", "Debug", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            if (devStats.Dev == null)
            {
                System.Windows.MessageBox.Show($"devStats.Dev est null pour {devStats.NomDev}", "Debug", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            try
            {
                System.Diagnostics.Debug.WriteLine($"[STATS] Ouverture détails dev: {devStats.Dev.Nom} (ID: {devStats.Dev.Id})");
                var detailsWindow = new Views.DevDetailsWindow(devStats.Dev, _backlogService, _craService);
                detailsWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Erreur lors de l'ouverture des détails du développeur: {ex.Message}\n\nStackTrace:\n{ex.StackTrace}",
                    "Erreur", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void AfficherDetailsCRADev(CRADevViewModel craDevStats)
        {
            if (craDevStats == null) return;

            try
            {
                // Trouver le dev correspondant
                var devs = _backlogService.GetAllDevs();
                var dev = devs.FirstOrDefault(d => d.Id == craDevStats.DevId);
                
                if (dev != null)
                {
                    var detailsWindow = new Views.DevDetailsWindow(dev, _backlogService, _craService);
                    detailsWindow.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Erreur lors de l'ouverture des détails du développeur: {ex.Message}",
                    "Erreur", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void ExporterEnPDF()
        {
            try
            {
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Fichier HTML (*.html)|*.html",
                    FileName = $"Rapport_Statistiques_{DateTime.Now:yyyyMMdd_HHmmss}.html"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    GenererRapportHTML(saveFileDialog.FileName);
                    
                    var result = System.Windows.MessageBox.Show(
                        "Rapport HTML généré avec succès !\n\n" +
                        "Vous pouvez l'ouvrir dans votre navigateur et l'imprimer en PDF (Ctrl+P → Enregistrer en PDF).\n\n" +
                        "Voulez-vous ouvrir le fichier maintenant ?",
                        "Export réussi",
                        System.Windows.MessageBoxButton.YesNo,
                        System.Windows.MessageBoxImage.Information);
                    
                    if (result == System.Windows.MessageBoxResult.Yes)
                    {
                        System.Diagnostics.Process.Start(saveFileDialog.FileName);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Erreur lors de l'export :\n{ex.Message}",
                    "Erreur", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void GenererRapportHTML(string filePath)
        {
            string periodeTexte = "";
            if (DateDebutFiltre.HasValue && DateFinFiltre.HasValue)
            {
                periodeTexte = $" | Période : du {DateDebutFiltre.Value:dd/MM/yyyy} au {DateFinFiltre.Value:dd/MM/yyyy}";
            }

            var html = new System.Text.StringBuilder();
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html>");
            html.AppendLine("<head>");
            html.AppendLine("    <meta charset='utf-8'>");
            html.AppendLine("    <title>Rapport de Statistiques - BNP Paribas</title>");
            html.AppendLine("    <style>");
            html.AppendLine("        body { font-family: Arial, sans-serif; margin: 40px; background-color: #f5f5f5; }");
            html.AppendLine("        .header { background-color: #00915A; color: white; padding: 30px; text-align: center; margin-bottom: 30px; }");
            html.AppendLine("        .header h1 { margin: 0; font-size: 32px; }");
            html.AppendLine("        .header p { margin: 10px 0 0 0; font-size: 14px; opacity: 0.9; }");
            html.AppendLine("        .kpi-cards { display: flex; gap: 20px; margin-bottom: 30px; flex-wrap: wrap; }");
            html.AppendLine("        .kpi-card { flex: 1; min-width: 200px; background: white; padding: 20px; border-radius: 8px; box-shadow: 0 2px 8px rgba(0,0,0,0.1); }");
            html.AppendLine("        .kpi-card h3 { margin: 0 0 10px 0; color: #666; font-size: 14px; }");
            html.AppendLine("        .kpi-card .value { font-size: 32px; font-weight: bold; color: #00915A; }");
            html.AppendLine("        .section { background: white; padding: 25px; margin-bottom: 25px; border-radius: 8px; box-shadow: 0 2px 8px rgba(0,0,0,0.1); }");
            html.AppendLine("        .section h2 { margin: 0 0 20px 0; color: #00915A; border-bottom: 2px solid #00915A; padding-bottom: 10px; }");
            html.AppendLine("        table { width: 100%; border-collapse: collapse; margin-top: 15px; }");
            html.AppendLine("        th { background-color: #00915A; color: white; padding: 12px; text-align: left; font-weight: 600; }");
            html.AppendLine("        td { padding: 12px; border-bottom: 1px solid #e0e0e0; }");
            html.AppendLine("        tr:hover { background-color: #f5f5f5; }");
            html.AppendLine("        .footer { text-align: center; margin-top: 40px; padding-top: 20px; border-top: 2px solid #e0e0e0; color: #666; font-size: 12px; }");
            html.AppendLine("        @media print { body { background: white; } .kpi-card, .section { box-shadow: none; border: 1px solid #e0e0e0; } }");
            html.AppendLine("    </style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");
            
            // Header
            html.AppendLine("    <div class='header'>");
            html.AppendLine("        <h1>📊 RAPPORT DE STATISTIQUES</h1>");
            html.AppendLine($"        <p>BNP Paribas - Backlog Manager | Généré le {DateTime.Now:dd/MM/yyyy à HH:mm}{periodeTexte}</p>");
            html.AppendLine("    </div>");
            
            // KPI Cards
            html.AppendLine("    <div class='kpi-cards'>");
            html.AppendLine($"        <div class='kpi-card'><h3>📋 Total de tâches</h3><div class='value'>{TotalTaches}</div></div>");
            html.AppendLine($"        <div class='kpi-card'><h3>✅ Terminées</h3><div class='value'>{TachesTerminees}</div><p>{PourcentageTerminees}</p></div>");
            html.AppendLine($"        <div class='kpi-card'><h3>🔄 En cours</h3><div class='value'>{TachesEnCours}</div></div>");
            html.AppendLine($"        <div class='kpi-card'><h3>📁 Projets actifs</h3><div class='value'>{ProjetsActifs}</div></div>");
            html.AppendLine($"        <div class='kpi-card'><h3>⏰ Total Heures CRA</h3><div class='value'>{TotalHeuresCRA}</div><p>{TotalJoursCRA}</p></div>");
            html.AppendLine($"        <div class='kpi-card'><h3>📅 Jours saisis ce mois</h3><div class='value'>{JoursSaisisMoisCourant}</div><p>{TauxCompletionMois}</p></div>");
            html.AppendLine($"        <div class='kpi-card'><h3>👥 Devs actifs</h3><div class='value'>{DevsActifs}</div></div>");
            html.AppendLine($"        <div class='kpi-card'><h3>🎯 Taux de réalisation</h3><div class='value'>{TauxRealisation}</div></div>");
            html.AppendLine("    </div>");
            
            // Charge par développeur (Tâches)
            html.AppendLine("    <div class='section'>");
            html.AppendLine("        <h2>👥 Charge par Développeur (Tâches)</h2>");
            html.AppendLine("        <table>");
            html.AppendLine("            <tr><th>Développeur</th><th>À faire</th><th>En cours</th><th>Terminées</th><th>Total</th></tr>");
            foreach (var dev in ChargeParDev)
            {
                html.AppendLine($"            <tr><td>{dev.NomDev}</td><td>{dev.AFaire}</td><td>{dev.EnCours}</td><td>{dev.Terminees}</td><td><strong>{dev.Total}</strong></td></tr>");
            }
            html.AppendLine("        </table>");
            html.AppendLine("    </div>");
            
            // CRA par développeur
            html.AppendLine("    <div class='section'>");
            html.AppendLine("        <h2>⏰ CRA par Développeur (Heures)</h2>");
            html.AppendLine("        <table>");
            html.AppendLine("            <tr><th>Développeur</th><th>Heures ce mois</th><th>Heures total</th><th>Jours total</th></tr>");
            foreach (var cra in CRAParDev)
            {
                html.AppendLine($"            <tr><td>{cra.NomDev}</td><td>{cra.HeuresMois}</td><td>{cra.HeuresTotal}</td><td>{cra.JoursTotal}</td></tr>");
            }
            html.AppendLine("        </table>");
            html.AppendLine("    </div>");
            
            // Projets
            html.AppendLine("    <div class='section'>");
            html.AppendLine("        <h2>📁 Taux de Complétion par Projet</h2>");
            html.AppendLine("        <table>");
            html.AppendLine("            <tr><th>Projet</th><th>Total tâches</th><th>Terminées</th><th>Taux</th><th>Heures CRA</th></tr>");
            foreach (var projet in CompletionParProjet)
            {
                html.AppendLine($"            <tr><td>{projet.NomProjet}</td><td>{projet.TotalTaches}</td><td>{projet.TachesTerminees}</td><td><strong>{projet.TauxCompletion}</strong></td><td>{projet.HeuresCRA}</td></tr>");
            }
            html.AppendLine("        </table>");
            html.AppendLine("    </div>");
            
            // Footer
            html.AppendLine("    <div class='footer'>");
            html.AppendLine("        <p>© BNP Paribas - Backlog Manager</p>");
            html.AppendLine("        <p>Pour imprimer en PDF : Fichier → Imprimer → Enregistrer en PDF</p>");
            html.AppendLine("    </div>");
            
            html.AppendLine("</body>");
            html.AppendLine("</html>");
            
            System.IO.File.WriteAllText(filePath, html.ToString(), System.Text.Encoding.UTF8);
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // ViewModels pour les collections
    public class StatutStatsViewModel
    {
        public string Statut { get; set; }
        public int Nombre { get; set; }
        public string Pourcentage { get; set; }
        public SolidColorBrush Couleur { get; set; }
        public double LargeurBarre { get; set; }
    }

    public class ChargeDevViewModel
    {
        public Dev Dev { get; set; }
        public string NomDev { get; set; }
        public int AFaire { get; set; }
        public int EnCours { get; set; }
        public int Terminees { get; set; }
        public int Total { get; set; }
        
        // Propriétés pour les barres de progression (largeur max 400px)
        public double LargeurAfaire => Total > 0 ? (AFaire * 400.0 / Total) : 0;
        public double LargeurEnCours => Total > 0 ? (EnCours * 400.0 / Total) : 0;
        public double LargeurTerminees => Total > 0 ? (Terminees * 400.0 / Total) : 0;
    }

    public class CRADevViewModel
    {
        public int DevId { get; set; }
        public string NomDev { get; set; }
        public string HeuresMois { get; set; }
        public string HeuresTotal { get; set; }
        public string JoursTotal { get; set; }
        
        // Propriété pour la barre de progression (basée sur le max des heures totales)
        // Calculée dynamiquement - largeur max 400px
        private static double _maxHeures = 1000; // Valeur par défaut
        public static void SetMaxHeures(double max) => _maxHeures = max > 0 ? max : 1000;
        public double LargeurBarreTotal
        {
            get
            {
                if (double.TryParse(HeuresTotal, out double heures))
                    return (heures * 400.0 / _maxHeures);
                return 0;
            }
        }
    }

    public class CompletionProjetViewModel
    {
        public Projet Projet { get; set; }
        public string NomProjet { get; set; }
        public int TotalTaches { get; set; }
        public int TachesTerminees { get; set; }
        public string TauxCompletion { get; set; }
        public string HeuresCRA { get; set; }
        
        // Propriétés pour la visualisation
        public string CouleurProjet => Projet?.CouleurHex ?? "#00915A";
        public double LargeurBarreCompletion
        {
            get
            {
                if (TotalTaches > 0)
                    return (TachesTerminees * 400.0 / TotalTaches);
                return 0;
            }
        }
        public string LabelCompletion => $"{TachesTerminees}/{TotalTaches}";
    }
}

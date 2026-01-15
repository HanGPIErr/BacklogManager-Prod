using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using BacklogManager.Domain;
using BacklogManager.Services;

namespace BacklogManager.Views
{
    public partial class DashboardView : INotifyPropertyChanged
    {
        private readonly BacklogService _backlogService;
        private readonly NotificationService _notificationService;
        private readonly AuthenticationService _authService;
        private readonly AuditLogService _auditLogService;
        private readonly CRAService _craService;
        private readonly PermissionService _permissionService;

        public event PropertyChangedEventHandler PropertyChanged;

        private string _nomUtilisateur;
        public string NomUtilisateur
        {
            get => _nomUtilisateur;
            set { _nomUtilisateur = value; OnPropertyChanged(); }
        }

        private string _dateAujourdhui;
        public string DateAujourdhui
        {
            get => _dateAujourdhui;
            set { _dateAujourdhui = value; OnPropertyChanged(); }
        }

        private ObservableCollection<TacheUrgenteViewModel> _tachesUrgentes;
        public ObservableCollection<TacheUrgenteViewModel> TachesUrgentes
        {
            get => _tachesUrgentes;
            set { _tachesUrgentes = value; OnPropertyChanged(); OnPropertyChanged(nameof(NbTachesUrgentes)); OnPropertyChanged(nameof(HasNoUrgentTasks)); }
        }

        private ObservableCollection<NotificationViewModel> _notificationsRecentes;
        public ObservableCollection<NotificationViewModel> NotificationsRecentes
        {
            get => _notificationsRecentes;
            set { _notificationsRecentes = value; OnPropertyChanged(); OnPropertyChanged(nameof(NbNotifications)); OnPropertyChanged(nameof(HasNoNotifications)); }
        }

        public int NbTachesUrgentes => TachesUrgentes?.Count ?? 0;
        public int NbNotifications => NotificationsRecentes?.Count ?? 0;
        public bool HasNoUrgentTasks => NbTachesUrgentes == 0;
        public bool HasNoNotifications => NbNotifications == 0;

        private int _nbTachesTerminees;
        public int NbTachesTerminees
        {
            get => _nbTachesTerminees;
            set { _nbTachesTerminees = value; OnPropertyChanged(); }
        }

        private int _nbTachesTotal;
        public int NbTachesTotal
        {
            get => _nbTachesTotal;
            set { _nbTachesTotal = value; OnPropertyChanged(); }
        }

        private int _nbTachesEnCours;
        public int NbTachesEnCours
        {
            get => _nbTachesEnCours;
            set { _nbTachesEnCours = value; OnPropertyChanged(); }
        }

        private int _nbProjetsActifs;
        public int NbProjetsActifs
        {
            get => _nbProjetsActifs;
            set { _nbProjetsActifs = value; OnPropertyChanged(); }
        }

        private int _nbEquipesActives;
        public int NbEquipesActives
        {
            get => _nbEquipesActives;
            set { _nbEquipesActives = value; OnPropertyChanged(); }
        }

        private int _nbRessourcesTotal;
        public int NbRessourcesTotal
        {
            get => _nbRessourcesTotal;
            set { _nbRessourcesTotal = value; OnPropertyChanged(); }
        }

        private double _chargeMoyenneParEquipe;
        public double ChargeMoyenneParEquipe
        {
            get => _chargeMoyenneParEquipe;
            set { _chargeMoyenneParEquipe = value; OnPropertyChanged(); }
        }

        private int _tauxProductivite;
        public int TauxProductivite
        {
            get => _tauxProductivite;
            set { _tauxProductivite = value; OnPropertyChanged(); OnPropertyChanged(nameof(MetriqueValeur)); }
        }

        private int _nbCRAsAValider;
        public int NbCRAsAValider
        {
            get => _nbCRAsAValider;
            set { _nbCRAsAValider = value; OnPropertyChanged(); OnPropertyChanged(nameof(MetriqueValeur)); }
        }

        private bool _estAdmin;
        public bool EstAdmin
        {
            get => _estAdmin;
            set { _estAdmin = value; OnPropertyChanged(); OnPropertyChanged(nameof(MetriqueLabel)); OnPropertyChanged(nameof(MetriqueValeur)); OnPropertyChanged(nameof(MetriqueSuffixe)); }
        }

        public string MetriqueLabel => EstAdmin ? LocalizationService.Instance["Dashboard_CRAToValidate"] : "Respect deadline";
        public string MetriqueValeur => EstAdmin ? NbCRAsAValider.ToString() : TauxProductivite.ToString();
        public string MetriqueSuffixe => EstAdmin ? "" : "%";

        // Propriétés pour les textes traduits
        public string HelloText => $"👋 {LocalizationService.Instance["Dashboard_Hello"]}, ";
        public string GuideText => $"📖 {LocalizationService.Instance["Dashboard_Guide"]}";
        public string CompletedText => LocalizationService.Instance["Dashboard_Completed"];
        public string InProgressText => LocalizationService.Instance["Dashboard_InProgress"];
        public string UrgentText => LocalizationService.Instance["Dashboard_Urgent"];
        public string ProjectsText => LocalizationService.Instance["Dashboard_Projects"];
        public string TeamsText => LocalizationService.Instance["Dashboard_Teams"];
        public string ResourcesText => LocalizationService.Instance["Dashboard_Resources"];
        public string LoadPerTeamText => LocalizationService.Instance["Dashboard_LoadPerTeam"];
        public string AllTeamsText => LocalizationService.Instance["Dashboard_AllTeams"];
        public string ClickForDetailsText => LocalizationService.Instance["Dashboard_ClickForDetails"];
        public string ResourceDistributionText => LocalizationService.Instance["Dashboard_ResourceDistribution"];
        public string ActiveMembersText => LocalizationService.Instance["Dashboard_ActiveMembers"];
        public string ProjectsOverviewText => LocalizationService.Instance["Dashboard_ProjectsOverview"];
        public string TasksText => LocalizationService.Instance["Dashboard_Tasks"];
        public string MyUrgentTasksText => LocalizationService.Instance["Dashboard_MyUrgentTasks"];
        public string ViewAllArrowText => LocalizationService.Instance["Dashboard_ViewAllArrow"];
        public string PerTeamText => LocalizationService.Instance["Dashboard_PerTeam"];
        public string RecentActivityText => LocalizationService.Instance["Dashboard_RecentActivity"];
        public string QuickActionsText => LocalizationService.Instance["Dashboard_QuickActions"];
        public string NewTaskText => LocalizationService.Instance["Dashboard_NewTask"];
        public string ViewKanbanText => LocalizationService.Instance["Dashboard_ViewKanban"];
        public string TimelineProjectText => LocalizationService.Instance["Dashboard_TimelineProject"];

        private ObservableCollection<ActiviteViewModel> _activitesRecentes;
        public ObservableCollection<ActiviteViewModel> ActivitesRecentes
        {
            get => _activitesRecentes;
            set { _activitesRecentes = value; OnPropertyChanged(); }
        }

        private ObservableCollection<StatistiqueProjetViewModel> _statistiquesProjetsList;
        public ObservableCollection<StatistiqueProjetViewModel> StatistiquesProjetsList
        {
            get => _statistiquesProjetsList;
            set { _statistiquesProjetsList = value; OnPropertyChanged(); OnPropertyChanged(nameof(AucunProjet)); }
        }

        public bool AucunProjet => StatistiquesProjetsList == null || StatistiquesProjetsList.Count == 0;

        public DashboardView(BacklogService backlogService, NotificationService notificationService, AuthenticationService authService, PermissionService permissionService)
        {
            _backlogService = backlogService;
            _notificationService = notificationService;
            _authService = authService;
            _auditLogService = authService.GetAuditLogService();
            _craService = new CRAService(backlogService.Database);
            _permissionService = permissionService;

            // Initialiser les collections AVANT InitializeComponent
            TachesUrgentes = new ObservableCollection<TacheUrgenteViewModel>();
            NotificationsRecentes = new ObservableCollection<NotificationViewModel>();
            ActivitesRecentes = new ObservableCollection<ActiviteViewModel>();
            StatistiquesProjetsList = new ObservableCollection<StatistiqueProjetViewModel>();

            InitializeComponent();
            DataContext = this;

            ChargerDonnees();
            InitialiserTextes();
            
            // S'abonner aux changements de langue
            LocalizationService.Instance.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "Item[]")
                {
                    InitialiserTextes();
                }
            };
        }

        private void InitialiserTextes()
        {
            var loc = LocalizationService.Instance;
            
            // Mettre à jour la date dans la langue appropriée
            var culture = loc.CurrentCulture;
            DateAujourdhui = DateTime.Now.ToString("dd MMMM yyyy", culture);
            
            // Notifier que tous les textes traduits ont changé
            OnPropertyChanged(nameof(HelloText));
            OnPropertyChanged(nameof(GuideText));
            OnPropertyChanged(nameof(CompletedText));
            OnPropertyChanged(nameof(InProgressText));
            OnPropertyChanged(nameof(UrgentText));
            OnPropertyChanged(nameof(ProjectsText));
            OnPropertyChanged(nameof(TeamsText));
            OnPropertyChanged(nameof(ResourcesText));
            OnPropertyChanged(nameof(LoadPerTeamText));
            OnPropertyChanged(nameof(AllTeamsText));
            OnPropertyChanged(nameof(ClickForDetailsText));
            OnPropertyChanged(nameof(MetriqueLabel));
            OnPropertyChanged(nameof(ResourceDistributionText));
            OnPropertyChanged(nameof(ActiveMembersText));
            OnPropertyChanged(nameof(ProjectsOverviewText));
            OnPropertyChanged(nameof(TasksText));
            OnPropertyChanged(nameof(MyUrgentTasksText));
            OnPropertyChanged(nameof(ViewAllArrowText));
            OnPropertyChanged(nameof(PerTeamText));
            OnPropertyChanged(nameof(RecentActivityText));
            OnPropertyChanged(nameof(QuickActionsText));
            OnPropertyChanged(nameof(NewTaskText));
            OnPropertyChanged(nameof(ViewKanbanText));
            OnPropertyChanged(nameof(TimelineProjectText));
        }

        private void ChargerDonnees()
        {
            // Nom utilisateur
            var user = _authService.CurrentUser;
            NomUtilisateur = user != null ? $"{user.Prenom} {user.Nom}" : "Utilisateur";

            // Date avec localisation
            var culture = LocalizationService.Instance.CurrentCulture;
            DateAujourdhui = DateTime.Now.ToString("dd MMMM yyyy", culture);

            // Tâches urgentes (priorité URGENTE + échéance proche)
            var toutes = _backlogService.GetAllBacklogItems();
            var projets = _backlogService.GetAllProjets();
            var urgentes = toutes
                .Where(t => t.Priorite == Priorite.Urgent || 
                           (t.DateFinAttendue.HasValue && (t.DateFinAttendue.Value - DateTime.Now).TotalDays <= 2))
                .Where(t => t.Statut != Statut.Termine)
                .OrderBy(t => t.DateFinAttendue)
                .Take(4)
                .Select(t => new TacheUrgenteViewModel
                {
                    Id = t.Id,
                    Titre = t.Titre,
                    DateFinAttendue = t.DateFinAttendue,
                    ProjetNom = t.ProjetId.HasValue ? projets.FirstOrDefault(p => p.Id == t.ProjetId.Value)?.Nom ?? "Sans projet" : "Sans projet"
                })
                .ToList();
            TachesUrgentes = new ObservableCollection<TacheUrgenteViewModel>(urgentes);

            // Notifications récentes (5 dernières non lues)
            var notifications = _notificationService.GetNotificationsNonLues()
                .OrderByDescending(n => n.DateCreation)
                .Take(5)
                .Select(n => new NotificationViewModel
                {
                    Titre = n.Titre,
                    Message = n.Message,
                    Icone = GetIconeNotification(n.Type),
                    EstNonLue = true
                })
                .ToList();
            NotificationsRecentes = new ObservableCollection<NotificationViewModel>(notifications);

            // Statistiques
            var allTaches = toutes.ToList();
            NbTachesTotal = allTaches.Count;
            NbTachesTerminees = allTaches.Count(t => t.Statut == Statut.Termine);
            NbTachesEnCours = allTaches.Count(t => t.Statut == Statut.EnCours);
            
            NbProjetsActifs = projets.Count(p => p.Actif);
            
            // Statistiques équipes
            var equipes = _backlogService.Database.GetAllEquipes();
            var utilisateurs = _backlogService.Database.GetUtilisateurs();
            
            NbEquipesActives = equipes.Count(e => e.Actif);
            NbRessourcesTotal = utilisateurs.Count(u => u.Actif && u.EquipeId.HasValue);
            
            // Charge moyenne par équipe (nombre de projets par équipe)
            if (NbEquipesActives > 0)
            {
                var totalProjetsAssignes = 0;
                foreach (var projet in projets.Where(p => p.Actif && p.EquipesAssigneesIds != null && p.EquipesAssigneesIds.Count > 0))
                {
                    totalProjetsAssignes += projet.EquipesAssigneesIds.Count;
                }
                ChargeMoyenneParEquipe = (double)totalProjetsAssignes / NbEquipesActives;
            }
            else
            {
                ChargeMoyenneParEquipe = 0;
            }
            
            // Métrique intelligente selon le rôle
            if (_permissionService.IsAdmin || _permissionService.IsChefDeProjet)
            {
                // Pour Admin/CP : Nombre de CRA à valider
                var tousLesCRAs = _craService?.GetAllCRAs() ?? new List<CRA>();
                NbCRAsAValider = tousLesCRAs.Count(c => c.EstAValider);
                EstAdmin = true;
            }
            else
            {
                // Pour Développeurs : Taux de respect des deadlines (tâches en cours)
                var tachesEnCours = allTaches.Where(t => t.Statut == Statut.EnCours && t.DateFinAttendue.HasValue).ToList();
                if (tachesEnCours.Any())
                {
                    var tachesDansLesTemps = tachesEnCours.Count(t => t.DateFinAttendue.Value >= DateTime.Now);
                    TauxProductivite = (tachesDansLesTemps * 100) / tachesEnCours.Count;
                }
                else
                {
                    TauxProductivite = 100; // Aucune tâche en cours = 100%
                }
                EstAdmin = false;
            }
            
            // Statistiques des projets
            ChargerStatistiquesProjets(projets, allTaches);
            
            // Section Équipe
            ChargerSectionEquipe();
            
            // Activités récentes (vraies données depuis AuditLog et CRA)
            ChargerActivitesRecentes();
        }

        private void ChargerStatistiquesProjets(IEnumerable<Projet> projets, List<BacklogItem> allTaches)
        {
            var statsProjects = new List<StatistiqueProjetViewModel>();

            foreach (var projet in projets.Where(p => p.Actif && p.Nom != "Tâches administratives").OrderBy(p => p.Nom))
            {
                var tachesProjet = allTaches.Where(t => t.ProjetId == projet.Id).ToList();
                var nbTotal = tachesProjet.Count;
                
                if (nbTotal == 0) continue; // Ignorer les projets sans tâches

                var nbAfaire = tachesProjet.Count(t => t.Statut == Statut.Afaire);
                var nbEnCours = tachesProjet.Count(t => t.Statut == Statut.EnCours);
                var nbTerminees = tachesProjet.Count(t => t.Statut == Statut.Termine);
                
                // Tâches en retard : en cours ou à faire avec date dépassée
                var nbEnRetard = tachesProjet.Count(t => 
                    t.Statut != Statut.Termine && 
                    t.DateFinAttendue.HasValue && 
                    t.DateFinAttendue.Value < DateTime.Now);

                var progression = nbTotal > 0 ? (nbTerminees * 100.0) / nbTotal : 0;
                var largeur = Math.Min(progression * 3, 300); // Max 300px

                statsProjects.Add(new StatistiqueProjetViewModel
                {
                    ProjetId = projet.Id,
                    NomProjet = projet.Nom,
                    CouleurProjet = !string.IsNullOrEmpty(projet.CouleurHex) ? projet.CouleurHex : "#00915A",
                    NbTachesTotal = nbTotal,
                    NbTachesAfaire = nbAfaire,
                    NbTachesEnCours = nbEnCours,
                    NbTachesTerminees = nbTerminees,
                    NbTachesEnRetard = nbEnRetard,
                    ProgressionPourcentage = progression,
                    ProgressionLargeur = largeur
                });
            }

            StatistiquesProjetsList = new ObservableCollection<StatistiqueProjetViewModel>(statsProjects);
        }

        private void ChargerActivitesRecentes()
        {
            var activites = new List<ActiviteViewModel>();
            var maintenant = DateTime.Now;
            var loc = LocalizationService.Instance;
            
            try
            {
                // 1. Récupérer les actions pertinentes depuis l'audit log
                var auditLogs = _auditLogService?.GetRecentLogs(50) ?? new List<AuditLog>();
                
                // Filtrer uniquement les actions liées aux tâches et projets (pas les connexions/déconnexions)
                var logsFiltered = auditLogs
                    .Where(log => log.EntityType == "BacklogItem" || 
                                  log.EntityType == "Projet" || 
                                  log.EntityType == "CRA")
                    .Take(10)
                    .ToList();
                
                foreach (var log in logsFiltered.Take(5))
                {
                    var tempsEcoule = GetTempsEcoule(log.DateAction, maintenant);
                    
                    string action = log.Action switch
                    {
                        "CREATE" => loc["Activity_Created"],
                        "UPDATE" => loc["Activity_Modified"],
                        "DELETE" => loc["Activity_Deleted"],
                        _ => log.Action
                    };
                    
                    string details = "";
                    int? backlogItemId = null;
                    bool estArchive = false;
                    
                    if (log.EntityType == "BacklogItem")
                    {
                        backlogItemId = log.EntityId;
                        
                        // Tâche : reconstruire le texte traduit dynamiquement
                        if (log.EntityId.HasValue)
                        {
                            var tache = _backlogService.GetBacklogItemById(log.EntityId.Value);
                            if (tache != null)
                            {
                                estArchive = tache.EstArchive;
                                var titre = tache.Titre.Length > 45 ? tache.Titre.Substring(0, 42) + "..." : tache.Titre;
                                // Reconstruire le texte traduit selon l'action
                                if (log.Action == "UPDATE")
                                {
                                    details = string.Format(loc["Audit_TaskModification"], log.EntityId);
                                }
                                else if (log.Action == "CREATE")
                                {
                                    details = string.Format(loc["Audit_TaskCreation"], log.EntityId);
                                }
                                else
                                {
                                    details = titre;
                                }
                            }
                            else
                            {
                                // Si la tâche n'existe plus, utiliser le texte traduit générique
                                details = string.Format(loc["Activity_TaskNumber"], log.EntityId);
                            }
                        }
                        else
                        {
                            details = loc["Activity_Task"];
                        }
                    }
                    else if (log.EntityType == "Projet")
                    {
                        details = !string.IsNullOrEmpty(log.Details) ? log.Details : loc["Activity_Project"];
                    }
                    else if (log.EntityType == "CRA")
                    {
                        details = loc["Activity_TimeLogged"];
                    }
                    
                    activites.Add(new ActiviteViewModel
                    {
                        Action = action,
                        Details = details,
                        Temps = tempsEcoule,
                        BacklogItemId = backlogItemId,
                        EstArchive = estArchive
                    });
                }
                
                // 2. Ajouter les CRA récents (congés, tâches travaillées, support)
                var currentUser = _authService.CurrentUser;
                if (currentUser != null)
                {
                    var crasRecents = _craService?.GetCRAsByDev(currentUser.Id, maintenant.AddDays(-7), maintenant) 
                        ?? new List<CRA>();
                    
                    var toutes = _backlogService.GetAllBacklogItems();
                    var tachesDico = toutes.ToDictionary(t => t.Id, t => t);
                    
                    var crasByDate = crasRecents
                        .Where(cra => tachesDico.ContainsKey(cra.BacklogItemId))
                        .OrderByDescending(c => c.Date)
                        .GroupBy(c => c.Date.Date)
                        .Take(5);
                    
                    foreach (var craGroup in crasByDate)
                    {
                        var date = craGroup.Key;
                        var tempsEcoule = GetTempsEcoule(date, maintenant);
                        
                        // Grouper par type de tâche
                        var conges = craGroup.Where(c => tachesDico[c.BacklogItemId].TypeDemande == TypeDemande.Conges).ToList();
                        var absences = craGroup.Where(c => tachesDico[c.BacklogItemId].TypeDemande == TypeDemande.NonTravaille).ToList();
                        var support = craGroup.Where(c => tachesDico[c.BacklogItemId].TypeDemande == TypeDemande.Support).ToList();
                        var travail = craGroup.Where(c => 
                            tachesDico[c.BacklogItemId].TypeDemande != TypeDemande.Conges &&
                            tachesDico[c.BacklogItemId].TypeDemande != TypeDemande.NonTravaille &&
                            tachesDico[c.BacklogItemId].TypeDemande != TypeDemande.Support).ToList();
                        
                        // Congés
                        if (conges.Any())
                        {
                            var totalJours = conges.Sum(c => c.HeuresTravaillees) / 8.0;
                            var tache = tachesDico[conges.First().BacklogItemId];
                            activites.Add(new ActiviteViewModel
                            {
                                Action = loc["Activity_Leave"],
                                Details = $"{totalJours:F1}{loc["Activity_Days"]} - {tache.Titre}",
                                Temps = tempsEcoule,
                                BacklogItemId = tache.Id,
                                EstArchive = tache.EstArchive
                            });
                        }
                        
                        // Absences
                        if (absences.Any())
                        {
                            var totalJours = absences.Sum(c => c.HeuresTravaillees) / 8.0;
                            var tache = tachesDico[absences.First().BacklogItemId];
                            activites.Add(new ActiviteViewModel
                            {
                                Action = loc["Activity_Absence"],
                                Details = $"{totalJours:F1}{loc["Activity_Days"]} - {tache.Titre}",
                                Temps = tempsEcoule,
                                BacklogItemId = tache.Id,
                                EstArchive = tache.EstArchive
                            });
                        }
                        
                        // Support
                        if (support.Any())
                        {
                            var totalJours = support.Sum(c => c.HeuresTravaillees) / 8.0;
                            var tache = tachesDico[support.First().BacklogItemId];
                            activites.Add(new ActiviteViewModel
                            {
                                Action = loc["Activity_Support"],
                                Details = $"{totalJours:F1}{loc["Activity_Days"]} - {tache.Titre}",
                                Temps = tempsEcoule,
                                BacklogItemId = tache.Id,
                                EstArchive = tache.EstArchive
                            });
                        }
                        
                        // Travail normal (afficher uniquement si plusieurs tâches dans la journée)
                        if (travail.Count >= 2)
                        {
                            var totalJours = travail.Sum(c => c.HeuresTravaillees) / 8.0;
                            var nbTaches = travail.Select(c => c.BacklogItemId).Distinct().Count();
                            var tasksWord = nbTaches > 1 ? loc["Activity_TasksPlural"] : loc["Activity_Tasks"];
                            activites.Add(new ActiviteViewModel
                            {
                                Action = loc["Activity_Work"],
                                Details = $"{totalJours:F1}{loc["Activity_Days"]} {loc["Activity_On"]} {nbTaches} {tasksWord}",
                                Temps = tempsEcoule
                            });
                        }
                        else if (travail.Count == 1)
                        {
                            var cra = travail.First();
                            var tache = tachesDico[cra.BacklogItemId];
                            var jours = cra.HeuresTravaillees / 8.0;
                            var titre = tache.Titre.Length > 35 ? tache.Titre.Substring(0, 32) + "..." : tache.Titre;
                            activites.Add(new ActiviteViewModel
                            {
                                Action = loc["Activity_TimeLogged"],
                                Details = $"{jours:F1}{loc["Activity_Days"]} - {titre}",
                                Temps = tempsEcoule,
                                BacklogItemId = tache.Id,
                                EstArchive = tache.EstArchive
                            });
                        }
                    }
                }
                
                // Trier par pertinence (plus récent en premier) et prendre les 8 premières
                ActivitesRecentes = new ObservableCollection<ActiviteViewModel>(
                    activites.Take(8)
                );
                
                // Si aucune activité, afficher un message
                if (ActivitesRecentes.Count == 0)
                {
                    ActivitesRecentes.Add(new ActiviteViewModel
                    {
                        Action = LocalizationService.Instance["Activity_NoActivity"],
                        Details = LocalizationService.Instance["Activity_StartWorking"],
                        Temps = LocalizationService.Instance["Activity_Now"]
                    });
                }
            }
            catch
            {
                // En cas d'erreur, afficher un message générique
                ActivitesRecentes = new ObservableCollection<ActiviteViewModel>
                {
                    new ActiviteViewModel
                    {
                        Action = LocalizationService.Instance["Activity_Error"],
                        Details = LocalizationService.Instance["Activity_LoadError"],
                        Temps = LocalizationService.Instance["Activity_Now"]
                    }
                };
            }
        }
        
        private string GetTempsEcoule(DateTime dateAction, DateTime maintenant)
        {
            var diff = maintenant - dateAction;
            var loc = LocalizationService.Instance;
            
            if (diff.TotalMinutes < 1)
                return loc["Activity_JustNow"];
            if (diff.TotalMinutes < 60)
                return string.Format(loc["Activity_MinutesAgo"], (int)diff.TotalMinutes);
            if (diff.TotalHours < 24)
                return string.Format(loc["Activity_HoursAgo"], (int)diff.TotalHours);
            if (diff.TotalDays < 2)
                return loc["Activity_Yesterday"];
            if (diff.TotalDays < 7)
                return string.Format(loc["Activity_DaysAgo"], (int)diff.TotalDays);
            
            return dateAction.ToString("dd/MM");
        }
        
        private string GetIconeAction(string action)
        {
            return action switch
            {
                "CREATE" => "✅",
                "UPDATE" => "📝",
                "DELETE" => "🗑️",
                "LOGIN" => "🔐",
                "LOGOUT" => "🚪",
                _ => "📋"
            };
        }

        private string GetIconeNotification(NotificationType type)
        {
            return type switch
            {
                NotificationType.Urgent => "🔴",
                NotificationType.Attention => "🟠",
                NotificationType.Info => "🔵",
                NotificationType.Success => "🟢",
                _ => "🔵"
            };
        }

        private void VoirToutesUrgentes_Click(object sender, RoutedEventArgs e)
        {
            // Navigation vers Backlog avec filtre urgentes
            var mainWindow = Window.GetWindow(this) as MainWindow;
            mainWindow?.AfficherBacklog();
        }

        private void VoirToutesNotifications_Click(object sender, RoutedEventArgs e)
        {
            // Navigation vers Notifications
            var mainWindow = Window.GetWindow(this) as MainWindow;
            mainWindow?.AfficherNotifications();
        }

        private void TacheUrgente_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var border = sender as System.Windows.Controls.Border;
            if (border?.DataContext is TacheUrgenteViewModel tache)
            {
                var mainWindow = Window.GetWindow(this) as MainWindow;
                mainWindow?.AfficherBacklog();
            }
        }

        private void NouvelleTache_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;
            mainWindow?.AfficherBacklog();
        }

        private void Activite_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is ActiviteViewModel activite && activite.BacklogItemId.HasValue)
            {
                var mainWindow = Window.GetWindow(this) as MainWindow;
                if (mainWindow != null)
                {
                    // Toujours naviguer vers le Backlog (qui contient aussi les archives)
                    mainWindow.AfficherBacklog();
                }
            }
        }

        private void VoirKanban_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;
            mainWindow?.AfficherKanban();
        }

        private void VoirTimeline_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;
            mainWindow?.AfficherTimeline();
        }

        private void BtnVoirGuide_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var mainWindow = Window.GetWindow(this) as MainWindow;
                mainWindow?.AfficherGuide();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ouverture du guide : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnVoirGuide_Click_Old(object sender, RoutedEventArgs e)
        {
            try
            {
                var guideWindow = new Views.GuideUtilisateurWindow(_authService, _backlogService.Database)
                {
                    Owner = Window.GetWindow(this)
                };

                if (guideWindow.ShowDialog() == true && guideWindow.Tag is string cible)
                {
                    // Naviguer vers la section demandée
                    var mainWindow = Window.GetWindow(this) as MainWindow;
                    
                    switch (cible)
                    {
                        case "Dashboard":
                            // Call the button click method to navigate to dashboard
                            var btnDashboard = mainWindow?.FindName("BtnDashboard") as System.Windows.Controls.Button;
                            btnDashboard?.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
                            break;
                        case "Backlog":
                            mainWindow?.AfficherBacklog();
                            break;
                        case "Kanban":
                            mainWindow?.AfficherKanban();
                            break;
                        case "CRA":
                            // Call the button click method to navigate to CRA
                            var btnCRA = mainWindow?.FindName("BtnSaisirCRA") as System.Windows.Controls.Button;
                            btnCRA?.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
                            break;
                        case "Demandes":
                            // Call the button click method to navigate to demandes
                            var btnDemandes = mainWindow?.FindName("BtnDemandes") as System.Windows.Controls.Button;
                            btnDemandes?.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
                            break;
                        case "Administration":
                            // Call the button click method to navigate to admin
                            var btnAdmin = mainWindow?.FindName("BtnAdmin") as System.Windows.Controls.Button;
                            btnAdmin?.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ouverture du guide : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ChargerSectionEquipe()
        {
            var user = _authService.CurrentUser;
            if (user == null) return;

            var equipes = _backlogService.Database.GetAllEquipes();
            var projets = _backlogService.GetAllProjets();
            var utilisateurs = _backlogService.Database.GetUtilisateurs();

            if (_permissionService.IsAdmin)
            {
                // Admin voit toutes les équipes
                TxtTitreEquipe.Text = LocalizationService.Instance["Dashboard_AllTeams"];
                
                var equipesViewModel = equipes.Where(e => e.Actif).Select(eq =>
                {
                    var nbProjets = 0;
                    foreach (var p in projets.Where(pr => pr.Actif && pr.EquipesAssigneesIds != null && pr.EquipesAssigneesIds.Count > 0))
                    {
                        if (p.EquipesAssigneesIds.Any(id => id == eq.Id))
                        {
                            nbProjets++;
                        }
                    }
                    
                    return new EquipeViewModel
                    {
                        Id = eq.Id,
                        Nom = eq.Nom,
                        NbMembres = utilisateurs.Count(u => u.EquipeId == eq.Id),
                        NbProjets = nbProjets
                    };
                }).ToList();

                if (equipesViewModel.Any())
                {
                    ListeToutesEquipes.ItemsSource = equipesViewModel;
                    ScrollEquipes.Visibility = Visibility.Visible;
                    PanelMonEquipe.Visibility = Visibility.Collapsed;
                    TxtAucuneEquipe.Visibility = Visibility.Collapsed;
                    
                    // Charger le graphique des ressources par équipe
                    ChargerGraphiqueEquipes(equipes, utilisateurs);
                }
                else
                {
                    ScrollEquipes.Visibility = Visibility.Collapsed;
                    PanelMonEquipe.Visibility = Visibility.Collapsed;
                    TxtAucuneEquipe.Visibility = Visibility.Visible;
                    GraphiqueEquipes.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                // Utilisateur standard voit sa propre équipe
                TxtTitreEquipe.Text = LocalizationService.Instance["Dashboard_MyTeam"];
                
                if (user.EquipeId.HasValue)
                {
                    var monEquipe = equipes.FirstOrDefault(e => e.Id == user.EquipeId.Value);
                    if (monEquipe != null)
                    {
                        TxtNomMonEquipe.Text = monEquipe.Nom;
                        TxtNbMembresMonEquipe.Text = utilisateurs.Count(u => u.EquipeId == monEquipe.Id).ToString();
                        
                        var nbProjets = 0;
                        foreach (var p in projets.Where(pr => pr.Actif && pr.EquipesAssigneesIds != null && pr.EquipesAssigneesIds.Count > 0))
                        {
                            if (p.EquipesAssigneesIds.Any(id => id == monEquipe.Id))
                            {
                                nbProjets++;
                            }
                        }
                        TxtNbProjetsMonEquipe.Text = nbProjets.ToString();
                        
                        PanelMonEquipe.Tag = monEquipe.Id;
                        PanelMonEquipe.Visibility = Visibility.Visible;
                        ListeToutesEquipes.Visibility = Visibility.Collapsed;
                        TxtAucuneEquipe.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        PanelMonEquipe.Visibility = Visibility.Collapsed;
                        ListeToutesEquipes.Visibility = Visibility.Collapsed;
                        TxtAucuneEquipe.Visibility = Visibility.Visible;
                    }
                }
                else
                {
                    PanelMonEquipe.Visibility = Visibility.Collapsed;
                    ListeToutesEquipes.Visibility = Visibility.Collapsed;
                    TxtAucuneEquipe.Visibility = Visibility.Visible;
                }
            }
        }

        private List<int> DeserializeEquipeIds(string json)
        {
            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<List<int>>(json) ?? new List<int>();
            }
            catch
            {
                return new List<int>();
            }
        }

        private void ChargerGraphiqueEquipes(List<Domain.Equipe> equipes, List<Domain.Utilisateur> utilisateurs)
        {
            try
            {
                // Calculer le nombre de membres par statut pour chaque équipe (sans compter les managers)
                var equipesStats = equipes
                    .Where(e => e.Actif)
                    .Select(e =>
                    {
                        var membresEquipe = utilisateurs.Where(u => u.EquipeId == e.Id && u.Actif && u.Id != e.ManagerId).ToList();
                        var manager = e.ManagerId.HasValue ? utilisateurs.FirstOrDefault(u => u.Id == e.ManagerId.Value) : null;
                        
                        // Construire le tooltip avec la liste des membres groupés par statut
                        var tooltip = new System.Text.StringBuilder();
                        tooltip.AppendLine($"📋 {e.Nom}");
                        tooltip.AppendLine($"━━━━━━━━━━━━━━━━━━━━");
                        
                        // Afficher le manager s'il existe
                        if (manager != null)
                        {
                            tooltip.AppendLine($"👨‍💼 Manager: {manager.Prenom} {manager.Nom}");
                            tooltip.AppendLine();
                        }
                        
                        tooltip.AppendLine($"Total: {membresEquipe.Count} membre(s)\n");
                        
                        // Grouper par statut
                        var parStatut = membresEquipe.GroupBy(u => u.Statut ?? "Non défini").OrderBy(g => g.Key);
                        foreach (var groupe in parStatut)
                        {
                            var icone = groupe.Key switch
                            {
                                "BAU" => "💼",
                                "PROJECTS" => "🎯",
                                "Temporary" => "⏱️",
                                "Hiring ongoing" => "🔍",
                                _ => "👤"
                            };
                            
                            tooltip.AppendLine($"{icone} {groupe.Key} ({groupe.Count()}):");
                            foreach (var membre in groupe.OrderBy(u => u.Nom))
                            {
                                tooltip.AppendLine($"   • {membre.Prenom} {membre.Nom}");
                            }
                            tooltip.AppendLine();
                        }
                        
                        return new EquipeGraphiqueViewModel
                        {
                            Nom = e.Nom,
                            NbBAU = membresEquipe.Count(u => u.Statut == "BAU"),
                            NbProjects = membresEquipe.Count(u => u.Statut == "PROJECTS"),
                            NbTemporary = membresEquipe.Count(u => u.Statut == "Temporary"),
                            NbHiringOngoing = membresEquipe.Count(u => u.Statut == "Hiring ongoing"),
                            NbMembres = membresEquipe.Count,
                            MembresToolTip = tooltip.ToString().TrimEnd()
                        };
                    })
                    .OrderByDescending(e => e.NbMembres)
                    .ToList();

                if (equipesStats.Any())
                {
                    var maxMembres = equipesStats.Any(e => e.NbMembres > 0) ? equipesStats.Max(e => e.NbMembres) : 1;
                    
                    // Définir le max pour les calculs de hauteur
                    foreach (var equipe in equipesStats)
                    {
                        equipe.MaxMembres = maxMembres;
                    }

                    GraphiqueBarresEquipes.ItemsSource = equipesStats;
                    GraphiqueEquipes.Visibility = Visibility.Visible;
                }
                else
                {
                    GraphiqueEquipes.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors du chargement du graphique : {ex.Message}");
                GraphiqueEquipes.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnVoirMonEquipe_Click(object sender, RoutedEventArgs e)
        {
            var user = _authService.CurrentUser;
            if (user?.EquipeId.HasValue == true)
            {
                NaviguerVersDetailEquipe(user.EquipeId.Value);
            }
        }

        private void BtnVoirEquipe_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is int equipeId)
            {
                NaviguerVersDetailEquipe(equipeId);
            }
        }

        private void NaviguerVersDetailEquipe(int equipeId)
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null)
            {
                var detailView = new Pages.DetailEquipeView(equipeId, _backlogService.Database, () =>
                {
                    // Callback pour retourner au Dashboard
                    mainWindow.NaviguerVersDashboard();
                }, _authService);
                var contentControl = (ContentControl)mainWindow.FindName("MainContentControl");
                if (contentControl != null)
                {
                    contentControl.Content = detailView;
                }
            }
        }

        private void ProjetCard_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is StatistiqueProjetViewModel projetStats)
                {
                    var mainWindow = Window.GetWindow(this) as MainWindow;
                    if (mainWindow != null)
                    {
                        // Récupérer le projet complet
                        var projet = _backlogService.GetAllProjets().FirstOrDefault(p => p.Id == projetStats.ProjetId);
                        if (projet != null)
                        {
                            // Naviguer vers Suivi CRA avec le projet sélectionné
                            mainWindow.NaviguerVersSuiviCRATimeline(projet);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la navigation : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class NotificationViewModel
    {
        public string Titre { get; set; }
        public string Message { get; set; }
        public string Icone { get; set; }
        public bool EstNonLue { get; set; }
    }

    public class ActiviteViewModel
    {
        public string Action { get; set; }
        public string Details { get; set; }
        public string Temps { get; set; }
        public int? BacklogItemId { get; set; }
        public bool EstArchive { get; set; }
    }

    public class StatistiqueProjetViewModel
    {
        public int ProjetId { get; set; }
        public string NomProjet { get; set; }
        public string CouleurProjet { get; set; }
        public int NbTachesTotal { get; set; }
        public int NbTachesAfaire { get; set; }
        public int NbTachesEnCours { get; set; }
        public int NbTachesTerminees { get; set; }
        public int NbTachesEnRetard { get; set; }
        public double ProgressionPourcentage { get; set; }
        public double ProgressionLargeur { get; set; }
        public double ProgressionLargeurCarte => (ProgressionPourcentage / 100.0) * 240; // 240px max pour les cartes
    }

    public class TacheUrgenteViewModel
    {
        public int Id { get; set; }
        public string Titre { get; set; }
        public DateTime? DateFinAttendue { get; set; }
        public string ProjetNom { get; set; }
    }

    public class EquipeViewModel
    {
        public int Id { get; set; }
        public string Nom { get; set; }
        public int NbMembres { get; set; }
        public int NbProjets { get; set; }
    }

    public class EquipeGraphiqueViewModel
    {
        public string Nom { get; set; }
        public int NbMembres { get; set; }
        public int MaxMembres { get; set; }
        public bool EstTopEquipe { get; set; }
        
        // Compteurs par statut
        public int NbBAU { get; set; }
        public int NbProjects { get; set; }
        public int NbTemporary { get; set; }
        public int NbHiringOngoing { get; set; }
        
        // Liste des membres pour le tooltip
        public string MembresToolTip { get; set; }
        
        // Hauteurs pour les barres empilées (en pixels, basé sur hauteur max de 250px)
        public double HauteurBAU => (MaxMembres > 0) ? (NbBAU * 250.0 / MaxMembres) : 0;
        public double HauteurProjects => (MaxMembres > 0) ? (NbProjects * 250.0 / MaxMembres) : 0;
        public double HauteurTemporary => (MaxMembres > 0) ? (NbTemporary * 250.0 / MaxMembres) : 0;
        public double HauteurHiringOngoing => (MaxMembres > 0) ? (NbHiringOngoing * 250.0 / MaxMembres) : 0;
    }
}

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

        private int _tauxProductivite;
        public int TauxProductivite
        {
            get => _tauxProductivite;
            set { _tauxProductivite = value; OnPropertyChanged(); }
        }

        private ObservableCollection<ActiviteViewModel> _activitesRecentes;
        public ObservableCollection<ActiviteViewModel> ActivitesRecentes
        {
            get => _activitesRecentes;
            set { _activitesRecentes = value; OnPropertyChanged(); }
        }

        public DashboardView(BacklogService backlogService, NotificationService notificationService, AuthenticationService authService)
        {
            _backlogService = backlogService;
            _notificationService = notificationService;
            _authService = authService;
            _auditLogService = authService.GetAuditLogService();
            _craService = new CRAService(backlogService.Database);

            // Initialiser les collections AVANT InitializeComponent
            TachesUrgentes = new ObservableCollection<TacheUrgenteViewModel>();
            NotificationsRecentes = new ObservableCollection<NotificationViewModel>();
            ActivitesRecentes = new ObservableCollection<ActiviteViewModel>();

            InitializeComponent();
            DataContext = this;

            ChargerDonnees();
        }

        private void ChargerDonnees()
        {
            // Nom utilisateur
            var user = _authService.CurrentUser;
            NomUtilisateur = user != null ? $"{user.Prenom} {user.Nom}" : "Utilisateur";

            // Date
            DateAujourdhui = DateTime.Now.ToString("dd MMMM yyyy", new System.Globalization.CultureInfo("fr-FR"));

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
            
            // Taux de productivité (% tâches terminées aujourd'hui)
            var aujourdhui = DateTime.Today;
            var tachesTermineesAujourdhui = allTaches.Count(t => 
                t.DateFin.HasValue && t.DateFin.Value.Date == aujourdhui && t.Statut == Statut.Termine);
            TauxProductivite = NbTachesEnCours > 0 ? (tachesTermineesAujourdhui * 100) / NbTachesEnCours : 100;
            
            // Activités récentes (vraies données depuis AuditLog et CRA)
            ChargerActivitesRecentes();
        }

        private void ChargerActivitesRecentes()
        {
            var activites = new List<ActiviteViewModel>();
            var maintenant = DateTime.Now;
            
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
                        "CREATE" => "✅ Créé",
                        "UPDATE" => "📝 Modifié",
                        "DELETE" => "🗑️ Supprimé",
                        _ => log.Action
                    };
                    
                    string details = "";
                    int? backlogItemId = null;
                    bool estArchive = false;
                    
                    if (log.EntityType == "BacklogItem")
                    {
                        backlogItemId = log.EntityId;
                        
                        // Tâche : afficher le titre si disponible
                        if (log.EntityId.HasValue)
                        {
                            var tache = _backlogService.GetBacklogItemById(log.EntityId.Value);
                            if (tache != null)
                            {
                                estArchive = tache.EstArchive;
                                var titre = tache.Titre.Length > 45 ? tache.Titre.Substring(0, 42) + "..." : tache.Titre;
                                details = titre;
                            }
                            else if (!string.IsNullOrEmpty(log.Details))
                            {
                                details = log.Details.Length > 45 ? log.Details.Substring(0, 42) + "..." : log.Details;
                            }
                            else
                            {
                                details = $"Tâche #{log.EntityId}";
                            }
                        }
                        else if (!string.IsNullOrEmpty(log.Details))
                        {
                            details = log.Details.Length > 45 ? log.Details.Substring(0, 42) + "..." : log.Details;
                        }
                        else
                        {
                            details = "Tâche";
                        }
                    }
                    else if (log.EntityType == "Projet")
                    {
                        details = !string.IsNullOrEmpty(log.Details) ? log.Details : "Projet";
                    }
                    else if (log.EntityType == "CRA")
                    {
                        details = "Temps saisi";
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
                                Action = "🏖️ Congé",
                                Details = $"{totalJours:F1}j - {tache.Titre}",
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
                                Action = "⏸️ Absence",
                                Details = $"{totalJours:F1}j - {tache.Titre}",
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
                                Action = "🤝 Support",
                                Details = $"{totalJours:F1}j - {tache.Titre}",
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
                            activites.Add(new ActiviteViewModel
                            {
                                Action = "💼 Travail",
                                Details = $"{totalJours:F1}j sur {nbTaches} tâche{(nbTaches > 1 ? "s" : "")}",
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
                                Action = "⏱️ Temps saisi",
                                Details = $"{jours:F1}j - {titre}",
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
                        Action = "ℹ️ Aucune activité récente",
                        Details = "Commencez à travailler sur des tâches",
                        Temps = "Maintenant"
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
                        Action = "⚠️ Erreur",
                        Details = "Impossible de charger les activités",
                        Temps = "Maintenant"
                    }
                };
            }
        }
        
        private string GetTempsEcoule(DateTime dateAction, DateTime maintenant)
        {
            var diff = maintenant - dateAction;
            
            if (diff.TotalMinutes < 1)
                return "À l'instant";
            if (diff.TotalMinutes < 60)
                return $"Il y a {(int)diff.TotalMinutes} min";
            if (diff.TotalHours < 24)
                return $"Il y a {(int)diff.TotalHours}h";
            if (diff.TotalDays < 2)
                return "Hier";
            if (diff.TotalDays < 7)
                return $"Il y a {(int)diff.TotalDays}j";
            
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
                var guideWindow = new Views.GuideUtilisateurWindow(_authService)
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

    public class ActiviteViewModel : INotifyPropertyChanged
    {
        public string Action { get; set; }
        public string Details { get; set; }
        public string Temps { get; set; }
        public int? BacklogItemId { get; set; }
        public bool EstArchive { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class TacheUrgenteViewModel
    {
        public int Id { get; set; }
        public string Titre { get; set; }
        public DateTime? DateFinAttendue { get; set; }
        public string ProjetNom { get; set; }
    }
}

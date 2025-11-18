using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using BacklogManager.Domain;
using BacklogManager.Services;

namespace BacklogManager.Views
{
    public partial class DashboardView : INotifyPropertyChanged
    {
        private readonly BacklogService _backlogService;
        private readonly NotificationService _notificationService;
        private readonly AuthenticationService _authService;

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
            NomUtilisateur = user != null ? $"{user.Prenom}" : "Utilisateur";

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
            
            // Activités récentes (simulé pour démo)
            ActivitesRecentes = new ObservableCollection<ActiviteViewModel>
            {
                new ActiviteViewModel { Action = "Tâche terminée", Details = "Validation des specs", Temps = "Il y a 2h" },
                new ActiviteViewModel { Action = "Commentaire ajouté", Details = "Sur tâche #15", Temps = "Il y a 3h" },
                new ActiviteViewModel { Action = "Sprint démarré", Details = "Sprint 12", Temps = "Hier" }
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
    }

    public class TacheUrgenteViewModel
    {
        public int Id { get; set; }
        public string Titre { get; set; }
        public DateTime? DateFinAttendue { get; set; }
        public string ProjetNom { get; set; }
    }
}

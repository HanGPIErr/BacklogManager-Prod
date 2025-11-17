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

        private ObservableCollection<BacklogItem> _tachesUrgentes;
        public ObservableCollection<BacklogItem> TachesUrgentes
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

        public DashboardView(BacklogService backlogService, NotificationService notificationService, AuthenticationService authService)
        {
            InitializeComponent();
            _backlogService = backlogService;
            _notificationService = notificationService;
            _authService = authService;

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
            var urgentes = toutes
                .Where(t => t.Priorite == Priorite.Urgent || 
                           (t.DateFinAttendue.HasValue && (t.DateFinAttendue.Value - DateTime.Now).TotalDays <= 2))
                .Where(t => t.Statut != Statut.Termine)
                .OrderBy(t => t.DateFinAttendue)
                .Take(4)
                .ToList();
            TachesUrgentes = new ObservableCollection<BacklogItem>(urgentes);

            // Notifications récentes (5 dernières non lues)
            var notifications = _notificationService.GetNotificationsNonLues()
                .OrderByDescending(n => n.DateCreation)
                .Take(5)
                .Select(n => new NotificationViewModel
                {
                    Titre = n.Titre,
                    Message = n.Message,
                    Icone = GetIconeNotification(n.Type)
                })
                .ToList();
            NotificationsRecentes = new ObservableCollection<NotificationViewModel>(notifications);

            // Statistiques
            var allTaches = toutes.ToList();
            NbTachesTotal = allTaches.Count;
            NbTachesTerminees = allTaches.Count(t => t.Statut == Statut.Termine);
            NbTachesEnCours = allTaches.Count(t => t.Statut == Statut.EnCours);
            
            var projets = _backlogService.GetAllProjets();
            NbProjetsActifs = projets.Count(p => p.Actif);
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
            if (border?.DataContext is BacklogItem tache)
            {
                var mainWindow = Window.GetWindow(this) as MainWindow;
                mainWindow?.AfficherBacklog();
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
    }
}

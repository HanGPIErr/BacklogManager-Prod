using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BacklogManager.Services;

namespace BacklogManager.Views
{
    public partial class NotificationsView : UserControl
    {
        private readonly NotificationService _notificationService;
        private readonly AuthenticationService _authService;
        private readonly EmailService _emailService;
        private readonly IDatabase _database;
        private List<Notification> _toutesNotifications;

        public NotificationsView()
        {
            InitializeComponent();
            
            // Récupérer les services depuis App
            var app = Application.Current as App;
            _notificationService = app?.NotificationService;
            _authService = app?.AuthService;
            _emailService = app?.EmailService;
            _database = app?.Database;
            
            InitialiserTextes();
            
            // Charger après que tous les contrôles soient initialisés
            Loaded += (s, e) => ChargerNotifications();
        }

        private void InitialiserTextes()
        {
            var loc = LocalizationService.Instance;

            // Titre et compteur
            TxtTitle.Text = loc.GetString("Notifications_Title");
            MettreAJourCompteur(); // Cela utilisera la traduction

            // Boutons d'action
            BtnMarquerToutesLues.Content = loc.GetString("Notifications_MarkAllRead");
            BtnSupprimerLues.Content = loc.GetString("Notifications_DeleteRead");

            // Filtres
            TxtFilter.Text = loc.GetString("Notifications_Filter");
            RadioTous.Content = loc.GetString("Notifications_All");
            RadioNonLues.Content = loc.GetString("Notifications_UnreadOnly");
            ChkUrgent.Content = loc.GetString("Notifications_Urgent");
            ChkAttention.Content = loc.GetString("Notifications_Attention");
            ChkInfo.Content = loc.GetString("Notifications_Info");
            ChkSuccess.Content = loc.GetString("Notifications_Success");

            // Message vide
            TxtNoNotifications.Text = loc.GetString("Notifications_NoNotifications");
            TxtUpToDate.Text = loc.GetString("Notifications_UpToDate");

            // Écouter les changements de langue
            loc.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "Item[]")
                {
                    InitialiserTextes();
                }
            };
        }

        private void ChargerNotifications()
        {
            if (_notificationService == null || _authService == null || _database == null) return;
            
            // Récupérer les notifications pour l'utilisateur connecté
            var utilisateurId = _authService.CurrentUser?.Id ?? 0;
            if (utilisateurId > 0)
            {
                _toutesNotifications = _database.GetNotificationsByUtilisateur(utilisateurId);
            }
            else
            {
                _toutesNotifications = _notificationService.GetAllNotifications();
            }
            
            AppliquerFiltres();
            MettreAJourCompteur();
        }

        private void AppliquerFiltres()
        {
            // Vérifier que les données sont chargées
            if (_toutesNotifications == null)
                return;
                
            var notifications = _toutesNotifications;

            // Filtre Lues/Non lues
            if (RadioNonLues.IsChecked == true)
            {
                notifications = notifications.Where(n => !n.EstLue).ToList();
            }

            // Filtres par type
            var typesSelectionnes = new List<NotificationType>();
            if (ChkUrgent.IsChecked == true) typesSelectionnes.Add(NotificationType.Urgent);
            if (ChkAttention.IsChecked == true) typesSelectionnes.Add(NotificationType.Attention);
            if (ChkInfo.IsChecked == true) typesSelectionnes.Add(NotificationType.Info);
            if (ChkSuccess.IsChecked == true) typesSelectionnes.Add(NotificationType.Success);

            if (typesSelectionnes.Count > 0)
            {
                notifications = notifications.Where(n => typesSelectionnes.Contains(n.Type)).ToList();
            }

            // Afficher les résultats
            ListeNotifications.ItemsSource = notifications;
            MessageVide.Visibility = notifications.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void MettreAJourCompteur()
        {
            if (_notificationService == null) return;
            
            int count = _notificationService.GetCountNotificationsNonLues();
            TxtCountNotifications.Text = string.Format(LocalizationService.Instance.GetString("Notifications_UnreadCount"), count);
        }

        private void Filtre_Changed(object sender, RoutedEventArgs e)
        {
            AppliquerFiltres();
        }

        private void Notification_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_notificationService == null) return;
            
            var border = sender as Border;
            if (border?.DataContext is Notification notification)
            {
                // Marquer comme lue
                if (!notification.EstLue)
                {
                    _notificationService.MarquerCommeLue(notification.Id);
                    ChargerNotifications(); // Rafraîchir
                }

                // Si associée à une tâche, ouvrir les détails (optionnel)
                if (notification.Tache != null)
                {
                    var loc = LocalizationService.Instance;
                    MessageBox.Show($"{loc.GetString("Notifications_Task")}: {notification.Tache.Titre}\n\n{loc.GetString("Notifications_Status")}: {notification.Tache.Statut}\n{loc.GetString("Notifications_Priority")}: {notification.Tache.Priorite}",
                        loc.GetString("Notifications_TaskDetails"), MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void BtnSupprimerNotification_Click(object sender, RoutedEventArgs e)
        {
            if (_notificationService == null) return;
            
            var button = sender as Button;
            if (button?.Tag is int notificationId)
            {
                _notificationService.SupprimerNotification(notificationId);
                ChargerNotifications();
            }
        }

        private void BtnMarquerToutesLues_Click(object sender, RoutedEventArgs e)
        {
            if (_notificationService == null) return;
            
            _notificationService.MarquerToutesCommeLues();
            ChargerNotifications();
        }

        private void BtnSupprimerLues_Click(object sender, RoutedEventArgs e)
        {
            if (_notificationService == null) return;
            
            var result = MessageBox.Show(LocalizationService.Instance.GetString("Notifications_ConfirmDeleteRead"),
                LocalizationService.Instance.GetString("Common_Confirmation"), MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                _notificationService.SupprimerToutesLues();
                ChargerNotifications();
            }
        }

        private void BtnEnvoyerEmail_Click(object sender, RoutedEventArgs e)
        {
            if (_notificationService == null || _emailService == null) return;
            
            var button = sender as Button;
            if (button?.Tag is Notification notification)
            {
                // Vérifier si l'email peut être envoyé
                if (!_emailService.PeutEnvoyerEmail(notification))
                {
                    var loc = LocalizationService.Instance;
                    MessageBox.Show(
                        loc.GetString("Notifications_CannotSendEmail"),
                        loc.GetString("Notifications_EmailUnavailable"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                // Envoyer l'email via Outlook
                _emailService.EnvoyerNotificationTache(notification);
                
                // Marquer la notification comme lue
                if (!notification.EstLue)
                {
                    _notificationService.MarquerCommeLue(notification.Id);
                    ChargerNotifications();
                }
            }
        }
        
        private void BtnAnnulerDemande_Click(object sender, RoutedEventArgs e)
        {
            if (_database == null || _authService == null) return;
            
            var button = sender as Button;
            if (button?.Tag is Notification notification && notification.DemandeEchangeVMId.HasValue)
            {
                var result = MessageBox.Show(LocalizationService.Instance.GetString("Notifications_ConfirmCancelRequest"),
                    LocalizationService.Instance.GetString("Common_Confirmation"), MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        _database.AnnulerDemandeEchangeVM(notification.DemandeEchangeVMId.Value);
                        MessageBox.Show(LocalizationService.Instance.GetString("Notifications_CancelSuccess"),
                            LocalizationService.Instance.GetString("Notifications_CancelSuccessTitle"), MessageBoxButton.OK, MessageBoxImage.Information);
                        ChargerNotifications();
                    }
                    catch (System.Exception ex)
                    {
                        MessageBox.Show($"{LocalizationService.Instance.GetString("Notifications_CancelError")}: {ex.Message}",
                            LocalizationService.Instance.GetString("Common_Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
    }
}

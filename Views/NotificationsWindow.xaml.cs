using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BacklogManager.Services;

namespace BacklogManager.Views
{
    public partial class NotificationsWindow : Window
    {
        private readonly NotificationService _notificationService;
        private List<Notification> _toutesNotifications;

        public NotificationsWindow(NotificationService notificationService)
        {
            InitializeComponent();
            _notificationService = notificationService;
            
            // Charger après que tous les contrôles soient initialisés
            Loaded += (s, e) => ChargerNotifications();
        }

        private void ChargerNotifications()
        {
            _toutesNotifications = _notificationService.GetAllNotifications();
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
            int count = _notificationService.GetCountNotificationsNonLues();
            TxtCountNotifications.Text = $"{count} notification(s) non lue(s)";
        }

        private void Filtre_Changed(object sender, RoutedEventArgs e)
        {
            AppliquerFiltres();
        }

        private void Notification_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
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
                    MessageBox.Show($"Tâche: {notification.Tache.Titre}\n\nStatut: {notification.Tache.Statut}\nPriorité: {notification.Tache.Priorite}",
                        "Détails de la tâche", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void BtnSupprimerNotification_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is int notificationId)
            {
                _notificationService.SupprimerNotification(notificationId);
                ChargerNotifications();
            }
        }

        private void BtnMarquerToutesLues_Click(object sender, RoutedEventArgs e)
        {
            _notificationService.MarquerToutesCommeLues();
            ChargerNotifications();
        }

        private void BtnSupprimerLues_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Voulez-vous vraiment supprimer toutes les notifications lues ?",
                "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                _notificationService.SupprimerToutesLues();
                ChargerNotifications();
            }
        }

        private void BtnActualiser_Click(object sender, RoutedEventArgs e)
        {
            // Ré-analyser le backlog pour générer de nouvelles notifications
            _notificationService.AnalyserEtGenererNotifications();
            ChargerNotifications();
        }

        private void BtnFermer_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

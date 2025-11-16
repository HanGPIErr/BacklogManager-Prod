using System;
using System.Collections.Generic;
using System.Linq;
using BacklogManager.Domain;

namespace BacklogManager.Services
{
    public enum NotificationType
    {
        Urgent,          // Retard critique
        Attention,       // Échéance proche (< 2 jours)
        Info,            // Information générale
        Success          // Tâche terminée récemment
    }

    public class Notification
    {
        public int Id { get; set; }
        public string Titre { get; set; }
        public string Message { get; set; }
        public NotificationType Type { get; set; }
        public DateTime DateCreation { get; set; }
        public bool EstLue { get; set; }
        public int? TacheId { get; set; }
        public BacklogItem Tache { get; set; }

        public string IconeEmoji
        {
            get
            {
                if (Type == NotificationType.Urgent) return "🔴";
                if (Type == NotificationType.Attention) return "🟠";
                if (Type == NotificationType.Info) return "🔵";
                if (Type == NotificationType.Success) return "✅";
                return "ℹ️";
            }
        }

        public string CouleurFond
        {
            get
            {
                if (Type == NotificationType.Urgent) return "#FFEBEE";
                if (Type == NotificationType.Attention) return "#FFF3E0";
                if (Type == NotificationType.Info) return "#E3F2FD";
                if (Type == NotificationType.Success) return "#E8F5E9";
                return "#F5F5F5";
            }
        }

        public string CouleurTexte
        {
            get
            {
                if (Type == NotificationType.Urgent) return "#C62828";
                if (Type == NotificationType.Attention) return "#E65100";
                if (Type == NotificationType.Info) return "#1565C0";
                if (Type == NotificationType.Success) return "#00915A";
                return "#333";
            }
        }
    }

    public class NotificationService
    {
        private readonly BacklogService _backlogService;
        private List<Notification> _notifications;
        private int _nextId = 1;

        public NotificationService(BacklogService backlogService)
        {
            _backlogService = backlogService;
            _notifications = new List<Notification>();
        }

        public List<Notification> GetAllNotifications()
        {
            return _notifications.OrderByDescending(n => n.DateCreation).ToList();
        }

        public List<Notification> GetNotificationsNonLues()
        {
            return _notifications.Where(n => !n.EstLue)
                                 .OrderByDescending(n => n.DateCreation)
                                 .ToList();
        }

        public int GetCountNotificationsNonLues()
        {
            return _notifications.Count(n => !n.EstLue);
        }

        public void MarquerCommeLue(int notificationId)
        {
            var notification = _notifications.FirstOrDefault(n => n.Id == notificationId);
            if (notification != null)
            {
                notification.EstLue = true;
            }
        }

        public void MarquerToutesCommeLues()
        {
            foreach (var notification in _notifications)
            {
                notification.EstLue = true;
            }
        }

        public void SupprimerNotification(int notificationId)
        {
            _notifications.RemoveAll(n => n.Id == notificationId);
        }

        public void SupprimerToutesLues()
        {
            _notifications.RemoveAll(n => n.EstLue);
        }

        /// <summary>
        /// Analyse le backlog et génère les notifications pertinentes
        /// </summary>
        public void AnalyserEtGenererNotifications()
        {
            var taches = _backlogService.GetAllBacklogItems();
            var aujourdhui = DateTime.Now;

            foreach (var tache in taches)
            {
                // Ignorer les tâches terminées
                if (tache.Statut == Statut.Termine)
                    continue;

                // URGENT: Tâches avec date limite dépassée
                if (tache.DateFinAttendue.HasValue && tache.DateFinAttendue.Value.Date < aujourdhui.Date)
                {
                    int joursRetard = (aujourdhui.Date - tache.DateFinAttendue.Value.Date).Days;
                    
                    // Éviter les doublons: ne créer que si pas déjà notifié pour ce retard
                    if (!_notifications.Any(n => n.TacheId == tache.Id && n.Type == NotificationType.Urgent && 
                                                  n.DateCreation.Date == aujourdhui.Date))
                    {
                        AjouterNotification(new Notification
                        {
                            Titre = $"⚠️ Retard critique: {tache.Titre}",
                            Message = $"Cette tâche a {joursRetard} jour(s) de retard (Échéance: {tache.DateFinAttendue.Value:dd/MM/yyyy})",
                            Type = NotificationType.Urgent,
                            TacheId = tache.Id,
                            Tache = tache
                        });
                    }
                }
                // ATTENTION: Tâches avec échéance dans moins de 2 jours
                else if (tache.DateFinAttendue.HasValue)
                {
                    int joursRestants = (tache.DateFinAttendue.Value.Date - aujourdhui.Date).Days;
                    
                    if (joursRestants >= 0 && joursRestants <= 2)
                    {
                        if (!_notifications.Any(n => n.TacheId == tache.Id && n.Type == NotificationType.Attention &&
                                                      n.DateCreation.Date == aujourdhui.Date))
                        {
                            AjouterNotification(new Notification
                            {
                                Titre = $"📅 Échéance proche: {tache.Titre}",
                                Message = $"Cette tâche doit être terminée dans {joursRestants} jour(s) (Échéance: {tache.DateFinAttendue.Value:dd/MM/yyyy})",
                                Type = NotificationType.Attention,
                                TacheId = tache.Id,
                                Tache = tache
                            });
                        }
                    }
                }

                // INFO: Tâches urgentes non assignées
                if (tache.Priorite == Priorite.Urgent && !tache.DevAssigneId.HasValue)
                {
                    if (!_notifications.Any(n => n.TacheId == tache.Id && n.Titre.Contains("non assignée")))
                    {
                        AjouterNotification(new Notification
                        {
                            Titre = $"🚨 Tâche urgente non assignée: {tache.Titre}",
                            Message = "Cette tâche prioritaire n'a pas encore de développeur assigné",
                            Type = NotificationType.Info,
                            TacheId = tache.Id,
                            Tache = tache
                        });
                    }
                }
            }

            // SUCCESS: Tâches terminées récemment (aujourd'hui)
            var tachesTermineesAujourdhui = taches.Where(t => t.Statut == Statut.Termine &&
                                                                t.DateDerniereMaj != null &&
                                                                t.DateDerniereMaj.Date == aujourdhui.Date)
                                                   .ToList();

            foreach (var tache in tachesTermineesAujourdhui)
            {
                if (!_notifications.Any(n => n.TacheId == tache.Id && n.Type == NotificationType.Success))
                {
                    AjouterNotification(new Notification
                    {
                        Titre = $"✅ Tâche terminée: {tache.Titre}",
                        Message = $"Félicitations ! Cette tâche a été complétée",
                        Type = NotificationType.Success,
                        TacheId = tache.Id,
                        Tache = tache
                    });
                }
            }
        }

        private void AjouterNotification(Notification notification)
        {
            notification.Id = _nextId++;
            notification.DateCreation = DateTime.Now;
            notification.EstLue = false;
            _notifications.Add(notification);
        }
    }
}

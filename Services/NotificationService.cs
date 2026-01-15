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
        public int? UtilisateurId { get; set; }  // ID de l'utilisateur destinataire (NULL = tous)
        public int? DemandeEchangeVMId { get; set; }  // ID de la demande d'échange VM associée
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

        public string ImageCaramelFlopy
        {
            get
            {
                return "/Images/agent-project-and-change.png";
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

        public string BadgeNouveau
        {
            get
            {
                return LocalizationService.Instance.GetString("Notifications_NewBadge");
            }
        }

        // Propriétés pour affichage traduit dynamique
        public string TitreAffiche
        {
            get
            {
                if (string.IsNullOrEmpty(Titre)) return Titre;

                // Extraire le nom de la tâche du titre (après ":" )
                string nomTache = "";
                if (Titre.Contains(":"))
                {
                    nomTache = Titre.Substring(Titre.IndexOf(":") + 1).Trim();
                }

                // Détecter le type de notification par le contenu ou l'emoji
                if (Titre.Contains("⚠️") || Titre.Contains("Retard critique") || Titre.Contains("Critical delay") || Titre.Contains("Retraso crítico"))
                {
                    return string.Format(LocalizationService.Instance.GetString("Notification_CriticalDelay"), nomTache);
                }
                else if (Titre.Contains("📅") || Titre.Contains("Échéance proche") || Titre.Contains("Deadline approaching") || Titre.Contains("Plazo próximo"))
                {
                    return string.Format(LocalizationService.Instance.GetString("Notification_DeadlineNear"), nomTache);
                }
                else if (Titre.Contains("🚨") || Titre.Contains("urgente non assignée") || Titre.Contains("Urgent unassigned") || Titre.Contains("urgente sin asignar"))
                {
                    return string.Format(LocalizationService.Instance.GetString("Notification_UrgentUnassigned"), nomTache);
                }
                else if (Titre.Contains("✅") || Titre.Contains("terminée") || Titre.Contains("completed") || Titre.Contains("completada"))
                {
                    return string.Format(LocalizationService.Instance.GetString("Notification_TaskCompleted"), nomTache);
                }

                // Si aucune correspondance, retourner le titre original
                return Titre;
            }
        }

        public string MessageAffiche
        {
            get
            {
                if (string.IsNullOrEmpty(Message)) return Message;

                // Extraire les nombres du message (jours de retard, jours restants, dates)
                var match = System.Text.RegularExpressions.Regex.Match(Message, @"(\d+)\s*jour");
                int jours = match.Success ? int.Parse(match.Groups[1].Value) : 0;

                var matchDate = System.Text.RegularExpressions.Regex.Match(Message, @"(\d{2}/\d{2}/\d{4})");
                string date = matchDate.Success ? matchDate.Groups[1].Value : "";

                // Détecter le type de message
                if (Message.Contains("jour(s) de retard") || Message.Contains("day(s) late") || Message.Contains("día(s) de retraso"))
                {
                    return string.Format(LocalizationService.Instance.GetString("Notification_DelayMessage"), jours, date);
                }
                else if (Message.Contains("doit être terminée") || Message.Contains("must be completed") || Message.Contains("debe completarse") ||
                         Message.Contains("jour(s) restant") || Message.Contains("day(s) remaining") || Message.Contains("día(s) restante"))
                {
                    return string.Format(LocalizationService.Instance.GetString("Notification_DeadlineMessage"), jours, date);
                }
                else if (Message.Contains("Aucun développeur") || Message.Contains("No developer") || Message.Contains("Ningún desarrollador"))
                {
                    return LocalizationService.Instance.GetString("Notification_UnassignedMessage");
                }
                else if (Message.Contains("a été marquée comme terminée") || Message.Contains("has been marked as completed") || Message.Contains("ha sido marcada como completada"))
                {
                    return LocalizationService.Instance.GetString("Notification_CompletedMessage");
                }

                // Si aucune correspondance, retourner le message original
                return Message;
            }
        }
    }

    public class NotificationService
    {
        private readonly BacklogService _backlogService;
        private readonly IDatabase _database;

        public NotificationService(BacklogService backlogService, IDatabase database)
        {
            _backlogService = backlogService;
            _database = database;
        }

        public List<Notification> GetAllNotifications()
        {
            return _database.GetNotifications();
        }

        public List<Notification> GetNotificationsNonLues()
        {
            return _database.GetNotifications()
                            .Where(n => !n.EstLue)
                            .OrderByDescending(n => n.DateCreation)
                            .ToList();
        }

        public int GetCountNotificationsNonLues()
        {
            return _database.GetNotifications().Count(n => !n.EstLue);
        }

        public void MarquerCommeLue(int notificationId)
        {
            _database.MarquerNotificationCommeLue(notificationId);
        }

        public void MarquerToutesCommeLues()
        {
            _database.MarquerToutesNotificationsCommeLues();
        }

        public void SupprimerNotification(int notificationId)
        {
            _database.DeleteNotification(notificationId);
        }

        public void SupprimerToutesLues()
        {
            _database.DeleteNotificationsLues();
        }

        /// <summary>
        /// Analyse le backlog et génère les notifications pertinentes
        /// </summary>
        public void AnalyserEtGenererNotifications()
        {
            var taches = _backlogService.GetAllBacklogItems();
            var aujourdhui = DateTime.Now;
            var notificationsExistantes = _database.GetNotifications();

            foreach (var tache in taches)
            {
                // Ignorer les tâches terminées
                if (tache.Statut == Statut.Termine)
                    continue;

                // Ignorer les tâches spéciales (Congés, Non travaillé, Support)
                if (tache.TypeDemande == TypeDemande.Conges || 
                    tache.TypeDemande == TypeDemande.NonTravaille || 
                    tache.TypeDemande == TypeDemande.Support)
                    continue;

                // URGENT: Tâches avec date limite dépassée
                if (tache.DateFinAttendue.HasValue && tache.DateFinAttendue.Value.Date < aujourdhui.Date)
                {
                    int joursRetard = (aujourdhui.Date - tache.DateFinAttendue.Value.Date).Days;
                    
                    // Éviter les doublons: ne créer que si pas déjà notifié pour ce retard
                    if (!notificationsExistantes.Any(n => n.TacheId == tache.Id && n.Type == NotificationType.Urgent && 
                                                  n.DateCreation.Date == aujourdhui.Date))
                    {
                        AjouterNotification(new Notification
                        {
                            Titre = string.Format(LocalizationService.Instance.GetString("Notification_CriticalDelay"), tache.Titre),
                            Message = string.Format(LocalizationService.Instance.GetString("Notification_DelayMessage"), joursRetard, tache.DateFinAttendue.Value.ToString("dd/MM/yyyy")),
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
                        if (!notificationsExistantes.Any(n => n.TacheId == tache.Id && n.Type == NotificationType.Attention &&
                                                      n.DateCreation.Date == aujourdhui.Date))
                        {
                            AjouterNotification(new Notification
                            {
                                Titre = string.Format(LocalizationService.Instance.GetString("Notification_DeadlineNear"), tache.Titre),
                                Message = string.Format(LocalizationService.Instance.GetString("Notification_DeadlineMessage"), joursRestants, tache.DateFinAttendue.Value.ToString("dd/MM/yyyy")),
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
                    if (!notificationsExistantes.Any(n => n.TacheId == tache.Id && n.Titre.Contains(LocalizationService.Instance.GetString("Notification_Unassigned"))))
                    {
                        AjouterNotification(new Notification
                        {
                            Titre = string.Format(LocalizationService.Instance.GetString("Notification_UrgentUnassigned"), tache.Titre),
                            Message = LocalizationService.Instance.GetString("Notification_UnassignedMessage"),
                            Type = NotificationType.Info,
                            TacheId = tache.Id,
                            Tache = tache
                        });
                    }
                }
            }

            // SUCCESS: Tâches terminées récemment (aujourd'hui) - exclure tâches spéciales
            var tachesTermineesAujourdhui = taches.Where(t => t.Statut == Statut.Termine &&
                                                                t.DateDerniereMaj != null &&
                                                                t.DateDerniereMaj.Date == aujourdhui.Date &&
                                                                t.TypeDemande != TypeDemande.Conges &&
                                                                t.TypeDemande != TypeDemande.NonTravaille &&
                                                                t.TypeDemande != TypeDemande.Support)
                                                   .ToList();

            foreach (var tache in tachesTermineesAujourdhui)
            {
                if (!notificationsExistantes.Any(n => n.TacheId == tache.Id && n.Type == NotificationType.Success))
                {
                    AjouterNotification(new Notification
                    {
                        Titre = string.Format(LocalizationService.Instance.GetString("Notification_TaskCompleted"), tache.Titre),
                        Message = LocalizationService.Instance.GetString("Notification_CompletedMessage"),
                        Type = NotificationType.Success,
                        TacheId = tache.Id,
                        Tache = tache
                    });
                }
            }
        }

        private void AjouterNotification(Notification notification)
        {
            notification.DateCreation = DateTime.Now;
            notification.EstLue = false;
            _database.AddOrUpdateNotification(notification);
        }
    }
}

using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using BacklogManager.Domain;

namespace BacklogManager.Services
{
    public class EmailService
    {
        private readonly BacklogService _backlogService;
        private readonly AuthenticationService _authService;

        public EmailService(BacklogService backlogService, AuthenticationService authService)
        {
            _backlogService = backlogService;
            _authService = authService;
        }

        /// <summary>
        /// Envoie une notification par email concernant une tâche via Outlook
        /// </summary>
        public void EnvoyerNotificationTache(Notification notification)
        {
            if (notification == null || notification.TacheId == null)
                return;

            var tache = _backlogService.GetBacklogItemById(notification.TacheId.Value);
            if (tache == null)
                return;

            // Récupérer le dev assigné
            Utilisateur devAssigne = null;
            if (tache.DevAssigneId.HasValue)
            {
                var utilisateurs = _backlogService.GetAllUtilisateurs();
                devAssigne = utilisateurs.FirstOrDefault(u => u.Id == tache.DevAssigneId.Value);
            }

            if (devAssigne == null || string.IsNullOrEmpty(devAssigne.Email))
            {
                System.Windows.MessageBox.Show(
                    "Impossible d'envoyer l'email : aucun développeur assigné ou email manquant.",
                    "Erreur",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

            // Construire le sujet
            string sujet = GenererSujet(notification, tache);

            // Construire le corps du message
            string corps = GenererCorpsMessage(notification, tache, devAssigne);

            // Ouvrir Outlook avec le brouillon pré-rempli
            OuvrirBrouillonOutlook(devAssigne.Email, sujet, corps);
        }

        private string GenererSujet(Notification notification, BacklogItem tache)
        {
            string prefixe = notification.Type switch
            {
                NotificationType.Urgent => "🔴 URGENT",
                NotificationType.Attention => "⚠️ ATTENTION",
                NotificationType.Success => "✅ Félicitations",
                _ => "📋 Information"
            };

            return $"{prefixe} - {tache.Titre}";
        }

        private string GenererCorpsMessage(Notification notification, BacklogItem tache, Utilisateur devAssigne)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"Bonjour {devAssigne.Prenom},");
            sb.AppendLine();

            // Message principal selon le type
            switch (notification.Type)
            {
                case NotificationType.Urgent:
                    sb.AppendLine("⚠️ Cette tâche nécessite votre attention immédiate !");
                    sb.AppendLine();
                    sb.AppendLine($"**Problème détecté :** {notification.Message}");
                    break;

                case NotificationType.Attention:
                    sb.AppendLine("📌 Un rappel concernant l'une de vos tâches :");
                    sb.AppendLine();
                    sb.AppendLine($"**Information :** {notification.Message}");
                    break;

                case NotificationType.Success:
                    sb.AppendLine("🎉 Félicitations pour votre excellent travail !");
                    sb.AppendLine();
                    sb.AppendLine($"**Message :** {notification.Message}");
                    break;

                default:
                    sb.AppendLine($"**Information :** {notification.Message}");
                    break;
            }

            sb.AppendLine();
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine("📋 DÉTAILS DE LA TÂCHE");
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine();
            sb.AppendLine($"• **Titre :** {tache.Titre}");
            sb.AppendLine($"• **ID :** #{tache.Id}");
            sb.AppendLine($"• **Statut :** {tache.Statut}");
            sb.AppendLine($"• **Priorité :** {tache.Priorite}");
            
            if (tache.DateFinAttendue.HasValue)
            {
                sb.AppendLine($"• **Échéance :** {tache.DateFinAttendue.Value:dd/MM/yyyy}");
                
                var joursRestants = (tache.DateFinAttendue.Value - DateTime.Now).Days;
                if (joursRestants < 0)
                    sb.AppendLine($"  ⚠️ **RETARD de {Math.Abs(joursRestants)} jour(s)**");
                else if (joursRestants <= 2)
                    sb.AppendLine($"  ⏰ **{joursRestants} jour(s) restant(s)**");
            }

            if (tache.ChiffrageHeures.HasValue)
            {
                sb.AppendLine($"• **Estimation :** {tache.ChiffrageHeures.Value:F1}h");
            }

            if (!string.IsNullOrEmpty(tache.Description))
            {
                sb.AppendLine();
                sb.AppendLine($"• **Description :**");
                sb.AppendLine($"  {tache.Description}");
            }

            sb.AppendLine();
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine();

            // Call to action selon le type
            switch (notification.Type)
            {
                case NotificationType.Urgent:
                    sb.AppendLine("🚨 **Action requise :** Merci de traiter cette tâche en priorité et de mettre à jour son statut.");
                    break;

                case NotificationType.Attention:
                    sb.AppendLine("👉 **Action suggérée :** Pensez à vérifier l'avancement de cette tâche et à la mettre à jour si nécessaire.");
                    break;

                case NotificationType.Success:
                    sb.AppendLine("✨ Continue comme ça ! Ton travail est apprécié par toute l'équipe.");
                    break;

                default:
                    sb.AppendLine("📌 Merci de prendre connaissance de cette information.");
                    break;
            }

            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine($"📧 Email automatique envoyé depuis BacklogManager BNP Paribas");
            sb.AppendLine($"⏰ Date : {DateTime.Now:dd/MM/yyyy à HH:mm}");
            
            var expediteur = _authService.CurrentUser;
            if (expediteur != null)
            {
                sb.AppendLine($"👤 Expéditeur : {expediteur.Prenom} {expediteur.Nom}");
            }

            return sb.ToString();
        }

        private void OuvrirBrouillonOutlook(string destinataire, string sujet, string corps)
        {
            try
            {
                // Encoder les paramètres pour l'URL mailto
                string mailto = $"mailto:{Uri.EscapeDataString(destinataire)}" +
                               $"?subject={Uri.EscapeDataString(sujet)}" +
                               $"&body={Uri.EscapeDataString(corps)}";

                // Ouvrir Outlook avec le brouillon
                Process.Start(new ProcessStartInfo
                {
                    FileName = mailto,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Erreur lors de l'ouverture d'Outlook :\n{ex.Message}\n\nVérifiez qu'Outlook est installé et configuré.",
                    "Erreur Outlook",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Vérifie si une notification peut être envoyée par email
        /// </summary>
        public bool PeutEnvoyerEmail(Notification notification)
        {
            if (notification == null || notification.TacheId == null)
                return false;

            var tache = _backlogService.GetBacklogItemById(notification.TacheId.Value);
            if (tache == null || !tache.DevAssigneId.HasValue)
                return false;

            var utilisateurs = _backlogService.GetAllUtilisateurs();
            var dev = utilisateurs.FirstOrDefault(u => u.Id == tache.DevAssigneId.Value);
            return dev != null && !string.IsNullOrEmpty(dev.Email);
        }
    }
}

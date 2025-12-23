using System;
using System.Collections.Generic;
using System.Threading;
using BacklogManager.Domain;

namespace BacklogManager.Services
{
    /// <summary>
    /// Wrapper autour de la base de données avec gestion des retry et séparation readonly/write
    /// </summary>
    public class DatabaseWrapper : IDatabase
    {
        private readonly IDatabase _database;
        private readonly int _maxRetries = 3;
        private readonly int _retryDelayMs = 100;

        public DatabaseWrapper(IDatabase database)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
        }

        /// <summary>
        /// Exécute une opération de lecture avec retry automatique
        /// </summary>
        private T ExecuteReadWithRetry<T>(Func<T> operation, string operationName)
        {
            int attempt = 0;
            Exception lastException = null;

            while (attempt < _maxRetries)
            {
                try
                {
                    return operation();
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    attempt++;
                    
                    LoggingService.Instance.LogWarning($"Tentative {attempt}/{_maxRetries} échouée pour '{operationName}': {ex.Message}");
                    
                    if (attempt < _maxRetries)
                    {
                        Thread.Sleep(_retryDelayMs * attempt); // Backoff exponentiel
                    }
                }
            }

            LoggingService.Instance.LogError($"Échec de l'opération de lecture '{operationName}' après {_maxRetries} tentatives", lastException);
            throw new Exception($"Échec de l'opération de lecture '{operationName}' après {_maxRetries} tentatives", lastException);
        }

        /// <summary>
        /// Exécute une opération d'écriture avec retry automatique
        /// </summary>
        private T ExecuteWriteWithRetry<T>(Func<T> operation, string operationName)
        {
            int attempt = 0;
            Exception lastException = null;

            while (attempt < _maxRetries)
            {
                try
                {
                    return operation();
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    attempt++;
                    
                    LoggingService.Instance.LogWarning($"Tentative {attempt}/{_maxRetries} échouée pour '{operationName}': {ex.Message}");
                    
                    if (attempt < _maxRetries)
                    {
                        Thread.Sleep(_retryDelayMs * attempt); // Backoff exponentiel
                    }
                }
            }

            LoggingService.Instance.LogError($"Échec de l'opération d'écriture '{operationName}' après {_maxRetries} tentatives", lastException);
            throw new Exception($"Échec de l'opération d'écriture '{operationName}' après {_maxRetries} tentatives", lastException);
        }

        /// <summary>
        /// Exécute une opération d'écriture void avec retry automatique
        /// </summary>
        private void ExecuteWriteWithRetry(Action operation, string operationName)
        {
            ExecuteWriteWithRetry<object>(() => { operation(); return null; }, operationName);
        }

        // ========== OPÉRATIONS DE LECTURE ==========

        public List<Role> GetRoles() => ExecuteReadWithRetry(() => _database.GetRoles(), nameof(GetRoles));
        public List<Utilisateur> GetUtilisateurs() => ExecuteReadWithRetry(() => _database.GetUtilisateurs(), nameof(GetUtilisateurs));
        public List<Projet> GetProjets() => ExecuteReadWithRetry(() => _database.GetProjets(), nameof(GetProjets));
        public List<Sprint> GetSprints() => ExecuteReadWithRetry(() => _database.GetSprints(), nameof(GetSprints));
        public List<BacklogItem> GetBacklogItems() => ExecuteReadWithRetry(() => _database.GetBacklogItems(), nameof(GetBacklogItems));
        public List<Demande> GetDemandes() => ExecuteReadWithRetry(() => _database.GetDemandes(), nameof(GetDemandes));
        public List<Dev> GetDevs() => ExecuteReadWithRetry(() => _database.GetDevs(), nameof(GetDevs));
        public List<Commentaire> GetCommentaires() => ExecuteReadWithRetry(() => _database.GetCommentaires(), nameof(GetCommentaires));
        public List<HistoriqueModification> GetHistoriqueModifications() => ExecuteReadWithRetry(() => _database.GetHistoriqueModifications(), nameof(GetHistoriqueModifications));
        public List<PokerSession> GetPokerSessions() => ExecuteReadWithRetry(() => _database.GetPokerSessions(), nameof(GetPokerSessions));
        public List<PokerVote> GetPokerVotes() => ExecuteReadWithRetry(() => _database.GetPokerVotes(), nameof(GetPokerVotes));
        public List<Disponibilite> GetDisponibilites() => ExecuteReadWithRetry(() => _database.GetDisponibilites(), nameof(GetDisponibilites));
        public List<BacklogItem> GetBacklog() => ExecuteReadWithRetry(() => _database.GetBacklog(), nameof(GetBacklog));
        public List<BacklogItem> GetAllBacklogItemsIncludingArchived() => ExecuteReadWithRetry(() => _database.GetAllBacklogItemsIncludingArchived(), nameof(GetAllBacklogItemsIncludingArchived));
        public List<AuditLog> GetAuditLogs() => ExecuteReadWithRetry(() => _database.GetAuditLogs(), nameof(GetAuditLogs));
        public List<CRA> GetCRAs(int? backlogItemId = null, int? devId = null, DateTime? dateDebut = null, DateTime? dateFin = null) 
            => ExecuteReadWithRetry(() => _database.GetCRAs(backlogItemId, devId, dateDebut, dateFin), nameof(GetCRAs));
        public List<CRA> GetAllCRAs() => ExecuteReadWithRetry(() => _database.GetAllCRAs(), nameof(GetAllCRAs));

        // ========== OPÉRATIONS D'ÉCRITURE ==========

        public Role AddOrUpdateRole(Role role) => ExecuteWriteWithRetry(() => _database.AddOrUpdateRole(role), nameof(AddOrUpdateRole));
        public void UpdateRole(Role role) => ExecuteWriteWithRetry(() => _database.UpdateRole(role), nameof(UpdateRole));
        public Utilisateur AddOrUpdateUtilisateur(Utilisateur utilisateur) => ExecuteWriteWithRetry(() => _database.AddOrUpdateUtilisateur(utilisateur), nameof(AddOrUpdateUtilisateur));
        public void AddUtilisateur(Utilisateur utilisateur) => ExecuteWriteWithRetry(() => _database.AddUtilisateur(utilisateur), nameof(AddUtilisateur));
        public void UpdateUtilisateur(Utilisateur utilisateur) => ExecuteWriteWithRetry(() => _database.UpdateUtilisateur(utilisateur), nameof(UpdateUtilisateur));
        public void DeleteUtilisateur(int id) => ExecuteWriteWithRetry(() => _database.DeleteUtilisateur(id), nameof(DeleteUtilisateur));
        public Projet AddOrUpdateProjet(Projet projet) => ExecuteWriteWithRetry(() => _database.AddOrUpdateProjet(projet), nameof(AddOrUpdateProjet));
        public Sprint AddOrUpdateSprint(Sprint sprint) => ExecuteWriteWithRetry(() => _database.AddOrUpdateSprint(sprint), nameof(AddOrUpdateSprint));
        public BacklogItem AddOrUpdateBacklogItem(BacklogItem item) => ExecuteWriteWithRetry(() => _database.AddOrUpdateBacklogItem(item), nameof(AddOrUpdateBacklogItem));
        public Demande AddOrUpdateDemande(Demande demande) => ExecuteWriteWithRetry(() => _database.AddOrUpdateDemande(demande), nameof(AddOrUpdateDemande));
        public void DeleteDemande(int id) => ExecuteWriteWithRetry(() => _database.DeleteDemande(id), nameof(DeleteDemande));
        public Dev AddOrUpdateDev(Dev dev) => ExecuteWriteWithRetry(() => _database.AddOrUpdateDev(dev), nameof(AddOrUpdateDev));
        public Commentaire AddCommentaire(Commentaire commentaire) => ExecuteWriteWithRetry(() => _database.AddCommentaire(commentaire), nameof(AddCommentaire));
        public Commentaire AddOrUpdateCommentaire(Commentaire commentaire) => ExecuteWriteWithRetry(() => _database.AddOrUpdateCommentaire(commentaire), nameof(AddOrUpdateCommentaire));
        public void AddHistorique(HistoriqueModification historique) => ExecuteWriteWithRetry(() => _database.AddHistorique(historique), nameof(AddHistorique));
        public HistoriqueModification AddOrUpdateHistoriqueModification(HistoriqueModification historique) => ExecuteWriteWithRetry(() => _database.AddOrUpdateHistoriqueModification(historique), nameof(AddOrUpdateHistoriqueModification));
        public PokerSession AddOrUpdatePokerSession(PokerSession session) => ExecuteWriteWithRetry(() => _database.AddOrUpdatePokerSession(session), nameof(AddOrUpdatePokerSession));
        public PokerVote AddPokerVote(PokerVote vote) => ExecuteWriteWithRetry(() => _database.AddPokerVote(vote), nameof(AddPokerVote));
        public Disponibilite AddOrUpdateDisponibilite(Disponibilite disponibilite) => ExecuteWriteWithRetry(() => _database.AddOrUpdateDisponibilite(disponibilite), nameof(AddOrUpdateDisponibilite));
        public void AddAuditLog(AuditLog auditLog) => ExecuteWriteWithRetry(() => _database.AddAuditLog(auditLog), nameof(AddAuditLog));
        public void SaveCRA(CRA cra) => ExecuteWriteWithRetry(() => _database.SaveCRA(cra), nameof(SaveCRA));
        public void DeleteCRA(int id) => ExecuteWriteWithRetry(() => _database.DeleteCRA(id), nameof(DeleteCRA));

        // Notifications
        public List<Notification> GetNotifications() => ExecuteReadWithRetry(() => _database.GetNotifications(), nameof(GetNotifications));
        public List<Notification> GetNotificationsByUtilisateur(int utilisateurId) => ExecuteReadWithRetry(() => _database.GetNotificationsByUtilisateur(utilisateurId), nameof(GetNotificationsByUtilisateur));
        public void AddOrUpdateNotification(Notification notification) => ExecuteWriteWithRetry(() => _database.AddOrUpdateNotification(notification), nameof(AddOrUpdateNotification));
        public void DeleteNotification(int notificationId) => ExecuteWriteWithRetry(() => _database.DeleteNotification(notificationId), nameof(DeleteNotification));
        public void DeleteNotificationsLues() => ExecuteWriteWithRetry(() => _database.DeleteNotificationsLues(), nameof(DeleteNotificationsLues));
        public void SupprimerToutesLesNotifications() => ExecuteWriteWithRetry(() => _database.SupprimerToutesLesNotifications(), nameof(SupprimerToutesLesNotifications));
        public void MarquerNotificationCommeLue(int notificationId) => ExecuteWriteWithRetry(() => _database.MarquerNotificationCommeLue(notificationId), nameof(MarquerNotificationCommeLue));
        public void MarquerToutesNotificationsCommeLues() => ExecuteWriteWithRetry(() => _database.MarquerToutesNotificationsCommeLues(), nameof(MarquerToutesNotificationsCommeLues));
        
        // Chat Conversations
        public List<ChatConversation> GetChatConversations() => ExecuteReadWithRetry(() => _database.GetChatConversations(), nameof(GetChatConversations));
        public ChatConversation GetChatConversation(int conversationId) => ExecuteReadWithRetry(() => _database.GetChatConversation(conversationId), nameof(GetChatConversation));
        public int CreateChatConversation(int userId, string username) => ExecuteWriteWithRetry(() => _database.CreateChatConversation(userId, username), nameof(CreateChatConversation));
        public void UpdateChatConversation(int conversationId) => ExecuteWriteWithRetry(() => _database.UpdateChatConversation(conversationId), nameof(UpdateChatConversation));
        public List<ChatMessageDB> GetChatMessages(int conversationId) => ExecuteReadWithRetry(() => _database.GetChatMessages(conversationId), nameof(GetChatMessages));
        public void AddChatMessage(ChatMessageDB message) => ExecuteWriteWithRetry(() => _database.AddChatMessage(message), nameof(AddChatMessage));
        public void DeleteUserChatConversations(int userId) => ExecuteWriteWithRetry(() => _database.DeleteUserChatConversations(userId), nameof(DeleteUserChatConversations));
        
        // Phase 1 : Gestion des Équipes
        public List<Equipe> GetAllEquipes() => ExecuteReadWithRetry(() => _database.GetAllEquipes(), nameof(GetAllEquipes));
        public Equipe GetEquipeById(int id) => ExecuteReadWithRetry(() => _database.GetEquipeById(id), nameof(GetEquipeById));
        public void AjouterEquipe(Equipe equipe) => ExecuteWriteWithRetry(() => _database.AjouterEquipe(equipe), nameof(AjouterEquipe));
        public void ModifierEquipe(Equipe equipe) => ExecuteWriteWithRetry(() => _database.ModifierEquipe(equipe), nameof(ModifierEquipe));
        public List<Utilisateur> GetMembresByEquipe(int equipeId) => ExecuteReadWithRetry(() => _database.GetMembresByEquipe(equipeId), nameof(GetMembresByEquipe));
        public List<Projet> GetProjetsByEquipe(int equipeId) => ExecuteReadWithRetry(() => _database.GetProjetsByEquipe(equipeId), nameof(GetProjetsByEquipe));
        public List<BacklogItem> GetBacklogItemsByDevId(int devId) => ExecuteReadWithRetry(() => _database.GetBacklogItemsByDevId(devId), nameof(GetBacklogItemsByDevId));
        
        // Phase 2 : Gestion des Programmes
        public List<Programme> GetAllProgrammes() => ExecuteReadWithRetry(() => _database.GetAllProgrammes(), nameof(GetAllProgrammes));
        public Programme GetProgrammeById(int id) => ExecuteReadWithRetry(() => _database.GetProgrammeById(id), nameof(GetProgrammeById));
        public void AjouterProgramme(Programme programme) => ExecuteWriteWithRetry(() => _database.AjouterProgramme(programme), nameof(AjouterProgramme));
        public void ModifierProgramme(Programme programme) => ExecuteWriteWithRetry(() => _database.ModifierProgramme(programme), nameof(ModifierProgramme));
        public void SupprimerProgramme(int id) => ExecuteWriteWithRetry(() => _database.SupprimerProgramme(id), nameof(SupprimerProgramme));
        public List<Projet> GetProjetsByProgramme(int programmeId) => ExecuteReadWithRetry(() => _database.GetProjetsByProgramme(programmeId), nameof(GetProjetsByProgramme));
        
        // Planning VM
        public List<PlanningVMJour> GetPlanningsVM() => ExecuteReadWithRetry(() => _database.GetPlanningsVM(), nameof(GetPlanningsVM));
        public PlanningVMJour GetPlanningVMById(int id) => ExecuteReadWithRetry(() => _database.GetPlanningVMById(id), nameof(GetPlanningVMById));
        public void AjouterPlanningVM(PlanningVMJour planning) => ExecuteWriteWithRetry(() => _database.AjouterPlanningVM(planning), nameof(AjouterPlanningVM));
        public void ModifierPlanningVM(PlanningVMJour planning) => ExecuteWriteWithRetry(() => _database.ModifierPlanningVM(planning), nameof(ModifierPlanningVM));
        public void SupprimerPlanningVM(int id) => ExecuteWriteWithRetry(() => _database.SupprimerPlanningVM(id), nameof(SupprimerPlanningVM));
        
        // Demandes d'échange VM
        public List<DemandeEchangeVM> GetDemandesEchangeVM() => ExecuteReadWithRetry(() => _database.GetDemandesEchangeVM(), nameof(GetDemandesEchangeVM));
        public DemandeEchangeVM GetDemandeEchangeVMById(int id) => ExecuteReadWithRetry(() => _database.GetDemandeEchangeVMById(id), nameof(GetDemandeEchangeVMById));
        public void AjouterDemandeEchangeVM(DemandeEchangeVM demande) => ExecuteWriteWithRetry(() => _database.AjouterDemandeEchangeVM(demande), nameof(AjouterDemandeEchangeVM));
        public int GetDerniereDemandeEchangeVMId() => ExecuteReadWithRetry(() => _database.GetDerniereDemandeEchangeVMId(), nameof(GetDerniereDemandeEchangeVMId));
        public void ModifierDemandeEchangeVM(DemandeEchangeVM demande) => ExecuteWriteWithRetry(() => _database.ModifierDemandeEchangeVM(demande), nameof(ModifierDemandeEchangeVM));
        public void SupprimerDemandeEchangeVM(int id) => ExecuteWriteWithRetry(() => _database.SupprimerDemandeEchangeVM(id), nameof(SupprimerDemandeEchangeVM));
        public void AnnulerDemandeEchangeVM(int demandeId) => ExecuteWriteWithRetry(() => _database.AnnulerDemandeEchangeVM(demandeId), nameof(AnnulerDemandeEchangeVM));
        public List<DemandeEchangeVM> GetDemandesEchangeVMEnAttentePourUtilisateur(int utilisateurId) => ExecuteReadWithRetry(() => _database.GetDemandesEchangeVMEnAttentePourUtilisateur(utilisateurId), nameof(GetDemandesEchangeVMEnAttentePourUtilisateur));
        public void AccepterEchangeVM(int demandeId, int planningVMJourId, int ancienUtilisateurId, int nouvelUtilisateurId) => ExecuteWriteWithRetry(() => _database.AccepterEchangeVM(demandeId, planningVMJourId, ancienUtilisateurId, nouvelUtilisateurId), nameof(AccepterEchangeVM));
        
        // Notification avec utilisateur cible
        public void AjouterNotification(Notification notification, int utilisateurId) => ExecuteWriteWithRetry(() => _database.AjouterNotification(notification, utilisateurId), nameof(AjouterNotification));
    }
}

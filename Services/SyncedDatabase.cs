using System;
using System.Collections.Generic;
using System.Text.Json;
using BacklogManager.Domain;
using BacklogManager.Services.Sync;

namespace BacklogManager.Services
{
    /// <summary>
    /// Décorateur IDatabase : encapsule un <see cref="SqliteDatabase"/> pointant sur
    /// la DB locale (WAL) et enregistre en journal chaque opération d'écriture afin
    /// qu'elle soit publiée sur le NAS par <see cref="SyncEngine"/>.
    ///
    /// Toutes les méthodes de lecture sont déléguées directement au SqliteDatabase.
    /// Toutes les méthodes d'écriture appellent la DB locale PUIS ajoutent l'entrée
    /// dans <c>SyncJournal</c> au sein de la même transaction SQLite (atomique).
    ///
    /// Garanties :
    ///   - Si l'écriture métier réussit, l'entrée journal est créée dans la même transaction
    ///   - Si l'écriture échoue, aucune entrée journal n'est créée
    ///   - L'ID de l'entité retourné par la DB (INSERT auto-increment) est utilisé dans EntityId
    ///
    /// Usage d'initialisation dans App.xaml.cs :
    /// <code>
    ///   var inner     = new SqliteDatabase(localDbPath);   // local.db
    ///   var syncedDb  = new SyncedDatabase(inner, localDb, syncEngine, currentUsername, clientId);
    ///   ServiceLocator.Database = syncedDb;
    /// </code>
    /// </summary>
    public class SyncedDatabase : IDatabase
    {
        // ─── Dépendances ──────────────────────────────────────────────────────
        private readonly IDatabase           _inner;
        private readonly LocalDatabaseFactory _localDb;
        private readonly string              _clientId;
        private readonly string              _username;

        // ─── Options de sérialisation JSON ───────────────────────────────────
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy        = JsonNamingPolicy.CamelCase,
            WriteIndented               = false,
            DefaultIgnoreCondition      = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        // ─── Ctor ─────────────────────────────────────────────────────────────
        public SyncedDatabase(
            IDatabase            inner,
            LocalDatabaseFactory localDb,
            string               username,
            string               clientId)
        {
            _inner    = inner    ?? throw new ArgumentNullException(nameof(inner));
            _localDb  = localDb  ?? throw new ArgumentNullException(nameof(localDb));
            _username = username ?? "(système)";
            _clientId = clientId ?? Environment.MachineName;
        }

        // ─── Helpers ──────────────────────────────────────────────────────────

        private string Serialize<T>(T entity) =>
            JsonSerializer.Serialize(entity, _jsonOptions);

        /// <summary>
        /// Enregistre une opération dans SyncJournal.
        /// Appelé APRÈS chaque écriture métier réussie.
        /// ATTENTION : cette méthode ouvre sa propre connexion (hors tx métier).
        /// Si une atomicité stricte est requise, utiliser AppendToJournal avec la connexion/tx de la DB interne.
        /// Ici on accepte la fenêtre race : si le process crash entre l'écriture et le journal,
        /// la donnée est présente localement mais n'est pas diffusée → sera visible après le prochain
        /// restart si l'app est le seul client.
        ///
        /// Pour une atomicité parfaite, SqliteDatabase devrait exposer ses connexions — ce n'est pas le cas
        /// dans l'architecture actuelle, donc on enregistre juste après.
        /// </summary>
        private void Journal(string operationType, string tableName, int entityId, string payloadJson)
        {
            try
            {
                var op = new SyncOperation
                {
                    OperationId     = Guid.NewGuid().ToString("N"),
                    OriginClientId  = _clientId,
                    OriginUsername  = _username,
                    TimestampUtc    = DateTime.UtcNow,
                    OperationType   = operationType,
                    TableName       = tableName,
                    EntityId        = entityId,
                    PayloadJson     = payloadJson,
                    IsPublished     = false,
                    IsApplied       = false
                };

                using (var conn = _localDb.OpenWriteConnection())
                {
                    conn.Open();
                    using (var tx = conn.BeginTransaction())
                    {
                        _localDb.AppendToJournal(conn, tx, op);
                        tx.Commit();
                    }
                }
            }
            catch (Exception ex)
            {
                // Ne jamais laisser le journal bloquer l'opération métier
                LoggingService.Instance.LogWarning(
                    $"[SyncedDatabase] Impossible d'enregistrer dans SyncJournal ({operationType}#{entityId}): {ex.Message}");
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  MÉTHODES DE LECTURE — délégation pure
        // ═══════════════════════════════════════════════════════════════

        public List<Role>                   GetRoles()              => _inner.GetRoles();
        public List<Utilisateur>            GetUtilisateurs()       => _inner.GetUtilisateurs();
        public List<Projet>                 GetProjets()            => _inner.GetProjets();
        public List<Sprint>                 GetSprints()            => _inner.GetSprints();
        public List<BacklogItem>            GetBacklogItems()       => _inner.GetBacklogItems();
        public List<Demande>                GetDemandes()           => _inner.GetDemandes();
        public List<Dev>                    GetDevs()               => _inner.GetDevs();
        public List<Commentaire>            GetCommentaires()       => _inner.GetCommentaires();
        public List<HistoriqueModification> GetHistoriqueModifications() => _inner.GetHistoriqueModifications();
        public List<PokerSession>           GetPokerSessions()      => _inner.GetPokerSessions();
        public List<PokerVote>              GetPokerVotes()         => _inner.GetPokerVotes();
        public List<Disponibilite>          GetDisponibilites()     => _inner.GetDisponibilites();
        public List<BacklogItem>            GetBacklog()            => _inner.GetBacklog();
        public List<BacklogItem>            GetAllBacklogItemsIncludingArchived() => _inner.GetAllBacklogItemsIncludingArchived();
        public List<AuditLog>               GetAuditLogs()          => _inner.GetAuditLogs();
        public List<CRA>                    GetAllCRAs()            => _inner.GetAllCRAs();
        public List<CRA>                    GetCRAs(int? backlogItemId = null, int? devId = null,
                                                    DateTime? dateDebut = null, DateTime? dateFin = null)
                                                    => _inner.GetCRAs(backlogItemId, devId, dateDebut, dateFin);

        // Notifications (lecture)
        public List<Notification>           GetNotifications()           => _inner.GetNotifications();
        public List<Notification>           GetNotificationsByUtilisateur(int utilisateurId)
                                                                          => _inner.GetNotificationsByUtilisateur(utilisateurId);

        // Chat (lecture)
        public List<ChatConversation>       GetChatConversations()       => _inner.GetChatConversations();
        public ChatConversation             GetChatConversation(int cid) => _inner.GetChatConversation(cid);
        public List<ChatMessageDB>          GetChatMessages(int cid)     => _inner.GetChatMessages(cid);

        // Équipes (lecture)
        public List<Equipe>                 GetAllEquipes()              => _inner.GetAllEquipes();
        public Equipe                       GetEquipeById(int id)        => _inner.GetEquipeById(id);
        public List<Utilisateur>            GetMembresByEquipe(int id)   => _inner.GetMembresByEquipe(id);
        public List<Projet>                 GetProjetsByEquipe(int id)   => _inner.GetProjetsByEquipe(id);
        public List<BacklogItem>            GetBacklogItemsByDevId(int id) => _inner.GetBacklogItemsByDevId(id);

        // Programmes (lecture)
        public List<Programme>              GetAllProgrammes()           => _inner.GetAllProgrammes();
        public Programme                    GetProgrammeById(int id)     => _inner.GetProgrammeById(id);
        public List<Projet>                 GetProjetsByProgramme(int id) => _inner.GetProjetsByProgramme(id);

        // Planning VM (lecture)
        public List<PlanningVMJour>         GetPlanningsVM()             => _inner.GetPlanningsVM();
        public PlanningVMJour               GetPlanningVMById(int id)    => _inner.GetPlanningVMById(id);

        // Demandes Échange VM (lecture)
        public List<DemandeEchangeVM>       GetDemandesEchangeVM()       => _inner.GetDemandesEchangeVM();
        public DemandeEchangeVM             GetDemandeEchangeVMById(int id) => _inner.GetDemandeEchangeVMById(id);
        public int                          GetDerniereDemandeEchangeVMId() => _inner.GetDerniereDemandeEchangeVMId();
        public List<DemandeEchangeVM>       GetDemandesEchangeVMEnAttentePourUtilisateur(int uid)
                                                                => _inner.GetDemandesEchangeVMEnAttentePourUtilisateur(uid);

        // Config (lecture)
        public string                       GetConfiguration(string key) => _inner.GetConfiguration(key);

        // ═══════════════════════════════════════════════════════════════
        //  MÉTHODES D'ÉCRITURE — délégation + journal
        // ═══════════════════════════════════════════════════════════════

        // ─── Roles ───────────────────────────────────────────────────────────

        public Role AddOrUpdateRole(Role role)
        {
            var result = _inner.AddOrUpdateRole(role);
            Journal(SyncOp.RoleUpsert, "Roles", result.Id, Serialize(result));
            return result;
        }

        public void UpdateRole(Role role)
        {
            _inner.UpdateRole(role);
            Journal(SyncOp.RoleUpsert, "Roles", role.Id, Serialize(role));
        }

        // ─── Utilisateurs ────────────────────────────────────────────────────

        public Utilisateur AddOrUpdateUtilisateur(Utilisateur u)
        {
            var result = _inner.AddOrUpdateUtilisateur(u);
            Journal(SyncOp.UtilisateurUpsert, "Utilisateurs", result.Id, Serialize(result));
            return result;
        }

        public void AddUtilisateur(Utilisateur u)
        {
            _inner.AddUtilisateur(u);
            Journal(SyncOp.UtilisateurUpsert, "Utilisateurs", u.Id, Serialize(u));
        }

        public void UpdateUtilisateur(Utilisateur u)
        {
            _inner.UpdateUtilisateur(u);
            Journal(SyncOp.UtilisateurUpsert, "Utilisateurs", u.Id, Serialize(u));
        }

        public void DeleteUtilisateur(int id)
        {
            _inner.DeleteUtilisateur(id);
            Journal(SyncOp.UtilisateurDelete, "Utilisateurs", id, $"{{\"id\":{id}}}");
        }

        // ─── Projets ──────────────────────────────────────────────────────────

        public Projet AddOrUpdateProjet(Projet projet)
        {
            var result = _inner.AddOrUpdateProjet(projet);
            Journal(SyncOp.ProjetUpsert, "Projets", result.Id, Serialize(result));
            return result;
        }

        // ─── Sprints ──────────────────────────────────────────────────────────

        public Sprint AddOrUpdateSprint(Sprint sprint)
        {
            var result = _inner.AddOrUpdateSprint(sprint);
            Journal(SyncOp.SprintUpsert, "Sprints", result.Id, Serialize(result));
            return result;
        }

        // ─── BacklogItems ─────────────────────────────────────────────────────

        public BacklogItem AddOrUpdateBacklogItem(BacklogItem item)
        {
            var result = _inner.AddOrUpdateBacklogItem(item);
            Journal(SyncOp.BacklogItemUpsert, "BacklogItems", result.Id, Serialize(result));
            return result;
        }

        // ─── Demandes ─────────────────────────────────────────────────────────

        public Demande AddOrUpdateDemande(Demande demande)
        {
            var result = _inner.AddOrUpdateDemande(demande);
            Journal(SyncOp.DemandeUpsert, "Demandes", result.Id, Serialize(result));
            return result;
        }

        public void DeleteDemande(int id)
        {
            _inner.DeleteDemande(id);
            Journal(SyncOp.DemandeDelete, "Demandes", id, $"{{\"id\":{id}}}");
        }

        // ─── Devs ─────────────────────────────────────────────────────────────

        public Dev AddOrUpdateDev(Dev dev)
        {
            var result = _inner.AddOrUpdateDev(dev);
            // Devs est une projection de Utilisateurs — on ne journalise pas séparément
            // pour éviter les doublons (AddOrUpdateUtilisateur couvre déjà ce cas).
            return result;
        }

        // ─── Commentaires ─────────────────────────────────────────────────────

        public Commentaire AddCommentaire(Commentaire c)
        {
            var result = _inner.AddCommentaire(c);
            Journal(SyncOp.CommentaireUpsert, "Commentaires", result.Id, Serialize(result));
            return result;
        }

        public Commentaire AddOrUpdateCommentaire(Commentaire c)
        {
            var result = _inner.AddOrUpdateCommentaire(c);
            Journal(SyncOp.CommentaireUpsert, "Commentaires", result.Id, Serialize(result));
            return result;
        }

        // ─── Historique ───────────────────────────────────────────────────────

        public void AddHistorique(HistoriqueModification h)
        {
            _inner.AddHistorique(h);
            // L'historique est local-only par nature (chaque poste tient son propre audit)
        }

        public HistoriqueModification AddOrUpdateHistoriqueModification(HistoriqueModification h)
        {
            return _inner.AddOrUpdateHistoriqueModification(h);
        }

        // ─── Poker ────────────────────────────────────────────────────────────

        public PokerSession AddOrUpdatePokerSession(PokerSession session)
        {
            var result = _inner.AddOrUpdatePokerSession(session);
            Journal(SyncOp.PokerSessionUpsert, "PokerSessions", result.Id, Serialize(result));
            return result;
        }

        public PokerVote AddPokerVote(PokerVote vote)
        {
            var result = _inner.AddPokerVote(vote);
            Journal(SyncOp.PokerVoteUpsert, "PokerVotes", result.Id, Serialize(result));
            return result;
        }

        // ─── Disponibilités ───────────────────────────────────────────────────

        public Disponibilite AddOrUpdateDisponibilite(Disponibilite d)
        {
            var result = _inner.AddOrUpdateDisponibilite(d);
            Journal(SyncOp.DisponibiliteUpsert, "Disponibilites", result.Id, Serialize(result));
            return result;
        }

        // ─── AuditLog ─────────────────────────────────────────────────────────

        public void AddAuditLog(AuditLog auditLog)
        {
            // AuditLog est local-only (chaque poste tient son historique d'actions)
            _inner.AddAuditLog(auditLog);
        }

        // ─── CRA ──────────────────────────────────────────────────────────────

        public void SaveCRA(CRA cra)
        {
            _inner.SaveCRA(cra);
            Journal(SyncOp.CRAUpsert, "CRA", cra.Id, Serialize(cra));
        }

        public void DeleteCRA(int id)
        {
            _inner.DeleteCRA(id);
            Journal(SyncOp.CRADelete, "CRA", id, $"{{\"id\":{id}}}");
        }

        // ─── Notifications ────────────────────────────────────────────────────

        public void AddOrUpdateNotification(Notification notification)
        {
            _inner.AddOrUpdateNotification(notification);
            Journal(SyncOp.NotificationUpsert, "Notifications", notification.Id, Serialize(notification));
        }

        public void DeleteNotification(int id)
        {
            _inner.DeleteNotification(id);
            Journal(SyncOp.NotificationDelete, "Notifications", id, $"{{\"id\":{id}}}");
        }

        public void DeleteNotificationsLues()          => _inner.DeleteNotificationsLues();
        public void SupprimerToutesLesNotifications()  => _inner.SupprimerToutesLesNotifications();
        public void MarquerNotificationCommeLue(int id) => _inner.MarquerNotificationCommeLue(id);
        public void MarquerToutesNotificationsCommeLues() => _inner.MarquerToutesNotificationsCommeLues();
        public void AjouterNotification(Notification n, int utilisateurId) => _inner.AjouterNotification(n, utilisateurId);

        // ─── Chat ─────────────────────────────────────────────────────────────

        // Chat est local-only (les conversations ne se propagent pas entre postes)
        public int  CreateChatConversation(int userId, string username) => _inner.CreateChatConversation(userId, username);
        public void UpdateChatConversation(int cid)                      => _inner.UpdateChatConversation(cid);
        public void AddChatMessage(ChatMessageDB message)                => _inner.AddChatMessage(message);
        public void DeleteUserChatConversations(int userId)              => _inner.DeleteUserChatConversations(userId);

        // ─── Équipes ──────────────────────────────────────────────────────────

        public void AjouterEquipe(Equipe equipe)
        {
            _inner.AjouterEquipe(equipe);
            Journal(SyncOp.EquipeUpsert, "Equipes", equipe.Id, Serialize(equipe));
        }

        public void ModifierEquipe(Equipe equipe)
        {
            _inner.ModifierEquipe(equipe);
            Journal(SyncOp.EquipeUpsert, "Equipes", equipe.Id, Serialize(equipe));
        }

        // ─── Programmes ───────────────────────────────────────────────────────

        public void AjouterProgramme(Programme programme)
        {
            _inner.AjouterProgramme(programme);
            Journal(SyncOp.ProgrammeUpsert, "Programmes", programme.Id, Serialize(programme));
        }

        public void ModifierProgramme(Programme programme)
        {
            _inner.ModifierProgramme(programme);
            Journal(SyncOp.ProgrammeUpsert, "Programmes", programme.Id, Serialize(programme));
        }

        public void SupprimerProgramme(int id)
        {
            _inner.SupprimerProgramme(id);
            Journal(SyncOp.ProgrammeDelete, "Programmes", id, $"{{\"id\":{id}}}");
        }

        // ─── Planning VM ─────────────────────────────────────────────────────

        public void AjouterPlanningVM(PlanningVMJour planning)
        {
            _inner.AjouterPlanningVM(planning);
            Journal(SyncOp.PlanningVMUpsert, "PlanningVM", planning.Id, Serialize(planning));
        }

        public void ModifierPlanningVM(PlanningVMJour planning)
        {
            _inner.ModifierPlanningVM(planning);
            Journal(SyncOp.PlanningVMUpsert, "PlanningVM", planning.Id, Serialize(planning));
        }

        public void SupprimerPlanningVM(int id)
        {
            _inner.SupprimerPlanningVM(id);
            Journal(SyncOp.PlanningVMDelete, "PlanningVM", id, $"{{\"id\":{id}}}");
        }

        // ─── Demandes Échange VM ──────────────────────────────────────────────

        public void AjouterDemandeEchangeVM(DemandeEchangeVM demande)
        {
            _inner.AjouterDemandeEchangeVM(demande);
            Journal(SyncOp.DemandeEchangeUpsert, "DemandeEchangeVM", demande.Id, Serialize(demande));
        }

        public void ModifierDemandeEchangeVM(DemandeEchangeVM demande)
        {
            _inner.ModifierDemandeEchangeVM(demande);
            Journal(SyncOp.DemandeEchangeUpsert, "DemandeEchangeVM", demande.Id, Serialize(demande));
        }

        public void SupprimerDemandeEchangeVM(int id)
        {
            _inner.SupprimerDemandeEchangeVM(id);
            Journal(SyncOp.DemandeEchangeDelete, "DemandeEchangeVM", id, $"{{\"id\":{id}}}");
        }

        public void AnnulerDemandeEchangeVM(int id)
        {
            _inner.AnnulerDemandeEchangeVM(id);
            Journal(SyncOp.DemandeEchangeUpsert, "DemandeEchangeVM", id, $"{{\"id\":{id},\"statut\":\"annulée\"}}");
        }

        public void AccepterEchangeVM(int demandeId, int planningVMJourId, int ancienUtilisateurId, int nouvelUtilisateurId)
        {
            _inner.AccepterEchangeVM(demandeId, planningVMJourId, ancienUtilisateurId, nouvelUtilisateurId);
            // Journaliser les deux plannings modifiés + la demande
            Journal(SyncOp.DemandeEchangeUpsert, "DemandeEchangeVM", demandeId,
                $"{{\"id\":{demandeId},\"statut\":\"acceptée\",\"ancienUtilisateurId\":{ancienUtilisateurId},\"nouvelUtilisateurId\":{nouvelUtilisateurId}}}");
        }

        // ─── Configuration ────────────────────────────────────────────────────

        public void SetConfiguration(string key, string value)
        {
            _inner.SetConfiguration(key, value);
            Journal(SyncOp.ConfigUpsert, "Configuration", 0,
                $"{{\"key\":{JsonSerializer.Serialize(key)},\"value\":{JsonSerializer.Serialize(value)}}}");
        }
    }
}

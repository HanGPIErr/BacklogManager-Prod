using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using BacklogManager.Domain;

namespace BacklogManager.Services
{
    public class DatabaseModel
    {
        public List<BacklogItem> BacklogItems { get; set; }
        public List<Dev> Devs { get; set; }
        public List<Projet> Projets { get; set; }
        public List<PokerSession> PokerSessions { get; set; }
        public List<PokerVote> PokerVotes { get; set; }
        public List<Utilisateur> Utilisateurs { get; set; }
        public List<Role> Roles { get; set; }
        public List<Demande> Demandes { get; set; }
        public List<Sprint> Sprints { get; set; }
        public List<Disponibilite> Disponibilites { get; set; }
        public List<Commentaire> Commentaires { get; set; }
        public List<HistoriqueModification> Historique { get; set; }
        public List<AuditLog> AuditLogs { get; set; }
        public List<CRA> CRAs { get; set; }

        public DatabaseModel()
        {
            BacklogItems = new List<BacklogItem>();
            Devs = new List<Dev>();
            Projets = new List<Projet>();
            PokerSessions = new List<PokerSession>();
            PokerVotes = new List<PokerVote>();
            Utilisateurs = new List<Utilisateur>();
            Roles = new List<Role>();
            Demandes = new List<Demande>();
            Sprints = new List<Sprint>();
            Disponibilites = new List<Disponibilite>();
            Commentaires = new List<Commentaire>();
            Historique = new List<HistoriqueModification>();
            AuditLogs = new List<AuditLog>();
            CRAs = new List<CRA>();
        }
    }

    public class JsonDatabase : IDatabase
    {
        private static readonly string FilePath = @"C:\Users\HanGP\BacklogManager\backlog-db.json";
        private static readonly object _lock = new object();
        private DatabaseModel _data;

        public JsonDatabase()
        {
            Load();
        }

        public void Load()
        {
            lock (_lock)
            {
                try
                {
                    if (File.Exists(FilePath))
                    {
                        string json = File.ReadAllText(FilePath);
                        _data = JsonSerializer.Deserialize<DatabaseModel>(json) ?? new DatabaseModel();
                    }
                    else
                    {
                        _data = new DatabaseModel();
                        Save();
                    }
                }
                catch (System.Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(string.Format("Error loading database: {0}", ex.Message));
                    _data = new DatabaseModel();
                }
            }
        }

        public void Save()
        {
            lock (_lock)
            {
                try
                {
                    var options = new JsonSerializerOptions 
                    { 
                        WriteIndented = true,
                        PropertyNamingPolicy = null, // Garder les noms de propriété tels quels
                        IncludeFields = false
                    };
                    string json = JsonSerializer.Serialize(_data, options);
                    
                    string directory = Path.GetDirectoryName(FilePath);
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                    
                    File.WriteAllText(FilePath, json);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(string.Format("Error saving database: {0}", ex.Message));
                }
            }
        }

        public List<BacklogItem> GetBacklog()
        {
            lock (_lock)
            {
                return new List<BacklogItem>(_data.BacklogItems);
            }
        }

        public List<BacklogItem> GetAllBacklogItemsIncludingArchived()
        {
            lock (_lock)
            {
                return new List<BacklogItem>(_data.BacklogItems);
            }
        }

        public List<AuditLog> GetAuditLogs()
        {
            lock (_lock)
            {
                return new List<AuditLog>(_data.AuditLogs ?? new List<AuditLog>());
            }
        }

        public void AddAuditLog(AuditLog auditLog)
        {
            lock (_lock)
            {
                if (_data.AuditLogs == null)
                    _data.AuditLogs = new List<AuditLog>();
                
                auditLog.Id = GetNextId(_data.AuditLogs);
                _data.AuditLogs.Add(auditLog);
                Save();
            }
        }

        // Notifications - Stubs pour JsonDatabase (non utilisé, SQLite uniquement)
        public List<Notification> GetNotifications() { return new List<Notification>(); }
        public void AddOrUpdateNotification(Notification notification) { }
        public void DeleteNotification(int notificationId) { }
        public void DeleteNotificationsLues() { }
        public void SupprimerToutesLesNotifications() { }
        public void MarquerNotificationCommeLue(int notificationId) { }
        public void MarquerToutesNotificationsCommeLues() { }

        public BacklogItem AddOrUpdateBacklogItem(BacklogItem item)
        {
            lock (_lock)
            {
                var existing = _data.BacklogItems.Find(x => x.Id == item.Id);
                if (existing != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[AddOrUpdateBacklogItem] Mise à jour tâche #{item.Id} - EstArchive: {item.EstArchive}");
                    _data.BacklogItems.Remove(existing);
                }
                else
                {
                    item.Id = GetNextId(_data.BacklogItems);
                    System.Diagnostics.Debug.WriteLine($"[AddOrUpdateBacklogItem] Nouvelle tâche #{item.Id} - EstArchive: {item.EstArchive}");
                }
                
                item.DateDerniereMaj = DateTime.Now;
                _data.BacklogItems.Add(item);
                Save();
                
                // Vérifier que c'est bien sauvegardé
                var saved = _data.BacklogItems.Find(x => x.Id == item.Id);
                System.Diagnostics.Debug.WriteLine($"[AddOrUpdateBacklogItem] Après sauvegarde, EstArchive = {saved?.EstArchive}");
                
                return item;
            }
        }

        public List<Dev> GetDevs()
        {
            lock (_lock)
            {
                return new List<Dev>(_data.Devs);
            }
        }

        public Dev AddOrUpdateDev(Dev dev)
        {
            lock (_lock)
            {
                var existing = _data.Devs.Find(x => x.Id == dev.Id);
                if (existing != null)
                {
                    _data.Devs.Remove(existing);
                }
                else
                {
                    dev.Id = GetNextId(_data.Devs);
                }
                
                _data.Devs.Add(dev);
                Save();
                return dev;
            }
        }

        public List<Projet> GetProjets()
        {
            lock (_lock)
            {
                return new List<Projet>(_data.Projets);
            }
        }

        public Projet AddOrUpdateProjet(Projet projet)
        {
            lock (_lock)
            {
                var existing = _data.Projets.Find(x => x.Id == projet.Id);
                if (existing != null)
                {
                    _data.Projets.Remove(existing);
                }
                else
                {
                    projet.Id = GetNextId(_data.Projets);
                }
                
                _data.Projets.Add(projet);
                Save();
                return projet;
            }
        }

        public List<PokerSession> GetPokerSessions()
        {
            lock (_lock)
            {
                return new List<PokerSession>(_data.PokerSessions);
            }
        }

        public PokerSession AddOrUpdatePokerSession(PokerSession session)
        {
            lock (_lock)
            {
                var existing = _data.PokerSessions.Find(x => x.Id == session.Id);
                if (existing != null)
                {
                    _data.PokerSessions.Remove(existing);
                }
                else
                {
                    session.Id = GetNextId(_data.PokerSessions);
                }
                
                _data.PokerSessions.Add(session);
                Save();
                return session;
            }
        }

        public List<PokerVote> GetPokerVotes()
        {
            lock (_lock)
            {
                return new List<PokerVote>(_data.PokerVotes);
            }
        }

        public PokerVote AddPokerVote(PokerVote vote)
        {
            lock (_lock)
            {
                vote.Id = GetNextId(_data.PokerVotes);
                _data.PokerVotes.Add(vote);
                Save();
                return vote;
            }
        }

        private int GetNextId<T>(List<T> items) where T : class
        {
            int maxId = 0;
            foreach (var item in items)
            {
                var idProp = item.GetType().GetProperty("Id");
                if (idProp != null)
                {
                    int id = (int)idProp.GetValue(item);
                    if (id > maxId) maxId = id;
                }
            }
            return maxId + 1;
        }

        // Utilisateurs
        public List<Utilisateur> GetUtilisateurs()
        {
            lock (_lock)
            {
                return new List<Utilisateur>(_data.Utilisateurs);
            }
        }

        public Utilisateur AddOrUpdateUtilisateur(Utilisateur utilisateur)
        {
            lock (_lock)
            {
                var existing = _data.Utilisateurs.Find(u => u.Id == utilisateur.Id);
                if (existing != null)
                {
                    _data.Utilisateurs.Remove(existing);
                }
                else
                {
                    utilisateur.Id = GetNextId(_data.Utilisateurs);
                }
                _data.Utilisateurs.Add(utilisateur);
                Save();
                return utilisateur;
            }
        }

        public void AddUtilisateur(Utilisateur utilisateur)
        {
            lock (_lock)
            {
                utilisateur.Id = GetNextId(_data.Utilisateurs);
                _data.Utilisateurs.Add(utilisateur);
                Save();
            }
        }

        public void UpdateUtilisateur(Utilisateur utilisateur)
        {
            lock (_lock)
            {
                var existing = _data.Utilisateurs.Find(u => u.Id == utilisateur.Id);
                if (existing != null)
                {
                    _data.Utilisateurs.Remove(existing);
                    _data.Utilisateurs.Add(utilisateur);
                    Save();
                }
            }
        }

        public void DeleteUtilisateur(int id)
        {
            lock (_lock)
            {
                var existing = _data.Utilisateurs.Find(u => u.Id == id);
                if (existing != null)
                {
                    _data.Utilisateurs.Remove(existing);
                    Save();
                }
            }
        }

        // Roles
        public List<Role> GetRoles()
        {
            lock (_lock)
            {
                return new List<Role>(_data.Roles);
            }
        }

        public Role AddOrUpdateRole(Role role)
        {
            lock (_lock)
            {
                var existing = _data.Roles.Find(r => r.Id == role.Id);
                if (existing != null)
                {
                    _data.Roles.Remove(existing);
                }
                else
                {
                    role.Id = GetNextId(_data.Roles);
                }
                _data.Roles.Add(role);
                Save();
                return role;
            }
        }

        public void UpdateRole(Role role)
        {
            lock (_lock)
            {
                var existing = _data.Roles.Find(r => r.Id == role.Id);
                if (existing != null)
                {
                    existing.PeutCreerDemandes = role.PeutCreerDemandes;
                    existing.PeutChiffrer = role.PeutChiffrer;
                    existing.PeutPrioriser = role.PeutPrioriser;
                    existing.PeutGererUtilisateurs = role.PeutGererUtilisateurs;
                    existing.PeutVoirKPI = role.PeutVoirKPI;
                    existing.PeutGererReferentiels = role.PeutGererReferentiels;
                    existing.PeutModifierTaches = role.PeutModifierTaches;
                    existing.PeutSupprimerTaches = role.PeutSupprimerTaches;
                    Save();
                }
            }
        }

        // Demandes
        public List<Demande> GetDemandes()
        {
            lock (_lock)
            {
                return new List<Demande>(_data.Demandes);
            }
        }

        public Demande AddOrUpdateDemande(Demande demande)
        {
            lock (_lock)
            {
                var existing = _data.Demandes.Find(d => d.Id == demande.Id);
                if (existing != null)
                {
                    _data.Demandes.Remove(existing);
                }
                else
                {
                    demande.Id = GetNextId(_data.Demandes);
                }
                _data.Demandes.Add(demande);
                Save();
                return demande;
            }
        }

        public void DeleteDemande(int id)
        {
            lock (_lock)
            {
                var demande = _data.Demandes.Find(d => d.Id == id);
                if (demande != null)
                {
                    _data.Demandes.Remove(demande);
                    
                    // Supprimer les commentaires associés
                    _data.Commentaires.RemoveAll(c => c.DemandeId == id);
                    
                    Save();
                }
            }
        }

        // Sprints
        public List<Sprint> GetSprints()
        {
            lock (_lock)
            {
                return new List<Sprint>(_data.Sprints);
            }
        }

        public Sprint AddOrUpdateSprint(Sprint sprint)
        {
            lock (_lock)
            {
                var existing = _data.Sprints.Find(s => s.Id == sprint.Id);
                if (existing != null)
                {
                    _data.Sprints.Remove(existing);
                }
                else
                {
                    sprint.Id = GetNextId(_data.Sprints);
                }
                _data.Sprints.Add(sprint);
                Save();
                return sprint;
            }
        }

        // Disponibilités
        public List<Disponibilite> GetDisponibilites()
        {
            lock (_lock)
            {
                return new List<Disponibilite>(_data.Disponibilites);
            }
        }

        public Disponibilite AddOrUpdateDisponibilite(Disponibilite disponibilite)
        {
            lock (_lock)
            {
                var existing = _data.Disponibilites.Find(d => d.Id == disponibilite.Id);
                if (existing != null)
                {
                    _data.Disponibilites.Remove(existing);
                }
                else
                {
                    disponibilite.Id = GetNextId(_data.Disponibilites);
                }
                _data.Disponibilites.Add(disponibilite);
                Save();
                return disponibilite;
            }
        }

        // Commentaires
        public List<Commentaire> GetCommentaires()
        {
            lock (_lock)
            {
                return new List<Commentaire>(_data.Commentaires);
            }
        }

        public Commentaire AddCommentaire(Commentaire commentaire)
        {
            lock (_lock)
            {
                commentaire.Id = GetNextId(_data.Commentaires);
                _data.Commentaires.Add(commentaire);
                Save();
                return commentaire;
            }
        }

        // Historique
        public List<HistoriqueModification> GetHistorique()
        {
            lock (_lock)
            {
                return new List<HistoriqueModification>(_data.Historique);
            }
        }

        public void AddHistorique(HistoriqueModification historique)
        {
            lock (_lock)
            {
                historique.Id = GetNextId(_data.Historique);
                _data.Historique.Add(historique);
                Save();
            }
        }

        // Implémentation IDatabase - méthodes manquantes
        public List<BacklogItem> GetBacklogItems()
        {
            lock (_lock)
            {
                return new List<BacklogItem>(_data.BacklogItems);
            }
        }

        public Commentaire AddOrUpdateCommentaire(Commentaire commentaire)
        {
            lock (_lock)
            {
                var existing = _data.Commentaires.Find(c => c.Id == commentaire.Id);
                if (existing != null)
                {
                    _data.Commentaires.Remove(existing);
                }
                else
                {
                    commentaire.Id = GetNextId(_data.Commentaires);
                }
                _data.Commentaires.Add(commentaire);
                Save();
                return commentaire;
            }
        }

        public List<HistoriqueModification> GetHistoriqueModifications()
        {
            return GetHistorique();
        }

        public HistoriqueModification AddOrUpdateHistoriqueModification(HistoriqueModification historique)
        {
            lock (_lock)
            {
                var existing = _data.Historique.Find(h => h.Id == historique.Id);
                if (existing != null)
                {
                    _data.Historique.Remove(existing);
                }
                else
                {
                    historique.Id = GetNextId(_data.Historique);
                }
                _data.Historique.Add(historique);
                Save();
                return historique;
            }
        }

        // CRA Methods
        public List<CRA> GetCRAs(int? backlogItemId = null, int? devId = null, System.DateTime? dateDebut = null, System.DateTime? dateFin = null)
        {
            lock (_lock)
            {
                var query = _data.CRAs.AsEnumerable();
                
                if (backlogItemId.HasValue)
                    query = query.Where(c => c.BacklogItemId == backlogItemId.Value);
                    
                if (devId.HasValue)
                    query = query.Where(c => c.DevId == devId.Value);
                    
                if (dateDebut.HasValue)
                    query = query.Where(c => c.Date >= dateDebut.Value);
                    
                if (dateFin.HasValue)
                    query = query.Where(c => c.Date <= dateFin.Value);
                    
                return query.ToList();
            }
        }

        public List<CRA> GetAllCRAs()
        {
            lock (_lock)
            {
                return _data.CRAs.ToList();
            }
        }

        public void SaveCRA(CRA cra)
        {
            lock (_lock)
            {
                var existing = _data.CRAs.FirstOrDefault(c => c.Id == cra.Id);
                if (existing != null)
                {
                    _data.CRAs.Remove(existing);
                }
                else if (cra.Id == 0)
                {
                    cra.Id = GetNextId(_data.CRAs);
                }
                _data.CRAs.Add(cra);
                Save();
            }
        }

        public void DeleteCRA(int id)
        {
            lock (_lock)
            {
                var cra = _data.CRAs.FirstOrDefault(c => c.Id == id);
                if (cra != null)
                {
                    _data.CRAs.Remove(cra);
                    Save();
                }
            }
        }
        
        // Chat Conversations - Not supported in JSON mode
        public List<ChatConversation> GetChatConversations()
        {
            throw new NotSupportedException("Les conversations chat ne sont supportées qu'en mode SQLite.");
        }
        
        public ChatConversation GetChatConversation(int conversationId)
        {
            throw new NotSupportedException("Les conversations chat ne sont supportées qu'en mode SQLite.");
        }
        
        public int CreateChatConversation(int userId, string username)
        {
            throw new NotSupportedException("Les conversations chat ne sont supportées qu'en mode SQLite.");
        }
        
        public void UpdateChatConversation(int conversationId)
        {
            throw new NotSupportedException("Les conversations chat ne sont supportées qu'en mode SQLite.");
        }
        
        public List<ChatMessageDB> GetChatMessages(int conversationId)
        {
            throw new NotSupportedException("Les conversations chat ne sont supportées qu'en mode SQLite.");
        }
        
        public void AddChatMessage(ChatMessageDB message)
        {
            throw new NotSupportedException("Les conversations chat ne sont supportées qu'en mode SQLite.");
        }

        public void DeleteUserChatConversations(int userId)
        {
            throw new NotSupportedException("Les conversations chat ne sont supportées qu'en mode SQLite.");
        }
    }
}

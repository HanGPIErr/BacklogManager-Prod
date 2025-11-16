using System.Collections.Generic;
using BacklogManager.Domain;

namespace BacklogManager.Services
{
    public interface IDatabase
    {
        List<Role> GetRoles();
        Role AddOrUpdateRole(Role role);
        void UpdateRole(Role role);
        List<Utilisateur> GetUtilisateurs();
        Utilisateur AddOrUpdateUtilisateur(Utilisateur utilisateur);
        void AddUtilisateur(Utilisateur utilisateur);
        void UpdateUtilisateur(Utilisateur utilisateur);
        void DeleteUtilisateur(int id);
        List<Projet> GetProjets();
        Projet AddOrUpdateProjet(Projet projet);
        List<Sprint> GetSprints();
        Sprint AddOrUpdateSprint(Sprint sprint);
        List<BacklogItem> GetBacklogItems();
        BacklogItem AddOrUpdateBacklogItem(BacklogItem item);
        List<Demande> GetDemandes();
        Demande AddOrUpdateDemande(Demande demande);
        List<Dev> GetDevs();
        Dev AddOrUpdateDev(Dev dev);
        List<Commentaire> GetCommentaires();
        Commentaire AddCommentaire(Commentaire commentaire);
        Commentaire AddOrUpdateCommentaire(Commentaire commentaire);
        List<HistoriqueModification> GetHistoriqueModifications();
        void AddHistorique(HistoriqueModification historique);
        HistoriqueModification AddOrUpdateHistoriqueModification(HistoriqueModification historique);
        List<PokerSession> GetPokerSessions();
        PokerSession AddOrUpdatePokerSession(PokerSession session);
        List<PokerVote> GetPokerVotes();
        PokerVote AddPokerVote(PokerVote vote);
        List<Disponibilite> GetDisponibilites();
        Disponibilite AddOrUpdateDisponibilite(Disponibilite disponibilite);
        List<BacklogItem> GetBacklog();
        List<AuditLog> GetAuditLogs();
        void AddAuditLog(AuditLog auditLog);
    }
}

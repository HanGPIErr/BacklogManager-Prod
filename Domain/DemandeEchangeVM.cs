using System;

namespace BacklogManager.Domain
{
    /// <summary>
    /// Représente une demande d'échange de planning VM entre deux membres d'équipe
    /// </summary>
    public class DemandeEchangeVM
    {
        public int Id { get; set; }
        public int PlanningVMJourId { get; set; }          // Référence vers le jour à échanger
        public int UtilisateurDemandeurId { get; set; }    // Celui qui demande l'échange
        public int UtilisateurCibleId { get; set; }        // Celui à qui on demande
        public DateTime DateDemande { get; set; }          // Quand la demande a été faite
        public string Statut { get; set; }                 // EN_ATTENTE, ACCEPTE, REFUSE
        public DateTime? DateReponse { get; set; }         // Quand la réponse a été donnée
        public string Message { get; set; }                // Message optionnel du demandeur
        
        // Navigation properties
        public PlanningVMJour PlanningVMJour { get; set; }
        public Utilisateur UtilisateurDemandeur { get; set; }
        public Utilisateur UtilisateurCible { get; set; }
    }
}

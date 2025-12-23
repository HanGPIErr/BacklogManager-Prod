using System;

namespace BacklogManager.Domain
{
    /// <summary>
    /// Représente l'assignation d'un membre de l'équipe pour gérer la VM un jour spécifique
    /// </summary>
    public class PlanningVMJour
    {
        public int Id { get; set; }
        public int EquipeId { get; set; }                  // Référence vers l'équipe
        public DateTime Date { get; set; }                 // Date du jour planifié
        public int? UtilisateurId { get; set; }            // Membre assigné (null si personne)
        public DateTime? DateAssignation { get; set; }     // Quand l'assignation a été faite
        public string Commentaire { get; set; }            // Commentaire optionnel
        
        // Navigation properties
        public Equipe Equipe { get; set; }
        public Utilisateur Utilisateur { get; set; }
    }
}

using System;

namespace BacklogManager.Domain
{
    public class Utilisateur
    {
        public int Id { get; set; }
        public string UsernameWindows { get; set; } // Ex: J12222, J04831
        public string Nom { get; set; }
        public string Prenom { get; set; }
        public string Email { get; set; }
        public int RoleId { get; set; }
        public bool Actif { get; set; }
        public DateTime DateCreation { get; set; }
        public DateTime? DateDerniereConnexion { get; set; }
        
        // Phase 1 : Gestion des équipes
        public int? EquipeId { get; set; }                  // Équipe principale de l'utilisateur
        public Equipe Equipe { get; set; }                  // Navigation property
        
        // Statut de l'utilisateur : BAU, PROJECTS, Temporary, Hiring ongoing
        public string Statut { get; set; }
        
        // Propriété calculée pour l'affichage
        public string NomComplet => $"{Prenom} {Nom}";
    }
}

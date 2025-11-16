using System;

namespace BacklogManager.Domain
{
    public class AuditLog
    {
        public int Id { get; set; }
        public string Action { get; set; } // CREATE, UPDATE, DELETE, LOGIN, LOGOUT, etc.
        public int UserId { get; set; }
        public string Username { get; set; }
        public string EntityType { get; set; } // BacklogItem, Projet, Utilisateur, Role, Dev, etc.
        public int? EntityId { get; set; }
        public string OldValue { get; set; } // JSON ou texte de l'état avant
        public string NewValue { get; set; } // JSON ou texte de l'état après
        public DateTime DateAction { get; set; }
        public string Details { get; set; } // Informations supplémentaires
    }
}

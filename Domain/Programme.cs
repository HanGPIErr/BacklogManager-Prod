using System;

namespace BacklogManager.Domain
{
    public class Programme
    {
        public int Id { get; set; }
        public string Nom { get; set; }                     // Ex: "DWINGS"
        public string Code { get; set; }                    // Ex: "DWG"
        public string Description { get; set; }
        public string Objectifs { get; set; }
        public int? ResponsableId { get; set; }             // Programme Manager (FK vers Utilisateurs)
        public DateTime? DateDebut { get; set; }
        public DateTime? DateFinCible { get; set; }
        public string StatutGlobal { get; set; }            // "On Track", "At Risk", "Delayed"
        public bool Actif { get; set; } = true;
        public DateTime DateCreation { get; set; }

        // Navigation properties
        public Utilisateur Responsable { get; set; }
    }
}

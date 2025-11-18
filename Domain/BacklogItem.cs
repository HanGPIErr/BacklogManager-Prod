using System;

namespace BacklogManager.Domain
{
    public class BacklogItem
    {
        public int Id { get; set; }
        public string Titre { get; set; }
        public string Description { get; set; }
        public TypeDemande TypeDemande { get; set; }
        public Statut Statut { get; set; }
        public Priorite Priorite { get; set; }
        public int? DevAssigneId { get; set; }
        public int? ProjetId { get; set; }
        public int? SprintId { get; set; }
        public int? DemandeId { get; set; } // Lien vers la demande métier d'origine
        public int? Complexite { get; set; }
        public double? ChiffrageHeures { get; set; } // Estimation en heures
        public double? TempsReelHeures { get; set; } // Temps réel passé
        public DateTime? DateFinAttendue { get; set; }
        public DateTime? DateDebut { get; set; }
        public DateTime? DateFin { get; set; }
        public DateTime DateCreation { get; set; }
        public DateTime DateDerniereMaj { get; set; }
        public bool EstArchive { get; set; }

        // Support : si TypeDemande == Support
        public int? DevSupporte { get; set; } // ID du dev qu'on aide
        public int? TacheSupportee { get; set; } // ID de la tâche sur laquelle on aide

        // Propriétés calculées pour affichage en jours (1j = 8h)
        public double? ChiffrageJours => ChiffrageHeures.HasValue ? ChiffrageHeures.Value / 8.0 : (double?)null;
        public double? TempsReelJours => TempsReelHeures.HasValue ? TempsReelHeures.Value / 8.0 : (double?)null;

        // Indique si la tâche doit être visible dans le Kanban
        public bool EstVisibleDansKanban => TypeDemande != TypeDemande.Conges && 
                                            TypeDemande != TypeDemande.NonTravaille && 
                                            TypeDemande != TypeDemande.Support;

        public BacklogItem()
        {
            DateCreation = DateTime.Now;
            DateDerniereMaj = DateTime.Now;
            EstArchive = false;
        }
    }
}

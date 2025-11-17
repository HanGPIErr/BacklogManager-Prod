using System;

namespace BacklogManager.Domain
{
    public enum StatutDemande
    {
        EnAttenteSpecification,
        EnAttenteChiffrage,
        EnAttenteArbitrage,
        Acceptee,
        Refusee,
        PlanifieeEnUS,
        EnCours,
        Livree
    }

    public enum Criticite
    {
        Basse,
        Moyenne,
        Haute,
        Bloquante
    }

    public class Demande
    {
        public int Id { get; set; }
        public string Titre { get; set; }
        public string Description { get; set; }
        public string Specifications { get; set; } // Spécifications détaillées
        public string ContexteMetier { get; set; } // Contexte métier
        public string BeneficesAttendus { get; set; } // Bénéfices attendus
        public int DemandeurId { get; set; } // Utilisateur qui a créé la demande
        public int? ProjetId { get; set; } // Projet associé à la demande
        public int? BusinessAnalystId { get; set; } // BA assigné
        public int? ChefProjetId { get; set; } // CP/PO assigné
        public int? DevChiffreurId { get; set; } // Dev affecté pour chiffrage
        public TypeDemande Type { get; set; }
        public Criticite Criticite { get; set; }
        public StatutDemande Statut { get; set; }
        public DateTime DateCreation { get; set; }
        public DateTime? DateValidationChiffrage { get; set; }
        public DateTime? DateAcceptation { get; set; }
        public DateTime? DateLivraison { get; set; }
        public double? ChiffrageEstimeJours { get; set; } // En jours (0.5 = demi-journée)
        public double? ChiffrageReelJours { get; set; } // En jours
        public DateTime? DatePrevisionnelleImplementation { get; set; }
        public string JustificationRefus { get; set; }
        public bool EstArchivee { get; set; }
    }
}

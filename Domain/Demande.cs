using System;

namespace BacklogManager.Domain
{
    public enum StatutDemande
    {
        EnAttenteSpecification,
        EnAttenteChiffrage,
        EnAttenteValidationManager,
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
        
        // Phase 2 : Enrichissement avec structure Programme
        public int? ProgrammeId { get; set; } // Programme associé
        public string Priorite { get; set; } // "Top High", "High", "Medium", "Low"
        public string Drivers { get; set; } // JSON array : ["Automation", "Efficiency Gains", ...]
        public string Ambition { get; set; } // "Automation Rate Increase", "Workload Gain", etc.
        public string Beneficiaires { get; set; } // JSON array : ["SGI", "TFSC", "Transversal"]
        public string GainsTemps { get; set; } // "X heures/semaine", "X jours/mois", "X% workload"
        public string GainsFinanciers { get; set; } // "X€ mensuels/annuels" ou "N/A"
        public string LeadProjet { get; set; } // "GTTO", "CCI", "Autre"
        public string TypeProjet { get; set; } // "Data", "Digital", "Regulatory", "Run", "Transformation"
        public string Categorie { get; set; } // "BAU", "TRANSFO"
        public bool EstImplemente { get; set; } // Implémentation oui/non
        public System.Collections.Generic.List<int> EquipesAssigneesIds { get; set; } = new System.Collections.Generic.List<int>(); // Équipes assignées à la demande
        
        // Champs temporaires pour l'analyse IA (non persistés en DB)
        public string Programme { get; set; } // Nom/Code du programme (temporaire, converti en ProgrammeId)
        public string Equipes { get; set; } // JSON array des noms d'équipes (temporaire, converti en EquipesAssigneesIds)
    }
}

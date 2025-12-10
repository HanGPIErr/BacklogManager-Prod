using System;
using System.Collections.Generic;

namespace BacklogManager.Domain
{
    public class Projet
    {
        public int Id { get; set; }
        public string Nom { get; set; }
        public string Description { get; set; }
        public DateTime DateCreation { get; set; }
        public DateTime? DateDebut { get; set; } // Date de début du projet
        public DateTime? DateFinPrevue { get; set; } // Date de fin prévue du projet
        public bool Actif { get; set; }
        public string CouleurHex { get; set; } // Couleur pour identification visuelle (ex: "#FF5722")
        
        // Phase 1 : Gestion des équipes
        public List<int> EquipesAssigneesIds { get; set; } = new List<int>(); // Plusieurs équipes possibles
        
        // Phase 2 : Gestion des programmes et enrichissement
        public int? ProgrammeId { get; set; }
        public Programme Programme { get; set; }
        
        // Phase 2 : Métadonnées enrichies
        public bool EstImplemente { get; set; }                    // Implémentation : oui/non
        public string TypeProjet { get; set; }                     // Data, Digital, Regulatory, Run, Transformation
        public string Categorie { get; set; }                       // BAU, TRANSFO
        public string Priorite { get; set; }                        // Top High, High, Medium, Low
        public string Drivers { get; set; }                         // JSON array - Automation, Efficiency Gains, etc.
        public string Ambition { get; set; }                        // Automation Rate Increase, Pricing Alignment, etc.
        public string Beneficiaires { get; set; }                   // JSON array - SGI, TFSC, Transversal
        public string GainsTemps { get; set; }                      // "X heures/semaine", "X jours/mois", etc.
        public string GainsFinanciers { get; set; }                 // "X€ mensuels/annuels" ou "N/A"
        public string LeadProjet { get; set; }                      // GTTO, CCI, Autre
        public string Timeline { get; set; }                        // JSON - { milestones: [], progress: "" }
        public string TargetDelivery { get; set; }                  // Ex: "Q3 2026"
        public string PerimetreProchainComite { get; set; }         // Texte libre
        public string NextActions { get; set; }                     // Texte libre
        public string StatutRAG { get; set; }                       // "Green", "Amber", "Red"

        // Palette de couleurs prédéfinies pour projets
        public static readonly string[] CouleursPalette = new[]
        {
            "#00915A", // BNP Green
            "#2196F3", // Blue
            "#FF9800", // Orange
            "#9C27B0", // Purple
            "#E91E63", // Pink
            "#4CAF50", // Green
            "#FF5722", // Deep Orange
            "#009688", // Teal
            "#795548", // Brown
            "#607D8B"  // Blue Grey
        };

        public Projet()
        {
            DateCreation = DateTime.Now;
            Actif = true;
            // Assigner une couleur aléatoire par défaut
            var random = new Random(Guid.NewGuid().GetHashCode());
            CouleurHex = CouleursPalette[random.Next(CouleursPalette.Length)];
        }
    }
}

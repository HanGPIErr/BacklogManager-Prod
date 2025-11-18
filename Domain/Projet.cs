using System;

namespace BacklogManager.Domain
{
    public class Projet
    {
        public int Id { get; set; }
        public string Nom { get; set; }
        public string Description { get; set; }
        public DateTime DateCreation { get; set; }
        public bool Actif { get; set; }
        public string CouleurHex { get; set; } // Couleur pour identification visuelle (ex: "#FF5722")

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

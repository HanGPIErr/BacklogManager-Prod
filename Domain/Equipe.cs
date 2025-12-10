using System;

namespace BacklogManager.Domain
{
    public class Equipe
    {
        public int Id { get; set; }
        public string Nom { get; set; }                    // Ex: "Transformation & Implementation"
        public string Code { get; set; }                   // Ex: "TRANSFO_IMPLEM"
        public string Description { get; set; }
        public string PerimetreFonctionnel { get; set; }
        public int? ManagerId { get; set; }                // Référence vers Utilisateur (manager)
        public string Contact { get; set; }                // Email ou autre
        public bool Actif { get; set; } = true;
        public DateTime DateCreation { get; set; } = DateTime.Now;

        // Navigation properties (optionnel, pour faciliter l'utilisation)
        public Utilisateur Manager { get; set; }
    }
}

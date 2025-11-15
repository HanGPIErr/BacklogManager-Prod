using System;

namespace BacklogManager.Domain
{
    public class Commentaire
    {
        public int Id { get; set; }
        public int? DemandeId { get; set; }
        public int? BacklogItemId { get; set; }
        public int AuteurId { get; set; }
        public string Contenu { get; set; }
        public DateTime DateCreation { get; set; }
        public string PieceJointeNom { get; set; }
        public string PieceJointeChemin { get; set; }
    }
}

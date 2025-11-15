using System;

namespace BacklogManager.Domain
{
    public enum TypeIndisponibilite
    {
        Conges,
        Absence,
        Formation,
        Autre
    }

    public class Disponibilite
    {
        public int Id { get; set; }
        public int UtilisateurId { get; set; }
        public TypeIndisponibilite Type { get; set; }
        public DateTime DateDebut { get; set; }
        public DateTime DateFin { get; set; }
        public string Motif { get; set; }
        public bool EstValide { get; set; } // Validé par le chef de projet
    }
}

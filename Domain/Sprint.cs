using System;

namespace BacklogManager.Domain
{
    public class Sprint
    {
        public int Id { get; set; }
        public string Nom { get; set; }
        public DateTime DateDebut { get; set; }
        public DateTime DateFin { get; set; }
        public string Objectif { get; set; }
        public bool EstActif { get; set; }
        public bool EstCloture { get; set; }
    }
}

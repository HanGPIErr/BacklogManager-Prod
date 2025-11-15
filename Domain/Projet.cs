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

        public Projet()
        {
            DateCreation = DateTime.Now;
            Actif = true;
        }
    }
}

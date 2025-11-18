using System;

namespace BacklogManager.Domain
{
    public class CRA
    {
        public int Id { get; set; }
        public int BacklogItemId { get; set; }
        public int DevId { get; set; }
        public DateTime Date { get; set; }
        public double HeuresTravaillees { get; set; }
        public string Commentaire { get; set; }
        public DateTime DateCreation { get; set; }
        public bool EstPrevisionnel { get; set; } // True si CRA pour date future (prévisionnel)
        public bool EstValide { get; set; } // True si CRA validé par le dev (compte dans temps réel)

        // Propriété calculée : CRA prévisionnel à valider (date passée mais pas encore validé)
        public bool EstAValider => EstPrevisionnel && Date.Date < DateTime.Now.Date && !EstValide;

        public CRA()
        {
            DateCreation = DateTime.Now;
            Date = DateTime.Today;
            EstPrevisionnel = false;
            EstValide = false;
        }
    }
}

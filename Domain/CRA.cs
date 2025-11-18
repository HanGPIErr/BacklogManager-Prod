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

        public CRA()
        {
            DateCreation = DateTime.Now;
            Date = DateTime.Today;
            EstPrevisionnel = false;
        }
    }
}

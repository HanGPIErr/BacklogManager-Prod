using System;

namespace BacklogManager.Domain
{
    public class PokerSession
    {
        public int Id { get; set; }
        public int BacklogItemId { get; set; }
        public DateTime DateSession { get; set; }
        public int? ComplexiteConsensus { get; set; }
        public double? JoursPlanifies { get; set; }

        public PokerSession()
        {
            DateSession = DateTime.Now;
        }
    }
}

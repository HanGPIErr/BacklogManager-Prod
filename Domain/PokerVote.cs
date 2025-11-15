namespace BacklogManager.Domain
{
    public class PokerVote
    {
        public int Id { get; set; }
        public int PokerSessionId { get; set; }
        public int DevId { get; set; }
        public int ValeurVote { get; set; }
    }
}

namespace BacklogManager.Domain
{
    public class Dev
    {
        public int Id { get; set; }
        public string Nom { get; set; }
        public string Initiales { get; set; }
        public bool Actif { get; set; }

        public Dev()
        {
            Actif = true;
        }
    }
}

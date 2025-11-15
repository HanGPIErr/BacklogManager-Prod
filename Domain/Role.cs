namespace BacklogManager.Domain
{
    public enum RoleType
    {
        Administrateur,
        BusinessAnalyst,
        ChefDeProjet,
        Developpeur
    }

    public class Role
    {
        public int Id { get; set; }
        public string Nom { get; set; }
        public RoleType Type { get; set; }
        public bool PeutCreerDemandes { get; set; }
        public bool PeutChiffrer { get; set; }
        public bool PeutPrioriser { get; set; }
        public bool PeutGererUtilisateurs { get; set; }
        public bool PeutVoirKPI { get; set; }
        public bool PeutGererReferentiels { get; set; }
        public bool PeutModifierTaches { get; set; }
        public bool PeutSupprimerTaches { get; set; }
        public bool Actif { get; set; }
    }
}

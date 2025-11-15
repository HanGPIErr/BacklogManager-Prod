using System;

namespace BacklogManager.Domain
{
    public enum TypeModification
    {
        Creation,
        Modification,
        Suppression,
        ChangementStatut,
        Affectation,
        Chiffrage
    }

    public class HistoriqueModification
    {
        public int Id { get; set; }
        public string TypeEntite { get; set; } // "Demande", "BacklogItem", "Utilisateur", etc.
        public int EntiteId { get; set; }
        public TypeModification TypeModification { get; set; }
        public int UtilisateurId { get; set; }
        public DateTime DateModification { get; set; }
        public string AncienneValeur { get; set; }
        public string NouvelleValeur { get; set; }
        public string ChampModifie { get; set; }
    }
}

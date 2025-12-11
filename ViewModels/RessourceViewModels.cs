using System.Collections.Generic;
using System.Windows.Media;

namespace BacklogManager.ViewModels
{
    // ViewModels pour la page ressources (partagé entre StatistiquesView et RessourcesEquipeDetailWindow)
    public class RessourceDetailViewModel
    {
        public int Id { get; set; }
        public string Nom { get; set; }
        public string Role { get; set; }
        public int? EquipeId { get; set; }
        public string NomEquipe { get; set; }
        public string CodeEquipe { get; set; }
        public string NomManager { get; set; }
        public int NbProjets { get; set; }
        public int NbTachesActives { get; set; }
        public string NiveauCharge { get; set; }
        public Color CouleurCharge { get; set; }
        public SolidColorBrush CouleurChargeBrush { get; set; }
        public double LargeurBarreCharge { get; set; }
        public List<ProjetDetailViewModel> ListeProjets { get; set; }
        public bool AucunProjet { get; set; }
    }

    public class ProjetDetailViewModel
    {
        public string Nom { get; set; }
        public int NbTaches { get; set; }
    }
}

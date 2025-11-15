using System;
using System.Linq;
using System.Windows;
using BacklogManager.Domain;
using BacklogManager.Services;

namespace BacklogManager.Views
{
    public partial class DetailsDemandeWindow : Window
    {
        private readonly int _demandeId;
        private readonly IDatabase _database;

        public DetailsDemandeWindow(int demandeId, IDatabase database)
        {
            InitializeComponent();
            _demandeId = demandeId;
            _database = database;
            
            ChargerDetails();
        }

        private void OnFermer(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ChargerDetails()
        {
            var demandes = _database.GetDemandes();
            var demande = demandes.FirstOrDefault(d => d.Id == _demandeId);
            
            if (demande == null)
            {
                MessageBox.Show("Demande introuvable.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
                return;
            }

            var utilisateurs = _database.GetUtilisateurs();
            
            TxtTitre.Text = demande.Titre;
            TxtDescription.Text = demande.Description;
            TxtType.Text = demande.Type.ToString();
            TxtCriticite.Text = demande.Criticite.ToString();
            TxtStatut.Text = FormatStatut(demande.Statut);
            
            TxtDemandeur.Text = ObtenirNomUtilisateur(demande.DemandeurId, utilisateurs);
            TxtBA.Text = ObtenirNomUtilisateur(demande.BusinessAnalystId, utilisateurs);
            TxtCP.Text = ObtenirNomUtilisateur(demande.ChefProjetId, utilisateurs);
            TxtDev.Text = ObtenirNomUtilisateur(demande.DevChiffreurId, utilisateurs);
            
            TxtChiffrage.Text = demande.ChiffrageEstimeHeures.HasValue ? 
                string.Format("{0:F1}", demande.ChiffrageEstimeHeures.Value) : "Non chiffré";
            TxtDateCreation.Text = demande.DateCreation.ToString("dd/MM/yyyy HH:mm");
            
            if (demande.DatePrevisionnelleImplementation.HasValue)
                TxtDatePrev.Text = demande.DatePrevisionnelleImplementation.Value.ToString("dd/MM/yyyy");
            else
                TxtDatePrev.Text = "Non définie";
        }

        private string ObtenirNomUtilisateur(int? userId, System.Collections.Generic.List<Utilisateur> utilisateurs)
        {
            if (!userId.HasValue) return "Non assigné";
            var user = utilisateurs.FirstOrDefault(u => u.Id == userId.Value);
            return user != null ? string.Format("{0} {1}", user.Prenom, user.Nom) : "Non assigné";
        }

        private string FormatStatut(StatutDemande statut)
        {
            switch (statut)
            {
                case StatutDemande.EnAttenteSpecification:
                    return "En attente spécification";
                case StatutDemande.EnAttenteChiffrage:
                    return "En attente chiffrage";
                case StatutDemande.EnAttenteArbitrage:
                    return "En attente arbitrage";
                case StatutDemande.Acceptee:
                    return "Acceptée";
                case StatutDemande.PlanifieeEnUS:
                    return "Planifiée en US";
                case StatutDemande.EnCours:
                    return "En cours";
                case StatutDemande.Livree:
                    return "Livrée";
                case StatutDemande.Refusee:
                    return "Refusée";
                default:
                    return statut.ToString();
            }
        }
    }
}

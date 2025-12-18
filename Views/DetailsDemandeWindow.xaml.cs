using System;
using System.Collections.Generic;
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
            var equipes = _database.GetAllEquipes();
            
            TxtTitre.Text = demande.Titre;
            TxtDescription.Text = demande.Description;
            TxtType.Text = demande.Type.ToString();
            TxtCriticite.Text = demande.Criticite.ToString();
            TxtStatut.Text = FormatStatut(demande.Statut);
            
            // En-tête enrichi
            TxtHeaderSubtitle.Text = string.Format("Demande #{0} • Créée le {1}", demande.Id, demande.DateCreation.ToString("dd/MM/yyyy"));
            TxtStatutBadge.Text = FormatStatut(demande.Statut);
            BadgeStatut.Background = GetStatutColor(demande.Statut);
            
            TxtContexte.Text = !string.IsNullOrWhiteSpace(demande.ContexteMetier) ? demande.ContexteMetier : "Non renseigné";
            TxtBenefices.Text = !string.IsNullOrWhiteSpace(demande.BeneficesAttendus) ? demande.BeneficesAttendus : "Non renseigné";
            
            TxtDemandeur.Text = ObtenirNomUtilisateur(demande.DemandeurId, utilisateurs);
            TxtBA.Text = ObtenirNomUtilisateur(demande.BusinessAnalystId, utilisateurs);
            TxtCP.Text = ObtenirManagersEquipes(demande.EquipesAssigneesIds, equipes, utilisateurs);
            TxtDev.Text = ObtenirNomUtilisateur(demande.DevChiffreurId, utilisateurs);
            
            TxtChiffrage.Text = demande.ChiffrageEstimeJours.HasValue ? 
                string.Format("{0:F1} jour(s)", demande.ChiffrageEstimeJours.Value) : "Non chiffré";
            TxtDateCreation.Text = demande.DateCreation.ToString("dd/MM/yyyy HH:mm");
            
            if (demande.DatePrevisionnelleImplementation.HasValue)
                TxtDatePrev.Text = demande.DatePrevisionnelleImplementation.Value.ToString("dd/MM/yyyy");
            else
                TxtDatePrev.Text = "Non définie";

            // === CHAMPS PHASE 2 ===
            
            // Programme
            if (demande.ProgrammeId.HasValue)
            {
                var programmes = _database.GetAllProgrammes();
                var programme = programmes.FirstOrDefault(p => p.Id == demande.ProgrammeId.Value);
                TxtProgramme.Text = programme != null ? programme.Nom : "Non défini";
            }
            else
            {
                TxtProgramme.Text = "Aucun";
            }
            
            // Priorité, Type, Catégorie, Lead
            TxtPriorite.Text = !string.IsNullOrWhiteSpace(demande.Priorite) ? demande.Priorite : "Non définie";
            TxtTypeProjet.Text = !string.IsNullOrWhiteSpace(demande.TypeProjet) ? demande.TypeProjet : "Non défini";
            TxtCategorie.Text = !string.IsNullOrWhiteSpace(demande.Categorie) ? demande.Categorie : "Non définie";
            TxtLeadProjet.Text = !string.IsNullOrWhiteSpace(demande.LeadProjet) ? demande.LeadProjet : "Non défini";
            TxtEstImplemente.Text = demande.EstImplemente ? "Oui" : "Non";
            
            // Drivers (désérialiser JSON)
            if (!string.IsNullOrWhiteSpace(demande.Drivers))
            {
                try
                {
                    var drivers = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.List<string>>(demande.Drivers);
                    TxtDrivers.Text = drivers != null && drivers.Count > 0 ? string.Join(", ", drivers) : "Aucun";
                }
                catch
                {
                    TxtDrivers.Text = "Aucun";
                }
            }
            else
            {
                TxtDrivers.Text = "Aucun";
            }
            
            // Ambition
            TxtAmbition.Text = !string.IsNullOrWhiteSpace(demande.Ambition) ? demande.Ambition : "N/A";
            
            // Bénéficiaires (désérialiser JSON)
            if (!string.IsNullOrWhiteSpace(demande.Beneficiaires))
            {
                try
                {
                    var beneficiaires = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.List<string>>(demande.Beneficiaires);
                    TxtBeneficiaires.Text = beneficiaires != null && beneficiaires.Count > 0 ? string.Join(", ", beneficiaires) : "Non définis";
                }
                catch
                {
                    TxtBeneficiaires.Text = "Non définis";
                }
            }
            else
            {
                TxtBeneficiaires.Text = "Non définis";
            }
            
            // Gains
            TxtGainsTemps.Text = !string.IsNullOrWhiteSpace(demande.GainsTemps) ? demande.GainsTemps : "Non définis";
            TxtGainsFinanciers.Text = !string.IsNullOrWhiteSpace(demande.GainsFinanciers) ? demande.GainsFinanciers : "N/A";
            
            // Équipes Assignées
            if (demande.EquipesAssigneesIds != null && demande.EquipesAssigneesIds.Count > 0)
            {
                var toutesEquipes = _database.GetAllEquipes();
                var equipesAssignees = toutesEquipes
                    .Where(e => demande.EquipesAssigneesIds.Contains(e.Id))
                    .Select(e => new { Nom = e.Nom })
                    .ToList();

                if (equipesAssignees.Count > 0)
                {
                    ListeEquipes.ItemsSource = equipesAssignees;
                    TxtAucuneEquipe.Visibility = Visibility.Collapsed;
                }
                else
                {
                    ListeEquipes.ItemsSource = null;
                    TxtAucuneEquipe.Visibility = Visibility.Visible;
                }
            }
            else
            {
                ListeEquipes.ItemsSource = null;
                TxtAucuneEquipe.Visibility = Visibility.Visible;
            }
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
                case StatutDemande.EnAttenteValidationManager:
                    return "En attente validation manager";
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

        private System.Windows.Media.Brush GetStatutColor(StatutDemande statut)
        {
            switch (statut)
            {
                case StatutDemande.EnAttenteSpecification:
                case StatutDemande.EnAttenteChiffrage:
                case StatutDemande.EnAttenteValidationManager:
                    return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 152, 0)); // Orange
                case StatutDemande.Acceptee:
                case StatutDemande.PlanifieeEnUS:
                    return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(33, 150, 243)); // Bleu
                case StatutDemande.EnCours:
                    return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(156, 39, 176)); // Violet
                case StatutDemande.Livree:
                    return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(76, 175, 80)); // Vert
                case StatutDemande.Refusee:
                    return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(244, 67, 54)); // Rouge
                default:
                    return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(158, 158, 158)); // Gris
            }
        }

        private string ObtenirManagersEquipes(List<int> equipesIds, List<Equipe> equipes, List<Utilisateur> utilisateurs)
        {
            if (equipesIds == null || equipesIds.Count == 0)
                return "Non assigné";

            var managers = new List<string>();
            foreach (var equipeId in equipesIds)
            {
                var equipe = equipes.FirstOrDefault(e => e.Id == equipeId);
                if (equipe != null && equipe.ManagerId.HasValue)
                {
                    var managerNom = ObtenirNomUtilisateur(equipe.ManagerId.Value, utilisateurs);
                    if (!string.IsNullOrEmpty(managerNom) && managerNom != "Non assigné" && !managers.Contains(managerNom))
                    {
                        managers.Add(managerNom);
                    }
                }
            }

            return managers.Count > 0 ? string.Join(", ", managers) : "Non assigné";
        }
    }
}

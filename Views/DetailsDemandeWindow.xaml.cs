using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
            
            InitialiserTextes();
            ChargerDetails();
            
            LocalizationService.Instance.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(LocalizationService.CurrentCulture))
                {
                    InitialiserTextes();
                    ChargerDetails(); // Refresh pour les statuts traduits
                }
            };
        }

        private void InitialiserTextes()
        {
            Title = LocalizationService.Instance["Requests_Details"];
            
            // Traduction directe des sections principales
            TxtMainTitle.Text = LocalizationService.Instance["Requests_Details"];
            TxtTitleSection.Text = "📌 " + LocalizationService.Instance["Requests_TitleSection"];
            TxtDescriptionSection.Text = "📝 " + LocalizationService.Instance["Common_Description"];
            TxtGeneralInfoSection.Text = "ℹ️ " + LocalizationService.Instance["Requests_GeneralInfo"];
            TxtBusinessContextSection.Text = "🏬 " + LocalizationService.Instance["Requests_BusinessContext"];
            TxtExpectedBenefitsSection.Text = "✨ " + LocalizationService.Instance["Requests_ExpectedBenefits"];
            TxtStakeholdersSection.Text = "👥 " + LocalizationService.Instance["Requests_Stakeholders"];
            
            // Sections phase 2
            TxtEstimationSection.Text = LocalizationService.Instance["Requests_Estimation"];
            TxtProgramClassificationSection.Text = LocalizationService.Instance["Requests_ProgramClassification"];
            TxtDriversAmbitionSection.Text = LocalizationService.Instance["Requests_DriversAmbition"];
            TxtExpectedGainsSection.Text = LocalizationService.Instance["Requests_ExpectedGains"];
            TxtAssignedTeamsSection.Text = "🏢 " + LocalizationService.Instance["Requests_AssignedTeams"];
            
            // Sous-labels
            TxtEstimationLabel.Text = LocalizationService.Instance["Requests_EstimationLabel"] + ":";
            TxtProgramLabel.Text = LocalizationService.Instance["Requests_Program"];
            TxtPriorityLabel.Text = LocalizationService.Instance["Requests_PriorityLabel"];
            TxtProjectTypeLabel.Text = LocalizationService.Instance["Requests_ProjectType"];
            TxtCategoryLabel.Text = LocalizationService.Instance["Requests_Category"];
            TxtLeadProjectLabel.Text = LocalizationService.Instance["Requests_ProjectLead"];
            TxtImplementedLabel.Text = LocalizationService.Instance["Requests_Implemented"];
            TxtDriversLabel.Text = LocalizationService.Instance["Requests_Drivers"];
            TxtAmbitionLabel.Text = LocalizationService.Instance["Requests_Ambition"];
            TxtBeneficiariesLabel.Text = LocalizationService.Instance["Requests_Beneficiaries"];
            TxtTimeGainsLabel.Text = LocalizationService.Instance["Requests_TimeGains"];
            TxtFinancialGainsLabel.Text = LocalizationService.Instance["Requests_FinancialGains"];

            // Find remaining TextBlocks in XAML to translate
            foreach (var child in FindVisualChildren<TextBlock>(this))
            {
                if (child.Text == "TYPE")
                    child.Text = LocalizationService.Instance["Requests_TypeLabel"];
                else if (child.Text == "CRITICITÉ")
                    child.Text = LocalizationService.Instance["Requests_CriticalityLabel"];
                else if (child.Text == "STATUT")
                    child.Text = LocalizationService.Instance["Common_Status"];
                else if (child.Text == "DATE CRÉATION")
                    child.Text = LocalizationService.Instance["Requests_DateCreation"];
                else if (child.Text == "📅 DATE PRÉVISIONNELLE")
                    child.Text = "📅 " + LocalizationService.Instance["Requests_ExpectedDate"];
                else if (child.Text == "Demandeur:")
                    child.Text = LocalizationService.Instance["Requests_Requester"] + ":";
                else if (child.Text == "Business Analyst:")
                    child.Text = LocalizationService.Instance["Requests_BusinessAnalyst"] + ":";
                else if (child.Text == "Manager(s):")
                    child.Text = LocalizationService.Instance["Requests_Managers"] + ":";
                else if (child.Text == "Développeur:")
                    child.Text = LocalizationService.Instance["Requests_Developer"] + ":";
                else if (child.Text == " heures")
                    child.Text = " " + LocalizationService.Instance["Common_Hours"];
                else if (child.Text == "🏢 ÉQUIPES ASSIGNÉES")
                    child.Text = "🏢 " + LocalizationService.Instance["Requests_AssignedTeams"];
                else if (child.Text == "Aucune équipe assignée")
                    child.Text = LocalizationService.Instance["Requests_NoTeamAssigned"];
            }
            
            // Traduire le bouton Fermer
            BtnFermer.Content = LocalizationService.Instance["Close"];
        }

        private IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
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
            TxtType.Text = FormatType(demande.Type);
            TxtCriticite.Text = FormatCriticite(demande.Criticite);
            TxtStatut.Text = FormatStatut(demande.Statut);
            
            // En-tête enrichi
            TxtHeaderSubtitle.Text = string.Format(LocalizationService.Instance["Requests_HeaderSubtitle"], demande.Id, demande.DateCreation.ToString("dd/MM/yyyy"));
            TxtStatutBadge.Text = FormatStatut(demande.Statut);
            BadgeStatut.Background = GetStatutColor(demande.Statut);
            
            TxtContexte.Text = !string.IsNullOrWhiteSpace(demande.ContexteMetier) ? demande.ContexteMetier : LocalizationService.Instance["Common_NotSpecified"];
            TxtBenefices.Text = !string.IsNullOrWhiteSpace(demande.BeneficesAttendus) ? demande.BeneficesAttendus : LocalizationService.Instance["Common_NotSpecified"];
            
            TxtDemandeur.Text = ObtenirNomUtilisateur(demande.DemandeurId, utilisateurs);
            TxtBA.Text = ObtenirNomUtilisateur(demande.BusinessAnalystId, utilisateurs);
            TxtCP.Text = ObtenirManagersEquipes(demande.EquipesAssigneesIds, equipes, utilisateurs);
            TxtDev.Text = ObtenirNomUtilisateur(demande.DevChiffreurId, utilisateurs);
            
            TxtChiffrage.Text = demande.ChiffrageEstimeJours.HasValue ? 
                string.Format("{0:F1} " + LocalizationService.Instance["Common_Days"], demande.ChiffrageEstimeJours.Value) : LocalizationService.Instance["Requests_NotEstimated"];
            TxtDateCreation.Text = demande.DateCreation.ToString("dd/MM/yyyy HH:mm");
            
            if (demande.DatePrevisionnelleImplementation.HasValue)
                TxtDatePrev.Text = demande.DatePrevisionnelleImplementation.Value.ToString("dd/MM/yyyy");
            else
                TxtDatePrev.Text = LocalizationService.Instance["Common_NotDefined"];

            // === CHAMPS PHASE 2 ===
            
            // Programme
            if (demande.ProgrammeId.HasValue)
            {
                var programmes = _database.GetAllProgrammes();
                var programme = programmes.FirstOrDefault(p => p.Id == demande.ProgrammeId.Value);
                TxtProgramme.Text = programme != null ? programme.Nom : LocalizationService.Instance["Common_NotDefined"];
            }
            else
            {
                TxtProgramme.Text = LocalizationService.Instance["Common_None"];
            }
            
            // Priorité, Type, Catégorie, Lead
            TxtPriorite.Text = !string.IsNullOrWhiteSpace(demande.Priorite) ? demande.Priorite : LocalizationService.Instance["Common_NotDefined"];
            TxtTypeProjet.Text = !string.IsNullOrWhiteSpace(demande.TypeProjet) ? demande.TypeProjet : LocalizationService.Instance["Common_NotDefined"];
            TxtCategorie.Text = !string.IsNullOrWhiteSpace(demande.Categorie) ? demande.Categorie : LocalizationService.Instance["Common_NotDefined"];
            TxtLeadProjet.Text = !string.IsNullOrWhiteSpace(demande.LeadProjet) ? demande.LeadProjet : LocalizationService.Instance["Common_NotDefined"];
            TxtEstImplemente.Text = demande.EstImplemente ? LocalizationService.Instance["Common_Yes"] : LocalizationService.Instance["Common_No"];
            
            // Drivers (désérialiser JSON)
            if (!string.IsNullOrWhiteSpace(demande.Drivers))
            {
                try
                {
                    var drivers = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.List<string>>(demande.Drivers);
                    TxtDrivers.Text = drivers != null && drivers.Count > 0 ? string.Join(", ", drivers) : LocalizationService.Instance["Common_None"];
                }
                catch
                {
                    TxtDrivers.Text = LocalizationService.Instance["Common_None"];
                }
            }
            else
            {
                TxtDrivers.Text = LocalizationService.Instance["Common_None"];
            }
            
            // Ambition
            TxtAmbition.Text = !string.IsNullOrWhiteSpace(demande.Ambition) ? demande.Ambition : LocalizationService.Instance["Common_NotApplicable"];
            
            // Bénéficiaires (désérialiser JSON)
            if (!string.IsNullOrWhiteSpace(demande.Beneficiaires))
            {
                try
                {
                    var beneficiaires = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.List<string>>(demande.Beneficiaires);
                    TxtBeneficiaires.Text = beneficiaires != null && beneficiaires.Count > 0 ? string.Join(", ", beneficiaires) : LocalizationService.Instance["Common_NotDefined"];
                }
                catch
                {
                    TxtBeneficiaires.Text = LocalizationService.Instance["Common_NotDefined"];
                }
            }
            else
            {
                TxtBeneficiaires.Text = LocalizationService.Instance["Common_NotDefined"];
            }
            
            // Gains
            TxtGainsTemps.Text = !string.IsNullOrWhiteSpace(demande.GainsTemps) ? demande.GainsTemps : LocalizationService.Instance["Common_NotDefined"];
            TxtGainsFinanciers.Text = !string.IsNullOrWhiteSpace(demande.GainsFinanciers) ? demande.GainsFinanciers : LocalizationService.Instance["Common_NotApplicable"];
            
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
            if (!userId.HasValue) return LocalizationService.Instance["Common_NotAssigned"];
            var user = utilisateurs.FirstOrDefault(u => u.Id == userId.Value);
            return user != null ? string.Format("{0} {1}", user.Prenom, user.Nom) : LocalizationService.Instance["Common_NotAssigned"];
        }

        private string FormatStatut(StatutDemande statut)
        {
            switch (statut)
            {
                case StatutDemande.EnAttenteSpecification:
                    return LocalizationService.Instance["Requests_StatusPendingSpec"];
                case StatutDemande.EnAttenteChiffrage:
                    return LocalizationService.Instance["Requests_StatusPendingEstimate"];
                case StatutDemande.EnAttenteValidationManager:
                    return LocalizationService.Instance["Requests_StatusPendingValidation"];
                case StatutDemande.Acceptee:
                    return LocalizationService.Instance["Requests_StatusAccepted"];
                case StatutDemande.PlanifieeEnUS:
                    return LocalizationService.Instance["Requests_StatusPlannedUS"];
                case StatutDemande.EnCours:
                    return LocalizationService.Instance["Requests_StatusInProgress"];
                case StatutDemande.Livree:
                    return LocalizationService.Instance["Requests_StatusDelivered"];
                case StatutDemande.Refusee:
                    return LocalizationService.Instance["Requests_StatusRefused"];
                default:
                    return statut.ToString();
            }
        }

        private string FormatCriticite(Criticite criticite)
        {
            switch (criticite)
            {
                case Criticite.Haute:
                    return LocalizationService.Instance["Requests_CriticalityHigh"];
                case Criticite.Moyenne:
                    return LocalizationService.Instance["Requests_CriticalityMedium"];
                case Criticite.Basse:
                    return LocalizationService.Instance["Requests_CriticalityLow"];
                default:
                    return criticite.ToString();
            }
        }

        private string FormatType(TypeDemande type)
        {
            switch (type)
            {
                case TypeDemande.Dev:
                    return LocalizationService.Instance["Requests_TypeDev"];
                case TypeDemande.Run:
                    return LocalizationService.Instance["Requests_TypeRun"];
                case TypeDemande.Support:
                    return LocalizationService.Instance["Requests_TypeSupport"];
                case TypeDemande.Autre:
                    return LocalizationService.Instance["Requests_TypeOther"];
                case TypeDemande.Conges:
                    return LocalizationService.Instance["Requests_TypeVacation"];
                case TypeDemande.NonTravaille:
                    return LocalizationService.Instance["Requests_TypeNotWorked"];
                default:
                    return type.ToString();
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

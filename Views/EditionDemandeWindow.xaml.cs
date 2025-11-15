using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using BacklogManager.Domain;
using BacklogManager.Services;

namespace BacklogManager.Views
{
    public partial class EditionDemandeWindow : Window
    {
        private readonly IDatabase _database;
        private readonly AuthenticationService _authService;
        private readonly int? _demandeId;
        private Demande _demandeActuelle;

        public EditionDemandeWindow(IDatabase database, AuthenticationService authService, int? demandeId = null)
        {
            InitializeComponent();
            _database = database;
            _authService = authService;
            _demandeId = demandeId;

            InitialiserComboBoxes();
            
            if (_demandeId.HasValue)
            {
                TxtTitre.Text = "MODIFIER LA DEMANDE";
                ChargerDemande();
                PanelChiffrage.Visibility = Visibility.Visible;
            }
            else
            {
                _demandeActuelle = new Demande
                {
                    Id = 0,
                    DateCreation = DateTime.Now,
                    Statut = StatutDemande.EnAttenteSpecification,
                    DemandeurId = _authService.CurrentUser?.Id ?? 0
                };
            }

            BtnAnnuler.Click += (s, e) => { DialogResult = false; Close(); };
            BtnEnregistrer.Click += BtnEnregistrer_Click;
        }

        private void InitialiserComboBoxes()
        {
            // Type
            CmbType.ItemsSource = Enum.GetValues(typeof(TypeDemande)).Cast<TypeDemande>()
                .Select(t => new { Value = t, Display = FormatTypeDemande(t) });
            CmbType.DisplayMemberPath = "Display";
            CmbType.SelectedValuePath = "Value";
            CmbType.SelectedIndex = 0;

            // Criticité
            CmbCriticite.ItemsSource = Enum.GetValues(typeof(Criticite)).Cast<Criticite>();
            CmbCriticite.SelectedIndex = 0;

            // Utilisateurs
            var utilisateurs = _database.GetUtilisateurs().Where(u => u.Actif).ToList();
            var roles = _database.GetRoles();

            // Business Analysts
            var bas = utilisateurs.Where(u =>
            {
                var role = roles.FirstOrDefault(r => r.Id == u.RoleId);
                return role?.Type == RoleType.BusinessAnalyst;
            }).Select(u => new { Id = u.Id, Nom = string.Format("{0} {1}", u.Prenom, u.Nom) }).ToList();
            bas.Insert(0, new { Id = 0, Nom = "Non assigné" });
            CmbBusinessAnalyst.ItemsSource = bas;
            CmbBusinessAnalyst.DisplayMemberPath = "Nom";
            CmbBusinessAnalyst.SelectedValuePath = "Id";
            CmbBusinessAnalyst.SelectedIndex = 0;

            // Chefs de projet
            var cps = utilisateurs.Where(u =>
            {
                var role = roles.FirstOrDefault(r => r.Id == u.RoleId);
                return role?.Type == RoleType.ChefDeProjet;
            }).Select(u => new { Id = u.Id, Nom = string.Format("{0} {1}", u.Prenom, u.Nom) }).ToList();
            cps.Insert(0, new { Id = 0, Nom = "Non assigné" });
            CmbChefProjet.ItemsSource = cps;
            CmbChefProjet.DisplayMemberPath = "Nom";
            CmbChefProjet.SelectedValuePath = "Id";
            CmbChefProjet.SelectedIndex = 0;

            // Développeurs
            var devs = utilisateurs.Where(u =>
            {
                var role = roles.FirstOrDefault(r => r.Id == u.RoleId);
                return role?.Type == RoleType.Developpeur;
            }).Select(u => new { Id = u.Id, Nom = string.Format("{0} {1}", u.Prenom, u.Nom) }).ToList();
            devs.Insert(0, new { Id = 0, Nom = "Non assigné" });
            CmbDevChiffreur.ItemsSource = devs;
            CmbDevChiffreur.DisplayMemberPath = "Nom";
            CmbDevChiffreur.SelectedValuePath = "Id";
            CmbDevChiffreur.SelectedIndex = 0;
        }

        private string FormatTypeDemande(TypeDemande type)
        {
            switch (type)
            {
                case TypeDemande.Run:
                    return "Run";
                case TypeDemande.Dev:
                    return "Dev";
                default:
                    return type.ToString();
            }
        }

        private void ChargerDemande()
        {
            var demandes = _database.GetDemandes();
            _demandeActuelle = demandes.FirstOrDefault(d => d.Id == _demandeId.Value);

            if (_demandeActuelle == null)
            {
                MessageBox.Show("Demande introuvable.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
                return;
            }

            TxtTitreDemande.Text = _demandeActuelle.Titre;
            TxtDescription.Text = _demandeActuelle.Description;

            // Sélectionner les valeurs dans les combos
            CmbType.SelectedValue = _demandeActuelle.Type;
            CmbCriticite.SelectedValue = _demandeActuelle.Criticite;

            if (_demandeActuelle.BusinessAnalystId.HasValue)
                CmbBusinessAnalyst.SelectedValue = _demandeActuelle.BusinessAnalystId.Value;

            if (_demandeActuelle.ChefProjetId.HasValue)
                CmbChefProjet.SelectedValue = _demandeActuelle.ChefProjetId.Value;

            if (_demandeActuelle.DevChiffreurId.HasValue)
                CmbDevChiffreur.SelectedValue = _demandeActuelle.DevChiffreurId.Value;

            TxtChiffrageEstime.Text = _demandeActuelle.ChiffrageEstimeHeures?.ToString() ?? "";
        }

        private void BtnEnregistrer_Click(object sender, RoutedEventArgs e)
        {
            TxtErreur.Visibility = Visibility.Collapsed;

            // Validation
            if (string.IsNullOrWhiteSpace(TxtTitreDemande.Text))
            {
                TxtErreur.Text = "Le titre est obligatoire.";
                TxtErreur.Visibility = Visibility.Visible;
                return;
            }

            if (string.IsNullOrWhiteSpace(TxtDescription.Text))
            {
                TxtErreur.Text = "La description est obligatoire.";
                TxtErreur.Visibility = Visibility.Visible;
                return;
            }

            if (CmbType.SelectedValue == null)
            {
                TxtErreur.Text = "Le type est obligatoire.";
                TxtErreur.Visibility = Visibility.Visible;
                return;
            }

            try
            {
                // Mise à jour des champs
                _demandeActuelle.Titre = TxtTitreDemande.Text.Trim();
                _demandeActuelle.Description = TxtDescription.Text.Trim();
                _demandeActuelle.Type = (TypeDemande)CmbType.SelectedValue;
                _demandeActuelle.Criticite = (Criticite)CmbCriticite.SelectedValue;

                // Assignations
                var baId = (int)CmbBusinessAnalyst.SelectedValue;
                _demandeActuelle.BusinessAnalystId = baId != 0 ? (int?)baId : null;

                var cpId = (int)CmbChefProjet.SelectedValue;
                _demandeActuelle.ChefProjetId = cpId != 0 ? (int?)cpId : null;

                // Chiffrage (si visible)
                if (PanelChiffrage.Visibility == Visibility.Visible)
                {
                    var devId = (int)CmbDevChiffreur.SelectedValue;
                    _demandeActuelle.DevChiffreurId = devId != 0 ? (int?)devId : null;

                    if (!string.IsNullOrWhiteSpace(TxtChiffrageEstime.Text) && 
                        decimal.TryParse(TxtChiffrageEstime.Text, out decimal heures))
                    {
                        _demandeActuelle.ChiffrageEstimeHeures = (double)heures;
                    }
                }

                // Enregistrement
                _database.AddOrUpdateDemande(_demandeActuelle);

                // Historique
                var utilisateur = _authService.CurrentUser;
                var historique = new HistoriqueModification
                {
                    TypeEntite = "Demande",
                    EntiteId = _demandeActuelle.Id,
                    UtilisateurId = utilisateur != null ? utilisateur.Id : 0,
                    DateModification = DateTime.Now,
                    TypeModification = _demandeId.HasValue ? Domain.TypeModification.Modification : Domain.TypeModification.Creation,
                    NouvelleValeur = _demandeActuelle.Titre,
                    ChampModifie = _demandeId.HasValue ? "Modification complète" : "Création"
                };
                _database.AddHistorique(historique);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                TxtErreur.Text = string.Format("Erreur : {0}", ex.Message);
                TxtErreur.Visibility = Visibility.Visible;
            }
        }
    }
}

using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BacklogManager.Domain;
using BacklogManager.Services;

namespace BacklogManager.Views
{
    public partial class TacheSpecialeWindow : Window
    {
        private readonly BacklogService _backlogService;
        private readonly PermissionService _permissionService;
        private readonly int _currentUserId;

        public TacheSpecialeWindow(BacklogService backlogService, PermissionService permissionService)
        {
            InitializeComponent();
            _backlogService = backlogService;
            _permissionService = permissionService;
            
            // Récupérer l'ID de l'utilisateur via AuthenticationService
            var authService = new AuthenticationService(backlogService.Database);
            _currentUserId = authService.CurrentUser?.Id ?? 0;

            LoadData();
        }

        private void LoadData()
        {
            // Charger les devs pour le support
            var devs = _backlogService.GetAllUtilisateurs().Where(u => u.Actif).ToList();
            DevSupporteComboBox.ItemsSource = devs;
            DevSupporteComboBox.SelectionChanged += (s, e) => LoadTachesForSupport();

            // Sélectionner le premier type par défaut
            TypeComboBox.SelectedIndex = 0;
        }

        private void TypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TypeComboBox.SelectedItem is ComboBoxItem item)
            {
                string tag = item.Tag?.ToString();
                
                // Afficher le panel de support seulement si type = Support
                SupportInfoPanel.Visibility = tag == "Support" ? Visibility.Visible : Visibility.Collapsed;

                // Pré-remplir le titre selon le type
                if (string.IsNullOrWhiteSpace(TitreTextBox.Text))
                {
                    switch (tag)
                    {
                        case "Conges":
                            TitreTextBox.Text = "Congés";
                            break;
                        case "NonTravaille":
                            TitreTextBox.Text = "Non travaillé";
                            break;
                        case "Support":
                            TitreTextBox.Text = "Support développeur";
                            break;
                        case "Run":
                            TitreTextBox.Text = "Run - Support production";
                            break;
                    }
                }
            }

            // Charger les tâches du dev sélectionné pour le support
            LoadTachesForSupport();
        }

        private void LoadTachesForSupport()
        {
            if (DevSupporteComboBox.SelectedValue != null)
            {
                int devId = (int)DevSupporteComboBox.SelectedValue;
                var taches = _backlogService.GetAllBacklogItems()
                    .Where(t => t.DevAssigneId == devId && !t.EstArchive)
                    .ToList();
                TacheSupporteeComboBox.ItemsSource = taches;
            }
        }

        private void Creer_Click(object sender, RoutedEventArgs e)
        {
            // Validation
            if (TypeComboBox.SelectedItem == null)
            {
                MessageBox.Show("Veuillez sélectionner un type de tâche.", "Validation", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(TitreTextBox.Text))
            {
                MessageBox.Show("Le titre est obligatoire.", "Validation", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedItem = TypeComboBox.SelectedItem as ComboBoxItem;
            string tag = selectedItem?.Tag?.ToString();

            // Validation spécifique pour Support
            if (tag == "Support")
            {
                if (DevSupporteComboBox.SelectedValue == null)
                {
                    MessageBox.Show("Veuillez sélectionner le développeur aidé.", "Validation",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (TacheSupporteeComboBox.SelectedValue == null)
                {
                    MessageBox.Show("Veuillez sélectionner la tâche supportée.", "Validation",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            // Récupérer le projet "Tâches administratives"
            var projetAdmin = _backlogService.GetAllProjets()
                .FirstOrDefault(p => p.Nom == "Tâches administratives");

            // Parse chiffrage (en jours → heures)
            double chiffrageJours = 1.0;
            if (double.TryParse(ChiffrageTextBox.Text, out double parsed))
            {
                chiffrageJours = parsed;
            }

            // Créer la tâche
            var tache = new BacklogItem
            {
                Titre = TitreTextBox.Text,
                Description = DescriptionTextBox.Text,
                TypeDemande = ParseTypeDemande(tag),
                Statut = Statut.Termine, // Les tâches spéciales sont toujours terminées
                Priorite = Priorite.Basse,
                DevAssigneId = _currentUserId, // Assigner au dev connecté
                ProjetId = projetAdmin?.Id,
                DateDebut = DateDebutPicker.SelectedDate ?? DateTime.Today,
                DateFinAttendue = DateFinPicker.SelectedDate ?? DateTime.Today,
                ChiffrageHeures = chiffrageJours * 8.0, // Convertir jours en heures
                EstArchive = false,
                DateCreation = DateTime.Now,
                DateDerniereMaj = DateTime.Now
            };

            // Pour les tâches de support, ajouter les infos
            if (tag == "Support")
            {
                tache.DevSupporte = (int?)DevSupporteComboBox.SelectedValue;
                tache.TacheSupportee = (int?)TacheSupporteeComboBox.SelectedValue;
            }

            _backlogService.SaveBacklogItem(tache);

            DialogResult = true;
            Close();
        }

        private TypeDemande ParseTypeDemande(string tag)
        {
            switch (tag)
            {
                case "Conges": return TypeDemande.Conges;
                case "NonTravaille": return TypeDemande.NonTravaille;
                case "Support": return TypeDemande.Support;
                case "Run": return TypeDemande.Run;
                default: return TypeDemande.Dev;
            }
        }

        private void Annuler_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}

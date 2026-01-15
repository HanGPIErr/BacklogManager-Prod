using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BacklogManager.Domain;
using BacklogManager.Services;

namespace BacklogManager.Views
{
    public partial class EquipeEditionWindow : Window
    {
        private readonly IDatabase _database;
        private readonly EquipeService _equipeService;
        private int? _equipeId;
        private Equipe _equipeActuelle;

        public EquipeEditionWindow(IDatabase database, int? equipeId = null)
        {
            InitializeComponent();
            _database = database;
            _equipeService = new EquipeService(_database);
            _equipeId = equipeId;

            // Initialiser les textes traduits
            InitialiserTextes();

            if (_equipeId.HasValue)
            {
                Title = "Modifier l'équipe";
                ChargerEquipe();
            }
            else
            {
                Title = "Nouvelle équipe";
            }

            ChargerManagers();
        }

        private void InitialiserTextes()
        {
            // Textes de l'interface
            TxtTitle.Text = "🏢 " + LocalizationService.Instance.GetString("Modal_Team_Title");
            TxtSubtitle.Text = LocalizationService.Instance.GetString("Modal_Team_Subtitle");
            LblName.Text = LocalizationService.Instance.GetString("Modal_Team_Name");
            LblCode.Text = LocalizationService.Instance.GetString("Modal_Team_Code");
            LblDescription.Text = LocalizationService.Instance.GetString("Modal_Team_Description");
            LblFunctionalScope.Text = LocalizationService.Instance.GetString("Modal_Team_FunctionalScope");
            LblManager.Text = "👤 " + LocalizationService.Instance.GetString("Modal_Team_Manager");
            LblManagerNote.Text = LocalizationService.Instance.GetString("Modal_Team_ManagerNote");
            LblContact.Text = LocalizationService.Instance.GetString("Modal_Team_Contact");
            BtnAnnuler.Content = LocalizationService.Instance.GetString("Common_Cancel");
            BtnEnregistrer.Content = LocalizationService.Instance.GetString("Common_Save");

            // S'abonner aux changements de langue
            LocalizationService.Instance.PropertyChanged += (s, e) =>
            {
                TxtTitle.Text = "🏢 " + LocalizationService.Instance.GetString("Modal_Team_Title");
                TxtSubtitle.Text = LocalizationService.Instance.GetString("Modal_Team_Subtitle");
                LblName.Text = LocalizationService.Instance.GetString("Modal_Team_Name");
                LblCode.Text = LocalizationService.Instance.GetString("Modal_Team_Code");
                LblDescription.Text = LocalizationService.Instance.GetString("Modal_Team_Description");
                LblFunctionalScope.Text = LocalizationService.Instance.GetString("Modal_Team_FunctionalScope");
                LblManager.Text = "👤 " + LocalizationService.Instance.GetString("Modal_Team_Manager");
                LblManagerNote.Text = LocalizationService.Instance.GetString("Modal_Team_ManagerNote");
                LblContact.Text = LocalizationService.Instance.GetString("Modal_Team_Contact");
                BtnAnnuler.Content = LocalizationService.Instance.GetString("Common_Cancel");
                BtnEnregistrer.Content = LocalizationService.Instance.GetString("Common_Save");
            };
        }

        private void ChargerEquipe()
        {
            try
            {
                _equipeActuelle = _equipeService.GetEquipeById(_equipeId.Value);
                if (_equipeActuelle != null)
                {
                    TxtNom.Text = _equipeActuelle.Nom;
                    TxtCode.Text = _equipeActuelle.Code;
                    TxtDescription.Text = _equipeActuelle.Description;
                    TxtPerimetreFonctionnel.Text = _equipeActuelle.PerimetreFonctionnel;
                    TxtContact.Text = _equipeActuelle.Contact;
                    
                    if (_equipeActuelle.ManagerId.HasValue)
                    {
                        CboManager.SelectedValue = _equipeActuelle.ManagerId.Value;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement de l'équipe: {ex.Message}", 
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ChargerManagers()
        {
            try
            {
                var utilisateurs = _database.GetUtilisateurs()
                    .Where(u => u.Actif)
                    .Select(u => new
                    {
                        Id = u.Id,
                        Display = $"{u.Prenom} {u.Nom}"
                    })
                    .OrderBy(u => u.Display)
                    .ToList();

                utilisateurs.Insert(0, new { Id = 0, Display = "-- Aucun manager --" });
                
                CboManager.ItemsSource = utilisateurs;
                CboManager.DisplayMemberPath = "Display";
                CboManager.SelectedValuePath = "Id";
                CboManager.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des managers: {ex.Message}", 
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnEnregistrer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validation
                if (string.IsNullOrWhiteSpace(TxtNom.Text))
                {
                    MessageBox.Show("Le nom de l'équipe est obligatoire.", 
                        "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtNom.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(TxtCode.Text))
                {
                    MessageBox.Show("Le code de l'équipe est obligatoire.", 
                        "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtCode.Focus();
                    return;
                }

                var equipe = _equipeActuelle ?? new Equipe();
                equipe.Nom = TxtNom.Text.Trim();
                equipe.Code = TxtCode.Text.Trim().ToUpper();
                equipe.Description = TxtDescription.Text?.Trim();
                equipe.PerimetreFonctionnel = TxtPerimetreFonctionnel.Text?.Trim();
                equipe.Contact = TxtContact.Text?.Trim();
                
                // Gestion du manager
                if (CboManager.SelectedValue != null)
                {
                    var managerId = Convert.ToInt32(CboManager.SelectedValue);
                    equipe.ManagerId = managerId > 0 ? (int?)managerId : null;
                    
                    // Debug log
                    System.Diagnostics.Debug.WriteLine($"Manager sélectionné - ID: {managerId}, ManagerId final: {equipe.ManagerId}");
                }
                else
                {
                    equipe.ManagerId = null;
                    System.Diagnostics.Debug.WriteLine("Aucun manager sélectionné");
                }

                if (_equipeId.HasValue)
                {
                    _equipeService.ModifierEquipe(equipe);
                    MessageBox.Show("Équipe modifiée avec succès!", 
                        "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    _equipeService.AjouterEquipe(equipe);
                    MessageBox.Show("Équipe créée avec succès!", 
                        "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'enregistrement: {ex.Message}", 
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnAnnuler_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}

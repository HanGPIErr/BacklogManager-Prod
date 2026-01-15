using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BacklogManager.Domain;
using BacklogManager.Services;

namespace BacklogManager.Views
{
    public partial class ProgrammeEditionWindow : Window
    {
        private readonly IDatabase _database;
        private readonly ProgrammeService _programmeService;
        private int? _programmeId;
        private Programme _programmeActuel;

        public ProgrammeEditionWindow(IDatabase database, int? programmeId = null)
        {
            InitializeComponent();
            _database = database;
            _programmeService = new ProgrammeService(_database);
            _programmeId = programmeId;

            // Initialiser les textes traduits
            InitialiserTextes();

            if (_programmeId.HasValue)
            {
                Title = "Modifier le programme";
                ChargerProgramme();
            }
            else
            {
                Title = "Nouveau programme";
            }

            ChargerResponsables();
        }

        private void InitialiserTextes()
        {
            // Textes de l'interface
            TxtTitle.Text = "📊 " + LocalizationService.Instance.GetString("Modal_Program_Title");
            TxtSubtitle.Text = LocalizationService.Instance.GetString("Modal_Program_Subtitle");
            LblName.Text = LocalizationService.Instance.GetString("Modal_Program_Name");
            LblCode.Text = LocalizationService.Instance.GetString("Modal_Program_Code");
            LblDescription.Text = LocalizationService.Instance.GetString("Modal_Program_Description");
            LblObjectifs.Text = LocalizationService.Instance.GetString("Modal_Program_Objectives");
            LblResponsable.Text = "👤 " + LocalizationService.Instance.GetString("Modal_Program_Responsible");
            LblStartDate.Text = LocalizationService.Instance.GetString("Modal_Program_StartDate");
            LblEndDate.Text = LocalizationService.Instance.GetString("Modal_Program_EndDate");
            LblGlobalStatus.Text = LocalizationService.Instance.GetString("Modal_Program_GlobalStatus");
            BtnAnnuler.Content = LocalizationService.Instance.GetString("Common_Cancel");
            BtnEnregistrer.Content = LocalizationService.Instance.GetString("Common_Save");

            // S'abonner aux changements de langue
            LocalizationService.Instance.PropertyChanged += (s, e) =>
            {
                TxtTitle.Text = "📊 " + LocalizationService.Instance.GetString("Modal_Program_Title");
                TxtSubtitle.Text = LocalizationService.Instance.GetString("Modal_Program_Subtitle");
                LblName.Text = LocalizationService.Instance.GetString("Modal_Program_Name");
                LblCode.Text = LocalizationService.Instance.GetString("Modal_Program_Code");
                LblDescription.Text = LocalizationService.Instance.GetString("Modal_Program_Description");
                LblObjectifs.Text = LocalizationService.Instance.GetString("Modal_Program_Objectives");
                LblResponsable.Text = "👤 " + LocalizationService.Instance.GetString("Modal_Program_Responsible");
                LblStartDate.Text = LocalizationService.Instance.GetString("Modal_Program_StartDate");
                LblEndDate.Text = LocalizationService.Instance.GetString("Modal_Program_EndDate");
                LblGlobalStatus.Text = LocalizationService.Instance.GetString("Modal_Program_GlobalStatus");
                BtnAnnuler.Content = LocalizationService.Instance.GetString("Common_Cancel");
                BtnEnregistrer.Content = LocalizationService.Instance.GetString("Common_Save");
            };
        }

        private void ChargerProgramme()
        {
            try
            {
                _programmeActuel = _programmeService.GetProgrammeById(_programmeId.Value);
                if (_programmeActuel != null)
                {
                    TxtNom.Text = _programmeActuel.Nom;
                    TxtCode.Text = _programmeActuel.Code;
                    TxtDescription.Text = _programmeActuel.Description;
                    TxtObjectifs.Text = _programmeActuel.Objectifs;
                    
                    if (_programmeActuel.ResponsableId.HasValue)
                    {
                        CboResponsable.SelectedValue = _programmeActuel.ResponsableId.Value;
                    }
                    
                    if (_programmeActuel.DateDebut.HasValue)
                    {
                        DtpDateDebut.SelectedDate = _programmeActuel.DateDebut.Value;
                    }
                    
                    if (_programmeActuel.DateFinCible.HasValue)
                    {
                        DtpDateFinCible.SelectedDate = _programmeActuel.DateFinCible.Value;
                    }
                    
                    // Sélectionner le statut
                    if (!string.IsNullOrWhiteSpace(_programmeActuel.StatutGlobal))
                    {
                        foreach (ComboBoxItem item in CboStatutGlobal.Items)
                        {
                            if (item.Content.ToString() == _programmeActuel.StatutGlobal)
                            {
                                CboStatutGlobal.SelectedItem = item;
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement du programme: {ex.Message}", 
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ChargerResponsables()
        {
            try
            {
                // Ne charger que les Admin et Manager pour les programmes
                var roles = _database.GetRoles();
                var adminManagerRoleIds = roles
                    .Where(r => r.Nom == "Admin" || r.Nom == "Manager")
                    .Select(r => r.Id)
                    .ToList();

                var utilisateurs = _database.GetUtilisateurs()
                    .Where(u => u.Actif && adminManagerRoleIds.Contains(u.RoleId))
                    .Select(u => new
                    {
                        Id = u.Id,
                        Display = $"{u.Prenom} {u.Nom}"
                    })
                    .OrderBy(u => u.Display)
                    .ToList();

                utilisateurs.Insert(0, new { Id = 0, Display = "-- Aucun responsable --" });
                
                CboResponsable.ItemsSource = utilisateurs;
                CboResponsable.DisplayMemberPath = "Display";
                CboResponsable.SelectedValuePath = "Id";
                CboResponsable.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des responsables: {ex.Message}", 
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
                    MessageBox.Show("Le nom du programme est obligatoire.", 
                        "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtNom.Focus();
                    return;
                }

                // Créer toujours un nouvel objet pour éviter les problèmes d'état
                var programme = new Programme();
                
                // Conserver l'ID en mode modification
                if (_programmeId.HasValue && _programmeActuel != null)
                {
                    programme.Id = _programmeActuel.Id;
                    programme.DateCreation = _programmeActuel.DateCreation;
                    programme.Actif = _programmeActuel.Actif;
                }
                else
                {
                    // Nouveau programme
                    programme.Actif = true;
                    programme.DateCreation = DateTime.Now;
                }
                
                programme.Nom = TxtNom.Text.Trim();
                programme.Code = TxtCode.Text?.Trim().ToUpper();
                programme.Description = TxtDescription.Text?.Trim();
                programme.Objectifs = TxtObjectifs.Text?.Trim();
                
                // Responsable
                int? responsableId = null;
                if (CboResponsable.SelectedValue != null)
                {
                    try
                    {
                        var selectedValue = CboResponsable.SelectedValue;
                        if (selectedValue is int intValue)
                        {
                            responsableId = intValue > 0 ? (int?)intValue : null;
                        }
                        else
                        {
                            var parsedValue = Convert.ToInt32(selectedValue);
                            responsableId = parsedValue > 0 ? (int?)parsedValue : null;
                        }
                    }
                    catch (Exception respEx)
                    {
                        MessageBox.Show($"Erreur avec le responsable: {respEx.Message}", 
                            "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
                programme.ResponsableId = responsableId;

                // Dates - Assigner directement les SelectedDate (nullable)
                DateTime? dateDebut = null;
                DateTime? dateFinCible = null;
                try
                {
                    // Lire les dates dans des variables locales d'abord
                    if (DtpDateDebut.SelectedDate.HasValue)
                    {
                        dateDebut = DtpDateDebut.SelectedDate.Value;
                    }
                    if (DtpDateFinCible.SelectedDate.HasValue)
                    {
                        dateFinCible = DtpDateFinCible.SelectedDate.Value;
                    }
                    
                    programme.DateDebut = dateDebut;
                    programme.DateFinCible = dateFinCible;
                }
                catch (Exception dateEx)
                {
                    MessageBox.Show($"Erreur avec les dates: {dateEx.Message}\n\nStackTrace:\n{dateEx.StackTrace}", 
                        "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                
                // Statut
                if (CboStatutGlobal.SelectedItem is ComboBoxItem selectedStatut)
                {
                    programme.StatutGlobal = selectedStatut.Content.ToString();
                }

                if (_programmeId.HasValue)
                {
                    _programmeService.ModifierProgramme(programme);
                    MessageBox.Show("Programme modifié avec succès!", 
                        "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    _programmeService.AjouterProgramme(programme);
                    MessageBox.Show("Programme créé avec succès!", 
                        "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'enregistrement: {ex.Message}\n\nStackTrace:\n{ex.StackTrace}\n\nInner Exception:\n{ex.InnerException?.Message}", 
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

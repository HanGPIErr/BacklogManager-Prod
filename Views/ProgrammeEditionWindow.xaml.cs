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
                var utilisateurs = _database.GetUtilisateurs()
                    .Where(u => u.Actif)
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

                var programme = _programmeActuel ?? new Programme();
                programme.Nom = TxtNom.Text.Trim();
                programme.Code = TxtCode.Text?.Trim().ToUpper();
                programme.Description = TxtDescription.Text?.Trim();
                programme.Objectifs = TxtObjectifs.Text?.Trim();
                
                // Responsable
                if (CboResponsable.SelectedValue != null)
                {
                    var responsableId = Convert.ToInt32(CboResponsable.SelectedValue);
                    programme.ResponsableId = responsableId > 0 ? (int?)responsableId : null;
                }
                else
                {
                    programme.ResponsableId = null;
                }

                // Dates
                programme.DateDebut = DtpDateDebut.SelectedDate;
                programme.DateFinCible = DtpDateFinCible.SelectedDate;
                
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

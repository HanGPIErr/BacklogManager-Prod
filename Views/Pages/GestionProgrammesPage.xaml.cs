using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BacklogManager.Domain;
using BacklogManager.Services;

namespace BacklogManager.Views.Pages
{
    public partial class GestionProgrammesPage : Page
    {
        private readonly IDatabase _database;
        private readonly ProgrammeService _programmeService;

        public GestionProgrammesPage(IDatabase database)
        {
            InitializeComponent();
            _database = database;
            _programmeService = new ProgrammeService(_database);
            
            // Initialiser les textes traduits
            InitialiserTextes();
            
            ChargerProgrammes();
        }

        private void InitialiserTextes()
        {
            // Textes des boutons
            BtnNouveauProgramme.Content = "➕ " + LocalizationService.Instance.GetString("Programs_NewProgram");
            BtnActualiser.Content = "🔄 " + LocalizationService.Instance.GetString("Programs_Refresh");

            // S'abonner aux changements de langue
            LocalizationService.Instance.PropertyChanged += (s, e) =>
            {
                BtnNouveauProgramme.Content = "➕ " + LocalizationService.Instance.GetString("Programs_NewProgram");
                BtnActualiser.Content = "🔄 " + LocalizationService.Instance.GetString("Programs_Refresh");
                
                // Recharger les programmes pour mettre à jour les textes traduits
                ChargerProgrammes();
            };
        }

        private void ChargerProgrammes()
        {
            try
            {
                var programmes = _programmeService.GetAllProgrammes();

                // Récupérer tous les utilisateurs pour les responsables
                var utilisateurs = _database.GetUtilisateurs();

                // Créer les ViewModels avec statistiques
                var programmesViewModel = programmes.Select(p => new ProgrammeViewModel
                {
                    Id = p.Id,
                    Nom = p.Nom,
                    Code = !string.IsNullOrWhiteSpace(p.Code) ? p.Code : LocalizationService.Instance.GetString("Programs_NotAvailable"),
                    Description = !string.IsNullOrWhiteSpace(p.Description) ? p.Description : LocalizationService.Instance.GetString("Programs_NoDescription"),
                    ResponsableNom = p.ResponsableId.HasValue 
                        ? utilisateurs.FirstOrDefault(u => u.Id == p.ResponsableId.Value)?.Prenom + " " + utilisateurs.FirstOrDefault(u => u.Id == p.ResponsableId.Value)?.Nom
                        : LocalizationService.Instance.GetString("Programs_NoResponsible"),
                    NombreProjets = _programmeService.GetNombreProjetsActifs(p.Id),
                    StatutGlobal = !string.IsNullOrWhiteSpace(p.StatutGlobal) ? p.StatutGlobal : LocalizationService.Instance.GetString("Programs_NotAvailable")
                }).ToList();

                LstProgrammes.ItemsSource = programmesViewModel;
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(LocalizationService.Instance.GetString("Programs_ErrorLoading"), ex.Message), 
                    LocalizationService.Instance.GetString("Common_Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnNouveauProgramme_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var window = new ProgrammeEditionWindow(_database);
                window.Owner = Window.GetWindow(this);
                if (window.ShowDialog() == true)
                {
                    ChargerProgrammes();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(LocalizationService.Instance.GetString("Programs_ErrorOpeningEditor"), ex.Message), 
                    LocalizationService.Instance.GetString("Common_Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnActualiser_Click(object sender, RoutedEventArgs e)
        {
            ChargerProgrammes();
        }

        private void Programme_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var border = sender as Border;
                var programme = border?.DataContext as ProgrammeViewModel;
                if (programme != null)
                {
                    var window = new ProgrammeEditionWindow(_database, programme.Id);
                    window.Owner = Window.GetWindow(this);
                    if (window.ShowDialog() == true)
                    {
                        ChargerProgrammes();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(LocalizationService.Instance.GetString("Programs_ErrorOpening"), ex.Message), 
                    LocalizationService.Instance.GetString("Common_Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ViewModel pour l'affichage des programmes
        private class ProgrammeViewModel
        {
            public int Id { get; set; }
            public string Nom { get; set; }
            public string Code { get; set; }
            public string Description { get; set; }
            public string ResponsableNom { get; set; }
            public int NombreProjets { get; set; }
            public string StatutGlobal { get; set; }
        }
    }
}

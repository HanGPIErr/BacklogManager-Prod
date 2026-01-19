using System;
using System.Windows;
using System.Windows.Media;
using BacklogManager.ViewModels;
using BacklogManager.Domain;
using BacklogManager.Services;

namespace BacklogManager.Views
{
    public partial class AperçuTacheTimelineWindow : Window
    {
        private readonly TacheTimelineViewModel _tache;

        public AperçuTacheTimelineWindow(TacheTimelineViewModel tache)
        {
            InitializeComponent();
            _tache = tache;
            InitializeLocalizedTexts();
            ChargerDetails(tache);
        }

        private void InitializeLocalizedTexts()
        {
            var loc = LocalizationService.Instance;
            
            // Window title
            Title = loc.GetString("TaskPreview_WindowTitle");
            
            // Header
            TxtDetailsTitle.Text = loc.GetString("TaskPreview_DetailsTitle");
            
            // Labels
            TxtDescriptionLabel.Text = loc.GetString("TaskPreview_Description");
            TxtAssignedDevLabel.Text = loc.GetString("TaskPreview_AssignedDev");
            TxtPriorityLabel.Text = loc.GetString("TaskPreview_Priority");
            TxtTypeLabel.Text = loc.GetString("TaskPreview_Type");
            TxtCreationDateLabel.Text = loc.GetString("TaskPreview_CreationDate");
            TxtDueDateLabel.Text = loc.GetString("TaskPreview_DueDate");
            TxtComplexityLabel.Text = loc.GetString("TaskPreview_Complexity");
            TxtEstimateLabel.Text = loc.GetString("TaskPreview_Estimate");
            TxtRealTimeLabel.Text = loc.GetString("TaskPreview_RealTime");
            TxtProgressLabel.Text = loc.GetString("TaskPreview_Progress");
            
            // Buttons
            BtnAnalyserIA.Content = loc.GetString("TaskPreview_AnalyzeIA");
            BtnFermer.Content = loc.GetString("TaskPreview_Close");
        }

        private void ChargerDetails(TacheTimelineViewModel tache)
        {
            var loc = LocalizationService.Instance;
            
            // Titre
            TxtTitre.Text = tache.Titre;

            // Statut avec couleur
            TxtStatut.Text = FormatStatut(tache.Statut);
            BadgeStatut.Background = GetStatutColor(tache.Statut);

            // Description
            TxtDescription.Text = !string.IsNullOrWhiteSpace(tache.BacklogItem?.Description)
                ? tache.BacklogItem.Description
                : loc.GetString("Common_NoDescription") ?? "Aucune description";

            // Développeur
            TxtDevAssigne.Text = !string.IsNullOrWhiteSpace(tache.DevAssigneNom)
                ? tache.DevAssigneNom
                : loc.GetString("Stats_NotAssigned") ?? "Non assigné";

            // Priorité
            if (tache.BacklogItem != null)
            {
                TxtPriorite.Text = tache.BacklogItem.Priorite.ToString();
                TxtType.Text = tache.BacklogItem.TypeDemande.ToString();
            }
            else
            {
                TxtPriorite.Text = "Non définie";
                TxtType.Text = "Non défini";
            }

            // Dates
            TxtDateCreation.Text = tache.DateCreation.ToString("dd/MM/yyyy");
            TxtDateFin.Text = tache.DateFinAttendue.HasValue
                ? tache.DateFinAttendue.Value.ToString("dd/MM/yyyy")
                : "Non définie";

            // Complexité
            TxtComplexite.Text = tache.Complexite.HasValue
                ? tache.Complexite.Value.ToString()
                : "Non évaluée";

            // Chiffrage et temps
            TxtChiffrage.Text = tache.ChiffrageJours.HasValue
                ? string.Format("{0:F1} jours", tache.ChiffrageJours.Value)
                : "Non chiffré";

            TxtTempsReel.Text = string.Format("{0:F1} jours ({1:F1}h)", 
                tache.TempsReelJours, tache.TempsReelHeures);

            TxtProgression.Text = string.Format("{0:F0}%", tache.ProgressionPourcentage);

            // Barre de progression
            BarreProgression.Width = Math.Min(tache.ProgressionPourcentage * 6.2, 620); // Max 620px

            // Couleur de la barre selon progression
            if (tache.EstEnRetard)
            {
                BarreProgression.Background = new SolidColorBrush(Color.FromRgb(211, 47, 47)); // Rouge
            }
            else if (tache.ProgressionPourcentage >= 100)
            {
                BarreProgression.Background = new SolidColorBrush(Color.FromRgb(76, 175, 80)); // Vert
            }
            else if (tache.ProgressionPourcentage >= 75)
            {
                BarreProgression.Background = new SolidColorBrush(Color.FromRgb(255, 152, 0)); // Orange
            }
            else
            {
                BarreProgression.Background = new SolidColorBrush(Color.FromRgb(33, 150, 243)); // Bleu
            }
        }

        private string FormatStatut(Statut statut)
        {
            var loc = LocalizationService.Instance;
            switch (statut)
            {
                case Statut.Afaire:
                    return loc.GetString("Stats_StatusToDo")?.ToUpper() ?? "À FAIRE";
                case Statut.EnCours:
                    return loc.GetString("Stats_StatusInProgress")?.ToUpper() ?? "EN COURS";
                case Statut.Test:
                    return loc.GetString("Stats_StatusInTest")?.ToUpper() ?? "EN TEST";
                case Statut.Termine:
                    return loc.GetString("Stats_StatusCompleted")?.ToUpper() ?? "TERMINÉ";
                default:
                    return statut.ToString().ToUpper();
            }
        }

        private Brush GetStatutColor(Statut statut)
        {
            switch (statut)
            {
                case Statut.Afaire:
                    return new SolidColorBrush(Color.FromRgb(158, 158, 158)); // Gris
                case Statut.EnCours:
                    return new SolidColorBrush(Color.FromRgb(33, 150, 243)); // Bleu
                case Statut.Test:
                    return new SolidColorBrush(Color.FromRgb(255, 152, 0)); // Orange
                case Statut.Termine:
                    return new SolidColorBrush(Color.FromRgb(76, 175, 80)); // Vert
                default:
                    return new SolidColorBrush(Color.FromRgb(158, 158, 158)); // Gris
            }
        }

        private void BtnFermer_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void BtnAnalyserIA_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_tache?.BacklogItem == null)
                {
                    MessageBox.Show("Impossible d'analyser cette tâche.", "Erreur", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Ouvrir la fenêtre d'analyse IA
                var analyseWindow = new AnalyseTacheIAWindow(
                    _tache.BacklogItem,
                    _tache.TempsReelHeures,
                    _tache.ProgressionPourcentage);
                analyseWindow.Owner = this;
                analyseWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ouverture de l'analyse IA :\n{ex.Message}", 
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

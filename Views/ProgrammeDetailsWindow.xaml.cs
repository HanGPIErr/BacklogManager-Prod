using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using BacklogManager.Domain;
using BacklogManager.Services;

namespace BacklogManager.Views
{
    public partial class ProgrammeDetailsWindow : Window
    {
        private readonly Programme _programme;
        private readonly BacklogService _backlogService;

        public ProgrammeDetailsWindow(Programme programme, BacklogService backlogService)
        {
            InitializeComponent();
            _programme = programme;
            _backlogService = backlogService;
            LoadData();
        }

        private void LoadData()
        {
            // Header
            TxtTitre.Text = $"🎯 {_programme.Nom}";
            TxtCode.Text = _programme.Code;

            // Informations générales
            TxtNom.Text = _programme.Nom;
            TxtCodeDetail.Text = _programme.Code;

            // Responsable
            if (_programme.ResponsableId.HasValue)
            {
                var responsable = _backlogService.GetAllDevs().FirstOrDefault(d => d.Id == _programme.ResponsableId.Value);
                TxtResponsable.Text = responsable != null ? $"👤 {responsable.Nom}" : "Non défini";
            }
            else
            {
                TxtResponsable.Text = "Non défini";
            }

            // Statut global
            if (!string.IsNullOrEmpty(_programme.StatutGlobal))
            {
                var statutColor = _programme.StatutGlobal == "On Track" ? "#4CAF50" :
                                  _programme.StatutGlobal == "At Risk" ? "#FF9800" : "#F44336";
                var statutIcon = _programme.StatutGlobal == "On Track" ? "✓" :
                                 _programme.StatutGlobal == "At Risk" ? "⚠" : "✗";

                BadgeStatutGlobal.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(statutColor));
                TxtStatutGlobal.Text = $"{statutIcon} {_programme.StatutGlobal}";
            }
            else
            {
                BadgeStatutGlobal.Background = new SolidColorBrush(Colors.Gray);
                TxtStatutGlobal.Text = "Non défini";
            }

            // Dates
            TxtDateDebut.Text = _programme.DateDebut?.ToString("dd/MM/yyyy") ?? "Non définie";
            TxtDateFinCible.Text = _programme.DateFinCible?.ToString("dd/MM/yyyy") ?? "Non définie";

            // Description
            if (!string.IsNullOrEmpty(_programme.Description))
            {
                TxtDescription.Text = _programme.Description;
                BorderDescription.Visibility = Visibility.Visible;
            }
            else
            {
                BorderDescription.Visibility = Visibility.Collapsed;
            }

            // Objectifs
            if (!string.IsNullOrEmpty(_programme.Objectifs))
            {
                TxtObjectifs.Text = _programme.Objectifs;
                BorderObjectifs.Visibility = Visibility.Visible;
            }
            else
            {
                BorderObjectifs.Visibility = Visibility.Collapsed;
            }

            // Statistiques
            var projets = _backlogService.GetAllProjets().Where(p => p.ProgrammeId == _programme.Id && p.Actif).ToList();
            TxtNbProjets.Text = projets.Count.ToString();

            // Calculer les statistiques (même logique que dans BacklogView)
            var toutesLesTaches = _backlogService.GetAllBacklogItemsIncludingArchived()
                .Where(t => projets.Any(p => p.Id == t.ProjetId) &&
                            t.TypeDemande != TypeDemande.Conges &&
                            t.TypeDemande != TypeDemande.NonTravaille)
                .ToList();

            var nbTachesTotal = toutesLesTaches.Count;
            var nbTachesTerminees = toutesLesTaches.Count(t => t.Statut == Statut.Termine || t.EstArchive);
            var pourcentageAvancement = nbTachesTotal > 0 ? (int)((double)nbTachesTerminees / nbTachesTotal * 100) : 0;

            TxtNbTaches.Text = nbTachesTotal.ToString();
            TxtProgression.Text = $"{pourcentageAvancement}%";

            // Barre de progression
            ProgressBar.Width = 850 * pourcentageAvancement / 100; // 850 = largeur max de la fenêtre moins marges
            TxtProgressionTexte.Text = $"{pourcentageAvancement}%";
            TxtProgressionTexte.Foreground = pourcentageAvancement > 50 ? Brushes.White : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666666"));

            // Indicateurs RAG
            var nbGreen = projets.Count(p => p.StatutRAG == "Green");
            var nbAmber = projets.Count(p => p.StatutRAG == "Amber");
            var nbRed = projets.Count(p => p.StatutRAG == "Red");

            TxtGreen.Text = $"🟢 {nbGreen} Green";
            TxtAmber.Text = $"🟠 {nbAmber} Amber";
            TxtRed.Text = $"🔴 {nbRed} Red";

            BadgeGreen.Visibility = nbGreen > 0 ? Visibility.Visible : Visibility.Collapsed;
            BadgeAmber.Visibility = nbAmber > 0 ? Visibility.Visible : Visibility.Collapsed;
            BadgeRed.Visibility = nbRed > 0 ? Visibility.Visible : Visibility.Collapsed;

            // Liste des projets
            ListeProjets.ItemsSource = projets.OrderBy(p => p.Nom);
        }

        private void BtnFermer_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

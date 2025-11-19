using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.IO;
using Microsoft.Win32;
using System.Diagnostics;
using BacklogManager.Domain;
using BacklogManager.Services;

namespace BacklogManager.Views
{
    public partial class DevDetailsWindow : Window
    {
        private readonly Dev _dev;
        private readonly BacklogService _backlogService;
        private readonly CRAService _craService;
        private List<TacheDevViewModel> _allTaches;
        
        private DateTime? _dateDebutFiltre;
        private DateTime? _dateFinFiltre;
        private bool _isInitialized = false;

        public DevDetailsWindow(Dev dev, BacklogService backlogService, CRAService craService)
        {
            InitializeComponent();
            _dev = dev;
            _backlogService = backlogService;
            _craService = craService;
            
            _isInitialized = true; // Marquer comme initialisé AVANT de charger les données
            LoadData();
        }

        private void CboPeriodeDev_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return; // Ignorer si pas encore initialisé
            
            if (CboPeriodeDev.SelectedIndex == 4) // Période personnalisée
            {
                PanelDatesDevCustom.Visibility = Visibility.Visible;
            }
            else
            {
                PanelDatesDevCustom.Visibility = Visibility.Collapsed;
                AppliquerFiltrePeriode(CboPeriodeDev.SelectedIndex);
            }
        }

        private void BtnAppliquerPeriodeDev_Click(object sender, RoutedEventArgs e)
        {
            AppliquerFiltrePeriode(CboPeriodeDev.SelectedIndex);
        }

        private void AppliquerFiltrePeriode(int periodeIndex)
        {
            DateTime now = DateTime.Now;
            
            switch (periodeIndex)
            {
                case 0: // Année en cours
                    _dateDebutFiltre = new DateTime(now.Year, 1, 1);
                    _dateFinFiltre = new DateTime(now.Year, 12, 31, 23, 59, 59);
                    break;
                case 1: // 6 derniers mois
                    _dateDebutFiltre = now.AddMonths(-6);
                    _dateFinFiltre = now;
                    break;
                case 2: // Mois en cours
                    _dateDebutFiltre = new DateTime(now.Year, now.Month, 1);
                    _dateFinFiltre = new DateTime(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month), 23, 59, 59);
                    break;
                case 3: // 3 derniers mois
                    _dateDebutFiltre = now.AddMonths(-3);
                    _dateFinFiltre = now;
                    break;
                case 4: // Période personnalisée
                    _dateDebutFiltre = DateDebutDev.SelectedDate;
                    _dateFinFiltre = DateFinDev.SelectedDate;
                    break;
                case 5: // Tout afficher
                default:
                    _dateDebutFiltre = null;
                    _dateFinFiltre = null;
                    break;
            }

            LoadData();
        }

        private void LoadData()
        {
            // Header
            TxtNomDev.Text = _dev.Nom;
            TxtRole.Text = "Développeur";
            
            // Initiales pour l'avatar
            var initiales = GetInitiales(_dev.Nom);
            TxtInitiales.Text = initiales;

            // Charger toutes les tâches du dev
            var taches = _backlogService.GetAllBacklogItems()
                .Where(t => t.DevAssigneId == _dev.Id && !t.EstArchive)
                .ToList();

            // Appliquer le filtre de période sur les tâches
            if (_dateDebutFiltre.HasValue && _dateFinFiltre.HasValue)
            {
                taches = taches.Where(t => t.DateCreation >= _dateDebutFiltre.Value && 
                                           t.DateCreation <= _dateFinFiltre.Value).ToList();
            }

            var projets = _backlogService.GetAllProjets();

            // Métriques
            int total = taches.Count;
            int afaire = taches.Count(t => t.Statut == Statut.Afaire);
            int enCours = taches.Count(t => t.Statut == Statut.EnCours);
            int enTest = taches.Count(t => t.Statut == Statut.Test);
            int termine = taches.Count(t => t.Statut == Statut.Termine);

            double chargeJours = taches.Sum(t => t.ChiffrageHeures ?? 0) / 7.0;
            
            // Récupérer les CRAs du dev pour calculer le temps réel
            var crasDev = _craService.GetCRAsByDev(_dev.Id);
            
            // Appliquer le filtre de période sur les CRA
            if (_dateDebutFiltre.HasValue && _dateFinFiltre.HasValue)
            {
                crasDev = crasDev.Where(c => c.Date >= _dateDebutFiltre.Value && 
                                             c.Date <= _dateFinFiltre.Value).ToList();
            }
            
            // Séparer par type : congés, non-travaillé, travail réel
            double heuresTravail = 0;
            double heuresConges = 0;
            double heuresNonTravaille = 0;
            
            foreach (var cra in crasDev)
            {
                var tache = _backlogService.GetBacklogItemById(cra.BacklogItemId);
                if (tache != null)
                {
                    if (tache.TypeDemande == TypeDemande.Conges)
                        heuresConges += cra.HeuresTravaillees;
                    else if (tache.TypeDemande == TypeDemande.NonTravaille)
                        heuresNonTravaille += cra.HeuresTravaillees;
                    else
                        heuresTravail += cra.HeuresTravaillees;
                }
            }
            
            double joursTravail = heuresTravail / 8.0;
            double joursConges = heuresConges / 8.0;
            double joursNonTravaille = heuresNonTravaille / 8.0;
            double totalJoursCRA = (heuresTravail + heuresConges + heuresNonTravaille) / 8.0;

            TxtTotalTaches.Text = total.ToString();
            TxtEnCours.Text = enCours.ToString();
            TxtTerminees.Text = termine.ToString();
            TxtCharge.Text = chargeJours.ToString("F1");
            TxtTempsReel.Text = joursTravail.ToString("F1");
            TxtConges.Text = joursConges.ToString("F1");
            TxtNonTravaille.Text = joursNonTravaille.ToString("F1");
            TxtTotalCRA.Text = totalJoursCRA.ToString("F1");

            // Calcul du taux de réalisation (seulement travail réel, pas congés)
            var tachesAvecEstimation = taches.Where(t => t.ChiffrageHeures.HasValue && t.ChiffrageHeures.Value > 0).ToList();
            var totalEstimeHeures = tachesAvecEstimation.Sum(t => t.ChiffrageHeures.Value);

            if (tachesAvecEstimation.Count == 0)
            {
                TxtTauxRealisation.Text = "⚠ Chiffrer";
            }
            else if (heuresTravail == 0)
            {
                TxtTauxRealisation.Text = "⚠ Saisir CRA";
            }
            else if (totalEstimeHeures == 0)
            {
                TxtTauxRealisation.Text = "N/A";
            }
            else
            {
                double taux = (heuresTravail / totalEstimeHeures) * 100.0;
                TxtTauxRealisation.Text = $"{taux:F0}%";
            }

            // Répartition par statut
            TxtCountAfaire.Text = afaire.ToString();
            TxtCountEnCours.Text = enCours.ToString();
            TxtCountEnTest.Text = enTest.ToString();
            TxtCountTermine.Text = termine.ToString();

            BarAfaire.Maximum = total;
            BarAfaire.Value = afaire;
            BarEnCours.Maximum = total;
            BarEnCours.Value = enCours;
            BarEnTest.Maximum = total;
            BarEnTest.Value = enTest;
            BarTermine.Maximum = total;
            BarTermine.Value = termine;

            // Répartition par projet
            var parProjet = taches.GroupBy(t => t.ProjetId)
                .Select(g => new ProjetCountViewModel
                {
                    ProjetNom = projets.FirstOrDefault(p => p.Id == g.Key)?.Nom ?? "Sans projet",
                    Count = g.Count()
                })
                .OrderByDescending(p => p.Count)
                .ToList();

            ListeProjets.ItemsSource = parProjet;

            // Liste des tâches
            _allTaches = taches.Select(t => {
                // Calculer le temps réel depuis les CRAs pour cette tâche
                var crasTache = crasDev.Where(c => c.BacklogItemId == t.Id).Sum(c => c.HeuresTravaillees);
                return new TacheDevViewModel
                {
                    Titre = t.Titre,
                    Statut = GetStatutDisplay(t.Statut),
                    StatutColor = GetStatutColor(t.Statut),
                    ProjetNom = projets.FirstOrDefault(p => p.Id == t.ProjetId)?.Nom ?? "Sans projet",
                    ChiffrageJours = t.ChiffrageHeures.HasValue ? t.ChiffrageHeures.Value / 7.0 : 0,
                    TempsReelJours = crasTache / 8.0,
                    ProgressionPct = CalculerProgression(t),
                    StatutOriginal = t.Statut
                };
            }).ToList();

            ListeTaches.ItemsSource = _allTaches;
        }

        private void CmbFiltreStatut_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_allTaches == null) return;

            var statutItem = CmbFiltreStatut.SelectedItem as ComboBoxItem;
            string statutFiltre = statutItem?.Content?.ToString() ?? "Tous";

            if (statutFiltre == "Tous")
            {
                ListeTaches.ItemsSource = _allTaches;
            }
            else
            {
                var filtered = _allTaches.Where(t => t.Statut == statutFiltre).ToList();
                ListeTaches.ItemsSource = filtered;
            }
        }

        private string GetInitiales(string nom)
        {
            if (string.IsNullOrEmpty(nom)) return "??";
            var parts = nom.Split(' ');
            if (parts.Length >= 2)
                return $"{parts[0][0]}{parts[1][0]}".ToUpper();
            return nom.Substring(0, Math.Min(2, nom.Length)).ToUpper();
        }

        private void ExporterPDF_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Filter = "Fichier HTML (*.html)|*.html",
                    FileName = $"Stats_{_dev.Nom.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd}.html"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    var html = GenererRapportDevHTML();
                    File.WriteAllText(saveDialog.FileName, html);

                    var result = MessageBox.Show(
                        $"Rapport exporté avec succès :\n{saveDialog.FileName}\n\nVoulez-vous l'ouvrir maintenant ?",
                        "Export réussi",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes)
                    {
                        Process.Start(saveDialog.FileName);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'export : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GenererRapportDevHTML()
        {
            var taches = _backlogService.GetAllBacklogItems()
                .Where(t => t.DevAssigneId == _dev.Id && !t.EstArchive)
                .ToList();

            // Appliquer le filtre de période
            if (_dateDebutFiltre.HasValue && _dateFinFiltre.HasValue)
            {
                taches = taches.Where(t => t.DateCreation >= _dateDebutFiltre.Value && 
                                           t.DateCreation <= _dateFinFiltre.Value).ToList();
            }

            var projets = _backlogService.GetAllProjets();
            var crasDev = _craService.GetCRAsByDev(_dev.Id);
            
            // Appliquer le filtre de période sur les CRA
            if (_dateDebutFiltre.HasValue && _dateFinFiltre.HasValue)
            {
                crasDev = crasDev.Where(c => c.Date >= _dateDebutFiltre.Value && 
                                             c.Date <= _dateFinFiltre.Value).ToList();
            }

            int total = taches.Count;
            int termine = taches.Count(t => t.Statut == Statut.Termine);
            int enCours = taches.Count(t => t.Statut == Statut.EnCours);
            double chargeJours = taches.Sum(t => t.ChiffrageHeures ?? 0) / 7.0;
            double tempsReel = taches.Sum(t => t.TempsReelHeures ?? 0);
            
            var tachesAvecEstimation = taches.Where(t => t.ChiffrageHeures.HasValue && t.ChiffrageHeures.Value > 0).ToList();
            var totalHeuresCRA = crasDev.Sum(c => c.HeuresTravaillees);
            var totalEstimeHeures = tachesAvecEstimation.Sum(t => t.ChiffrageHeures.Value);
            string tauxRealisation = "N/A";
            if (tachesAvecEstimation.Count > 0 && totalHeuresCRA > 0 && totalEstimeHeures > 0)
            {
                double taux = (totalHeuresCRA / totalEstimeHeures) * 100.0;
                tauxRealisation = $"{taux:F0}%";
            }

            string periodeTexte = "";
            if (_dateDebutFiltre.HasValue && _dateFinFiltre.HasValue)
            {
                periodeTexte = $"<p style='margin: 5px 0 0 0; opacity: 0.8;'>Période : du {_dateDebutFiltre.Value:dd/MM/yyyy} au {_dateFinFiltre.Value:dd/MM/yyyy}</p>";
            }

            var html = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Rapport - {_dev.Nom}</title>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, sans-serif; margin: 40px; background: #f5f5f5; }}
        .header {{ background: #00915A; color: white; padding: 30px; border-radius: 8px; margin-bottom: 30px; }}
        .header h1 {{ margin: 0; font-size: 28px; }}
        .header p {{ margin: 5px 0 0 0; opacity: 0.9; }}
        .metrics {{ display: grid; grid-template-columns: repeat(3, 1fr); gap: 20px; margin-bottom: 30px; }}
        .metric {{ background: white; padding: 20px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
        .metric-label {{ color: #666; font-size: 12px; text-transform: uppercase; margin-bottom: 8px; }}
        .metric-value {{ font-size: 32px; font-weight: bold; color: #00915A; }}
        .metric-unit {{ font-size: 14px; color: #999; }}
        table {{ width: 100%; border-collapse: collapse; background: white; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
        th {{ background: #00915A; color: white; padding: 12px; text-align: left; font-weight: 600; }}
        td {{ padding: 10px; border-bottom: 1px solid #eee; }}
        tr:last-child td {{ border-bottom: none; }}
        .statut {{ display: inline-block; padding: 4px 12px; border-radius: 12px; font-size: 11px; font-weight: 600; }}
        .statut-termine {{ background: #C8E6C9; color: #2E7D32; }}
        .statut-encours {{ background: #BBDEFB; color: #1565C0; }}
        .statut-test {{ background: #FFE082; color: #F57C00; }}
        .statut-afaire {{ background: #E0E0E0; color: #424242; }}
        .footer {{ margin-top: 40px; text-align: center; color: #999; font-size: 12px; }}
        @media print {{ body {{ margin: 20px; }} }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>📊 Rapport de Performance - {_dev.Nom}</h1>
        <p>Généré le {DateTime.Now:dd/MM/yyyy à HH:mm}</p>
        {periodeTexte}
    </div>

    <div class='metrics'>
        <div class='metric'>
            <div class='metric-label'>Total Tâches</div>
            <div class='metric-value'>{total}</div>
        </div>
        <div class='metric'>
            <div class='metric-label'>En Cours</div>
            <div class='metric-value' style='color: #1E88E5;'>{enCours}</div>
        </div>
        <div class='metric'>
            <div class='metric-label'>Terminées</div>
            <div class='metric-value' style='color: #43A047;'>{termine}</div>
        </div>
        <div class='metric'>
            <div class='metric-label'>Charge (jours)</div>
            <div class='metric-value' style='color: #FB8C00;'>{chargeJours:F1}</div>
        </div>
        <div class='metric'>
            <div class='metric-label'>Temps Réel (h)</div>
            <div class='metric-value' style='color: #E91E63;'>{tempsReel:F1}</div>
        </div>
        <div class='metric'>
            <div class='metric-label'>Taux Réalisation</div>
            <div class='metric-value' style='color: #FBC02D;'>{tauxRealisation}</div>
        </div>
    </div>

    <h2 style='color: #00915A; margin-top: 40px;'>📋 Liste des Tâches</h2>
    <table>
        <thead>
            <tr>
                <th>Titre</th>
                <th>Projet</th>
                <th>Statut</th>
                <th>Chiffrage (j)</th>
                <th>Temps (h)</th>
            </tr>
        </thead>
        <tbody>";

            foreach (var tache in taches.OrderByDescending(t => t.Statut).ThenBy(t => t.Titre))
            {
                var projet = projets.FirstOrDefault(p => p.Id == tache.ProjetId);
                var projetNom = projet?.Nom ?? "Sans projet";
                var chiffrageJours = tache.ChiffrageHeures.HasValue ? (tache.ChiffrageHeures.Value / 8.0).ToString("F1") : "-";
                var tempsReelH = tache.TempsReelHeures.HasValue ? tache.TempsReelHeures.Value.ToString("F1") : "0";
                
                string statutClass = "statut-afaire";
                string statutText = "À faire";
                switch (tache.Statut)
                {
                    case Statut.EnCours:
                        statutClass = "statut-encours";
                        statutText = "En cours";
                        break;
                    case Statut.Test:
                        statutClass = "statut-test";
                        statutText = "En test";
                        break;
                    case Statut.Termine:
                        statutClass = "statut-termine";
                        statutText = "Terminé";
                        break;
                }

                html += $@"
            <tr>
                <td>{tache.Titre}</td>
                <td>{projetNom}</td>
                <td><span class='statut {statutClass}'>{statutText}</span></td>
                <td>{chiffrageJours}</td>
                <td>{tempsReelH}</td>
            </tr>";
            }

            html += $@"
        </tbody>
    </table>

    <div class='footer'>
        <p>BacklogManager - BNP Paribas © {DateTime.Now.Year}</p>
        <p>Pour convertir en PDF, utilisez Ctrl+P puis 'Enregistrer au format PDF'</p>
    </div>
</body>
</html>";

            return html;
        }

        private double CalculerProgression(BacklogItem tache)
        {
            if (!tache.ChiffrageHeures.HasValue || tache.ChiffrageHeures.Value == 0)
                return 0;
            double tempsReel = tache.TempsReelHeures ?? 0;
            return Math.Min(100, (tempsReel / tache.ChiffrageHeures.Value) * 100);
        }

        private string GetStatutDisplay(Statut statut)
        {
            switch (statut)
            {
                case Statut.Afaire: return "À faire";
                case Statut.EnCours: return "En cours";
                case Statut.Test: return "En test";
                case Statut.Termine: return "Terminé";
                default: return statut.ToString();
            }
        }

        private Brush GetStatutColor(Statut statut)
        {
            switch (statut)
            {
                case Statut.Afaire: return new SolidColorBrush(Color.FromRgb(33, 150, 243));
                case Statut.EnCours: return new SolidColorBrush(Color.FromRgb(255, 152, 0));
                case Statut.Test: return new SolidColorBrush(Color.FromRgb(156, 39, 176));
                case Statut.Termine: return new SolidColorBrush(Color.FromRgb(0, 145, 90));
                default: return new SolidColorBrush(Color.FromRgb(158, 158, 158));
            }
        }
    }

    public class TacheDevViewModel
    {
        public string Titre { get; set; }
        public string Statut { get; set; }
        public Brush StatutColor { get; set; }
        public string ProjetNom { get; set; }
        public double ChiffrageJours { get; set; }
        public double TempsReelJours { get; set; }
        public double ProgressionPct { get; set; }
        public Statut StatutOriginal { get; set; }
    }

    public class ProjetCountViewModel
    {
        public string ProjetNom { get; set; }
        public int Count { get; set; }
    }
}

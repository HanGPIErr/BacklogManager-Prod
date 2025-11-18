using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using BacklogManager.Domain;
using BacklogManager.Services;
using Microsoft.Win32;

namespace BacklogManager.Views
{
    public partial class StatistiquesWindow : Window
    {
        private readonly BacklogService _backlogService;
        private readonly CRAService _craService;
        private readonly IDatabase _database;

        public StatistiquesWindow(BacklogService backlogService, IDatabase database, CRAService craService)
        {
            InitializeComponent();
            _backlogService = backlogService;
            _database = database;
            _craService = craService;
            ChargerStatistiques();
        }

        private void ChargerStatistiques()
        {
            try
            {
                var taches = _backlogService.GetAllBacklogItems();
                var projets = _backlogService.GetAllProjets();
                var devs = _backlogService.GetAllDevs();

                // Cartes rapides
                TxtTotalTaches.Text = taches.Count.ToString();
                var tachesTerminees = taches.Count(t => t.Statut == Statut.Termine);
                TxtTachesTerminees.Text = tachesTerminees.ToString();
                TxtTachesEnCours.Text = taches.Count(t => t.Statut == Statut.EnCours).ToString();
                TxtProjetsActifs.Text = projets.Count(p => p.Actif).ToString();
                
                if (taches.Count > 0)
                {
                    var pourcentage = (tachesTerminees * 100.0 / taches.Count);
                    TxtPourcentageTerminees.Text = $"{pourcentage:F1}%";
                }

                // Graphique: Tâches par statut
                var tachesParStatut = new List<StatutStats>
                {
                    new StatutStats 
                    { 
                        Statut = "À faire", 
                        Nombre = taches.Count(t => t.Statut == Statut.Afaire),
                        Couleur = new SolidColorBrush(Color.FromRgb(255, 152, 0)),
                        LargeurMaximale = 400
                    },
                    new StatutStats 
                    { 
                        Statut = "En cours", 
                        Nombre = taches.Count(t => t.Statut == Statut.EnCours),
                        Couleur = new SolidColorBrush(Color.FromRgb(33, 150, 243)),
                        LargeurMaximale = 400
                    },
                    new StatutStats 
                    { 
                        Statut = "En test", 
                        Nombre = taches.Count(t => t.Statut == Statut.Test),
                        Couleur = new SolidColorBrush(Color.FromRgb(156, 39, 176)),
                        LargeurMaximale = 400
                    },
                    new StatutStats 
                    { 
                        Statut = "Terminé", 
                        Nombre = tachesTerminees,
                        Couleur = new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                        LargeurMaximale = 400
                    }
                };

                var maxTaches = tachesParStatut.Max(s => s.Nombre);
                if (maxTaches > 0)
                {
                    foreach (var stat in tachesParStatut)
                    {
                        stat.LargeurBarre = (stat.Nombre * 400.0 / maxTaches);
                    }
                }

                GraphiqueTachesParStatut.ItemsSource = tachesParStatut;

                // Charge par développeur
                var chargeParDev = new List<ChargeDevStats>();
                foreach (var dev in devs)
                {
                    var tachesDev = taches.Where(t => t.DevAssigneId == dev.Id).ToList();
                    var total = tachesDev.Count;
                    var chargePercent = total > 0 ? (total * 100.0 / taches.Count) : 0;

                    chargeParDev.Add(new ChargeDevStats
                    {
                        Dev = dev,
                        NomDev = dev.Nom,
                        AFaire = tachesDev.Count(t => t.Statut == Statut.Afaire),
                        EnCours = tachesDev.Count(t => t.Statut == Statut.EnCours),
                        Terminees = tachesDev.Count(t => t.Statut == Statut.Termine),
                        Total = total,
                        ChargePercent = $"{chargePercent:F1}%"
                    });
                }

                GridChargeParDev.ItemsSource = chargeParDev.OrderByDescending(c => c.Total);

                // Taux de complétion par projet
                var completionParProjet = new List<CompletionProjetStats>();
                foreach (var projet in projets.Where(p => p.Actif))
                {
                    var tachesProjet = taches.Where(t => t.ProjetId == projet.Id).ToList();
                    var totalProjet = tachesProjet.Count;
                    var termineesProjet = tachesProjet.Count(t => t.Statut == Statut.Termine);
                    var tauxCompletion = totalProjet > 0 ? (termineesProjet * 100.0 / totalProjet) : 0;

                    completionParProjet.Add(new CompletionProjetStats
                    {
                        Projet = projet,
                        NomProjet = projet.Nom,
                        TotalTaches = totalProjet,
                        TachesTerminees = termineesProjet,
                        TauxCompletion = $"{tauxCompletion:F1}%"
                    });
                }

                GridCompletionParProjet.ItemsSource = completionParProjet.OrderByDescending(c => c.TotalTaches);

                // Temps moyen par complexité - Vue d'ensemble
                var complexiteStats = new List<ComplexiteStats>();
                var complexites = new[] { 1, 2, 3, 5, 8, 13, 21, 34 };
                var maxTemps = 0.0;

                foreach (var complexite in complexites)
                {
                    var tachesComplexite = taches.Where(t => 
                        t.Complexite == complexite && 
                        t.TempsReelHeures.HasValue &&
                        t.TempsReelHeures.Value > 0).ToList();

                    if (tachesComplexite.Any())
                    {
                        var tempsMoyenHeures = tachesComplexite.Average(t => t.TempsReelHeures.Value);
                        if (tempsMoyenHeures > maxTemps) maxTemps = tempsMoyenHeures;

                        complexiteStats.Add(new ComplexiteStats
                        {
                            Complexite = $"{complexite} pts",
                            TempsHeures = tempsMoyenHeures,
                            NbTaches = tachesComplexite.Count,
                            LargeurMaximale = 500
                        });
                    }
                }

                if (maxTemps > 0)
                {
                    foreach (var stat in complexiteStats)
                    {
                        stat.LargeurBarre = (stat.TempsHeures * 500.0 / maxTemps);
                    }
                }

                GraphiqueComplexite.ItemsSource = complexiteStats;

                // Temps moyen par complexité - Par développeur
                var devComplexiteStats = new List<DevComplexiteStats>();
                var devsActifs = devs.Where(d => d.Actif).OrderBy(d => d.Nom).ToList();

                foreach (var dev in devsActifs)
                {
                    var tachesDev = taches.Where(t => 
                        t.DevAssigneId == dev.Id && 
                        t.TempsReelHeures.HasValue &&
                        t.TempsReelHeures.Value > 0).ToList();

                    if (tachesDev.Any())
                    {
                        var statsParComplexite = new List<ComplexiteStats>();
                        var maxTempsDev = 0.0;

                        foreach (var complexite in complexites)
                        {
                            var tachesComplexite = tachesDev.Where(t => t.Complexite == complexite).ToList();

                            if (tachesComplexite.Any())
                            {
                                var tempsMoyenHeures = tachesComplexite.Average(t => t.TempsReelHeures.Value);
                                if (tempsMoyenHeures > maxTempsDev) maxTempsDev = tempsMoyenHeures;

                                statsParComplexite.Add(new ComplexiteStats
                                {
                                    Complexite = $"{complexite} pts",
                                    TempsHeures = tempsMoyenHeures,
                                    NbTaches = tachesComplexite.Count,
                                    LargeurMaximale = 400
                                });
                            }
                        }

                        if (maxTempsDev > 0)
                        {
                            foreach (var stat in statsParComplexite)
                            {
                                stat.LargeurBarre = (stat.TempsHeures * 400.0 / maxTempsDev);
                            }
                        }

                        devComplexiteStats.Add(new DevComplexiteStats
                        {
                            DevNom = dev.Nom,
                            Stats = statsParComplexite
                        });
                    }
                }

                GraphiqueComplexiteParDev.ItemsSource = devComplexiteStats;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des statistiques: {ex.Message}\n\nDétails: {ex.StackTrace}", 
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnExporterPDF_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Fichier HTML (*.html)|*.html",
                    FileName = $"Rapport_Statistiques_{DateTime.Now:yyyyMMdd_HHmmss}.html"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    GenerateHtmlReport(saveFileDialog.FileName);
                    
                    var result = MessageBox.Show(
                        "Rapport HTML généré avec succès !\n\n" +
                        "Vous pouvez l'ouvrir dans votre navigateur et l'imprimer en PDF (Ctrl+P → Enregistrer en PDF).\n\n" +
                        "Voulez-vous ouvrir le fichier maintenant ?",
                        "Export réussi",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);
                    
                    if (result == MessageBoxResult.Yes)
                    {
                        System.Diagnostics.Process.Start(saveFileDialog.FileName);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'export :\n{ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GenerateHtmlReport(string filePath)
        {
            // Récupérer les données
            var taches = _backlogService.GetAllBacklogItems();
            var projets = _backlogService.GetAllProjets();
            var devs = _backlogService.GetAllDevs();
            
            var html = new System.Text.StringBuilder();
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html>");
            html.AppendLine("<head>");
            html.AppendLine("    <meta charset='utf-8'>");
            html.AppendLine("    <title>Rapport de Statistiques - BNP Paribas</title>");
            html.AppendLine("    <style>");
            html.AppendLine("        body { font-family: Arial, sans-serif; margin: 40px; background-color: #f5f5f5; }");
            html.AppendLine("        .header { background-color: #00915A; color: white; padding: 30px; text-align: center; margin-bottom: 30px; }");
            html.AppendLine("        .header h1 { margin: 0; font-size: 32px; }");
            html.AppendLine("        .header p { margin: 10px 0 0 0; font-size: 14px; opacity: 0.9; }");
            html.AppendLine("        .kpi-cards { display: flex; gap: 20px; margin-bottom: 30px; flex-wrap: wrap; }");
            html.AppendLine("        .kpi-card { flex: 1; min-width: 200px; background: white; padding: 20px; border-radius: 8px; box-shadow: 0 2px 8px rgba(0,0,0,0.1); }");
            html.AppendLine("        .kpi-card h3 { margin: 0 0 10px 0; color: #666; font-size: 14px; }");
            html.AppendLine("        .kpi-card .value { font-size: 32px; font-weight: bold; color: #00915A; }");
            html.AppendLine("        .section { background: white; padding: 25px; margin-bottom: 25px; border-radius: 8px; box-shadow: 0 2px 8px rgba(0,0,0,0.1); }");
            html.AppendLine("        .section h2 { margin: 0 0 20px 0; color: #00915A; border-bottom: 2px solid #00915A; padding-bottom: 10px; }");
            html.AppendLine("        table { width: 100%; border-collapse: collapse; margin-top: 15px; }");
            html.AppendLine("        th { background-color: #00915A; color: white; padding: 12px; text-align: left; font-weight: 600; }");
            html.AppendLine("        td { padding: 12px; border-bottom: 1px solid #e0e0e0; }");
            html.AppendLine("        tr:hover { background-color: #f5f5f5; }");
            html.AppendLine("        .bar-chart { margin: 20px 0; }");
            html.AppendLine("        .bar { background-color: #00915A; height: 30px; margin: 8px 0; border-radius: 4px; display: flex; align-items: center; padding-left: 10px; color: white; font-weight: bold; }");
            html.AppendLine("        .footer { text-align: center; margin-top: 40px; padding-top: 20px; border-top: 2px solid #e0e0e0; color: #666; font-size: 12px; }");
            html.AppendLine("        @media print { body { background: white; } .kpi-card, .section { box-shadow: none; border: 1px solid #e0e0e0; } }");
            html.AppendLine("    </style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");
            
            // Header
            html.AppendLine("    <div class='header'>");
            html.AppendLine("        <h1>📊 RAPPORT DE STATISTIQUES</h1>");
            html.AppendLine($"        <p>BNP Paribas - Backlog Manager | Généré le {DateTime.Now:dd/MM/yyyy à HH:mm}</p>");
            html.AppendLine("    </div>");
            
            // KPI Cards
            var totalTaches = taches.Count;
            var totalTerminees = taches.Count(t => t.Statut == Statut.Termine);
            var tauxCompletion = totalTaches > 0 ? (totalTerminees * 100.0 / totalTaches) : 0;
            var enCours = taches.Count(t => t.Statut == Statut.EnCours);
            
            html.AppendLine("    <div class='kpi-cards'>");
            html.AppendLine($"        <div class='kpi-card'><h3>📋 Total de tâches</h3><div class='value'>{totalTaches}</div></div>");
            html.AppendLine($"        <div class='kpi-card'><h3>✅ Taux de complétion</h3><div class='value'>{tauxCompletion:F1}%</div></div>");
            html.AppendLine($"        <div class='kpi-card'><h3>🔄 En cours</h3><div class='value'>{enCours}</div></div>");
            html.AppendLine($"        <div class='kpi-card'><h3>📁 Projets actifs</h3><div class='value'>{projets.Count(p => p.Actif)}</div></div>");
            html.AppendLine("    </div>");
            
            // Tâches par statut
            var tachesParStatut = new Dictionary<string, int>
            {
                { "À faire", taches.Count(t => t.Statut == Statut.Afaire) },
                { "En cours", taches.Count(t => t.Statut == Statut.EnCours) },
                { "En test", taches.Count(t => t.Statut == Statut.Test) },
                { "Terminé", totalTerminees }
            };
            
            html.AppendLine("    <div class='section'>");
            html.AppendLine("        <h2>📊 Tâches par Statut</h2>");
            html.AppendLine("        <div class='bar-chart'>");
            var maxTaches = tachesParStatut.Values.Any() ? tachesParStatut.Values.Max() : 1;
            foreach (var statut in tachesParStatut)
            {
                var widthPercent = maxTaches > 0 ? (statut.Value * 100.0 / maxTaches) : 0;
                html.AppendLine($"            <div class='bar' style='width: {widthPercent}%'>{statut.Key}: {statut.Value}</div>");
            }
            html.AppendLine("        </div>");
            html.AppendLine("    </div>");
            
            // Charge par développeur
            if (devs.Any())
            {
                html.AppendLine("    <div class='section'>");
                html.AppendLine("        <h2>👥 Charge par Développeur</h2>");
                html.AppendLine("        <table>");
                html.AppendLine("            <tr><th>Développeur</th><th>Nombre de tâches</th></tr>");
                foreach (var dev in devs)
                {
                    var nombreTaches = taches.Count(t => t.DevAssigneId == dev.Id);
                    if (nombreTaches > 0)
                    {
                        html.AppendLine($"            <tr><td>{dev.Nom}</td><td>{nombreTaches}</td></tr>");
                    }
                }
                html.AppendLine("        </table>");
                html.AppendLine("    </div>");
            }
            
            // Taux de complétion par projet
            var projetsActifs = projets.Where(p => p.Actif).ToList();
            if (projetsActifs.Any())
            {
                html.AppendLine("    <div class='section'>");
                html.AppendLine("        <h2>📁 Taux de Complétion par Projet</h2>");
                html.AppendLine("        <table>");
                html.AppendLine("            <tr><th>Projet</th><th>Total</th><th>Terminées</th><th>Taux</th></tr>");
                foreach (var projet in projetsActifs)
                {
                    var tachesProjet = taches.Where(t => t.ProjetId == projet.Id).ToList();
                    var totalProjet = tachesProjet.Count;
                    var termineesProjet = tachesProjet.Count(t => t.Statut == Statut.Termine);
                    var tauxProjet = totalProjet > 0 ? (termineesProjet * 100.0 / totalProjet) : 0;
                    
                    if (totalProjet > 0)
                    {
                        html.AppendLine($"            <tr><td>{projet.Nom}</td><td>{totalProjet}</td><td>{termineesProjet}</td><td>{tauxProjet:F1}%</td></tr>");
                    }
                }
                html.AppendLine("        </table>");
                html.AppendLine("    </div>");
            }
            
            // Temps moyen par complexité
            var complexites = new[] { 1, 2, 3, 5, 8, 13, 21 };
            var hasComplexiteData = false;
            var complexiteData = new Dictionary<int, (int count, double avgDays)>();
            
            foreach (var complexite in complexites)
            {
                var tachesComplexite = taches.Where(t => 
                    t.Complexite == complexite && 
                    t.Statut == Statut.Termine &&
                    t.DateCreation != default &&
                    t.DateFinAttendue.HasValue).ToList();

                if (tachesComplexite.Any())
                {
                    hasComplexiteData = true;
                    var tempsTotal = 0.0;
                    foreach (var tache in tachesComplexite)
                    {
                        var duree = (tache.DateFinAttendue.Value - tache.DateCreation).TotalDays;
                        if (duree > 0) tempsTotal += duree;
                    }
                    var tempsMoyen = tempsTotal / tachesComplexite.Count;
                    complexiteData[complexite] = (tachesComplexite.Count, tempsMoyen);
                }
            }
            
            if (hasComplexiteData)
            {
                html.AppendLine("    <div class='section'>");
                html.AppendLine("        <h2>⏱️ Temps Moyen par Complexité</h2>");
                html.AppendLine("        <table>");
                html.AppendLine("            <tr><th>Complexité</th><th>Nombre de tâches</th><th>Temps moyen (jours)</th></tr>");
                foreach (var data in complexiteData.OrderBy(d => d.Key))
                {
                    html.AppendLine($"            <tr><td>{data.Key} pts</td><td>{data.Value.count}</td><td>{data.Value.avgDays:F1}</td></tr>");
                }
                html.AppendLine("        </table>");
                html.AppendLine("    </div>");
            }
            
            // Footer
            html.AppendLine("    <div class='footer'>");
            html.AppendLine("        <p>© BNP Paribas - Backlog Manager</p>");
            html.AppendLine("        <p>Pour imprimer en PDF : Fichier → Imprimer → Enregistrer en PDF</p>");
            html.AppendLine("    </div>");
            
            html.AppendLine("</body>");
            html.AppendLine("</html>");
            
            System.IO.File.WriteAllText(filePath, html.ToString(), System.Text.Encoding.UTF8);
        }

        private void BtnActualiser_Click(object sender, RoutedEventArgs e)
        {
            ChargerStatistiques();
            MessageBox.Show("Statistiques actualisées!", "Succès", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnFermer_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void GridChargeParDev_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (GridChargeParDev.SelectedItem is ChargeDevStats devStats && devStats.Dev != null)
            {
                var detailsWindow = new DevDetailsWindow(devStats.Dev, _backlogService, _craService);
                detailsWindow.ShowDialog();
            }
        }

        private void GridCompletionParProjet_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (GridCompletionParProjet.SelectedItem is CompletionProjetStats projetStats && projetStats.Projet != null)
            {
                var detailsWindow = new ProjetDetailsWindow(projetStats.Projet, _backlogService);
                detailsWindow.ShowDialog();
            }
        }
    }

    // Classes pour les données de statistiques
    public class StatutStats
    {
        public string Statut { get; set; }
        public int Nombre { get; set; }
        public SolidColorBrush Couleur { get; set; }
        public double LargeurBarre { get; set; }
        public double LargeurMaximale { get; set; }
    }

    public class ChargeDevStats
    {
        public Dev Dev { get; set; }
        public string NomDev { get; set; }
        public int AFaire { get; set; }
        public int EnCours { get; set; }
        public int Terminees { get; set; }
        public int Total { get; set; }
        public string ChargePercent { get; set; }
    }

    public class CompletionProjetStats
    {
        public Projet Projet { get; set; }
        public string NomProjet { get; set; }
        public int TotalTaches { get; set; }
        public int TachesTerminees { get; set; }
        public string TauxCompletion { get; set; }
    }

    public class ComplexiteStats
    {
        public string Complexite { get; set; }
        public double TempsHeures { get; set; }
        public int NbTaches { get; set; }
        public double LargeurBarre { get; set; }
        public double LargeurMaximale { get; set; }
    }

    public class DevComplexiteStats
    {
        public string DevNom { get; set; }
        public List<ComplexiteStats> Stats { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BacklogManager.Domain;
using BacklogManager.Services;

namespace BacklogManager.Views
{
    public partial class DevDetailsWindow : Window
    {
        private readonly Dev _dev;
        private readonly BacklogService _backlogService;
        private List<TacheDevViewModel> _allTaches;

        public DevDetailsWindow(Dev dev, BacklogService backlogService)
        {
            InitializeComponent();
            _dev = dev;
            _backlogService = backlogService;
            
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

            var projets = _backlogService.GetAllProjets();

            // Métriques
            int total = taches.Count;
            int afaire = taches.Count(t => t.Statut == Statut.Afaire);
            int enCours = taches.Count(t => t.Statut == Statut.EnCours);
            int enTest = taches.Count(t => t.Statut == Statut.Test);
            int termine = taches.Count(t => t.Statut == Statut.Termine);

            double chargeJours = taches.Sum(t => t.ChiffrageHeures ?? 0) / 7.0;
            double tempsReel = taches.Sum(t => t.TempsReelHeures ?? 0);

            TxtTotalTaches.Text = total.ToString();
            TxtEnCours.Text = enCours.ToString();
            TxtTerminees.Text = termine.ToString();
            TxtCharge.Text = chargeJours.ToString("F1");
            TxtTempsReel.Text = tempsReel.ToString("F1");

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
            _allTaches = taches.Select(t => new TacheDevViewModel
            {
                Titre = t.Titre,
                Statut = GetStatutDisplay(t.Statut),
                StatutColor = GetStatutColor(t.Statut),
                ProjetNom = projets.FirstOrDefault(p => p.Id == t.ProjetId)?.Nom ?? "Sans projet",
                ChiffrageJours = t.ChiffrageHeures.HasValue ? t.ChiffrageHeures.Value / 7.0 : 0,
                TempsReelHeures = t.TempsReelHeures ?? 0,
                ProgressionPct = CalculerProgression(t),
                StatutOriginal = t.Statut
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
        public double TempsReelHeures { get; set; }
        public double ProgressionPct { get; set; }
        public Statut StatutOriginal { get; set; }
    }

    public class ProjetCountViewModel
    {
        public string ProjetNom { get; set; }
        public int Count { get; set; }
    }
}

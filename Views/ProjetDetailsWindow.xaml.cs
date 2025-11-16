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
    public partial class ProjetDetailsWindow : Window
    {
        private readonly Projet _projet;
        private readonly BacklogService _backlogService;
        private List<TacheDetailsViewModel> _allTaches;
        private List<TacheDetailsViewModel> _filteredTaches;

        public ProjetDetailsWindow(Projet projet, BacklogService backlogService)
        {
            InitializeComponent();
            _projet = projet;
            _backlogService = backlogService;
            
            LoadData();
        }

        private void LoadData()
        {
            // Header du projet
            TxtNomProjet.Text = _projet.Nom;
            TxtDescription.Text = string.IsNullOrEmpty(_projet.Description) ? "Aucune description" : _projet.Description;

            // Charger toutes les tâches du projet
            var taches = _backlogService.GetAllBacklogItems()
                .Where(t => t.ProjetId == _projet.Id && !t.EstArchive)
                .ToList();

            var utilisateurs = _backlogService.GetAllUtilisateurs();

            // Créer les ViewModels
            _allTaches = taches.Select(t => new TacheDetailsViewModel
            {
                Titre = t.Titre,
                Priorite = t.Priorite.ToString(),
                PrioriteColor = GetPrioriteColor(t.Priorite),
                Statut = GetStatutDisplay(t.Statut),
                DevNom = t.DevAssigneId.HasValue 
                    ? utilisateurs.FirstOrDefault(u => u.Id == t.DevAssigneId.Value)?.Nom ?? "Non assigné"
                    : "Non assigné",
                ChiffrageJours = t.ChiffrageHeures.HasValue ? t.ChiffrageHeures.Value / 7.0 : 0,
                TempsReelHeures = t.TempsReelHeures ?? 0,
                ProgressionPct = CalculerProgression(t),
                StatutOriginal = t.Statut
            }).ToList();

            _filteredTaches = new List<TacheDetailsViewModel>(_allTaches);

            // Calculer les métriques
            CalculerMetriques(taches);

            // Afficher les tâches
            ListeTaches.ItemsSource = _filteredTaches;

            // Filtres
            TxtRecherche.TextChanged += (s, e) => AppliquerFiltres();
            CmbFiltreStatut.SelectionChanged += (s, e) => AppliquerFiltres();
        }

        private void CalculerMetriques(List<BacklogItem> taches)
        {
            int total = taches.Count;
            int afaire = taches.Count(t => t.Statut == Statut.Afaire);
            int enCours = taches.Count(t => t.Statut == Statut.EnCours);
            int enTest = taches.Count(t => t.Statut == Statut.Test);
            int termine = taches.Count(t => t.Statut == Statut.Termine);

            double chargeEstimeeHeures = taches.Sum(t => t.ChiffrageHeures ?? 0);
            double tempsReelHeures = taches.Sum(t => t.TempsReelHeures ?? 0);
            
            // Progression basée sur temps réel vs charge prévue (comme Kanban)
            double progression = chargeEstimeeHeures > 0 ? Math.Min(100, (tempsReelHeures / chargeEstimeeHeures) * 100) : 0;

            // Afficher les métriques
            TxtTotalTaches.Text = total.ToString();
            TxtProgression.Text = progression.ToString("F0");
            TxtChargeEstimee.Text = (chargeEstimeeHeures / 7.0).ToString("F1");
            TxtTempsReel.Text = tempsReelHeures.ToString("F1");

            TxtCountAfaire.Text = afaire.ToString();
            TxtCountEnCours.Text = enCours.ToString();
            TxtCountEnTest.Text = enTest.ToString();
            TxtCountTermine.Text = termine.ToString();

            // Barre de progression
            ProgressBarGlobal.Value = progression;
            TxtProgressionLabel.Text = $"{tempsReelHeures:F1}h / {chargeEstimeeHeures:F1}h ({progression:F0}%)";

            // Couleur du statut
            if (progression >= 100)
            {
                BorderStatut.Background = new SolidColorBrush(Color.FromRgb(0, 145, 90)); // BNP Green
                TxtStatut.Text = "✅ TERMINÉ";
                TxtStatut.Foreground = Brushes.White;
            }
            else if (progression >= 75)
            {
                BorderStatut.Background = new SolidColorBrush(Color.FromRgb(76, 175, 80)); // Vert clair
                TxtStatut.Text = "🚀 EN BONNE VOIE";
                TxtStatut.Foreground = Brushes.White;
            }
            else if (progression >= 50)
            {
                BorderStatut.Background = new SolidColorBrush(Color.FromRgb(255, 152, 0)); // Orange
                TxtStatut.Text = "⚠️ EN COURS";
                TxtStatut.Foreground = Brushes.White;
            }
            else if (progression > 0)
            {
                BorderStatut.Background = new SolidColorBrush(Color.FromRgb(33, 150, 243)); // Bleu
                TxtStatut.Text = "🔵 DÉMARRÉ";
                TxtStatut.Foreground = Brushes.White;
            }
            else
            {
                BorderStatut.Background = new SolidColorBrush(Color.FromRgb(158, 158, 158)); // Gris
                TxtStatut.Text = "⏸️ NON DÉMARRÉ";
                TxtStatut.Foreground = Brushes.White;
            }
        }

        private void AppliquerFiltres()
        {
            var filtered = _allTaches.AsEnumerable();

            // Filtre recherche
            string recherche = TxtRecherche.Text?.ToLower() ?? "";
            if (!string.IsNullOrWhiteSpace(recherche))
            {
                filtered = filtered.Where(t => 
                    t.Titre.ToLower().Contains(recherche) ||
                    t.DevNom.ToLower().Contains(recherche));
            }

            // Filtre statut
            var statutItem = CmbFiltreStatut.SelectedItem as ComboBoxItem;
            string statutFiltre = statutItem?.Content?.ToString() ?? "Tous";
            if (statutFiltre != "Tous")
            {
                filtered = filtered.Where(t => t.Statut == statutFiltre);
            }

            _filteredTaches = filtered.ToList();
            ListeTaches.ItemsSource = _filteredTaches;
        }

        private double CalculerProgression(BacklogItem tache)
        {
            if (!tache.ChiffrageHeures.HasValue || tache.ChiffrageHeures.Value == 0)
                return 0;

            double tempsReel = tache.TempsReelHeures ?? 0;
            double progression = Math.Min(100, (tempsReel / tache.ChiffrageHeures.Value) * 100);
            return progression;
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

        private Brush GetPrioriteColor(Priorite priorite)
        {
            switch (priorite)
            {
                case Priorite.Urgent:
                    return new SolidColorBrush(Color.FromRgb(211, 47, 47)); // Rouge
                case Priorite.Haute:
                    return new SolidColorBrush(Color.FromRgb(255, 87, 34)); // Orange foncé
                case Priorite.Moyenne:
                    return new SolidColorBrush(Color.FromRgb(255, 152, 0)); // Orange
                case Priorite.Basse:
                    return new SolidColorBrush(Color.FromRgb(76, 175, 80)); // Vert
                default:
                    return new SolidColorBrush(Color.FromRgb(158, 158, 158)); // Gris
            }
        }
    }

    public class TacheDetailsViewModel
    {
        public string Titre { get; set; }
        public string Priorite { get; set; }
        public Brush PrioriteColor { get; set; }
        public string Statut { get; set; }
        public string DevNom { get; set; }
        public double ChiffrageJours { get; set; }
        public double TempsReelHeures { get; set; }
        public double ProgressionPct { get; set; }
        public Statut StatutOriginal { get; set; }
    }
}

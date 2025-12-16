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
        private bool _afficherArchivees = true; // Afficher par défaut

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
            
            // Afficher la phase du projet
            if (!string.IsNullOrEmpty(_projet.Phase))
            {
                TxtPhase.Text = _projet.Phase;
            }
            else
            {
                TxtPhase.Text = "Non définie";
            }

            // Charger et afficher les équipes assignées
            ChargerEquipes();

            // Charger toutes les tâches du projet (y compris archivées) - utiliser GetAllBacklogItemsIncludingArchived
            var taches = _backlogService.GetAllBacklogItemsIncludingArchived()
                .Where(t => t.ProjetId == _projet.Id)
                .Where(t => t.TypeDemande != TypeDemande.Conges && t.TypeDemande != TypeDemande.NonTravaille)
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
                TempsReelJours = (t.TempsReelHeures ?? 0) / 8.0, // Conversion heures en jours
                ProgressionPct = CalculerProgression(t),
                StatutOriginal = t.Statut,
                EstArchive = t.EstArchive
            }).ToList();

            _filteredTaches = new List<TacheDetailsViewModel>(_allTaches);

            // Calculer les métriques
            CalculerMetriques(taches);

            // Afficher les tâches
            ListeTaches.ItemsSource = _filteredTaches;

            // Filtres
            TxtRecherche.TextChanged += (s, e) => AppliquerFiltres();
            CmbFiltreStatut.SelectionChanged += (s, e) => AppliquerFiltres();
            ChkAfficherArchivees.Checked += (s, e) => { _afficherArchivees = true; AppliquerFiltres(); };
            ChkAfficherArchivees.Unchecked += (s, e) => { _afficherArchivees = false; AppliquerFiltres(); };
        }

        private void CalculerMetriques(List<BacklogItem> taches)
        {
            int total = taches.Count;
            int afaire = taches.Count(t => t.Statut == Statut.Afaire && !t.EstArchive);
            int enCours = taches.Count(t => t.Statut == Statut.EnCours && !t.EstArchive);
            int enTest = taches.Count(t => t.Statut == Statut.Test && !t.EstArchive);
            int termine = taches.Count(t => t.Statut == Statut.Termine || t.EstArchive);

            double chargeEstimeeHeures = taches.Sum(t => t.ChiffrageHeures ?? 0);
            double tempsReelHeures = taches.Sum(t => t.TempsReelHeures ?? 0);
            
            // Conversion en jours (7.4h = 1j)
            double chargeEstimeeJours = chargeEstimeeHeures / 7.4;
            double tempsReelJours = tempsReelHeures / 7.4;
            
            // Arrondir au demi-jour le plus proche pour l'affichage
            double tempsReelJoursAffiche = Math.Round(tempsReelJours * 2) / 2;
            
            // Progression basée sur le nombre de tâches terminées (incluant archivées) - comme dans BacklogView et ProjetsViewModel
            double progression = total > 0 ? Math.Round((double)termine / total * 100) : 0;

            // Afficher les métriques
            TxtTotalTaches.Text = total.ToString();
            TxtProgression.Text = progression.ToString("F0");
            TxtChargeEstimee.Text = chargeEstimeeJours.ToString("F1");
            TxtTempsReel.Text = tempsReelJoursAffiche.ToString("F1"); // Affichage en jours

            TxtCountAfaire.Text = afaire.ToString();
            TxtCountEnCours.Text = enCours.ToString();
            TxtCountEnTest.Text = enTest.ToString();
            TxtCountTermine.Text = termine.ToString();

            // Barre de progression
            ProgressBarGlobal.Value = progression;
            TxtProgressionLabel.Text = $"{termine} / {total} tâches ({progression:F0}%)";

            // Calcul du RAG automatique (EXACTEMENT la même logique que ProjetsViewModel)
            // Compter les tâches en retard (non terminées avec date dépassée, et non archivées)
            int nbEnRetard = taches.Count(t => 
                t.Statut != Statut.Termine && 
                !t.EstArchive &&
                t.DateFinAttendue.HasValue && 
                t.DateFinAttendue.Value < DateTime.Now);
            
            // IMPORTANT: tauxRetard calculé sur le total de TOUTES les tâches (y compris archivées)
            double tauxRetard = total > 0 ? (nbEnRetard * 100.0 / total) : 0;
            
            string statutRAG;
            SolidColorBrush couleurRAG;
            
            // Logique RAG : Red si > 30% de tâches en retard OU progression < 30% avec des retards
            if (tauxRetard > 30 || (progression < 30 && nbEnRetard > 0))
            {
                statutRAG = "RED";
                couleurRAG = new SolidColorBrush(Color.FromRgb(244, 67, 54));
            }
            // Amber si > 15% de tâches en retard OU progression entre 30-60% avec retards (et au moins 4 en retard)
            else if (tauxRetard > 15 || (progression < 60 && nbEnRetard > 3))
            {
                statutRAG = "AMBER";
                couleurRAG = new SolidColorBrush(Color.FromRgb(255, 152, 0));
            }
            else
            {
                statutRAG = "GREEN";
                couleurRAG = new SolidColorBrush(Color.FromRgb(76, 175, 80));
            }
            
            BorderRAG.Background = couleurRAG;
            TxtRAG.Text = statutRAG;

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

            // Filtre archivées
            if (!_afficherArchivees)
            {
                filtered = filtered.Where(t => !t.EstArchive);
            }

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

        private void ChargerEquipes()
        {
            if (_projet.EquipesAssigneesIds != null && _projet.EquipesAssigneesIds.Count > 0)
            {
                var toutesEquipes = _backlogService.Database.GetAllEquipes();
                var equipesAssignees = toutesEquipes
                    .Where(e => _projet.EquipesAssigneesIds.Contains(e.Id))
                    .Select(e => new { Nom = e.Nom })
                    .ToList();

                if (equipesAssignees.Count > 0)
                {
                    ListeEquipes.ItemsSource = equipesAssignees;
                    TxtAucuneEquipe.Visibility = Visibility.Collapsed;
                }
                else
                {
                    ListeEquipes.ItemsSource = null;
                    TxtAucuneEquipe.Visibility = Visibility.Visible;
                }
            }
            else
            {
                ListeEquipes.ItemsSource = null;
                TxtAucuneEquipe.Visibility = Visibility.Visible;
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
        public double TempsReelJours { get; set; } // Temps en jours (calculé lors de la création)
        public double ProgressionPct { get; set; }
        public Statut StatutOriginal { get; set; }
        public bool EstArchive { get; set; }
    }
}

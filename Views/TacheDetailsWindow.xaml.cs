using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using BacklogManager.Domain;
using BacklogManager.Services;

namespace BacklogManager.Views
{
    public partial class TacheDetailsWindow : Window
    {
        private readonly BacklogItem _tache;
        private readonly BacklogService _backlogService;
        private readonly PermissionService _permissionService;
        private readonly CRAService _craService;
        private bool _modified = false;

        public bool Modified => _modified;

        public TacheDetailsWindow(BacklogItem tache, BacklogService backlogService, 
                                  PermissionService permissionService, CRAService craService)
        {
            InitializeComponent();
            _tache = tache;
            _backlogService = backlogService;
            _permissionService = permissionService;
            _craService = craService;
            
            LoadData();
        }

        private void LoadData()
        {
            // Titre
            TxtTitre.Text = _tache.Titre;

            // Description
            TxtDescription.Text = string.IsNullOrEmpty(_tache.Description) 
                ? "Aucune description" 
                : _tache.Description;

            // Priorité
            TxtPriorite.Text = _tache.Priorite.ToString();
            BorderPriorite.Background = GetPrioriteBrush(_tache.Priorite);

            // Statut
            TxtStatut.Text = GetStatutDisplay(_tache.Statut);
            BorderStatut.Background = GetStatutBrush(_tache.Statut);

            // Type de demande
            TxtTypeDemande.Text = GetTypeDemandeDisplay(_tache.TypeDemande);

            // Dev assigné
            if (_tache.DevAssigneId.HasValue)
            {
                var dev = _backlogService.GetAllDevs().FirstOrDefault(d => d.Id == _tache.DevAssigneId.Value);
                TxtDev.Text = dev?.Nom ?? "Non assigné";
                BorderDev.Visibility = Visibility.Visible;
            }
            else
            {
                BorderDev.Visibility = Visibility.Collapsed;
            }

            // Projet
            if (_tache.ProjetId.HasValue)
            {
                var projet = _backlogService.GetAllProjets().FirstOrDefault(p => p.Id == _tache.ProjetId.Value);
                TxtProjet.Text = projet?.Nom ?? "Aucun projet";
                BorderProjet.Visibility = Visibility.Visible;
            }
            else
            {
                BorderProjet.Visibility = Visibility.Collapsed;
            }

            // Métriques
            double chiffrageJours = (_tache.ChiffrageHeures ?? 0) / 7.4;
            TxtChiffrage.Text = chiffrageJours.ToString("F1");

            double tempsReelJours = (_tache.TempsReelHeures ?? 0) / 7.4;
            TxtTempsReel.Text = tempsReelJours.ToString("F1");

            // Calcul de la progression
            double progression = 0;
            if (_tache.Statut == Statut.Termine || _tache.EstArchive)
            {
                progression = 100;
            }
            else if ((_tache.ChiffrageHeures ?? 0) > 0)
            {
                progression = Math.Min(100, ((_tache.TempsReelHeures ?? 0) / (_tache.ChiffrageHeures ?? 1)) * 100);
            }
            TxtProgression.Text = progression.ToString("F0");
            ProgressBarTache.Value = progression;
            TxtProgressionBarre.Text = $"{progression:F0}%";

            // Calcul du RAG
            string statutRAG;
            SolidColorBrush couleurRAG;
            
            if (_tache.Statut == Statut.Termine || _tache.EstArchive)
            {
                statutRAG = "GREEN";
                couleurRAG = new SolidColorBrush(Color.FromRgb(76, 175, 80));
            }
            else if (_tache.DateFinAttendue.HasValue)
            {
                var joursRestants = (_tache.DateFinAttendue.Value - DateTime.Now).TotalDays;
                
                if (joursRestants < 0)
                {
                    statutRAG = "RED";
                    couleurRAG = new SolidColorBrush(Color.FromRgb(244, 67, 54));
                }
                else if (joursRestants <= 3 && progression < 100)
                {
                    statutRAG = "AMBER";
                    couleurRAG = new SolidColorBrush(Color.FromRgb(255, 152, 0));
                }
                else if (joursRestants <= 7 && progression < 70)
                {
                    statutRAG = "AMBER";
                    couleurRAG = new SolidColorBrush(Color.FromRgb(255, 152, 0));
                }
                else
                {
                    statutRAG = "GREEN";
                    couleurRAG = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                }
            }
            else
            {
                // Pas d'échéance : basé sur l'avancement
                if (progression >= 70)
                {
                    statutRAG = "GREEN";
                    couleurRAG = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                }
                else if (progression >= 30)
                {
                    statutRAG = "AMBER";
                    couleurRAG = new SolidColorBrush(Color.FromRgb(255, 152, 0));
                }
                else
                {
                    statutRAG = "RED";
                    couleurRAG = new SolidColorBrush(Color.FromRgb(244, 67, 54));
                }
            }
            
            BorderRAG.Background = couleurRAG;
            TxtRAG.Text = statutRAG;

            // Dates
            TxtDateDebut.Text = _tache.DateDebut?.ToString("dd/MM/yyyy") ?? "Non définie";
            TxtDatePrevue.Text = _tache.DateFin?.ToString("dd/MM/yyyy") ?? "Non définie";
            TxtEcheance.Text = _tache.DateFinAttendue?.ToString("dd/MM/yyyy") ?? "Non définie";

            TxtDateCreation.Text = _tache.DateCreation.ToString("dd/MM/yyyy HH:mm");
            TxtDateModification.Text = _tache.DateDerniereMaj.ToString("dd/MM/yyyy HH:mm");
        }

        private string GetStatutDisplay(Statut statut)
        {
            switch (statut)
            {
                case Statut.Afaire: return "À faire";
                case Statut.EnCours: return "En cours";
                case Statut.Test: return "En test";
                case Statut.Termine: return "Terminé";
                case Statut.EnAttente: return "En attente";
                case Statut.APrioriser: return "À prioriser";
                default: return statut.ToString();
            }
        }

        private Brush GetStatutBrush(Statut statut)
        {
            switch (statut)
            {
                case Statut.Afaire: return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9E9E9E"));
                case Statut.EnCours: return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2196F3"));
                case Statut.Test: return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF9800"));
                case Statut.Termine: return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
                case Statut.EnAttente: return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFC107"));
                case Statut.APrioriser: return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9C27B0"));
                default: return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9E9E9E"));
            }
        }

        private Brush GetPrioriteBrush(Priorite priorite)
        {
            switch (priorite)
            {
                case Priorite.Basse: return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
                case Priorite.Moyenne: return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF9800"));
                case Priorite.Haute: return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F44336"));
                case Priorite.Urgent: return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B71C1C"));
                default: return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9E9E9E"));
            }
        }

        private string GetTypeDemandeDisplay(TypeDemande type)
        {
            switch (type)
            {
                case TypeDemande.Run: return "Run";
                case TypeDemande.Dev: return "Dev";
                case TypeDemande.Autre: return "Autre";
                case TypeDemande.Support: return "Support";
                case TypeDemande.Conges: return "Congés";
                case TypeDemande.NonTravaille: return "Non travaillé";
                default: return type.ToString();
            }
        }

        private void BtnModifier_Click(object sender, RoutedEventArgs e)
        {
            var editWindow = new EditTacheWindow(_tache, _backlogService, _permissionService, _craService);
            
            if (Owner != null)
            {
                editWindow.Owner = Owner;
            }
            
            if (editWindow.ShowDialog() == true && editWindow.Saved)
            {
                _modified = true;
                LoadData(); // Recharger les données
            }
        }

        private void BtnFermer_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = _modified;
            Close();
        }
    }
}

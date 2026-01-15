using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using BacklogManager.Domain;
using BacklogManager.Services;
using BacklogManager.Shared;

namespace BacklogManager.ViewModels
{
    public class ProjetItemViewModel : INotifyPropertyChanged
    {
        private Projet _projet;
        private int _nbAfaire;
        private int _nbEnCours;
        private int _nbEnTest;
        private int _nbTermine;
        private List<BacklogItem> _tachesProjet; // Stocker les tâches pour le calcul RAG

        public Projet Projet
        {
            get { return _projet; }
            set { _projet = value; OnPropertyChanged(); }
        }

        public int NbAfaire
        {
            get { return _nbAfaire; }
            set { _nbAfaire = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalTaches)); OnPropertyChanged(nameof(ProgressionPourcent)); }
        }

        public int NbEnCours
        {
            get { return _nbEnCours; }
            set { _nbEnCours = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalTaches)); OnPropertyChanged(nameof(ProgressionPourcent)); }
        }

        public int NbEnTest
        {
            get { return _nbEnTest; }
            set { _nbEnTest = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalTaches)); OnPropertyChanged(nameof(ProgressionPourcent)); }
        }

        public int NbTermine
        {
            get { return _nbTermine; }
            set { _nbTermine = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalTaches)); OnPropertyChanged(nameof(ProgressionPourcent)); }
        }

        public List<BacklogItem> TachesProjet
        {
            get { return _tachesProjet; }
            set { _tachesProjet = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatutRAGDisplay)); OnPropertyChanged(nameof(CouleurRAG)); }
        }

        public int TotalTaches => NbAfaire + NbEnCours + NbEnTest + NbTermine;

        public int ProgressionPourcent
        {
            get
            {
                if (TotalTaches == 0) return 0;
                return (int)((double)NbTermine / TotalTaches * 100);
            }
        }

        public string StatutRAGDisplay
        {
            get
            {
                if (!string.IsNullOrEmpty(Projet?.StatutRAG))
                    return Projet.StatutRAG.ToUpper();
                
                // Calcul automatique basé sur progression et tâches en retard (comme dans BacklogView)
                if (TachesProjet == null || TachesProjet.Count == 0)
                    return "GREEN";

                var nbTotal = TachesProjet.Count;
                var progression = ProgressionPourcent;

                // Compter les tâches en retard (non terminées avec date dépassée)
                var nbEnRetard = TachesProjet.Count(t => 
                    t.Statut != Statut.Termine && 
                    !t.EstArchive &&
                    t.DateFinAttendue.HasValue && 
                    t.DateFinAttendue.Value < DateTime.Now);

                var tauxRetard = nbTotal > 0 ? (nbEnRetard * 100.0 / nbTotal) : 0;

                // Logique RAG : Red si > 30% de tâches en retard OU progression < 30% avec des retards
                if (tauxRetard > 30 || (progression < 30 && nbEnRetard > 0))
                    return "RED";
                
                // Amber si > 15% de tâches en retard OU progression entre 30-60% avec retards
                if (tauxRetard > 15 || (progression < 60 && nbEnRetard > 3))
                    return "AMBER";
                
                return "GREEN";
            }
        }

        public System.Windows.Media.Brush CouleurRAG
        {
            get
            {
                var statutRAG = StatutRAGDisplay;
                if (statutRAG == "GREEN")
                    return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(76, 175, 80)); // #4CAF50
                if (statutRAG == "AMBER")
                    return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 152, 0)); // #FF9800
                if (statutRAG == "RED")
                    return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(244, 67, 54)); // #F44336
                return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(158, 158, 158)); // #9E9E9E
            }
        }

        public string DateCreationFormatee => Projet?.DateCreation.ToString("dd/MM/yyyy HH:mm") ?? "";

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class TacheEnrichie
    {
        private const double HEURES_PAR_JOUR = 7.4;
        
        public BacklogItem Tache { get; set; }
        public string DevAssigneNom { get; set; }

        // Propriétés miroirs pour le binding
        public string Titre => Tache?.Titre;
        public Statut Statut => Tache?.Statut ?? Statut.Afaire;
        public Priorite Priorite => Tache?.Priorite ?? Priorite.Basse;
        public int? Complexite => Tache?.Complexite;
        public DateTime DateCreation => Tache?.DateCreation ?? DateTime.Now;
        public DateTime? DateDebut => Tache?.DateDebut;
        public DateTime? DateFinPrevue => Tache?.DateFinAttendue;
        
        // % d'avancement (comme dans Kanban)
        public double Avancement
        {
            get
            {
                if (Tache == null) return 0;
                
                if (Tache.Statut == Statut.Termine || Tache.EstArchive)
                    return 100;
                
                if (Tache.ChiffrageHeures.HasValue && Tache.ChiffrageHeures.Value > 0)
                {
                    double tempsPasséHeures = Tache.TempsReelHeures ?? 0;
                    double avancement = (tempsPasséHeures / Tache.ChiffrageHeures.Value) * 100;
                    return Math.Min(100, avancement);
                }
                
                return 0;
            }
        }
        
        public string AvancementInfo => $"{Avancement:F0}%";
        
        // Statut RAG basé sur l'avancement et les retards
        public string StatutRAG
        {
            get
            {
                if (Tache == null) return "GREEN";
                
                // Si la tâche est terminée ou archivée = GREEN
                if (Tache.Statut == Statut.Termine || Tache.EstArchive)
                    return "GREEN";
                
                // Si la tâche a une échéance
                if (Tache.DateFinAttendue.HasValue)
                {
                    var joursRestants = (Tache.DateFinAttendue.Value - DateTime.Now).TotalDays;
                    
                    // Échéance dépassée = RED
                    if (joursRestants < 0)
                        return "RED";
                    
                    // Échéance très proche (moins de 3 jours) et pas terminé = AMBER
                    if (joursRestants <= 3 && Avancement < 100)
                        return "AMBER";
                    
                    // Échéance proche (moins d'une semaine) mais bon avancement = GREEN
                    if (joursRestants <= 7 && Avancement >= 70)
                        return "GREEN";
                    
                    // Échéance proche (moins d'une semaine) et avancement faible = AMBER
                    if (joursRestants <= 7 && Avancement < 70)
                        return "AMBER";
                    
                    // Échéance dans plus d'une semaine = GREEN (temps suffisant)
                    if (joursRestants > 7)
                        return "GREEN";
                }
                
                // Pas d'échéance définie : basé uniquement sur l'avancement
                if (Avancement >= 70) return "GREEN";
                if (Avancement >= 30) return "AMBER";
                
                return "RED";
            }
        }
        
        public System.Windows.Media.Brush CouleurRAG
        {
            get
            {
                if (StatutRAG == "GREEN")
                    return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(76, 175, 80));
                if (StatutRAG == "AMBER")
                    return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 152, 0));
                if (StatutRAG == "RED")
                    return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(244, 67, 54));
                return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(158, 158, 158));
            }
        }
    }

    public class ProjetsViewModel : INotifyPropertyChanged
    {
        private readonly BacklogService _backlogService;
        private readonly PermissionService _permissionService;
        private readonly CRAService _craService;
        private ProjetItemViewModel _selectedProjet;
        private string _searchText;
        private bool _filterActifsOnly;
        private string _triSelection;
        private bool _triDecroissant = true; // Plus récent en premier par défaut
        private string _prioriteSelectionnee;
        private string _statutRAGSelectionne;
        private string _equipeSelectionnee;

        public ObservableCollection<ProjetItemViewModel> Projets { get; set; }
        public ObservableCollection<ProjetItemViewModel> ProjetsFiltres { get; set; }
        public ObservableCollection<TacheEnrichie> TachesProjetSelectionne { get; set; }

        public string SearchText
        {
            get { return _searchText; }
            set
            {
                _searchText = value;
                OnPropertyChanged();
                AppliquerFiltres();
            }
        }

        public bool FilterActifsOnly
        {
            get { return _filterActifsOnly; }
            set
            {
                _filterActifsOnly = value;
                OnPropertyChanged();
                AppliquerFiltres();
            }
        }

        public string TriSelection
        {
            get { return _triSelection; }
            set
            {
                _triSelection = value;
                OnPropertyChanged();
                AppliquerFiltres();
            }
        }

        public string PrioriteSelectionnee
        {
            get { return _prioriteSelectionnee; }
            set
            {
                _prioriteSelectionnee = value;
                OnPropertyChanged();
                AppliquerFiltres();
            }
        }

        public string StatutRAGSelectionne
        {
            get { return _statutRAGSelectionne; }
            set
            {
                _statutRAGSelectionne = value;
                OnPropertyChanged();
                AppliquerFiltres();
            }
        }

        public string EquipeSelectionnee
        {
            get { return _equipeSelectionnee; }
            set
            {
                _equipeSelectionnee = value;
                OnPropertyChanged();
                AppliquerFiltres();
            }
        }

        public ObservableCollection<string> OptionsTriDisponibles { get; set; }
        public ObservableCollection<string> PrioritesDisponibles { get; set; }
        public ObservableCollection<string> StatutsRAGDisponibles { get; set; }
        public ObservableCollection<string> EquipesDisponibles { get; set; }

        public ProjetItemViewModel SelectedProjet
        {
            get { return _selectedProjet; }
            set
            {
                _selectedProjet = value;
                OnPropertyChanged();
                LoadTachesProjet();
            }
        }

        public ICommand AjouterProjetCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ClearFiltersCommand { get; }

        // Propriétés de visibilité basées sur les permissions
        public Visibility PeutCreerProjetsVisibility => _permissionService?.PeutGererReferentiels == true 
            ? Visibility.Visible : Visibility.Collapsed;
        public Visibility PeutModifierTachesVisibility => _permissionService?.PeutModifierTaches == true 
            ? Visibility.Visible : Visibility.Collapsed;
        public Visibility PeutSupprimerTachesVisibility => _permissionService?.PeutSupprimerTaches == true 
            ? Visibility.Visible : Visibility.Collapsed;
        public Visibility PeutPrioriserVisibility => _permissionService?.PeutPrioriser == true 
            ? Visibility.Visible : Visibility.Collapsed;

        public ProjetsViewModel(BacklogService backlogService, PermissionService permissionService = null, CRAService craService = null)
        {
            _backlogService = backlogService;
            _permissionService = permissionService;
            _craService = craService;
            Projets = new ObservableCollection<ProjetItemViewModel>();
            ProjetsFiltres = new ObservableCollection<ProjetItemViewModel>();
            TachesProjetSelectionne = new ObservableCollection<TacheEnrichie>();

            OptionsTriDisponibles = new ObservableCollection<string>
            {
                LocalizationService.Instance.GetString("Projects_SortDateRecent"),
                LocalizationService.Instance.GetString("Projects_SortDateOldest"),
                LocalizationService.Instance.GetString("Projects_SortNameAZ"),
                LocalizationService.Instance.GetString("Projects_SortNameZA"),
                LocalizationService.Instance.GetString("Projects_SortTasksMore"),
                LocalizationService.Instance.GetString("Projects_SortTasksLess")
            };

            // Priorités projets (nouvelles valeurs)
            PrioritesDisponibles = new ObservableCollection<string>
            {
                LocalizationService.Instance.GetString("Projects_AllFeminine"),
                "Top High",
                "High",
                "Medium",
                "Low"
            };

            // Statuts RAG
            StatutsRAGDisponibles = new ObservableCollection<string>
            {
                LocalizationService.Instance.GetString("Projects_AllMasculine"),
                "Green",
                "Amber",
                "Red"
            };

            // Équipes disponibles
            EquipesDisponibles = new ObservableCollection<string> { LocalizationService.Instance.GetString("Projects_AllFeminine") };
            var equipes = _backlogService.GetAllEquipes();
            foreach (var equipe in equipes)
            {
                EquipesDisponibles.Add(equipe.Nom);
            }

            _filterActifsOnly = true;
            _triSelection = LocalizationService.Instance.GetString("Projects_SortDateRecent");
            _prioriteSelectionnee = LocalizationService.Instance.GetString("Projects_AllFeminine");
            _statutRAGSelectionne = LocalizationService.Instance.GetString("Projects_AllMasculine");
            _equipeSelectionnee = LocalizationService.Instance.GetString("Projects_AllFeminine");

            AjouterProjetCommand = new RelayCommand(_ => AjouterProjet(), _ => CanAjouterProjet());
            RefreshCommand = new RelayCommand(_ => LoadProjets());
            ClearFiltersCommand = new RelayCommand(_ => ClearFilters());

            LoadProjets();
        }

        private bool CanAjouterProjet()
        {
            if (_permissionService == null) return true;
            return _permissionService.PeutGererReferentiels;
        }

        public void LoadProjets()
        {
            var projets = _backlogService.GetAllProjets();
            // Utiliser GetAllBacklogItemsIncludingArchived pour compter aussi les tâches archivées (comme StatistiquesViewModel)
            var taches = _backlogService.GetAllBacklogItemsIncludingArchived()
                .Where(t => t.TypeDemande != TypeDemande.Conges && t.TypeDemande != TypeDemande.NonTravaille)
                .ToList();

            Projets.Clear();
            foreach (var projet in projets)
            {
                var tachesProjet = taches.Where(t => t.ProjetId == projet.Id).ToList();

                var projetVM = new ProjetItemViewModel
                {
                    Projet = projet,
                    TachesProjet = tachesProjet, // Stocker pour le calcul RAG
                    NbAfaire = tachesProjet.Count(t => t.Statut == Statut.Afaire && !t.EstArchive),
                    NbEnCours = tachesProjet.Count(t => t.Statut == Statut.EnCours && !t.EstArchive),
                    NbEnTest = tachesProjet.Count(t => t.Statut == Statut.Test && !t.EstArchive),
                    // Terminé = Statut.Termine OU EstArchive (comme dans StatistiquesViewModel)
                    NbTermine = tachesProjet.Count(t => t.Statut == Statut.Termine || t.EstArchive)
                };

                Projets.Add(projetVM);
            }

            AppliquerFiltres();
        }

        public void LoadData()
        {
            LoadProjets();
        }

        private void AppliquerFiltres()
        {
            var projetsFiltered = Projets.AsEnumerable();

            // Filtre actifs uniquement
            if (FilterActifsOnly)
            {
                projetsFiltered = projetsFiltered.Where(p => p.Projet.Actif);
            }

            // Filtre recherche texte
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var search = SearchText.ToLower();
                projetsFiltered = projetsFiltered.Where(p => 
                    p.Projet.Nom.ToLower().Contains(search) || 
                    (p.Projet.Description ?? "").ToLower().Contains(search));
            }

            // Filtre Priorité (nouvelles valeurs: Top High, High, Medium, Low)
            if (!string.IsNullOrWhiteSpace(PrioriteSelectionnee) && PrioriteSelectionnee != LocalizationService.Instance.GetString("Projects_AllFeminine"))
            {
                projetsFiltered = projetsFiltered.Where(p => 
                    p.Projet.Priorite != null && p.Projet.Priorite.Equals(PrioriteSelectionnee, StringComparison.OrdinalIgnoreCase));
            }

            // Filtre Équipe
            if (!string.IsNullOrWhiteSpace(EquipeSelectionnee) && EquipeSelectionnee != LocalizationService.Instance.GetString("Projects_AllFeminine"))
            {
                var equipe = _backlogService.GetAllEquipes().FirstOrDefault(e => e.Nom == EquipeSelectionnee);
                if (equipe != null)
                {
                    // Filtrer par les projets qui ont cette équipe dans leur liste EquipesAssigneesIds
                    projetsFiltered = projetsFiltered.Where(p => 
                        p.Projet.EquipesAssigneesIds != null && 
                        p.Projet.EquipesAssigneesIds.Contains(equipe.Id));
                }
            }

            // Filtre Statut RAG
            if (!string.IsNullOrWhiteSpace(StatutRAGSelectionne) && StatutRAGSelectionne != LocalizationService.Instance.GetString("Projects_AllMasculine"))
            {
                // Filtrer sur le StatutRAGDisplay calculé (pas le manuel)
                projetsFiltered = projetsFiltered.Where(p => 
                    p.StatutRAGDisplay.Equals(StatutRAGSelectionne, StringComparison.OrdinalIgnoreCase));
            }

            // Tri
            switch (TriSelection)
            {
                case var sort when sort == LocalizationService.Instance.GetString("Projects_SortDateRecent"):
                    projetsFiltered = projetsFiltered.OrderByDescending(p => p.Projet.DateCreation);
                    break;
                case var sort when sort == LocalizationService.Instance.GetString("Projects_SortDateOldest"):
                    projetsFiltered = projetsFiltered.OrderBy(p => p.Projet.DateCreation);
                    break;
                case var sort when sort == LocalizationService.Instance.GetString("Projects_SortNameAZ"):
                    projetsFiltered = projetsFiltered.OrderBy(p => p.Projet.Nom);
                    break;
                case var sort when sort == LocalizationService.Instance.GetString("Projects_SortNameZA"):
                    projetsFiltered = projetsFiltered.OrderByDescending(p => p.Projet.Nom);
                    break;
                case var sort when sort == LocalizationService.Instance.GetString("Projects_SortTasksMore"):
                    projetsFiltered = projetsFiltered.OrderByDescending(p => p.TotalTaches);
                    break;
                case var sort when sort == LocalizationService.Instance.GetString("Projects_SortTasksLess"):
                    projetsFiltered = projetsFiltered.OrderBy(p => p.TotalTaches);
                    break;
            }

            ProjetsFiltres.Clear();
            foreach (var projet in projetsFiltered)
            {
                ProjetsFiltres.Add(projet);
            }
        }

        private void ClearFilters()
        {
            SearchText = "";
            FilterActifsOnly = true;
            TriSelection = LocalizationService.Instance.GetString("Projects_SortDateRecent");
            PrioriteSelectionnee = LocalizationService.Instance.GetString("Projects_AllFeminine");
            StatutRAGSelectionne = LocalizationService.Instance.GetString("Projects_AllMasculine");
            EquipeSelectionnee = LocalizationService.Instance.GetString("Projects_AllFeminine");
        }

        private void LoadTachesProjet()
        {
            TachesProjetSelectionne.Clear();
            if (SelectedProjet == null) return;

            // Charger TOUTES les tâches du projet, y compris les archivées
            var taches = _backlogService.GetAllBacklogItemsIncludingArchived()
                .Where(t => t.ProjetId == SelectedProjet.Projet.Id);

            // Appliquer le tri selon l'ordre
            taches = _triDecroissant 
                ? taches.OrderByDescending(t => t.DateCreation) // Plus récent en premier
                : taches.OrderBy(t => t.DateCreation); // Plus ancien en premier

            // Charger tous les développeurs
            var devs = _backlogService.GetAllUtilisateurs().ToDictionary(d => d.Id, d => d.Nom);

            foreach (var tache in taches.ToList())
            {
                var tacheEnrichie = new TacheEnrichie
                {
                    Tache = tache,
                    DevAssigneNom = tache.DevAssigneId.HasValue && devs.ContainsKey(tache.DevAssigneId.Value)
                        ? devs[tache.DevAssigneId.Value]
                        : null
                };
                TachesProjetSelectionne.Add(tacheEnrichie);
            }
        }

        public void InverserTriTaches()
        {
            _triDecroissant = !_triDecroissant;
            LoadTachesProjet();
        }

        public void ModifierProjet(Projet projet)
        {
            var editWindow = new Views.EditProjetWindow(_backlogService, projet);
            
            // Trouver la fenêtre parente
            var mainWindow = System.Windows.Application.Current.MainWindow;
            if (mainWindow != null && mainWindow != editWindow)
            {
                editWindow.Owner = mainWindow;
            }
            
            if (editWindow.ShowDialog() == true)
            {
                LoadData();
            }
        }

        public void SupprimerProjet(Projet projet)
        {
            try
            {
                // Récupérer les tâches liées au projet
                var tachesLiees = _backlogService.GetAllBacklogItemsIncludingArchived()
                    .Where(t => t.ProjetId == projet.Id)
                    .ToList();

                // Si des tâches sont liées, les dissocier du projet
                if (tachesLiees.Any())
                {
                    foreach (var tache in tachesLiees)
                    {
                        tache.ProjetId = null;
                        _backlogService.SaveBacklogItem(tache);
                    }
                }

                // Supprimer le projet
                _backlogService.DeleteProjet(projet.Id);
                
                // Rafraîchir la liste
                LoadData();
                
                // Si le projet supprimé était sélectionné, réinitialiser la sélection
                if (SelectedProjet != null && SelectedProjet.Projet.Id == projet.Id)
                {
                    SelectedProjet = null;
                    TachesProjetSelectionne.Clear();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Erreur lors de la suppression du projet: {ex.Message}",
                    "Erreur",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }

        private void AjouterProjet()
        {
            var editWindow = new Views.EditProjetWindow(_backlogService);
            
            // Trouver la fenêtre parente
            var mainWindow = System.Windows.Application.Current.MainWindow;
            if (mainWindow != null && mainWindow != editWindow)
            {
                editWindow.Owner = mainWindow;
            }
            
            if (editWindow.ShowDialog() == true)
            {
                LoadProjets();
            }
        }

        public void ModifierTache(BacklogItem tache)
        {
            var editWindow = new Views.EditTacheWindow(tache, _backlogService, _permissionService, _craService);
            
            // Trouver la fenêtre parente
            var mainWindow = System.Windows.Application.Current.MainWindow;
            if (mainWindow != null && mainWindow != editWindow)
            {
                editWindow.Owner = mainWindow;
            }
            
            if (editWindow.ShowDialog() == true && editWindow.Saved)
            {
                LoadProjets();
                LoadTachesProjet();
            }
        }

        public void SupprimerTache(BacklogItem tache)
        {
            tache.EstArchive = true;
            _backlogService.SaveBacklogItem(tache);
            LoadProjets();
            LoadTachesProjet();
        }

        public void VoirDetailsProjet(Projet projet)
        {
            var detailsWindow = new Views.ProjetDetailsWindow(projet, _backlogService);
            
            // Trouver la fenêtre parente
            var mainWindow = System.Windows.Application.Current.MainWindow;
            if (mainWindow != null && mainWindow != detailsWindow)
            {
                detailsWindow.Owner = mainWindow;
            }
            
            detailsWindow.ShowDialog();
        }

        public void VoirDetailsTache(BacklogItem tache)
        {
            var detailsWindow = new Views.TacheDetailsWindow(tache, _backlogService, _permissionService, _craService);
            
            // Trouver la fenêtre parente
            var mainWindow = System.Windows.Application.Current.MainWindow;
            if (mainWindow != null && mainWindow != detailsWindow)
            {
                detailsWindow.Owner = mainWindow;
            }
            
            if (detailsWindow.ShowDialog() == true && detailsWindow.Modified)
            {
                LoadProjets();
                LoadTachesProjet();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

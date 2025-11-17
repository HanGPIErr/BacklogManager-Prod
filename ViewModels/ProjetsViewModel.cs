using System;
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

        public Projet Projet
        {
            get { return _projet; }
            set { _projet = value; OnPropertyChanged(); }
        }

        public int NbAfaire
        {
            get { return _nbAfaire; }
            set { _nbAfaire = value; OnPropertyChanged(); }
        }

        public int NbEnCours
        {
            get { return _nbEnCours; }
            set { _nbEnCours = value; OnPropertyChanged(); }
        }

        public int NbEnTest
        {
            get { return _nbEnTest; }
            set { _nbEnTest = value; OnPropertyChanged(); }
        }

        public int NbTermine
        {
            get { return _nbTermine; }
            set { _nbTermine = value; OnPropertyChanged(); }
        }

        public int TotalTaches => NbAfaire + NbEnCours + NbEnTest + NbTermine;

        public string DateCreationFormatee => Projet?.DateCreation.ToString("dd/MM/yyyy HH:mm") ?? "";

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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

        public ObservableCollection<ProjetItemViewModel> Projets { get; set; }
        public ObservableCollection<ProjetItemViewModel> ProjetsFiltres { get; set; }
        public ObservableCollection<BacklogItem> TachesProjetSelectionne { get; set; }

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

        public ObservableCollection<string> OptionsTriDisponibles { get; set; }

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
            TachesProjetSelectionne = new ObservableCollection<BacklogItem>();

            OptionsTriDisponibles = new ObservableCollection<string>
            {
                "Date (plus récent)",
                "Date (plus ancien)",
                "Nom (A-Z)",
                "Nom (Z-A)",
                "Nb tâches (plus)",
                "Nb tâches (moins)"
            };

            _filterActifsOnly = true;
            _triSelection = "Date (plus récent)";

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
            var taches = _backlogService.GetAllBacklogItems();

            Projets.Clear();
            foreach (var projet in projets)
            {
                var tachesProjet = taches.Where(t => t.ProjetId == projet.Id).ToList();

                var projetVM = new ProjetItemViewModel
                {
                    Projet = projet,
                    NbAfaire = tachesProjet.Count(t => t.Statut == Statut.Afaire),
                    NbEnCours = tachesProjet.Count(t => t.Statut == Statut.EnCours),
                    NbEnTest = tachesProjet.Count(t => t.Statut == Statut.Test),
                    NbTermine = tachesProjet.Count(t => t.Statut == Statut.Termine)
                };

                Projets.Add(projetVM);
            }

            AppliquerFiltres();
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

            // Tri
            switch (TriSelection)
            {
                case "Date (plus récent)":
                    projetsFiltered = projetsFiltered.OrderByDescending(p => p.Projet.DateCreation);
                    break;
                case "Date (plus ancien)":
                    projetsFiltered = projetsFiltered.OrderBy(p => p.Projet.DateCreation);
                    break;
                case "Nom (A-Z)":
                    projetsFiltered = projetsFiltered.OrderBy(p => p.Projet.Nom);
                    break;
                case "Nom (Z-A)":
                    projetsFiltered = projetsFiltered.OrderByDescending(p => p.Projet.Nom);
                    break;
                case "Nb tâches (plus)":
                    projetsFiltered = projetsFiltered.OrderByDescending(p => p.TotalTaches);
                    break;
                case "Nb tâches (moins)":
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
            TriSelection = "Date (plus récent)";
        }

        private void LoadTachesProjet()
        {
            TachesProjetSelectionne.Clear();
            if (SelectedProjet == null) return;

            var taches = _backlogService.GetAllBacklogItems()
                .Where(t => t.ProjetId == SelectedProjet.Projet.Id)
                .OrderBy(t => t.Statut)
                .ThenByDescending(t => t.Priorite)
                .ToList();

            foreach (var tache in taches)
            {
                TachesProjetSelectionne.Add(tache);
            }
        }

        private void AjouterProjet()
        {
            var nom = Microsoft.VisualBasic.Interaction.InputBox("Nom du projet:", "Nouveau Projet", "");
            if (string.IsNullOrWhiteSpace(nom))
                return;

            var description = Microsoft.VisualBasic.Interaction.InputBox("Description (optionnel):", "Nouveau Projet", "");

            var nouveauProjet = new Projet
            {
                Nom = nom,
                Description = description,
                DateCreation = DateTime.Now,
                Actif = true
            };

            _backlogService.SaveProjet(nouveauProjet);
            LoadProjets();
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

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

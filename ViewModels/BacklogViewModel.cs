using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using BacklogManager.Domain;
using BacklogManager.Services;
using BacklogManager.Shared;

namespace BacklogManager.ViewModels
{
    public class BacklogViewModel : INotifyPropertyChanged
    {
        private readonly BacklogService _backlogService;
        private string _searchText;
        private TypeDemande? _selectedType;
        private Priorite? _selectedPriorite;
        private Statut? _selectedStatut;
        private int? _selectedDevId;
        private int? _selectedProjetId;
        private BacklogItem _selectedItem;
        private bool _isEditMode;

        public ObservableCollection<BacklogItem> BacklogItems { get; set; }
        public ObservableCollection<BacklogItem> AllBacklogItems { get; set; }
        public ObservableCollection<Utilisateur> Devs { get; set; }
        public ObservableCollection<Projet> Projets { get; set; }
        public List<int?> ComplexiteValues { get; set; }

        public string SearchText
        {
            get { return _searchText; }
            set
            {
                _searchText = value;
                OnPropertyChanged();
                ApplyFilters();
            }
        }

        public TypeDemande? SelectedType
        {
            get { return _selectedType; }
            set
            {
                _selectedType = value;
                OnPropertyChanged();
                ApplyFilters();
            }
        }

        public Priorite? SelectedPriorite
        {
            get { return _selectedPriorite; }
            set
            {
                _selectedPriorite = value;
                OnPropertyChanged();
                ApplyFilters();
            }
        }

        public Statut? SelectedStatut
        {
            get { return _selectedStatut; }
            set
            {
                _selectedStatut = value;
                OnPropertyChanged();
                ApplyFilters();
            }
        }

        public int? SelectedDevId
        {
            get { return _selectedDevId; }
            set
            {
                _selectedDevId = value;
                OnPropertyChanged();
                ApplyFilters();
            }
        }

        public int? SelectedProjetId
        {
            get { return _selectedProjetId; }
            set
            {
                _selectedProjetId = value;
                OnPropertyChanged();
                ApplyFilters();
            }
        }

        public BacklogItem SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                _selectedItem = value;
                OnPropertyChanged();
                IsEditMode = value != null;
            }
        }

        public bool IsEditMode
        {
            get { return _isEditMode; }
            set
            {
                _isEditMode = value;
                OnPropertyChanged();
            }
        }

        public ICommand SaveCommand { get; }
        public ICommand NewCommand { get; }
        public ICommand ClearFiltersCommand { get; }
        public ICommand NewProjetCommand { get; }

        public BacklogViewModel(BacklogService backlogService)
        {
            _backlogService = backlogService;
            BacklogItems = new ObservableCollection<BacklogItem>();
            AllBacklogItems = new ObservableCollection<BacklogItem>();
            Devs = new ObservableCollection<Utilisateur>();
            Projets = new ObservableCollection<Projet>();
            ComplexiteValues = new List<int?> { null, 1, 2, 3, 5, 8, 13, 21 };

            SaveCommand = new RelayCommand(_ => SaveItem());
            NewCommand = new RelayCommand(_ => CreateNewItem());
            ClearFiltersCommand = new RelayCommand(_ => ClearFilters());
            NewProjetCommand = new RelayCommand(_ => CreateNewProjet());

            LoadData();
        }

        public void LoadData()
        {
            var items = _backlogService.GetAllBacklogItems();
            AllBacklogItems.Clear();
            BacklogItems.Clear();
            foreach (var item in items)
            {
                AllBacklogItems.Add(item);
                BacklogItems.Add(item);
            }

            var utilisateurs = _backlogService.GetAllUtilisateurs();
            Devs.Clear();
            foreach (var user in utilisateurs)
            {
                Devs.Add(user);
            }

            var projets = _backlogService.GetAllProjets();
            Projets.Clear();
            foreach (var projet in projets)
            {
                Projets.Add(projet);
            }
        }

        private void ApplyFilters()
        {
            var filtered = _backlogService.SearchBacklog(_searchText, _selectedType, _selectedPriorite, _selectedStatut, _selectedDevId);
            
            if (_selectedProjetId.HasValue)
            {
                filtered = filtered.Where(x => x.ProjetId == _selectedProjetId.Value).ToList();
            }

            BacklogItems.Clear();
            foreach (var item in filtered)
            {
                BacklogItems.Add(item);
            }
        }

        private void SaveItem()
        {
            if (SelectedItem == null) return;

            _backlogService.SaveBacklogItem(SelectedItem);
            LoadData();
        }

        private void CreateNewItem()
        {
            var newItem = new BacklogItem
            {
                Titre = "Nouvelle tâche",
                Description = "",
                TypeDemande = TypeDemande.Dev,
                Statut = Statut.Afaire,
                Priorite = Priorite.Moyenne,
                DevAssigneId = SelectedDevId,
                ProjetId = SelectedProjetId,
                DateCreation = DateTime.Now,
                DateDerniereMaj = DateTime.Now,
                EstArchive = false
            };
            SelectedItem = newItem;
        }

        private void ClearFilters()
        {
            SearchText = "";
            SelectedType = null;
            SelectedPriorite = null;
            SelectedStatut = null;
            SelectedDevId = null;
            SelectedProjetId = null;
            LoadData();
        }

        private void CreateNewProjet()
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
            LoadData();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
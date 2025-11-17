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
    public class BacklogItemViewModel : INotifyPropertyChanged
    {
        private BacklogItem _item;
        private string _devNom;
        private string _projetNom;

        public BacklogItem Item
        {
            get { return _item; }
            set { _item = value; OnPropertyChanged(); }
        }

        public string DevNom
        {
            get { return _devNom; }
            set { _devNom = value; OnPropertyChanged(); }
        }

        public string ProjetNom
        {
            get { return _projetNom; }
            set { _projetNom = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class BacklogViewModel : INotifyPropertyChanged
    {
        private readonly BacklogService _backlogService;
        private readonly PermissionService _permissionService;
        private string _searchText;

        // Expose services for view code-behind
        public BacklogService BacklogService => _backlogService;
        public PermissionService PermissionService => _permissionService;
        private TypeDemande? _selectedType;
        private Priorite? _selectedPriorite;
        private Statut? _selectedStatut;
        private int? _selectedDevId;
        private int? _selectedProjetId;
        private BacklogItemViewModel _selectedItem;
        private bool _isEditMode;

        public ObservableCollection<BacklogItemViewModel> BacklogItems { get; set; }
        public ObservableCollection<BacklogItem> AllBacklogItems { get; set; }
        public ObservableCollection<Utilisateur> Devs { get; set; }
        public ObservableCollection<Projet> Projets { get; set; }
        public List<int?> ComplexiteValues { get; set; }

        // Propriétés de visibilité selon les permissions
        public Visibility PeutCreerTachesVisibility => _permissionService?.PeutCreerTaches == true ? Visibility.Visible : Visibility.Collapsed;
        public Visibility PeutModifierTachesVisibility => _permissionService?.PeutModifierTaches == true ? Visibility.Visible : Visibility.Collapsed;
        public Visibility PeutGererReferentielsVisibility => _permissionService?.PeutGererReferentiels == true ? Visibility.Visible : Visibility.Collapsed;
        public Visibility PeutCreerProjetsVisibility => _permissionService?.PeutGererReferentiels == true ? Visibility.Visible : Visibility.Collapsed;
        public Visibility PeutPrioriserVisibility => _permissionService?.PeutPrioriser == true ? Visibility.Visible : Visibility.Collapsed;
        public Visibility PeutAssignerDevVisibility => _permissionService?.PeutAssignerDev == true ? Visibility.Visible : Visibility.Collapsed;
        public Visibility PeutChiffrerVisibility => _permissionService?.PeutChiffrer == true ? Visibility.Visible : Visibility.Collapsed;

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

        public BacklogItemViewModel SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                _selectedItem = value;
                OnPropertyChanged();
                IsEditMode = value != null;
                // Mettre à jour les commandes après changement de sélection
                CommandManager.InvalidateRequerySuggested();
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
        public ICommand EditCommand { get; }

        // Événements pour notifier les créations/modifications
        public event EventHandler ProjetCreated;
        public event EventHandler TacheCreated;

        public BacklogViewModel(BacklogService backlogService, PermissionService permissionService = null)
        {
            _backlogService = backlogService;
            _permissionService = permissionService;
            BacklogItems = new ObservableCollection<BacklogItemViewModel>();
            AllBacklogItems = new ObservableCollection<BacklogItem>();
            Devs = new ObservableCollection<Utilisateur>();
            Projets = new ObservableCollection<Projet>();
            ComplexiteValues = new List<int?> { null, 1, 2, 3, 5, 8, 13, 21 };

            SaveCommand = new RelayCommand(_ => SaveItem(), _ => CanSaveItem());
            NewCommand = new RelayCommand(_ => CreateNewItem(), _ => CanCreateItem());
            ClearFiltersCommand = new RelayCommand(_ => ClearFilters());
            NewProjetCommand = new RelayCommand(_ => CreateNewProjet(), _ => CanCreateProjet());
            EditCommand = new RelayCommand(item => EditItem(item));

            LoadData();
        }

        private bool CanSaveItem()
        {
            if (_permissionService == null) return true; // Par défaut, autorisé si pas de service
            if (_selectedItem == null) return false;
            return _permissionService.PeutModifierTache(_selectedItem.Item);
        }

        private bool CanCreateItem()
        {
            if (_permissionService == null) return true;
            return _permissionService.PeutCreerTaches;
        }

        private bool CanCreateProjet()
        {
            if (_permissionService == null) return true;
            return _permissionService.PeutGererReferentiels;
        }

        public void LoadData()
        {
            var items = _backlogService.GetAllBacklogItems();
            AllBacklogItems.Clear();
            foreach (var item in items)
            {
                AllBacklogItems.Add(item);
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
            
            ApplyFilters();
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
                var devNom = "Non assigné";
                if (item.DevAssigneId.HasValue)
                {
                    var dev = Devs.FirstOrDefault(d => d.Id == item.DevAssigneId.Value);
                    devNom = dev?.Nom ?? "Non assigné";
                }

                var projetNom = "";
                if (item.ProjetId.HasValue)
                {
                    var projet = Projets.FirstOrDefault(p => p.Id == item.ProjetId.Value);
                    projetNom = projet?.Nom ?? "";
                }

                BacklogItems.Add(new BacklogItemViewModel
                {
                    Item = item,
                    DevNom = devNom,
                    ProjetNom = projetNom
                });
            }
        }

        private void SaveItem()
        {
            if (SelectedItem == null) return;

            _backlogService.SaveBacklogItem(SelectedItem.Item);
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
            
            // Ouvrir la fenêtre d'édition
            var editWindow = new Views.EditTacheWindow(newItem, _backlogService, _permissionService);
            
            // Trouver la fenêtre parente
            var mainWindow = System.Windows.Application.Current.MainWindow;
            if (mainWindow != null && mainWindow != editWindow)
            {
                editWindow.Owner = mainWindow;
            }
            
            if (editWindow.ShowDialog() == true && editWindow.Saved)
            {
                LoadData();
                TacheCreated?.Invoke(this, EventArgs.Empty);
            }
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
            ProjetCreated?.Invoke(this, EventArgs.Empty);
        }

        private void EditItem(object parameter)
        {
            if (parameter is BacklogItemViewModel itemViewModel)
            {
                // Ouvrir la fenêtre d'édition
                var editWindow = new Views.EditTacheWindow(itemViewModel.Item, _backlogService, _permissionService);
                
                // Trouver la fenêtre parente
                var mainWindow = System.Windows.Application.Current.MainWindow;
                if (mainWindow != null && mainWindow != editWindow)
                {
                    editWindow.Owner = mainWindow;
                }
                
                if (editWindow.ShowDialog() == true && editWindow.Saved)
                {
                    LoadData();
                    TacheCreated?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
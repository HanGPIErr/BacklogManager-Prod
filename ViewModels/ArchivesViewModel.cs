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
    public class ArchivesViewModel : INotifyPropertyChanged
    {
        private readonly BacklogService _backlogService;
        private readonly PermissionService _permissionService;
        private readonly CRAService _craService;

        public ObservableCollection<BacklogItemViewModel> TachesArchivees { get; set; }
        public ObservableCollection<Projet> Projets { get; set; }
        public ObservableCollection<Dev> Devs { get; set; }

        private string _searchText;
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

        private string _periodeSelectionnee;
        public string PeriodeSelectionnee
        {
            get { return _periodeSelectionnee; }
            set
            {
                _periodeSelectionnee = value;
                OnPropertyChanged();
                AppliquerFiltres();
            }
        }

        private int? _projetSelectionneId;
        public int? ProjetSelectionneId
        {
            get { return _projetSelectionneId; }
            set
            {
                _projetSelectionneId = value;
                OnPropertyChanged();
                AppliquerFiltres();
            }
        }

        private int? _devSelectionneId;
        public int? DevSelectionneId
        {
            get { return _devSelectionneId; }
            set
            {
                _devSelectionneId = value;
                OnPropertyChanged();
                AppliquerFiltres();
            }
        }

        public List<string> Periodes { get; set; }

        private List<BacklogItem> _toutesLesArchives;

        public ICommand DesarchiverTacheCommand { get; }
        public ICommand RafraichirCommand { get; }
        public ICommand ResetFiltresCommand { get; }

        public bool EstAdministrateur => _permissionService?.EstAdministrateur == true;

        public ArchivesViewModel(BacklogService backlogService, PermissionService permissionService, CRAService craService)
        {
            _backlogService = backlogService;
            _permissionService = permissionService;
            _craService = craService;

            TachesArchivees = new ObservableCollection<BacklogItemViewModel>();
            Projets = new ObservableCollection<Projet>();
            Devs = new ObservableCollection<Dev>();
            Periodes = new List<string> 
            { 
                LocalizationService.Instance.GetString("Archives_All"),
                LocalizationService.Instance.GetString("Archives_ThisWeek"),
                LocalizationService.Instance.GetString("Archives_ThisMonth"),
                LocalizationService.Instance.GetString("Archives_ThisQuarter"),
                LocalizationService.Instance.GetString("Archives_ThisYear")
            };

            _periodeSelectionnee = LocalizationService.Instance.GetString("Archives_All");

            DesarchiverTacheCommand = new RelayCommand(item => DesarchiverTache(item as BacklogItem), _ => EstAdministrateur);
            RafraichirCommand = new RelayCommand(_ => ChargerArchives());
            ResetFiltresCommand = new RelayCommand(_ => ResetFiltres());

            // S'abonner aux changements de langue
            LocalizationService.Instance.PropertyChanged += (s, e) =>
            {
                // Réinitialiser les périodes avec les nouvelles traductions
                var periodeActuelle = _periodeSelectionnee;
                var indexPeriode = Periodes.IndexOf(periodeActuelle);
                
                Periodes = new List<string>
                {
                    LocalizationService.Instance.GetString("Archives_All"),
                    LocalizationService.Instance.GetString("Archives_ThisWeek"),
                    LocalizationService.Instance.GetString("Archives_ThisMonth"),
                    LocalizationService.Instance.GetString("Archives_ThisQuarter"),
                    LocalizationService.Instance.GetString("Archives_ThisYear")
                };
                
                // Restaurer la sélection par index
                if (indexPeriode >= 0 && indexPeriode < Periodes.Count)
                {
                    _periodeSelectionnee = Periodes[indexPeriode];
                    OnPropertyChanged(nameof(PeriodeSelectionnee));
                }
                
                OnPropertyChanged(nameof(Periodes));
                
                // Recharger pour mettre à jour "Tous les projets" et "Tous les développeurs"
                ChargerArchives();
            };

            ChargerArchives();
        }

        private void ChargerArchives()
        {
            // Charger tous les projets et devs
            Projets.Clear();
            Projets.Add(new Projet { Id = 0, Nom = LocalizationService.Instance.GetString("Archives_AllProjects") });
            foreach (var projet in _backlogService.GetAllProjets())
            {
                Projets.Add(projet);
            }

            Devs.Clear();
            Devs.Add(new Dev { Id = 0, Nom = LocalizationService.Instance.GetString("Archives_AllDevelopers") });
            foreach (var dev in _backlogService.GetAllDevs())
            {
                Devs.Add(dev);
            }

            // Charger toutes les archives (y compris archivées)
            var allItems = _backlogService.GetAllBacklogItemsIncludingArchived();
            
            _toutesLesArchives = allItems
                .Where(i => i.EstArchive)
                .OrderByDescending(i => i.DateDerniereMaj)
                .ToList();

            AppliquerFiltres();
        }

        private void AppliquerFiltres()
        {
            TachesArchivees.Clear();

            if (_toutesLesArchives == null)
            {
                return;
            }

            var items = _toutesLesArchives.AsEnumerable();

            // Filtre par texte
            if (!string.IsNullOrWhiteSpace(_searchText))
            {
                items = items.Where(i => 
                    i.Titre.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (i.Description != null && i.Description.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) >= 0));
            }

            // Filtre par projet
            if (_projetSelectionneId.HasValue && _projetSelectionneId.Value > 0)
            {
                items = items.Where(i => i.ProjetId == _projetSelectionneId.Value);
            }

            // Filtre par dev
            if (_devSelectionneId.HasValue && _devSelectionneId.Value > 0)
            {
                items = items.Where(i => i.DevAssigneId == _devSelectionneId.Value);
            }

            // Filtre par période
            if (!string.IsNullOrEmpty(_periodeSelectionnee))
            {
                var maintenant = DateTime.Now;
                
                if (_periodeSelectionnee == LocalizationService.Instance.GetString("Archives_ThisWeek"))
                {
                    var debutSemaine = maintenant.AddDays(-(int)maintenant.DayOfWeek);
                    items = items.Where(i => i.DateDerniereMaj >= debutSemaine);
                }
                else if (_periodeSelectionnee == LocalizationService.Instance.GetString("Archives_ThisMonth"))
                {
                    var debutMois = new DateTime(maintenant.Year, maintenant.Month, 1);
                    items = items.Where(i => i.DateDerniereMaj >= debutMois);
                }
                else if (_periodeSelectionnee == LocalizationService.Instance.GetString("Archives_ThisQuarter"))
                {
                    var trimestre = (maintenant.Month - 1) / 3;
                    var debutTrimestre = new DateTime(maintenant.Year, trimestre * 3 + 1, 1);
                    items = items.Where(i => i.DateDerniereMaj >= debutTrimestre);
                }
                else if (_periodeSelectionnee == LocalizationService.Instance.GetString("Archives_ThisYear"))
                {
                    var debutAnnee = new DateTime(maintenant.Year, 1, 1);
                    items = items.Where(i => i.DateDerniereMaj >= debutAnnee);
                }
            }

            foreach (var item in items)
            {
                var dev = Devs.FirstOrDefault(d => d.Id == item.DevAssigneId);
                var projet = Projets.FirstOrDefault(p => p.Id == item.ProjetId);
                var tempsReel = _craService?.GetTempsReelTache(item.Id) ?? 0.0;

                var viewModel = new BacklogItemViewModel
                {
                    Item = item,
                    DevNom = dev?.Nom ?? "Non assigné",
                    ProjetNom = projet?.Nom ?? "Aucun projet",
                    TempsReel = tempsReel
                };

                if (item.ChiffrageHeures.HasValue && item.ChiffrageHeures.Value > 0)
                {
                    viewModel.EcartHeures = tempsReel - item.ChiffrageHeures.Value;
                    viewModel.EcartPourcentage = (viewModel.EcartHeures / item.ChiffrageHeures.Value) * 100;

                    if (viewModel.EcartPourcentage >= 100)
                        viewModel.EnDepassement = true;
                    else if (viewModel.EcartPourcentage >= 80)
                        viewModel.EnRisque = true;
                }

                TachesArchivees.Add(viewModel);
            }
        }

        private void ResetFiltres()
        {
            SearchText = "";
            ProjetSelectionneId = null;
            DevSelectionneId = null;
            PeriodeSelectionnee = LocalizationService.Instance.GetString("Archives_ThisMonth");
        }

        private void DesarchiverTache(BacklogItem item)
        {
            if (item == null || !EstAdministrateur) return;

            var result = MessageBox.Show(
                $"Voulez-vous désarchiver la tâche \"{item.Titre}\" ?\n\nElle sera à nouveau visible dans le Kanban et le Backlog.",
                "Désarchivage de tâche",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                item.EstArchive = false;
                item.DateDerniereMaj = DateTime.Now;
                _backlogService.SaveBacklogItem(item);

                MessageBox.Show("Tâche désarchivée avec succès !", "Désarchivage", MessageBoxButton.OK, MessageBoxImage.Information);

                ChargerArchives();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

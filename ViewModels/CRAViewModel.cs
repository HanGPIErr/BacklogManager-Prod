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
    public class CRAViewModel : INotifyPropertyChanged
    {
        private readonly CRAService _craService;
        private readonly IDatabase _db;
        private readonly int _currentUserId;
        private readonly bool _isAdmin;

        private DateTime _dateSelectionnee;
        private Utilisateur _devSelectionne;
        private BacklogItem _tacheSelectionnee;
        private double _jours;
        private string _commentaire;
        private double _totalJour;
        private string _totalJourCouleur;

        public ObservableCollection<Utilisateur> Devs { get; set; }
        public ObservableCollection<BacklogItem> TachesActives { get; set; }

        public DateTime DateSelectionnee
        {
            get => _dateSelectionnee;
            set
            {
                if (_dateSelectionnee != value)
                {
                    _dateSelectionnee = value;
                    OnPropertyChanged();
                    UpdateTotalJour();
                }
            }
        }

        public Utilisateur DevSelectionne
        {
            get => _devSelectionne;
            set
            {
                if (_devSelectionne != value)
                {
                    _devSelectionne = value;
                    OnPropertyChanged();
                    LoadTachesActives();
                    UpdateTotalJour();
                }
            }
        }

        public BacklogItem TacheSelectionnee
        {
            get => _tacheSelectionnee;
            set
            {
                if (_tacheSelectionnee != value)
                {
                    _tacheSelectionnee = value;
                    OnPropertyChanged();
                }
            }
        }

        public double Jours
        {
            get => _jours;
            set
            {
                if (_jours != value)
                {
                    _jours = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Commentaire
        {
            get => _commentaire;
            set
            {
                if (_commentaire != value)
                {
                    _commentaire = value;
                    OnPropertyChanged();
                }
            }
        }

        public double TotalJour
        {
            get => _totalJour;
            private set
            {
                if (_totalJour != value)
                {
                    _totalJour = value;
                    OnPropertyChanged();
                    UpdateTotalJourCouleur();
                }
            }
        }

        public string TotalJourCouleur
        {
            get => _totalJourCouleur;
            private set
            {
                if (_totalJourCouleur != value)
                {
                    _totalJourCouleur = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool CanSelectDev => _isAdmin;

        public ICommand SaveCRACommand { get; }
        public ICommand SetJoursCommand { get; }
        public ICommand DeleteCRACommand { get; }

        public CRAViewModel(IDatabase db, int currentUserId, bool isAdmin)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _craService = new CRAService(db);
            _currentUserId = currentUserId;
            _isAdmin = isAdmin;

            DateSelectionnee = DateTime.Today;
            Devs = new ObservableCollection<Utilisateur>();
            TachesActives = new ObservableCollection<BacklogItem>();

            SaveCRACommand = new RelayCommand(SaveCRA, CanSaveCRA);
            SetJoursCommand = new RelayCommand(param => SetJours((double)param));
            DeleteCRACommand = new RelayCommand(param => DeleteCRA((int)param));

            LoadDevs();
        }

        private void LoadDevs()
        {
            Devs.Clear();
            var utilisateurs = _db.GetUtilisateurs()
                .Where(u => u.Actif)
                .OrderBy(u => u.Nom)
                .ToList();

            foreach (var user in utilisateurs)
            {
                Devs.Add(user);
            }

            // Pré-sélection de l'utilisateur courant
            DevSelectionne = Devs.FirstOrDefault(d => d.Id == _currentUserId);
        }

        private void LoadTachesActives()
        {
            TachesActives.Clear();

            if (DevSelectionne == null)
                return;

            var taches = _craService.GetTachesActivesDev(DevSelectionne.Id);
            foreach (var tache in taches)
            {
                TachesActives.Add(tache);
            }

            // Auto-sélection si une seule tâche
            if (TachesActives.Count == 1)
            {
                TacheSelectionnee = TachesActives[0];
            }
        }

        private void UpdateTotalJour()
        {
            if (DevSelectionne == null)
            {
                TotalJour = 0;
                return;
            }

            var chargeJourHeures = _craService.GetChargeParJour(DevSelectionne.Id, DateSelectionnee);
            TotalJour = chargeJourHeures / 8.0; // Convertir heures -> jours
        }

        private void UpdateTotalJourCouleur()
        {
            if (TotalJour > 3) // Plus de 3 jours = 24h
                TotalJourCouleur = "Red";
            else if (TotalJour > 1) // Plus de 1 jour = 8h
                TotalJourCouleur = "Orange";
            else
                TotalJourCouleur = "Green";
        }

        private bool CanSaveCRA(object parameter)
        {
            return DevSelectionne != null &&
                   TacheSelectionnee != null &&
                   Jours > 0 &&
                   DateSelectionnee <= DateTime.Today;
        }

        private void SaveCRA(object parameter)
        {
            try
            {
                // Convertir jours en heures pour les validations (1j = 8h)
                double heures = Jours * 8.0;
                
                // Validation finale (limite à 3 jours = 24h par jour)
                if (TotalJour + Jours > 3)
                {
                    MessageBox.Show(
                        $"Impossible de saisir {Jours:F1}j : le total du jour dépasserait 3j (actuellement {TotalJour:F1}j).",
                        "Validation",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                var cra = new CRA
                {
                    BacklogItemId = TacheSelectionnee.Id,
                    DevId = DevSelectionne.Id,
                    Date = DateSelectionnee,
                    HeuresTravaillees = heures, // Stocker en heures dans la BDD
                    Commentaire = string.IsNullOrWhiteSpace(Commentaire) ? null : Commentaire.Trim()
                };

                _craService.SaveCRA(cra);

                MessageBox.Show(
                    $"CRA sauvegardé : {Jours:F1}j sur '{TacheSelectionnee.Titre}'",
                    "Succès",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // Reset du formulaire
                Jours = 0;
                Commentaire = string.Empty;
                TacheSelectionnee = null;
                UpdateTotalJour();

                // Si Window, fermer
                if (parameter is Window window)
                {
                    window.DialogResult = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erreur lors de la sauvegarde : {ex.Message}",
                    "Erreur",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void SetJours(double jours)
        {
            Jours = jours;
        }

        private void DeleteCRA(int craId)
        {
            try
            {
                var result = MessageBox.Show(
                    "Voulez-vous vraiment supprimer ce CRA ?",
                    "Confirmation",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _craService.DeleteCRA(craId, _currentUserId, _isAdmin);
                    UpdateTotalJour();

                    MessageBox.Show(
                        "CRA supprimé avec succès",
                        "Succès",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erreur lors de la suppression : {ex.Message}",
                    "Erreur",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

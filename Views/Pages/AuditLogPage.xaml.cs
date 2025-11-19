using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using BacklogManager.Domain;
using BacklogManager.Services;
using Microsoft.Win32;

namespace BacklogManager.Views.Pages
{
    public partial class AuditLogPage : Page
    {
        private readonly IDatabase _database;
        private readonly AuditLogService _auditLogService;
        private List<AuditLog> _allLogs;
        private List<AuditLog> _filteredLogs;
        private List<AuditLog> _pagedLogs;
        private bool _isInitialized = false;
        
        // Pagination
        private const int ITEMS_PER_PAGE = 50;
        private int _currentPage = 1;
        private int _totalPages = 1;

        public AuditLogPage(IDatabase database, AuditLogService auditLogService = null)
        {
            InitializeComponent();
            _database = database;
            _auditLogService = auditLogService;
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                _allLogs = _database.GetAuditLogs();
                if (_allLogs == null)
                    _allLogs = new List<AuditLog>();
                    
                _filteredLogs = new List<AuditLog>(_allLogs);
                
                // Peupler les filtres
                var utilisateurs = _allLogs.Select(l => new { l.UserId, l.Username }).Distinct().ToList();
                utilisateurs.Insert(0, new { UserId = 0, Username = "Tous" });
                CmbUtilisateur.ItemsSource = utilisateurs;
                CmbUtilisateur.SelectedIndex = 0;

                // Initialiser la pagination
                _currentPage = 1;
                UpdatePagination();
                
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des logs d'audit :\n{ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    
                _allLogs = new List<AuditLog>();
                _filteredLogs = new List<AuditLog>();
                DgAuditLogs.ItemsSource = _filteredLogs;
                _isInitialized = true;
            }
        }

        private void FiltreChanged(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized)
                return;
                
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            try
            {
                if (_allLogs == null)
                    _allLogs = new List<AuditLog>();
                    
                _filteredLogs = new List<AuditLog>(_allLogs);

                // Filtre par utilisateur
                if (CmbUtilisateur != null && CmbUtilisateur.SelectedItem != null)
                {
                    try
                    {
                        var selectedUser = CmbUtilisateur.SelectedItem;
                        var userIdProp = selectedUser.GetType().GetProperty("UserId");
                        if (userIdProp != null)
                        {
                            var userId = (int)userIdProp.GetValue(selectedUser);
                            if (userId > 0)
                            {
                                _filteredLogs = _filteredLogs.Where(l => l.UserId == userId).ToList();
                            }
                        }
                    }
                    catch { /* Ignorer erreur filtre utilisateur */ }
                }

                // Filtre par date début
                if (DpDateDebut != null && DpDateDebut.SelectedDate.HasValue)
                {
                    var dateDebut = DpDateDebut.SelectedDate.Value.Date;
                    _filteredLogs = _filteredLogs.Where(l => l.DateAction.Date >= dateDebut).ToList();
                }

                // Filtre par date fin
                if (DpDateFin != null && DpDateFin.SelectedDate.HasValue)
                {
                    var dateFin = DpDateFin.SelectedDate.Value.Date;
                    _filteredLogs = _filteredLogs.Where(l => l.DateAction.Date <= dateFin).ToList();
                }

                // Filtre par action
                if (CmbAction != null && CmbAction.SelectedItem != null)
                {
                    var selectedAction = (CmbAction.SelectedItem as ComboBoxItem)?.Content?.ToString();
                    if (!string.IsNullOrEmpty(selectedAction) && selectedAction != "Toutes")
                    {
                        _filteredLogs = _filteredLogs.Where(l => l.Action == selectedAction).ToList();
                    }
                }

                // Réinitialiser à la première page après filtrage
                _currentPage = 1;
                UpdatePagination();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'application des filtres :\n{ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnExporterCSV_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Filter = "Fichiers CSV (*.csv)|*.csv",
                    FileName = $"AuditLog_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
                    Title = "Exporter le journal d'audit"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    var csv = new StringBuilder();
                    csv.AppendLine("Date;Utilisateur;Action;Type d'Entité;ID Entité;Ancienne Valeur;Nouvelle Valeur;Détails");

                    foreach (var log in _filteredLogs)
                    {
                        csv.AppendLine($"{log.DateAction:dd/MM/yyyy HH:mm:ss};{log.Username};{log.Action};{log.EntityType};{log.EntityId};{log.OldValue};{log.NewValue};{log.Details}");
                    }

                    File.WriteAllText(saveDialog.FileName, csv.ToString(), Encoding.UTF8);
                    MessageBox.Show($"Export réussi !\n{_filteredLogs.Count} enregistrements exportés.", 
                        "Export CSV", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'export CSV :\n{ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnActualiser_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void UpdatePagination()
        {
            try
            {
                if (_filteredLogs == null || _filteredLogs.Count == 0)
                {
                    _totalPages = 1;
                    _currentPage = 1;
                    _pagedLogs = new List<AuditLog>();
                    DgAuditLogs.ItemsSource = _pagedLogs;
                    TxtPagination.Text = "Page 1 / 1 (0 entrées)";
                    BtnPremierePage.IsEnabled = false;
                    BtnPagePrecedente.IsEnabled = false;
                    BtnPageSuivante.IsEnabled = false;
                    BtnDernierePage.IsEnabled = false;
                    return;
                }

                // Calculer le nombre total de pages
                _totalPages = (int)Math.Ceiling((double)_filteredLogs.Count / ITEMS_PER_PAGE);

                // S'assurer que la page actuelle est valide
                if (_currentPage > _totalPages)
                    _currentPage = _totalPages;
                if (_currentPage < 1)
                    _currentPage = 1;

                // Extraire les éléments de la page actuelle
                _pagedLogs = _filteredLogs
                    .Skip((_currentPage - 1) * ITEMS_PER_PAGE)
                    .Take(ITEMS_PER_PAGE)
                    .ToList();

                // Mettre à jour le DataGrid
                DgAuditLogs.ItemsSource = null;
                DgAuditLogs.ItemsSource = _pagedLogs;

                // Mettre à jour le texte de pagination
                int startIndex = (_currentPage - 1) * ITEMS_PER_PAGE + 1;
                int endIndex = Math.Min(_currentPage * ITEMS_PER_PAGE, _filteredLogs.Count);
                TxtPagination.Text = $"Page {_currentPage} / {_totalPages} ({startIndex}-{endIndex} sur {_filteredLogs.Count} entrées)";

                // Activer/désactiver les boutons
                BtnPremierePage.IsEnabled = _currentPage > 1;
                BtnPagePrecedente.IsEnabled = _currentPage > 1;
                BtnPageSuivante.IsEnabled = _currentPage < _totalPages;
                BtnDernierePage.IsEnabled = _currentPage < _totalPages;

                // Changer la couleur des boutons selon l'état
                BtnPremierePage.Background = new System.Windows.Media.SolidColorBrush(
                    _currentPage > 1 ? (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#00915A") 
                                     : (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#95A5A6"));
                BtnPagePrecedente.Background = new System.Windows.Media.SolidColorBrush(
                    _currentPage > 1 ? (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#00915A") 
                                     : (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#95A5A6"));
                BtnPageSuivante.Background = new System.Windows.Media.SolidColorBrush(
                    _currentPage < _totalPages ? (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#00915A") 
                                               : (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#95A5A6"));
                BtnDernierePage.Background = new System.Windows.Media.SolidColorBrush(
                    _currentPage < _totalPages ? (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#00915A") 
                                               : (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#95A5A6"));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la mise à jour de la pagination :\n{ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnPremierePage_Click(object sender, RoutedEventArgs e)
        {
            _currentPage = 1;
            UpdatePagination();
        }

        private void BtnPagePrecedente_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                UpdatePagination();
            }
        }

        private void BtnPageSuivante_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage < _totalPages)
            {
                _currentPage++;
                UpdatePagination();
            }
        }

        private void BtnDernierePage_Click(object sender, RoutedEventArgs e)
        {
            _currentPage = _totalPages;
            UpdatePagination();
        }
    }
}

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

namespace BacklogManager.Views
{
    public partial class AuditLogWindow : Window
    {
        private readonly IDatabase _database;
        private List<AuditLog> _allLogs;
        private List<AuditLog> _filteredLogs;

        public AuditLogWindow(IDatabase database)
        {
            InitializeComponent();
            _database = database;
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

                // Afficher les logs
                DgAuditLogs.ItemsSource = _filteredLogs;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des logs d'audit :\n{ex.Message}\n\nStackTrace:\n{ex.StackTrace}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    
                // Initialiser avec des listes vides en cas d'erreur
                _allLogs = new List<AuditLog>();
                _filteredLogs = new List<AuditLog>();
                DgAuditLogs.ItemsSource = _filteredLogs;
            }
        }

        private void FiltreChanged(object sender, RoutedEventArgs e)
        {
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
                if (CmbUtilisateur.SelectedItem != null)
                {
                    var selectedUser = CmbUtilisateur.SelectedItem as dynamic;
                    if (selectedUser.UserId > 0)
                    {
                        _filteredLogs = _filteredLogs.Where(l => l.UserId == selectedUser.UserId).ToList();
                    }
                }

                // Filtre par date début
                if (DpDateDebut.SelectedDate.HasValue)
                {
                    var dateDebut = DpDateDebut.SelectedDate.Value.Date;
                    _filteredLogs = _filteredLogs.Where(l => l.DateAction.Date >= dateDebut).ToList();
                }

                // Filtre par date fin
                if (DpDateFin.SelectedDate.HasValue)
                {
                    var dateFin = DpDateFin.SelectedDate.Value.Date;
                    _filteredLogs = _filteredLogs.Where(l => l.DateAction.Date <= dateFin).ToList();
                }

                // Filtre par action
                if (CmbAction.SelectedItem != null)
                {
                    var selectedAction = (CmbAction.SelectedItem as ComboBoxItem)?.Content?.ToString();
                    if (!string.IsNullOrEmpty(selectedAction) && selectedAction != "Toutes")
                    {
                        _filteredLogs = _filteredLogs.Where(l => l.Action == selectedAction).ToList();
                    }
                }

                DgAuditLogs.ItemsSource = null;
                DgAuditLogs.ItemsSource = _filteredLogs;
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
                        var line = $"{log.DateAction:dd/MM/yyyy HH:mm:ss};" +
                                   $"{EscapeCsv(log.Username)};" +
                                   $"{log.Action};" +
                                   $"{EscapeCsv(log.EntityType)};" +
                                   $"{log.EntityId?.ToString() ?? ""};" +
                                   $"{EscapeCsv(log.OldValue)};" +
                                   $"{EscapeCsv(log.NewValue)};" +
                                   $"{EscapeCsv(log.Details)}";
                        csv.AppendLine(line);
                    }

                    File.WriteAllText(saveDialog.FileName, csv.ToString(), Encoding.UTF8);
                    MessageBox.Show($"Export réussi !\n{_filteredLogs.Count} entrées exportées vers :\n{saveDialog.FileName}",
                        "Export CSV", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'export CSV :\n{ex.Message}\n\nStackTrace:\n{ex.StackTrace}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            // Échapper les guillemets et entourer de guillemets si nécessaire
            if (value.Contains(";") || value.Contains("\"") || value.Contains("\n"))
            {
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }
            return value;
        }

        private void BtnActualiser_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
            MessageBox.Show("Données actualisées !", "Actualisation", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnFermer_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}

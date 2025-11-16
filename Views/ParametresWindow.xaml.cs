using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using BacklogManager.Services;
using Microsoft.Win32;

namespace BacklogManager.Views
{
    public partial class ParametresWindow : Window
    {
        private readonly IDatabase _database;
        private readonly string _backupFolder;

        public ParametresWindow(IDatabase database)
        {
            InitializeComponent();
            _database = database;
            _backupFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");
            
            // Créer le dossier de sauvegarde s'il n'existe pas
            if (!Directory.Exists(_backupFolder))
            {
                Directory.CreateDirectory(_backupFolder);
            }

            ChargerParametres();
        }

        private void ChargerParametres()
        {
            try
            {
                // Afficher le chemin actuel de la DB
                var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "backlog.db");
                TxtCheminDB.Text = dbPath;

                // Afficher la dernière sauvegarde
                AfficherDerniereSauvegarde();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des paramètres: {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AfficherDerniereSauvegarde()
        {
            try
            {
                if (Directory.Exists(_backupFolder))
                {
                    var backups = Directory.GetFiles(_backupFolder, "backup_*.zip")
                        .Select(f => new FileInfo(f))
                        .OrderByDescending(f => f.LastWriteTime)
                        .FirstOrDefault();

                    if (backups != null)
                    {
                        TxtDerniereSauvegarde.Text = $"{backups.LastWriteTime:dd/MM/yyyy HH:mm:ss} - {backups.Name}";
                    }
                }
            }
            catch { }
        }

        private void BtnChangerDB_Click(object sender, RoutedEventArgs e)
        {
            var openDialog = new OpenFileDialog
            {
                Filter = "Base de données SQLite (*.db)|*.db|Tous les fichiers (*.*)|*.*",
                Title = "Sélectionner une base de données",
                InitialDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data")
            };

            if (openDialog.ShowDialog() == true)
            {
                TxtCheminDB.Text = openDialog.FileName;
                MessageBox.Show(
                    "Le chemin de la base de données a été modifié.\n\n" +
                    "Veuillez redémarrer l'application pour appliquer les changements.\n\n" +
                    "Note: Cette fonctionnalité nécessite une configuration supplémentaire dans App.config.",
                    "Redémarrage requis",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        private void BtnExportJSON_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Filter = "Fichiers JSON (*.json)|*.json",
                    FileName = $"BacklogManager_Export_{DateTime.Now:yyyyMMdd_HHmmss}.json",
                    Title = "Exporter les données en JSON"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    // Export simplifié sans Newtonsoft.Json
                    var json = new StringBuilder();
                    json.AppendLine("{");
                    json.AppendLine($"  \"ExportDate\": \"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\",");
                    json.AppendLine($"  \"Version\": \"1.0\",");
                    
                    var items = _database.GetBacklog();
                    var projets = _database.GetProjets();
                    var users = _database.GetUtilisateurs();
                    
                    json.AppendLine($"  \"TotalBacklogItems\": {items.Count},");
                    json.AppendLine($"  \"TotalProjets\": {projets.Count},");
                    json.AppendLine($"  \"TotalUtilisateurs\": {users.Count}");
                    json.AppendLine("}");

                    File.WriteAllText(saveDialog.FileName, json.ToString(), Encoding.UTF8);

                    MessageBox.Show(
                        $"Export réussi !\n\n" +
                        $"Fichier créé: {saveDialog.FileName}\n\n" +
                        $"Statistiques:\n" +
                        $"- {items.Count} tâches\n" +
                        $"- {projets.Count} projets\n" +
                        $"- {users.Count} utilisateurs\n\n" +
                        $"Note: Pour un export complet avec toutes les données, utilisez la fonction Sauvegarde.",
                        "Export JSON", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'export JSON:\n{ex.Message}\n\nStackTrace:\n{ex.StackTrace}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnExportCSV_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Filter = "Fichiers CSV (*.csv)|*.csv",
                    FileName = $"BacklogItems_Export_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
                    Title = "Exporter les tâches en CSV"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    var items = _database.GetBacklog();
                    var csv = new StringBuilder();
                    
                    // En-tête
                    csv.AppendLine("Id;Titre;Description;Type;Priorite;Statut;Complexite;ProjetId;DevId;DateCreation");

                    // Données
                    foreach (var item in items)
                    {
                        csv.AppendLine($"{item.Id};" +
                            $"{EscapeCsv(item.Titre)};" +
                            $"{EscapeCsv(item.Description)};" +
                            $"{item.TypeDemande};" +
                            $"{item.Priorite};" +
                            $"{item.Statut};" +
                            $"{item.Complexite};" +
                            $"{item.ProjetId};" +
                            $"{item.DevAssigneId?.ToString() ?? ""};" +
                            $"{item.DateCreation:yyyy-MM-dd HH:mm:ss}");
                    }

                    File.WriteAllText(saveDialog.FileName, csv.ToString(), Encoding.UTF8);

                    MessageBox.Show($"Export réussi !\n{items.Count} tâches exportées vers:\n{saveDialog.FileName}",
                        "Export CSV", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'export CSV:\n{ex.Message}\n\nStackTrace:\n{ex.StackTrace}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            if (value.Contains(";") || value.Contains("\"") || value.Contains("\n"))
            {
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }
            return value;
        }

        private void BtnImportJSON_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "⚠️ ATTENTION ⚠️\n\n" +
                "L'import va REMPLACER toutes les données actuelles par celles du fichier JSON.\n\n" +
                "Cette action est IRRÉVERSIBLE !\n\n" +
                "Avez-vous créé une sauvegarde avant de continuer ?",
                "Confirmer l'import",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                var openDialog = new OpenFileDialog
                {
                    Filter = "Fichiers JSON (*.json)|*.json",
                    Title = "Importer des données depuis JSON"
                };

                if (openDialog.ShowDialog() == true)
                {
                    MessageBox.Show(
                        "Fonctionnalité d'import en cours de développement.\n\n" +
                        "Pour l'instant, utilisez la restauration depuis une sauvegarde.",
                        "Import JSON",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'import:\n{ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCreerSauvegarde_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var backupFileName = $"backup_{timestamp}.db";
                var backupPath = Path.Combine(_backupFolder, backupFileName);

                // Copier le fichier de base de données
                var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "backlog.db");
                
                if (File.Exists(dbPath))
                {
                    File.Copy(dbPath, backupPath, true);

                    MessageBox.Show(
                        $"Sauvegarde créée avec succès !\n\n" +
                        $"Fichier: {backupFileName}\n" +
                        $"Emplacement: {_backupFolder}",
                        "Sauvegarde",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    AfficherDerniereSauvegarde();
                }
                else
                {
                    MessageBox.Show("Le fichier de base de données n'a pas été trouvé.",
                        "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la création de la sauvegarde:\n{ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnRestaurerSauvegarde_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "⚠️ ATTENTION ⚠️\n\n" +
                "La restauration va REMPLACER la base de données actuelle.\n\n" +
                "Toutes les modifications non sauvegardées seront PERDUES !\n\n" +
                "Voulez-vous continuer ?",
                "Confirmer la restauration",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                var openDialog = new OpenFileDialog
                {
                    Filter = "Sauvegardes (*.db)|*.db",
                    Title = "Sélectionner une sauvegarde à restaurer",
                    InitialDirectory = _backupFolder
                };

                if (openDialog.ShowDialog() == true)
                {
                    var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "backlog.db");
                    
                    // Fermer toutes les connexions (nécessite un redémarrage)
                    File.Copy(openDialog.FileName, dbPath, true);

                    MessageBox.Show(
                        "Restauration effectuée avec succès !\n\n" +
                        "L'application va maintenant se fermer.\n" +
                        "Veuillez la redémarrer pour charger les données restaurées.",
                        "Restauration",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    Application.Current.Shutdown();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la restauration:\n{ex.Message}\n\nStackTrace:\n{ex.StackTrace}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CboTheme_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // Fonctionnalité future
        }

        private void CboLangue_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // Fonctionnalité future
        }

        private void BtnFermer_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}

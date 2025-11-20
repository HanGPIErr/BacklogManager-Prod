using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using BacklogManager.Services;
using BacklogManager.Domain;
using Microsoft.Win32;

namespace BacklogManager.Views
{
    public partial class ParametresWindow : Window
    {
        private readonly IDatabase _database;
        private readonly string _backupFolder;
        private DispatcherTimer _autoSaveTimer;
        private DateTime _nextAutoSave;

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
            InitialiserSauvegardeAutomatique();
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
                    var backups = Directory.GetFiles(_backupFolder, "backup_manual_*.db")
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

        #region Sauvegarde Automatique

        private void InitialiserSauvegardeAutomatique()
        {
            _autoSaveTimer = new DispatcherTimer();
            _autoSaveTimer.Tick += AutoSaveTimer_Tick;
            
            // Charger les préférences (à implémenter avec config file)
            ChkSauvegardeAuto.IsChecked = false;
            TxtIntervalleMinutes.Text = "30";
            UpdateStatutSauvegardeAuto();
        }

        private void ChkSauvegardeAuto_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (ChkSauvegardeAuto.IsChecked == true)
            {
                DemarrerSauvegardeAutomatique();
            }
            else
            {
                ArreterSauvegardeAutomatique();
            }
            UpdateStatutSauvegardeAuto();
        }

        private void BtnAppliquerIntervalle_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(TxtIntervalleMinutes.Text, out int minutes) && minutes > 0)
            {
                if (ChkSauvegardeAuto.IsChecked == true)
                {
                    DemarrerSauvegardeAutomatique();
                }
                MessageBox.Show($"Intervalle mis à jour : {minutes} minutes", 
                    "Configuration", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Veuillez saisir un nombre de minutes valide (> 0)", 
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtIntervalleMinutes.Text = "30";
            }
        }

        private void TxtIntervalleMinutes_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Autoriser uniquement les chiffres
            e.Handled = !IsTextNumeric(e.Text);
        }

        private bool IsTextNumeric(string text)
        {
            return Regex.IsMatch(text, "^[0-9]+$");
        }

        private void DemarrerSauvegardeAutomatique()
        {
            if (int.TryParse(TxtIntervalleMinutes.Text, out int minutes) && minutes > 0)
            {
                _autoSaveTimer.Stop();
                _autoSaveTimer.Interval = TimeSpan.FromMinutes(minutes);
                _autoSaveTimer.Start();
                _nextAutoSave = DateTime.Now.AddMinutes(minutes);
                UpdateStatutSauvegardeAuto();
            }
        }

        private void ArreterSauvegardeAutomatique()
        {
            _autoSaveTimer.Stop();
            UpdateStatutSauvegardeAuto();
        }

        private void AutoSaveTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                // Créer une sauvegarde automatique
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var backupFileName = $"backup_auto_{timestamp}.db";
                var backupPath = Path.Combine(_backupFolder, backupFileName);

                var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "backlog.db");
                
                if (File.Exists(dbPath))
                {
                    File.Copy(dbPath, backupPath, true);
                    
                    // Nettoyer les anciennes sauvegardes auto (garder les 10 dernières)
                    NettoyerAnciennesSauvegardes("backup_auto_*.db", 10);
                }

                // Calculer la prochaine sauvegarde
                if (int.TryParse(TxtIntervalleMinutes.Text, out int minutes))
                {
                    _nextAutoSave = DateTime.Now.AddMinutes(minutes);
                    UpdateStatutSauvegardeAuto();
                }
            }
            catch (Exception ex)
            {
                // Log silencieux pour ne pas interrompre l'utilisateur
                System.Diagnostics.Debug.WriteLine($"Erreur sauvegarde auto: {ex.Message}");
            }
        }

        private void NettoyerAnciennesSauvegardes(string pattern, int keepCount)
        {
            try
            {
                var files = Directory.GetFiles(_backupFolder, pattern)
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.LastWriteTime)
                    .Skip(keepCount);

                foreach (var file in files)
                {
                    file.Delete();
                }
            }
            catch { }
        }

        private void UpdateStatutSauvegardeAuto()
        {
            if (ChkSauvegardeAuto.IsChecked == true && _autoSaveTimer.IsEnabled)
            {
                TxtStatutSauvegardeAuto.Text = $"✅ Activée - Intervalle: {TxtIntervalleMinutes.Text} minutes";
                TxtProchaineSauvegarde.Text = $"⏰ Prochaine sauvegarde: {_nextAutoSave:dd/MM/yyyy HH:mm:ss}";
            }
            else
            {
                TxtStatutSauvegardeAuto.Text = "❌ Désactivée";
                TxtProchaineSauvegarde.Text = "";
            }
        }

        #endregion

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

        private void BtnExportSQLite_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Filter = "Base de données SQLite (*.db)|*.db",
                    FileName = $"BacklogManager_SQLite_{DateTime.Now:yyyyMMdd_HHmmss}.db",
                    Title = "Exporter la base de données SQLite"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "backlog.db");
                    
                    if (File.Exists(dbPath))
                    {
                        File.Copy(dbPath, saveDialog.FileName, true);
                        
                        var fileInfo = new FileInfo(saveDialog.FileName);
                        MessageBox.Show(
                            $"Export SQLite réussi !\n\n" +
                            $"Fichier: {Path.GetFileName(saveDialog.FileName)}\n" +
                            $"Taille: {fileInfo.Length / 1024} KB\n" +
                            $"Emplacement: {Path.GetDirectoryName(saveDialog.FileName)}",
                            "Export SQLite", 
                            MessageBoxButton.OK, 
                            MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Le fichier de base de données n'a pas été trouvé.",
                            "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'export SQLite:\n{ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnExportJSON_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Filter = "Fichiers JSON (*.json)|*.json",
                    FileName = $"BacklogManager_Full_{DateTime.Now:yyyyMMdd_HHmmss}.json",
                    Title = "Exporter toutes les données en JSON"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    var json = ExporterToutesLesDonneesJSON();
                    File.WriteAllText(saveDialog.FileName, json, Encoding.UTF8);

                    var fileInfo = new FileInfo(saveDialog.FileName);
                    MessageBox.Show(
                        $"Export JSON complet réussi !\n\n" +
                        $"Fichier: {Path.GetFileName(saveDialog.FileName)}\n" +
                        $"Taille: {fileInfo.Length / 1024} KB\n" +
                        $"Contenu: Toutes les tables (BacklogItems, Utilisateurs, Projets, CRA, etc.)",
                        "Export JSON", 
                        MessageBoxButton.OK, 
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'export JSON:\n{ex.Message}\n\nStackTrace:\n{ex.StackTrace}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnExportComplet_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Filter = "Archive ZIP (*.zip)|*.zip",
                    FileName = $"BacklogManager_Complete_{DateTime.Now:yyyyMMdd_HHmmss}.zip",
                    Title = "Export complet (SQLite + JSON)"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    var tempFolder = Path.Combine(Path.GetTempPath(), "BacklogExport_" + Guid.NewGuid().ToString());
                    Directory.CreateDirectory(tempFolder);

                    try
                    {
                        // Copier SQLite
                        var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "backlog.db");
                        var dbExportPath = Path.Combine(tempFolder, "backlog.db");
                        File.Copy(dbPath, dbExportPath, true);

                        // Créer JSON
                        var jsonContent = ExporterToutesLesDonneesJSON();
                        var jsonPath = Path.Combine(tempFolder, "data_export.json");
                        File.WriteAllText(jsonPath, jsonContent, Encoding.UTF8);

                        // Créer README
                        var readmePath = Path.Combine(tempFolder, "README.txt");
                        File.WriteAllText(readmePath, 
                            $"BacklogManager - Export Complet\n" +
                            $"================================\n\n" +
                            $"Date d'export: {DateTime.Now:dd/MM/yyyy HH:mm:ss}\n\n" +
                            $"Contenu:\n" +
                            $"- backlog.db: Base de données SQLite complète\n" +
                            $"- data_export.json: Toutes les données en format JSON\n\n" +
                            $"Pour restaurer:\n" +
                            $"- SQLite: Utiliser 'Importer SQLite' dans les paramètres\n" +
                            $"- JSON: Utiliser 'Importer JSON' dans les paramètres\n", 
                            Encoding.UTF8);

                        // Créer le ZIP
                        if (File.Exists(saveDialog.FileName))
                            File.Delete(saveDialog.FileName);
                        
                        System.IO.Compression.ZipFile.CreateFromDirectory(tempFolder, saveDialog.FileName);

                        var fileInfo = new FileInfo(saveDialog.FileName);
                        MessageBox.Show(
                            $"Export complet réussi !\n\n" +
                            $"Fichier: {Path.GetFileName(saveDialog.FileName)}\n" +
                            $"Taille: {fileInfo.Length / 1024} KB\n" +
                            $"Contenu: SQLite + JSON + README\n" +
                            $"Emplacement: {Path.GetDirectoryName(saveDialog.FileName)}",
                            "Export Complet", 
                            MessageBoxButton.OK, 
                            MessageBoxImage.Information);
                    }
                    finally
                    {
                        // Nettoyer le dossier temporaire
                        if (Directory.Exists(tempFolder))
                            Directory.Delete(tempFolder, true);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'export complet:\n{ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string ExporterToutesLesDonneesJSON()
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine($"  \"ExportDate\": \"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\",");
            sb.AppendLine($"  \"Version\": \"2.0\",");
            sb.AppendLine($"  \"Application\": \"BacklogManager\",");
            
            // BacklogItems
            var items = _database.GetBacklog();
            sb.AppendLine($"  \"BacklogItems\": [");
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                sb.AppendLine("    {");
                sb.AppendLine($"      \"Id\": {item.Id},");
                sb.AppendLine($"      \"Titre\": {EscapeJson(item.Titre)},");
                sb.AppendLine($"      \"Description\": {EscapeJson(item.Description)},");
                sb.AppendLine($"      \"TypeDemande\": \"{item.TypeDemande}\",");
                sb.AppendLine($"      \"Priorite\": \"{item.Priorite}\",");
                sb.AppendLine($"      \"Statut\": \"{item.Statut}\",");
                sb.AppendLine($"      \"Complexite\": {item.Complexite ?? 0},");
                sb.AppendLine($"      \"ChiffrageHeures\": {item.ChiffrageHeures ?? 0},");
                sb.AppendLine($"      \"TempsReelHeures\": {item.TempsReelHeures ?? 0},");
                sb.AppendLine($"      \"ProjetId\": {(item.ProjetId.HasValue ? item.ProjetId.Value.ToString() : "null")},");
                sb.AppendLine($"      \"DevAssigneId\": {(item.DevAssigneId.HasValue ? item.DevAssigneId.Value.ToString() : "null")},");
                sb.AppendLine($"      \"DateCreation\": \"{item.DateCreation:yyyy-MM-dd HH:mm:ss}\",");
                sb.AppendLine($"      \"EstArchive\": {item.EstArchive.ToString().ToLower()}");
                sb.Append("    }");
                if (i < items.Count - 1) sb.AppendLine(",");
                else sb.AppendLine();
            }
            sb.AppendLine("  ],");
            
            // Projets
            var projets = _database.GetProjets();
            sb.AppendLine($"  \"Projets\": [");
            for (int i = 0; i < projets.Count; i++)
            {
                var p = projets[i];
                sb.AppendLine("    {");
                sb.AppendLine($"      \"Id\": {p.Id},");
                sb.AppendLine($"      \"Nom\": {EscapeJson(p.Nom)},");
                sb.AppendLine($"      \"Description\": {EscapeJson(p.Description)},");
                sb.AppendLine($"      \"DateCreation\": \"{p.DateCreation:yyyy-MM-dd HH:mm:ss}\",");
                sb.AppendLine($"      \"Actif\": {p.Actif.ToString().ToLower()},");
                sb.AppendLine($"      \"CouleurHex\": {EscapeJson(p.CouleurHex)}");
                sb.Append("    }");
                if (i < projets.Count - 1) sb.AppendLine(",");
                else sb.AppendLine();
            }
            sb.AppendLine("  ],");
            
            // Utilisateurs
            var users = _database.GetUtilisateurs();
            sb.AppendLine($"  \"Utilisateurs\": [");
            for (int i = 0; i < users.Count; i++)
            {
                var u = users[i];
                sb.AppendLine("    {");
                sb.AppendLine($"      \"Id\": {u.Id},");
                sb.AppendLine($"      \"UsernameWindows\": {EscapeJson(u.UsernameWindows)},");
                sb.AppendLine($"      \"Nom\": {EscapeJson(u.Nom)},");
                sb.AppendLine($"      \"Prenom\": {EscapeJson(u.Prenom)},");
                sb.AppendLine($"      \"Email\": {EscapeJson(u.Email)},");
                sb.AppendLine($"      \"RoleId\": {u.RoleId},");
                sb.AppendLine($"      \"Actif\": {u.Actif.ToString().ToLower()}");
                sb.Append("    }");
                if (i < users.Count - 1) sb.AppendLine(",");
                else sb.AppendLine();
            }
            sb.AppendLine("  ],");
            
            // Statistiques
            sb.AppendLine($"  \"Statistics\": {{");
            sb.AppendLine($"    \"TotalBacklogItems\": {items.Count},");
            sb.AppendLine($"    \"TotalProjets\": {projets.Count},");
            sb.AppendLine($"    \"TotalUtilisateurs\": {users.Count}");
            sb.AppendLine("  }");
            
            sb.AppendLine("}");
            
            return sb.ToString();
        }

        private string EscapeJson(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "\"\"";
            
            value = value.Replace("\\", "\\\\")
                        .Replace("\"", "\\\"")
                        .Replace("\n", "\\n")
                        .Replace("\r", "\\r")
                        .Replace("\t", "\\t");
            
            return $"\"{value}\"";
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

        private void BtnImportSQLite_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "⚠️ ATTENTION ⚠️\n\n" +
                "L'import va REMPLACER la base de données actuelle.\n\n" +
                "Toutes les modifications non sauvegardées seront PERDUES !\n\n" +
                "Voulez-vous continuer ?",
                "Confirmer l'import SQLite",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                var openDialog = new OpenFileDialog
                {
                    Filter = "Base de données SQLite (*.db)|*.db",
                    Title = "Sélectionner une base SQLite à importer"
                };

                if (openDialog.ShowDialog() == true)
                {
                    var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "backlog.db");
                    
                    // Créer une sauvegarde de sécurité avant import
                    var backupBeforeImport = Path.Combine(_backupFolder, $"backup_before_import_{DateTime.Now:yyyyMMdd_HHmmss}.db");
                    if (File.Exists(dbPath))
                    {
                        File.Copy(dbPath, backupBeforeImport, true);
                    }

                    // Copier le nouveau fichier
                    File.Copy(openDialog.FileName, dbPath, true);

                    MessageBox.Show(
                        "Import SQLite effectué avec succès !\n\n" +
                        "Une sauvegarde de l'ancienne base a été créée.\n\n" +
                        "L'application va maintenant se fermer.\n" +
                        "Veuillez la redémarrer pour charger les nouvelles données.",
                        "Import SQLite",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    Application.Current.Shutdown();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'import SQLite:\n{ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnImportJSON_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "⚠️ ATTENTION ⚠️\n\n" +
                "L'import JSON va REMPLACER toutes les données actuelles par celles du fichier JSON.\n\n" +
                "Cette action est IRRÉVERSIBLE !\n\n" +
                "Une sauvegarde automatique sera créée avant l'import.\n\n" +
                "Voulez-vous continuer ?",
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
                        "Fonctionnalité d'import JSON en cours de développement.\n\n" +
                        "Pour l'instant, utilisez 'Importer SQLite' pour restaurer une base complète.\n\n" +
                        "L'import JSON sera disponible dans une prochaine version pour permettre:\n" +
                        "- Import sélectif de données\n" +
                        "- Fusion avec données existantes\n" +
                        "- Import depuis exports d'autres outils",
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
                var backupFileName = $"backup_manual_{timestamp}.db";
                var backupPath = Path.Combine(_backupFolder, backupFileName);

                // Copier le fichier de base de données
                var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "backlog.db");
                
                if (File.Exists(dbPath))
                {
                    File.Copy(dbPath, backupPath, true);

                    MessageBox.Show(
                        $"Sauvegarde manuelle créée avec succès !\n\n" +
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

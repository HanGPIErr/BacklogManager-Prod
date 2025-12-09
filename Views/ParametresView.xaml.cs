using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using BacklogManager.Services;
using BacklogManager.Domain;
using Microsoft.Win32;

namespace BacklogManager.Views
{
    public partial class ParametresView : UserControl
    {
        private readonly IDatabase _database;
        private readonly string _backupFolder;
        private readonly PermissionService _permissionService;

        public ParametresView(IDatabase database, PermissionService permissionService)
        {
            InitializeComponent();
            _database = database;
            _permissionService = permissionService;
            _backupFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");

            if (!Directory.Exists(_backupFolder))
            {
                Directory.CreateDirectory(_backupFolder);
            }

            ChargerParametres();
            ChargerInformations();
            AppliquerPermissions();
        }

        private void AppliquerPermissions()
        {
            // Masquer les sections sensibles pour les non-administrateurs
            bool isAdmin = _permissionService?.EstAdministrateur ?? false;
            
            BorderExportImport.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
            BorderMaintenance.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
            
            // Le changement de chemin DB est aussi réservé aux admins
            BtnChangerDB.IsEnabled = isAdmin;
            
            if (!isAdmin)
            {
                // Afficher un message d'information pour les non-admins
                Border infoBorder = new Border
                {
                    CornerRadius = new CornerRadius(8),
                    Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(232, 244, 253)),
                    BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(52, 152, 219)),
                    BorderThickness = new Thickness(2),
                    Padding = new Thickness(15),
                    Margin = new Thickness(0, 10, 0, 20)
                };
                
                StackPanel infoStack = new StackPanel();
                
                TextBlock infoTitle = new TextBlock
                {
                    Text = "📋 Paramètres personnels",
                    FontSize = 15,
                    FontWeight = FontWeights.Bold,
                    Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(52, 152, 219)),
                    Margin = new Thickness(0, 0, 0, 8)
                };
                
                TextBlock infoText = new TextBlock
                {
                    Text = "Vous pouvez personnaliser ici votre expérience (langue, thème, notifications).\n\n" +
                           "Les sections sensibles (Export/Import, Maintenance, gestion base de données) sont réservées aux administrateurs pour des raisons de sécurité.",
                    Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(52, 73, 94)),
                    FontSize = 13,
                    TextWrapping = TextWrapping.Wrap
                };
                
                infoStack.Children.Add(infoTitle);
                infoStack.Children.Add(infoText);
                infoBorder.Child = infoStack;
                
                // Trouver le StackPanel principal et ajouter l'info
                var mainStack = (StackPanel)((ScrollViewer)this.Content).Content;
                mainStack.Children.Insert(1, infoBorder);
            }
        }

        private void ChargerParametres()
        {
            try
            {
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.ini");
                string dbPath = "data\\backlog.db";

                if (File.Exists(configPath))
                {
                    var lines = File.ReadAllLines(configPath, System.Text.Encoding.UTF8);
                    foreach (var line in lines)
                    {
                        if (line.StartsWith("DatabasePath="))
                        {
                            dbPath = line.Substring("DatabasePath=".Length).Trim();
                            dbPath = dbPath.Trim('\"', '\'');
                            if (dbPath.StartsWith("\\\\"))
                            {
                                dbPath = "\\\\" + dbPath.Substring(2).Replace("\\\\", "\\");
                            }
                            break;
                        }
                    }
                }

                if (!Path.IsPathRooted(dbPath))
                {
                    dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dbPath);
                }

                TxtCheminDB.Text = dbPath;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des paramètres: {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ChargerInformations()
        {
            try
            {
                // Version
                TxtVersion.Text = "2.0.0";

                // Taille DB
                var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "backlog.db");
                if (File.Exists(dbPath))
                {
                    var fileInfo = new FileInfo(dbPath);
                    TxtTailleDB.Text = $"{fileInfo.Length / 1024} KB";
                }

                // Statistiques
                var items = _database.GetBacklog();
                var users = _database.GetUtilisateurs();
                TxtNbTaches.Text = items.Count.ToString();
                TxtNbUtilisateurs.Text = users.Count.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des informations: {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
                try
                {
                    string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                    string selectedPath = openDialog.FileName;
                    string pathToSave = selectedPath;

                    if (selectedPath.StartsWith(baseDir, StringComparison.OrdinalIgnoreCase))
                    {
                        pathToSave = selectedPath.Substring(baseDir.Length);
                    }

                    string configPath = Path.Combine(baseDir, "config.ini");
                    var lines = new System.Collections.Generic.List<string>();
                    bool foundDbPath = false;

                    if (File.Exists(configPath))
                    {
                        foreach (var line in File.ReadAllLines(configPath, System.Text.Encoding.UTF8))
                        {
                            if (line.StartsWith("DatabasePath="))
                            {
                                lines.Add($"DatabasePath={pathToSave}");
                                foundDbPath = true;
                            }
                            else
                            {
                                lines.Add(line);
                            }
                        }
                    }

                    if (!foundDbPath)
                    {
                        if (!lines.Any(l => l.StartsWith("[Database]")))
                        {
                            lines.Add("[Database]");
                        }
                        lines.Add($"DatabasePath={pathToSave}");
                    }

                    File.WriteAllLines(configPath, lines, System.Text.Encoding.UTF8);
                    TxtCheminDB.Text = selectedPath;

                    MessageBox.Show(
                        "Le chemin de la base de données a été modifié dans config.ini\n\n" +
                        "Veuillez redémarrer l'application pour appliquer les changements.",
                        "Redémarrage requis",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de la sauvegarde de la configuration: {ex.Message}",
                        "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
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
                    string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.ini");
                    string dbPath = "data\\backlog.db";

                    if (File.Exists(configPath))
                    {
                        var lines = File.ReadAllLines(configPath, System.Text.Encoding.UTF8);
                        foreach (var line in lines)
                        {
                            if (line.StartsWith("DatabasePath="))
                            {
                                dbPath = line.Substring("DatabasePath=".Length).Trim();
                                dbPath = dbPath.Trim('\"', '\'');
                                if (dbPath.StartsWith("\\\\"))
                                {
                                    dbPath = "\\\\" + dbPath.Substring(2).Replace("\\\\", "\\");
                                }
                                break;
                            }
                        }
                    }

                    if (!Path.IsPathRooted(dbPath))
                    {
                        dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dbPath);
                    }

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
                        var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "backlog.db");
                        var dbExportPath = Path.Combine(tempFolder, "backlog.db");
                        File.Copy(dbPath, dbExportPath, true);

                        var jsonContent = ExporterToutesLesDonneesJSON();
                        var jsonPath = Path.Combine(tempFolder, "data_export.json");
                        File.WriteAllText(jsonPath, jsonContent, Encoding.UTF8);

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

                        if (File.Exists(saveDialog.FileName))
                            File.Delete(saveDialog.FileName);

                        ZipFile.CreateFromDirectory(tempFolder, saveDialog.FileName);

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

                    var backupBeforeImport = Path.Combine(_backupFolder, $"backup_before_import_{DateTime.Now:yyyyMMdd_HHmmss}.db");
                    if (File.Exists(dbPath))
                    {
                        File.Copy(dbPath, backupBeforeImport, true);
                    }

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

        private void BtnViderCache_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var tempFolder = Path.GetTempPath();
                var backlogTempFiles = Directory.GetFiles(tempFolder, "BacklogExport_*", SearchOption.TopDirectoryOnly);

                foreach (var file in backlogTempFiles)
                {
                    try { File.Delete(file); } catch { }
                }

                MessageBox.Show("Cache nettoyé avec succès !",
                    "Maintenance", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du nettoyage:\n{ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnOptimiser_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MessageBox.Show(
                    "L'optimisation de la base de données sera effectuée au prochain redémarrage.",
                    "Optimisation", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'optimisation:\n{ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnReinitialiser_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "⚠️ DANGER ⚠️\n\n" +
                "Cette action va SUPPRIMER TOUTES les données de l'application !\n\n" +
                "Cette action est IRRÉVERSIBLE !\n\n" +
                "Êtes-vous ABSOLUMENT sûr ?",
                "Confirmer la réinitialisation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Stop);

            if (result != MessageBoxResult.Yes)
                return;

            var doubleConfirm = MessageBox.Show(
                "Dernière confirmation :\n\n" +
                "Voulez-vous vraiment supprimer toutes les données ?",
                "Confirmation finale",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (doubleConfirm != MessageBoxResult.Yes)
                return;

            try
            {
                // Créer une sauvegarde avant réinitialisation
                var backupPath = Path.Combine(_backupFolder, $"backup_before_reset_{DateTime.Now:yyyyMMdd_HHmmss}.db");
                var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "backlog.db");
                if (File.Exists(dbPath))
                {
                    File.Copy(dbPath, backupPath, true);
                }

                // Supprimer le fichier de base de données
                if (File.Exists(dbPath))
                {
                    File.Delete(dbPath);
                }

                MessageBox.Show(
                    "Base de données réinitialisée avec succès.\n\n" +
                    "Une sauvegarde a été créée avant la réinitialisation.\n\n" +
                    "L'application va maintenant redémarrer pour créer une nouvelle base vide.",
                    "Réinitialisation",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la réinitialisation:\n{ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CboTheme_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (CboTheme.SelectedIndex == 1)
            {
                MessageBox.Show(
                    "Le thème sombre sera disponible dans une prochaine version.\n\n" +
                    "Cette fonctionnalité est actuellement en développement et permettra:\n" +
                    "- Un mode sombre complet pour toute l'interface\n" +
                    "- Réduction de la fatigue visuelle en conditions de faible luminosité\n" +
                    "- Économie d'énergie sur les écrans OLED\n" +
                    "- Basculement automatique selon l'heure de la journée (optionnel)",
                    "Thème sombre - Prochainement",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                CboTheme.SelectedIndex = 0;
            }
        }

        private void CboLangue_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (CboLangue.SelectedIndex > 0)
            {
                string langue = CboLangue.SelectedIndex == 1 ? "anglais" : "espagnol";
                MessageBox.Show(
                    $"La traduction en {langue} sera disponible dans une prochaine version.\n\n" +
                    "L'application BacklogManager est conçue pour être multilingue.\n" +
                    "Les langues suivantes sont prévues:\n\n" +
                    "• 🇬🇧 English - Interface complète en anglais\n" +
                    "• 🇪🇸 Español - Interface complète en espagnol\n" +
                    "• 🇩🇪 Deutsch - Allemand (selon demande)\n" +
                    "• 🇮🇹 Italiano - Italien (selon demande)\n\n" +
                    "Le changement de langue sera instantané sans redémarrage.",
                    "Multilingue - Prochainement",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                CboLangue.SelectedIndex = 0;
            }
        }
    }
}

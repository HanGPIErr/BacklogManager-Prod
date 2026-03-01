using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using BacklogManager.Domain;

namespace BacklogManager.Services
{
    public class UpdateService
    {
        private readonly string _updateServerPath;
        private readonly string _currentVersion;

        public UpdateService()
        {
            // Lire le chemin du serveur de mise à jour depuis config.ini
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.ini");
            _updateServerPath = GetUpdateServerPath(configPath);
            
            // Récupérer la version actuelle depuis l'assembly
            _currentVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }

        public string GetDiagnosticInfo()
        {
            var info = new System.Text.StringBuilder();
            info.AppendLine("=== DIAGNOSTIC MISE À JOUR ===\n");
            
            // Version actuelle
            info.AppendLine($"Version actuelle: {_currentVersion}");
            info.AppendLine($"Assembly: {Assembly.GetExecutingAssembly().GetName().Version}");
            
            // Config.ini
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.ini");
            info.AppendLine($"\nconfig.ini: {configPath}");
            info.AppendLine($"Existe: {File.Exists(configPath)}");
            info.AppendLine($"UpdateServerPath: {_updateServerPath ?? "(null)"}");
            
            // version.json
            if (!string.IsNullOrEmpty(_updateServerPath))
            {
                string versionFilePath = Path.Combine(_updateServerPath, "version.json");
                info.AppendLine($"\nversion.json: {versionFilePath}");
                info.AppendLine($"Existe: {File.Exists(versionFilePath)}");
                
                if (File.Exists(versionFilePath))
                {
                    try
                    {
                        string jsonContent = File.ReadAllText(versionFilePath, System.Text.Encoding.UTF8);
                        
                        // Options pour désérialisation case-insensitive
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        };
                        
                        var versionInfo = JsonSerializer.Deserialize<VersionInfo>(jsonContent, options);
                        info.AppendLine($"Version remote: {versionInfo.Version}");
                        info.AppendLine($"Download URL: {versionInfo.DownloadUrl}");
                        
                        // Test comparaison
                        bool isNewer = IsNewerVersion(versionInfo.Version, _currentVersion);
                        info.AppendLine($"\nIsNewerVersion('{versionInfo.Version}', '{_currentVersion}') = {isNewer}");
                    }
                    catch (Exception ex)
                    {
                        info.AppendLine($"ERREUR lecture: {ex.Message}");
                    }
                }
            }
            
            return info.ToString();
        }

        private string GetUpdateServerPath(string configPath)
        {
            if (File.Exists(configPath))
            {
                try
                {
                    var lines = File.ReadAllLines(configPath, System.Text.Encoding.UTF8);
                    foreach (var line in lines)
                    {
                        if (line.StartsWith("UpdateServerPath="))
                        {
                            var path = line.Substring("UpdateServerPath=".Length).Trim();
                            // Nettoyer les guillemets si présents
                            path = path.Trim('\"', '\'');
                            // Normaliser les chemins UNC (remplacer \\\\ par \\\\)
                            if (!string.IsNullOrEmpty(path) && path.StartsWith("\\\\"))
                            {
                                path = "\\\\" + path.Substring(2).Replace("\\\\", "\\");
                            }
                            return path;
                        }
                    }
                }
                catch { }
            }
            return null;
        }

        public Task<VersionInfo> CheckForUpdatesAsync()
        {
            System.Diagnostics.Debug.WriteLine($"[UPDATE] CheckForUpdatesAsync démarré");
            System.Diagnostics.Debug.WriteLine($"[UPDATE] _updateServerPath = {_updateServerPath}");
            System.Diagnostics.Debug.WriteLine($"[UPDATE] _currentVersion = {_currentVersion}");
            
            if (string.IsNullOrEmpty(_updateServerPath))
            {
                System.Diagnostics.Debug.WriteLine($"[UPDATE] updateServerPath vide, abandon");
                return Task.FromResult<VersionInfo>(null);
            }

            try
            {
                string versionFilePath = Path.Combine(_updateServerPath, "version.json");
                System.Diagnostics.Debug.WriteLine($"[UPDATE] Checking: {versionFilePath}");
                
                if (!File.Exists(versionFilePath))
                {
                    System.Diagnostics.Debug.WriteLine($"[UPDATE] Fichier version.json non trouvé!");
                    return Task.FromResult<VersionInfo>(null);
                }

                string jsonContent = File.ReadAllText(versionFilePath, System.Text.Encoding.UTF8);
                System.Diagnostics.Debug.WriteLine($"[UPDATE] version.json lu: {jsonContent.Substring(0, Math.Min(100, jsonContent.Length))}...");
                
                // Options pour désérialisation case-insensitive
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                
                var versionInfo = JsonSerializer.Deserialize<VersionInfo>(jsonContent, options);
                System.Diagnostics.Debug.WriteLine($"[UPDATE] Version remote: {versionInfo.Version}");

                // Comparer les versions
                bool isNewer = IsNewerVersion(versionInfo.Version, _currentVersion);
                System.Diagnostics.Debug.WriteLine($"[UPDATE] IsNewerVersion({versionInfo.Version}, {_currentVersion}) = {isNewer}");
                
                if (isNewer)
                {
                    System.Diagnostics.Debug.WriteLine($"[UPDATE] Mise à jour disponible!");
                    return Task.FromResult(versionInfo);
                }

                System.Diagnostics.Debug.WriteLine($"[UPDATE] Pas de mise à jour (version actuelle >= version remote)");
                return Task.FromResult<VersionInfo>(null);
            }
            catch (Exception ex)
            {
                // Log l'erreur mais ne pas bloquer l'application
                System.Diagnostics.Debug.WriteLine($"[UPDATE] ERREUR: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[UPDATE] StackTrace: {ex.StackTrace}");
                return Task.FromResult<VersionInfo>(null);
            }
        }

        private bool IsNewerVersion(string remoteVersion, string currentVersion)
        {
            try
            {
                // Nettoyer les versions
                string cleanRemote = remoteVersion.Trim();
                string cleanCurrent = currentVersion.Trim();
                
                // S'assurer qu'on a bien 4 parties (X.Y.Z.W)
                var remoteParts = cleanRemote.Split('.');
                var currentParts = cleanCurrent.Split('.');
                
                // Si version remote a moins de 4 parties, ajouter des .0
                if (remoteParts.Length < 4)
                {
                    for (int i = remoteParts.Length; i < 4; i++)
                        cleanRemote += ".0";
                }
                if (currentParts.Length < 4)
                {
                    for (int i = currentParts.Length; i < 4; i++)
                        cleanCurrent += ".0";
                }
                
                System.Diagnostics.Debug.WriteLine($"[UPDATE] Comparing: '{cleanRemote}' vs '{cleanCurrent}'");
                
                Version remote = new Version(cleanRemote);
                Version current = new Version(cleanCurrent);
                
                bool result = remote > current;
                System.Diagnostics.Debug.WriteLine($"[UPDATE] remote > current = {result} (remote={remote}, current={current})");
                
                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UPDATE] IsNewerVersion ERROR: {ex.Message}");
                return false;
            }
        }

        public bool DownloadAndInstallUpdate(string downloadUrl, Action<int> progressCallback = null)
        {
            try
            {
                string tempPath = Path.Combine(Path.GetTempPath(), "BacklogManager_Update.zip");
                string extractPath = Path.Combine(Path.GetTempPath(), "BacklogManager_Update");

                // Télécharger le fichier
                if (File.Exists(downloadUrl))
                {
                    File.Copy(downloadUrl, tempPath, true);
                }
                else
                {
                    return false;
                }

                // Créer le script de mise à jour
                string updateScriptPath = Path.Combine(Path.GetTempPath(), "UpdateBacklogManager.ps1");
                string appPath = AppDomain.CurrentDomain.BaseDirectory;
                
                string script = $@"
# Attendre que l'application se ferme
Start-Sleep -Seconds 2

# Extraire le ZIP
Expand-Archive -Path '{tempPath}' -DestinationPath '{extractPath}' -Force

# Copier les fichiers
Copy-Item -Path '{extractPath}\*' -Destination '{appPath}' -Recurse -Force

# Nettoyer
Remove-Item -Path '{tempPath}' -Force
Remove-Item -Path '{extractPath}' -Recurse -Force

# Relancer l'application
Start-Process '{Path.Combine(appPath, "BacklogManager.exe")}'

# Supprimer ce script
Remove-Item -Path '{updateScriptPath}' -Force
";

                File.WriteAllText(updateScriptPath, script);

                // Lancer le script de mise à jour
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-ExecutionPolicy Bypass -WindowStyle Hidden -File \"{updateScriptPath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                System.Diagnostics.Process.Start(psi);

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur installation mise à jour: {ex.Message}");
                return false;
            }
        }

        public string GetCurrentVersion()
        {
            return _currentVersion;
        }
    }
}

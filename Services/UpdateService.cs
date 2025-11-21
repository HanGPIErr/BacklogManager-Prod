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
                            return line.Substring("UpdateServerPath=".Length).Trim();
                        }
                    }
                }
                catch { }
            }
            return null;
        }

        public async Task<VersionInfo> CheckForUpdatesAsync()
        {
            if (string.IsNullOrEmpty(_updateServerPath))
                return null;

            try
            {
                string versionFilePath = Path.Combine(_updateServerPath, "version.json");
                
                if (!File.Exists(versionFilePath))
                    return null;

                string jsonContent = File.ReadAllText(versionFilePath, System.Text.Encoding.UTF8);
                var versionInfo = JsonSerializer.Deserialize<VersionInfo>(jsonContent);

                // Comparer les versions
                if (IsNewerVersion(versionInfo.Version, _currentVersion))
                {
                    return versionInfo;
                }

                return null;
            }
            catch (Exception ex)
            {
                // Log l'erreur mais ne pas bloquer l'application
                System.Diagnostics.Debug.WriteLine($"Erreur vérification mise à jour: {ex.Message}");
                return null;
            }
        }

        private bool IsNewerVersion(string remoteVersion, string currentVersion)
        {
            try
            {
                Version remote = new Version(remoteVersion);
                Version current = new Version(currentVersion);
                return remote > current;
            }
            catch
            {
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Windows;

namespace BacklogManager.Services
{
    /// <summary>
    /// Service pour mapper automatiquement les chemins UNC en lecteurs réseau
    /// </summary>
    public static class NetworkPathMapper
    {
        // Import de l'API Windows pour mapper les lecteurs réseau
        [DllImport("mpr.dll", CharSet = CharSet.Unicode)]
        private static extern int WNetAddConnection2(ref NETRESOURCE lpNetResource, string lpPassword, string lpUsername, int dwFlags);

        [DllImport("mpr.dll", CharSet = CharSet.Unicode)]
        private static extern int WNetCancelConnection2(string lpName, int dwFlags, bool fForce);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct NETRESOURCE
        {
            public int dwScope;
            public int dwType;
            public int dwDisplayType;
            public int dwUsage;
            public string lpLocalName;
            public string lpRemoteName;
            public string lpComment;
            public string lpProvider;
        }

        private const int RESOURCETYPE_DISK = 0x00000001;
        private const int CONNECT_TEMPORARY = 0x00000004;
        /// <summary>
        /// Convertit un chemin UNC en chemin de lecteur mappé.
        /// Si le chemin n'est pas UNC, le retourne tel quel.
        /// Si un mapping existe déjà, utilise le lecteur existant.
        /// Sinon, crée un nouveau mapping automatique.
        /// </summary>
        public static string MapUncPathToDrive(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;

            // Si ce n'est pas un chemin UNC, retourner tel quel
            if (!path.StartsWith("\\\\"))
                return path;

            try
            {
                // Récupérer tous les lecteurs mappés existants
                var mappedDrives = GetMappedNetworkDrives();

                // Chercher si un lecteur existe déjà pour ce chemin UNC ou un parent
                var existingMapping = FindExistingMapping(path, mappedDrives);
                if (existingMapping != null)
                {
                    // Remplacer la partie UNC par le lecteur mappé
                    string relativePath = path.Substring(existingMapping.UncPath.Length).TrimStart('\\');
                    return Path.Combine(existingMapping.DriveLetter + ":\\", relativePath);
                }

                // Aucun mapping existant, créer un nouveau mapping
                string newDrive = MapNewDrive(path);
                if (!string.IsNullOrEmpty(newDrive))
                {
                    // Extraire la partie relative après le chemin UNC mappé
                    var uncRoot = GetUncRoot(path);
                    string relativePath = path.Substring(uncRoot.Length).TrimStart('\\');
                    return Path.Combine(newDrive + ":\\", relativePath);
                }

                // Si le mapping a échoué, afficher un message d'erreur
                System.Windows.MessageBox.Show(
                    $"Impossible de mapper automatiquement le chemin réseau:\n{path}\n\n" +
                    "Veuillez mapper manuellement un lecteur réseau (ex: Z:) vers ce partage,\n" +
                    "puis modifier le chemin dans config.ini pour utiliser le lecteur mappé.\n\n" +
                    "Exemple: DatabasePath=Z:\\backlog.db",
                    "Erreur de mapping réseau",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);

                // Retourner le chemin original (échouera probablement avec SQLite)
                return path;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur mapping UNC: {ex.Message}");
                return path;
            }
        }

        /// <summary>
        /// Récupère tous les lecteurs réseau mappés avec leurs chemins UNC
        /// </summary>
        private static List<NetworkDriveMapping> GetMappedNetworkDrives()
        {
            var mappings = new List<NetworkDriveMapping>();

            try
            {
                // Méthode 1: Utiliser WMI pour récupérer les mappings
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_MappedLogicalDisk"))
                {
                    foreach (ManagementObject drive in searcher.Get())
                    {
                        string driveLetter = drive["DeviceID"]?.ToString()?.Replace(":", "");
                        string uncPath = drive["ProviderName"]?.ToString();

                        if (!string.IsNullOrEmpty(driveLetter) && !string.IsNullOrEmpty(uncPath))
                        {
                            mappings.Add(new NetworkDriveMapping
                            {
                                DriveLetter = driveLetter,
                                UncPath = uncPath
                            });
                        }
                    }
                }
            }
            catch
            {
                // Méthode alternative si WMI échoue
                var drives = DriveInfo.GetDrives();
                foreach (var drive in drives.Where(d => d.DriveType == DriveType.Network))
                {
                    try
                    {
                        string driveLetter = drive.Name.Replace(":\\", "").Replace(":", "");
                        // Tenter de récupérer le chemin UNC via une autre méthode
                        mappings.Add(new NetworkDriveMapping
                        {
                            DriveLetter = driveLetter,
                            UncPath = drive.Name // Fallback
                        });
                    }
                    catch { }
                }
            }

            return mappings;
        }

        /// <summary>
        /// Trouve un mapping existant pour le chemin UNC donné.
        /// Cherche aussi dans les sous-dossiers (ex: \\serveur\share\folderA\folderB)
        /// </summary>
        private static NetworkDriveMapping FindExistingMapping(string uncPath, List<NetworkDriveMapping> mappedDrives)
        {
            // Normaliser le chemin UNC
            uncPath = uncPath.Replace("/", "\\").TrimEnd('\\');

            // Chercher le mapping le plus spécifique (le plus long qui correspond)
            return mappedDrives
                .Where(m => uncPath.StartsWith(m.UncPath, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(m => m.UncPath.Length)
                .FirstOrDefault();
        }

        /// <summary>
        /// Crée un nouveau mapping de lecteur réseau pour le chemin UNC
        /// </summary>
        private static string MapNewDrive(string uncPath)
        {
            try
            {
                // Obtenir la racine UNC à mapper
                string uncRoot = GetUncRoot(uncPath);

                // Trouver une lettre de lecteur disponible
                string availableDrive = FindAvailableDriveLetter();
                if (string.IsNullOrEmpty(availableDrive))
                {
                    System.Diagnostics.Debug.WriteLine("Aucun lecteur disponible");
                    return null;
                }

                // Utiliser l'API Windows WNetAddConnection2 (fonctionne sans admin)
                try
                {
                    var netResource = new NETRESOURCE
                    {
                        dwType = RESOURCETYPE_DISK,
                        lpLocalName = availableDrive + ":",
                        lpRemoteName = uncRoot,
                        lpProvider = null
                    };

                    int result = WNetAddConnection2(ref netResource, null, null, CONNECT_TEMPORARY);

                    if (result == 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"Lecteur {availableDrive}: mappé vers {uncRoot}");
                        return availableDrive;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Erreur WNetAddConnection2: code {result}");
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erreur mapping: {ex.Message}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur création mapping: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Extrait la racine UNC à mapper (\\serveur\partage\dossier1\dossier2 maximum)
        /// </summary>
        private static string GetUncRoot(string uncPath)
        {
            var parts = uncPath.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
            
            // Minimum: \\serveur\partage
            // Maximum: \\serveur\partage\folder1\folder2 (4 niveaux max pour éviter les chemins trop profonds)
            int maxParts = Math.Min(parts.Length, 4);
            
            return "\\\\" + string.Join("\\", parts.Take(maxParts));
        }

        /// <summary>
        /// Trouve une lettre de lecteur disponible (Z vers A)
        /// </summary>
        private static string FindAvailableDriveLetter()
        {
            var usedDrives = DriveInfo.GetDrives().Select(d => d.Name[0]).ToHashSet();

            // Chercher de Z vers M (éviter A-L souvent utilisés)
            for (char letter = 'Z'; letter >= 'M'; letter--)
            {
                if (!usedDrives.Contains(letter))
                    return letter.ToString();
            }

            return null;
        }

        /// <summary>
        /// Structure pour stocker les informations de mapping
        /// </summary>
        private class NetworkDriveMapping
        {
            public string DriveLetter { get; set; }
            public string UncPath { get; set; }
        }
    }
}

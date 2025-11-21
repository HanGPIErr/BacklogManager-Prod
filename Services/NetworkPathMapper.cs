using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;

namespace BacklogManager.Services
{
    /// <summary>
    /// Service pour mapper automatiquement les chemins UNC en lecteurs réseau
    /// </summary>
    public static class NetworkPathMapper
    {
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

                // Si le mapping a échoué, retourner le chemin original
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
                // Obtenir la racine UNC à mapper (\\serveur\partage ou jusqu'à 2 niveaux de sous-dossiers)
                string uncRoot = GetUncRoot(uncPath);

                // Trouver une lettre de lecteur disponible
                string availableDrive = FindAvailableDriveLetter();
                if (string.IsNullOrEmpty(availableDrive))
                    return null;

                // Utiliser WScript.Network pour mapper le lecteur
                try
                {
                    var networkObject = Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Network"));
                    var method = networkObject.GetType().GetMethod("MapNetworkDrive");
                    method.Invoke(networkObject, new object[] { availableDrive + ":", uncRoot, false });

                    System.Diagnostics.Debug.WriteLine($"Lecteur {availableDrive}: mappé vers {uncRoot}");
                    return availableDrive;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erreur mapping lecteur: {ex.Message}");
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

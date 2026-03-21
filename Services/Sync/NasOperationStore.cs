using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace BacklogManager.Services.Sync
{
    /// <summary>
    /// Gère le stockage des opérations de synchronisation sur le NAS (partage réseau).
    ///
    /// Structure NAS (NasSyncPath) :
    ///   ops/          ← un fichier JSON par opération publiée, nommé par timestamp+client+opId
    ///   snapshots/    ← bases SQLite compactées + hash SHA256
    ///   leases/       ← fichiers de lease temporaires pour la compaction
    ///   manifest.json ← pointe sur le snapshot actif
    ///
    /// Propriétés de sécurité :
    ///   - Atomicité d'écriture : write-then-rename (écriture dans .tmp puis rename)
    ///   - Idempotence : si le fichier existe déjà, l'écriture est skippée
    ///   - Ordre de lecture garanti par tri lexicographique des noms (timestamp en préfixe)
    ///   - Résilience réseau : toutes les opérations NAS sont enveloppées dans des try/catch
    /// </summary>
    public class NasOperationStore
    {
        private readonly string _nasSyncPath;
        private readonly string _opsPath;
        private readonly string _snapshotsPath;
        private readonly string _leasesPath;

        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public string NasSyncPath        => _nasSyncPath;
        public string SnapshotsPath      => _snapshotsPath;
        public string LeasesPath         => _leasesPath;

        public NasOperationStore(string nasSyncPath)
        {
            _nasSyncPath   = nasSyncPath;
            _opsPath       = Path.Combine(nasSyncPath, NasLayout.OpsFolder);
            _snapshotsPath = Path.Combine(nasSyncPath, NasLayout.SnapshotsFolder);
            _leasesPath    = Path.Combine(nasSyncPath, NasLayout.LeasesFolder);
        }

        /// <summary>
        /// Initialise la structure de dossiers sur le NAS.
        /// Appelé au démarrage et lors d'un premier accès.
        /// </summary>
        public bool EnsureDirectoriesExist()
        {
            try
            {
                Directory.CreateDirectory(_opsPath);
                Directory.CreateDirectory(_snapshotsPath);
                Directory.CreateDirectory(_leasesPath);
                return true;
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogWarning($"[NasStore] Impossible de créer les dossiers NAS : {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Vérifie si le NAS est accessible.
        /// </summary>
        public bool IsNasAvailable()
        {
            try
            {
                return Directory.Exists(_opsPath);
            }
            catch
            {
                return false;
            }
        }

        // ─── Publication (PUSH) ──────────────────────────────────────────

        /// <summary>
        /// Publie une liste d'opérations locales sur le NAS.
        /// Atomique par fichier (write to .tmp, puis rename).
        /// Idempotent : si le fichier existe déjà, skippé silencieusement.
        /// </summary>
        /// <returns>Liste des OperationId publiés avec succès.</returns>
        public List<string> Publish(IEnumerable<SyncOperation> operations)
        {
            var published = new List<string>();

            foreach (var op in operations)
            {
                try
                {
                    string fileName  = op.NasFileName;
                    string filePath  = Path.Combine(_opsPath, fileName);
                    string tmpPath   = filePath + ".tmp";

                    // Idempotence : fichier déjà présent = déjà publié
                    if (File.Exists(filePath))
                    {
                        published.Add(op.OperationId);
                        continue;
                    }

                    string json = JsonSerializer.Serialize(op, _jsonOptions);
                    File.WriteAllText(tmpPath, json, Encoding.UTF8);
                    File.Move(tmpPath, filePath);   // atomique sur NTFS

                    published.Add(op.OperationId);
                    LoggingService.Instance.LogInfo($"[NasStore] Publié : {fileName}");
                }
                catch (Exception ex)
                {
                    LoggingService.Instance.LogWarning($"[NasStore] Échec publication {op.OperationId}: {ex.Message}");
                    // On continue les autres opérations
                }
            }

            return published;
        }

        // ─── Récupération (PULL) ─────────────────────────────────────────

        /// <summary>
        /// Récupère toutes les opérations distantes dont le nom de fichier est
        /// STRICTEMENT SUPÉRIEUR à <paramref name="afterNasFileName"/> (curseur).
        ///
        /// En cas de premier pull (cursor vide) et d'un snapshot disponible,
        /// le caller devrait d'abord reconstruire depuis le snapshot (voir SnapshotManager).
        ///
        /// Tri garanti : ordre lexicographique = ordre chronologique (timestamp en préfixe).
        /// </summary>
        public List<SyncOperation> PullSince(string afterNasFileName, string excludeClientId = null)
        {
            var result = new List<SyncOperation>();

            try
            {
                if (!Directory.Exists(_opsPath))
                    return result;

                var files = Directory.GetFiles(_opsPath, "*" + NasLayout.OpExtension);
                Array.Sort(files); // Tri lexicographique = ordre chronologique

                foreach (var file in files)
                {
                    string name = Path.GetFileName(file);

                    // Filtre curseur
                    if (!string.IsNullOrEmpty(afterNasFileName) &&
                        string.Compare(name, afterNasFileName, StringComparison.Ordinal) <= 0)
                        continue;

                    // Filtrer les opérations de notre propre client (déjà dans le journal local)
                    // Utiliser les délimiteurs _ pour éviter les faux positifs (ex: PC1 vs PC10)
                    if (!string.IsNullOrEmpty(excludeClientId) &&
                        name.Contains("_" + SanitizeForFilename(excludeClientId) + "_"))
                        continue;

                    try
                    {
                        string json = File.ReadAllText(file, Encoding.UTF8);
                        var op = JsonSerializer.Deserialize<SyncOperation>(json, _jsonOptions);
                        if (op != null)
                            result.Add(op);
                    }
                    catch (FileNotFoundException)
                    {
                        // Race condition bénigne : le fichier a été supprimé par une compaction
                        // entre le Directory.GetFiles() et le ReadAllText(). Ignorer silencieusement.
                    }
                    catch (Exception ex)
                    {
                        LoggingService.Instance.LogWarning($"[NasStore] Fichier corrompu ignoré : {name} ({ex.Message})");
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogWarning($"[NasStore] Erreur lors du pull : {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Retourne le nom du dernier fichier .syncop présent sur le NAS.
        /// Utilisé pour mettre à jour le curseur local après un pull complet.
        /// </summary>
        public string GetLatestOperationFileName()
        {
            try
            {
                if (!Directory.Exists(_opsPath)) return null;
                var files = Directory.GetFiles(_opsPath, "*" + NasLayout.OpExtension);
                if (files.Length == 0) return null;
                Array.Sort(files);
                return Path.GetFileName(files[files.Length - 1]);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Compte le nombre de fichiers opération sur le NAS (pour décider d'une compaction).
        /// </summary>
        public int CountOperations()
        {
            try
            {
                if (!Directory.Exists(_opsPath)) return 0;
                return Directory.GetFiles(_opsPath, "*" + NasLayout.OpExtension).Length;
            }
            catch
            {
                return 0;
            }
        }

        // ─── Manifest ────────────────────────────────────────────────────

        public SyncManifest ReadManifest()
        {
            string path = Path.Combine(_nasSyncPath, NasLayout.ManifestFile);
            try
            {
                if (!File.Exists(path)) return null;
                string json = File.ReadAllText(path, Encoding.UTF8);
                return JsonSerializer.Deserialize<SyncManifest>(json, _jsonOptions);
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogWarning($"[NasStore] Impossible de lire le manifest : {ex.Message}");
                return null;
            }
        }

        public bool WriteManifest(SyncManifest manifest)
        {
            string path    = Path.Combine(_nasSyncPath, NasLayout.ManifestFile);
            string tmpPath = path + ".tmp";
            try
            {
                string json = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                File.WriteAllText(tmpPath, json, Encoding.UTF8);
                if (File.Exists(path)) File.Delete(path);
                File.Move(tmpPath, path);
                return true;
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogWarning($"[NasStore] Impossible d'écrire le manifest : {ex.Message}");
                return false;
            }
        }

        // ─── Archivage (post-compaction) ─────────────────────────────────

        /// <summary>
        /// Archive (supprime) les fichiers .syncop dont le nom est INFÉRIEUR OU ÉGAL
        /// à <paramref name="upToNasFileName"/> (inclus).
        /// Appelé après une compaction réussie pour maîtriser la taille du dossier ops/.
        /// </summary>
        public int ArchiveOperationsBefore(string upToNasFileName)
        {
            int count = 0;
            try
            {
                var files = Directory.GetFiles(_opsPath, "*" + NasLayout.OpExtension);
                foreach (var file in files)
                {
                    string name = Path.GetFileName(file);
                    if (string.Compare(name, upToNasFileName, StringComparison.Ordinal) <= 0)
                    {
                        try { File.Delete(file); count++; }
                        catch { /* fichier déjà supprimé par un autre client */ }
                    }
                }
                LoggingService.Instance.LogInfo($"[NasStore] Archivage : {count} fichiers ops supprimés.");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogWarning($"[NasStore] Erreur lors de l'archivage : {ex.Message}");
            }
            return count;
        }

        /// <summary>
        /// Supprime TOUS les fichiers .syncop, tous les snapshots et le manifest.
        /// Utilisé après un BackupToNetworkDb réussi : la DB réseau est complète,
        /// les fichiers de sync intermédiaires sont devenus redondants.
        /// </summary>
        public void CleanAllSyncFiles()
        {
            int opsDeleted = 0, snapshotsDeleted = 0;

            // 1. Supprimer tous les .syncop
            try
            {
                if (Directory.Exists(_opsPath))
                {
                    foreach (var file in Directory.GetFiles(_opsPath, "*" + NasLayout.OpExtension))
                    {
                        try { File.Delete(file); opsDeleted++; } catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogWarning($"[NasStore] Erreur nettoyage ops : {ex.Message}");
            }

            // 2. Supprimer tous les snapshots
            try
            {
                if (Directory.Exists(_snapshotsPath))
                {
                    foreach (var file in Directory.GetFiles(_snapshotsPath, "*" + NasLayout.SnapshotExtension))
                    {
                        try { File.Delete(file); snapshotsDeleted++; } catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogWarning($"[NasStore] Erreur nettoyage snapshots : {ex.Message}");
            }

            // 3. Supprimer le manifest (il référence un snapshot qui n'existe plus)
            try
            {
                string manifestPath = Path.Combine(_nasSyncPath, NasLayout.ManifestFile);
                if (File.Exists(manifestPath)) File.Delete(manifestPath);
            }
            catch { }

            // 4. Supprimer les leases stale
            try
            {
                if (Directory.Exists(_leasesPath))
                {
                    foreach (var file in Directory.GetFiles(_leasesPath))
                    {
                        try { File.Delete(file); } catch { }
                    }
                }
            }
            catch { }

            LoggingService.Instance.LogInfo(
                $"[NasStore] Nettoyage complet : {opsDeleted} ops + {snapshotsDeleted} snapshots supprimés.");
        }

        // ─── Helpers ─────────────────────────────────────────────────────

        private static string SanitizeForFilename(string s)
        {
            if (string.IsNullOrEmpty(s)) return "unknown";
            foreach (var c in Path.GetInvalidFileNameChars())
                s = s.Replace(c, '_');
            return s.Length > 32 ? s.Substring(0, 32) : s;
        }
    }
}

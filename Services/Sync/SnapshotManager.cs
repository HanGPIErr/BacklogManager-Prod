using System;
using System.Data.SQLite;
using System.IO;

namespace BacklogManager.Services.Sync
{
    /// <summary>
    /// Gère les snapshots de la DB locale publiés sur le NAS pour permettre :
    ///   a) le rebuild rapide d'un nouveau poste (snapshot + delta récent)
    ///   b) la compaction du dossier ops/ (suppression des anciens fichiers ops)
    ///
    /// Stratégie :
    ///   - Un snapshot = copie à chaud de la DB locale via SQLite BACKUP API
    ///   - Le snapshot est copié sur le NAS dans snapshots/{timestamp}_{clientId}.snapshot
    ///   - Le manifest est mis à jour pour pointer sur ce nouveau snapshot
    ///   - Les anciens fichiers ops (antérieurs au snapshot) sont archivés
    ///
    /// Seuil de compaction : configurable, défaut 500 opérations sur le NAS.
    ///
    /// Protection : utilise LeaseManager pour n'avoir qu'un seul producteur de snapshot.
    /// </summary>
    public class SnapshotManager
    {
        public const int DefaultCompactionThreshold = 500;

        private readonly NasOperationStore  _nasStore;
        private readonly LeaseManager       _leaseManager;
        private readonly string             _localDbPath;
        private readonly string             _clientId;
        private readonly int                _compactionThreshold;

        public SnapshotManager(
            NasOperationStore  nasStore,
            LeaseManager       leaseManager,
            string             localDbPath,
            string             clientId,
            int                compactionThreshold = DefaultCompactionThreshold)
        {
            _nasStore            = nasStore;
            _leaseManager        = leaseManager;
            _localDbPath         = localDbPath;
            _clientId            = clientId;
            _compactionThreshold = compactionThreshold;
        }

        /// <summary>
        /// Vérifie si une compaction est nécessaire (nombre d'ops NAS > seuil).
        /// </summary>
        public bool IsCompactionNeeded()
        {
            return _nasStore.CountOperations() >= _compactionThreshold;
        }

        /// <summary>
        /// Tente une compaction complète :
        ///   1. Acquiert le lease NAS
        ///   2. Crée un snapshot BACKUP de la DB locale
        ///   3. Copie le snapshot sur le NAS
        ///   4. Met à jour le manifest
        ///   5. Archive les anciens ops
        ///   6. Libère le lease
        ///
        /// Sûr d'appeler même si la compaction n'est pas nécessaire (IsCompactionNeeded() est vérifié en interne).
        /// </summary>
        /// <returns>True si la compaction a réussi.</returns>
        public bool TryCompact()
        {
            if (!IsCompactionNeeded())
                return false;

            if (!_leaseManager.TryAcquireCompactionLease())
            {
                LoggingService.Instance.LogInfo("[SnapshotManager] Compaction en cours par un autre client - ignorée.");
                return false;
            }

            try
            {
                LoggingService.Instance.LogInfo("[SnapshotManager] Démarrage compaction...");

                // 1. Capturer le nom du dernier op AVANT le snapshot (pour le manifest)
                string lastOpBefore = _nasStore.GetLatestOperationFileName();

                // 2. Nom du snapshot
                string snapshotName =
                    $"{DateTime.UtcNow:yyyyMMddHHmmss}_{SanitizeForFilename(_clientId)}{NasLayout.SnapshotExtension}";
                string snapshotPath = Path.Combine(_nasStore.SnapshotsPath, snapshotName);
                string snapshotTmp  = snapshotPath + ".tmp";

                // 3. SQLite Online Backup API (copie à chaud sans lock UI)
                _leaseManager.RenewLease();
                CreateSQLiteBackup(_localDbPath, snapshotTmp);

                // 4. Rename atomique
                if (File.Exists(snapshotPath)) File.Delete(snapshotPath);
                File.Move(snapshotTmp, snapshotPath);
                _leaseManager.RenewLease();

                LoggingService.Instance.LogInfo($"[SnapshotManager] Snapshot créé : {snapshotName}");

                // 5. Mettre à jour le manifest
                // OpsAfterSnapshot = premier op dont on devra rejouer APRÈS le snapshot
                // = nom lexicographiquement immédiatement supérieur au dernier op avant snapshot.
                // On utilise lastOpBefore + "~" (tilde > tout caractère ASCII standard).
                string opsAfter = lastOpBefore == null ? "" : lastOpBefore + "~";
                int opCount = _nasStore.CountOperations();

                var manifest = new SyncManifest
                {
                    SnapshotFileName          = snapshotName,
                    SnapshotTimestampUtc      = DateTime.UtcNow,
                    OpsAfterSnapshot          = opsAfter,
                    CreatedByClientId         = _clientId,
                    OperationCountAtSnapshot  = opCount
                };
                _nasStore.WriteManifest(manifest);
                _leaseManager.RenewLease();

                // 6. Archiver les anciens ops (ceux inclus dans le snapshot)
                if (!string.IsNullOrEmpty(lastOpBefore))
                {
                    int archived = _nasStore.ArchiveOperationsBefore(lastOpBefore);
                    LoggingService.Instance.LogInfo($"[SnapshotManager] {archived} ops archivés.");
                }

                LoggingService.Instance.LogInfo("[SnapshotManager] Compaction terminée avec succès.");
                return true;
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("[SnapshotManager] Erreur durant la compaction", ex);
                return false;
            }
            finally
            {
                _leaseManager.ReleaseLease();
            }
        }

        /// <summary>
        /// Reconstruit la DB locale à partir du dernier snapshot NAS + replay des ops récents.
        /// Utilisé pour :
        ///   a) initialiser un nouveau poste
        ///   b) restaurer une DB locale corrompue
        ///
        /// IMPORTANT : les données locales courantes sont écrasées.
        /// C'est une opération de REBUILD - à n'utiliser qu'explicitement.
        /// </summary>
        public bool TryRebuildFromSnapshot(LocalDatabaseFactory localDb)
        {
            try
            {
                var manifest = _nasStore.ReadManifest();
                if (manifest == null)
                {
                    LoggingService.Instance.LogInfo("[SnapshotManager] Pas de snapshot disponible pour rebuild.");
                    return false;
                }

                string snapshotPath = Path.Combine(_nasStore.SnapshotsPath, manifest.SnapshotFileName);
                if (!File.Exists(snapshotPath))
                {
                    LoggingService.Instance.LogWarning($"[SnapshotManager] Fichier snapshot manquant : {manifest.SnapshotFileName}");
                    return false;
                }

                // Copier le snapshot vers la DB locale (remplace l'existant)
                string localPath = localDb.LocalDbPath;
                string backupOld = localPath + ".bak";

                if (File.Exists(localPath))
                {
                    if (File.Exists(backupOld)) File.Delete(backupOld);
                    File.Copy(localPath, backupOld);
                }

                File.Copy(snapshotPath, localPath, overwrite: true);

                // Supprimer le WAL obsolète s'il existe
                string walPath = localPath + "-wal";
                string shmPath = localPath + "-shm";
                if (File.Exists(walPath)) File.Delete(walPath);
                if (File.Exists(shmPath)) File.Delete(shmPath);

                // Remettre les sync tables (le snapshot peut ne pas les avoir)
                localDb.GetOrCreateLocalDb();

                LoggingService.Instance.LogInfo(
                    $"[SnapshotManager] DB locale reconstruite depuis snapshot {manifest.SnapshotFileName}");

                // Retourner le curseur ops (l'appelant devra puller et rejouer depuis manifest.OpsAfterSnapshot)
                localDb.SetSyncState("NasCursor", manifest.OpsAfterSnapshot ?? "");

                return true;
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("[SnapshotManager] Erreur rebuild depuis snapshot", ex);
                return false;
            }
        }

        // ─── Helpers ─────────────────────────────────────────────────────

        /// <summary>
        /// Crée une copie à chaud de la DB SQLite via l'API Online Backup.
        /// Non-bloquant pour les lectures sur la DB source.
        /// </summary>
        private void CreateSQLiteBackup(string sourcePath, string destinationPath)
        {
            // Ouvrir la source en lecture seule
            var srcCsb = new SQLiteConnectionStringBuilder
            {
                DataSource  = sourcePath,
                Version     = 3,
                ReadOnly    = true,
                Pooling     = false
            };
            using (var srcConn = new SQLiteConnection(srcCsb.ConnectionString))
            {
                srcConn.Open();

                // Créer la destination
                SQLiteConnection.CreateFile(destinationPath);
                var dstCsb = new SQLiteConnectionStringBuilder
                {
                    DataSource = destinationPath,
                    Version    = 3,
                    Pooling    = false
                };
                using (var dstConn = new SQLiteConnection(dstCsb.ConnectionString))
                {
                    dstConn.Open();
                    srcConn.BackupDatabase(dstConn, "main", "main", -1, null, 0);
                }
            }
        }

        private static string SanitizeForFilename(string s)
        {
            if (string.IsNullOrEmpty(s)) return "unknown";
            foreach (var c in System.IO.Path.GetInvalidFileNameChars())
                s = s.Replace(c, '_');
            return s.Length > 24 ? s.Substring(0, 24) : s;
        }
    }
}

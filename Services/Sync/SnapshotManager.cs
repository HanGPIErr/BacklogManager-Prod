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
        public const int DefaultCompactionThreshold = 50;

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
        /// Crée un snapshot initial sur le NAS (sans lease ni archivage, sans condition de seuil).
        /// Utilisé au premier lancement pour permettre aux autres clients de bootstrapper.
        /// Si un manifest existe déjà, ne fait rien.
        /// </summary>
        public bool ForceInitialSnapshot(LocalDatabaseFactory localDb)
        {
            try
            {
                // Ne pas écraser un snapshot existant
                var existing = _nasStore.ReadManifest();
                if (existing != null) return false;

                if (!_nasStore.EnsureDirectoriesExist()) return false;

                string snapshotName =
                    $"{DateTime.UtcNow:yyyyMMddHHmmss}_{SanitizeForFilename(_clientId)}_init{NasLayout.SnapshotExtension}";
                string snapshotPath = System.IO.Path.Combine(_nasStore.SnapshotsPath, snapshotName);
                string snapshotTmp  = snapshotPath + ".tmp";

                CreateSQLiteBackup(_localDbPath, snapshotTmp);

                if (System.IO.File.Exists(snapshotPath)) System.IO.File.Delete(snapshotPath);
                System.IO.File.Move(snapshotTmp, snapshotPath);

                var manifest = new SyncManifest
                {
                    SnapshotFileName         = snapshotName,
                    SnapshotTimestampUtc     = DateTime.UtcNow,
                    OpsAfterSnapshot         = "",     // Tous les ops futurs devront être rejoués
                    CreatedByClientId        = _clientId,
                    OperationCountAtSnapshot = 0
                };
                _nasStore.WriteManifest(manifest);

                LoggingService.Instance.LogInfo($"[SnapshotManager] Snapshot initial créé : {snapshotName}");
                return true;
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogWarning(
                    $"[SnapshotManager] Impossible de créer le snapshot initial : {ex.Message}");
                return false;
            }
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
            return DoCompact();
        }

        /// <summary>
        /// Force une compaction sans vérifier le seuil.
        /// Utilisé à la fermeture de l'application pour nettoyer les .syncop.
        /// </summary>
        public bool ForceCompaction()
        {
            if (_nasStore.CountOperations() == 0)
                return false;
            return DoCompact();
        }

        private bool DoCompact()
        {

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

                // 7. Supprimer les anciens snapshots (garder seulement le nouveau)
                CleanOldSnapshots(snapshotName);

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
                // Sécurité : ne JAMAIS écraser la DB locale si des opérations
                // locales n'ont pas encore été publiées sur le NAS (sinon elles seraient perdues).
                var unpublished = localDb.GetUnpublishedOperations();
                if (unpublished.Count > 0)
                {
                    LoggingService.Instance.LogWarning(
                        $"[SnapshotManager] Rebuild refusé : {unpublished.Count} op(s) locale(s) non publiée(s). " +
                        "Le push doit réussir avant de reconstruire depuis le snapshot.");
                    return false;
                }

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

                // Le snapshot NAS peut être sur un chemin UNC. SQLite ne supporte pas
                // correctement les chemins UNC, donc on copie d'abord vers un fichier
                // local temporaire puis on utilise le BACKUP API.
                string localPath = localDb.LocalDbPath;
                string localTmp = localPath + ".snap_tmp";
                try
                {
                    File.Copy(snapshotPath, localTmp, overwrite: true);
                }
                catch (Exception exCopy)
                {
                    LoggingService.Instance.LogWarning(
                        $"[SnapshotManager] Impossible de copier le snapshot vers le fichier temporaire : {exCopy.Message}");
                    return false;
                }

                try
                {
                    // Utiliser le BACKUP API de SQLite pour remplacer le contenu de la DB
                    // locale par celui du snapshot, SANS remplacer le fichier sous les
                    // connexions ouvertes (ce qui causait "database disk image is malformed").
                    var srcCsb = new SQLiteConnectionStringBuilder
                    {
                        DataSource  = localTmp,
                        Version     = 3,
                        ReadOnly    = true,
                        Pooling     = false
                    };
                    var dstCsb = new SQLiteConnectionStringBuilder
                    {
                        DataSource  = localPath,
                        Version     = 3,
                        Pooling     = false,
                        JournalMode = SQLiteJournalModeEnum.Wal
                    };

                    using (var srcConn = new SQLiteConnection(srcCsb.ConnectionString))
                    {
                        srcConn.Open();
                        using (var dstConn = new SQLiteConnection(dstCsb.ConnectionString))
                        {
                            dstConn.Open();
                            srcConn.BackupDatabase(dstConn, "main", "main", -1, null, 0);
                        }
                    }
                }
                finally
                {
                    try { if (File.Exists(localTmp)) File.Delete(localTmp); } catch { }
                }

                // Invalider les connexions poolées pour qu'elles voient le nouveau contenu
                SQLiteConnection.ClearAllPools();

                // Remettre les sync tables (le snapshot peut ne pas les avoir)
                localDb.GetOrCreateLocalDb();

                // Peupler SyncEntityOrigin pour les entités du créateur du snapshot.
                // Sans cela, les futures ops de mise à jour du créateur seraient vues
                // comme des collisions d'Id (car l'entité existe localement mais n'est
                // pas encore connue dans SyncEntityOrigin pour ce client).
                PopulateSyncEntityOriginAfterRebuild(localDb, manifest.CreatedByClientId);

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

        /// <summary>
        /// Après un rebuild depuis snapshot, enregistre toutes les entités existantes
        /// qui ne sont pas encore dans SyncEntityOrigin comme appartenant au créateur
        /// du snapshot. Évite les faux positifs de collision lors des mises à jour suivantes.
        /// </summary>
        private void PopulateSyncEntityOriginAfterRebuild(LocalDatabaseFactory localDb, string snapshotCreatorClientId)
        {
            if (string.IsNullOrEmpty(snapshotCreatorClientId)) return;

            string[] dataTables = {
                "Utilisateurs", "Devs", "Projets", "Sprints", "Demandes", "BacklogItems",
                "Commentaires", "CRA", "Disponibilites", "Roles", "Equipes", "Programmes",
                "Notifications", "PlanningVM", "DemandeEchangeVM", "PokerSessions", "PokerVotes"
            };

            try
            {
                using (var conn = localDb.OpenWriteConnection())
                using (var tx = conn.BeginTransaction())
                {
                    foreach (var table in dataTables)
                    {
                        try
                        {
                            using (var cmd = conn.CreateCommand())
                            {
                                cmd.Transaction = tx;
                                // Insérer uniquement pour les entités PAS déjà trackées dans SyncEntityOrigin
                                cmd.CommandText = $@"
                                    INSERT OR IGNORE INTO SyncEntityOrigin 
                                        (TableName, RemoteEntityId, OriginClientId, LocalEntityId)
                                    SELECT @tbl, Id, @client, Id FROM [{table}]
                                    WHERE NOT EXISTS (
                                        SELECT 1 FROM SyncEntityOrigin 
                                        WHERE TableName = @tbl AND LocalEntityId = [{table}].Id
                                    )";
                                cmd.Parameters.AddWithValue("@tbl", table);
                                cmd.Parameters.AddWithValue("@client", snapshotCreatorClientId);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        catch
                        {
                            // Table n'existe peut-être pas encore — ignorer silencieusement
                        }
                    }
                    tx.Commit();
                }

                LoggingService.Instance.LogInfo(
                    $"[SnapshotManager] SyncEntityOrigin peuplé après rebuild (creator={snapshotCreatorClientId}).");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogWarning(
                    $"[SnapshotManager] Erreur peuplement SyncEntityOrigin après rebuild : {ex.Message}");
            }
        }

        // ─── Nettoyage snapshots ─────────────────────────────────────────

        /// <summary>
        /// Supprime tous les anciens fichiers snapshot du NAS sauf celui en cours.
        /// </summary>
        private void CleanOldSnapshots(string keepSnapshotName)
        {
            try
            {
                string snapshotsDir = _nasStore.SnapshotsPath;
                if (!Directory.Exists(snapshotsDir)) return;

                var files = Directory.GetFiles(snapshotsDir, "*" + NasLayout.SnapshotExtension);
                int deleted = 0;
                foreach (var file in files)
                {
                    string name = Path.GetFileName(file);
                    if (string.Equals(name, keepSnapshotName, StringComparison.OrdinalIgnoreCase))
                        continue; // garder le snapshot courant
                    try { File.Delete(file); deleted++; }
                    catch { /* déjà supprimé ou verrouillé par un autre client */ }
                }
                if (deleted > 0)
                    LoggingService.Instance.LogInfo($"[SnapshotManager] {deleted} ancien(s) snapshot(s) supprimé(s).");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogWarning($"[SnapshotManager] Erreur nettoyage snapshots : {ex.Message}");
            }
        }

        // ─── Helpers ─────────────────────────────────────────────────────

        /// <summary>
        /// Crée une copie à chaud de la DB SQLite via l'API Online Backup.
        /// Non-bloquant pour les lectures sur la DB source.
        /// Le backup s'effectue d'abord vers un fichier local temporaire puis est copié vers
        /// la destination (qui peut être sur le réseau / NAS). SQLite ne supporte pas
        /// correctement les chemins UNC (mode WAL, lock files), d'où ce transit local.
        /// </summary>
        private void CreateSQLiteBackup(string sourcePath, string destinationPath)
        {
            // Étape 1 : Backup SQLite → fichier local temporaire
            string localTmp = Path.Combine(
                Path.GetDirectoryName(sourcePath),
                "snapshot_" + Guid.NewGuid().ToString("N").Substring(0, 8) + ".tmp");

            try
            {
                var srcCsb = new SQLiteConnectionStringBuilder
                {
                    DataSource  = sourcePath,
                    Version     = 3,
                    ReadOnly    = true,
                    Pooling     = false
                };
                var dstCsb = new SQLiteConnectionStringBuilder
                {
                    DataSource  = localTmp,
                    Version     = 3,
                    Pooling     = false,
                    JournalMode = SQLiteJournalModeEnum.Delete   // Pas de WAL pour un fichier temporaire
                };

                SQLiteConnection.CreateFile(localTmp);
                using (var srcConn = new SQLiteConnection(srcCsb.ConnectionString))
                {
                    srcConn.Open();
                    using (var dstConn = new SQLiteConnection(dstCsb.ConnectionString))
                    {
                        dstConn.Open();
                        srcConn.BackupDatabase(dstConn, "main", "main", -1, null, 0);
                    }
                }

                // Étape 2 : Copier le fichier local vers la destination réseau
                File.Copy(localTmp, destinationPath, overwrite: true);
            }
            finally
            {
                // Nettoyage du fichier temporaire local
                try { if (File.Exists(localTmp)) File.Delete(localTmp); } catch { }
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

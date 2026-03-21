using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Threading;

namespace BacklogManager.Services.Sync
{
    /// <summary>
    /// Moteur de synchronisation en arrière-plan.
    ///
    /// Boucle de synchronisation (toutes les <see cref="SyncIntervalMs"/> ms) :
    ///   1. Publier sur le NAS les opérations locales non encore publiées
    ///   2. Tirer depuis le NAS les opérations distantes dont le nom de fichier > curseur local
    ///   3. Appliquer chaque opération distante dans la DB locale (via SyncApplier)
    ///   4. Mettre à jour le curseur NAS dans SyncState
    ///   5. Vérifier si une compaction est nécessaire (seuil configurable)
    ///
    /// Thread-safety : la boucle tourne sur un Thread dédié (non-WPF), toutes les
    /// lectures/écritures passent par LocalDatabaseFactory qui gère un verrou interne.
    /// Le WPF peut interroger <see cref="NasAvailable"/> à tout moment sans risque.
    /// </summary>
    public class SyncEngine : IDisposable
    {
        // ─── Dépendances ─────────────────────────────────────────────────────
        private readonly LocalDatabaseFactory _localDb;
        private readonly NasOperationStore    _nasStore;
        private readonly SyncApplier          _applier;
        private readonly SnapshotManager      _snapshotMgr;
        private readonly string               _clientId;

        // ─── Configuration ───────────────────────────────────────────────────
        public int SyncIntervalMs { get; set; } = 15_000;   // 15 secondes

        /// <summary>Nombre de cycles normaux entre deux réconciliations complètes (0 = désactivé).</summary>
        public int ReconciliationEveryNCycles { get; set; } = 40;  // ~10 min à 15s/cycle

        // ─── État interne ─────────────────────────────────────────────────────
        private Thread      _thread;
        private bool        _running;
        private bool        _disposed;
        private int         _cyclesSinceReconciliation;
        private bool        _startupReconciliationDone;
        private readonly ManualResetEventSlim _pushSignal = new ManualResetEventSlim(false);
        private readonly object _reconcileLock = new object();

        // La clé dans SyncState pour le curseur NAS (dernier fichier .syncop vu)
        private const string NasCursorKey = "NasCursor";

        // ─── Observable pour la couche UI ────────────────────────────────────
        private bool _nasAvailable;
        public bool  NasAvailable => _nasAvailable;

        /// <summary>Déclenché à chaque fois que NasAvailable change de valeur.</summary>
        public event EventHandler<bool> NasAvailabilityChanged;

        /// <summary>Déclenché après chaque cycle de synchro complet (même vide).</summary>
        public event EventHandler SyncCycleCompleted;

        // ─── Compteurs (diagnostics) ─────────────────────────────────────────
        public int  TotalPublished  { get; private set; }
        public int  TotalApplied    { get; private set; }
        public DateTime LastSyncUtc { get; private set; }

        // ─── Ctor ─────────────────────────────────────────────────────────────
        public SyncEngine(
            LocalDatabaseFactory localDb,
            NasOperationStore    nasStore,
            SyncApplier          applier,
            SnapshotManager      snapshotMgr,
            string               clientId)
        {
            _localDb     = localDb     ?? throw new ArgumentNullException(nameof(localDb));
            _nasStore    = nasStore    ?? throw new ArgumentNullException(nameof(nasStore));
            _applier     = applier     ?? throw new ArgumentNullException(nameof(applier));
            _snapshotMgr = snapshotMgr ?? throw new ArgumentNullException(nameof(snapshotMgr));
            _clientId    = clientId    ?? throw new ArgumentNullException(nameof(clientId));
        }

        // ─── Cycle de vie ────────────────────────────────────────────────────
        public void Start()
        {
            if (_running) return;
            _running = true;

            _thread = new Thread(SyncLoop)
            {
                Name         = "BacklogManager.SyncEngine",
                IsBackground = true,   // n'empêche pas l'arrêt du processus
                Priority     = ThreadPriority.BelowNormal
            };
            _thread.Start();
            LoggingService.Instance.LogInfo($"[SyncEngine] Démarré (clientId={_clientId}, intervalle={SyncIntervalMs}ms)");
        }

        public void Stop()
        {
            _running = false;
            _pushSignal.Set(); // Réveiller la boucle pour qu'elle puisse sortir
            if (_thread != null && _thread.IsAlive)
            {
                _thread.Join(5000);
            }
        }

        // ─── Boucle principale ───────────────────────────────────────────────
        private void SyncLoop()
        {
            while (_running)
            {
                try
                {
                    RunOneCycle();
                }
                catch (Exception ex)
                {
                    // Erreur non gérée dans le cycle : log et Continue — ne pas laisser le thread mourir
                    LoggingService.Instance.LogError("[SyncEngine] Erreur inattendue dans la boucle de synchro", ex);
                }

                // Attendre le prochain cycle — mais se réveiller immédiatement si un push est signalé
                _pushSignal.Reset();
                _pushSignal.Wait(SyncIntervalMs);
            }

            LoggingService.Instance.LogInfo("[SyncEngine] Thread de synchronisation arrêté.");
        }

        /// <summary>
        /// Signale au moteur de sync qu'une nouvelle opération vient d'être journalisée.
        /// Le push sera déclenché quasi-immédiatement au lieu d'attendre le prochain cycle.
        /// Thread-safe — peut être appelé depuis n'importe quel thread.
        /// </summary>
        public void NotifyNewOperation()
        {
            _pushSignal.Set();
        }

        // ─── Un cycle de synchro ─────────────────────────────────────────────
        private void RunOneCycle()
        {
            // ── 0. Vérifier la disponibilité du NAS ──────────────────────────
            bool nasNow = _nasStore.IsNasAvailable();
            if (nasNow != _nasAvailable)
            {
                _nasAvailable = nasNow;
                NasAvailabilityChanged?.Invoke(this, _nasAvailable);
                LoggingService.Instance.LogInfo(
                    $"[SyncEngine] NAS {(_nasAvailable ? "disponible" : "INDISPONIBLE")}");
            }

            if (!_nasAvailable)
            {
                // Pas de NAS → local-only silencieux
                LastSyncUtc = DateTime.UtcNow;
                SyncCycleCompleted?.Invoke(this, EventArgs.Empty);
                return;
            }

            // ── 0b. Réconciliation au premier cycle après démarrage ─────────
            if (!_startupReconciliationDone)
            {
                _startupReconciliationDone = true;
                LoggingService.Instance.LogInfo("[SyncEngine] Réconciliation de démarrage...");
                FullReconciliation();
                LastSyncUtc = DateTime.UtcNow;
                SyncCycleCompleted?.Invoke(this, EventArgs.Empty);
                return; // Le premier cycle est dédié à la réconciliation
            }

            // ── 1. S'assurer qu'un snapshot initial existe sur le NAS ─────
            EnsureInitialSnapshot();

            // ── 2. Publier les opérations locales non publiées ───────────────
            PushUnpublished();

            // ── 3. Tirer & appliquer les opérations distantes ────────────────
            PullAndApply();

            // ── 4. Compaction si nécessaire ──────────────────────────────────
            CheckCompaction();

            // ── 5. Réconciliation périodique (toutes les N cycles) ───────────
            _cyclesSinceReconciliation++;
            if (ReconciliationEveryNCycles > 0 &&
                _cyclesSinceReconciliation >= ReconciliationEveryNCycles)
            {
                _cyclesSinceReconciliation = 0;
                LoggingService.Instance.LogInfo("[SyncEngine] Réconciliation périodique...");
                FullReconciliation();
            }

            LastSyncUtc = DateTime.UtcNow;
            SyncCycleCompleted?.Invoke(this, EventArgs.Empty);
        }

        // ─── Push ────────────────────────────────────────────────────────────
        private void PushUnpublished()
        {
            try
            {
                var unpublished = _localDb.GetUnpublishedOperations();
                if (unpublished.Count == 0) return;

                var published = _nasStore.Publish(unpublished);
                if (published.Count > 0)
                {
                    _localDb.MarkPublished(published);
                    TotalPublished += published.Count;
                    LoggingService.Instance.LogInfo(
                        $"[SyncEngine] {published.Count} opération(s) publiée(s) sur le NAS.");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogWarning(
                    $"[SyncEngine] Erreur lors du push : {ex.Message}");
            }
        }

        // ─── Snapshot initial ────────────────────────────────────────────────
        /// <summary>
        /// Vérifie si un snapshot/manifest existe sur le NAS. Sinon, en crée un
        /// à partir de la DB locale pour les autres clients.
        /// </summary>
        private void EnsureInitialSnapshot()
        {
            try
            {
                var manifest = _nasStore.ReadManifest();
                if (manifest != null) return; // Un snapshot existe déjà

                // Pas de manifest → créer un snapshot initial
                bool created = _snapshotMgr.ForceInitialSnapshot(_localDb);
                if (created)
                    LoggingService.Instance.LogInfo("[SyncEngine] Snapshot initial créé sur le NAS.");
            }
            catch (Exception ex)
            {
                // Non critique : sera réessayé au prochain cycle
                LoggingService.Instance.LogWarning(
                    $"[SyncEngine] Impossible de créer le snapshot initial : {ex.Message}");
            }
        }

        // ─── Pull + Apply ────────────────────────────────────────────────────
        private void PullAndApply()
        {
            try
            {
                string cursor = _localDb.GetSyncState(NasCursorKey);   // null ou dernier fichier vu

                // ── Détection nouveau client ou curseur périmé (post-compaction) ──
                if (string.IsNullOrEmpty(cursor))
                {
                    // Premier démarrage : tenter de récupérer le snapshot NAS
                    if (_snapshotMgr.TryRebuildFromSnapshot(_localDb))
                    {
                        LoggingService.Instance.LogInfo("[SyncEngine] DB locale reconstruite depuis snapshot (premier démarrage).");
                        cursor = _localDb.GetSyncState(NasCursorKey);
                    }
                }

                var remoteOps = _nasStore.PullSince(cursor, _clientId);

                // Détection de gap : le curseur pointe un ancien fichier supprimé par compaction
                // mais il existe des ops plus récentes qu'on ne voit pas (elles sont > cursor,
                // mais cursor lui-même et les précédentes ont été archivées).
                if (remoteOps.Count == 0 && !string.IsNullOrEmpty(cursor))
                {
                    string latestOnNas = _nasStore.GetLatestOperationFileName();
                    if (latestOnNas != null && string.Compare(latestOnNas, cursor, System.StringComparison.Ordinal) > 0)
                    {
                        // Il existe des ops plus récentes, mais PullSince n'en a trouvé aucune
                        // → le curseur est dans une zone compactée. Rebuild nécessaire.
                        LoggingService.Instance.LogWarning("[SyncEngine] Gap détecté (cursor périmé après compaction), rebuild depuis snapshot...");
                        if (_snapshotMgr.TryRebuildFromSnapshot(_localDb))
                        {
                            cursor = _localDb.GetSyncState(NasCursorKey);
                            remoteOps = _nasStore.PullSince(cursor, _clientId);
                        }
                    }
                }

                if (remoteOps.Count == 0) return;

                // Appliquer par petits lots pour ne pas bloquer les écritures UI trop longtemps
                const int BatchSize = 50;
                for (int i = 0; i < remoteOps.Count; i += BatchSize)
                {
                    int end = Math.Min(i + BatchSize, remoteOps.Count);
                    using (var conn = _localDb.OpenWriteConnection())
                    {
                        // OpenWriteConnection() ouvre déjà la connexion — ne pas rappeler conn.Open()
                        using (var tx = conn.BeginTransaction())
                        {
                            for (int j = i; j < end; j++)
                            {
                                _applier.Apply(conn, tx, remoteOps[j]);
                            }
                            tx.Commit();
                        }
                    }
                }

                // Avancer le curseur au nom du dernier fichier traité
                string lastFile = remoteOps[remoteOps.Count - 1].NasFileName;
                _localDb.SetSyncState(NasCursorKey, lastFile);

                TotalApplied += remoteOps.Count;
                LoggingService.Instance.LogInfo(
                    $"[SyncEngine] {remoteOps.Count} opération(s) distante(s) appliquée(s). Curseur → {lastFile}");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogWarning(
                    $"[SyncEngine] Erreur lors du pull/apply : {ex.Message}");
            }
        }

        // ─── Compaction ──────────────────────────────────────────────────────
        private void CheckCompaction()
        {
            try
            {
                if (_snapshotMgr.IsCompactionNeeded())
                {
                    LoggingService.Instance.LogInfo("[SyncEngine] Seuil de compaction atteint, tentative...");
                    _snapshotMgr.TryCompact();
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogWarning(
                    $"[SyncEngine] Erreur lors de la vérification de compaction : {ex.Message}");
            }
        }

        // ─── Forcer un cycle immédiat (depuis l'UI) ──────────────────────────
        /// <summary>
        /// Déclenche un cycle de synchronisation immédiat (sur le thread appelant).
        /// À appeler depuis un Task / BackgroundWorker dans les ViewModels si nécessaire.
        /// </summary>
        public void ForceSyncNow()
        {
            try { RunOneCycle(); }
            catch (Exception ex)
            {
                LoggingService.Instance.LogWarning($"[SyncEngine] ForceSyncNow : {ex.Message}");
            }
        }

        // ─── Réconciliation complète ─────────────────────────────────────────
        /// <summary>
        /// Réconciliation complète depuis le NAS :
        ///   1. Push toutes les ops locales non publiées
        ///   2. Pull TOUTES les ops NAS (ignore le curseur)
        ///   3. Applique-les (SyncApplied = idempotent, pas de double-apply)
        ///   4. Met à jour le curseur
        ///
        /// Contrairement au rebuild (qui écrase la DB locale), la réconciliation
        /// AJOUTE les données manquantes sans rien supprimer.
        /// Sûr d'appeler à tout moment, même pendant la boucle de sync.
        /// </summary>
        public void FullReconciliation()
        {
            // Empêcher l'exécution concurrente (UI + thread sync)
            if (!System.Threading.Monitor.TryEnter(_reconcileLock))
            {
                LoggingService.Instance.LogInfo("[SyncEngine:Reconcile] Réconciliation déjà en cours, ignorée.");
                return;
            }
            try
            {
                // ── 1. Push d'abord : ne pas perdre les ops locales ─────────
                PushUnpublished();

                // ── 2. Pull TOUTES les ops NAS (curseur vide) ───────────────
                var allRemoteOps = _nasStore.PullSince("", _clientId);
                if (allRemoteOps.Count == 0)
                {
                    LoggingService.Instance.LogInfo("[SyncEngine:Reconcile] Aucune opération distante sur le NAS.");
                    return;
                }

                // ── 3. Appliquer par lots (SyncApplied = idempotent) ────────
                int applied = 0;
                const int BatchSize = 50;
                for (int i = 0; i < allRemoteOps.Count; i += BatchSize)
                {
                    int end = Math.Min(i + BatchSize, allRemoteOps.Count);
                    using (var conn = _localDb.OpenWriteConnection())
                    {
                        using (var tx = conn.BeginTransaction())
                        {
                            for (int j = i; j < end; j++)
                            {
                                _applier.Apply(conn, tx, allRemoteOps[j]);
                            }
                            tx.Commit();
                        }
                    }
                    applied += (end - i);
                }

                // ── 4. Mettre à jour le curseur au dernier fichier NAS ──────
                string lastFile = allRemoteOps[allRemoteOps.Count - 1].NasFileName;
                string currentCursor = _localDb.GetSyncState(NasCursorKey);

                // Ne reculer jamais le curseur (lastFile pourrait être plus ancien si
                // des ops locales récentes ont été publiées entre-temps)
                if (string.IsNullOrEmpty(currentCursor) ||
                    string.Compare(lastFile, currentCursor, StringComparison.Ordinal) > 0)
                {
                    _localDb.SetSyncState(NasCursorKey, lastFile);
                }

                TotalApplied += applied;
                LoggingService.Instance.LogInfo(
                    $"[SyncEngine:Reconcile] Réconciliation terminée. {allRemoteOps.Count} ops traitées, curseur → {lastFile}");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogWarning(
                    $"[SyncEngine:Reconcile] Erreur réconciliation : {ex.Message}");
            }
            finally
            {
                System.Threading.Monitor.Exit(_reconcileLock);
            }
        }

        /// <summary>
        /// Force une réconciliation complète (accessible depuis l'UI).
        /// Pousse les ops locales, puis relecture de TOUTES les ops NAS.
        /// Sûr d'appeler à tout moment — idempotent.
        /// </summary>
        public void ForceFullReconciliation()
        {
            try { FullReconciliation(); }
            catch (Exception ex)
            {
                LoggingService.Instance.LogWarning($"[SyncEngine] ForceFullReconciliation : {ex.Message}");
            }
        }

        // ─── Arrêt propre avec sync finale ────────────────────────────────────
        /// <summary>
        /// Arrêt propre de la synchronisation à la fermeture de l'application :
        ///   1. Stoppe la boucle de sync
        ///   2. Pousse les dernières ops locales non publiées sur le NAS
        ///   3. Réconciliation finale (pull all + apply)
        ///   4. Copie la DB locale vers la DB réseau (BACKUP API) si elle est accessible en écriture
        ///   5. Crée un snapshot NAS à jour + nettoie les anciens fichiers .syncop
        ///
        /// Si la DB réseau n'est pas accessible, seules les étapes 1-3 sont exécutées
        /// (les données restent dans local.db + NAS .syncop).
        /// </summary>
        public void GracefulShutdown(string networkDbPath = null)
        {
            LoggingService.Instance.LogInfo("[SyncEngine] Arrêt propre en cours...");

            // 1. Stopper la boucle
            Stop();
            LoggingService.Instance.LogInfo("[SyncEngine] Boucle de sync arrêtée.");

            // 2. Vérifier si le NAS est dispo pour les étapes suivantes
            if (!_nasStore.IsNasAvailable())
            {
                LoggingService.Instance.LogInfo("[SyncEngine] NAS indisponible — arrêt sans sync finale.");
                return;
            }
            LoggingService.Instance.LogInfo("[SyncEngine] NAS disponible, sync finale...");

            try
            {
                // 3. Push final : envoyer les ops locales restantes
                PushUnpublished();
                LoggingService.Instance.LogInfo("[SyncEngine] Push final terminé.");

                // Vérifier que toutes les ops ont bien été publiées
                var remaining = _localDb.GetUnpublishedOperations();
                if (remaining.Count > 0)
                {
                    LoggingService.Instance.LogWarning(
                        $"[SyncEngine] {remaining.Count} op(s) restent non publiées après push final — 2e tentative...");
                    PushUnpublished();
                    remaining = _localDb.GetUnpublishedOperations();
                    if (remaining.Count > 0)
                        LoggingService.Instance.LogWarning(
                            $"[SyncEngine] {remaining.Count} op(s) toujours non publiées (seront synchronisées au prochain démarrage).");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogWarning(
                    $"[SyncEngine] Erreur push final : {ex.Message}");
            }

            try
            {
                // 4. Réconciliation finale : pull ALL + apply (idempotent)
                FullReconciliation();
                LoggingService.Instance.LogInfo("[SyncEngine] Réconciliation finale terminée.");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogWarning(
                    $"[SyncEngine] Erreur réconciliation finale : {ex.Message}");
            }

            // 5. Copier local.db → DB réseau si accessible en écriture
            if (!string.IsNullOrWhiteSpace(networkDbPath))
            {
                LoggingService.Instance.LogInfo(
                    $"[SyncEngine] Backup vers DB réseau : {networkDbPath}");
                try
                {
                    BackupToNetworkDb(networkDbPath);
                    LoggingService.Instance.LogInfo("[SyncEngine] Backup DB réseau réussi.");
                }
                catch (Exception ex)
                {
                    LoggingService.Instance.LogWarning(
                        $"[SyncEngine] Impossible de synchroniser vers la DB réseau : {ex.Message}");
                }
            }
            else
            {
                LoggingService.Instance.LogInfo("[SyncEngine] Pas de chemin DB réseau fourni — backup réseau skippé.");
            }

            // 6. Compaction classique (snapshot + nettoyage des vieilles ops)
            // NE PAS supprimer tous les fichiers sync : les autres clients actifs ont besoin
            // des .syncop pour synchroniser. Le backup réseau est un filet de sécurité, pas
            // un remplacement du mécanisme de sync par ops.
            try
            {
                _snapshotMgr.ForceCompaction();
                LoggingService.Instance.LogInfo("[SyncEngine] Compaction (snapshot) effectuée à l'arrêt.");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogWarning(
                    $"[SyncEngine] Compaction à l'arrêt : {ex.Message}");
            }

            LoggingService.Instance.LogInfo("[SyncEngine] Arrêt propre terminé.");
        }

        /// <summary>
        /// Copie la DB locale vers la DB réseau via SQLite BACKUP API.
        /// Vérifie d'abord que le fichier réseau est accessible en écriture.
        /// Gère le cas où DatabasePath pointe vers un dossier (ajoute \backlog.db).
        /// </summary>
        private void BackupToNetworkDb(string networkDbPath)
        {
            // Résoudre chemin relatif/UNC
            if (!Path.IsPathRooted(networkDbPath))
                networkDbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, networkDbPath);
            if (networkDbPath.StartsWith("\\\\"))
                networkDbPath = NetworkPathMapper.MapUncPathToDrive(networkDbPath, silent: true);

            LoggingService.Instance.LogInfo(
                $"[SyncEngine] BackupToNetworkDb — chemin résolu : {networkDbPath}");

            // Si le chemin pointe vers un dossier existant → ajouter le nom de fichier attendu
            if (Directory.Exists(networkDbPath))
            {
                networkDbPath = Path.Combine(networkDbPath, "backlog.db");
                LoggingService.Instance.LogInfo(
                    $"[SyncEngine] DatabasePath est un dossier, fichier cible : {networkDbPath}");
            }
            // Sinon on utilise le chemin tel quel (fichier avec ou sans extension)

            // Vérifier que le dossier parent existe et est accessible
            string dir = Path.GetDirectoryName(networkDbPath);
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
            {
                LoggingService.Instance.LogInfo(
                    $"[SyncEngine] Dossier réseau inaccessible : {dir}");
                return;
            }

            // Tester l'accès en écriture avant de lancer le backup
            if (!CanWriteToPath(networkDbPath))
            {
                LoggingService.Instance.LogInfo(
                    $"[SyncEngine] DB réseau non accessible en écriture : {networkDbPath}");
                return;
            }

            string localDbPath = _localDb.LocalDbPath;

            // SQLite Online Backup API : local.db → network DB
            var srcCsb = new SQLiteConnectionStringBuilder
            {
                DataSource = localDbPath,
                Version    = 3,
                ReadOnly   = true,
                Pooling    = false
            };
            var dstCsb = new SQLiteConnectionStringBuilder
            {
                DataSource  = networkDbPath,
                Version     = 3,
                Pooling     = false,
                BusyTimeout = 10000,
                JournalMode = SQLiteJournalModeEnum.Delete  // DELETE pour réseau (pas WAL)
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

            // Supprimer les tables sync internes de la DB réseau (elles sont locales uniquement)
            try
            {
                using (var conn = new SQLiteConnection(dstCsb.ConnectionString))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
                            DROP TABLE IF EXISTS SyncJournal;
                            DROP TABLE IF EXISTS SyncApplied;
                            DROP TABLE IF EXISTS SyncState;
                            DROP TABLE IF EXISTS SyncEntityOrigin;";
                        cmd.ExecuteNonQuery();

                        // VACUUM pour compacter la DB réseau
                        cmd.CommandText = "VACUUM;";
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogWarning(
                    $"[SyncEngine] Nettoyage tables sync dans DB réseau : {ex.Message}");
            }

            LoggingService.Instance.LogInfo(
                $"[SyncEngine] DB réseau synchronisée : {localDbPath} → {networkDbPath}");
        }

        /// <summary>
        /// Vérifie si on peut écrire à un chemin (fichier existant OU dossier pour nouveau fichier).
        /// Utilise FileShare.ReadWrite pour ne pas être bloqué si un autre process lit le fichier.
        /// </summary>
        private static bool CanWriteToPath(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    // Tester l'ouverture en écriture AVEC partage (ne pas bloquer si un autre process lit)
                    using (var fs = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
                    {
                        return true;
                    }
                }
                else
                {
                    // Tester la création dans le dossier parent
                    string dir = Path.GetDirectoryName(path);
                    if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return false;
                    string testFile = Path.Combine(dir, ".writetest_" + Guid.NewGuid().ToString("N"));
                    File.WriteAllText(testFile, "");
                    File.Delete(testFile);
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        // ─── IDisposable ─────────────────────────────────────────────────────
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Stop();
        }
    }
}

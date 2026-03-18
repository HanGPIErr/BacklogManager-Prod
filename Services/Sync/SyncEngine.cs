using System;
using System.Collections.Generic;
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

        // ─── État interne ─────────────────────────────────────────────────────
        private Thread      _thread;
        private bool        _running;
        private bool        _disposed;

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
            // On laisse le thread se terminer naturellement après son prochain sleep
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

                // Attente inter-cycles (découpée en petits tranches pour réagir rapidement à Stop())
                int slept = 0;
                while (_running && slept < SyncIntervalMs)
                {
                    Thread.Sleep(Math.Min(500, SyncIntervalMs - slept));
                    slept += 500;
                }
            }

            LoggingService.Instance.LogInfo("[SyncEngine] Thread de synchronisation arrêté.");
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

            // ── 1. Publier les opérations locales non publiées ───────────────
            PushUnpublished();

            // ── 2. Tirer & appliquer les opérations distantes ────────────────
            PullAndApply();

            // ── 3. Compaction si nécessaire ──────────────────────────────────
            CheckCompaction();

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

        // ─── Pull + Apply ────────────────────────────────────────────────────
        private void PullAndApply()
        {
            try
            {
                string cursor = _localDb.GetSyncState(NasCursorKey);   // null ou dernier fichier vu
                var remoteOps = _nasStore.PullSince(cursor, _clientId);

                if (remoteOps.Count == 0) return;

                using (var conn = _localDb.OpenWriteConnection())
                {
                    conn.Open();
                    using (var tx = conn.BeginTransaction())
                    {
                        foreach (var op in remoteOps)
                        {
                            _applier.Apply(conn, tx, op);
                        }
                        tx.Commit();
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

        // ─── IDisposable ─────────────────────────────────────────────────────
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Stop();
        }
    }
}

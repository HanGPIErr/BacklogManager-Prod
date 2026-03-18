using System;
using System.Data.SQLite;
using System.IO;

namespace BacklogManager.Services.Sync
{
    /// <summary>
    /// Gère la base SQLite locale par poste.
    ///
    /// Stratégie :
    /// - La DB locale est stockée dans %APPDATA%\BacklogManager\local.db
    ///   (ou un chemin configurable via config.ini LocalDatabasePath=)
    /// - Elle utilise le mode WAL pour des performances optimales (multi-reader, mono-writer)
    /// - Elle JAMAIS sur le NAS : le NAS sert uniquement à la synchronisation
    /// - Elle contient également la table SyncJournal (journal des opérations lokales non publiées)
    ///   et la table SyncApplied (table d'idempotence des opérations reçues)
    ///
    /// Utilisation :
    ///   var factory = new LocalDatabaseFactory();
    ///   string localPath = factory.GetOrCreateLocalDb();
    /// </summary>
    public class LocalDatabaseFactory
    {
        private readonly string _localDbPath;
        private bool _initialized;

        // Compteur de séquence in-memory — élimine un SELECT MAX() par opération d'écriture
        private long _sequenceCounter = -1;
        private readonly object _seqLock = new object();

        public string LocalDbPath => _localDbPath;

        public LocalDatabaseFactory(string overridePath = null)
        {
            if (!string.IsNullOrWhiteSpace(overridePath))
            {
                _localDbPath = overridePath;
            }
            else
            {
                // Par défaut : %APPDATA%\BacklogManager\local.db
                // Sur un réseau d'entreprise, AppData est souvent redirigé  → reste local
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string folder  = Path.Combine(appData, "BacklogManager");
                Directory.CreateDirectory(folder);
                _localDbPath = Path.Combine(folder, "local.db");
            }
        }

        /// <summary>
        /// Initialise (si besoin) la DB locale et retourne son chemin.
        /// Peut être appelé plusieurs fois : idempotent.
        /// </summary>
        public string GetOrCreateLocalDb()
        {
            if (_initialized) return _localDbPath;

            bool isNew = !File.Exists(_localDbPath);
            if (isNew)
            {
                SQLiteConnection.CreateFile(_localDbPath);
                LoggingService.Instance.LogInfo($"[LocalDatabase] Création base locale : {_localDbPath}");
            }

            ApplyWalAndPragmas();
            CreateSyncTables();

            _initialized = true;
            LoggingService.Instance.LogInfo($"[LocalDatabase] Base locale prête : {_localDbPath} (nouveau={isNew})");
            return _localDbPath;
        }

        // ─── Connexions ──────────────────────────────────────────────────

        public SQLiteConnection OpenReadConnection()
        {
            var csb = new SQLiteConnectionStringBuilder
            {
                DataSource  = _localDbPath,
                Version     = 3,
                JournalMode = SQLiteJournalModeEnum.Wal,
                Pooling     = true,
                ReadOnly    = true,
                BusyTimeout = 2000,
                SyncMode    = SynchronizationModes.Normal
            };
            var conn = new SQLiteConnection(csb.ConnectionString);
            conn.Open();
            return conn;
        }

        public SQLiteConnection OpenWriteConnection()
        {
            var csb = new SQLiteConnectionStringBuilder
            {
                DataSource  = _localDbPath,
                Version     = 3,
                JournalMode = SQLiteJournalModeEnum.Wal,
                Pooling     = false,   // Pas de pool pour les writes : connexion exclusive
                ReadOnly    = false,
                BusyTimeout = 5000,
                SyncMode    = SynchronizationModes.Normal  // WAL = Normal est sûr et rapide
            };
            var conn = new SQLiteConnection(csb.ConnectionString);
            conn.Open();
            return conn;
        }

        // ─── Internes ────────────────────────────────────────────────────

        private void ApplyWalAndPragmas()
        {
            // On ouvre une connexion sans Pooling pour forcer le mode WAL et les pragmas globaux
            var csb = new SQLiteConnectionStringBuilder
            {
                DataSource  = _localDbPath,
                Version     = 3,
                Pooling     = false,
                BusyTimeout = 5000
            };
            using (var conn = new SQLiteConnection(csb.ConnectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    // WAL : lectures non bloquées par les écritures, performances ×3-5 vs DELETE journal
                    cmd.CommandText = "PRAGMA journal_mode=WAL;";
                    cmd.ExecuteNonQuery();

                    // NORMAL est sûr avec WAL (fsync uniquement aux checkpoints)
                    cmd.CommandText = "PRAGMA synchronous=NORMAL;";
                    cmd.ExecuteNonQuery();

                    // Cache 20 Mo (20000 pages × 1024 octets typique)
                    cmd.CommandText = "PRAGMA cache_size=-20000;";
                    cmd.ExecuteNonQuery();

                    // Stocker les tables temporaires en mémoire
                    cmd.CommandText = "PRAGMA temp_store=MEMORY;";
                    cmd.ExecuteNonQuery();

                    // Auto-checkpoint WAL à 500 pages (équilibre performance/taille WAL)
                    cmd.CommandText = "PRAGMA wal_autocheckpoint=500;";
                    cmd.ExecuteNonQuery();

                    // Activer les clés étrangères
                    cmd.CommandText = "PRAGMA foreign_keys=ON;";
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void CreateSyncTables()
        {
            using (var conn = OpenWriteConnection())
            using (var cmd = conn.CreateCommand())
            {
                // Table journal : opérations locales en attente de publication NAS
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS SyncJournal (
                        Id              INTEGER PRIMARY KEY AUTOINCREMENT,
                        OperationId     TEXT    NOT NULL UNIQUE,
                        LocalSequence   INTEGER NOT NULL,
                        OriginClientId  TEXT    NOT NULL,
                        OriginUsername  TEXT    NOT NULL,
                        TimestampUtc    TEXT    NOT NULL,
                        OperationType   TEXT    NOT NULL,
                        TableName       TEXT    NOT NULL,
                        EntityId        INTEGER NOT NULL,
                        PayloadJson     TEXT    NOT NULL,
                        IsPublished     INTEGER NOT NULL DEFAULT 0,
                        PublishedAtUtc  TEXT,
                        CreatedAtLocal  TEXT    NOT NULL DEFAULT (datetime('now'))
                    );
                    CREATE INDEX IF NOT EXISTS idx_syncjournal_published  ON SyncJournal(IsPublished);
                    CREATE INDEX IF NOT EXISTS idx_syncjournal_seq        ON SyncJournal(LocalSequence);
                    CREATE INDEX IF NOT EXISTS idx_syncjournal_opid       ON SyncJournal(OperationId);
                ";
                cmd.ExecuteNonQuery();

                // Table idempotence : opérations distantes déjà appliquées
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS SyncApplied (
                        OperationId     TEXT    PRIMARY KEY NOT NULL,
                        OriginClientId  TEXT    NOT NULL,
                        TimestampUtc    TEXT    NOT NULL,
                        OperationType   TEXT    NOT NULL,
                        AppliedAtUtc    TEXT    NOT NULL DEFAULT (datetime('now')),
                        HasConflict     INTEGER NOT NULL DEFAULT 0,
                        ConflictDetail  TEXT
                    );
                    CREATE INDEX IF NOT EXISTS idx_syncapplied_client ON SyncApplied(OriginClientId);
                    CREATE INDEX IF NOT EXISTS idx_syncapplied_ts     ON SyncApplied(TimestampUtc);
                ";
                cmd.ExecuteNonQuery();

                // Table metadata locale de la sync (curseur NAS, dernier snapshot, etc.)
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS SyncState (
                        Key     TEXT PRIMARY KEY NOT NULL,
                        Value   TEXT,
                        UpdatedAtUtc TEXT NOT NULL DEFAULT (datetime('now'))
                    );
                ";
                cmd.ExecuteNonQuery();
            }
        }

        // ─── Accès au SyncJournal ────────────────────────────────────────

        /// <summary>
        /// Enregistre une opération dans le journal local (atomique avec la modification métier).
        /// Doit être appelé dans la MÊME transaction que l'écriture métier.
        /// </summary>
        public void AppendToJournal(SQLiteConnection conn, SQLiteTransaction tx, SyncOperation op)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = @"
                    INSERT OR IGNORE INTO SyncJournal
                        (OperationId, LocalSequence, OriginClientId, OriginUsername,
                         TimestampUtc, OperationType, TableName, EntityId, PayloadJson, IsPublished)
                    VALUES
                        (@OperationId, @LocalSequence, @OriginClientId, @OriginUsername,
                         @TimestampUtc, @OperationType, @TableName, @EntityId, @PayloadJson, 0)";
                cmd.Parameters.AddWithValue("@OperationId",    op.OperationId);
                cmd.Parameters.AddWithValue("@LocalSequence",  NextSequence());
                cmd.Parameters.AddWithValue("@OriginClientId", op.OriginClientId);
                cmd.Parameters.AddWithValue("@OriginUsername", op.OriginUsername);
                cmd.Parameters.AddWithValue("@TimestampUtc",   op.TimestampUtc.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                cmd.Parameters.AddWithValue("@OperationType",  op.OperationType);
                cmd.Parameters.AddWithValue("@TableName",      op.TableName);
                cmd.Parameters.AddWithValue("@EntityId",       op.EntityId);
                cmd.Parameters.AddWithValue("@PayloadJson",    op.PayloadJson);
                cmd.ExecuteNonQuery();
            }
        }

        // Compteur de séquence thread-safe en mémoire
        private long NextSequence()
        {
            lock (_seqLock)
            {
                if (_sequenceCounter < 0)
                {
                    // Initialisation paresseuse : lire le MAX actuel une seule fois
                    using (var conn = OpenReadConnection())
                    using (var cmd  = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT COALESCE(MAX(LocalSequence), 0) FROM SyncJournal;";
                        _sequenceCounter = Convert.ToInt64(cmd.ExecuteScalar());
                    }
                }
                return ++_sequenceCounter;
            }
        }

        /// <summary>
        /// Marque une opération distante comme appliquée EN UTILISANT la connexion/transaction
        /// courante (ne pas ouvrir de 2e connexion en écriture — évite le SQLite BUSY).
        /// À appeler depuis SyncApplier.Apply() dans le contexte de PullAndApply().
        /// </summary>
        public void MarkAppliedInline(SQLiteConnection conn, SQLiteTransaction tx,
            SyncOperation op, bool hasConflict = false, string conflictDetail = null)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = @"
                    INSERT OR IGNORE INTO SyncApplied
                        (OperationId, OriginClientId, TimestampUtc, OperationType,
                         AppliedAtUtc, HasConflict, ConflictDetail)
                    VALUES (@id, @client, @ts, @type, @applied, @conflict, @detail)";
                cmd.Parameters.AddWithValue("@id",      op.OperationId);
                cmd.Parameters.AddWithValue("@client",  op.OriginClientId ?? "");
                cmd.Parameters.AddWithValue("@ts",      op.TimestampUtc.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                cmd.Parameters.AddWithValue("@type",    op.OperationType ?? "");
                cmd.Parameters.AddWithValue("@applied", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                cmd.Parameters.AddWithValue("@conflict", hasConflict ? 1 : 0);
                cmd.Parameters.AddWithValue("@detail",  (object)conflictDetail ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Marque les opérations du journal comme publiées.
        /// </summary>
        public void MarkPublished(System.Collections.Generic.IEnumerable<string> operationIds)
        {
            using (var conn = OpenWriteConnection())
            using (var tx   = conn.BeginTransaction())
            using (var cmd  = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = @"
                    UPDATE SyncJournal
                    SET IsPublished = 1, PublishedAtUtc = @ts
                    WHERE OperationId = @id";
                cmd.Parameters.Add("@ts", System.Data.DbType.String);
                cmd.Parameters.Add("@id", System.Data.DbType.String);
                string ts = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                foreach (var id in operationIds)
                {
                    cmd.Parameters["@ts"].Value = ts;
                    cmd.Parameters["@id"].Value = id;
                    cmd.ExecuteNonQuery();
                }
                tx.Commit();
            }
        }

        /// <summary>
        /// Retourne toutes les opérations non encore publiées, triées par séquence.
        /// </summary>
        public System.Collections.Generic.List<SyncOperation> GetUnpublishedOperations()
        {
            var list = new System.Collections.Generic.List<SyncOperation>();
            using (var conn = OpenReadConnection())
            using (var cmd  = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT OperationId, LocalSequence, OriginClientId, OriginUsername,
                           TimestampUtc, OperationType, TableName, EntityId, PayloadJson
                    FROM SyncJournal WHERE IsPublished = 0
                    ORDER BY LocalSequence";
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(new SyncOperation
                        {
                            OperationId    = r.GetString(0),
                            LocalSequence  = r.GetInt64(1),
                            OriginClientId = r.GetString(2),
                            OriginUsername = r.GetString(3),
                            TimestampUtc   = DateTime.Parse(r.GetString(4)),
                            OperationType  = r.GetString(5),
                            TableName      = r.GetString(6),
                            EntityId       = r.GetInt32(7),
                            PayloadJson    = r.GetString(8)
                        });
                    }
                }
            }
            return list;
        }

        // ─── État de sync ────────────────────────────────────────────────

        public string GetSyncState(string key)
        {
            using (var conn = OpenReadConnection())
            using (var cmd  = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT Value FROM SyncState WHERE Key = @k";
                cmd.Parameters.AddWithValue("@k", key);
                return cmd.ExecuteScalar()?.ToString();
            }
        }

        public void SetSyncState(string key, string value)
        {
            using (var conn = OpenWriteConnection())
            using (var cmd  = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    INSERT OR REPLACE INTO SyncState (Key, Value, UpdatedAtUtc)
                    VALUES (@k, @v, @ts)";
                cmd.Parameters.AddWithValue("@k",  key);
                cmd.Parameters.AddWithValue("@v",  value ?? "");
                cmd.Parameters.AddWithValue("@ts", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                cmd.ExecuteNonQuery();
            }
        }

        // ─── Idempotence ─────────────────────────────────────────────────

        /// <summary>
        /// Vérifie si une opération distante a déjà été appliquée.
        /// </summary>
        public bool IsAlreadyApplied(string operationId)
        {
            using (var conn = OpenReadConnection())
            using (var cmd  = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(*) FROM SyncApplied WHERE OperationId = @id";
                cmd.Parameters.AddWithValue("@id", operationId);
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        /// <summary>
        /// Marque une opération distante comme appliquée (idempotence).
        /// </summary>
        public void MarkApplied(SyncOperation op, bool hasConflict = false, string conflictDetail = null)
        {
            using (var conn = OpenWriteConnection())
            using (var cmd  = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    INSERT OR IGNORE INTO SyncApplied
                        (OperationId, OriginClientId, TimestampUtc, OperationType,
                         AppliedAtUtc, HasConflict, ConflictDetail)
                    VALUES (@id, @client, @ts, @type, @applied, @conflict, @detail)";
                cmd.Parameters.AddWithValue("@id",      op.OperationId);
                cmd.Parameters.AddWithValue("@client",  op.OriginClientId);
                cmd.Parameters.AddWithValue("@ts",      op.TimestampUtc.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                cmd.Parameters.AddWithValue("@type",    op.OperationType);
                cmd.Parameters.AddWithValue("@applied", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                cmd.Parameters.AddWithValue("@conflict", hasConflict ? 1 : 0);
                cmd.Parameters.AddWithValue("@detail",  (object)conflictDetail ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
        }
    }
}

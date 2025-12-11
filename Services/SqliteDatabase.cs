using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using BacklogManager.Domain;

namespace BacklogManager.Services
{
    public class SqliteDatabase : IDatabase
    {
        private readonly string _databasePath;
        private readonly string _connectionString; // Connection string READ-WRITE par défaut (compatibilité)
        private readonly string _readConnectionString;
        private readonly string _writeConnectionString;

        public SqliteDatabase()
        {
            // Lire la configuration depuis config.ini
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.ini");
            string dbPath = "data\\backlog.db"; // Valeur par défaut

            if (File.Exists(configPath))
            {
                try
                {
                    var lines = File.ReadAllLines(configPath, System.Text.Encoding.UTF8);
                    foreach (var line in lines)
                    {
                        if (line.StartsWith("DatabasePath="))
                        {
                            dbPath = line.Substring("DatabasePath=".Length).Trim();
                            // Nettoyer les guillemets si présents
                            dbPath = dbPath.Trim('\"', '\'');
                            // Normaliser les chemins UNC (remplacer \\\\ par \\\\)
                            if (dbPath.StartsWith("\\\\"))
                            {
                                dbPath = "\\\\" + dbPath.Substring(2).Replace("\\\\", "\\");
                            }
                            break;
                        }
                    }
                }
                catch
                {
                    // En cas d'erreur de lecture, utiliser la valeur par défaut
                }
            }

            // Convertir le chemin relatif en chemin absolu si nécessaire
            if (!Path.IsPathRooted(dbPath))
            {
                dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dbPath);
            }

            // Mapper les chemins UNC vers des lecteurs réseau
            if (dbPath.StartsWith("\\\\"))
            {
                dbPath = NetworkPathMapper.MapUncPathToDrive(dbPath);
            }

            _databasePath = dbPath;
            
            // Créer le dossier parent si nécessaire
            var directory = Path.GetDirectoryName(_databasePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Pour les chemins UNC, désactiver WAL (pas supporté sur les partages réseau)
            bool isUncPath = _databasePath.StartsWith("\\\\");
            
            // Connexion READONLY pour les lectures
            var readBuilder = new SQLiteConnectionStringBuilder
            {
                DataSource = _databasePath,
                Version = 3,
                JournalMode = isUncPath ? SQLiteJournalModeEnum.Delete : SQLiteJournalModeEnum.Delete, // DELETE pour réseau
                Pooling = true,
                BusyTimeout = 30000, // 30 secondes
                ReadOnly = true,
                SyncMode = SynchronizationModes.Full // Sécurité maximale
            };
            _readConnectionString = readBuilder.ConnectionString;

            // Connexion READ-WRITE pour les écritures
            var writeBuilder = new SQLiteConnectionStringBuilder
            {
                DataSource = _databasePath,
                Version = 3,
                JournalMode = isUncPath ? SQLiteJournalModeEnum.Delete : SQLiteJournalModeEnum.Delete, // DELETE pour réseau
                Pooling = true,
                BusyTimeout = 30000, // 30 secondes
                ReadOnly = false,
                SyncMode = SynchronizationModes.Full // Sécurité maximale
            };
            _writeConnectionString = writeBuilder.ConnectionString;
            
            // Par défaut, utiliser READ-ONLY pour toutes les opérations (sécurité)
            _connectionString = _readConnectionString;

            InitializeDatabase();
        }

        // Méthodes helper pour obtenir les bonnes connection strings
        // Par défaut READ-ONLY pour la sécurité
        private SQLiteConnection GetConnection()
        {
            return new SQLiteConnection(_readConnectionString);
        }

        private SQLiteConnection GetConnectionForWrite()
        {
            var conn = new SQLiteConnection(_writeConnectionString);
            
            // Ouvrir avec retry pour gérer les locks
            int retryCount = 0;
            int maxRetries = 10; // Plus de tentatives pour l'ouverture
            
            while (true)
            {
                try
                {
                    conn.Open();
                    
                    // Configurer PRAGMA pour multi-utilisateurs
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "PRAGMA busy_timeout = 30000;"; // 30 secondes
                        cmd.ExecuteNonQuery();
                        
                        cmd.CommandText = "PRAGMA journal_mode = DELETE;"; // Plus safe pour réseau
                        cmd.ExecuteNonQuery();
                        
                        cmd.CommandText = "PRAGMA synchronous = FULL;"; // Sécurité maximale
                        cmd.ExecuteNonQuery();
                    }
                    
                    return conn;
                }
                catch (SQLiteException ex) when (ex.ResultCode == SQLiteErrorCode.Busy || 
                                                   ex.ResultCode == SQLiteErrorCode.Locked)
                {
                    retryCount++;
                    if (retryCount >= maxRetries)
                    {
                        conn.Dispose();
                        throw new Exception($"❌ La base de données est verrouillée par un autre utilisateur.\n\n" +
                                          $"Impossible d'accéder après {maxRetries} tentatives.\n\n" +
                                          $"Veuillez réessayer dans quelques instants.\n\n" +
                                          $"Si le problème persiste, contactez l'administrateur.", ex);
                    }
                    
                    // Attente exponentielle avec jitter pour éviter les collisions
                    var baseDelay = 200 * (int)Math.Pow(1.5, retryCount - 1); // 200ms, 300ms, 450ms, 675ms...
                    var jitter = new Random().Next(0, 100); // Ajouter du hasard
                    System.Threading.Thread.Sleep(baseDelay + jitter);
                    
                    System.Diagnostics.Debug.WriteLine($"[SqliteDatabase] Retry #{retryCount} après {baseDelay + jitter}ms - {ex.Message}");
                }
                catch
                {
                    conn.Dispose();
                    throw;
                }
            }
        }

        private SQLiteConnection GetConnectionForRead()
        {
            return new SQLiteConnection(_readConnectionString);
        }

        private void InitializeDatabase()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[SqliteDatabase] Chemin DB: {_databasePath}");
                System.Diagnostics.Debug.WriteLine($"[SqliteDatabase] Connection String READ: {_readConnectionString}");
                System.Diagnostics.Debug.WriteLine($"[SqliteDatabase] Connection String WRITE: {_writeConnectionString}");
                
                if (!File.Exists(_databasePath))
                {
                    var directory = Path.GetDirectoryName(_databasePath);
                    System.Diagnostics.Debug.WriteLine($"[SqliteDatabase] Dossier parent: {directory}");
                    System.Diagnostics.Debug.WriteLine($"[SqliteDatabase] Dossier existe: {Directory.Exists(directory)}");
                    
                    SQLiteConnection.CreateFile(_databasePath);
                    System.Diagnostics.Debug.WriteLine($"[SqliteDatabase] Fichier DB cree avec succes");
                    CreateTables();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[SqliteDatabase] Fichier DB existe deja");
                    // Appliquer les migrations sur base existante
                    using (var conn = GetConnectionForWrite())
                    {
                        
                        // Configuration pour multi-utilisateurs
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = "PRAGMA journal_mode=WAL;";
                            cmd.ExecuteNonQuery();
                            cmd.CommandText = "PRAGMA synchronous=NORMAL;";
                            cmd.ExecuteNonQuery();
                            cmd.CommandText = "PRAGMA temp_store=MEMORY;";
                            cmd.ExecuteNonQuery();
                            cmd.CommandText = "PRAGMA cache_size=10000;";
                            cmd.ExecuteNonQuery();
                        }
                        
                        MigrateDatabaseSchema(conn);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SqliteDatabase] ERREUR: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[SqliteDatabase] Stack: {ex.StackTrace}");
                throw;
            }
        }

        // Exécution avec retry en cas de lock ou erreur réseau
        private T ExecuteWithRetry<T>(Func<T> action, int maxRetries = 10)
        {
            int retryCount = 0;
            while (true)
            {
                try
                {
                    return action();
                }
                catch (SQLiteException ex) when (ex.ResultCode == SQLiteErrorCode.Busy || 
                                                   ex.ResultCode == SQLiteErrorCode.Locked ||
                                                   ex.ResultCode == SQLiteErrorCode.IoErr)
                {
                    retryCount++;
                    if (retryCount >= maxRetries)
                    {
                        throw new Exception($"❌ Opération impossible après {maxRetries} tentatives.\n\n" +
                                          $"La base de données est actuellement utilisée par un autre utilisateur.\n\n" +
                                          $"Code erreur: {ex.ResultCode}\n" +
                                          $"Message: {ex.Message}\n\n" +
                                          $"Veuillez réessayer dans quelques instants.", ex);
                    }
                    // Attente exponentielle avec jitter: 100ms, 200ms, 400ms, 800ms...
                    var baseDelay = 100 * (int)Math.Pow(2, retryCount - 1);
                    var jitter = new Random().Next(0, 50);
                    System.Threading.Thread.Sleep(baseDelay + jitter);
                    
                    System.Diagnostics.Debug.WriteLine($"[SqliteDatabase] ExecuteWithRetry #{retryCount} après {baseDelay + jitter}ms - {ex.ResultCode}: {ex.Message}");
                }
                catch (System.IO.IOException ioEx)
                {
                    // Erreurs réseau/fichier
                    retryCount++;
                    if (retryCount >= maxRetries)
                    {
                        throw new Exception($"❌ Erreur d'accès au fichier après {maxRetries} tentatives.\n\n" +
                                          $"Vérifiez que le partage réseau est accessible.\n\n" +
                                          $"Message: {ioEx.Message}", ioEx);
                    }
                    
                    System.Threading.Thread.Sleep(500 * retryCount); // Attente plus longue pour les erreurs réseau
                    System.Diagnostics.Debug.WriteLine($"[SqliteDatabase] IOException Retry #{retryCount} - {ioEx.Message}");
                }
            }
        }

        private void ExecuteWithRetry(Action action, int maxRetries = 10)
        {
            ExecuteWithRetry<object>(() => { action(); return null; }, maxRetries);
        }

        // Wrapper pour exécuter une transaction avec retry automatique
        private T ExecuteInTransaction<T>(Func<SQLiteConnection, SQLiteTransaction, T> action)
        {
            return ExecuteWithRetry(() =>
            {
                using (var conn = GetConnectionForWrite())
                {
                    using (var transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            var result = action(conn, transaction);
                            transaction.Commit();
                            return result;
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            });
        }

        private void ExecuteInTransaction(Action<SQLiteConnection, SQLiteTransaction> action)
        {
            ExecuteInTransaction<object>((conn, trans) =>
            {
                action(conn, trans);
                return null;
            });
        }

        private void CreateTables()
        {
            using (var conn = GetConnectionForWrite())
            {
                
                // Configuration pour multi-utilisateurs
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "PRAGMA journal_mode=WAL;";
                    cmd.ExecuteNonQuery();
                    cmd.CommandText = "PRAGMA synchronous=NORMAL;";
                    cmd.ExecuteNonQuery();
                    cmd.CommandText = "PRAGMA temp_store=MEMORY;";
                    cmd.ExecuteNonQuery();
                    cmd.CommandText = "PRAGMA cache_size=10000;";
                    cmd.ExecuteNonQuery();
                }
                
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        CREATE TABLE IF NOT EXISTS Roles (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            Nom TEXT NOT NULL,
                            Type INTEGER NOT NULL,
                            PeutCreerDemandes INTEGER NOT NULL,
                            PeutChiffrer INTEGER NOT NULL,
                            PeutPrioriser INTEGER NOT NULL,
                            PeutGererUtilisateurs INTEGER NOT NULL,
                            PeutVoirKPI INTEGER NOT NULL,
                            PeutGererReferentiels INTEGER NOT NULL,
                            PeutModifierTaches INTEGER NOT NULL DEFAULT 0,
                            PeutSupprimerTaches INTEGER NOT NULL DEFAULT 0,
                            Actif INTEGER NOT NULL
                        );

                        CREATE TABLE IF NOT EXISTS Utilisateurs (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            UsernameWindows TEXT NOT NULL UNIQUE,
                            Nom TEXT NOT NULL,
                            Prenom TEXT NOT NULL,
                            Email TEXT,
                            RoleId INTEGER NOT NULL,
                            Actif INTEGER NOT NULL,
                            DateCreation TEXT NOT NULL,
                            DateDerniereConnexion TEXT,
                            FOREIGN KEY (RoleId) REFERENCES Roles(Id)
                        );

                        CREATE TABLE IF NOT EXISTS Projets (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            Nom TEXT NOT NULL,
                            Description TEXT,
                            ChefProjetId INTEGER,
                            DateDebut TEXT,
                            DateFin TEXT,
                            DateCreation TEXT NOT NULL,
                            Actif INTEGER NOT NULL,
                            FOREIGN KEY (ChefProjetId) REFERENCES Utilisateurs(Id)
                        );

                        CREATE TABLE IF NOT EXISTS Sprints (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            Nom TEXT NOT NULL,
                            ProjetId INTEGER NOT NULL,
                            DateDebut TEXT NOT NULL,
                            DateFin TEXT NOT NULL,
                            Objectif TEXT,
                            EstActif INTEGER NOT NULL,
                            FOREIGN KEY (ProjetId) REFERENCES Projets(Id)
                        );

                        CREATE TABLE IF NOT EXISTS BacklogItems (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            Titre TEXT NOT NULL,
                            Description TEXT,
                            ProjetId INTEGER,
                            DevId INTEGER,
                            Type INTEGER NOT NULL,
                            Priorite INTEGER NOT NULL,
                            Statut INTEGER NOT NULL,
                            Points INTEGER,
                            ChiffrageHeures REAL,
                            TempsReelHeures REAL,
                            DateFinAttendue TEXT,
                            DateDebut TEXT,
                            DateFin TEXT,
                            DateCreation TEXT NOT NULL,
                            DateDerniereMaj TEXT NOT NULL,
                            EstArchive INTEGER NOT NULL,
                            SprintId INTEGER,
                            DemandeId INTEGER,
                            DevSupporte INTEGER,
                            TacheSupportee INTEGER,
                            FOREIGN KEY (ProjetId) REFERENCES Projets(Id),
                            FOREIGN KEY (DevId) REFERENCES Utilisateurs(Id),
                            FOREIGN KEY (SprintId) REFERENCES Sprints(Id),
                            FOREIGN KEY (DemandeId) REFERENCES Demandes(Id),
                            FOREIGN KEY (DevSupporte) REFERENCES Utilisateurs(Id),
                            FOREIGN KEY (TacheSupportee) REFERENCES BacklogItems(Id)
                        );

                        CREATE TABLE IF NOT EXISTS Demandes (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            Titre TEXT NOT NULL,
                            Description TEXT,
                            Specifications TEXT,
                            ContexteMetier TEXT,
                            BeneficesAttendus TEXT,
                            DemandeurId INTEGER NOT NULL,
                            BusinessAnalystId INTEGER,
                            ChefProjetId INTEGER,
                            DevChiffreurId INTEGER,
                            Type INTEGER NOT NULL,
                            Criticite INTEGER NOT NULL,
                            Statut INTEGER NOT NULL,
                            DateCreation TEXT NOT NULL,
                            DateValidationChiffrage TEXT,
                            DateAcceptation TEXT,
                            DateLivraison TEXT,
                            ChiffrageEstimeJours REAL,
                            ChiffrageReelJours REAL,
                            DatePrevisionnelleImplementation TEXT,
                            JustificationRefus TEXT,
                            EstArchivee INTEGER NOT NULL,
                            FOREIGN KEY (DemandeurId) REFERENCES Utilisateurs(Id),
                            FOREIGN KEY (BusinessAnalystId) REFERENCES Utilisateurs(Id),
                            FOREIGN KEY (ChefProjetId) REFERENCES Utilisateurs(Id),
                            FOREIGN KEY (DevChiffreurId) REFERENCES Utilisateurs(Id)
                        );

                        CREATE TABLE IF NOT EXISTS Commentaires (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            DemandeId INTEGER,
                            BacklogItemId INTEGER,
                            AuteurId INTEGER NOT NULL,
                            Contenu TEXT NOT NULL,
                            DateCreation TEXT NOT NULL,
                            PieceJointeNom TEXT,
                            PieceJointeChemin TEXT,
                            FOREIGN KEY (DemandeId) REFERENCES Demandes(Id),
                            FOREIGN KEY (BacklogItemId) REFERENCES BacklogItems(Id),
                            FOREIGN KEY (AuteurId) REFERENCES Utilisateurs(Id)
                        );

                        CREATE TABLE IF NOT EXISTS HistoriqueModifications (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            TypeEntite TEXT NOT NULL,
                            EntiteId INTEGER NOT NULL,
                            TypeModification INTEGER NOT NULL,
                            UtilisateurId INTEGER NOT NULL,
                            DateModification TEXT NOT NULL,
                            AncienneValeur TEXT,
                            NouvelleValeur TEXT,
                            ChampModifie TEXT,
                            FOREIGN KEY (UtilisateurId) REFERENCES Utilisateurs(Id)
                        );

                        CREATE TABLE IF NOT EXISTS PokerSessions (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            BacklogItemId INTEGER NOT NULL,
                            DateCreation TEXT NOT NULL,
                            EstActive INTEGER NOT NULL,
                            EstTerminee INTEGER NOT NULL,
                            ConsensusPoints INTEGER,
                            FOREIGN KEY (BacklogItemId) REFERENCES BacklogItems(Id)
                        );

                        CREATE TABLE IF NOT EXISTS PokerVotes (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            SessionId INTEGER NOT NULL,
                            DevId INTEGER NOT NULL,
                            Points INTEGER NOT NULL,
                            DateVote TEXT NOT NULL,
                            FOREIGN KEY (SessionId) REFERENCES PokerSessions(Id),
                            FOREIGN KEY (DevId) REFERENCES Utilisateurs(Id)
                        );

                        CREATE TABLE IF NOT EXISTS Disponibilites (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            DevId INTEGER NOT NULL,
                            SprintId INTEGER NOT NULL,
                            JoursDisponibles REAL NOT NULL,
                            Commentaire TEXT,
                            FOREIGN KEY (DevId) REFERENCES Utilisateurs(Id),
                            FOREIGN KEY (SprintId) REFERENCES Sprints(Id)
                        );

                        CREATE TABLE IF NOT EXISTS Devs (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            Nom TEXT NOT NULL,
                            Initiales TEXT NOT NULL,
                            Actif INTEGER NOT NULL
                        );

                        CREATE TABLE IF NOT EXISTS AuditLog (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            Action TEXT NOT NULL,
                            UserId INTEGER NOT NULL,
                            Username TEXT NOT NULL,
                            EntityType TEXT NOT NULL,
                            EntityId INTEGER,
                            OldValue TEXT,
                            NewValue TEXT,
                            DateAction TEXT NOT NULL,
                            Details TEXT,
                            FOREIGN KEY (UserId) REFERENCES Utilisateurs(Id)
                        );

                        CREATE TABLE IF NOT EXISTS Notifications (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            Titre TEXT NOT NULL,
                            Message TEXT NOT NULL,
                            Type INTEGER NOT NULL,
                            DateCreation TEXT NOT NULL,
                            EstLue INTEGER NOT NULL,
                            TacheId INTEGER,
                            FOREIGN KEY (TacheId) REFERENCES BacklogItems(Id)
                        );

                        CREATE TABLE IF NOT EXISTS CRA (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            BacklogItemId INTEGER NOT NULL,
                            DevId INTEGER NOT NULL,
                            Date TEXT NOT NULL,
                            HeuresTravaillees REAL NOT NULL,
                            Commentaire TEXT,
                            DateCreation TEXT NOT NULL,
                            EstPrevisionnel INTEGER DEFAULT 0,
                            EstValide INTEGER DEFAULT 0,
                            FOREIGN KEY (BacklogItemId) REFERENCES BacklogItems(Id) ON DELETE CASCADE,
                            FOREIGN KEY (DevId) REFERENCES Utilisateurs(Id)
                        );

                        CREATE INDEX IF NOT EXISTS idx_cra_backlogitem ON CRA(BacklogItemId);
                        CREATE INDEX IF NOT EXISTS idx_cra_dev ON CRA(DevId);
                        CREATE INDEX IF NOT EXISTS idx_cra_date ON CRA(Date);
                    ";
                    cmd.ExecuteNonQuery();
                }
                
                // Migration: Ajouter les colonnes manquantes si nécessaire
                MigrateDatabaseSchema(conn);
            }
        }
        
        private void MigrateDatabaseSchema(SQLiteConnection conn)
        {
            using (var cmd = conn.CreateCommand())
            {
                // Vérifier si les tables de base existent, sinon les créer toutes
                cmd.CommandText = @"SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='Roles';";
                var hasRolesTable = Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                
                if (!hasRolesTable)
                {
                    // Si la table Roles n'existe pas, créer toutes les tables de base
                    CreateTables();
                    return; // Les tables sont créées, pas besoin de migrations supplémentaires
                }
                
                // Vérifier et créer la table Notifications si elle n'existe pas
                cmd.CommandText = @"SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='Notifications';";
                var hasNotificationsTable = Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                
                if (!hasNotificationsTable)
                {
                    cmd.CommandText = @"
                        CREATE TABLE IF NOT EXISTS Notifications (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            Titre TEXT NOT NULL,
                            Message TEXT NOT NULL,
                            Type INTEGER NOT NULL,
                            DateCreation TEXT NOT NULL,
                            EstLue INTEGER NOT NULL,
                            TacheId INTEGER,
                            FOREIGN KEY (TacheId) REFERENCES BacklogItems(Id)
                        );";
                    cmd.ExecuteNonQuery();
                }
                
                // Vérifier et ajouter PeutModifierTaches si manquant
                cmd.CommandText = @"SELECT COUNT(*) FROM pragma_table_info('Roles') WHERE name='PeutModifierTaches';";
                var hasPeutModifierTaches = Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                
                if (!hasPeutModifierTaches)
                {
                    cmd.CommandText = @"ALTER TABLE Roles ADD COLUMN PeutModifierTaches INTEGER NOT NULL DEFAULT 1;";
                    cmd.ExecuteNonQuery();
                    
                    // Mettre à jour les valeurs par défaut selon les rôles
                    cmd.CommandText = @"
                        UPDATE Roles SET PeutModifierTaches = 1 WHERE Type IN (0, 2);
                        UPDATE Roles SET PeutModifierTaches = 1 WHERE Type = 3;
                        UPDATE Roles SET PeutModifierTaches = 0 WHERE Type = 1;
                    ";
                    cmd.ExecuteNonQuery();
                }
                
                // Vérifier et ajouter PeutSupprimerTaches si manquant
                cmd.CommandText = @"SELECT COUNT(*) FROM pragma_table_info('Roles') WHERE name='PeutSupprimerTaches';";
                var hasPeutSupprimerTaches = Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                
                if (!hasPeutSupprimerTaches)
                {
                    cmd.CommandText = @"ALTER TABLE Roles ADD COLUMN PeutSupprimerTaches INTEGER NOT NULL DEFAULT 0;";
                    cmd.ExecuteNonQuery();
                    
                    // Mettre à jour les valeurs par défaut selon les rôles
                    cmd.CommandText = @"
                        UPDATE Roles SET PeutSupprimerTaches = 1 WHERE Type IN (0, 2);
                        UPDATE Roles SET PeutSupprimerTaches = 0 WHERE Type IN (1, 3);
                    ";
                    cmd.ExecuteNonQuery();
                }
                
                // Vérifier et ajouter Specifications si manquant dans Demandes
                cmd.CommandText = @"SELECT COUNT(*) FROM pragma_table_info('Demandes') WHERE name='Specifications';";
                var hasSpecifications = Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                
                if (!hasSpecifications)
                {
                    cmd.CommandText = @"ALTER TABLE Demandes ADD COLUMN Specifications TEXT;";
                    cmd.ExecuteNonQuery();
                }
                
                // Vérifier et ajouter ContexteMetier si manquant
                cmd.CommandText = @"SELECT COUNT(*) FROM pragma_table_info('Demandes') WHERE name='ContexteMetier';";
                var hasContexteMetier = Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                
                if (!hasContexteMetier)
                {
                    cmd.CommandText = @"ALTER TABLE Demandes ADD COLUMN ContexteMetier TEXT;";
                    cmd.ExecuteNonQuery();
                }
                
                // Vérifier et ajouter BeneficesAttendus si manquant
                cmd.CommandText = @"SELECT COUNT(*) FROM pragma_table_info('Demandes') WHERE name='BeneficesAttendus';";
                var hasBeneficesAttendus = Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                
                if (!hasBeneficesAttendus)
                {
                    cmd.CommandText = @"ALTER TABLE Demandes ADD COLUMN BeneficesAttendus TEXT;";
                    cmd.ExecuteNonQuery();
                }
                
                // Vérifier et ajouter ProjetId si manquant
                cmd.CommandText = @"SELECT COUNT(*) FROM pragma_table_info('Demandes') WHERE name='ProjetId';";
                var hasProjetId = Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                
                if (!hasProjetId)
                {
                    cmd.CommandText = @"ALTER TABLE Demandes ADD COLUMN ProjetId INTEGER REFERENCES Projets(Id);";
                    cmd.ExecuteNonQuery();
                }
                
                // Vérifier et créer la table CRA si elle n'existe pas
                cmd.CommandText = @"SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='CRA';";
                var hasCRATable = Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                
                if (!hasCRATable)
                {
                    cmd.CommandText = @"
                        CREATE TABLE IF NOT EXISTS CRA (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            BacklogItemId INTEGER NOT NULL,
                            DevId INTEGER NOT NULL,
                            Date TEXT NOT NULL,
                            HeuresTravaillees REAL NOT NULL,
                            Commentaire TEXT,
                            DateCreation TEXT NOT NULL,
                            EstPrevisionnel INTEGER DEFAULT 0,
                            EstValide INTEGER DEFAULT 0,
                            FOREIGN KEY (BacklogItemId) REFERENCES BacklogItems(Id) ON DELETE CASCADE,
                            FOREIGN KEY (DevId) REFERENCES Utilisateurs(Id)
                        );
                        
                        CREATE INDEX IF NOT EXISTS idx_cra_backlogitem ON CRA(BacklogItemId);
                        CREATE INDEX IF NOT EXISTS idx_cra_dev ON CRA(DevId);
                        CREATE INDEX IF NOT EXISTS idx_cra_date ON CRA(Date);
                    ";
                    cmd.ExecuteNonQuery();
                }
                
                // Vérifier et ajouter EstValide à la table CRA si manquant
                cmd.CommandText = @"SELECT COUNT(*) FROM pragma_table_info('CRA') WHERE name='EstValide';";
                var hasEstValide = Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                
                if (!hasEstValide)
                {
                    cmd.CommandText = @"ALTER TABLE CRA ADD COLUMN EstValide INTEGER DEFAULT 0;";
                    cmd.ExecuteNonQuery();
                    
                    // Migration : marquer tous les CRA existants comme validés (sauf les prévisionnels dans le futur)
                    cmd.CommandText = @"
                        UPDATE CRA 
                        SET EstValide = CASE 
                            WHEN EstPrevisionnel = 0 THEN 1 
                            WHEN date(Date) < date('now') THEN 0
                            ELSE 0
                        END;
                    ";
                    cmd.ExecuteNonQuery();
                }
                
                // Vérifier et ajouter DevSupporte si manquant dans BacklogItems
                cmd.CommandText = @"SELECT COUNT(*) FROM pragma_table_info('BacklogItems') WHERE name='DevSupporte';";
                var hasDevSupporte = Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                
                if (!hasDevSupporte)
                {
                    cmd.CommandText = @"ALTER TABLE BacklogItems ADD COLUMN DevSupporte INTEGER REFERENCES Utilisateurs(Id);";
                    cmd.ExecuteNonQuery();
                }
                
                // Vérifier et ajouter TacheSupportee si manquant dans BacklogItems
                cmd.CommandText = @"SELECT COUNT(*) FROM pragma_table_info('BacklogItems') WHERE name='TacheSupportee';";
                var hasTacheSupportee = Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                
                if (!hasTacheSupportee)
                {
                    cmd.CommandText = @"ALTER TABLE BacklogItems ADD COLUMN TacheSupportee INTEGER REFERENCES BacklogItems(Id);";
                    cmd.ExecuteNonQuery();
                }

                // Vérifier et ajouter EstPrevisionnel si manquant dans CRA
                cmd.CommandText = @"SELECT COUNT(*) FROM pragma_table_info('CRA') WHERE name='EstPrevisionnel';";
                var hasEstPrevisionnel = Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                
                if (!hasEstPrevisionnel)
                {
                    cmd.CommandText = @"ALTER TABLE CRA ADD COLUMN EstPrevisionnel INTEGER DEFAULT 0;";
                    cmd.ExecuteNonQuery();
                }

                // Vérifier et ajouter CouleurHex si manquant dans Projets
                cmd.CommandText = @"SELECT COUNT(*) FROM pragma_table_info('Projets') WHERE name='CouleurHex';";
                var hasCouleurHex = Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                
                if (!hasCouleurHex)
                {
                    cmd.CommandText = @"ALTER TABLE Projets ADD COLUMN CouleurHex TEXT DEFAULT '#00915A';";
                    cmd.ExecuteNonQuery();
                }

                // Vérifier et ajouter DateFinPrevue si manquant dans Projets (renommage de DateFin)
                cmd.CommandText = @"SELECT COUNT(*) FROM pragma_table_info('Projets') WHERE name='DateFinPrevue';";
                var hasDateFinPrevue = Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                
                if (!hasDateFinPrevue)
                {
                    cmd.CommandText = @"ALTER TABLE Projets ADD COLUMN DateFinPrevue TEXT;";
                    cmd.ExecuteNonQuery();
                    
                    // Copier les données de DateFin vers DateFinPrevue si DateFin existe
                    cmd.CommandText = @"UPDATE Projets SET DateFinPrevue = DateFin WHERE DateFin IS NOT NULL;";
                    cmd.ExecuteNonQuery();
                }
                
                // Vérifier et créer la table ChatConversations si elle n'existe pas
                cmd.CommandText = @"SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='ChatConversations';";
                var hasChatConversationsTable = Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                
                if (!hasChatConversationsTable)
                {
                    cmd.CommandText = @"
                        CREATE TABLE ChatConversations (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            UserId INTEGER NOT NULL,
                            Username TEXT NOT NULL,
                            DateDebut TEXT NOT NULL,
                            DateDernierMessage TEXT NOT NULL,
                            NombreMessages INTEGER NOT NULL DEFAULT 0,
                            FOREIGN KEY (UserId) REFERENCES Utilisateurs(Id)
                        );
                        
                        CREATE INDEX IF NOT EXISTS idx_chatconv_userid ON ChatConversations(UserId);
                        CREATE INDEX IF NOT EXISTS idx_chatconv_date ON ChatConversations(DateDernierMessage);
                    ";
                    cmd.ExecuteNonQuery();
                }
                
                // Vérifier et créer la table ChatMessages si elle n'existe pas
                cmd.CommandText = @"SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='ChatMessages';";
                var hasChatMessagesTable = Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                
                if (!hasChatMessagesTable)
                {
                    cmd.CommandText = @"
                        CREATE TABLE ChatMessages (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            ConversationId INTEGER NOT NULL,
                            UserId INTEGER NOT NULL,
                            Username TEXT NOT NULL,
                            IsUser INTEGER NOT NULL,
                            Message TEXT NOT NULL,
                            DateMessage TEXT NOT NULL,
                            Reaction TEXT,
                            FOREIGN KEY (ConversationId) REFERENCES ChatConversations(Id) ON DELETE CASCADE,
                            FOREIGN KEY (UserId) REFERENCES Utilisateurs(Id)
                        );
                        
                        CREATE INDEX IF NOT EXISTS idx_chatmsg_convid ON ChatMessages(ConversationId);
                        CREATE INDEX IF NOT EXISTS idx_chatmsg_date ON ChatMessages(DateMessage);
                    ";
                    cmd.ExecuteNonQuery();
                }
                
                // ========== PHASE 1 : Gestion des Équipes ==========
                
                // Vérifier et créer la table Equipes si elle n'existe pas
                cmd.CommandText = @"SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='Equipes';";
                var hasEquipesTable = Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                
                if (!hasEquipesTable)
                {
                    cmd.CommandText = @"
                        CREATE TABLE Equipes (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            Nom TEXT NOT NULL,
                            Code TEXT NOT NULL UNIQUE,
                            Description TEXT,
                            PerimetreFonctionnel TEXT,
                            ManagerId INTEGER,
                            Contact TEXT,
                            Actif INTEGER NOT NULL DEFAULT 1,
                            DateCreation TEXT NOT NULL,
                            FOREIGN KEY (ManagerId) REFERENCES Utilisateurs(Id)
                        );
                        
                        CREATE INDEX IF NOT EXISTS idx_equipes_code ON Equipes(Code);
                        CREATE INDEX IF NOT EXISTS idx_equipes_manager ON Equipes(ManagerId);
                    ";
                    cmd.ExecuteNonQuery();
                    
                    // Initialiser les 9 équipes prédéfinies
                    cmd.CommandText = @"
                        INSERT INTO Equipes (Nom, Code, Description, PerimetreFonctionnel, Actif, DateCreation)
                        VALUES 
                        ('Transformation & Implementation', 'TRANSFO_IMPLEM', 'Équipe de transformation et d''implémentation des projets stratégiques', 'Transformation digitale, implémentation de solutions', 1, datetime('now')),
                        ('IT Assets Management', 'IT_ASSETS', 'Gestion des actifs IT et infrastructure', 'Gestion d''actifs, infrastructure IT', 1, datetime('now')),
                        ('Process, Control & Compliance', 'PCC', 'Équipe en charge des processus, contrôles et conformité', 'Processus métier, contrôles, conformité réglementaire', 1, datetime('now')),
                        ('Change BAU', 'CHANGE_BAU', 'Gestion du changement et support BAU', 'Change management, Business As Usual', 1, datetime('now')),
                        ('Watchtower / Risk Monitoring', 'WATCHTOWER', 'Surveillance et monitoring des risques', 'Surveillance des risques, monitoring opérationnel', 1, datetime('now')),
                        ('TCS / IM', 'TCS_IM', 'Third-Party & Integration Management', 'Gestion tiers et intégration de services', 1, datetime('now')),
                        ('L1 Support / First Line', 'L1_SUPPORT', 'Support de premier niveau', 'Support utilisateur, first line', 1, datetime('now')),
                        ('Data Office / Data Management', 'DATA_OFFICE', 'Gestion et gouvernance des données', 'Data management, gouvernance données', 1, datetime('now')),
                        ('Tactical Solutions / Rapid Delivery', 'TACTICAL_SOLUTIONS', 'Solutions tactiques et livraison rapide', 'Solutions rapides, développement tactique', 1, datetime('now'));
                    ";
                    cmd.ExecuteNonQuery();
                }
                
                // Vérifier et ajouter EquipeId si manquant dans Utilisateurs
                cmd.CommandText = @"SELECT COUNT(*) FROM pragma_table_info('Utilisateurs') WHERE name='EquipeId';";
                var hasEquipeId = Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                
                if (!hasEquipeId)
                {
                    cmd.CommandText = @"ALTER TABLE Utilisateurs ADD COLUMN EquipeId INTEGER REFERENCES Equipes(Id);";
                    cmd.ExecuteNonQuery();
                    
                    // Créer un index pour la performance
                    cmd.CommandText = @"CREATE INDEX IF NOT EXISTS idx_utilisateurs_equipe ON Utilisateurs(EquipeId);";
                    cmd.ExecuteNonQuery();
                }
                
                // Vérifier et ajouter Statut si manquant dans Utilisateurs
                cmd.CommandText = @"SELECT COUNT(*) FROM pragma_table_info('Utilisateurs') WHERE name='Statut';";
                var hasStatut = Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                
                if (!hasStatut)
                {
                    cmd.CommandText = @"ALTER TABLE Utilisateurs ADD COLUMN Statut TEXT DEFAULT 'BAU';";
                    cmd.ExecuteNonQuery();
                    
                    // Initialiser les utilisateurs existants avec 'BAU'
                    cmd.CommandText = @"UPDATE Utilisateurs SET Statut = 'BAU' WHERE Statut IS NULL;";
                    cmd.ExecuteNonQuery();
                }
                
                // Vérifier et ajouter EquipesAssigneesIds si manquant dans Projets
                cmd.CommandText = @"SELECT COUNT(*) FROM pragma_table_info('Projets') WHERE name='EquipesAssigneesIds';";
                var hasEquipesAssigneesIds = Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                
                if (!hasEquipesAssigneesIds)
                {
                    cmd.CommandText = @"ALTER TABLE Projets ADD COLUMN EquipesAssigneesIds TEXT;";
                    cmd.ExecuteNonQuery();
                    
                    // Initialiser avec un tableau JSON vide pour les projets existants
                    cmd.CommandText = @"UPDATE Projets SET EquipesAssigneesIds = '[]' WHERE EquipesAssigneesIds IS NULL;";
                    cmd.ExecuteNonQuery();
                }
                
                // ========== PHASE 2 : Gestion des Programmes ==========
                
                // Vérifier et créer la table Programmes si elle n'existe pas
                cmd.CommandText = @"SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='Programmes';";
                var hasProgrammesTable = Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                
                if (!hasProgrammesTable)
                {
                    cmd.CommandText = @"
                        CREATE TABLE Programmes (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            Nom TEXT NOT NULL,
                            Code TEXT UNIQUE,
                            Description TEXT,
                            Objectifs TEXT,
                            ResponsableId INTEGER,
                            DateDebut TEXT,
                            DateFinCible TEXT,
                            StatutGlobal TEXT,
                            Actif INTEGER NOT NULL DEFAULT 1,
                            DateCreation TEXT NOT NULL,
                            FOREIGN KEY (ResponsableId) REFERENCES Utilisateurs(Id)
                        );
                        
                        CREATE INDEX IF NOT EXISTS idx_programmes_code ON Programmes(Code);
                        CREATE INDEX IF NOT EXISTS idx_programmes_responsable ON Programmes(ResponsableId);
                    ";
                    cmd.ExecuteNonQuery();
                    
                    // Initialiser les 3 programmes prédéfinis
                    cmd.CommandText = @"
                        INSERT INTO Programmes (Nom, Code, Description, StatutGlobal, Actif, DateCreation)
                        VALUES 
                        ('DWINGS', 'DWG', 'Programme de digitalisation et automatisation', 'On Track', 1, datetime('now')),
                        ('E2E BG Program', 'E2E_BG', 'Programme End-to-End Business Growth', 'On Track', 1, datetime('now')),
                        ('TOM Europe', 'TOM_EUR', 'Target Operating Model Europe', 'On Track', 1, datetime('now'));
                    ";
                    cmd.ExecuteNonQuery();
                }
                
                // Vérifier et ajouter ProgrammeId si manquant dans Projets
                cmd.CommandText = @"SELECT COUNT(*) FROM pragma_table_info('Projets') WHERE name='ProgrammeId';";
                var hasProgrammeId = Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                
                if (!hasProgrammeId)
                {
                    cmd.CommandText = @"ALTER TABLE Projets ADD COLUMN ProgrammeId INTEGER REFERENCES Programmes(Id);";
                    cmd.ExecuteNonQuery();
                    
                    // Créer un index pour la performance
                    cmd.CommandText = @"CREATE INDEX IF NOT EXISTS idx_projets_programme ON Projets(ProgrammeId);";
                    cmd.ExecuteNonQuery();
                }
                
                // ========== Enrichissement Projets avec champs Phase 2 ==========
                
                // Vérifier et ajouter les colonnes Phase 2 dans Projets
                cmd.CommandText = @"SELECT COUNT(*) FROM pragma_table_info('Projets') WHERE name='Priorite';";
                var hasProjetsPriorite = Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                
                if (!hasProjetsPriorite)
                {
                    cmd.CommandText = @"
                        ALTER TABLE Projets ADD COLUMN EstImplemente INTEGER DEFAULT 0;
                        ALTER TABLE Projets ADD COLUMN TypeProjet TEXT;
                        ALTER TABLE Projets ADD COLUMN Categorie TEXT;
                        ALTER TABLE Projets ADD COLUMN Priorite TEXT;
                        ALTER TABLE Projets ADD COLUMN Drivers TEXT;
                        ALTER TABLE Projets ADD COLUMN Ambition TEXT;
                        ALTER TABLE Projets ADD COLUMN Beneficiaires TEXT;
                        ALTER TABLE Projets ADD COLUMN GainsTemps TEXT;
                        ALTER TABLE Projets ADD COLUMN GainsFinanciers TEXT;
                        ALTER TABLE Projets ADD COLUMN LeadProjet TEXT;
                        ALTER TABLE Projets ADD COLUMN Timeline TEXT;
                        ALTER TABLE Projets ADD COLUMN TargetDelivery TEXT;
                        ALTER TABLE Projets ADD COLUMN PerimetreProchainComite TEXT;
                        ALTER TABLE Projets ADD COLUMN NextActions TEXT;
                        ALTER TABLE Projets ADD COLUMN StatutRAG TEXT;
                    ";
                    cmd.ExecuteNonQuery();
                }
                
                // ========== Enrichissement Demandes avec structure Programme ==========
                
                // Vérifier et ajouter ProgrammeId dans Demandes
                cmd.CommandText = @"SELECT COUNT(*) FROM pragma_table_info('Demandes') WHERE name='ProgrammeId';";
                var hasDemandesProgrammeId = Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                
                if (!hasDemandesProgrammeId)
                {
                    cmd.CommandText = @"
                        ALTER TABLE Demandes ADD COLUMN ProgrammeId INTEGER REFERENCES Programmes(Id);
                        ALTER TABLE Demandes ADD COLUMN Priorite TEXT;
                        ALTER TABLE Demandes ADD COLUMN Drivers TEXT;
                        ALTER TABLE Demandes ADD COLUMN Ambition TEXT;
                        ALTER TABLE Demandes ADD COLUMN Beneficiaires TEXT;
                        ALTER TABLE Demandes ADD COLUMN GainsTemps TEXT;
                        ALTER TABLE Demandes ADD COLUMN GainsFinanciers TEXT;
                        ALTER TABLE Demandes ADD COLUMN LeadProjet TEXT;
                        ALTER TABLE Demandes ADD COLUMN TypeProjet TEXT;
                        ALTER TABLE Demandes ADD COLUMN Categorie TEXT;
                        ALTER TABLE Demandes ADD COLUMN EstImplemente INTEGER DEFAULT 0;
                    ";
                    cmd.ExecuteNonQuery();
                    
                    // Créer un index pour la performance
                    cmd.CommandText = @"CREATE INDEX IF NOT EXISTS idx_demandes_programme ON Demandes(ProgrammeId);";
                    cmd.ExecuteNonQuery();
                }
                
                // Vérifier et ajouter EquipesAssigneesIds dans Demandes
                cmd.CommandText = @"SELECT COUNT(*) FROM pragma_table_info('Demandes') WHERE name='EquipesAssigneesIds';";
                var hasDemandesEquipesAssigneesIds = Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                
                if (!hasDemandesEquipesAssigneesIds)
                {
                    cmd.CommandText = @"ALTER TABLE Demandes ADD COLUMN EquipesAssigneesIds TEXT;";
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // READ OPERATIONS (Read-Only Connection)
        public List<Role> GetRoles()
        {
            var roles = new List<Role>();
            using (var conn = GetConnection())
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(@"SELECT Id, Nom, Type, PeutCreerDemandes, PeutChiffrer, 
                    PeutPrioriser, PeutGererUtilisateurs, PeutVoirKPI, PeutGererReferentiels, 
                    PeutModifierTaches, PeutSupprimerTaches, Actif FROM Roles WHERE Actif = 1", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        roles.Add(new Role
                        {
                            Id = reader.GetInt32(0),
                            Nom = reader.GetString(1),
                            Type = (RoleType)reader.GetInt32(2),
                            PeutCreerDemandes = reader.GetInt32(3) == 1,
                            PeutChiffrer = reader.GetInt32(4) == 1,
                            PeutPrioriser = reader.GetInt32(5) == 1,
                            PeutGererUtilisateurs = reader.GetInt32(6) == 1,
                            PeutVoirKPI = reader.GetInt32(7) == 1,
                            PeutGererReferentiels = reader.GetInt32(8) == 1,
                            PeutModifierTaches = reader.GetInt32(9) == 1,
                            PeutSupprimerTaches = reader.GetInt32(10) == 1,
                            Actif = reader.GetInt32(11) == 1
                        });
                    }
                }
            }
            return roles;
        }

        public List<Utilisateur> GetUtilisateurs()
        {
            var utilisateurs = new List<Utilisateur>();
            using (var conn = GetConnection())
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("SELECT Id, UsernameWindows, Nom, Prenom, Email, RoleId, Actif, DateCreation, DateDerniereConnexion, EquipeId, Statut FROM Utilisateurs", conn))
                using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    utilisateurs.Add(new Utilisateur
                    {
                        Id = reader.GetInt32(0),
                        UsernameWindows = reader.GetString(1),
                        Nom = reader.GetString(2),
                        Prenom = reader.GetString(3),
                        Email = reader.IsDBNull(4) ? null : reader.GetString(4),
                        RoleId = reader.GetInt32(5),
                        Actif = reader.GetInt32(6) == 1,
                        DateCreation = DateTime.Parse(reader.GetString(7)),
                        DateDerniereConnexion = reader.IsDBNull(8) ? (DateTime?)null : DateTime.Parse(reader.GetString(8)),
                        EquipeId = reader.IsDBNull(9) ? (int?)null : reader.GetInt32(9),
                        Statut = reader.IsDBNull(10) ? "BAU" : reader.GetString(10)
                    });
                }
            }
            }
            return utilisateurs;
        }

        public List<Demande> GetDemandes()
        {
            var demandes = new List<Demande>();
            using (var conn = GetConnection())
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(@"SELECT Id, Titre, Description, Specifications, ContexteMetier, BeneficesAttendus,
                    DemandeurId, BusinessAnalystId, ChefProjetId, DevChiffreurId, ProjetId, Type, Criticite, Statut, DateCreation,
                    DateValidationChiffrage, DateAcceptation, DateLivraison, ChiffrageEstimeJours, 
                    ChiffrageReelJours, DatePrevisionnelleImplementation, JustificationRefus, EstArchivee,
                    ProgrammeId, Priorite, Drivers, Ambition, Beneficiaires, GainsTemps, GainsFinanciers, 
                    LeadProjet, TypeProjet, Categorie, EstImplemente, EquipesAssigneesIds 
                    FROM Demandes", conn))
                using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    demandes.Add(new Demande
                    {
                        Id = reader.GetInt32(0),
                        Titre = reader.GetString(1),
                        Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                        Specifications = reader.IsDBNull(3) ? null : reader.GetString(3),
                        ContexteMetier = reader.IsDBNull(4) ? null : reader.GetString(4),
                        BeneficesAttendus = reader.IsDBNull(5) ? null : reader.GetString(5),
                        DemandeurId = reader.GetInt32(6),
                        BusinessAnalystId = reader.IsDBNull(7) ? (int?)null : reader.GetInt32(7),
                        ChefProjetId = reader.IsDBNull(8) ? (int?)null : reader.GetInt32(8),
                        DevChiffreurId = reader.IsDBNull(9) ? (int?)null : reader.GetInt32(9),
                        ProjetId = reader.IsDBNull(10) ? (int?)null : reader.GetInt32(10),
                        Type = (TypeDemande)reader.GetInt32(11),
                        Criticite = (Criticite)reader.GetInt32(12),
                        Statut = (StatutDemande)reader.GetInt32(13),
                        DateCreation = DateTime.Parse(reader.GetString(14)),
                        DateValidationChiffrage = reader.IsDBNull(15) ? (DateTime?)null : DateTime.Parse(reader.GetString(15)),
                        DateAcceptation = reader.IsDBNull(16) ? (DateTime?)null : DateTime.Parse(reader.GetString(16)),
                        DateLivraison = reader.IsDBNull(17) ? (DateTime?)null : DateTime.Parse(reader.GetString(17)),
                        ChiffrageEstimeJours = reader.IsDBNull(18) ? (double?)null : reader.GetDouble(18),
                        ChiffrageReelJours = reader.IsDBNull(19) ? (double?)null : reader.GetDouble(19),
                        DatePrevisionnelleImplementation = reader.IsDBNull(20) ? (DateTime?)null : DateTime.Parse(reader.GetString(20)),
                        JustificationRefus = reader.IsDBNull(21) ? null : reader.GetString(21),
                        EstArchivee = reader.GetInt32(22) == 1,
                        // Phase 2 : Nouveaux champs
                        ProgrammeId = reader.IsDBNull(23) ? (int?)null : reader.GetInt32(23),
                        Priorite = reader.IsDBNull(24) ? null : reader.GetString(24),
                        Drivers = reader.IsDBNull(25) ? null : reader.GetString(25),
                        Ambition = reader.IsDBNull(26) ? null : reader.GetString(26),
                        Beneficiaires = reader.IsDBNull(27) ? null : reader.GetString(27),
                        GainsTemps = reader.IsDBNull(28) ? null : reader.GetString(28),
                        GainsFinanciers = reader.IsDBNull(29) ? null : reader.GetString(29),
                        LeadProjet = reader.IsDBNull(30) ? null : reader.GetString(30),
                        TypeProjet = reader.IsDBNull(31) ? null : reader.GetString(31),
                        Categorie = reader.IsDBNull(32) ? null : reader.GetString(32),
                        EstImplemente = !reader.IsDBNull(33) && reader.GetInt32(33) == 1,
                        EquipesAssigneesIds = reader.IsDBNull(34) ? new List<int>() : 
                            System.Text.Json.JsonSerializer.Deserialize<List<int>>(reader.GetString(34)) ?? new List<int>()
                    });
                }
            }
            }
            return demandes;
        }

        // WRITE OPERATIONS (Write Connection - opened, used, closed immediately)
        public Role AddOrUpdateRole(Role role)
        {
            using (var conn = GetConnectionForWrite())
            {
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        using (var cmd = conn.CreateCommand())
                    {
                        if (role.Id == 0)
                        {
                            cmd.CommandText = @"INSERT INTO Roles (Nom, Type, PeutCreerDemandes, PeutChiffrer, PeutPrioriser, 
                                PeutGererUtilisateurs, PeutVoirKPI, PeutGererReferentiels, Actif) 
                                VALUES (@Nom, @Type, @PeutCreerDemandes, @PeutChiffrer, @PeutPrioriser, 
                                @PeutGererUtilisateurs, @PeutVoirKPI, @PeutGererReferentiels, @Actif);
                                SELECT last_insert_rowid();";
                        }
                        else
                        {
                            cmd.CommandText = @"UPDATE Roles SET Nom = @Nom, Type = @Type, PeutCreerDemandes = @PeutCreerDemandes,
                                PeutChiffrer = @PeutChiffrer, PeutPrioriser = @PeutPrioriser, PeutGererUtilisateurs = @PeutGererUtilisateurs,
                                PeutVoirKPI = @PeutVoirKPI, PeutGererReferentiels = @PeutGererReferentiels, Actif = @Actif 
                                WHERE Id = @Id";
                            cmd.Parameters.AddWithValue("@Id", role.Id);
                        }

                        cmd.Parameters.AddWithValue("@Nom", role.Nom);
                        cmd.Parameters.AddWithValue("@Type", (int)role.Type);
                        cmd.Parameters.AddWithValue("@PeutCreerDemandes", role.PeutCreerDemandes ? 1 : 0);
                        cmd.Parameters.AddWithValue("@PeutChiffrer", role.PeutChiffrer ? 1 : 0);
                        cmd.Parameters.AddWithValue("@PeutPrioriser", role.PeutPrioriser ? 1 : 0);
                        cmd.Parameters.AddWithValue("@PeutGererUtilisateurs", role.PeutGererUtilisateurs ? 1 : 0);
                        cmd.Parameters.AddWithValue("@PeutVoirKPI", role.PeutVoirKPI ? 1 : 0);
                        cmd.Parameters.AddWithValue("@PeutGererReferentiels", role.PeutGererReferentiels ? 1 : 0);
                        cmd.Parameters.AddWithValue("@Actif", role.Actif ? 1 : 0);

                        if (role.Id == 0)
                        {
                            role.Id = Convert.ToInt32(cmd.ExecuteScalar());
                        }
                        else
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }
                    transaction.Commit();
                    return role;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            }
        }

        public void UpdateRole(Role role)
        {
            using (var conn = GetConnectionForWrite())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"UPDATE Roles SET 
                        PeutCreerDemandes = @PeutCreerDemandes,
                        PeutChiffrer = @PeutChiffrer,
                        PeutPrioriser = @PeutPrioriser,
                        PeutGererUtilisateurs = @PeutGererUtilisateurs,
                        PeutVoirKPI = @PeutVoirKPI,
                        PeutGererReferentiels = @PeutGererReferentiels,
                        PeutModifierTaches = @PeutModifierTaches,
                        PeutSupprimerTaches = @PeutSupprimerTaches
                        WHERE Id = @Id";
                    
                    cmd.Parameters.AddWithValue("@Id", role.Id);
                    cmd.Parameters.AddWithValue("@PeutCreerDemandes", role.PeutCreerDemandes ? 1 : 0);
                    cmd.Parameters.AddWithValue("@PeutChiffrer", role.PeutChiffrer ? 1 : 0);
                    cmd.Parameters.AddWithValue("@PeutPrioriser", role.PeutPrioriser ? 1 : 0);
                    cmd.Parameters.AddWithValue("@PeutGererUtilisateurs", role.PeutGererUtilisateurs ? 1 : 0);
                    cmd.Parameters.AddWithValue("@PeutVoirKPI", role.PeutVoirKPI ? 1 : 0);
                    cmd.Parameters.AddWithValue("@PeutGererReferentiels", role.PeutGererReferentiels ? 1 : 0);
                    cmd.Parameters.AddWithValue("@PeutModifierTaches", role.PeutModifierTaches ? 1 : 0);
                    cmd.Parameters.AddWithValue("@PeutSupprimerTaches", role.PeutSupprimerTaches ? 1 : 0);
                    
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public Utilisateur AddOrUpdateUtilisateur(Utilisateur utilisateur)
        {
            using (var conn = GetConnectionForWrite())
            {
                using (var transaction = conn.BeginTransaction())
            {
                try
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        if (utilisateur.Id == 0)
                        {
                            cmd.CommandText = @"INSERT INTO Utilisateurs (UsernameWindows, Nom, Prenom, Email, RoleId, EquipeId, Statut, Actif, DateCreation, DateDerniereConnexion)
                                VALUES (@UsernameWindows, @Nom, @Prenom, @Email, @RoleId, @EquipeId, @Statut, @Actif, @DateCreation, @DateDerniereConnexion);
                                SELECT last_insert_rowid();";
                        }
                        else
                        {
                            cmd.CommandText = @"UPDATE Utilisateurs SET UsernameWindows = @UsernameWindows, Nom = @Nom, Prenom = @Prenom,
                                Email = @Email, RoleId = @RoleId, EquipeId = @EquipeId, Statut = @Statut, Actif = @Actif, DateDerniereConnexion = @DateDerniereConnexion
                                WHERE Id = @Id";
                            cmd.Parameters.AddWithValue("@Id", utilisateur.Id);
                        }

                        cmd.Parameters.AddWithValue("@UsernameWindows", utilisateur.UsernameWindows);
                        cmd.Parameters.AddWithValue("@Nom", utilisateur.Nom);
                        cmd.Parameters.AddWithValue("@Prenom", utilisateur.Prenom);
                        cmd.Parameters.AddWithValue("@Email", utilisateur.Email ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@RoleId", utilisateur.RoleId);
                        cmd.Parameters.AddWithValue("@EquipeId", utilisateur.EquipeId.HasValue ? (object)utilisateur.EquipeId.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@Statut", utilisateur.Statut ?? "BAU");
                        cmd.Parameters.AddWithValue("@Actif", utilisateur.Actif ? 1 : 0);
                        cmd.Parameters.AddWithValue("@DateCreation", utilisateur.DateCreation.ToString("yyyy-MM-dd HH:mm:ss"));
                        cmd.Parameters.AddWithValue("@DateDerniereConnexion", utilisateur.DateDerniereConnexion?.ToString("yyyy-MM-dd HH:mm:ss") ?? (object)DBNull.Value);

                        if (utilisateur.Id == 0)
                        {
                            utilisateur.Id = Convert.ToInt32(cmd.ExecuteScalar());
                        }
                        else
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }
                    transaction.Commit();
                    return utilisateur;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            }
        }

        public void AddUtilisateur(Utilisateur utilisateur)
        {
            utilisateur.Id = 0; // Force insertion
            AddOrUpdateUtilisateur(utilisateur);
        }

        public void UpdateUtilisateur(Utilisateur utilisateur)
        {
            AddOrUpdateUtilisateur(utilisateur);
        }

        public void DeleteUtilisateur(int id)
        {
            using (var conn = GetConnectionForWrite())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM Utilisateurs WHERE Id = @Id";
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public Demande AddOrUpdateDemande(Demande demande)
        {
            using (var conn = GetConnectionForWrite())
            {
                using (var transaction = conn.BeginTransaction())
            {
                try
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        if (demande.Id == 0)
                        {
                            cmd.CommandText = @"INSERT INTO Demandes (Titre, Description, Specifications, ContexteMetier, BeneficesAttendus,
                                DemandeurId, BusinessAnalystId, ChefProjetId, DevChiffreurId, ProjetId,
                                Type, Criticite, Statut, DateCreation, DateValidationChiffrage, DateAcceptation, DateLivraison,
                                ChiffrageEstimeJours, ChiffrageReelJours, DatePrevisionnelleImplementation, JustificationRefus, EstArchivee,
                                ProgrammeId, Priorite, Drivers, Ambition, Beneficiaires, GainsTemps, GainsFinanciers, LeadProjet, TypeProjet, Categorie, EstImplemente, EquipesAssigneesIds)
                                VALUES (@Titre, @Description, @Specifications, @ContexteMetier, @BeneficesAttendus,
                                @DemandeurId, @BusinessAnalystId, @ChefProjetId, @DevChiffreurId, @ProjetId,
                                @Type, @Criticite, @Statut, @DateCreation, @DateValidationChiffrage, @DateAcceptation, @DateLivraison,
                                @ChiffrageEstimeJours, @ChiffrageReelJours, @DatePrevisionnelleImplementation, @JustificationRefus, @EstArchivee,
                                @ProgrammeId, @Priorite, @Drivers, @Ambition, @Beneficiaires, @GainsTemps, @GainsFinanciers, @LeadProjet, @TypeProjet, @Categorie, @EstImplemente, @EquipesAssigneesIds);
                                SELECT last_insert_rowid();";
                        }
                        else
                        {
                            cmd.CommandText = @"UPDATE Demandes SET Titre = @Titre, Description = @Description, Specifications = @Specifications,
                                ContexteMetier = @ContexteMetier, BeneficesAttendus = @BeneficesAttendus, DemandeurId = @DemandeurId,
                                BusinessAnalystId = @BusinessAnalystId, ChefProjetId = @ChefProjetId, DevChiffreurId = @DevChiffreurId, ProjetId = @ProjetId,
                                Type = @Type, Criticite = @Criticite, Statut = @Statut, DateValidationChiffrage = @DateValidationChiffrage,
                                DateAcceptation = @DateAcceptation, DateLivraison = @DateLivraison, ChiffrageEstimeJours = @ChiffrageEstimeJours,
                                ChiffrageReelJours = @ChiffrageReelJours, DatePrevisionnelleImplementation = @DatePrevisionnelleImplementation,
                                JustificationRefus = @JustificationRefus, EstArchivee = @EstArchivee,
                                ProgrammeId = @ProgrammeId, Priorite = @Priorite, Drivers = @Drivers, Ambition = @Ambition, Beneficiaires = @Beneficiaires,
                                GainsTemps = @GainsTemps, GainsFinanciers = @GainsFinanciers, LeadProjet = @LeadProjet, TypeProjet = @TypeProjet, Categorie = @Categorie, EstImplemente = @EstImplemente, EquipesAssigneesIds = @EquipesAssigneesIds
                                WHERE Id = @Id";
                            cmd.Parameters.AddWithValue("@Id", demande.Id);
                        }

                        cmd.Parameters.AddWithValue("@Titre", demande.Titre);
                        cmd.Parameters.AddWithValue("@Description", demande.Description ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Specifications", demande.Specifications ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@ContexteMetier", demande.ContexteMetier ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@BeneficesAttendus", demande.BeneficesAttendus ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@DemandeurId", demande.DemandeurId);
                        cmd.Parameters.AddWithValue("@BusinessAnalystId", demande.BusinessAnalystId ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@ChefProjetId", demande.ChefProjetId ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@DevChiffreurId", demande.DevChiffreurId ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@ProjetId", demande.ProjetId ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Type", (int)demande.Type);
                        cmd.Parameters.AddWithValue("@Criticite", (int)demande.Criticite);
                        cmd.Parameters.AddWithValue("@Statut", (int)demande.Statut);
                        cmd.Parameters.AddWithValue("@DateCreation", demande.DateCreation.ToString("yyyy-MM-dd HH:mm:ss"));
                        cmd.Parameters.AddWithValue("@DateValidationChiffrage", demande.DateValidationChiffrage?.ToString("yyyy-MM-dd HH:mm:ss") ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@DateAcceptation", demande.DateAcceptation?.ToString("yyyy-MM-dd HH:mm:ss") ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@DateLivraison", demande.DateLivraison?.ToString("yyyy-MM-dd HH:mm:ss") ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@ChiffrageEstimeJours", demande.ChiffrageEstimeJours ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@ChiffrageReelJours", demande.ChiffrageReelJours ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@DatePrevisionnelleImplementation", demande.DatePrevisionnelleImplementation?.ToString("yyyy-MM-dd HH:mm:ss") ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@JustificationRefus", demande.JustificationRefus ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@EstArchivee", demande.EstArchivee ? 1 : 0);
                        
                        // Phase 2 enrichment fields
                        cmd.Parameters.AddWithValue("@ProgrammeId", demande.ProgrammeId ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Priorite", demande.Priorite ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Drivers", demande.Drivers ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Ambition", demande.Ambition ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Beneficiaires", demande.Beneficiaires ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@GainsTemps", demande.GainsTemps ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@GainsFinanciers", demande.GainsFinanciers ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@LeadProjet", demande.LeadProjet ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@TypeProjet", demande.TypeProjet ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Categorie", demande.Categorie ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@EstImplemente", demande.EstImplemente ? 1 : 0);
                        
                        // Équipes assignées (List<int> -> JSON)
                        var equipesJson = demande.EquipesAssigneesIds != null && demande.EquipesAssigneesIds.Count > 0 
                            ? System.Text.Json.JsonSerializer.Serialize(demande.EquipesAssigneesIds) 
                            : (object)DBNull.Value;
                        cmd.Parameters.AddWithValue("@EquipesAssigneesIds", equipesJson);

                        if (demande.Id == 0)
                        {
                            demande.Id = Convert.ToInt32(cmd.ExecuteScalar());
                        }
                        else
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }
                    transaction.Commit();
                    return demande;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            }
        }

        public void DeleteDemande(int id)
        {
            using (var conn = GetConnectionForWrite())
            {
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // Supprimer les commentaires associés
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = "DELETE FROM Commentaires WHERE DemandeId = @Id";
                            cmd.Parameters.AddWithValue("@Id", id);
                            cmd.ExecuteNonQuery();
                        }

                        // Supprimer la demande
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = "DELETE FROM Demandes WHERE Id = @Id";
                            cmd.Parameters.AddWithValue("@Id", id);
                            cmd.ExecuteNonQuery();
                        }

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        // Stubs pour les autres méthodes
        public List<BacklogItem> GetBacklogItems()
        {
            var items = new List<BacklogItem>();
            using (var conn = GetConnection())
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(@"SELECT Id, Titre, Description, ProjetId, DevId, Type, Priorite, Statut,
                    Points, ChiffrageHeures, TempsReelHeures, DateFinAttendue, DateDebut, DateFin, 
                    DateCreation, DateDerniereMaj, EstArchive, SprintId, DemandeId, DevSupporte, TacheSupportee 
                    FROM BacklogItems WHERE EstArchive = 0", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        items.Add(new BacklogItem
                        {
                            Id = reader.GetInt32(0),
                            Titre = reader.GetString(1),
                            Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                            ProjetId = reader.IsDBNull(3) ? (int?)null : reader.GetInt32(3),
                            DevAssigneId = reader.IsDBNull(4) ? (int?)null : reader.GetInt32(4),
                            TypeDemande = (TypeDemande)reader.GetInt32(5),
                            Priorite = (Priorite)reader.GetInt32(6),
                            Statut = (Statut)reader.GetInt32(7),
                            Complexite = reader.IsDBNull(8) ? (int?)null : reader.GetInt32(8),
                            ChiffrageHeures = reader.IsDBNull(9) ? (double?)null : reader.GetDouble(9),
                            TempsReelHeures = reader.IsDBNull(10) ? (double?)null : reader.GetDouble(10),
                            DateFinAttendue = reader.IsDBNull(11) ? (DateTime?)null : DateTime.Parse(reader.GetString(11)),
                            DateDebut = reader.IsDBNull(12) ? (DateTime?)null : DateTime.Parse(reader.GetString(12)),
                            DateFin = reader.IsDBNull(13) ? (DateTime?)null : DateTime.Parse(reader.GetString(13)),
                            DateCreation = DateTime.Parse(reader.GetString(14)),
                            DateDerniereMaj = DateTime.Parse(reader.GetString(15)),
                            EstArchive = reader.GetInt32(16) == 1,
                            SprintId = reader.IsDBNull(17) ? (int?)null : reader.GetInt32(17),
                            DemandeId = reader.IsDBNull(18) ? (int?)null : reader.GetInt32(18),
                            DevSupporte = reader.IsDBNull(19) ? (int?)null : reader.GetInt32(19),
                            TacheSupportee = reader.IsDBNull(20) ? (int?)null : reader.GetInt32(20)
                        });
                    }
                }
            }
            return items;
        }
        
        public List<BacklogItem> GetBacklog() { return GetBacklogItems(); }
        
        public List<BacklogItem> GetAllBacklogItemsIncludingArchived()
        {
            var items = new List<BacklogItem>();
            using (var conn = GetConnection())
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(@"SELECT Id, Titre, Description, ProjetId, DevId, Type, Priorite, Statut,
                    Points, ChiffrageHeures, TempsReelHeures, DateFinAttendue, DateDebut, DateFin, 
                    DateCreation, DateDerniereMaj, EstArchive, SprintId, DemandeId, DevSupporte, TacheSupportee 
                    FROM BacklogItems", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        items.Add(new BacklogItem
                        {
                            Id = reader.GetInt32(0),
                            Titre = reader.GetString(1),
                            Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                            ProjetId = reader.IsDBNull(3) ? (int?)null : reader.GetInt32(3),
                            DevAssigneId = reader.IsDBNull(4) ? (int?)null : reader.GetInt32(4),
                            TypeDemande = (TypeDemande)reader.GetInt32(5),
                            Priorite = (Priorite)reader.GetInt32(6),
                            Statut = (Statut)reader.GetInt32(7),
                            Complexite = reader.IsDBNull(8) ? (int?)null : reader.GetInt32(8),
                            ChiffrageHeures = reader.IsDBNull(9) ? (double?)null : reader.GetDouble(9),
                            TempsReelHeures = reader.IsDBNull(10) ? (double?)null : reader.GetDouble(10),
                            DateFinAttendue = reader.IsDBNull(11) ? (DateTime?)null : DateTime.Parse(reader.GetString(11)),
                            DateDebut = reader.IsDBNull(12) ? (DateTime?)null : DateTime.Parse(reader.GetString(12)),
                            DateFin = reader.IsDBNull(13) ? (DateTime?)null : DateTime.Parse(reader.GetString(13)),
                            DateCreation = DateTime.Parse(reader.GetString(14)),
                            DateDerniereMaj = DateTime.Parse(reader.GetString(15)),
                            EstArchive = reader.GetInt32(16) == 1,
                            SprintId = reader.IsDBNull(17) ? (int?)null : reader.GetInt32(17),
                            DemandeId = reader.IsDBNull(18) ? (int?)null : reader.GetInt32(18),
                            DevSupporte = reader.IsDBNull(19) ? (int?)null : reader.GetInt32(19),
                            TacheSupportee = reader.IsDBNull(20) ? (int?)null : reader.GetInt32(20)
                        });
                    }
                }
            }
            return items;
        }
        
        public List<AuditLog> GetAuditLogs()
        {
            var logs = new List<AuditLog>();
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand(@"SELECT Id, Action, UserId, Username, EntityType, EntityId, 
                        OldValue, NewValue, DateAction, Details FROM AuditLog ORDER BY DateAction DESC", conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            logs.Add(new AuditLog
                            {
                                Id = reader.GetInt32(0),
                                Action = reader.GetString(1),
                                UserId = reader.GetInt32(2),
                                Username = reader.GetString(3),
                                EntityType = reader.GetString(4),
                                EntityId = reader.IsDBNull(5) ? (int?)null : reader.GetInt32(5),
                                OldValue = reader.IsDBNull(6) ? null : reader.GetString(6),
                                NewValue = reader.IsDBNull(7) ? null : reader.GetString(7),
                                DateAction = DateTime.Parse(reader.GetString(8)),
                                Details = reader.IsDBNull(9) ? null : reader.GetString(9)
                            });
                        }
                    }
                }
            }
            catch (Exception)
            {
                // En cas d'erreur, retourner une liste vide plutôt que null
                return new List<AuditLog>();
            }
            return logs;
        }

        public void AddAuditLog(AuditLog auditLog)
        {
            using (var conn = GetConnectionForWrite())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"INSERT INTO AuditLog (Action, UserId, Username, EntityType, EntityId, 
                        OldValue, NewValue, DateAction, Details)
                        VALUES (@Action, @UserId, @Username, @EntityType, @EntityId, @OldValue, @NewValue, @DateAction, @Details)";
                    
                    cmd.Parameters.AddWithValue("@Action", auditLog.Action);
                    cmd.Parameters.AddWithValue("@UserId", auditLog.UserId);
                    cmd.Parameters.AddWithValue("@Username", auditLog.Username);
                    cmd.Parameters.AddWithValue("@EntityType", auditLog.EntityType);
                    cmd.Parameters.AddWithValue("@EntityId", auditLog.EntityId.HasValue ? (object)auditLog.EntityId.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@OldValue", auditLog.OldValue ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@NewValue", auditLog.NewValue ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@DateAction", auditLog.DateAction.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("@Details", auditLog.Details ?? (object)DBNull.Value);
                    
                    cmd.ExecuteNonQuery();
                }
            }
        }
        
        public List<Projet> GetProjets()
        {
            var projets = new List<Projet>();
            using (var conn = GetConnection())
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(@"SELECT Id, Nom, Description, DateCreation, DateDebut, DateFin, CouleurHex, Actif,
                    ProgrammeId, EstImplemente, TypeProjet, Categorie, Priorite, Drivers, Ambition, Beneficiaires,
                    GainsTemps, GainsFinanciers, LeadProjet, Timeline, TargetDelivery, PerimetreProchainComite, NextActions, StatutRAG, EquipesAssigneesIds 
                    FROM Projets ORDER BY DateCreation DESC", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        projets.Add(new Projet
                        {
                            Id = reader.GetInt32(0),
                            Nom = reader.GetString(1),
                            Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                            DateCreation = reader.IsDBNull(3) ? DateTime.Now : DateTime.Parse(reader.GetString(3)),
                            DateDebut = reader.IsDBNull(4) ? (DateTime?)null : DateTime.Parse(reader.GetString(4)),
                            DateFinPrevue = reader.IsDBNull(5) ? (DateTime?)null : DateTime.Parse(reader.GetString(5)),
                            CouleurHex = reader.IsDBNull(6) ? "#00915A" : reader.GetString(6),
                            Actif = reader.GetInt32(7) == 1,
                            // Phase 2 fields
                            ProgrammeId = reader.IsDBNull(8) ? (int?)null : reader.GetInt32(8),
                            EstImplemente = reader.IsDBNull(9) ? false : reader.GetInt32(9) == 1,
                            TypeProjet = reader.IsDBNull(10) ? null : reader.GetString(10),
                            Categorie = reader.IsDBNull(11) ? null : reader.GetString(11),
                            Priorite = reader.IsDBNull(12) ? null : reader.GetString(12),
                            Drivers = reader.IsDBNull(13) ? null : reader.GetString(13),
                            Ambition = reader.IsDBNull(14) ? null : reader.GetString(14),
                            Beneficiaires = reader.IsDBNull(15) ? null : reader.GetString(15),
                            GainsTemps = reader.IsDBNull(16) ? null : reader.GetString(16),
                            GainsFinanciers = reader.IsDBNull(17) ? null : reader.GetString(17),
                            LeadProjet = reader.IsDBNull(18) ? null : reader.GetString(18),
                            Timeline = reader.IsDBNull(19) ? null : reader.GetString(19),
                            TargetDelivery = reader.IsDBNull(20) ? null : reader.GetString(20),
                            PerimetreProchainComite = reader.IsDBNull(21) ? null : reader.GetString(21),
                            NextActions = reader.IsDBNull(22) ? null : reader.GetString(22),
                            StatutRAG = reader.IsDBNull(23) ? null : reader.GetString(23),
                            // Équipes Assignées (JSON → List<int>)
                            EquipesAssigneesIds = reader.IsDBNull(24) ? new List<int>() : System.Text.Json.JsonSerializer.Deserialize<List<int>>(reader.GetString(24))
                        });
                    }
                }
            }
            return projets;
        }
        
        public List<Sprint> GetSprints()
        {
            var sprints = new List<Sprint>();
            using (var conn = GetConnection())
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("SELECT Id, Nom, DateDebut, DateFin, Objectif, EstActif FROM Sprints", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        sprints.Add(new Sprint
                        {
                            Id = reader.GetInt32(0),
                            Nom = reader.GetString(1),
                            DateDebut = DateTime.Parse(reader.GetString(2)),
                            DateFin = DateTime.Parse(reader.GetString(3)),
                            Objectif = reader.IsDBNull(4) ? null : reader.GetString(4),
                            EstActif = reader.GetInt32(5) == 1
                        });
                    }
                }
            }
            return sprints;
        }
        
        public List<Dev> GetDevs()
        {
            var devs = new List<Dev>();
            using (var conn = GetConnection())
            {
                conn.Open();
                // Récupérer les utilisateurs avec le rôle Développeur (RoleId = 4)
                using (var cmd = new SQLiteCommand(@"
                    SELECT u.Id, u.Nom, u.UsernameWindows 
                    FROM Utilisateurs u
                    INNER JOIN Roles r ON u.RoleId = r.Id
                    WHERE r.Nom = 'Développeur' AND u.Actif = 1", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var nom = reader.GetString(1);
                        var username = reader.GetString(2);
                        devs.Add(new Dev
                        {
                            Id = reader.GetInt32(0),
                            Nom = nom,
                            Initiales = username.Length >= 2 ? username.Substring(0, 2).ToUpper() : username.ToUpper(),
                            Actif = true
                        });
                    }
                }
            }
            return devs;
        }
        
        public List<Commentaire> GetCommentaires()
        {
            var commentaires = new List<Commentaire>();
            using (var conn = GetConnection())
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM Commentaires ORDER BY DateCreation DESC";
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            commentaires.Add(new Commentaire
                            {
                                Id = reader.GetInt32(0),
                                DemandeId = reader.IsDBNull(1) ? (int?)null : reader.GetInt32(1),
                                BacklogItemId = reader.IsDBNull(2) ? (int?)null : reader.GetInt32(2),
                                AuteurId = reader.GetInt32(3),
                                Contenu = reader.GetString(4),
                                DateCreation = DateTime.Parse(reader.GetString(5)),
                                PieceJointeNom = reader.IsDBNull(6) ? null : reader.GetString(6),
                                PieceJointeChemin = reader.IsDBNull(7) ? null : reader.GetString(7)
                            });
                        }
                    }
                }
            }
            return commentaires;
        }
        public List<HistoriqueModification> GetHistoriqueModifications() { return new List<HistoriqueModification>(); }
        public List<PokerSession> GetPokerSessions() { return new List<PokerSession>(); }
        public List<PokerVote> GetPokerVotes() { return new List<PokerVote>(); }
        public List<Disponibilite> GetDisponibilites() { return new List<Disponibilite>(); }

        public BacklogItem AddOrUpdateBacklogItem(BacklogItem item)
        {
            using (var conn = GetConnectionForWrite())
            {
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        using (var cmd = conn.CreateCommand())
                        {
                            if (item.Id == 0)
                            {
                                cmd.CommandText = @"INSERT INTO BacklogItems (Titre, Description, ProjetId, DevId, Type, Priorite, Statut, 
                                    Points, ChiffrageHeures, TempsReelHeures, DateFinAttendue, DateDebut, DateFin, 
                                    DateCreation, DateDerniereMaj, EstArchive, SprintId, DemandeId, DevSupporte, TacheSupportee) 
                                    VALUES (@Titre, @Description, @ProjetId, @DevId, @Type, @Priorite, @Statut, 
                                    @Points, @ChiffrageHeures, @TempsReelHeures, @DateFinAttendue, @DateDebut, @DateFin, 
                                    @DateCreation, @DateDerniereMaj, @EstArchive, @SprintId, @DemandeId, @DevSupporte, @TacheSupportee);
                                    SELECT last_insert_rowid();";
                            }
                            else
                            {
                                cmd.CommandText = @"UPDATE BacklogItems SET Titre = @Titre, Description = @Description, 
                                    ProjetId = @ProjetId, DevId = @DevId, Type = @Type, Priorite = @Priorite, Statut = @Statut, 
                                    Points = @Points, ChiffrageHeures = @ChiffrageHeures, TempsReelHeures = @TempsReelHeures, 
                                    DateFinAttendue = @DateFinAttendue, DateDebut = @DateDebut, DateFin = @DateFin, 
                                    DateDerniereMaj = @DateDerniereMaj, EstArchive = @EstArchive, SprintId = @SprintId, DemandeId = @DemandeId,
                                    DevSupporte = @DevSupporte, TacheSupportee = @TacheSupportee 
                                    WHERE Id = @Id";
                                cmd.Parameters.AddWithValue("@Id", item.Id);
                            }

                            cmd.Parameters.AddWithValue("@Titre", item.Titre);
                            cmd.Parameters.AddWithValue("@Description", (object)item.Description ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ProjetId", (object)item.ProjetId ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@DevId", (object)item.DevAssigneId ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Type", (int)item.TypeDemande);
                            cmd.Parameters.AddWithValue("@Priorite", (int)item.Priorite);
                            cmd.Parameters.AddWithValue("@Statut", (int)item.Statut);
                            cmd.Parameters.AddWithValue("@Points", (object)item.Complexite ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ChiffrageHeures", (object)item.ChiffrageHeures ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@TempsReelHeures", (object)item.TempsReelHeures ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@DateFinAttendue", item.DateFinAttendue.HasValue ? item.DateFinAttendue.Value.ToString("yyyy-MM-dd HH:mm:ss") : (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@DateDebut", item.DateDebut.HasValue ? item.DateDebut.Value.ToString("yyyy-MM-dd HH:mm:ss") : (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@DateFin", item.DateFin.HasValue ? item.DateFin.Value.ToString("yyyy-MM-dd HH:mm:ss") : (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@DateCreation", item.DateCreation.ToString("yyyy-MM-dd HH:mm:ss"));
                            cmd.Parameters.AddWithValue("@DateDerniereMaj", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                            cmd.Parameters.AddWithValue("@EstArchive", item.EstArchive ? 1 : 0);
                            cmd.Parameters.AddWithValue("@SprintId", (object)item.SprintId ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@DemandeId", (object)item.DemandeId ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@DevSupporte", (object)item.DevSupporte ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@TacheSupportee", (object)item.TacheSupportee ?? DBNull.Value);

                            if (item.Id == 0)
                            {
                                item.Id = Convert.ToInt32(cmd.ExecuteScalar());
                            }
                            else
                            {
                                cmd.ExecuteNonQuery();
                            }
                        }
                        transaction.Commit();
                        return item;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }
        
        public Projet AddOrUpdateProjet(Projet projet)
        {
            using (var conn = GetConnectionForWrite())
            {
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        using (var cmd = conn.CreateCommand())
                        {
                            if (projet.Id == 0)
                            {
                                cmd.CommandText = @"INSERT INTO Projets (Nom, Description, DateCreation, DateDebut, DateFin, CouleurHex, Actif,
                                    ProgrammeId, EstImplemente, TypeProjet, Categorie, Priorite, Drivers, Ambition, Beneficiaires,
                                    GainsTemps, GainsFinanciers, LeadProjet, Timeline, TargetDelivery, PerimetreProchainComite, NextActions, StatutRAG, EquipesAssigneesIds) 
                                    VALUES (@Nom, @Description, @DateCreation, @DateDebut, @DateFin, @CouleurHex, @Actif,
                                    @ProgrammeId, @EstImplemente, @TypeProjet, @Categorie, @Priorite, @Drivers, @Ambition, @Beneficiaires,
                                    @GainsTemps, @GainsFinanciers, @LeadProjet, @Timeline, @TargetDelivery, @PerimetreProchainComite, @NextActions, @StatutRAG, @EquipesAssigneesIds);
                                    SELECT last_insert_rowid();";
                            }
                            else
                            {
                                cmd.CommandText = @"UPDATE Projets SET Nom = @Nom, Description = @Description, DateDebut = @DateDebut, DateFin = @DateFin, CouleurHex = @CouleurHex, Actif = @Actif,
                                    ProgrammeId = @ProgrammeId, EstImplemente = @EstImplemente, TypeProjet = @TypeProjet, Categorie = @Categorie, Priorite = @Priorite, 
                                    Drivers = @Drivers, Ambition = @Ambition, Beneficiaires = @Beneficiaires, GainsTemps = @GainsTemps, GainsFinanciers = @GainsFinanciers,
                                    LeadProjet = @LeadProjet, Timeline = @Timeline, TargetDelivery = @TargetDelivery, PerimetreProchainComite = @PerimetreProchainComite,
                                    NextActions = @NextActions, StatutRAG = @StatutRAG, EquipesAssigneesIds = @EquipesAssigneesIds
                                    WHERE Id = @Id";
                                cmd.Parameters.AddWithValue("@Id", projet.Id);
                            }

                            cmd.Parameters.AddWithValue("@Nom", projet.Nom);
                            cmd.Parameters.AddWithValue("@Description", (object)projet.Description ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@DateCreation", projet.DateCreation.ToString("yyyy-MM-dd HH:mm:ss"));
                            cmd.Parameters.AddWithValue("@DateDebut", projet.DateDebut.HasValue ? (object)projet.DateDebut.Value.ToString("yyyy-MM-dd") : DBNull.Value);
                            cmd.Parameters.AddWithValue("@DateFin", projet.DateFinPrevue.HasValue ? (object)projet.DateFinPrevue.Value.ToString("yyyy-MM-dd") : DBNull.Value);
                            cmd.Parameters.AddWithValue("@CouleurHex", (object)projet.CouleurHex ?? "#00915A");
                            cmd.Parameters.AddWithValue("@Actif", projet.Actif ? 1 : 0);
                            
                            // Phase 2 fields
                            cmd.Parameters.AddWithValue("@ProgrammeId", (object)projet.ProgrammeId ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@EstImplemente", projet.EstImplemente ? 1 : 0);
                            cmd.Parameters.AddWithValue("@TypeProjet", (object)projet.TypeProjet ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Categorie", (object)projet.Categorie ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Priorite", (object)projet.Priorite ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Drivers", (object)projet.Drivers ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Ambition", (object)projet.Ambition ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Beneficiaires", (object)projet.Beneficiaires ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@GainsTemps", (object)projet.GainsTemps ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@GainsFinanciers", (object)projet.GainsFinanciers ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@LeadProjet", (object)projet.LeadProjet ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Timeline", (object)projet.Timeline ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@TargetDelivery", (object)projet.TargetDelivery ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@PerimetreProchainComite", (object)projet.PerimetreProchainComite ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@NextActions", (object)projet.NextActions ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@StatutRAG", (object)projet.StatutRAG ?? DBNull.Value);
                            
                            // Équipes Assignées (List<int> → JSON)
                            string equipesJson;
                            if (projet.EquipesAssigneesIds != null && projet.EquipesAssigneesIds.Count > 0)
                            {
                                equipesJson = System.Text.Json.JsonSerializer.Serialize(projet.EquipesAssigneesIds);
                            }
                            else
                            {
                                equipesJson = null;
                            }
                            cmd.Parameters.AddWithValue("@EquipesAssigneesIds", (object)equipesJson ?? DBNull.Value);

                            if (projet.Id == 0)
                            {
                                projet.Id = Convert.ToInt32(cmd.ExecuteScalar());
                            }
                            else
                            {
                                cmd.ExecuteNonQuery();
                            }
                        }
                        transaction.Commit();
                        return projet;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }
        
        public Sprint AddOrUpdateSprint(Sprint sprint)
        {
            using (var conn = GetConnectionForWrite())
            {
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        using (var cmd = conn.CreateCommand())
                        {
                            if (sprint.Id == 0)
                            {
                                cmd.CommandText = @"INSERT INTO Sprints (Nom, DateDebut, DateFin, Objectif, EstActif) 
                                    VALUES (@Nom, @DateDebut, @DateFin, @Objectif, @EstActif);
                                    SELECT last_insert_rowid();";
                            }
                            else
                            {
                                cmd.CommandText = @"UPDATE Sprints SET Nom = @Nom, 
                                    DateDebut = @DateDebut, DateFin = @DateFin, Objectif = @Objectif, EstActif = @EstActif 
                                    WHERE Id = @Id";
                                cmd.Parameters.AddWithValue("@Id", sprint.Id);
                            }

                            cmd.Parameters.AddWithValue("@Nom", sprint.Nom);
                            cmd.Parameters.AddWithValue("@DateDebut", sprint.DateDebut.ToString("yyyy-MM-dd HH:mm:ss"));
                            cmd.Parameters.AddWithValue("@DateFin", sprint.DateFin.ToString("yyyy-MM-dd HH:mm:ss"));
                            cmd.Parameters.AddWithValue("@Objectif", (object)sprint.Objectif ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@EstActif", sprint.EstActif ? 1 : 0);

                            if (sprint.Id == 0)
                            {
                                sprint.Id = Convert.ToInt32(cmd.ExecuteScalar());
                            }
                            else
                            {
                                cmd.ExecuteNonQuery();
                            }
                        }
                        transaction.Commit();
                        return sprint;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }
        
        public Dev AddOrUpdateDev(Dev dev)
        {
            using (var conn = GetConnectionForWrite())
            {
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        using (var cmd = conn.CreateCommand())
                        {
                            if (dev.Id == 0)
                            {
                                cmd.CommandText = @"INSERT INTO Devs (Nom, Initiales, Actif) 
                                    VALUES (@Nom, @Initiales, @Actif);
                                    SELECT last_insert_rowid();";
                            }
                            else
                            {
                                cmd.CommandText = @"UPDATE Devs SET Nom = @Nom, Initiales = @Initiales, Actif = @Actif 
                                    WHERE Id = @Id";
                                cmd.Parameters.AddWithValue("@Id", dev.Id);
                            }

                            cmd.Parameters.AddWithValue("@Nom", dev.Nom);
                            cmd.Parameters.AddWithValue("@Initiales", dev.Initiales);
                            cmd.Parameters.AddWithValue("@Actif", dev.Actif ? 1 : 0);

                            if (dev.Id == 0)
                            {
                                dev.Id = Convert.ToInt32(cmd.ExecuteScalar());
                            }
                            else
                            {
                                cmd.ExecuteNonQuery();
                            }
                        }
                        transaction.Commit();
                        return dev;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }
        
        public Commentaire AddCommentaire(Commentaire commentaire)
        {
            using (var conn = GetConnectionForWrite())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"INSERT INTO Commentaires 
                        (DemandeId, BacklogItemId, AuteurId, Contenu, DateCreation, PieceJointeNom, PieceJointeChemin) 
                        VALUES (@DemandeId, @BacklogItemId, @AuteurId, @Contenu, @DateCreation, @PieceJointeNom, @PieceJointeChemin)";
                    
                    cmd.Parameters.AddWithValue("@DemandeId", commentaire.DemandeId.HasValue ? (object)commentaire.DemandeId.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@BacklogItemId", commentaire.BacklogItemId.HasValue ? (object)commentaire.BacklogItemId.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@AuteurId", commentaire.AuteurId);
                    cmd.Parameters.AddWithValue("@Contenu", commentaire.Contenu);
                    cmd.Parameters.AddWithValue("@DateCreation", commentaire.DateCreation.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("@PieceJointeNom", string.IsNullOrEmpty(commentaire.PieceJointeNom) ? (object)DBNull.Value : commentaire.PieceJointeNom);
                    cmd.Parameters.AddWithValue("@PieceJointeChemin", string.IsNullOrEmpty(commentaire.PieceJointeChemin) ? (object)DBNull.Value : commentaire.PieceJointeChemin);
                    
                    cmd.ExecuteNonQuery();
                    commentaire.Id = (int)conn.LastInsertRowId;
                }
            }
            return commentaire;
        }
        public Commentaire AddOrUpdateCommentaire(Commentaire commentaire) { return commentaire; }
        public void AddHistorique(HistoriqueModification historique) { }
        public HistoriqueModification AddOrUpdateHistoriqueModification(HistoriqueModification historique) { return historique; }
        public PokerSession AddOrUpdatePokerSession(PokerSession session) { return session; }
        public PokerVote AddPokerVote(PokerVote vote) { return vote; }
        public Disponibilite AddOrUpdateDisponibilite(Disponibilite disponibilite) { return disponibilite; }

        // Notifications
        public List<Notification> GetNotifications()
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT Id, Titre, Message, Type, DateCreation, EstLue, TacheId FROM Notifications ORDER BY DateCreation DESC";
                    
                    var notifications = new List<Notification>();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            notifications.Add(new Notification
                            {
                                Id = reader.GetInt32(0),
                                Titre = reader.GetString(1),
                                Message = reader.GetString(2),
                                Type = (NotificationType)reader.GetInt32(3),
                                DateCreation = DateTime.Parse(reader.GetString(4)),
                                EstLue = reader.GetInt32(5) == 1,
                                TacheId = reader.IsDBNull(6) ? (int?)null : reader.GetInt32(6)
                            });
                        }
                    }
                    return notifications;
                }
            }
        }

        public void AddOrUpdateNotification(Notification notification)
        {
            using (var conn = GetConnectionForWrite())
            {
                using (var cmd = conn.CreateCommand())
                {
                    if (notification.Id == 0)
                    {
                        cmd.CommandText = @"
                            INSERT INTO Notifications (Titre, Message, Type, DateCreation, EstLue, TacheId)
                            VALUES (@Titre, @Message, @Type, @DateCreation, @EstLue, @TacheId)";
                    }
                    else
                    {
                        cmd.CommandText = @"
                            UPDATE Notifications 
                            SET Titre = @Titre, Message = @Message, Type = @Type, 
                                DateCreation = @DateCreation, EstLue = @EstLue, TacheId = @TacheId
                            WHERE Id = @Id";
                        cmd.Parameters.AddWithValue("@Id", notification.Id);
                    }
                    
                    cmd.Parameters.AddWithValue("@Titre", notification.Titre);
                    cmd.Parameters.AddWithValue("@Message", notification.Message);
                    cmd.Parameters.AddWithValue("@Type", (int)notification.Type);
                    cmd.Parameters.AddWithValue("@DateCreation", notification.DateCreation.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("@EstLue", notification.EstLue ? 1 : 0);
                    cmd.Parameters.AddWithValue("@TacheId", notification.TacheId.HasValue ? (object)notification.TacheId.Value : DBNull.Value);
                    
                    cmd.ExecuteNonQuery();
                    
                    if (notification.Id == 0)
                    {
                        notification.Id = (int)conn.LastInsertRowId;
                    }
                }
            }
        }

        public void DeleteNotification(int notificationId)
        {
            using (var conn = GetConnectionForWrite())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM Notifications WHERE Id = @Id";
                    cmd.Parameters.AddWithValue("@Id", notificationId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void DeleteNotificationsLues()
        {
            using (var conn = GetConnectionForWrite())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM Notifications WHERE EstLue = 1";
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void MarquerNotificationCommeLue(int notificationId)
        {
            using (var conn = GetConnectionForWrite())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "UPDATE Notifications SET EstLue = 1 WHERE Id = @Id";
                    cmd.Parameters.AddWithValue("@Id", notificationId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void MarquerToutesNotificationsCommeLues()
        {
            using (var conn = GetConnectionForWrite())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "UPDATE Notifications SET EstLue = 1";
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void SupprimerToutesLesNotifications()
        {
            using (var conn = GetConnectionForWrite())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM Notifications";
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // ============================================
        // Méthodes CRA (Compte Rendu d'Activité)
        // ============================================

        public List<CRA> GetCRAs(int? backlogItemId = null, int? devId = null, DateTime? dateDebut = null, DateTime? dateFin = null)
        {
            var cras = new List<CRA>();

            using (var conn = GetConnection())
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    var whereClauses = new List<string>();
                    
                    if (backlogItemId.HasValue)
                    {
                        whereClauses.Add("BacklogItemId = @BacklogItemId");
                        cmd.Parameters.AddWithValue("@BacklogItemId", backlogItemId.Value);
                    }
                    
                    if (devId.HasValue)
                    {
                        whereClauses.Add("DevId = @DevId");
                        cmd.Parameters.AddWithValue("@DevId", devId.Value);
                    }
                    
                    if (dateDebut.HasValue)
                    {
                        whereClauses.Add("Date >= @DateDebut");
                        cmd.Parameters.AddWithValue("@DateDebut", dateDebut.Value.ToString("yyyy-MM-dd"));
                    }
                    
                    if (dateFin.HasValue)
                    {
                        whereClauses.Add("Date <= @DateFin");
                        cmd.Parameters.AddWithValue("@DateFin", dateFin.Value.ToString("yyyy-MM-dd"));
                    }

                    cmd.CommandText = "SELECT * FROM CRA";
                    if (whereClauses.Any())
                    {
                        cmd.CommandText += " WHERE " + string.Join(" AND ", whereClauses);
                    }
                    cmd.CommandText += " ORDER BY Date DESC, Id DESC";

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            cras.Add(new CRA
                            {
                                Id = reader.GetInt32(0),
                                BacklogItemId = reader.GetInt32(1),
                                DevId = reader.GetInt32(2),
                                Date = DateTime.Parse(reader.GetString(3)),
                                HeuresTravaillees = reader.GetDouble(4),
                                Commentaire = reader.IsDBNull(5) ? null : reader.GetString(5),
                                DateCreation = DateTime.Parse(reader.GetString(6)),
                                EstPrevisionnel = !reader.IsDBNull(7) && reader.GetInt32(7) == 1,
                                EstValide = !reader.IsDBNull(8) && reader.GetInt32(8) == 1
                            });
                        }
                    }
                }
            }

            return cras;
        }

        public List<CRA> GetAllCRAs()
        {
            return GetCRAs();
        }

        public void SaveCRA(CRA cra)
        {
            using (var conn = GetConnectionForWrite())
            {
                using (var cmd = conn.CreateCommand())
                {
                    if (cra.Id == 0)
                    {
                        // Insert
                        cmd.CommandText = @"
                            INSERT INTO CRA (BacklogItemId, DevId, Date, HeuresTravaillees, Commentaire, DateCreation, EstPrevisionnel, EstValide)
                            VALUES (@BacklogItemId, @DevId, @Date, @HeuresTravaillees, @Commentaire, @DateCreation, @EstPrevisionnel, @EstValide);
                            SELECT last_insert_rowid();
                        ";
                        cmd.Parameters.AddWithValue("@BacklogItemId", cra.BacklogItemId);
                        cmd.Parameters.AddWithValue("@DevId", cra.DevId);
                        cmd.Parameters.AddWithValue("@Date", cra.Date.ToString("yyyy-MM-dd"));
                        cmd.Parameters.AddWithValue("@HeuresTravaillees", cra.HeuresTravaillees);
                        cmd.Parameters.AddWithValue("@Commentaire", (object)cra.Commentaire ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@DateCreation", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        cmd.Parameters.AddWithValue("@EstPrevisionnel", cra.EstPrevisionnel ? 1 : 0);
                        cmd.Parameters.AddWithValue("@EstValide", cra.EstValide ? 1 : 0);

                        cra.Id = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                    else
                    {
                        // Update
                        cmd.CommandText = @"
                            UPDATE CRA 
                            SET BacklogItemId = @BacklogItemId,
                                DevId = @DevId,
                                Date = @Date,
                                HeuresTravaillees = @HeuresTravaillees,
                                Commentaire = @Commentaire,
                                EstPrevisionnel = @EstPrevisionnel,
                                EstValide = @EstValide
                            WHERE Id = @Id
                        ";
                        cmd.Parameters.AddWithValue("@Id", cra.Id);
                        cmd.Parameters.AddWithValue("@BacklogItemId", cra.BacklogItemId);
                        cmd.Parameters.AddWithValue("@DevId", cra.DevId);
                        cmd.Parameters.AddWithValue("@Date", cra.Date.ToString("yyyy-MM-dd"));
                        cmd.Parameters.AddWithValue("@HeuresTravaillees", cra.HeuresTravaillees);
                        cmd.Parameters.AddWithValue("@Commentaire", (object)cra.Commentaire ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@EstPrevisionnel", cra.EstPrevisionnel ? 1 : 0);
                        cmd.Parameters.AddWithValue("@EstValide", cra.EstValide ? 1 : 0);

                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        public void DeleteCRA(int id)
        {
            using (var conn = GetConnectionForWrite())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM CRA WHERE Id = @Id";
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.ExecuteNonQuery();
                }
            }
        }
        
        // Chat Conversations Methods
        public List<ChatConversation> GetChatConversations()
        {
            var conversations = new List<ChatConversation>();
            using (var conn = GetConnectionForRead())
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(@"
                    SELECT Id, UserId, Username, DateDebut, DateDernierMessage, NombreMessages 
                    FROM ChatConversations 
                    ORDER BY DateDernierMessage DESC", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        conversations.Add(new ChatConversation
                        {
                            Id = reader.GetInt32(0),
                            UserId = reader.GetInt32(1),
                            Username = reader.GetString(2),
                            DateDebut = DateTime.Parse(reader.GetString(3)),
                            DateDernierMessage = DateTime.Parse(reader.GetString(4)),
                            NombreMessages = reader.GetInt32(5)
                        });
                    }
                }
            }
            return conversations;
        }
        
        public ChatConversation GetChatConversation(int conversationId)
        {
            using (var conn = GetConnectionForRead())
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(@"
                    SELECT Id, UserId, Username, DateDebut, DateDernierMessage, NombreMessages 
                    FROM ChatConversations 
                    WHERE Id = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", conversationId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new ChatConversation
                            {
                                Id = reader.GetInt32(0),
                                UserId = reader.GetInt32(1),
                                Username = reader.GetString(2),
                                DateDebut = DateTime.Parse(reader.GetString(3)),
                                DateDernierMessage = DateTime.Parse(reader.GetString(4)),
                                NombreMessages = reader.GetInt32(5)
                            };
                        }
                    }
                }
            }
            return null;
        }
        
        public int CreateChatConversation(int userId, string username)
        {
            using (var conn = GetConnectionForWrite())
            {
                using (var cmd = conn.CreateCommand())
                {
                    var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    cmd.CommandText = @"
                        INSERT INTO ChatConversations (UserId, Username, DateDebut, DateDernierMessage, NombreMessages)
                        VALUES (@UserId, @Username, @DateDebut, @DateDernierMessage, 0);
                        SELECT last_insert_rowid();
                    ";
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.Parameters.AddWithValue("@Username", username);
                    cmd.Parameters.AddWithValue("@DateDebut", now);
                    cmd.Parameters.AddWithValue("@DateDernierMessage", now);
                    
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }
        
        public void UpdateChatConversation(int conversationId)
        {
            using (var conn = GetConnectionForWrite())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        UPDATE ChatConversations 
                        SET DateDernierMessage = @DateDernierMessage,
                            NombreMessages = (SELECT COUNT(*) FROM ChatMessages WHERE ConversationId = @ConversationId)
                        WHERE Id = @ConversationId
                    ";
                    cmd.Parameters.AddWithValue("@ConversationId", conversationId);
                    cmd.Parameters.AddWithValue("@DateDernierMessage", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.ExecuteNonQuery();
                }
            }
        }
        
        public List<ChatMessageDB> GetChatMessages(int conversationId)
        {
            var messages = new List<ChatMessageDB>();
            using (var conn = GetConnectionForRead())
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(@"
                    SELECT Id, ConversationId, UserId, Username, IsUser, Message, DateMessage, Reaction 
                    FROM ChatMessages 
                    WHERE ConversationId = @ConversationId 
                    ORDER BY DateMessage ASC", conn))
                {
                    cmd.Parameters.AddWithValue("@ConversationId", conversationId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            messages.Add(new ChatMessageDB
                            {
                                Id = reader.GetInt32(0),
                                ConversationId = reader.GetInt32(1),
                                UserId = reader.GetInt32(2),
                                Username = reader.GetString(3),
                                IsUser = reader.GetInt32(4) == 1,
                                Message = reader.GetString(5),
                                DateMessage = DateTime.Parse(reader.GetString(6)),
                                Reaction = reader.IsDBNull(7) ? null : reader.GetString(7)
                            });
                        }
                    }
                }
            }
            return messages;
        }
        
        public void AddChatMessage(ChatMessageDB message)
        {
            using (var conn = GetConnectionForWrite())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        INSERT INTO ChatMessages (ConversationId, UserId, Username, IsUser, Message, DateMessage, Reaction)
                        VALUES (@ConversationId, @UserId, @Username, @IsUser, @Message, @DateMessage, @Reaction);
                        SELECT last_insert_rowid();
                    ";
                    cmd.Parameters.AddWithValue("@ConversationId", message.ConversationId);
                    cmd.Parameters.AddWithValue("@UserId", message.UserId);
                    cmd.Parameters.AddWithValue("@Username", message.Username);
                    cmd.Parameters.AddWithValue("@IsUser", message.IsUser ? 1 : 0);
                    cmd.Parameters.AddWithValue("@Message", message.Message);
                    cmd.Parameters.AddWithValue("@DateMessage", message.DateMessage.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("@Reaction", (object)message.Reaction ?? DBNull.Value);
                    
                    message.Id = Convert.ToInt32(cmd.ExecuteScalar());
                }
                
                // Mettre à jour la conversation
                UpdateChatConversation(message.ConversationId);
            }
        }

        public void DeleteUserChatConversations(int userId)
        {
            using (var conn = GetConnectionForWrite())
            {
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // Supprimer d'abord tous les messages des conversations de l'utilisateur
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = @"
                                DELETE FROM ChatMessages 
                                WHERE ConversationId IN (
                                    SELECT Id FROM ChatConversations WHERE UserId = @UserId
                                )
                            ";
                            cmd.Parameters.AddWithValue("@UserId", userId);
                            cmd.ExecuteNonQuery();
                        }

                        // Ensuite supprimer les conversations
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = "DELETE FROM ChatConversations WHERE UserId = @UserId";
                            cmd.Parameters.AddWithValue("@UserId", userId);
                            cmd.ExecuteNonQuery();
                        }

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }
        
        // ========== PHASE 1 : Méthodes de gestion des Équipes ==========
        
        public List<Equipe> GetAllEquipes()
        {
            var equipes = new List<Equipe>();
            using (var conn = GetConnection())
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(@"
                    SELECT Id, Nom, Code, Description, PerimetreFonctionnel, ManagerId, Contact, Actif, DateCreation 
                    FROM Equipes 
                    WHERE Actif = 1 
                    ORDER BY Nom", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        equipes.Add(new Equipe
                        {
                            Id = reader.GetInt32(0),
                            Nom = reader.GetString(1),
                            Code = reader.GetString(2),
                            Description = !reader.IsDBNull(3) ? reader.GetString(3) : null,
                            PerimetreFonctionnel = !reader.IsDBNull(4) ? reader.GetString(4) : null,
                            ManagerId = !reader.IsDBNull(5) ? (int?)reader.GetInt32(5) : null,
                            Contact = !reader.IsDBNull(6) ? reader.GetString(6) : null,
                            Actif = reader.GetInt32(7) == 1,
                            DateCreation = DateTime.Parse(reader.GetString(8))
                        });
                    }
                }
            }
            return equipes;
        }
        
        public Equipe GetEquipeById(int id)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(@"
                    SELECT Id, Nom, Code, Description, PerimetreFonctionnel, ManagerId, Contact, Actif, DateCreation 
                    FROM Equipes 
                    WHERE Id = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new Equipe
                            {
                                Id = reader.GetInt32(0),
                                Nom = reader.GetString(1),
                                Code = reader.GetString(2),
                                Description = !reader.IsDBNull(3) ? reader.GetString(3) : null,
                                PerimetreFonctionnel = !reader.IsDBNull(4) ? reader.GetString(4) : null,
                                ManagerId = !reader.IsDBNull(5) ? (int?)reader.GetInt32(5) : null,
                                Contact = !reader.IsDBNull(6) ? reader.GetString(6) : null,
                                Actif = reader.GetInt32(7) == 1,
                                DateCreation = DateTime.Parse(reader.GetString(8))
                            };
                        }
                    }
                }
            }
            return null;
        }
        
        public void AjouterEquipe(Equipe equipe)
        {
            using (var conn = GetConnectionForWrite())
            {
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = @"
                                INSERT INTO Equipes (Nom, Code, Description, PerimetreFonctionnel, ManagerId, Contact, Actif, DateCreation)
                                VALUES (@Nom, @Code, @Description, @PerimetreFonctionnel, @ManagerId, @Contact, @Actif, @DateCreation);
                                SELECT last_insert_rowid();
                            ";
                            cmd.Parameters.AddWithValue("@Nom", equipe.Nom);
                            cmd.Parameters.AddWithValue("@Code", equipe.Code);
                            cmd.Parameters.AddWithValue("@Description", (object)equipe.Description ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@PerimetreFonctionnel", (object)equipe.PerimetreFonctionnel ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ManagerId", equipe.ManagerId.HasValue ? (object)equipe.ManagerId.Value : DBNull.Value);
                            cmd.Parameters.AddWithValue("@Contact", (object)equipe.Contact ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Actif", equipe.Actif ? 1 : 0);
                            cmd.Parameters.AddWithValue("@DateCreation", equipe.DateCreation.ToString("yyyy-MM-dd HH:mm:ss"));
                            
                            equipe.Id = Convert.ToInt32(cmd.ExecuteScalar());
                        }
                        
                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }
        
        public void ModifierEquipe(Equipe equipe)
        {
            using (var conn = GetConnectionForWrite())
            {
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = @"
                                UPDATE Equipes 
                                SET Nom = @Nom,
                                    Code = @Code,
                                    Description = @Description,
                                    PerimetreFonctionnel = @PerimetreFonctionnel,
                                    ManagerId = @ManagerId,
                                    Contact = @Contact,
                                    Actif = @Actif
                                WHERE Id = @Id
                            ";
                            cmd.Parameters.AddWithValue("@Id", equipe.Id);
                            cmd.Parameters.AddWithValue("@Nom", equipe.Nom);
                            cmd.Parameters.AddWithValue("@Code", equipe.Code);
                            cmd.Parameters.AddWithValue("@Description", (object)equipe.Description ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@PerimetreFonctionnel", (object)equipe.PerimetreFonctionnel ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ManagerId", equipe.ManagerId.HasValue ? (object)equipe.ManagerId.Value : DBNull.Value);
                            cmd.Parameters.AddWithValue("@Contact", (object)equipe.Contact ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Actif", equipe.Actif ? 1 : 0);
                            
                            cmd.ExecuteNonQuery();
                        }
                        
                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }
        
        public List<Utilisateur> GetMembresByEquipe(int equipeId)
        {
            var membres = new List<Utilisateur>();
            using (var conn = GetConnection())
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(@"
                    SELECT Id, UsernameWindows, Nom, Prenom, Email, RoleId, Actif, DateCreation, DateDerniereConnexion, EquipeId
                    FROM Utilisateurs 
                    WHERE EquipeId = @EquipeId AND Actif = 1
                    ORDER BY Nom, Prenom", conn))
                {
                    cmd.Parameters.AddWithValue("@EquipeId", equipeId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            membres.Add(new Utilisateur
                            {
                                Id = reader.GetInt32(0),
                                UsernameWindows = reader.GetString(1),
                                Nom = reader.GetString(2),
                                Prenom = reader.GetString(3),
                                Email = !reader.IsDBNull(4) ? reader.GetString(4) : null,
                                RoleId = reader.GetInt32(5),
                                Actif = reader.GetInt32(6) == 1,
                                DateCreation = DateTime.Parse(reader.GetString(7)),
                                DateDerniereConnexion = !reader.IsDBNull(8) ? (DateTime?)DateTime.Parse(reader.GetString(8)) : null,
                                EquipeId = !reader.IsDBNull(9) ? (int?)reader.GetInt32(9) : null
                            });
                        }
                    }
                }
            }
            return membres;
        }
        
        public List<Projet> GetProjetsByEquipe(int equipeId)
        {
            var projets = new List<Projet>();
            using (var conn = GetConnection())
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(@"
                    SELECT Id, Nom, Description, DateCreation, DateDebut, DateFinPrevue, Actif, CouleurHex, EquipesAssigneesIds
                    FROM Projets 
                    WHERE Actif = 1", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var projet = new Projet
                        {
                            Id = reader.GetInt32(0),
                            Nom = reader.GetString(1),
                            Description = !reader.IsDBNull(2) ? reader.GetString(2) : null,
                            DateCreation = DateTime.Parse(reader.GetString(3)),
                            DateDebut = !reader.IsDBNull(4) ? (DateTime?)DateTime.Parse(reader.GetString(4)) : null,
                            DateFinPrevue = !reader.IsDBNull(5) ? (DateTime?)DateTime.Parse(reader.GetString(5)) : null,
                            Actif = reader.GetInt32(6) == 1,
                            CouleurHex = !reader.IsDBNull(7) ? reader.GetString(7) : "#00915A"
                        };
                        
                        // Parser les équipes assignées (JSON array)
                        if (!reader.IsDBNull(8))
                        {
                            string equipesJson = reader.GetString(8);
                            if (!string.IsNullOrWhiteSpace(equipesJson) && equipesJson != "[]")
                            {
                                try
                                {
                                    projet.EquipesAssigneesIds = System.Text.Json.JsonSerializer.Deserialize<List<int>>(equipesJson);
                                }
                                catch
                                {
                                    projet.EquipesAssigneesIds = new List<int>();
                                }
                            }
                        }
                        
                        // Ajouter le projet si l'équipe fait partie des équipes assignées
                        if (projet.EquipesAssigneesIds != null && projet.EquipesAssigneesIds.Contains(equipeId))
                        {
                            projets.Add(projet);
                        }
                    }
                }
            }
            return projets;
        }
        
        public List<BacklogItem> GetBacklogItemsByDevId(int devId)
        {
            var items = new List<BacklogItem>();
            using (var conn = GetConnection())
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(@"
                    SELECT Id, Titre, Description, ProjetId, DevId, Type, Priorite, Statut, Points, 
                           ChiffrageHeures, TempsReelHeures, DateFinAttendue, DateDebut, DateFin, 
                           DateCreation, DateDerniereMaj, EstArchive, SprintId, DemandeId
                    FROM BacklogItems 
                    WHERE DevId = @DevId", conn))
                {
                    cmd.Parameters.AddWithValue("@DevId", devId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            items.Add(new BacklogItem
                            {
                                Id = reader.GetInt32(0),
                                Titre = reader.GetString(1),
                                Description = !reader.IsDBNull(2) ? reader.GetString(2) : null,
                                ProjetId = !reader.IsDBNull(3) ? (int?)reader.GetInt32(3) : null,
                                DevAssigneId = !reader.IsDBNull(4) ? (int?)reader.GetInt32(4) : null,
                                TypeDemande = (TypeDemande)reader.GetInt32(5),
                                Priorite = (Priorite)reader.GetInt32(6),
                                Statut = (Statut)reader.GetInt32(7),
                                Complexite = !reader.IsDBNull(8) ? (int?)reader.GetInt32(8) : null,
                                ChiffrageHeures = !reader.IsDBNull(9) ? (double?)reader.GetDouble(9) : null,
                                TempsReelHeures = !reader.IsDBNull(10) ? (double?)reader.GetDouble(10) : null,
                                DateFinAttendue = !reader.IsDBNull(11) ? (DateTime?)DateTime.Parse(reader.GetString(11)) : null,
                                DateDebut = !reader.IsDBNull(12) ? (DateTime?)DateTime.Parse(reader.GetString(12)) : null,
                                DateFin = !reader.IsDBNull(13) ? (DateTime?)DateTime.Parse(reader.GetString(13)) : null,
                                DateCreation = DateTime.Parse(reader.GetString(14)),
                                DateDerniereMaj = DateTime.Parse(reader.GetString(15)),
                                EstArchive = reader.GetInt32(16) == 1,
                                SprintId = !reader.IsDBNull(17) ? (int?)reader.GetInt32(17) : null,
                                DemandeId = !reader.IsDBNull(18) ? (int?)reader.GetInt32(18) : null
                            });
                        }
                    }
                }
            }
            return items;
        }
        
        // ========== CRUD Programmes ==========
        
        public List<Programme> GetAllProgrammes()
        {
            var programmes = new List<Programme>();
            using (var conn = GetConnection())
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(@"
                    SELECT Id, Nom, Code, Description, Objectifs, ResponsableId, DateDebut, DateFinCible, StatutGlobal, Actif, DateCreation
                    FROM Programmes 
                    ORDER BY Nom", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        programmes.Add(new Programme
                        {
                            Id = reader.GetInt32(0),
                            Nom = reader.GetString(1),
                            Code = !reader.IsDBNull(2) ? reader.GetString(2) : null,
                            Description = !reader.IsDBNull(3) ? reader.GetString(3) : null,
                            Objectifs = !reader.IsDBNull(4) ? reader.GetString(4) : null,
                            ResponsableId = !reader.IsDBNull(5) ? (int?)reader.GetInt32(5) : null,
                            DateDebut = !reader.IsDBNull(6) ? (DateTime?)DateTime.Parse(reader.GetString(6)) : null,
                            DateFinCible = !reader.IsDBNull(7) ? (DateTime?)DateTime.Parse(reader.GetString(7)) : null,
                            StatutGlobal = !reader.IsDBNull(8) ? reader.GetString(8) : null,
                            Actif = reader.GetInt32(9) == 1,
                            DateCreation = DateTime.Parse(reader.GetString(10))
                        });
                    }
                }
            }
            return programmes;
        }
        
        public Programme GetProgrammeById(int id)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(@"
                    SELECT Id, Nom, Code, Description, Objectifs, ResponsableId, DateDebut, DateFinCible, StatutGlobal, Actif, DateCreation
                    FROM Programmes 
                    WHERE Id = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new Programme
                            {
                                Id = reader.GetInt32(0),
                                Nom = reader.GetString(1),
                                Code = !reader.IsDBNull(2) ? reader.GetString(2) : null,
                                Description = !reader.IsDBNull(3) ? reader.GetString(3) : null,
                                Objectifs = !reader.IsDBNull(4) ? reader.GetString(4) : null,
                                ResponsableId = !reader.IsDBNull(5) ? (int?)reader.GetInt32(5) : null,
                                DateDebut = !reader.IsDBNull(6) ? (DateTime?)DateTime.Parse(reader.GetString(6)) : null,
                                DateFinCible = !reader.IsDBNull(7) ? (DateTime?)DateTime.Parse(reader.GetString(7)) : null,
                                StatutGlobal = !reader.IsDBNull(8) ? reader.GetString(8) : null,
                                Actif = reader.GetInt32(9) == 1,
                                DateCreation = DateTime.Parse(reader.GetString(10))
                            };
                        }
                    }
                }
            }
            return null;
        }
        
        public void AjouterProgramme(Programme programme)
        {
            using (var conn = GetConnectionForWrite())
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(@"
                    INSERT INTO Programmes (Nom, Code, Description, Objectifs, ResponsableId, DateDebut, DateFinCible, StatutGlobal, Actif, DateCreation)
                    VALUES (@Nom, @Code, @Description, @Objectifs, @ResponsableId, @DateDebut, @DateFinCible, @StatutGlobal, @Actif, @DateCreation);
                    SELECT last_insert_rowid();", conn))
                {
                    cmd.Parameters.AddWithValue("@Nom", programme.Nom);
                    cmd.Parameters.AddWithValue("@Code", string.IsNullOrWhiteSpace(programme.Code) ? DBNull.Value : (object)programme.Code);
                    cmd.Parameters.AddWithValue("@Description", string.IsNullOrWhiteSpace(programme.Description) ? DBNull.Value : (object)programme.Description);
                    cmd.Parameters.AddWithValue("@Objectifs", string.IsNullOrWhiteSpace(programme.Objectifs) ? DBNull.Value : (object)programme.Objectifs);
                    cmd.Parameters.AddWithValue("@ResponsableId", programme.ResponsableId.HasValue ? (object)programme.ResponsableId.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@DateDebut", programme.DateDebut.HasValue ? (object)programme.DateDebut.Value.ToString("yyyy-MM-dd HH:mm:ss") : DBNull.Value);
                    cmd.Parameters.AddWithValue("@DateFinCible", programme.DateFinCible.HasValue ? (object)programme.DateFinCible.Value.ToString("yyyy-MM-dd HH:mm:ss") : DBNull.Value);
                    cmd.Parameters.AddWithValue("@StatutGlobal", string.IsNullOrWhiteSpace(programme.StatutGlobal) ? DBNull.Value : (object)programme.StatutGlobal);
                    cmd.Parameters.AddWithValue("@Actif", programme.Actif ? 1 : 0);
                    cmd.Parameters.AddWithValue("@DateCreation", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    
                    programme.Id = Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }
        
        public void ModifierProgramme(Programme programme)
        {
            using (var conn = GetConnectionForWrite())
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(@"
                    UPDATE Programmes 
                    SET Nom = @Nom, 
                        Code = @Code, 
                        Description = @Description, 
                        Objectifs = @Objectifs,
                        ResponsableId = @ResponsableId,
                        DateDebut = @DateDebut,
                        DateFinCible = @DateFinCible,
                        StatutGlobal = @StatutGlobal,
                        Actif = @Actif
                    WHERE Id = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", programme.Id);
                    cmd.Parameters.AddWithValue("@Nom", programme.Nom);
                    cmd.Parameters.AddWithValue("@Code", string.IsNullOrWhiteSpace(programme.Code) ? DBNull.Value : (object)programme.Code);
                    cmd.Parameters.AddWithValue("@Description", string.IsNullOrWhiteSpace(programme.Description) ? DBNull.Value : (object)programme.Description);
                    cmd.Parameters.AddWithValue("@Objectifs", string.IsNullOrWhiteSpace(programme.Objectifs) ? DBNull.Value : (object)programme.Objectifs);
                    cmd.Parameters.AddWithValue("@ResponsableId", programme.ResponsableId.HasValue ? (object)programme.ResponsableId.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@DateDebut", programme.DateDebut.HasValue ? (object)programme.DateDebut.Value.ToString("yyyy-MM-dd HH:mm:ss") : DBNull.Value);
                    cmd.Parameters.AddWithValue("@DateFinCible", programme.DateFinCible.HasValue ? (object)programme.DateFinCible.Value.ToString("yyyy-MM-dd HH:mm:ss") : DBNull.Value);
                    cmd.Parameters.AddWithValue("@StatutGlobal", string.IsNullOrWhiteSpace(programme.StatutGlobal) ? DBNull.Value : (object)programme.StatutGlobal);
                    cmd.Parameters.AddWithValue("@Actif", programme.Actif ? 1 : 0);
                    
                    cmd.ExecuteNonQuery();
                }
            }
        }
        
        public void SupprimerProgramme(int id)
        {
            using (var conn = GetConnectionForWrite())
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(@"DELETE FROM Programmes WHERE Id = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.ExecuteNonQuery();
                }
            }
        }
        
        public List<Projet> GetProjetsByProgramme(int programmeId)
        {
            var projets = new List<Projet>();
            using (var conn = GetConnection())
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(@"
                    SELECT Id, Nom, Description, DateCreation, DateDebut, DateFinPrevue, Actif, CouleurHex, ProgrammeId
                    FROM Projets 
                    WHERE ProgrammeId = @ProgrammeId AND Actif = 1
                    ORDER BY Nom", conn))
                {
                    cmd.Parameters.AddWithValue("@ProgrammeId", programmeId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            projets.Add(new Projet
                            {
                                Id = reader.GetInt32(0),
                                Nom = reader.GetString(1),
                                Description = !reader.IsDBNull(2) ? reader.GetString(2) : null,
                                DateCreation = DateTime.Parse(reader.GetString(3)),
                                DateDebut = !reader.IsDBNull(4) ? (DateTime?)DateTime.Parse(reader.GetString(4)) : null,
                                DateFinPrevue = !reader.IsDBNull(5) ? (DateTime?)DateTime.Parse(reader.GetString(5)) : null,
                                Actif = reader.GetInt32(6) == 1,
                                CouleurHex = !reader.IsDBNull(7) ? reader.GetString(7) : "#00915A",
                                ProgrammeId = !reader.IsDBNull(8) ? (int?)reader.GetInt32(8) : null
                            });
                        }
                    }
                }
            }
            return projets;
        }
    }
}

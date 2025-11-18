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
        private readonly string _connectionString;

        public SqliteDatabase()
        {
            var appDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory(appDataPath);
            }

            _databasePath = Path.Combine(appDataPath, "backlog.db");
            _connectionString = string.Format("Data Source={0};Version=3;", _databasePath);

            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            if (!File.Exists(_databasePath))
            {
                SQLiteConnection.CreateFile(_databasePath);
                CreateTables();
            }
            else
            {
                // Appliquer les migrations sur base existante
                using (var conn = GetConnection())
                {
                    conn.Open();
                    MigrateDatabaseSchema(conn);
                }
            }
        }

        // Connexion standard
        private SQLiteConnection GetConnection()
        {
            return new SQLiteConnection(_connectionString);
        }

        private void CreateTables()
        {
            using (var conn = GetConnection())
            {
                conn.Open();
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
                            FOREIGN KEY (BacklogItemId) REFERENCES BacklogItems(Id) ON DELETE CASCADE,
                            FOREIGN KEY (DevId) REFERENCES Utilisateurs(Id)
                        );
                        
                        CREATE INDEX IF NOT EXISTS idx_cra_backlogitem ON CRA(BacklogItemId);
                        CREATE INDEX IF NOT EXISTS idx_cra_dev ON CRA(DevId);
                        CREATE INDEX IF NOT EXISTS idx_cra_date ON CRA(Date);
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
                using (var cmd = new SQLiteCommand("SELECT * FROM Utilisateurs", conn))
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
                        DateDerniereConnexion = reader.IsDBNull(8) ? (DateTime?)null : DateTime.Parse(reader.GetString(8))
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
                    ChiffrageReelJours, DatePrevisionnelleImplementation, JustificationRefus, EstArchivee 
                    FROM Demandes WHERE EstArchivee = 0", conn))
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
                        EstArchivee = reader.GetInt32(22) == 1
                    });
                }
            }
            }
            return demandes;
        }

        // WRITE OPERATIONS (Write Connection - opened, used, closed immediately)
        public Role AddOrUpdateRole(Role role)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
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
            using (var conn = GetConnection())
            {
                conn.Open();
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
            using (var conn = GetConnection())
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
            {
                try
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        if (utilisateur.Id == 0)
                        {
                            cmd.CommandText = @"INSERT INTO Utilisateurs (UsernameWindows, Nom, Prenom, Email, RoleId, Actif, DateCreation, DateDerniereConnexion)
                                VALUES (@UsernameWindows, @Nom, @Prenom, @Email, @RoleId, @Actif, @DateCreation, @DateDerniereConnexion);
                                SELECT last_insert_rowid();";
                        }
                        else
                        {
                            cmd.CommandText = @"UPDATE Utilisateurs SET UsernameWindows = @UsernameWindows, Nom = @Nom, Prenom = @Prenom,
                                Email = @Email, RoleId = @RoleId, Actif = @Actif, DateDerniereConnexion = @DateDerniereConnexion
                                WHERE Id = @Id";
                            cmd.Parameters.AddWithValue("@Id", utilisateur.Id);
                        }

                        cmd.Parameters.AddWithValue("@UsernameWindows", utilisateur.UsernameWindows);
                        cmd.Parameters.AddWithValue("@Nom", utilisateur.Nom);
                        cmd.Parameters.AddWithValue("@Prenom", utilisateur.Prenom);
                        cmd.Parameters.AddWithValue("@Email", utilisateur.Email ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@RoleId", utilisateur.RoleId);
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
            using (var conn = GetConnection())
            {
                conn.Open();
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
            using (var conn = GetConnection())
            {
                conn.Open();
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
                                ChiffrageEstimeJours, ChiffrageReelJours, DatePrevisionnelleImplementation, JustificationRefus, EstArchivee)
                                VALUES (@Titre, @Description, @Specifications, @ContexteMetier, @BeneficesAttendus,
                                @DemandeurId, @BusinessAnalystId, @ChefProjetId, @DevChiffreurId, @ProjetId,
                                @Type, @Criticite, @Statut, @DateCreation, @DateValidationChiffrage, @DateAcceptation, @DateLivraison,
                                @ChiffrageEstimeJours, @ChiffrageReelJours, @DatePrevisionnelleImplementation, @JustificationRefus, @EstArchivee);
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
                                JustificationRefus = @JustificationRefus, EstArchivee = @EstArchivee WHERE Id = @Id";
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
            using (var conn = GetConnection())
            {
                conn.Open();
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
            using (var conn = GetConnection())
            {
                conn.Open();
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
                using (var cmd = new SQLiteCommand("SELECT Id, Nom, Description, DateCreation, Actif FROM Projets WHERE Actif = 1 ORDER BY DateCreation DESC", conn))
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
                            Actif = reader.GetInt32(4) == 1
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
            using (var conn = GetConnection())
            {
                conn.Open();
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
            using (var conn = GetConnection())
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        using (var cmd = conn.CreateCommand())
                        {
                            if (projet.Id == 0)
                            {
                                cmd.CommandText = @"INSERT INTO Projets (Nom, Description, DateCreation, Actif) 
                                    VALUES (@Nom, @Description, @DateCreation, @Actif);
                                    SELECT last_insert_rowid();";
                            }
                            else
                            {
                                cmd.CommandText = @"UPDATE Projets SET Nom = @Nom, Description = @Description, Actif = @Actif 
                                    WHERE Id = @Id";
                                cmd.Parameters.AddWithValue("@Id", projet.Id);
                            }

                            cmd.Parameters.AddWithValue("@Nom", projet.Nom);
                            cmd.Parameters.AddWithValue("@Description", (object)projet.Description ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@DateCreation", projet.DateCreation.ToString("yyyy-MM-dd HH:mm:ss"));
                            cmd.Parameters.AddWithValue("@Actif", projet.Actif ? 1 : 0);

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
            using (var conn = GetConnection())
            {
                conn.Open();
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
            using (var conn = GetConnection())
            {
                conn.Open();
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
            using (var conn = GetConnection())
            {
                conn.Open();
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
            using (var conn = new SQLiteConnection(_connectionString))
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
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
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
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
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
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM Notifications WHERE EstLue = 1";
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void MarquerNotificationCommeLue(int notificationId)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
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
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "UPDATE Notifications SET EstLue = 1";
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

            using (var conn = new SQLiteConnection(_connectionString))
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
                                DateCreation = DateTime.Parse(reader.GetString(6))
                            });
                        }
                    }
                }
            }

            return cras;
        }

        public void SaveCRA(CRA cra)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    if (cra.Id == 0)
                    {
                        // Insert
                        cmd.CommandText = @"
                            INSERT INTO CRA (BacklogItemId, DevId, Date, HeuresTravaillees, Commentaire, DateCreation)
                            VALUES (@BacklogItemId, @DevId, @Date, @HeuresTravaillees, @Commentaire, @DateCreation);
                            SELECT last_insert_rowid();
                        ";
                        cmd.Parameters.AddWithValue("@BacklogItemId", cra.BacklogItemId);
                        cmd.Parameters.AddWithValue("@DevId", cra.DevId);
                        cmd.Parameters.AddWithValue("@Date", cra.Date.ToString("yyyy-MM-dd"));
                        cmd.Parameters.AddWithValue("@HeuresTravaillees", cra.HeuresTravaillees);
                        cmd.Parameters.AddWithValue("@Commentaire", (object)cra.Commentaire ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@DateCreation", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

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
                                Commentaire = @Commentaire
                            WHERE Id = @Id
                        ";
                        cmd.Parameters.AddWithValue("@Id", cra.Id);
                        cmd.Parameters.AddWithValue("@BacklogItemId", cra.BacklogItemId);
                        cmd.Parameters.AddWithValue("@DevId", cra.DevId);
                        cmd.Parameters.AddWithValue("@Date", cra.Date.ToString("yyyy-MM-dd"));
                        cmd.Parameters.AddWithValue("@HeuresTravaillees", cra.HeuresTravaillees);
                        cmd.Parameters.AddWithValue("@Commentaire", (object)cra.Commentaire ?? DBNull.Value);

                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        public void DeleteCRA(int id)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM CRA WHERE Id = @Id";
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}

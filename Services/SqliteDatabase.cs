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
                            FOREIGN KEY (ProjetId) REFERENCES Projets(Id),
                            FOREIGN KEY (DevId) REFERENCES Utilisateurs(Id),
                            FOREIGN KEY (SprintId) REFERENCES Sprints(Id),
                            FOREIGN KEY (DemandeId) REFERENCES Demandes(Id)
                        );

                        CREATE TABLE IF NOT EXISTS Demandes (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            Titre TEXT NOT NULL,
                            Description TEXT,
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
                            ChiffrageEstimeHeures REAL,
                            ChiffrageReelHeures REAL,
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
                    ";
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
                using (var cmd = new SQLiteCommand("SELECT * FROM Roles WHERE Actif = 1", conn))
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
                            Actif = reader.GetInt32(9) == 1
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
                using (var cmd = new SQLiteCommand("SELECT * FROM Demandes WHERE EstArchivee = 0", conn))
                using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    demandes.Add(new Demande
                    {
                        Id = reader.GetInt32(0),
                        Titre = reader.GetString(1),
                        Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                        DemandeurId = reader.GetInt32(3),
                        BusinessAnalystId = reader.IsDBNull(4) ? (int?)null : reader.GetInt32(4),
                        ChefProjetId = reader.IsDBNull(5) ? (int?)null : reader.GetInt32(5),
                        DevChiffreurId = reader.IsDBNull(6) ? (int?)null : reader.GetInt32(6),
                        Type = (TypeDemande)reader.GetInt32(7),
                        Criticite = (Criticite)reader.GetInt32(8),
                        Statut = (StatutDemande)reader.GetInt32(9),
                        DateCreation = DateTime.Parse(reader.GetString(10)),
                        DateValidationChiffrage = reader.IsDBNull(11) ? (DateTime?)null : DateTime.Parse(reader.GetString(11)),
                        DateAcceptation = reader.IsDBNull(12) ? (DateTime?)null : DateTime.Parse(reader.GetString(12)),
                        DateLivraison = reader.IsDBNull(13) ? (DateTime?)null : DateTime.Parse(reader.GetString(13)),
                        ChiffrageEstimeHeures = reader.IsDBNull(14) ? (double?)null : reader.GetDouble(14),
                        ChiffrageReelHeures = reader.IsDBNull(15) ? (double?)null : reader.GetDouble(15),
                        DatePrevisionnelleImplementation = reader.IsDBNull(16) ? (DateTime?)null : DateTime.Parse(reader.GetString(16)),
                        JustificationRefus = reader.IsDBNull(17) ? null : reader.GetString(17),
                        EstArchivee = reader.GetInt32(18) == 1
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
                            cmd.CommandText = @"INSERT INTO Demandes (Titre, Description, DemandeurId, BusinessAnalystId, ChefProjetId, DevChiffreurId,
                                Type, Criticite, Statut, DateCreation, DateValidationChiffrage, DateAcceptation, DateLivraison,
                                ChiffrageEstimeHeures, ChiffrageReelHeures, DatePrevisionnelleImplementation, JustificationRefus, EstArchivee)
                                VALUES (@Titre, @Description, @DemandeurId, @BusinessAnalystId, @ChefProjetId, @DevChiffreurId,
                                @Type, @Criticite, @Statut, @DateCreation, @DateValidationChiffrage, @DateAcceptation, @DateLivraison,
                                @ChiffrageEstimeHeures, @ChiffrageReelHeures, @DatePrevisionnelleImplementation, @JustificationRefus, @EstArchivee);
                                SELECT last_insert_rowid();";
                        }
                        else
                        {
                            cmd.CommandText = @"UPDATE Demandes SET Titre = @Titre, Description = @Description, DemandeurId = @DemandeurId,
                                BusinessAnalystId = @BusinessAnalystId, ChefProjetId = @ChefProjetId, DevChiffreurId = @DevChiffreurId,
                                Type = @Type, Criticite = @Criticite, Statut = @Statut, DateValidationChiffrage = @DateValidationChiffrage,
                                DateAcceptation = @DateAcceptation, DateLivraison = @DateLivraison, ChiffrageEstimeHeures = @ChiffrageEstimeHeures,
                                ChiffrageReelHeures = @ChiffrageReelHeures, DatePrevisionnelleImplementation = @DatePrevisionnelleImplementation,
                                JustificationRefus = @JustificationRefus, EstArchivee = @EstArchivee WHERE Id = @Id";
                            cmd.Parameters.AddWithValue("@Id", demande.Id);
                        }

                        cmd.Parameters.AddWithValue("@Titre", demande.Titre);
                        cmd.Parameters.AddWithValue("@Description", demande.Description ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@DemandeurId", demande.DemandeurId);
                        cmd.Parameters.AddWithValue("@BusinessAnalystId", demande.BusinessAnalystId ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@ChefProjetId", demande.ChefProjetId ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@DevChiffreurId", demande.DevChiffreurId ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Type", (int)demande.Type);
                        cmd.Parameters.AddWithValue("@Criticite", (int)demande.Criticite);
                        cmd.Parameters.AddWithValue("@Statut", (int)demande.Statut);
                        cmd.Parameters.AddWithValue("@DateCreation", demande.DateCreation.ToString("yyyy-MM-dd HH:mm:ss"));
                        cmd.Parameters.AddWithValue("@DateValidationChiffrage", demande.DateValidationChiffrage?.ToString("yyyy-MM-dd HH:mm:ss") ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@DateAcceptation", demande.DateAcceptation?.ToString("yyyy-MM-dd HH:mm:ss") ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@DateLivraison", demande.DateLivraison?.ToString("yyyy-MM-dd HH:mm:ss") ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@ChiffrageEstimeHeures", demande.ChiffrageEstimeHeures ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@ChiffrageReelHeures", demande.ChiffrageReelHeures ?? (object)DBNull.Value);
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

        // Stubs pour les autres méthodes
        public List<BacklogItem> GetBacklogItems()
        {
            var items = new List<BacklogItem>();
            using (var conn = GetConnection())
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("SELECT * FROM BacklogItems WHERE EstArchive = 0", conn))
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
                            DemandeId = reader.IsDBNull(18) ? (int?)null : reader.GetInt32(18)
                        });
                    }
                }
            }
            return items;
        }
        
        public List<BacklogItem> GetBacklog() { return GetBacklogItems(); }
        
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
        
        public List<Commentaire> GetCommentaires() { return new List<Commentaire>(); }
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
                                    DateCreation, DateDerniereMaj, EstArchive, SprintId, DemandeId) 
                                    VALUES (@Titre, @Description, @ProjetId, @DevId, @Type, @Priorite, @Statut, 
                                    @Points, @ChiffrageHeures, @TempsReelHeures, @DateFinAttendue, @DateDebut, @DateFin, 
                                    @DateCreation, @DateDerniereMaj, @EstArchive, @SprintId, @DemandeId);
                                    SELECT last_insert_rowid();";
                            }
                            else
                            {
                                cmd.CommandText = @"UPDATE BacklogItems SET Titre = @Titre, Description = @Description, 
                                    ProjetId = @ProjetId, DevId = @DevId, Type = @Type, Priorite = @Priorite, Statut = @Statut, 
                                    Points = @Points, ChiffrageHeures = @ChiffrageHeures, TempsReelHeures = @TempsReelHeures, 
                                    DateFinAttendue = @DateFinAttendue, DateDebut = @DateDebut, DateFin = @DateFin, 
                                    DateDerniereMaj = @DateDerniereMaj, EstArchive = @EstArchive, SprintId = @SprintId, DemandeId = @DemandeId 
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
        
        public Commentaire AddCommentaire(Commentaire commentaire) { return commentaire; }
        public Commentaire AddOrUpdateCommentaire(Commentaire commentaire) { return commentaire; }
        public void AddHistorique(HistoriqueModification historique) { }
        public HistoriqueModification AddOrUpdateHistoriqueModification(HistoriqueModification historique) { return historique; }
        public PokerSession AddOrUpdatePokerSession(PokerSession session) { return session; }
        public PokerVote AddPokerVote(PokerVote vote) { return vote; }
        public Disponibilite AddOrUpdateDisponibilite(Disponibilite disponibilite) { return disponibilite; }
    }
}

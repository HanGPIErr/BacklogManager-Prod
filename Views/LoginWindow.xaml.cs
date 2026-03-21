using System;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Windows;
using System.Data.SQLite;
using BacklogManager.Services;
using BacklogManager.Services.Sync;
using BacklogManager.Domain;

namespace BacklogManager.Views
{
    public partial class LoginWindow : Window
    {
        private readonly AuthenticationService _authService;
        private readonly InitializationService _initService;

        public LoginWindow()
        {
            InitializeComponent();
            
            // ─── Initialisation de la base de données ─────────────────────────
            // Lire les clés de synchronisation depuis config.ini
            string nasSyncPath     = ReadConfigKey("NasSyncPath");
            string localDbOverride = ReadConfigKey("LocalDatabasePath");

            IDatabase database;
            SyncEngine syncEngine = null;

            if (!string.IsNullOrWhiteSpace(nasSyncPath))
            {
                // ─── Mode local-first + sync NAS ─────────────────────────────
                try
                {
                    var localDbFactory = new LocalDatabaseFactory(
                        string.IsNullOrWhiteSpace(localDbOverride) ? null : localDbOverride);
                    string localDbPath = localDbFactory.LocalDbPath;
                    bool isNewLocalDb  = !File.Exists(localDbPath);

                    // ── Amorçage initial : copier la DB réseau vers local.db ──
                    if (isNewLocalDb)
                    {
                        SeedLocalDbFromNetworkDb(localDbPath);
                    }
                    else
                    {
                        // Si la DB réseau a été supprimée/recréée, re-seeder depuis le réseau
                        ReseedIfNetworkDbReset(localDbPath);
                    }
                    // Si local.db existe déjà, on la garde telle quelle.
                    // La réconciliation au démarrage (SyncEngine) rattrapera les ops manquantes.
                    // En cas de curseur périmé, TryRebuildFromSnapshot reconstruit depuis le snapshot NAS.

                    localDbFactory.GetOrCreateLocalDb(); // sync tables (IF NOT EXISTS)

                    // SqliteDatabase local (WAL)
                    var innerDb = new SqliteDatabase(localDbPath);

                    // ClientId unique et stable : stocké dans SyncState, généré au premier lancement.
                    // Combine MachineName + GUID tronqué pour éviter toute collision
                    // (même en cas de 2 machines avec le même hostname).
                    string clientId = localDbFactory.GetSyncState("ClientId");
                    if (string.IsNullOrWhiteSpace(clientId))
                    {
                        clientId = Environment.MachineName.ToUpperInvariant()
                                   + "_" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpperInvariant();
                        localDbFactory.SetSyncState("ClientId", clientId);
                        LoggingService.Instance.LogInfo($"[LoginWindow] Nouveau ClientId généré : {clientId}");

                        // Décaler les séquences AUTOINCREMENT pour éviter les collisions d'IDs
                        // entre postes ayant été amorcés depuis la même DB réseau
                        localDbFactory.OffsetAutoIncrementSequences(clientId);
                    }

                    // Construire les composants de synchronisation
                    var nasStore    = new NasOperationStore(nasSyncPath);
                    // EnsureDirectoriesExist non bloquant — le NAS peut être temporairement indisponible
                    try { nasStore.EnsureDirectoriesExist(); }
                    catch (Exception exNas)
                    {
                        LoggingService.Instance.LogWarning(
                            $"[LoginWindow] NAS indisponible au démarrage (sera réessayé par SyncEngine) : {exNas.Message}");
                    }

                    var leaseManager  = new LeaseManager(nasStore.LeasesPath, clientId);
                    var snapshotMgr   = new SnapshotManager(nasStore, leaseManager, localDbPath, clientId);
                    var syncApplier   = new SyncApplier(localDbFactory, clientId);
                    syncEngine        = new SyncEngine(localDbFactory, nasStore, syncApplier, snapshotMgr, clientId);

                    // Pousser un snapshot initial si premier client et NAS dispo
                    if (isNewLocalDb)
                    {
                        try { snapshotMgr.ForceInitialSnapshot(localDbFactory); }
                        catch (Exception exSnap)
                        {
                            LoggingService.Instance.LogWarning(
                                $"[LoginWindow] Snapshot initial impossible (sera créé plus tard) : {exSnap.Message}");
                        }
                    }

                    // Décorateur qui journalise chaque écriture
                    string windowsUser = WindowsIdentity.GetCurrent().Name;
                    if (windowsUser.Contains("\\")) windowsUser = windowsUser.Split('\\')[1];

                    database = new SyncedDatabase(innerDb, localDbFactory, windowsUser, clientId);
                    ((SyncedDatabase)database).SetSyncEngine(syncEngine);

                    LoggingService.Instance.LogInfo($"[LoginWindow] Mode local-first activé. NAS={nasSyncPath}, localDb={localDbPath}");
                }
                catch (Exception ex)
                {
                    // Erreur critique (local.db inaccessible, etc.)
                    // On NE tombe PAS silencieusement sur la DB réseau → split-brain
                    LoggingService.Instance.LogError("[LoginWindow] Échec init sync", ex);
                    syncEngine = null;

                    // Tenter de démarrer en mode classique avec avertissement visible
                    database = new SqliteDatabase();
                    MessageBox.Show(
                        $"Impossible d'initialiser le mode synchronisation.\n\n" +
                        $"L'application démarre en mode classique (base de données directe).\n" +
                        $"Les modifications ne seront PAS synchronisées avec les autres postes.\n\n" +
                        $"Erreur : {ex.Message}",
                        "Avertissement - Mode synchronisation",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            else
            {
                // ─── Mode classique (DB partagée NAS ou locale, config.ini) ──
                database = new SqliteDatabase();
                LoggingService.Instance.LogInfo("[LoginWindow] Mode classique (DatabasePath de config.ini).");
            }
            // ─────────────────────────────────────────────────────────────────

            _authService = new AuthenticationService(database);
            _initService = new InitializationService(database);
            
            // Initialiser le service de configuration IA
            AIConfigService.Initialize(database);
            
            // Assigner les services à App pour qu'ils soient accessibles partout
            var app = Application.Current as App;
            if (app != null)
            {
                app.AuthService  = _authService;
                app.Database     = database;
                app.SyncEngine   = syncEngine;
            }
            
            // Initialiser les données par défaut
            _initService.InitializeDefaultData();

            // Démarrer la synchronisation NAS APRÈS InitializeDefaultData() pour éviter
            // que TryRebuildFromSnapshot ne remplace la DB pendant l'initialisation.
            if (syncEngine != null)
                syncEngine.Start();
            
            // Afficher le username
            AfficherUsername();
            
            // Connexion automatique
            Loaded += (s, e) => TentativeConnexionAutomatique();
            
            // Vérifier les mises à jour en arrière-plan
            Loaded += async (s, e) => await VerifierMisesAJour();
        }

        private async System.Threading.Tasks.Task VerifierMisesAJour()
        {
            try
            {
                var updateService = new UpdateService();
                var versionInfo = await updateService.CheckForUpdatesAsync();

                if (versionInfo != null)
                {
                    // Afficher la fenêtre de mise à jour
                    Dispatcher.Invoke(() =>
                    {
                        var updateWindow = new UpdateWindow(versionInfo, updateService);
                        updateWindow.ShowDialog();
                    });
                }
            }
            catch (Exception ex)
            {
                // Ignorer les erreurs de mise à jour pour ne pas bloquer l'application
                System.Diagnostics.Debug.WriteLine($"Erreur vérification MAJ: {ex.Message}");
            }
        }

        private void TentativeConnexionAutomatique()
        {
            try
            {
                // Authentification Windows pure - récupérer le username Windows de l'utilisateur
                string windowsUsername = WindowsIdentity.GetCurrent().Name;
                if (windowsUsername.Contains("\\"))
                {
                    windowsUsername = windowsUsername.Split('\\')[1];
                }

                // Gestion des droits admin automatiques
                var database = ((App)Application.Current).Database;
                var roleAdmin = database.GetRoles().FirstOrDefault(r => r.Type == RoleType.Administrateur);
                var utilisateurs = database.GetUtilisateurs();
                
                // CAS 1 : Premier utilisateur qui se connecte = auto-admin
                if (!utilisateurs.Any())
                {
                    var premierUtilisateur = new Utilisateur
                    {
                        UsernameWindows = windowsUsername,
                        Nom = "",
                        Prenom = windowsUsername,
                        Email = $"{windowsUsername}@bnpparibas.com",
                        RoleId = roleAdmin?.Id ?? 1,
                        Actif = true,
                        DateCreation = DateTime.Now,
                        Statut = "BAU"
                    };
                    database.AddOrUpdateUtilisateur(premierUtilisateur);
                }
                // CAS 2 : Utilisateur spécial 610506 = toujours admin
                else if (windowsUsername == "610506")
                {
                    var utilisateur = utilisateurs.FirstOrDefault(u => u.UsernameWindows == "610506");
                    
                    if (utilisateur == null)
                    {
                        // Créer le compte admin automatiquement
                        utilisateur = new Utilisateur
                        {
                            UsernameWindows = "610506",
                            Nom = "Admin",
                            Prenom = "Super",
                            Email = "610506@bnpparibas.com",
                            RoleId = roleAdmin?.Id ?? 1,
                            Actif = true,
                            DateCreation = DateTime.Now,
                            Statut = "BAU"
                        };
                        database.AddOrUpdateUtilisateur(utilisateur);
                    }
                    else if (utilisateur.RoleId != roleAdmin?.Id)
                    {
                        // S'assurer qu'il a le rôle admin
                        utilisateur.RoleId = roleAdmin?.Id ?? 1;
                        utilisateur.Actif = true;
                        database.AddOrUpdateUtilisateur(utilisateur);
                    }
                }

                // Tenter la connexion avec le username Windows
                bool success = _authService.LoginWithUsername(windowsUsername);
                if (success)
                {
                    // Ouvrir directement la fenêtre principale
                    var mainWindow = new MainWindow(_authService);
                    mainWindow.Show();
                    this.Close();
                    return;
                }

                // Si la connexion échoue, afficher le message
                Dispatcher.Invoke(() =>
                {
                    TxtUsername.Text = $"Compte non enregistré";
                    TxtStatut.Text = "Veuillez contacter votre administrateur";
                    TxtErreur.Text = $"Votre compte Windows '{windowsUsername}' n'est pas enregistré dans l'application.\n\nContactez votre administrateur pour créer votre compte utilisateur.";
                    TxtErreur.Visibility = Visibility.Visible;
                    BtnConnecter.IsEnabled = true;
                });
            }
            catch (Exception ex)
            {
                // En cas d'erreur, afficher un message
                Dispatcher.Invoke(() =>
                {
                    TxtUsername.Text = "Erreur de connexion";
                    TxtStatut.Text = "Une erreur est survenue";
                    TxtErreur.Text = $"Erreur: {ex.Message}";
                    TxtErreur.Visibility = Visibility.Visible;
                    BtnConnecter.IsEnabled = true;
                });
            }
        }

        /// <summary>Lit une clé de config.ini (section [Sync] ou toute section).</summary>
        private static string ReadConfigKey(string key)
        {
            try
            {
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.ini");
                if (!File.Exists(configPath)) return null;
                foreach (var line in File.ReadAllLines(configPath, System.Text.Encoding.UTF8))
                {
                    var trimmed = line.Trim();
                    if (trimmed.StartsWith(key + "="))
                        return trimmed.Substring(key.Length + 1).Trim().Trim('"', '\'');
                }
            }
            catch { /* ignorer */ }
            return null;
        }

        /// <summary>
        /// Amorce local.db depuis la DB réseau (DatabasePath de config.ini) au premier lancement sync.
        /// Copie le fichier SQLite entier, puis les sync tables sont ajoutées par-dessus.
        /// </summary>
        private static void SeedLocalDbFromNetworkDb(string localDbPath)
        {
            try
            {
                string networkDbPath = ReadConfigKey("DatabasePath");
                if (string.IsNullOrWhiteSpace(networkDbPath)) return;

                // Résoudre chemin relatif
                if (!Path.IsPathRooted(networkDbPath))
                    networkDbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, networkDbPath);

                // Résoudre chemin UNC via drive mapping si nécessaire
                if (networkDbPath.StartsWith("\\\\"))
                    networkDbPath = NetworkPathMapper.MapUncPathToDrive(networkDbPath);

                // Si le chemin résolu est un dossier → chercher backlog.db dedans
                if (Directory.Exists(networkDbPath))
                {
                    networkDbPath = Path.Combine(networkDbPath, "backlog.db");
                    LoggingService.Instance.LogInfo(
                        $"[LoginWindow] DatabasePath est un dossier, fichier source : {networkDbPath}");
                }

                if (!File.Exists(networkDbPath))
                {
                    LoggingService.Instance.LogInfo(
                        $"[LoginWindow] Aucune DB réseau trouvée à {networkDbPath}, démarrage avec DB vide.");
                    return;
                }

                // Créer le dossier parent
                string dir = Path.GetDirectoryName(localDbPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                // Utiliser l'API SQLite BACKUP pour copier de manière atomique et cohérente,
                // même si un autre processus écrit dans la DB réseau simultanément.
                using (var src = new SQLiteConnection($"Data Source={networkDbPath};Version=3;Read Only=True;"))
                using (var dst = new SQLiteConnection($"Data Source={localDbPath};Version=3;"))
                {
                    src.Open();
                    dst.Open();
                    src.BackupDatabase(dst, "main", "main", -1, null, 0);
                }

                LoggingService.Instance.LogInfo(
                    $"[LoginWindow] DB locale amorcée (BACKUP API) depuis {networkDbPath} → {localDbPath}");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogWarning(
                    $"[LoginWindow] Impossible d'amorcer local.db depuis la DB réseau : {ex.Message}");
            }
        }

        /// <summary>
        /// Vérifie si la DB réseau a été supprimée puis recréée (ou est absente).
        /// Si la DB réseau n'existe plus, supprime la DB locale pour repartir de zéro.
        /// Si la DB réseau est plus récente et vide (= reset), re-seede.
        /// </summary>
        private static void ReseedIfNetworkDbReset(string localDbPath)
        {
            try
            {
                string networkDbPath = ResolveNetworkDbPath();
                if (networkDbPath == null) return;

                if (!File.Exists(networkDbPath))
                {
                    // La DB réseau a été supprimée → supprimer local.db pour repartir proprement
                    LoggingService.Instance.LogInfo(
                        "[LoginWindow] DB réseau absente — suppression de local.db pour reseed au prochain démarrage.");
                    try
                    {
                        // Supprimer local.db et fichiers WAL/SHM associés
                        File.Delete(localDbPath);
                        if (File.Exists(localDbPath + "-wal")) File.Delete(localDbPath + "-wal");
                        if (File.Exists(localDbPath + "-shm")) File.Delete(localDbPath + "-shm");
                    }
                    catch (Exception exDel)
                    {
                        LoggingService.Instance.LogWarning(
                            $"[LoginWindow] Impossible de supprimer local.db : {exDel.Message}");
                    }
                    return;
                }

                // Vérifier si la DB réseau est « vide » (remise à zéro)
                // Heuristique : si la table Utilisateurs existe mais est vide, c'est un reset
                try
                {
                    using (var conn = new SQLiteConnection($"Data Source={networkDbPath};Version=3;Read Only=True;"))
                    {
                        conn.Open();
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = "SELECT COUNT(*) FROM Utilisateurs";
                            long count = Convert.ToInt64(cmd.ExecuteScalar());
                            if (count == 0)
                            {
                                LoggingService.Instance.LogInfo(
                                    "[LoginWindow] DB réseau vide détectée (reset) — suppression de local.db pour reseed.");
                                conn.Close();
                                try
                                {
                                    File.Delete(localDbPath);
                                    if (File.Exists(localDbPath + "-wal")) File.Delete(localDbPath + "-wal");
                                    if (File.Exists(localDbPath + "-shm")) File.Delete(localDbPath + "-shm");
                                }
                                catch (Exception exDel)
                                {
                                    LoggingService.Instance.LogWarning(
                                        $"[LoginWindow] Impossible de supprimer local.db : {exDel.Message}");
                                }
                            }
                        }
                    }
                }
                catch (Exception exCheck)
                {
                    // Table Utilisateurs n'existe peut-être pas → DB réseau corrompue/vide
                    LoggingService.Instance.LogWarning(
                        $"[LoginWindow] Impossible de lire la DB réseau pour vérifier reset : {exCheck.Message}");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogWarning(
                    $"[LoginWindow] Erreur vérification reset réseau : {ex.Message}");
            }
        }

        /// <summary>
        /// Résout le chemin de la DB réseau depuis config.ini (factorisation).
        /// </summary>
        private static string ResolveNetworkDbPath()
        {
            string networkDbPath = ReadConfigKey("DatabasePath");
            if (string.IsNullOrWhiteSpace(networkDbPath)) return null;

            if (!Path.IsPathRooted(networkDbPath))
                networkDbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, networkDbPath);

            if (networkDbPath.StartsWith("\\\\"))
                networkDbPath = NetworkPathMapper.MapUncPathToDrive(networkDbPath);

            if (Directory.Exists(networkDbPath))
                networkDbPath = Path.Combine(networkDbPath, "backlog.db");

            return networkDbPath;
        }

        /// <summary>
        private void AfficherUsername()
        {
            try
            {
                TxtUsername.Text = "Connexion automatique en cours...";
                TxtUsernameInput.Text = "";
                TxtStatut.Text = "";
            }
            catch (Exception ex)
            {
                TxtUsername.Text = "Erreur de connexion";
                TxtErreur.Text = ex.Message;
                TxtErreur.Visibility = Visibility.Visible;
            }
        }

        private void BtnConnecter_Click(object sender, RoutedEventArgs e)
        {
            TxtErreur.Visibility = Visibility.Collapsed;
            BtnConnecter.IsEnabled = false;
            TxtStatut.Text = "Connexion en cours...";

            try
            {
                string usernameToUse = TxtUsernameInput.Text.Trim();
                
                if (string.IsNullOrWhiteSpace(usernameToUse))
                {
                    // Utiliser l'authentification Windows automatique
                    usernameToUse = WindowsIdentity.GetCurrent().Name;
                    if (usernameToUse.Contains("\\"))
                    {
                        usernameToUse = usernameToUse.Split('\\')[1];
                    }
                }

                bool success = _authService.LoginWithUsername(usernameToUse);

                if (success)
                {
                    var user = _authService.CurrentUser;
                    var role = _authService.GetCurrentUserRole();

                    TxtStatut.Text = string.Format("Connecté avec succès en tant que {0} ({1})", user.Prenom + " " + user.Nom, role?.Nom);

                    // Ouvrir la fenêtre principale
                    var mainWindow = new MainWindow(_authService);
                    mainWindow.Show();
                    this.Close();
                }
                else
                {
                    TxtErreur.Text = string.Format("Accès refusé: Le compte '{0}' n'est pas enregistré dans l'application.\n\nContactez votre administrateur pour créer votre compte utilisateur.", usernameToUse);
                    TxtErreur.Visibility = Visibility.Visible;
                    TxtStatut.Text = "";
                    BtnConnecter.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                TxtErreur.Text = string.Format("Erreur lors de la connexion: {0}", ex.Message);
                TxtErreur.Visibility = Visibility.Visible;
                TxtStatut.Text = "";
                BtnConnecter.IsEnabled = true;
            }
        }
    }
}

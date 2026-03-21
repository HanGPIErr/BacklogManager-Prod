using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BacklogManager.Services;
using BacklogManager.Services.Sync;
using BacklogManager.Views.Pages;

namespace BacklogManager.Views
{
    public partial class AdministrationView : UserControl
    {
        private readonly IDatabase _database;
        private readonly AuditLogService _auditLogService;
        private readonly AuthenticationService _authService;

        public AdministrationView(IDatabase database, AuditLogService auditLogService = null, AuthenticationService authService = null)
        {
            InitializeComponent();
            _database = database;
            _auditLogService = auditLogService;
            _authService = authService;

            // Initialiser les textes traduits
            InitialiserTextes();

            // Charger les pages dans les frames
            ChargerPages();
        }

        private void InitialiserTextes()
        {
            // Header principal
            TxtAdministrationTitle.Text = LocalizationService.Instance.GetString("Administration_Title");
            TxtAdministrationDescription.Text = LocalizationService.Instance.GetString("Administration_Description");

            // Onglets principaux
            TxtUsersAndRolesTab.Text = LocalizationService.Instance.GetString("Administration_UsersAndRoles");
            TxtProjectsAndTeamTab.Text = LocalizationService.Instance.GetString("Administration_ProjectsAndTeam");
            TxtReportingTab.Text = LocalizationService.Instance.GetString("Administration_Reporting");
            TxtChatHistoryTab.Text = LocalizationService.Instance.GetString("Administration_ChatHistory");
            TxtAuditLogTab.Text = LocalizationService.Instance.GetString("Administration_AuditLog");

            // Sous-onglets Utilisateurs & Rôles
            TxtUsersSubTab.Text = LocalizationService.Instance.GetString("Administration_Users");
            TxtRolesSubTab.Text = LocalizationService.Instance.GetString("Administration_Roles");

            // Sous-onglets Projets & Équipe
            TxtProgramsSubTab.Text = LocalizationService.Instance.GetString("Administration_Programs");
            TxtProjectsSubTab.Text = LocalizationService.Instance.GetString("Administration_Projects");
            TxtTeamsSubTab.Text = LocalizationService.Instance.GetString("Administration_Teams");

            // Sous-onglets Journal d'audit
            TxtAuditLogSubTab.Text = LocalizationService.Instance.GetString("Administration_AuditLog");
            TxtAuditStatsSubTab.Text = LocalizationService.Instance.GetString("Administration_AuditStats");

            // Bouton Historique Chat
            TxtOpenChatHistory.Text = LocalizationService.Instance.GetString("Administration_OpenChatHistory");

            // Configuration IA
            TxtAIConfigTab.Text = LocalizationService.Instance.GetString("Administration_AIConfig");
            TxtAIConfigTitle.Text = "🤖 " + LocalizationService.Instance.GetString("AIConfig_Title");
            TxtAIConfigDescription.Text = LocalizationService.Instance.GetString("AIConfig_Description");
            LblApiUrl.Text = LocalizationService.Instance.GetString("AIConfig_UrlLabel");
            LblAIModel.Text = LocalizationService.Instance.GetString("AIConfig_ModelLabel");
            LblTokenAPI.Text = LocalizationService.Instance.GetString("AIConfig_TokenLabel");
            TxtTokenHelp.Text = LocalizationService.Instance.GetString("AIConfig_TokenHelp");
            BtnTestToken.Content = LocalizationService.Instance.GetString("AIConfig_TestToken");
            BtnSaveToken.Content = LocalizationService.Instance.GetString("AIConfig_Save");
            TxtAIConfigWarning.Text = LocalizationService.Instance.GetString("AIConfig_Warning");

            // S'abonner aux changements de langue
            LocalizationService.Instance.PropertyChanged += (s, e) =>
            {
                TxtAdministrationTitle.Text = LocalizationService.Instance.GetString("Administration_Title");
                TxtAdministrationDescription.Text = LocalizationService.Instance.GetString("Administration_Description");
                TxtUsersAndRolesTab.Text = LocalizationService.Instance.GetString("Administration_UsersAndRoles");
                TxtProjectsAndTeamTab.Text = LocalizationService.Instance.GetString("Administration_ProjectsAndTeam");
                TxtReportingTab.Text = LocalizationService.Instance.GetString("Administration_Reporting");
                TxtChatHistoryTab.Text = LocalizationService.Instance.GetString("Administration_ChatHistory");
                TxtAuditLogTab.Text = LocalizationService.Instance.GetString("Administration_AuditLog");
                TxtUsersSubTab.Text = LocalizationService.Instance.GetString("Administration_Users");
                TxtRolesSubTab.Text = LocalizationService.Instance.GetString("Administration_Roles");
                TxtProgramsSubTab.Text = LocalizationService.Instance.GetString("Administration_Programs");
                TxtProjectsSubTab.Text = LocalizationService.Instance.GetString("Administration_Projects");
                TxtTeamsSubTab.Text = LocalizationService.Instance.GetString("Administration_Teams");
                TxtAuditLogSubTab.Text = LocalizationService.Instance.GetString("Administration_AuditLog");
                TxtAuditStatsSubTab.Text = LocalizationService.Instance.GetString("Administration_AuditStats");
                TxtOpenChatHistory.Text = LocalizationService.Instance.GetString("Administration_OpenChatHistory");
                TxtAIConfigTab.Text = LocalizationService.Instance.GetString("Administration_AIConfig");
                TxtAIConfigTitle.Text = "🤖 " + LocalizationService.Instance.GetString("AIConfig_Title");
                TxtAIConfigDescription.Text = LocalizationService.Instance.GetString("AIConfig_Description");
                LblApiUrl.Text = LocalizationService.Instance.GetString("AIConfig_UrlLabel");
                LblAIModel.Text = LocalizationService.Instance.GetString("AIConfig_ModelLabel");
                LblTokenAPI.Text = LocalizationService.Instance.GetString("AIConfig_TokenLabel");
                TxtTokenHelp.Text = LocalizationService.Instance.GetString("AIConfig_TokenHelp");
                BtnTestToken.Content = LocalizationService.Instance.GetString("AIConfig_TestToken");
                BtnSaveToken.Content = LocalizationService.Instance.GetString("AIConfig_Save");
                TxtAIConfigWarning.Text = LocalizationService.Instance.GetString("AIConfig_Warning");
            };
        }

        private void ChargerPages()
        {
            try
            {
                // Charger la première page de chaque groupe par défaut
                if (FrameUtilisateursRoles != null)
                {
                    FrameUtilisateursRoles.Content = new GestionUtilisateursPage(_database, _auditLogService);
                }
                
                if (FrameProjetsEquipe != null)
                {
                    FrameProjetsEquipe.Content = new GestionProgrammesPage(_database);
                }
                
                if (FrameAudit != null)
                {
                    FrameAudit.Content = new AuditLogPage(_database, _auditLogService);
                }
                
                // Charger la page de Reporting seulement si authService est disponible
                if (FrameReporting != null && _authService != null)
                {
                    try
                    {
                        FrameReporting.Content = new AdminReportingView(_database, _authService);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Erreur lors du chargement du Reporting: {ex.Message}\n\nStackTrace: {ex.StackTrace}", 
                            "Erreur Reporting", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }

                // Charger la configuration IA
                ChargerConfigurationIA();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des pages: {ex.Message}\n\nStackTrace: {ex.StackTrace}", 
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ChargerConfigurationIA()
        {
            try
            {
                // Charger l'URL et le modèle (constantes, toujours disponibles)
                TxtAPIUrl.Text = AIConfigService.API_URL;
                TxtAIModel.Text = AIConfigService.MODEL;

                try
                {
                    // Charger le token actuel depuis la base de données
                    var token = AIConfigService.GetToken();
                    TxtAPIToken.Text = token;

                    // Afficher le statut du token
                    if (AIConfigService.IsTokenConfigured())
                    {
                        BorderTokenStatus.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E8F5E9"));
                        BorderTokenStatus.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
                        TxtTokenStatusIcon.Text = "✓";
                        TxtTokenStatusIcon.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
                        TxtTokenStatus.Text = LocalizationService.Instance.GetString("AIConfig_TokenConfigured");
                        TxtTokenStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2E7D32"));
                        BorderTokenStatus.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        BorderTokenStatus.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF3E0"));
                        BorderTokenStatus.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF9800"));
                        TxtTokenStatusIcon.Text = "⚠";
                        TxtTokenStatusIcon.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F57C00"));
                        TxtTokenStatus.Text = LocalizationService.Instance.GetString("AIConfig_TokenNotConfigured");
                        TxtTokenStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E65100"));
                        BorderTokenStatus.Visibility = Visibility.Visible;
                    }
                }
                catch (Exception dbEx)
                {
                    // Si erreur d'accès à la DB, afficher un message mais ne pas bloquer l'interface
                    TxtAPIToken.Text = "";
                    BorderTokenStatus.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFEBEE"));
                    BorderTokenStatus.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F44336"));
                    TxtTokenStatusIcon.Text = "✗";
                    TxtTokenStatusIcon.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F44336"));
                    TxtTokenStatus.Text = $"Erreur d'accès à la base de données : {dbEx.Message}";
                    TxtTokenStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C62828"));
                    BorderTokenStatus.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                // En cas d'erreur générale, logger mais ne pas afficher de popup
                System.Diagnostics.Debug.WriteLine($"Erreur lors du chargement de la configuration IA: {ex.Message}");
            }
        }

        // Gestion des sous-onglets Utilisateurs & Rôles
        private void BtnSousOngletUtilisateurs_Click(object sender, RoutedEventArgs e)
        {
            // Mettre à jour les styles des boutons
            BtnSousOngletUtilisateurs.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00915A"));
            BtnSousOngletUtilisateurs.Foreground = Brushes.White;
            BtnSousOngletUtilisateurs.FontWeight = FontWeights.SemiBold;

            BtnSousOngletRoles.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0"));
            BtnSousOngletRoles.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6D6D6D"));
            BtnSousOngletRoles.FontWeight = FontWeights.Normal;

            // Charger le contenu
            FrameUtilisateursRoles.Content = new GestionUtilisateursPage(_database, _auditLogService);
        }

        private void BtnSousOngletRoles_Click(object sender, RoutedEventArgs e)
        {
            // Mettre à jour les styles des boutons
            BtnSousOngletRoles.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00915A"));
            BtnSousOngletRoles.Foreground = Brushes.White;
            BtnSousOngletRoles.FontWeight = FontWeights.SemiBold;

            BtnSousOngletUtilisateurs.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0"));
            BtnSousOngletUtilisateurs.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6D6D6D"));
            BtnSousOngletUtilisateurs.FontWeight = FontWeights.Normal;

            // Charger le contenu
            FrameUtilisateursRoles.Content = new GestionRolesPage(_database, _auditLogService);
        }

        // Gestion des sous-onglets Projets & Équipe
        private void BtnSousOngletProgrammes_Click(object sender, RoutedEventArgs e)
        {
            // Mettre à jour les styles des boutons
            BtnSousOngletProgrammes.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00915A"));
            BtnSousOngletProgrammes.Foreground = Brushes.White;
            BtnSousOngletProgrammes.FontWeight = FontWeights.SemiBold;

            BtnSousOngletProjets.Background = Brushes.Transparent;
            BtnSousOngletProjets.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6D6D6D"));
            BtnSousOngletProjets.FontWeight = FontWeights.Normal;

            BtnSousOngletEquipes.Background = Brushes.Transparent;
            BtnSousOngletEquipes.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6D6D6D"));
            BtnSousOngletEquipes.FontWeight = FontWeights.Normal;

            // Charger le contenu
            FrameProjetsEquipe.Content = new GestionProgrammesPage(_database);
        }

        private void BtnSousOngletProjets_Click(object sender, RoutedEventArgs e)
        {
            // Mettre à jour les styles des boutons
            BtnSousOngletProjets.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00915A"));
            BtnSousOngletProjets.Foreground = Brushes.White;
            BtnSousOngletProjets.FontWeight = FontWeights.SemiBold;

            BtnSousOngletProgrammes.Background = Brushes.Transparent;
            BtnSousOngletProgrammes.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6D6D6D"));
            BtnSousOngletProgrammes.FontWeight = FontWeights.Normal;

            BtnSousOngletEquipes.Background = Brushes.Transparent;
            BtnSousOngletEquipes.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6D6D6D"));
            BtnSousOngletEquipes.FontWeight = FontWeights.Normal;

            // Charger le contenu
            FrameProjetsEquipe.Content = new GestionProjetsPage(_database, _auditLogService);
        }

        private void BtnSousOngletEquipes_Click(object sender, RoutedEventArgs e)
        {
            // Mettre à jour les styles des boutons
            BtnSousOngletEquipes.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00915A"));
            BtnSousOngletEquipes.Foreground = Brushes.White;
            BtnSousOngletEquipes.FontWeight = FontWeights.SemiBold;

            BtnSousOngletProgrammes.Background = Brushes.Transparent;
            BtnSousOngletProgrammes.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6D6D6D"));
            BtnSousOngletProgrammes.FontWeight = FontWeights.Normal;

            BtnSousOngletProjets.Background = Brushes.Transparent;
            BtnSousOngletProjets.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6D6D6D"));
            BtnSousOngletProjets.FontWeight = FontWeights.Normal;

            // Charger le contenu avec la nouvelle page de gestion des équipes
            FrameProjetsEquipe.Content = new GestionEquipesPage(_database);
        }

        // Gestion des sous-onglets Journal d'audit
        private void BtnSousOngletAuditLog_Click(object sender, RoutedEventArgs e)
        {
            BtnSousOngletAuditLog.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00915A"));
            BtnSousOngletAuditLog.Foreground = Brushes.White;
            BtnSousOngletAuditLog.FontWeight = FontWeights.SemiBold;

            BtnSousOngletAuditStats.Background = Brushes.Transparent;
            BtnSousOngletAuditStats.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6D6D6D"));
            BtnSousOngletAuditStats.FontWeight = FontWeights.Normal;

            FrameAudit.Content = new AuditLogPage(_database, _auditLogService);
        }

        private void BtnSousOngletAuditStats_Click(object sender, RoutedEventArgs e)
        {
            BtnSousOngletAuditStats.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00915A"));
            BtnSousOngletAuditStats.Foreground = Brushes.White;
            BtnSousOngletAuditStats.FontWeight = FontWeights.SemiBold;

            BtnSousOngletAuditLog.Background = Brushes.Transparent;
            BtnSousOngletAuditLog.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6D6D6D"));
            BtnSousOngletAuditLog.FontWeight = FontWeights.Normal;

            FrameAudit.Content = new AuditStatsPage(_database);
        }

        private void BtnOuvrirHistoriqueChat_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var chatHistoryService = new ChatHistoryService(_database);
                var historiqueWindow = new ChatHistoriqueAdminWindow(chatHistoryService)
                {
                    Owner = Window.GetWindow(this)
                };
                historiqueWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ouverture de l'historique des conversations : {ex.Message}", 
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Gestion de la configuration IA
        private void BtnSaveToken_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var token = TxtAPIToken.Text?.Trim();
                
                if (string.IsNullOrWhiteSpace(token))
                {
                    MessageBox.Show("Veuillez saisir un token valide.", 
                        "Token manquant", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Vérifier le format basique du JWT (3 sections séparées par des points)
                var parts = token.Split('.');
                if (parts.Length != 3)
                {
                    MessageBox.Show("Le token ne semble pas être un JWT valide.\n\n" +
                        "Un JWT doit avoir 3 sections séparées par des points : Header.Payload.Signature", 
                        "Format invalide", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Enregistrer le token
                AIConfigService.SetToken(token);

                // Afficher le statut de succès
                BorderTokenStatus.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E8F5E9"));
                BorderTokenStatus.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
                TxtTokenStatusIcon.Text = "✓";
                TxtTokenStatusIcon.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
                TxtTokenStatus.Text = LocalizationService.Instance.GetString("AIConfig_TokenSaved");
                TxtTokenStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2E7D32"));
                BorderTokenStatus.Visibility = Visibility.Visible;

                MessageBox.Show("Le token API a été enregistré avec succès.\n\n" +
                    "Les fonctionnalités IA utilisent maintenant ce nouveau token.", 
                    "Succès", MessageBoxButton.OK, MessageBoxImage.Information);

                // Log dans l'audit si disponible
                _auditLogService?.LogAction("CONFIG_IA", "CONFIGURATION", null, 
                    "Token API", "Token configuré", "Configuration du token API IA");
            }
            catch (Exception ex)
            {
                // Afficher le statut d'erreur
                BorderTokenStatus.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFEBEE"));
                BorderTokenStatus.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F44336"));
                TxtTokenStatusIcon.Text = "✗";
                TxtTokenStatusIcon.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F44336"));
                TxtTokenStatus.Text = "Erreur lors de l'enregistrement";
                TxtTokenStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C62828"));
                BorderTokenStatus.Visibility = Visibility.Visible;

                MessageBox.Show($"Erreur lors de l'enregistrement du token : {ex.Message}", 
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnTestToken_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var token = TxtAPIToken.Text?.Trim();
                
                if (string.IsNullOrWhiteSpace(token))
                {
                    MessageBox.Show("Veuillez saisir un token pour le tester.", 
                        "Token manquant", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Vérifier le format basique du JWT
                var parts = token.Split('.');
                if (parts.Length != 3)
                {
                    // Afficher le statut d'erreur
                    BorderTokenStatus.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFEBEE"));
                    BorderTokenStatus.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F44336"));
                    TxtTokenStatusIcon.Text = "✗";
                    TxtTokenStatusIcon.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F44336"));
                    TxtTokenStatus.Text = "Format de token invalide (doit être au format JWT)";
                    TxtTokenStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C62828"));
                    BorderTokenStatus.Visibility = Visibility.Visible;

                    MessageBox.Show("Le token ne semble pas être un JWT valide.\n\n" +
                        "Un JWT doit avoir 3 sections séparées par des points : Header.Payload.Signature\n\n" +
                        $"Votre token a {parts.Length} section(s).", 
                        "Format invalide", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Afficher le statut de succès (format OK)
                BorderTokenStatus.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E8F5E9"));
                BorderTokenStatus.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
                TxtTokenStatusIcon.Text = "✓";
                TxtTokenStatusIcon.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
                TxtTokenStatus.Text = "Format de token valide (JWT)";
                TxtTokenStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2E7D32"));
                BorderTokenStatus.Visibility = Visibility.Visible;

                MessageBox.Show("Le format du token est valide.\n\n" +
                    "Pour vérifier que le token fonctionne réellement avec l'API, " +
                    "enregistrez-le et testez une fonctionnalité IA (analyse d'email, chat, etc.).", 
                    "Format valide", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du test du token : {ex.Message}", 
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        //  TEST UNITAIRE SYNCHRONISATION
        // ═══════════════════════════════════════════════════════════════════

        private SyncTestService _syncTestService;

        private async void BtnLancerTest_Click(object sender, RoutedEventArgs e)
        {
            var app = (App)Application.Current;
            var syncEngine = app.SyncEngine;
            if (syncEngine == null)
            {
                MessageBox.Show("Le moteur de synchronisation n'est pas actif.\nCe test nécessite le mode local-first avec NAS.",
                    "Sync non disponible", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                "Ce test va créer ~36 entités de test dans la base de données.\n" +
                "Il doit être lancé simultanément sur 2 postes.\n\n" +
                "Les entités de test seront préfixées par [TEST_<ClientId>].\n\n" +
                "Voulez-vous continuer ?",
                "Confirmation test synchronisation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            // Récupérer le NasSyncPath et le ClientId
            string nasSyncPath = null;
            string clientId = null;
            try
            {
                nasSyncPath = ReadConfigKey("NasSyncPath");
                if (_database is SyncedDatabase sdb)
                    clientId = sdb.ClientId;
                else
                    clientId = Environment.MachineName.ToUpperInvariant();
            }
            catch { }

            if (string.IsNullOrWhiteSpace(nasSyncPath))
            {
                MessageBox.Show("NasSyncPath non configuré dans config.ini.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // UI
            BtnLancerTest.IsEnabled = false;
            BtnAnnulerTest.IsEnabled = true;
            TxtTestLog.Text = "";
            ProgressTest.Value = 0;
            TxtTestStatus.Text = "Initialisation...";

            _syncTestService = new SyncTestService(_database, syncEngine, clientId, nasSyncPath);

            _syncTestService.LogUpdated += msg =>
            {
                Dispatcher.BeginInvoke((Action)(() =>
                {
                    TxtTestLog.Text += msg + "\n";
                    ScrollTestLog.ScrollToEnd();
                }));
            };

            _syncTestService.ProgressChanged += pct =>
            {
                Dispatcher.BeginInvoke((Action)(() =>
                {
                    ProgressTest.Value = pct;
                    TxtTestStatus.Text = $"Progression : {pct}%";
                }));
            };

            _syncTestService.TestCompleted += testResult =>
            {
                Dispatcher.BeginInvoke((Action)(() =>
                {
                    BtnLancerTest.IsEnabled = true;
                    BtnAnnulerTest.IsEnabled = false;
                    ProgressTest.Value = 100;

                    if (testResult.Success)
                    {
                        TxtTestStatus.Text = $"✅ RÉUSSI — {testResult.TotalFound}/{testResult.TotalExpected} entités synchronisées";
                        TxtTestStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2E7D32"));
                    }
                    else
                    {
                        TxtTestStatus.Text = $"❌ ÉCHOUÉ — {testResult.Failures.Count} entité(s) manquante(s)";
                        TxtTestStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C62828"));
                    }
                }));
            };

            await Task.Run(() => _syncTestService.RunAsync());
        }

        private void BtnAnnulerTest_Click(object sender, RoutedEventArgs e)
        {
            _syncTestService?.Cancel();
            BtnAnnulerTest.IsEnabled = false;
            TxtTestStatus.Text = "Annulation en cours...";
        }

        private static string ReadConfigKey(string key)
        {
            try
            {
                string configPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    @"sgi_support\APPLICATIONS\config.ini");
                if (!System.IO.File.Exists(configPath))
                {
                    configPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.ini");
                }
                if (!System.IO.File.Exists(configPath)) return null;

                foreach (var line in System.IO.File.ReadAllLines(configPath))
                {
                    var trimmed = line.Trim();
                    if (trimmed.StartsWith(key + "=", StringComparison.OrdinalIgnoreCase))
                        return trimmed.Substring(key.Length + 1).Trim();
                }
            }
            catch { }
            return null;
        }
    }
}

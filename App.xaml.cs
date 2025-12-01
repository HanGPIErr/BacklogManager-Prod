using System;
using System.IO;
using System.Windows;
using BacklogManager.Services;
using BacklogManager.ViewModels;
using BacklogManager.Views;

namespace BacklogManager
{
    public partial class App : Application
    {
        public NotificationService NotificationService { get; set; }
        public EmailService EmailService { get; set; }
        
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Nettoyer les anciens logs au démarrage
            LoggingService.Instance.CleanOldLogs();
            LoggingService.Instance.LogInfo("=== Démarrage de l'application ===");

            // Gestionnaire d'exceptions globales - Exceptions non-UI
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                Exception ex = args.ExceptionObject as Exception;
                LoggingService.Instance.LogError("Exception non gérée (non-UI)", ex);
                
                MessageBox.Show($"Erreur critique: {ex?.Message}\n\n" +
                    $"L'erreur a été enregistrée dans les logs.\n" +
                    $"Veuillez contacter le support si le problème persiste.", 
                    "Erreur Critique", MessageBoxButton.OK, MessageBoxImage.Error);
            };

            // Gestionnaire d'exceptions globales - Exceptions UI
            DispatcherUnhandledException += (sender, args) =>
            {
                LoggingService.Instance.LogError("Exception non gérée (UI)", args.Exception);
                
                MessageBox.Show($"Erreur UI: {args.Exception.Message}\n\n" +
                    $"L'erreur a été enregistrée dans les logs.\n" +
                    $"Veuillez contacter le support si le problème persiste.", 
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                args.Handled = true;
            };

            try
            {
                // Créer raccourci bureau au premier lancement
                CreerRaccourciDesktop();

                // Show login window instead of MainWindow
                var loginWindow = new LoginWindow();
                loginWindow.Show();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Erreur au démarrage de l'application", ex);
                MessageBox.Show($"Erreur au démarrage: {ex.Message}\n\n" +
                    $"L'erreur a été enregistrée dans les logs.", 
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Crée un raccourci sur le bureau au premier lancement de l'application
        /// </summary>
        private void CreerRaccourciDesktop()
        {
            try
            {
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string shortcutPath = Path.Combine(desktopPath, "BacklogManager.lnk");

                // Ne créer que si le raccourci n'existe pas déjà
                if (!File.Exists(shortcutPath))
                {
                    string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                    string workingDirectory = Path.GetDirectoryName(exePath);

                    // Utiliser l'API Windows Shell pour créer le raccourci
                    Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                    dynamic shell = Activator.CreateInstance(shellType);
                    dynamic shortcut = shell.CreateShortcut(shortcutPath);

                    shortcut.TargetPath = exePath;
                    shortcut.WorkingDirectory = workingDirectory;
                    shortcut.Description = "BacklogManager - Gestion de projets Agile";
                    shortcut.IconLocation = exePath + ",0";
                    shortcut.Save();

                    System.Runtime.InteropServices.Marshal.ReleaseComObject(shortcut);
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(shell);
                }
            }
            catch
            {
                // Ignorer les erreurs silencieusement (permissions, raccourci déjà existant, etc.)
            }
        }
    }
}

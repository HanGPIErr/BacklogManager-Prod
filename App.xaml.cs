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
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Gestionnaire d'exceptions globales
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                Exception ex = args.ExceptionObject as Exception;
                MessageBox.Show($"Erreur critique: {ex?.Message}\n\nStack: {ex?.StackTrace}", 
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            };

            DispatcherUnhandledException += (sender, args) =>
            {
                MessageBox.Show($"Erreur UI: {args.Exception.Message}\n\nStack: {args.Exception.StackTrace}", 
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
                MessageBox.Show($"Erreur au démarrage: {ex.Message}\n\nStack: {ex.StackTrace}", 
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

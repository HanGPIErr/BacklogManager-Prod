using System;
using System.IO;
using System.Text;

namespace BacklogManager.Services
{
    /// <summary>
    /// Service de logging pour enregistrer les erreurs et événements importants
    /// </summary>
    public class LoggingService
    {
        private static LoggingService _instance;
        private static readonly object _lock = new object();
        private readonly string _logFilePath;

        private LoggingService()
        {
            // Créer le dossier de logs s'il n'existe pas
            string logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            // Fichier de log avec la date du jour
            string logFileName = $"BacklogManager_{DateTime.Now:yyyyMMdd}.log";
            _logFilePath = Path.Combine(logDirectory, logFileName);
        }

        public static LoggingService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new LoggingService();
                        }
                    }
                }
                return _instance;
            }
        }

        public void LogError(string message, Exception exception = null)
        {
            Log("ERROR", message, exception);
        }

        public void LogWarning(string message)
        {
            Log("WARNING", message, null);
        }

        public void LogInfo(string message)
        {
            Log("INFO", message, null);
        }

        private void Log(string level, string message, Exception exception)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}");
                
                if (exception != null)
                {
                    sb.AppendLine($"Exception: {exception.GetType().Name}");
                    sb.AppendLine($"Message: {exception.Message}");
                    sb.AppendLine($"StackTrace: {exception.StackTrace}");
                    
                    if (exception.InnerException != null)
                    {
                        sb.AppendLine($"InnerException: {exception.InnerException.Message}");
                        sb.AppendLine($"InnerStackTrace: {exception.InnerException.StackTrace}");
                    }
                }
                
                sb.AppendLine(new string('-', 80));

                lock (_lock)
                {
                    File.AppendAllText(_logFilePath, sb.ToString());
                }
            }
            catch
            {
                // Si le logging échoue, ne pas crasher l'application
            }
        }

        /// <summary>
        /// Nettoie les logs de plus de 30 jours
        /// </summary>
        public void CleanOldLogs()
        {
            try
            {
                string logDirectory = Path.GetDirectoryName(_logFilePath);
                var logFiles = Directory.GetFiles(logDirectory, "BacklogManager_*.log");
                
                foreach (var logFile in logFiles)
                {
                    var fileInfo = new FileInfo(logFile);
                    if (fileInfo.CreationTime < DateTime.Now.AddDays(-30))
                    {
                        File.Delete(logFile);
                    }
                }
            }
            catch
            {
                // Ignorer les erreurs de nettoyage
            }
        }
    }
}

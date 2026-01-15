using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Resources;
using System.Windows;

namespace BacklogManager.Services
{
    /// <summary>
    /// Service de localisation pour gérer les traductions de l'application
    /// </summary>
    public class LocalizationService : INotifyPropertyChanged
    {
        private static LocalizationService _instance;
        private static readonly object _lock = new object();
        private ResourceManager _resourceManager;
        private CultureInfo _currentCulture;
        private const string ConfigFileName = "config.ini";
        
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Instance singleton du service de localisation
        /// </summary>
        public static LocalizationService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new LocalizationService();
                        }
                    }
                }
                return _instance;
            }
        }

        private LocalizationService()
        {
            // Initialiser le ResourceManager avec le fichier de ressources
            _resourceManager = new ResourceManager(
                "BacklogManager.Resources.Strings",
                typeof(LocalizationService).Assembly);

            // Charger la langue depuis le fichier de configuration
            LoadLanguageFromConfig();
        }

        /// <summary>
        /// Culture actuelle de l'application
        /// </summary>
        public CultureInfo CurrentCulture
        {
            get => _currentCulture;
            private set
            {
                if (_currentCulture != value)
                {
                    _currentCulture = value;
                    OnPropertyChanged(nameof(CurrentCulture));
                    // Notifier tous les bindings de se rafraîchir
                    OnPropertyChanged("Item[]");
                }
            }
        }

        /// <summary>
        /// Code de langue actuel (fr, en, es)
        /// </summary>
        public string CurrentLanguageCode => CurrentCulture?.TwoLetterISOLanguageName ?? "fr";

        /// <summary>
        /// Indexeur pour accéder aux chaînes localisées depuis XAML
        /// </summary>
        public string this[string key]
        {
            get
            {
                if (string.IsNullOrEmpty(key))
                    return string.Empty;

                try
                {
                    string value = _resourceManager.GetString(key, CurrentCulture);
                    return value ?? $"[{key}]";
                }
                catch
                {
                    return $"[{key}]";
                }
            }
        }

        /// <summary>
        /// Obtient une chaîne localisée
        /// </summary>
        public string GetString(string key)
        {
            return this[key];
        }

        /// <summary>
        /// Change la langue de l'application
        /// </summary>
        /// <param name="languageCode">Code de langue (fr, en, es)</param>
        public void ChangeLanguage(string languageCode)
        {
            try
            {
                CultureInfo newCulture = null;

                switch (languageCode.ToLower())
                {
                    case "fr":
                        newCulture = new CultureInfo("fr-FR");
                        break;
                    case "en":
                        newCulture = new CultureInfo("en-US");
                        break;
                    case "es":
                        newCulture = new CultureInfo("es-ES");
                        break;
                    default:
                        newCulture = new CultureInfo("en-US"); // Par défaut anglais
                        break;
                }

                CurrentCulture = newCulture;
                
                // Sauvegarder dans le fichier de configuration
                SaveLanguageToConfig(languageCode);

                LoggingService.Instance?.LogInfo($"Langue changée vers: {languageCode}");
            }
            catch (Exception ex)
            {
                LoggingService.Instance?.LogError("Erreur lors du changement de langue", ex);
                // En cas d'erreur, utiliser l'anglais par défaut
                CurrentCulture = new CultureInfo("en-US");
            }
        }

        /// <summary>
        /// Détecte et applique la langue du système Windows
        /// </summary>
        public void DetectAndApplySystemLanguage()
        {
            try
            {
                // Vérifier d'abord si une langue est déjà configurée
                string configuredLanguage = LoadLanguageFromConfigFile();
                if (!string.IsNullOrEmpty(configuredLanguage))
                {
                    // Utiliser la langue configurée
                    ChangeLanguage(configuredLanguage);
                    return;
                }

                // Sinon, détecter la langue du système
                CultureInfo systemCulture = CultureInfo.CurrentUICulture;
                string systemLanguage = systemCulture.TwoLetterISOLanguageName.ToLower();

                LoggingService.Instance?.LogInfo($"Langue système détectée: {systemLanguage}");

                // Appliquer la langue selon le système
                switch (systemLanguage)
                {
                    case "fr":
                        ChangeLanguage("fr");
                        break;
                    case "es":
                        ChangeLanguage("es");
                        break;
                    default:
                        // Pour toutes les autres langues, utiliser l'anglais
                        ChangeLanguage("en");
                        break;
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance?.LogError("Erreur lors de la détection de la langue système", ex);
                // En cas d'erreur, utiliser l'anglais
                ChangeLanguage("en");
            }
        }

        /// <summary>
        /// Charge la langue depuis le fichier de configuration
        /// </summary>
        private void LoadLanguageFromConfig()
        {
            try
            {
                string language = LoadLanguageFromConfigFile();
                
                if (!string.IsNullOrEmpty(language))
                {
                    ChangeLanguage(language);
                }
                else
                {
                    // Si aucune langue configurée, détecter celle du système
                    DetectAndApplySystemLanguage();
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance?.LogError("Erreur lors du chargement de la langue depuis la config", ex);
                // En cas d'erreur, utiliser l'anglais
                CurrentCulture = new CultureInfo("en-US");
            }
        }

        /// <summary>
        /// Lit la langue depuis le fichier config.ini
        /// </summary>
        private string LoadLanguageFromConfigFile()
        {
            try
            {
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigFileName);
                
                if (File.Exists(configPath))
                {
                    var lines = File.ReadAllLines(configPath);
                    foreach (var line in lines)
                    {
                        if (line.StartsWith("Language=", StringComparison.OrdinalIgnoreCase))
                        {
                            return line.Substring("Language=".Length).Trim();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance?.LogError("Erreur lors de la lecture du fichier de configuration", ex);
            }

            return null;
        }

        /// <summary>
        /// Sauvegarde la langue dans le fichier de configuration
        /// </summary>
        private void SaveLanguageToConfig(string languageCode)
        {
            try
            {
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigFileName);
                
                string[] lines;
                bool languageLineFound = false;

                if (File.Exists(configPath))
                {
                    lines = File.ReadAllLines(configPath);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        if (lines[i].StartsWith("Language=", StringComparison.OrdinalIgnoreCase))
                        {
                            lines[i] = $"Language={languageCode}";
                            languageLineFound = true;
                            break;
                        }
                    }

                    if (!languageLineFound)
                    {
                        // Ajouter la ligne Language
                        Array.Resize(ref lines, lines.Length + 1);
                        lines[lines.Length - 1] = $"Language={languageCode}";
                    }
                }
                else
                {
                    // Créer le fichier avec la ligne Language
                    lines = new[] { $"Language={languageCode}" };
                }

                File.WriteAllLines(configPath, lines);
                LoggingService.Instance?.LogInfo($"Langue sauvegardée dans config.ini: {languageCode}");
            }
            catch (Exception ex)
            {
                LoggingService.Instance?.LogError("Erreur lors de la sauvegarde de la langue dans config.ini", ex);
            }
        }

        /// <summary>
        /// Déclenche l'événement PropertyChanged
        /// </summary>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Obtient les langues disponibles
        /// </summary>
        public static string[] GetAvailableLanguages()
        {
            return new[] { "fr", "en", "es" };
        }

        /// <summary>
        /// Obtient le nom d'affichage d'une langue
        /// </summary>
        public string GetLanguageDisplayName(string languageCode)
        {
            switch (languageCode.ToLower())
            {
                case "fr":
                    return GetString("Language_French");
                case "en":
                    return GetString("Language_English");
                case "es":
                    return GetString("Language_Spanish");
                default:
                    return languageCode;
            }
        }
    }
}

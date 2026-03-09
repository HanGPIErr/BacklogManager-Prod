using System;

namespace BacklogManager.Services
{
    /// <summary>
    /// Service de configuration centralisée pour l'IA
    /// Stocke le token d'API utilisé par toute l'application dans la base de données
    /// </summary>
    public static class AIConfigService
    {
        // URL de l'API IA
        public const string API_URL = "https://genfactory-ai.analytics.cib.echonet/genai/api/v2/chat/completions";
        
        // Modèle IA à utiliser
        public const string MODEL = "gpt-oss-120b";
        
        // Clé de configuration pour le token
        private const string TOKEN_CONFIG_KEY = "AI_API_TOKEN";
        
        // Instance du database
        private static IDatabase _database;
        
        /// <summary>
        /// Initialise le service avec la base de données
        /// </summary>
        public static void Initialize(IDatabase database)
        {
            _database = database;
        }
        
        /// <summary>
        /// Obtient le token API utilisé pour les appels à l'IA
        /// </summary>
        public static string GetToken()
        {
            if (_database == null)
            {
                // Fallback sur les settings utilisateur si la DB n'est pas initialisée
                return Properties.Settings.Default.AgentChatToken?.Trim() ?? string.Empty;
            }
            
            // Lire le token depuis la base de données
            return _database.GetConfiguration(TOKEN_CONFIG_KEY)?.Trim() ?? string.Empty;
        }
        
        /// <summary>
        /// Définit le token API pour les appels à l'IA
        /// </summary>
        public static void SetToken(string token)
        {
            if (_database == null)
            {
                // Fallback sur les settings utilisateur si la DB n'est pas initialisée
                Properties.Settings.Default.AgentChatToken = token?.Trim() ?? string.Empty;
                Properties.Settings.Default.Save();
                return;
            }
            
            // Enregistrer le token dans la base de données
            _database.SetConfiguration(TOKEN_CONFIG_KEY, token?.Trim() ?? string.Empty);
        }
        
        /// <summary>
        /// Vérifie si le token est configuré
        /// </summary>
        public static bool IsTokenConfigured()
        {
            var token = GetToken();
            return !string.IsNullOrWhiteSpace(token);
        }
    }
}

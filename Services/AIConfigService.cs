using System;

namespace BacklogManager.Services
{
    /// <summary>
    /// Service de configuration centralisée pour l'IA
    /// Stocke le token d'API utilisé par toute l'application
    /// </summary>
    public static class AIConfigService
    {
        // URL de l'API IA
        public const string API_URL = "https://genfactory-ai.analytics.cib.echonet/genai/api/v2/chat/completions";
        
        // Modèle IA à utiliser
        public const string MODEL = "gpt-oss-120b";
        
        // Token centralisé pour toute l'application
        // Ce token est utilisé par tous les utilisateurs de l'application
        private const string CENTRALIZED_TOKEN = "eyJraWQiOiIxZTU5ZjQ5OS0yZWQ2LTRiOTgtYTk1Yi0xYjljNGQyMjRjNzYiLCJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiJ9.eyJ1bnRpdGxlbWVudHMiOlsiR0VORkFDVE9SWS1TQU5EQk9YLVBSRCIsIkdFTkZBQ1RPUlktU0FORE9SWS1TQU5EQk9YLVBSRC1HQi1VU0VSIl0sImNvbN1bWVvX2F1dGhlbnRpY2F0aW9uIjoiYWJpbm1iYXJjc2lsInN1YiI6ImIwNDQwNiIsInVzZXJfZW1haWwiOiJnaWFubmkuYXVjbnVAZXh0ZXJuZS5ibnBwYXJpYmFzLmNvbSIsIm1zYWlsbGluZyI6Imh0dHBzOi8vbXJzLmNvbnNhY2NlLmNvbSIsImNsaWVuY2VfaWQiOiIxYjliNmRFZzE2QXJlZm9yMiIsInRlbmFudCI6IkJhbGxvY2tlbSIsInZzIjoiMSIsImZ1bGxuYW1lIjoiGianni MurruIn0.QwNiIsInVzZXJfZmxhaWwiOiJnaWFubmkuYXVjbnVAZXh0ZXJuZS5ibnBwYXJpYmFzLmNvbSIsIm1zYWlsbGluZyI6Imh0dHBzOi8vbXJzLmNvbnNhY2NlLmNvbSIsImNsaWVuY2VfaWQiOiIxYjliNmRFZzE2QXJlZm9yMiIsInRlbmFudCI6IkJhbGxvY2tlbSIsInZzIjoiMSIsImZ1bGxuYW1lIjoiGianni MurruIn0.NDdmJ2SmhyQSl5mF1ZCI6Imh0dHBzOi8vbXJzLmNvbnNhY2NlLmNvbSIsImNsaWVuY2VfaWQiOiIxYjliNmRFZzE2QXJlZm9yMiIsInRlbmFudCI6IkJhbGxvY2tlbSIsInZzIjoiMSIsImZ1bGxuYW1lIjoiGianni MurruIn0.RmN0LWFjZC0gZ2VuLWFpLWNvZGUuZXhhbXBsZS5jb25zdW1lch9hHBfZGlzcGxheV9uYW1lIjoiR0VORkFDVE9SWS1TQU5EQk9YLVBSRC1DT0RJTkdBU1NJU1RBTlQtUFJELCJwcm9kdWN0X2pc3QiOiJsiW29hdXR0bCIsImV4cCI6MTc4OTczODMwNiwiYWF0IjoxNzU4MjYyMjM1LCJqdGkiOiJXNTExZTIwZmYxNDhjLTA5Yy1iMTQxMzLTcwQi1JbnN0cnVjdG9yIiwiZXh0X2NvbXBsZXRpb25fdHlwZSI6IkNvZGVBcmVudCIsInBhcmFtcyI6eyJleHAiOjE3ODk3MzgzMDYsImFhdCI6MTc1ODI2MjIzNSwiY2xpZW50X2lkIjoiMWI5Ym5kRWc2QXJlZm9yMiIsInRlbmFudCI6IkJhbGxvY2tlbSIsInVzZXIiOiJnaWFubmkuYXVjbnVAZXh0ZXJuZS5ibnBwYXJpYmFzLmNvbSJ9fQ.qsZbNfb9YsWeJViC9Vh8vLGOKkATqxc4XZDFQpDK2_Ewd6zS91RE7LnHNTmsmZNnZWJVMggXTVMryIGxsW4dqVnDUdy9OnG310lEz3YcdOdoEMr89pDt5sE_no16a0A";
        
        /// <summary>
        /// Obtient le token API utilisé pour les appels à l'IA
        /// </summary>
        public static string GetToken()
        {
            return CENTRALIZED_TOKEN;
        }
        
        /// <summary>
        /// Vérifie si le token est configuré
        /// </summary>
        public static bool IsTokenConfigured()
        {
            return !string.IsNullOrWhiteSpace(CENTRALIZED_TOKEN);
        }
    }
}

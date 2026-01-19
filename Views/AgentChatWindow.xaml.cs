using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Text.Json;
using BacklogManager.Services;
using BacklogManager.Domain;
using System.IO;

namespace BacklogManager.Views
{
    public partial class AgentChatWindow : Window, INotifyPropertyChanged
    {
        private const string API_URL = "https://genfactory-ai.analytics.cib.echonet/genai/api/v2/chat/completions";
        private const string MODEL = "gpt-oss-120b";
        private const string TOKEN_KEY = "AgentChatToken";
        private const string LOG_FILE = "chat_debug.log";
        
        private string _apiToken;
        private bool _needTokenConfiguration;
        private bool _chatVisible;
        private string _messageActuel;
        private bool _canSendMessage;
        private readonly ChatHistoryService _chatHistoryService;
        private readonly Utilisateur _currentUser;
        private int? _conversationId;

        public ObservableCollection<ChatMessage> Messages { get; set; }

        public bool NeedTokenConfiguration
        {
            get => _needTokenConfiguration;
            set { _needTokenConfiguration = value; OnPropertyChanged(); }
        }

        public bool ChatVisible
        {
            get => _chatVisible;
            set { _chatVisible = value; OnPropertyChanged(); }
        }

        public string MessageActuel
        {
            get => _messageActuel;
            set 
            { 
                _messageActuel = value; 
                OnPropertyChanged();
                CanSendMessage = !string.IsNullOrWhiteSpace(value);
            }
        }

        public bool CanSendMessage
        {
            get => _canSendMessage;
            set { _canSendMessage = value; OnPropertyChanged(); }
        }

        public AgentChatWindow(ChatHistoryService chatHistoryService, Utilisateur currentUser)
        {
            InitializeComponent();
            DataContext = this;
            Messages = new ObservableCollection<ChatMessage>();
            
            // Initialiser les textes localisés
            TxtAgentTitle.Text = LocalizationService.Instance.GetString("AIChat_AgentTitle");
            TxtAgentSubtitle.Text = LocalizationService.Instance.GetString("AIChat_AgentSubtitle");
            BtnRetour.Content = LocalizationService.Instance.GetString("AIChat_BtnReturn");
            TxtSendHint.Text = LocalizationService.Instance.GetString("AIChat_SendHint");
            BtnClearHistory.Content = LocalizationService.Instance.GetString("AIChat_BtnClearHistory");
            
            _chatHistoryService = chatHistoryService;
            _currentUser = currentUser;
            
            LoadToken();
        }

        private void BtnRetour_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void AddReaction(ChatMessage message, string emoji)
        {
            if (message.Reaction == emoji)
            {
                // Si la même réaction est cliquée, on la retire
                message.Reaction = null;
            }
            else
            {
                message.Reaction = emoji;
            }
        }

        private void ReactionButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.DataContext is ChatMessage message)
            {
                // Récupérer l'emoji du TextBlock dans le Content
                if (button.Content is TextBlock textBlock && !string.IsNullOrEmpty(textBlock.Text))
                {
                    AddReaction(message, textBlock.Text);
                }
            }
        }

        private void LoadToken()
        {
            try
            {
                // Charger le token depuis les paramètres locaux
                _apiToken = Properties.Settings.Default[TOKEN_KEY]?.ToString()?.Trim();
                
                if (string.IsNullOrWhiteSpace(_apiToken))
                {
                    NeedTokenConfiguration = true;
                    ChatVisible = false;
                }
                else
                {
                    NeedTokenConfiguration = false;
                    ChatVisible = true;
                    
                    // Message de bienvenue
                    Messages.Add(new ChatMessage
                    {
                        IsUser = false,
                        Auteur = "🤖 " + LocalizationService.Instance.GetString("AIChat_AgentTitle"),
                        Message = LocalizationService.Instance.GetString("AIChat_WelcomeMessageFull"),
                        Horodatage = DateTime.Now.ToString("HH:mm")
                    });
                }
            }
            catch
            {
                NeedTokenConfiguration = true;
                ChatVisible = false;
            }
        }

        private void SaveToken_Click(object sender, RoutedEventArgs e)
        {
            var token = TxtToken.Text.Trim();
            
            if (string.IsNullOrWhiteSpace(token))
            {
                MessageBox.Show("Veuillez saisir un token valide.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Sauvegarder dans les paramètres locaux
                Properties.Settings.Default[TOKEN_KEY] = token;
                Properties.Settings.Default.Save();
                
                _apiToken = token;
                NeedTokenConfiguration = false;
                ChatVisible = true;
                
                // Message de bienvenue
                Messages.Add(new ChatMessage
                {
                    IsUser = false,
                    Auteur = LocalizationService.Instance.GetString("AIChat_AgentTitle"),
                    Message = LocalizationService.Instance.GetString("AIChat_WelcomeMessageFull"),
                    Horodatage = DateTime.Now.ToString("HH:mm")
                });
                
                MessageBox.Show("Token enregistré avec succès !", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'enregistrement du token : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void TxtMessage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.Shift)
            {
                e.Handled = true;
                if (!string.IsNullOrWhiteSpace(MessageActuel))
                {
                    _ = SendMessage();
                }
            }
        }

        private async void SendMessage_Click(object sender, RoutedEventArgs e)
        {
            await SendMessage();
        }

        private async Task SendMessage()
        {
            if (string.IsNullOrWhiteSpace(MessageActuel)) return;

            var userMessage = MessageActuel;
            MessageActuel = string.Empty;
            CanSendMessage = false;

            // Créer une nouvelle conversation si nécessaire
            if (!_conversationId.HasValue)
            {
                _conversationId = _chatHistoryService.StartNewConversation(_currentUser.Id, $"{_currentUser.Prenom} {_currentUser.Nom}");
            }

            // Ajouter le message utilisateur
            var userChatMsg = new ChatMessage
            {
                IsUser = true,
                Auteur = "Vous",
                Message = userMessage,
                Horodatage = DateTime.Now.ToString("HH:mm")
            };
            Messages.Add(userChatMsg);

            // Sauvegarder le message utilisateur dans la BDD
            _chatHistoryService.SaveMessage(_conversationId.Value, _currentUser.Id, $"{_currentUser.Prenom} {_currentUser.Nom}", true, userMessage);

            ScrollToBottom();

            // Ajouter un message "en train de réfléchir"
            var thinkingMessage = new ChatMessage
            {
                IsUser = false,
                Auteur = "Agent Project & Change",
                Message = "Je réfléchis... 💭",
                Horodatage = DateTime.Now.ToString("HH:mm")
            };
            Messages.Add(thinkingMessage);
            ScrollToBottom();

            try
            {
                // Appeler l'API
                var response = await CallChatAPI(userMessage);
                
                // Retirer le message "en train de réfléchir"
                Messages.Remove(thinkingMessage);
                
                // Ajouter la réponse de l'agent
                var agentChatMsg = new ChatMessage
                {
                    IsUser = false,
                    Auteur = "Agent Project & Change",
                    Message = response,
                    Horodatage = DateTime.Now.ToString("HH:mm")
                };
                Messages.Add(agentChatMsg);

                // Sauvegarder la réponse de l'agent dans la BDD
                _chatHistoryService.SaveMessage(_conversationId.Value, _currentUser.Id, "Agent Project & Change", false, response);
            }
            catch (Exception ex)
            {
                Messages.Remove(thinkingMessage);
                var errorMsg = new ChatMessage
                {
                    IsUser = false,
                    Auteur = "Agent Project & Change",
                    Message = $"❌ Désolée, j'ai rencontré une erreur : {ex.Message}\n\nAssurez-vous que votre token est valide et que vous avez accès à l'API.",
                    Horodatage = DateTime.Now.ToString("HH:mm")
                };
                Messages.Add(errorMsg);
            }

            ScrollToBottom();
            CanSendMessage = true;
        }

        private async Task<string> CallChatAPI(string userMessage)
        {
            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(60);
                client.DefaultRequestHeaders.Clear();
                
                // Logging pour debug
                LogDebug($"Token length: {_apiToken?.Length ?? 0}");
                LogDebug($"Token first 20 chars: {(_apiToken?.Length >= 20 ? _apiToken.Substring(0, 20) : _apiToken)}");
                
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiToken}");
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                
                // Construire l'historique de conversation pour le contexte
                var conversationHistory = Messages
                    .Where(m => m.Message != "Je réfléchis... 💭")
                    .Select(m => new
                    {
                        role = m.IsUser ? "user" : "assistant",
                        content = m.Message
                    })
                    .ToList();

                // Détecter la langue de l'utilisateur
                string currentLanguage = LocalizationService.Instance.CurrentLanguageCode;
                string systemPrompt = GetSystemPrompt(currentLanguage, userMessage);

                // Ajouter le message système avec la personnalité de l'agent
                var messages = new[]
                {
                    new
                    {
                        role = "system",
                        content = systemPrompt
                    }
                }.Concat(conversationHistory).ToList();

                var requestBody = new
                {
                    model = MODEL,
                    messages = messages,
                    temperature = 0.7,
                    max_tokens = 400
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Logging pour debug
                LogDebug($"API URL: {API_URL}");
                LogDebug($"Request body: {jsonContent.Substring(0, Math.Min(500, jsonContent.Length))}...");
                LogDebug($"Authorization header: Bearer {_apiToken?.Substring(0, Math.Min(20, _apiToken?.Length ?? 0))}...");

                var response = await client.PostAsync(API_URL, content);
                
                // Gérer les erreurs HTTP avec plus de détails
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    var statusCode = (int)response.StatusCode;
                    
                    // Log détaillé pour debug
                    LogDebug($"ERROR - Status Code: {statusCode}");
                    LogDebug($"ERROR - Response: {errorContent}");
                    LogDebug($"ERROR - Headers sent:");
                    foreach (var header in client.DefaultRequestHeaders)
                    {
                        LogDebug($"  {header.Key}: {string.Join(", ", header.Value)}");
                    }
                    
                    throw new Exception($"Le code d'état de réponse n'indique pas la réussite : {statusCode} ({response.StatusCode}).\n\nDétails : {errorContent}\n\nVérifiez que votre token est valide et que vous avez les droits d'accès à l'API.");
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                var jsonResponse = JsonDocument.Parse(responseBody);
                
                var messageContent = jsonResponse.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                return messageContent;
            }
        }

        private void ScrollToBottom()
        {
            ScrollMessages.ScrollToBottom();
        }

        private void ClearHistory_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Êtes-vous sûr de vouloir effacer tout l'historique de conversation ?",
                "Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                Messages.Clear();
                Messages.Add(new ChatMessage
                {
                    IsUser = false,
                    Auteur = "Agent Project & Change",
                    Message = "Historique effacé ! On repart sur de bonnes bases. Que puis-je faire pour vous ? 😊",
                    Horodatage = DateTime.Now.ToString("HH:mm")
                });
            }
        }

        private void ChangeToken_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Voulez-vous vraiment changer votre token d'accès ?\nL'historique de conversation sera conservé.",
                "Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                ChatVisible = false;
                NeedTokenConfiguration = true;
                TxtToken.Clear();
            }
        }

        private string GetSystemPrompt(string languageCode, string userMessage)
        {
            // Détecter la langue du message utilisateur (si différente de la langue système)
            string detectedLanguage = DetectMessageLanguage(userMessage);
            string targetLanguage = !string.IsNullOrEmpty(detectedLanguage) ? detectedLanguage : languageCode;

            switch (targetLanguage.ToLower())
            {
                case "es":
                    return @"Eres Agente Project & Change, asistente virtual experta en gestión de proyectos ágiles para BacklogManager.

**Personalidad**: Amable, pedagógica, con humor y emojis. Responde de manera CONCISA (3-4 líneas máx).

**BacklogManager - Funciones principales**:
• 📋 Backlog: Crear/modificar tareas (➕ Nueva tarea)
• 🎯 Kanban: Arrastrar y soltar tarjetas entre columnas (POR HACER → EN CURSO → TERMINADO)
• ⏱️ CRA: Registrar horas trabajadas (calendario mensual, validado por admin)
• 📝 Demandas: Business Analyst crea, Jefe de Proyecto valida
• 📊 Estadísticas & KPI: Velocidad, productividad

**Roles**:
• Admin: Todos los derechos + validación CRA
• Jefe Proyecto: Gestión completa tareas/proyectos
• BA: Crear demandas
• Dev: Sus tareas + CRA

**Instrucciones**:
✓ Respuestas cortas y directas con formato Markdown rico
✓ Usa **negrita**, *cursiva*, listas con viñetas, `código`
✓ Menciona los emojis de iconos
✓ Indica el rol requerido si es relevante
✓ Tono cálido y alentador
✓ Usa bloques de código ```cuando sea apropiado
✓ Estructura tus respuestas con títulos ### cuando sea necesario";

                case "en":
                    return @"You are Agent Project & Change, a virtual assistant expert in agile project management for BacklogManager.

**Personality**: Kind, educational, with humor and emojis. Answer CONCISELY (3-4 lines max).

**BacklogManager - Main features**:
• 📋 Backlog: Create/edit tasks (➕ New task)
• 🎯 Kanban: Drag and drop cards between columns (TO DO → IN PROGRESS → DONE)
• ⏱️ CRA: Log worked hours (monthly calendar, validated by admin)
• 📝 Requests: Business Analyst creates, Project Manager validates
• 📊 Stats & KPI: Velocity, productivity

**Roles**:
• Admin: All rights + CRA validation
• Project Manager: Full task/project management
• BA: Create requests
• Dev: Their tasks + CRA

**Instructions**:
✓ Short and direct answers with rich Markdown formatting
✓ Use **bold**, *italic*, bullet lists, `code`
✓ Mention icon emojis
✓ Indicate required role if relevant
✓ Warm and encouraging tone
✓ Use code blocks ```when appropriate
✓ Structure your answers with ### headings when needed";

                default: // French
                    return @"Tu es Agent Project & Change, assistante virtuelle experte en gestion de projet agile pour BacklogManager.

**Personnalité**: Bienveillante, pédagogue, avec humour et émojis. Réponds de façon CONCISE (3-4 lignes max).

**BacklogManager - Fonctions principales**:
• 📋 Backlog: Créer/modifier tâches (➕ Nouvelle tâche)
• 🎯 Kanban: Glisser-déposer cartes entre colonnes (À FAIRE → EN COURS → TERMINÉ)
• ⏱️ CRA: Saisir heures travaillées (calendrier mensuel, validé par admin)
• 📝 Demandes: Business Analyst crée, Chef de Projet valide
• 📊 Stats & KPI: Vélocité, productivité

**Rôles**:
• Admin: Tous droits + validation CRA
• Chef Projet: Gestion complète tâches/projets
• BA: Créer demandes
• Dev: Ses tâches + CRA

**Consignes**:
✓ Réponses courtes et directes avec formatage Markdown riche
✓ Utilise **gras**, *italique*, listes à puces, `code`
✓ Mentionne les émojis d'icônes
✓ Indique le rôle requis si pertinent
✓ Ton chaleureux et encourageant
✓ Utilise des blocs de code ```quand approprié
✓ Structure tes réponses avec des titres ### si nécessaire";
            }
        }

        private string DetectMessageLanguage(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return null;

            // Mots clés espagnols
            string[] spanishKeywords = { "hola", "como", "que", "hacer", "puedo", "ayuda", "gracias", "por favor", "buenos", "dias" };
            // Mots clés anglais
            string[] englishKeywords = { "hello", "how", "what", "can", "help", "please", "thanks", "good", "morning", "afternoon" };
            // Mots clés français
            string[] frenchKeywords = { "bonjour", "comment", "quoi", "faire", "peux", "aide", "merci", "s'il", "bonne", "journée" };

            string lowerMessage = message.ToLower();
            
            int spanishCount = 0, englishCount = 0, frenchCount = 0;

            foreach (var word in spanishKeywords)
                if (lowerMessage.Contains(word)) spanishCount++;

            foreach (var word in englishKeywords)
                if (lowerMessage.Contains(word)) englishCount++;

            foreach (var word in frenchKeywords)
                if (lowerMessage.Contains(word)) frenchCount++;

            if (spanishCount > englishCount && spanishCount > frenchCount)
                return "es";
            if (englishCount > spanishCount && englishCount > frenchCount)
                return "en";
            if (frenchCount > spanishCount && frenchCount > englishCount)
                return "fr";

            return null; // Pas de langue détectée clairement
        }

        private void LogDebug(string message)
        {
            try
            {
                var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LOG_FILE);
                var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}{Environment.NewLine}";
                File.AppendAllText(logPath, logMessage);
            }
            catch
            {
                // Ignorer les erreurs de log
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ChatMessage : INotifyPropertyChanged
    {
        public bool IsUser { get; set; }
        public string Auteur { get; set; }
        public string Message { get; set; }
        public string Horodatage { get; set; }
        
        public HorizontalAlignment Alignment => IsUser ? HorizontalAlignment.Right : HorizontalAlignment.Left;

        private string _reaction;
        public string Reaction
        {
            get => _reaction;
            set
            {
                _reaction = value;
                OnPropertyChanged(nameof(Reaction));
                OnPropertyChanged(nameof(HasReaction));
            }
        }

        public bool HasReaction => !string.IsNullOrEmpty(Reaction);

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

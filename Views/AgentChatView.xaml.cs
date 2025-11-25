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
using System.Text.Json;
using BacklogManager.Services;
using BacklogManager.Domain;
using System.IO;

namespace BacklogManager.Views
{
    public partial class AgentChatView : UserControl, INotifyPropertyChanged
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
        private readonly MainWindow _mainWindow;
        private int? _conversationId;

        public event PropertyChangedEventHandler PropertyChanged;

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

        public AgentChatView(ChatHistoryService chatHistoryService, Utilisateur currentUser, MainWindow mainWindow)
        {
            InitializeComponent();
            DataContext = this;
            Messages = new ObservableCollection<ChatMessage>();
            
            _chatHistoryService = chatHistoryService;
            _currentUser = currentUser;
            _mainWindow = mainWindow;
            
            LoadToken();
        }

        private void BtnRetour_Click(object sender, RoutedEventArgs e)
        {
            // Retour au Dashboard
            var btnDashboard = _mainWindow.FindName("BtnDashboard") as Button;
            btnDashboard?.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
        }

        private void LoadToken()
        {
            try
            {
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
                        Auteur = "Agent Project & Change",
                        Message = "Bonjour ! Je suis votre assistante virtuelle pour gérer votre backlog. Comment puis-je vous aider aujourd'hui ? 😊",
                        Horodatage = DateTime.Now.ToString("HH:mm")
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement du token : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
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
                Properties.Settings.Default[TOKEN_KEY] = token;
                Properties.Settings.Default.Save();
                
                _apiToken = token;
                NeedTokenConfiguration = false;
                ChatVisible = true;
                
                Messages.Add(new ChatMessage
                {
                    IsUser = false,
                    Auteur = "Agent Project & Change",
                    Message = "Bonjour ! Je suis votre assistante virtuelle. N'hésitez pas à me poser vos questions ! 😊",
                    Horodatage = DateTime.Now.ToString("HH:mm")
                });
                
                MessageBox.Show("Token enregistré avec succès !", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'enregistrement du token : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void Send_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(MessageActuel)) return;

            var userMessage = MessageActuel;
            MessageActuel = "";
            CanSendMessage = false;

            // Ajouter message utilisateur
            var userChatMsg = new ChatMessage
            {
                IsUser = true,
                Auteur = _currentUser.Nom,
                Message = userMessage,
                Horodatage = DateTime.Now.ToString("HH:mm")
            };
            Messages.Add(userChatMsg);

            // Créer conversation si nécessaire
            if (!_conversationId.HasValue)
            {
                _conversationId = _chatHistoryService.StartNewConversation(_currentUser.Id, _currentUser.Nom);
            }

            // Sauvegarder message utilisateur
            _chatHistoryService.SaveMessage(_conversationId.Value, _currentUser.Id, _currentUser.Nom, true, userMessage);

            // Message "en train de réfléchir"
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
                var response = await CallChatAPI(userMessage);
                Messages.Remove(thinkingMessage);

                var agentChatMsg = new ChatMessage
                {
                    IsUser = false,
                    Auteur = "Agent Project & Change",
                    Message = response,
                    Horodatage = DateTime.Now.ToString("HH:mm")
                };
                Messages.Add(agentChatMsg);

                _chatHistoryService.SaveMessage(_conversationId.Value, _currentUser.Id, "Agent Project & Change", false, response);
            }
            catch (Exception ex)
            {
                Messages.Remove(thinkingMessage);
                var errorMsg = new ChatMessage
                {
                    IsUser = false,
                    Auteur = "Agent Project & Change",
                    Message = $"❌ Désolée, j'ai rencontré une erreur : {ex.Message}\n\nAssurez-vous que votre token est valide.",
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
                
                LogDebug($"Token length: {_apiToken?.Length ?? 0}");
                LogDebug($"Token first 20 chars: {(_apiToken?.Length >= 20 ? _apiToken.Substring(0, 20) : _apiToken)}");
                
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiToken}");
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                
                var conversationHistory = Messages
                    .Where(m => m.Message != "Je réfléchis... 💭")
                    .Select(m => new
                    {
                        role = m.IsUser ? "user" : "assistant",
                        content = m.Message
                    })
                    .ToList();

                var messages = new[]
                {
                    new
                    {
                        role = "system",
                        content = @"Tu es Agent Project & Change, assistante virtuelle experte en gestion de projet agile pour BacklogManager.

**Personnalité** : Bienveillante, pédagogue, avec humour et émojis. Réponds de façon CONCISE (3-4 lignes max).

**BacklogManager - Fonctions principales** :
• 📋 Backlog : Créer/modifier tâches (➕ Nouvelle tâche)
• 🎯 Kanban : Glisser-déposer cartes entre colonnes (À FAIRE → EN COURS → TERMINÉ)
• ⏱️ CRA : Saisir heures travaillées (calendrier mensuel, validé par admin)
• 📝 Demandes : Business Analyst crée, Chef de Projet valide
• 📊 Stats & KPI : Vélocité, productivité

**Rôles** :
• Admin : Tous droits + validation CRA
• Chef Projet : Gestion complète tâches/projets
• BA : Créer demandes
• Dev : Ses tâches + CRA

**Consignes** :
✓ Réponses courtes et directes
✓ Utilise **gras**, *italique*, listes à puces
✓ Mentionne les émojis d'icônes
✓ Indique le rôle requis si pertinent
✓ Ton chaleureux et encourageant"
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

                LogDebug($"API URL: {API_URL}");
                LogDebug($"Request body: {jsonContent.Substring(0, Math.Min(500, jsonContent.Length))}...");
                LogDebug($"Authorization header: Bearer {_apiToken?.Substring(0, Math.Min(20, _apiToken?.Length ?? 0))}...");

                var response = await client.PostAsync(API_URL, content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    var statusCode = (int)response.StatusCode;
                    
                    LogDebug($"ERROR - Status Code: {statusCode}");
                    LogDebug($"ERROR - Response: {errorContent}");
                    LogDebug($"ERROR - Headers sent:");
                    foreach (var header in client.DefaultRequestHeaders)
                    {
                        LogDebug($"  {header.Key}: {string.Join(", ", header.Value)}");
                    }
                    
                    throw new Exception($"Erreur {statusCode}: {errorContent}");
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

        private void NewConversation_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Voulez-vous démarrer une nouvelle conversation ?\n\nL'historique actuel sera conservé dans la base de données.",
                "Nouvelle conversation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                Messages.Clear();
                _conversationId = null;
                
                Messages.Add(new ChatMessage
                {
                    IsUser = false,
                    Auteur = "Agent Project & Change",
                    Message = "Nouvelle conversation démarrée ! Comment puis-je vous aider ? 😊",
                    Horodatage = DateTime.Now.ToString("HH:mm")
                });
            }
        }

        private void ClearHistory_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Voulez-vous vraiment effacer tout l'historique des conversations ?\n\nCette action est irréversible.",
                "Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _chatHistoryService.DeleteAllConversations(_currentUser.Id);
                    Messages.Clear();
                    _conversationId = null;
                    
                    Messages.Add(new ChatMessage
                    {
                        IsUser = false,
                        Auteur = "Agent Project & Change",
                        Message = "L'historique a été effacé. Comment puis-je vous aider ? 😊",
                        Horodatage = DateTime.Now.ToString("HH:mm")
                    });
                    
                    MessageBox.Show("L'historique a été effacé avec succès.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de l'effacement de l'historique : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ChangeToken_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Voulez-vous modifier votre token d'accès API ?\n\nVous devrez saisir un nouveau token.",
                "Modifier le token",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                ChatVisible = false;
                NeedTokenConfiguration = true;
                TxtToken.Text = "";
            }
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

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

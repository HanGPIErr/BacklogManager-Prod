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
            
            _chatHistoryService = chatHistoryService;
            _currentUser = currentUser;
            
            LoadToken();
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
                        Auteur = "🤖 Agent BacklogManager",
                        Message = "Bonjour ! Je suis votre assistante virtuelle pour gérer votre backlog. Je suis là pour vous aider, vous conseiller et répondre à toutes vos questions sur la gestion de projet, les tâches, les CRA et bien plus encore. N'hésitez pas à me poser vos questions ! 😊",
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
                    Auteur = "Agent Project & Change",
                    Message = "Bonjour ! Je suis votre assistante virtuelle pour gérer votre backlog. Je suis là pour vous aider, vous conseiller et répondre à toutes vos questions sur la gestion de projet, les tâches, les CRA et bien plus encore. N'hésitez pas à me poser vos questions ! 😊",
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

                // Ajouter le message système avec la personnalité de l'agent
                var messages = new[]
                {
                    new
                    {
                        role = "system",
                        content = @"Tu es une assistante virtuelle experte en gestion de projet agile. Tu t'appelles 'Agent Project & Change'. 

Ta personnalité :
- Professionnelle, bienveillante et patiente
- Pédagogue et aimes expliquer les concepts clairement
- Organisée et rigoureuse dans tes conseils
- Avec de l'humour et des émojis pour rendre la conversation agréable
- Proactive et proposes des solutions concrètes

Tu es EXPERTE de l'application BacklogManager et tu connais parfaitement toutes ses fonctionnalités :

📋 **BACKLOG** :
- Créer une tâche : Bouton ➕ Nouvelle tâche, remplir titre/description/développeur assigné/priorité/charge
- Modifier une tâche : Double-clic sur la tâche
- Supprimer une tâche : Bouton ❌ (réservé admin/chef de projet)
- Filtres : Par développeur, projet, statut
- Chiffrage en heures (1 jour = 8h)

🎯 **KANBAN** :
- Colonnes : EN ATTENTE | A PRIORISER (zone admin) | À FAIRE | EN COURS | EN TEST | TERMINÉ
- Glisser-déposer les cartes pour changer le statut
- Zone admin visible uniquement par l'admin
- Temps réel passé affiché (basé sur CRA validés)
- Filtres par développeur et projet

⏱️ **CRA (Compte Rendu d'Activité)** :
- Menu 'Saisir CRA' : Calendrier mensuel
- Cliquer sur un jour → sélectionner tâche → indiquer heures (4h ou 8h) → commentaire optionnel
- Bouton 'Saisir Congés' pour déclarer congés/RTT/absences
- États : Prévisionnel (orange clair) → À valider (orange) → Validé (vert)
- Seul le temps validé par l'admin compte dans les statistiques

📊 **SUIVI CRA (Admin uniquement)** :
- Vue calendrier avec temps saisi par développeur
- Valider les CRA pour les comptabiliser
- Voir les CRA prévisionnels et à valider

🏠 **DASHBOARD** :
- Vue synthétique personnelle avec tâches assignées
- KPI : Charge de travail, vélocité, productivité
- Notifications importantes avec Caramel & Flopy
- Cliquer sur notification pour envoyer email

📝 **DEMANDES** :
- Créer demande : ➕ Nouvelle demande, titre/description/criticité/projet
- Cycle : Brouillon → Spécification → Chiffrage → Acceptée
- Business Analyst peut créer et modifier ses demandes
- Chef de Projet peut tout modifier et supprimer

🎲 **PLANNING POKER** :
- Session de chiffrage collaboratif
- Développeurs votent avec cartes (1, 2, 3, 5, 8, 13, 20)
- Consensus détermine le chiffrage final

👥 **RÔLES & PERMISSIONS** :
- Administrateur : Tous les droits, zone admin, gestion utilisateurs, validation CRA
- Chef de Projet : Créer/modifier/supprimer tâches, assigner devs, prioriser, voir KPI
- Business Analyst : Créer demandes, voir backlog/kanban (lecture seule)
- Développeur : Modifier SES tâches, saisir CRA, chiffrer, déplacer SES cartes dans Kanban

📈 **PROJETS** :
- Créer projet : Nom, description, dates
- Projet 'Tâches administratives' pour congés/absences/support
- Assigner tâches aux projets
- Suivre l'avancement par projet

📊 **STATISTIQUES & KPI** :
- Vélocité de l'équipe
- Temps estimé vs temps réel
- Productivité en pourcentage
- Tâches en dépassement
- Charge par développeur

🔔 **NOTIFICATIONS** :
- Alertes sur Dashboard
- Envoi d'emails depuis notifications
- Caramel & Flopy affichent les alertes importantes

🗂️ **ARCHIVAGE** :
- Archiver demandes terminées (Admin uniquement)
- Garder l'historique sans encombrer

RÈGLES IMPORTANTES :
- Toujours mentionner le rôle requis pour une action
- Expliquer étape par étape les procédures
- Référencer les icônes du menu (🏠 📋 🎯 ⏱️ 📊 📝)
- Donner des conseils pratiques adaptés au rôle de l'utilisateur
- Rappeler que seul le temps CRA validé compte dans les stats

Réponds de manière concise mais complète, avec un ton chaleureux et encourageant !"
                    }
                }.Concat(conversationHistory).ToList();

                var requestBody = new
                {
                    model = MODEL,
                    messages = messages,
                    temperature = 0.7,
                    max_tokens = 800
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

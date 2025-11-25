using System;
using System.Collections.Generic;
using BacklogManager.Domain;

namespace BacklogManager.Services
{
    public class ChatHistoryService
    {
        private readonly IDatabase _database;
        private int? _currentConversationId;

        public ChatHistoryService(IDatabase database)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
        }

        /// <summary>
        /// Démarre une nouvelle conversation pour un utilisateur
        /// </summary>
        public int StartNewConversation(int userId, string username)
        {
            _currentConversationId = _database.CreateChatConversation(userId, username);
            return _currentConversationId.Value;
        }

        /// <summary>
        /// Enregistre un message dans la conversation actuelle
        /// </summary>
        public void SaveMessage(int conversationId, int userId, string username, bool isUser, string message, string reaction = null)
        {
            var chatMessage = new ChatMessageDB
            {
                ConversationId = conversationId,
                UserId = userId,
                Username = username,
                IsUser = isUser,
                Message = message,
                DateMessage = DateTime.Now,
                Reaction = reaction
            };

            _database.AddChatMessage(chatMessage);
        }

        /// <summary>
        /// Récupère toutes les conversations
        /// </summary>
        public List<ChatConversation> GetAllConversations()
        {
            return _database.GetChatConversations();
        }

        /// <summary>
        /// Récupère l'historique d'une conversation spécifique
        /// </summary>
        public List<ChatMessageDB> GetConversationHistory(int conversationId)
        {
            return _database.GetChatMessages(conversationId);
        }

        /// <summary>
        /// Récupère les informations d'une conversation
        /// </summary>
        public ChatConversation GetConversation(int conversationId)
        {
            return _database.GetChatConversation(conversationId);
        }

        /// <summary>
        /// Supprime toutes les conversations d'un utilisateur
        /// </summary>
        public void DeleteAllConversations(int userId)
        {
            _database.DeleteUserChatConversations(userId);
            _currentConversationId = null;
        }

        /// <summary>
        /// ID de la conversation actuelle
        /// </summary>
        public int? CurrentConversationId
        {
            get => _currentConversationId;
            set => _currentConversationId = value;
        }
    }
}

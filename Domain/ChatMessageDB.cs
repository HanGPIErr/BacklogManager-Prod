using System;

namespace BacklogManager.Domain
{
    public class ChatMessageDB
    {
        public int Id { get; set; }
        public int ConversationId { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; }
        public bool IsUser { get; set; } // true si message utilisateur, false si message agent
        public string Message { get; set; }
        public DateTime DateMessage { get; set; }
        public string Reaction { get; set; } // Emoji de réaction (optionnel)
    }
}

using System;

namespace BacklogManager.Domain
{
    public class ChatConversation
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; }
        public DateTime DateDebut { get; set; }
        public DateTime DateDernierMessage { get; set; }
        public int NombreMessages { get; set; }
    }
}

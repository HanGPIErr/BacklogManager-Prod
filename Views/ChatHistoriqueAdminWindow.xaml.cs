using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using BacklogManager.Domain;
using BacklogManager.Services;
using BacklogManager.Shared;

namespace BacklogManager.Views
{
    public partial class ChatHistoriqueAdminWindow : Window, INotifyPropertyChanged
    {
        private readonly ChatHistoryService _chatHistoryService;

        public ObservableCollection<ChatConversation> Conversations { get; set; }
        public ObservableCollection<ChatMessageViewModel> Messages { get; set; }

        private ChatConversation _conversationSelectionnee;
        public ChatConversation ConversationSelectionnee
        {
            get => _conversationSelectionnee;
            set
            {
                _conversationSelectionnee = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ConversationSelectionneeVisibility));
                OnPropertyChanged(nameof(AucuneConversationVisibility));
            }
        }

        public Visibility ConversationSelectionneeVisibility => ConversationSelectionnee != null ? Visibility.Visible : Visibility.Collapsed;
        public Visibility AucuneConversationVisibility => ConversationSelectionnee == null ? Visibility.Visible : Visibility.Collapsed;

        public int NbConversations => Conversations?.Count ?? 0;

        public ICommand SelectConversationCommand { get; set; }

        public ChatHistoriqueAdminWindow(ChatHistoryService chatHistoryService)
        {
            InitializeComponent();
            DataContext = this;

            _chatHistoryService = chatHistoryService;
            Conversations = new ObservableCollection<ChatConversation>();
            Messages = new ObservableCollection<ChatMessageViewModel>();

            SelectConversationCommand = new RelayCommand(param => SelectConversation((ChatConversation)param));

            LoadConversations();
        }

        private void LoadConversations()
        {
            try
            {
                var conversations = _chatHistoryService.GetAllConversations();
                Conversations.Clear();
                foreach (var conv in conversations)
                {
                    Conversations.Add(conv);
                }
                OnPropertyChanged(nameof(NbConversations));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des conversations : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SelectConversation(ChatConversation conversation)
        {
            try
            {
                ConversationSelectionnee = conversation;
                Messages.Clear();

                var messages = _chatHistoryService.GetConversationHistory(conversation.Id);
                foreach (var msg in messages)
                {
                    Messages.Add(new ChatMessageViewModel(msg));
                }

                // Scroller vers le bas
                MessagesScrollViewer.ScrollToBottom();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement de la conversation : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ChatMessageViewModel
    {
        private readonly ChatMessageDB _message;

        public ChatMessageViewModel(ChatMessageDB message)
        {
            _message = message;
        }

        public string Username => _message.Username;
        public string Message => _message.Message;
        public DateTime DateMessage => _message.DateMessage;
        public string Reaction => _message.Reaction;
        public bool HasReactionBool => !string.IsNullOrEmpty(_message.Reaction);
        public Visibility HasReaction => HasReactionBool ? Visibility.Visible : Visibility.Collapsed;

        public HorizontalAlignment Alignment => _message.IsUser ? HorizontalAlignment.Right : HorizontalAlignment.Left;
        
        public string BackgroundColor => _message.IsUser ? "#E3F2FD" : "#F3F2F1";
        public string BorderColor => _message.IsUser ? "#BBDEFB" : "#E1DFDD";
        public string HeaderColor => _message.IsUser ? "#1976D2" : "#616161";
        public string ReactionBackground => "#FFF4E5";
    }
}

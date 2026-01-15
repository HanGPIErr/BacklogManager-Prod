using System;
using System.Collections.Generic;
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

        public ObservableCollection<UtilisateurConversation> Utilisateurs { get; set; }
        public ObservableCollection<ChatMessageViewModel> Messages { get; set; }

        private UtilisateurConversation _utilisateurSelectionne;
        public UtilisateurConversation UtilisateurSelectionne
        {
            get => _utilisateurSelectionne;
            set
            {
                _utilisateurSelectionne = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ConversationSelectionneeVisibility));
                OnPropertyChanged(nameof(AucuneConversationVisibility));
            }
        }

        public Visibility ConversationSelectionneeVisibility => UtilisateurSelectionne != null ? Visibility.Visible : Visibility.Collapsed;
        public Visibility AucuneConversationVisibility => UtilisateurSelectionne == null ? Visibility.Visible : Visibility.Collapsed;

        public int NbUtilisateurs => Utilisateurs?.Count ?? 0;

        public ICommand SelectUtilisateurCommand { get; set; }

        public ChatHistoriqueAdminWindow(ChatHistoryService chatHistoryService)
        {
            InitializeComponent();
            DataContext = this;

            _chatHistoryService = chatHistoryService;
            Utilisateurs = new ObservableCollection<UtilisateurConversation>();
            Messages = new ObservableCollection<ChatMessageViewModel>();

            SelectUtilisateurCommand = new RelayCommand(param => SelectUtilisateur((UtilisateurConversation)param));

            InitialiserTextes();
            LoadUtilisateurs();
        }
        
        private void InitialiserTextes()
        {
            // Titre de la fenêtre
            this.Title = LocalizationService.Instance.GetString("ChatHistory_Title");
            
            // Labels
            TxtUsers.Text = LocalizationService.Instance.GetString("ChatHistory_Users");
            TxtFullHistory.Text = LocalizationService.Instance.GetString("ChatHistory_FullHistory");
            TxtSelectUser.Text = LocalizationService.Instance.GetString("ChatHistory_SelectUser");
            
            // S'abonner aux changements de langue
            LocalizationService.Instance.PropertyChanged += (s, e) =>
            {
                this.Title = LocalizationService.Instance.GetString("ChatHistory_Title");
                TxtUsers.Text = LocalizationService.Instance.GetString("ChatHistory_Users");
                TxtFullHistory.Text = LocalizationService.Instance.GetString("ChatHistory_FullHistory");
                TxtSelectUser.Text = LocalizationService.Instance.GetString("ChatHistory_SelectUser");
                OnPropertyChanged(nameof(NbUtilisateurs));
            };
        }

        private void LoadUtilisateurs()
        {
            try
            {
                var conversations = _chatHistoryService.GetAllConversations();
                
                // Grouper par utilisateur et prendre la dernière conversation
                var utilisateursGroupes = conversations
                    .GroupBy(c => c.UserId)
                    .Select(g => new UtilisateurConversation
                    {
                        UserId = g.Key,
                        Username = g.First().Username,
                        NombreConversations = g.Count(),
                        DerniereConversation = g.OrderByDescending(c => c.DateDernierMessage).First(),
                        ToutesLesConversations = g.OrderByDescending(c => c.DateDernierMessage).ToList()
                    })
                    .OrderByDescending(u => u.DerniereConversation.DateDernierMessage)
                    .ToList();

                Utilisateurs.Clear();
                foreach (var utilisateur in utilisateursGroupes)
                {
                    Utilisateurs.Add(utilisateur);
                }
                OnPropertyChanged(nameof(NbUtilisateurs));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des utilisateurs : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SelectUtilisateur(UtilisateurConversation utilisateur)
        {
            try
            {
                UtilisateurSelectionne = utilisateur;
                Messages.Clear();

                // Charger tous les messages de toutes les conversations de cet utilisateur
                foreach (var conversation in utilisateur.ToutesLesConversations)
                {
                    var messages = _chatHistoryService.GetConversationHistory(conversation.Id);
                    foreach (var msg in messages)
                    {
                        Messages.Add(new ChatMessageViewModel(msg));
                    }
                }

                // Trier les messages par date
                var messagesTries = Messages.OrderBy(m => m.DateMessage).ToList();
                Messages.Clear();
                foreach (var msg in messagesTries)
                {
                    Messages.Add(msg);
                }

                // Scroller vers le bas
                MessagesScrollViewer.ScrollToBottom();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement de l'historique : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class UtilisateurConversation
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public int NombreConversations { get; set; }
        public ChatConversation DerniereConversation { get; set; }
        public List<ChatConversation> ToutesLesConversations { get; set; }

        public string DateDernierMessage => string.Format(LocalizationService.Instance.GetString("ChatHistory_LastMessage"), 
            DerniereConversation.DateDernierMessage.ToString("dd/MM/yyyy HH:mm"));
        public string NbMessages => string.Format(LocalizationService.Instance.GetString("ChatHistory_MessageCount"), 
            DerniereConversation.NombreMessages);
        public string NbConversations => NombreConversations > 1 
            ? string.Format(LocalizationService.Instance.GetString("ChatHistory_ConversationCount"), NombreConversations)
            : LocalizationService.Instance.GetString("ChatHistory_OneConversation");
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

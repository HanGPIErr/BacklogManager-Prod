using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using BacklogManager.Domain;
using BacklogManager.Services;
using BacklogManager.Shared;

namespace BacklogManager.ViewModels
{
    public class DevVoteViewModel : INotifyPropertyChanged
    {
        public Dev Dev { get; set; }
        
        private int _voteValue;
        public int VoteValue
        {
            get { return _voteValue; }
            set
            {
                _voteValue = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class PokerViewModel : INotifyPropertyChanged
    {
        private readonly BacklogService _backlogService;
        private readonly PokerService _pokerService;
        private BacklogItem _selectedBacklogItem;
        private PokerSession _currentSession;
        private int _voteRound;
        private string _statusMessage;
        private int _consensus;
        private double _joursPlanifies;

        public ObservableCollection<BacklogItem> BacklogItems { get; set; }
        public ObservableCollection<DevVoteViewModel> DevVotes { get; set; }
        public List<int> VoteValues { get; set; }

        public BacklogItem SelectedBacklogItem
        {
            get { return _selectedBacklogItem; }
            set
            {
                _selectedBacklogItem = value;
                OnPropertyChanged();
            }
        }

        public string StatusMessage
        {
            get { return _statusMessage; }
            set
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }

        public int Consensus
        {
            get { return _consensus; }
            set
            {
                _consensus = value;
                OnPropertyChanged();
            }
        }

        public double JoursPlanifies
        {
            get { return _joursPlanifies; }
            set
            {
                _joursPlanifies = value;
                OnPropertyChanged();
            }
        }

        public ICommand StartVoteCommand { get; }
        public ICommand SubmitVotesCommand { get; }

        public PokerViewModel(BacklogService backlogService, PokerService pokerService)
        {
            _backlogService = backlogService;
            _pokerService = pokerService;
            
            BacklogItems = new ObservableCollection<BacklogItem>();
            DevVotes = new ObservableCollection<DevVoteViewModel>();
            VoteValues = new List<int> { 1, 2, 3, 5, 8, 13, 21 };
            
            _voteRound = 0;

            StartVoteCommand = new RelayCommand(_ => StartVote(), _ => SelectedBacklogItem != null);
            SubmitVotesCommand = new RelayCommand(_ => SubmitVotes(), _ => _currentSession != null);

            LoadBacklogItems();
        }

        public void LoadBacklogItems()
        {
            var items = _backlogService.GetAllBacklogItems()
                .Where(x => x.Statut != Statut.Termine)
                .ToList();
            
            BacklogItems.Clear();
            foreach (var item in items)
            {
                BacklogItems.Add(item);
            }
        }

        private void StartVote()
        {
            if (SelectedBacklogItem == null) return;

            _currentSession = _pokerService.CreatePokerSession(SelectedBacklogItem.Id);
            _voteRound = 1;

            LoadDevVotes();
            StatusMessage = "Tour 1 : Votez pour la complexité (1-21)";
        }

        private void LoadDevVotes()
        {
            var devs = _backlogService.GetAllDevs();
            DevVotes.Clear();
            foreach (var dev in devs)
            {
                DevVotes.Add(new DevVoteViewModel 
                { 
                    Dev = dev,
                    VoteValue = 1
                });
            }
        }

        private void SubmitVotes()
        {
            if (_currentSession == null) return;

            foreach (var devVote in DevVotes)
            {
                _pokerService.AddVote(_currentSession.Id, devVote.Dev.Id, devVote.VoteValue);
            }

            if (_voteRound == 1)
            {
                bool hasGaps = _pokerService.HasVoteGaps(_currentSession.Id);
                if (hasGaps)
                {
                    StatusMessage = "Écarts détectés ! Tour 2 : Revotez pour atteindre un consensus.";
                    _voteRound = 2;
                    foreach (var devVote in DevVotes)
                    {
                        devVote.VoteValue = 1;
                    }
                }
                else
                {
                    FinalizeVoting();
                }
            }
            else if (_voteRound == 2)
            {
                FinalizeVoting();
            }
        }

        private void FinalizeVoting()
        {
            int consensus = _pokerService.CalculateConsensus(_currentSession.Id);
            _pokerService.FinalizeSession(_currentSession.Id, consensus);

            Consensus = consensus;
            JoursPlanifies = consensus / 2.0;

            StatusMessage = string.Format("Consensus atteint ! Complexité: {0}, Jours planifiés: {1:F2}", consensus, JoursPlanifies);
            
            _currentSession = null;
            _voteRound = 0;
            
            LoadBacklogItems();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

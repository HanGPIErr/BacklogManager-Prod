using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using BacklogManager.Shared;

namespace BacklogManager.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private object _currentView;

        public object CurrentView
        {
            get { return _currentView; }
            set
            {
                _currentView = value;
                OnPropertyChanged();
            }
        }

        public ICommand ShowProjetsCommand { get; }
        public ICommand ShowBacklogCommand { get; }
        public ICommand ShowKanbanCommand { get; }
        public ICommand ShowPokerCommand { get; }
        public ICommand ShowTimelineCommand { get; }

        public ProjetsViewModel ProjetsViewModel { get; }
        public BacklogViewModel BacklogViewModel { get; }
        public KanbanViewModel KanbanViewModel { get; }
        public PokerViewModel PokerViewModel { get; }
        public TimelineViewModel TimelineViewModel { get; }

        public MainViewModel(ProjetsViewModel projetsViewModel, BacklogViewModel backlogViewModel, KanbanViewModel kanbanViewModel, PokerViewModel pokerViewModel, TimelineViewModel timelineViewModel)
        {
            ProjetsViewModel = projetsViewModel;
            BacklogViewModel = backlogViewModel;
            KanbanViewModel = kanbanViewModel;
            PokerViewModel = pokerViewModel;
            TimelineViewModel = timelineViewModel;

            ShowProjetsCommand = new RelayCommand(_ => 
            {
                ProjetsViewModel.LoadProjets();
                CurrentView = ProjetsViewModel;
            });
            ShowBacklogCommand = new RelayCommand(_ => CurrentView = BacklogViewModel);
            ShowKanbanCommand = new RelayCommand(_ => 
            {
                KanbanViewModel.LoadItems();
                CurrentView = KanbanViewModel;
            });
            ShowPokerCommand = new RelayCommand(_ => 
            {
                PokerViewModel.LoadBacklogItems();
                CurrentView = PokerViewModel;
            });
            ShowTimelineCommand = new RelayCommand(_ =>
            {
                TimelineViewModel.LoadItems();
                CurrentView = TimelineViewModel;
            });

            // Show Projets by default
            CurrentView = ProjetsViewModel;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

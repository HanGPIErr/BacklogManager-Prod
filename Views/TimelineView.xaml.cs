using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BacklogManager.ViewModels;
using BacklogManager.Services;

namespace BacklogManager.Views
{
    public partial class TimelineView : UserControl
    {
        public TimelineView()
        {
            InitializeComponent();
        }

        private void ProjetBarre_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is TimelineProjetViewModel projetVM)
            {
                // Récupérer le BacklogService depuis le DataContext
                var timelineVM = this.DataContext as TimelineViewModel;
                if (timelineVM != null)
                {
                    var backlogService = new BacklogService(new SqliteDatabase());
                    var detailsWindow = new ProjetDetailsWindow(projetVM.Projet, backlogService);
                    detailsWindow.ShowDialog();
                }
            }
        }
    }
}

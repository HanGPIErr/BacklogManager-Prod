using System.Windows.Controls;

namespace BacklogManager.Views
{
    public partial class SuiviCRAView : UserControl
    {
        public SuiviCRAView()
        {
            InitializeComponent();
        }

        private void TimelineScroll_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (sender is ScrollViewer scrollViewer && TimelineHeaderScroll != null)
            {
                TimelineHeaderScroll.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset);
            }
        }
    }
}

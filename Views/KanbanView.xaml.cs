using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BacklogManager.Domain;
using BacklogManager.ViewModels;

namespace BacklogManager.Views
{
    public partial class KanbanView : UserControl
    {
        public KanbanView()
        {
            InitializeComponent();
        }

        private void KanbanCard_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var border = sender as Border;
            if (border?.DataContext is KanbanItemViewModel kanbanItem)
            {
                var viewModel = DataContext as KanbanViewModel;
                if (viewModel != null)
                {
                    viewModel.OuvrirDetailsTache(kanbanItem.Item);
                }
            }
        }
    }
}

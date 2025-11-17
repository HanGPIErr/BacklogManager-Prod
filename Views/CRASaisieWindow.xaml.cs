using System.Windows;
using BacklogManager.ViewModels;
using BacklogManager.Services;

namespace BacklogManager.Views
{
    public partial class CRASaisieWindow : Window
    {
        public CRASaisieWindow(IDatabase db, int currentUserId, bool isAdmin)
        {
            InitializeComponent();
            DataContext = new CRAViewModel(db, currentUserId, isAdmin);
        }
    }
}

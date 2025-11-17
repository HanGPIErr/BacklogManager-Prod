using System.Windows;
using BacklogManager.ViewModels;
using BacklogManager.Services;

namespace BacklogManager.Views
{
    public partial class CRAHistoriqueWindow : Window
    {
        public CRAHistoriqueWindow(IDatabase db, int currentUserId, bool isAdmin)
        {
            InitializeComponent();
            DataContext = new CRAHistoriqueViewModel(db, currentUserId, isAdmin);
        }
    }
}

using System;
using System.Windows;
using BacklogManager.Services;
using BacklogManager.ViewModels;
using BacklogManager.Views;

namespace BacklogManager
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Show login window instead of MainWindow
            var loginWindow = new LoginWindow();
            loginWindow.Show();
        }
    }
}

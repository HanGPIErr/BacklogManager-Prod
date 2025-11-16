using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BacklogManager.Domain;
using BacklogManager.Services;

namespace BacklogManager.Views
{
    public partial class GestionRolesWindow : Window
    {
        private readonly IDatabase _database;

        public GestionRolesWindow()
        {
            InitializeComponent();
            _database = new SqliteDatabase();
            ChargerRoles();
        }

        private void ChargerRoles()
        {
            var roles = _database.GetRoles();
            LstRoles.ItemsSource = roles;
        }

        private void BtnEnregistrerRole_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var role = button?.Tag as Role;

            if (role != null)
            {
                _database.UpdateRole(role);
                MessageBox.Show($"Les permissions du rôle '{role.Nom}' ont été mises à jour avec succès.", 
                                "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnFermer_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

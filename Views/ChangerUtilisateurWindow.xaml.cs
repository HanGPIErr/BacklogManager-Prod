using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using BacklogManager.Domain;
using BacklogManager.Services;

namespace BacklogManager.Views
{
    public partial class ChangerUtilisateurWindow : Window
    {
        private readonly IDatabase _database;
        public Utilisateur UtilisateurSelectionne { get; private set; }

        public ChangerUtilisateurWindow(IDatabase database)
        {
            InitializeComponent();
            _database = database;
            ChargerUtilisateurs();
        }

        private void ChargerUtilisateurs()
        {
            var utilisateurs = _database.GetUtilisateurs()
                .Where(u => u.Actif)
                .OrderBy(u => u.RoleId)
                .ThenBy(u => u.Nom)
                .ToList();

            var roles = _database.GetRoles();

            foreach (var utilisateur in utilisateurs)
            {
                var role = roles.FirstOrDefault(r => r.Id == utilisateur.RoleId);
                
                var border = new Border
                {
                    Style = (Style)FindResource("UserItemStyle"),
                    Tag = utilisateur
                };

                border.MouseLeftButtonDown += Border_MouseLeftButtonDown;
                border.MouseEnter += Border_MouseEnter;
                border.MouseLeave += Border_MouseLeave;

                var stackPanel = new StackPanel();

                // Nom complet
                var nomTextBlock = new TextBlock
                {
                    Text = $"{utilisateur.Prenom} {utilisateur.Nom}",
                    Style = (Style)FindResource("UserNameStyle")
                };
                stackPanel.Children.Add(nomTextBlock);

                // Rôle
                string roleIcon = GetRoleIcon(role?.Type ?? RoleType.Developpeur);
                var roleTextBlock = new TextBlock
                {
                    Text = $"{roleIcon} {role?.Nom ?? "Sans rôle"}",
                    Style = (Style)FindResource("UserRoleStyle")
                };
                stackPanel.Children.Add(roleTextBlock);

                // Email
                var emailTextBlock = new TextBlock
                {
                    Text = utilisateur.Email,
                    FontSize = 11,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#999999")),
                    Margin = new Thickness(0, 2, 0, 0)
                };
                stackPanel.Children.Add(emailTextBlock);

                border.Child = stackPanel;
                PanelUtilisateurs.Children.Add(border);
            }
        }

        private string GetRoleIcon(RoleType roleType)
        {
            return roleType switch
            {
                RoleType.Administrateur => "⚙️",
                RoleType.BusinessAnalyst => "📊",
                RoleType.ChefDeProjet => "👔",
                RoleType.Developpeur => "💻",
                _ => "👤"
            };
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is Utilisateur utilisateur)
            {
                UtilisateurSelectionne = utilisateur;
                DialogResult = true;
                Close();
            }
        }

        private void Border_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                border.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00915A"));
                
                // Changer la couleur des textes au survol
                if (border.Child is StackPanel panel)
                {
                    foreach (var child in panel.Children)
                    {
                        if (child is TextBlock textBlock)
                        {
                            textBlock.Foreground = Brushes.White;
                        }
                    }
                }
            }
        }

        private void Border_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                border.Background = Brushes.White;
                
                // Rétablir les couleurs d'origine
                if (border.Child is StackPanel panel)
                {
                    int index = 0;
                    foreach (var child in panel.Children)
                    {
                        if (child is TextBlock textBlock)
                        {
                            if (index == 0) // Nom
                                textBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1919"));
                            else // Rôle et email
                                textBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666666"));
                            
                            if (index == 2) // Email
                                textBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#999999"));
                            
                            index++;
                        }
                    }
                }
            }
        }

        private void BtnAnnuler_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}

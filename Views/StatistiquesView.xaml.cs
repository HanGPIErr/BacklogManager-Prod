using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using BacklogManager.ViewModels;

namespace BacklogManager.Views
{
    public partial class StatistiquesView : UserControl
    {
        public StatistiquesView()
        {
            InitializeComponent();
            Loaded += StatistiquesView_Loaded;
        }

        private void StatistiquesView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (CboPeriode != null)
            {
                CboPeriode.SelectedIndex = 5; // "Tout afficher" par défaut
            }
        }

        private void CboPeriode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Vérifier que le DataContext est initialisé
            if (DataContext == null) return;

            // Afficher/masquer les DatePickers pour période personnalisée
            if (CboPeriode.SelectedIndex == 4) // Période personnalisée
            {
                PanelDatesCustom.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                PanelDatesCustom.Visibility = System.Windows.Visibility.Collapsed;
                
                // Appliquer directement le filtre pour les périodes prédéfinies
                if (DataContext is StatistiquesViewModel viewModel)
                {
                    viewModel.AppliquerFiltrePeriode(CboPeriode.SelectedIndex);
                }
            }
        }

        private void BtnAppliquerPeriode_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is StatistiquesViewModel viewModel)
            {
                viewModel.AppliquerFiltrePeriode(CboPeriode.SelectedIndex);
            }
        }

        private void GridChargeParDev_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is StatistiquesViewModel viewModel && ((DataGrid)sender).SelectedItem is ChargeDevViewModel devStats)
            {
                viewModel.AfficherDetailsDevCommand?.Execute(devStats);
            }
        }

        private void GridCRAParDev_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is StatistiquesViewModel viewModel && ((DataGrid)sender).SelectedItem is CRADevViewModel craDevStats)
            {
                viewModel.AfficherDetailsCRADevCommand?.Execute(craDevStats);
            }
        }

        // Event handlers pour les effets hover sur les cartes
        private void DevCard_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                border.Background = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                border.BorderBrush = new SolidColorBrush(Color.FromRgb(0, 145, 90));
                border.BorderThickness = new System.Windows.Thickness(2);
            }
        }

        private void DevCard_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                border.Background = new SolidColorBrush(Color.FromRgb(249, 249, 249));
                border.BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224));
                border.BorderThickness = new System.Windows.Thickness(1);
            }
        }
    }
}

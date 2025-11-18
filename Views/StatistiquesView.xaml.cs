using System.Windows.Controls;
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

        private void GridChargeParDev_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (DataContext is StatistiquesViewModel viewModel && GridChargeParDev.SelectedItem is ChargeDevViewModel devStats)
            {
                viewModel.AfficherDetailsDevCommand?.Execute(devStats);
            }
        }

        private void GridCRAParDev_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (DataContext is StatistiquesViewModel viewModel && GridCRAParDev.SelectedItem is CRADevViewModel craDevStats)
            {
                viewModel.AfficherDetailsCRADevCommand?.Execute(craDevStats);
            }
        }
    }
}

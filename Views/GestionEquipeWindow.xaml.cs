using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BacklogManager.Domain;
using BacklogManager.Services;

namespace BacklogManager.Views
{
    public partial class GestionEquipeWindow : Window
    {
        private readonly BacklogService _backlogService;
        private Dev _devEnEdition = null;

        public GestionEquipeWindow(BacklogService backlogService)
        {
            InitializeComponent();
            _backlogService = backlogService;
            ChargerDevs();
        }

        private void ChargerDevs()
        {
            var devs = _backlogService.GetAllDevs();
            LstDevs.ItemsSource = devs;
            TxtCountDevs.Text = string.Format("{0} dev(s)", devs.Count);
        }

        private void BtnAjouter_Click(object sender, RoutedEventArgs e)
        {
            var nom = TxtNomDev.Text.Trim();
            var initiales = TxtInitiales.Text.Trim().ToUpper();
            
            if (string.IsNullOrEmpty(nom))
            {
                MessageBox.Show("Veuillez saisir un nom.", "Validation", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            if (string.IsNullOrEmpty(initiales))
            {
                MessageBox.Show("Veuillez saisir des initiales.", "Validation", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var newDev = new Dev
            {
                Id = 0,
                Nom = nom,
                Initiales = initiales,
                Actif = true
            };

            _backlogService.SaveDev(newDev);
            TxtNomDev.Clear();
            TxtInitiales.Clear();
            ChargerDevs();

            MessageBox.Show(string.Format("Développeur '{0}' ({1}) ajouté avec succès!", nom, initiales), "Succès", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnModifier_Click(object sender, RoutedEventArgs e)
        {
            if (_devEnEdition == null) return;

            var nom = TxtNomDev.Text.Trim();
            var initiales = TxtInitiales.Text.Trim().ToUpper();
            
            if (string.IsNullOrEmpty(nom))
            {
                MessageBox.Show("Veuillez saisir un nom.", "Validation", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            if (string.IsNullOrEmpty(initiales))
            {
                MessageBox.Show("Veuillez saisir des initiales.", "Validation", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _devEnEdition.Nom = nom;
            _devEnEdition.Initiales = initiales;
            _backlogService.SaveDev(_devEnEdition);

            AnnulerEdition();
            ChargerDevs();

            MessageBox.Show("Développeur modifié avec succès!", "Succès", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnAnnuler_Click(object sender, RoutedEventArgs e)
        {
            AnnulerEdition();
        }

        private void AnnulerEdition()
        {
            _devEnEdition = null;
            TxtNomDev.Clear();
            TxtInitiales.Clear();
            LstDevs.SelectedItem = null;

            BtnAjouter.Visibility = Visibility.Visible;
            BtnModifier.Visibility = Visibility.Collapsed;
            BtnAnnuler.Visibility = Visibility.Collapsed;
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var dev = button?.Tag as Dev;
            if (dev == null) return;

            _devEnEdition = dev;
            TxtNomDev.Text = dev.Nom;
            TxtInitiales.Text = dev.Initiales;

            BtnAjouter.Visibility = Visibility.Collapsed;
            BtnModifier.Visibility = Visibility.Visible;
            BtnAnnuler.Visibility = Visibility.Visible;
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var dev = button?.Tag as Dev;
            if (dev == null) return;

            var result = MessageBox.Show(
                string.Format("Êtes-vous sûr de vouloir supprimer '{0}' ?\n\nAttention: Les tâches assignées à ce développeur ne seront pas supprimées.", dev.Nom),
                "Confirmer la suppression",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                _backlogService.DeleteDev(dev.Id);
                AnnulerEdition();
                ChargerDevs();

                MessageBox.Show("Développeur supprimé avec succès!", "Succès", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void LstDevs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Optionnel: gérer la sélection
        }

        private void BtnFermer_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}

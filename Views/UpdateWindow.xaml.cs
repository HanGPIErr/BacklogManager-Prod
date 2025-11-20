using System;
using System.Windows;
using BacklogManager.Domain;
using BacklogManager.Services;

namespace BacklogManager.Views
{
    public partial class UpdateWindow : Window
    {
        private readonly VersionInfo _versionInfo;
        private readonly UpdateService _updateService;
        private readonly bool _mandatory;

        public UpdateWindow(VersionInfo versionInfo, UpdateService updateService)
        {
            InitializeComponent();
            
            _versionInfo = versionInfo;
            _updateService = updateService;
            _mandatory = versionInfo.Mandatory;

            LoadVersionInfo();
        }

        private void LoadVersionInfo()
        {
            TxtCurrentVersion.Text = _updateService.GetCurrentVersion();
            TxtNewVersion.Text = _versionInfo.Version;
            TxtReleaseDate.Text = _versionInfo.ReleaseDate.ToString("dd/MM/yyyy");
            TxtChangelog.Text = _versionInfo.Changelog ?? "Aucune information disponible.";

            if (_mandatory)
            {
                BorderMandatory.Visibility = Visibility.Visible;
                BtnLater.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnUpdate_Click(object sender, RoutedEventArgs e)
        {
            BtnUpdate.IsEnabled = false;
            BtnLater.IsEnabled = false;
            BtnUpdate.Content = "Installation en cours...";

            try
            {
                bool success = _updateService.DownloadAndInstallUpdate(_versionInfo.DownloadUrl);

                if (success)
                {
                    MessageBox.Show(
                        "La mise à jour va être installée.\n\n" +
                        "L'application va se fermer et redémarrer automatiquement.",
                        "Mise à jour",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    // Fermer l'application pour permettre la mise à jour
                    Application.Current.Shutdown();
                }
                else
                {
                    MessageBox.Show(
                        "Erreur lors du téléchargement de la mise à jour.\n\n" +
                        "Veuillez réessayer plus tard ou contacter votre administrateur.",
                        "Erreur",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);

                    BtnUpdate.IsEnabled = true;
                    BtnLater.IsEnabled = true;
                    BtnUpdate.Content = "Mettre à jour";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erreur lors de l'installation de la mise à jour:\n\n{ex.Message}",
                    "Erreur",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                BtnUpdate.IsEnabled = true;
                BtnLater.IsEnabled = true;
                BtnUpdate.Content = "Mettre à jour";
            }
        }

        private void BtnLater_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (_mandatory)
            {
                e.Cancel = true;
                MessageBox.Show(
                    "Cette mise à jour est obligatoire.\n\n" +
                    "Vous devez installer la mise à jour pour continuer à utiliser l'application.",
                    "Mise à jour obligatoire",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            base.OnClosing(e);
        }
    }
}

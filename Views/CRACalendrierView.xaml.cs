using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BacklogManager.ViewModels;

namespace BacklogManager.Views
{
    public partial class CRACalendrierView : UserControl
    {
        private CRADisplayViewModel _craDragged = null;
        private Point _startPoint;
        private bool _isDragging = false;

        public CRACalendrierView()
        {
            InitializeComponent();
        }

        private void CRA_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Enregistrer le point de départ du drag
            _startPoint = e.GetPosition(null);
            _isDragging = false;
            
            if (sender is Border border)
            {
                _craDragged = border.Tag as CRADisplayViewModel;
                System.Diagnostics.Debug.WriteLine($"CRA PreviewMouseDown: {_craDragged?.TacheNom}");
            }
        }

        private void CRA_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("CRA MouseUp");
            // Reset du drag
            _isDragging = false;
            _craDragged = null;
        }

        private void CRA_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && !_isDragging && _craDragged != null)
            {
                Point currentPosition = e.GetPosition(null);
                
                // Vérifier que la souris a bougé suffisamment pour initier un drag
                if (Math.Abs(currentPosition.X - _startPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(currentPosition.Y - _startPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    _isDragging = true;
                    System.Diagnostics.Debug.WriteLine($"Starting drag for: {_craDragged.TacheNom}");
                    
                    if (sender is Border border)
                    {
                        // Créer un DataObject avec les données
                        DataObject dragData = new DataObject(typeof(CRADisplayViewModel), _craDragged);
                        
                        // Initier le drag and drop
                        DragDropEffects result = DragDrop.DoDragDrop(border, dragData, DragDropEffects.Move);
                        
                        System.Diagnostics.Debug.WriteLine($"Drag completed with result: {result}");
                        
                        // Reset après le drop
                        _isDragging = false;
                        _craDragged = null;
                    }
                }
            }
        }

        private void Jour_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(CRADisplayViewModel)) && sender is Border border)
            {
                // Mettre en surbrillance le jour cible
                var jour = border.Tag as JourCalendrierViewModel;
                if (jour != null && jour.EstDansMois)
                {
                    e.Effects = DragDropEffects.Move;
                    // Changer l'opacité ou la couleur pour feedback visuel
                    border.Opacity = 0.7;
                }
                else
                {
                    e.Effects = DragDropEffects.None;
                }
            }
            e.Handled = true;
        }

        private void Jour_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(CRADisplayViewModel)))
            {
                e.Effects = DragDropEffects.Move;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void Jour_DragLeave(object sender, DragEventArgs e)
        {
            // Restaurer l'apparence normale
            if (sender is Border border)
            {
                border.Opacity = 1.0;
            }
            e.Handled = true;
        }

        private void Jour_Drop(object sender, DragEventArgs e)
        {
            // Restaurer l'opacité
            if (sender is Border border)
            {
                border.Opacity = 1.0;
            }

            if (e.Data.GetDataPresent(typeof(CRADisplayViewModel)) && sender is Border brd)
            {
                var craDisplay = e.Data.GetData(typeof(CRADisplayViewModel)) as CRADisplayViewModel;
                var jourCible = brd.Tag as JourCalendrierViewModel;
                
                if (craDisplay != null && jourCible != null && DataContext is CRACalendrierViewModel viewModel)
                {
                    // Appeler la commande de déplacement dans le ViewModel
                    viewModel.DeplacerCRA(craDisplay, jourCible);
                }
            }
            e.Handled = true;
        }

        private void Jour_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // Gérer la sélection du jour (équivalent au Command du Button)
            if (sender is Border border && border.Tag is JourCalendrierViewModel jour)
            {
                if (DataContext is CRACalendrierViewModel viewModel)
                {
                    viewModel.JourSelectionne = jour;
                }
            }
        }

        private void BtnValiderTousCRA_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as CRACalendrierViewModel;
            if (viewModel == null || viewModel.DevSelectionne == null) return;

            var result = MessageBox.Show(
                $"Voulez-vous vraiment valider TOUS les CRA non validés de {viewModel.DevSelectionne.Nom} ?\n\n" +
                "Cette action validera tous les CRA (passés et futurs) et les verrouillera définitivement.",
                "Confirmation validation globale",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // Récupérer les CRA avant validation pour le rapport
                    var tousLesCRA = viewModel.GetAllCRAs();
                    var craAValider = tousLesCRA.Where(c => 
                        !c.EstValide && 
                        c.DevId == viewModel.DevSelectionne.Id).ToList();

                    var nombreValidations = viewModel.ValiderTousLesCRADuDev();
                    
                    if (nombreValidations > 0)
                    {
                        // Générer le rapport
                        var (tachesRetard, tachesTemps) = viewModel.GenererRapportRespectDates(craAValider);
                        
                        // Convertir en format pour la fenêtre
                        var tachesRetardList = tachesRetard.Select(t => new RapportValidationWindow.TacheRapport 
                        { 
                            Nom = t.Nom, 
                            Detail = t.Detail 
                        }).ToList();
                        
                        var tachesTemplist = tachesTemps.Select(t => new RapportValidationWindow.TacheRapport 
                        { 
                            Nom = t.Nom, 
                            Detail = t.Detail 
                        }).ToList();
                        
                        // Afficher le rapport dans une fenêtre dédiée
                        var rapportWindow = new RapportValidationWindow(nombreValidations, tachesRetardList, tachesTemplist);
                        rapportWindow.Owner = Window.GetWindow(this);
                        rapportWindow.ShowDialog();
                    }
                    else
                    {
                        MessageBox.Show(
                            $"Aucun CRA en attente de validation pour {viewModel.DevSelectionne.Nom}.",
                            "Information",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Erreur lors de la validation : {ex.Message}",
                        "Erreur",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        private void BtnValiderCRAAValider_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as CRACalendrierViewModel;
            if (viewModel == null || viewModel.DevSelectionne == null) return;

            var result = MessageBox.Show(
                $"Voulez-vous valider les CRA en orange (à valider) de {viewModel.DevSelectionne.Nom} ?\n\n" +
                "Cette action validera uniquement les CRA prévisionnels avant aujourd'hui.",
                "Confirmation validation CRA à valider",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // Récupérer les CRA avant validation pour le rapport
                    var tousLesCRA = viewModel.GetAllCRAs();
                    var craAValider = tousLesCRA.Where(c => 
                        c.DevId == viewModel.DevSelectionne.Id && 
                        c.EstAValider).ToList();

                    var nombreValidations = viewModel.ValiderCRAAValiderDuDev();
                    
                    if (nombreValidations > 0)
                    {
                        // Générer le rapport
                        var (tachesRetard, tachesTemps) = viewModel.GenererRapportRespectDates(craAValider);
                        
                        // Convertir en format pour la fenêtre
                        var tachesRetardList = tachesRetard.Select(t => new RapportValidationWindow.TacheRapport 
                        { 
                            Nom = t.Nom, 
                            Detail = t.Detail 
                        }).ToList();
                        
                        var tachesTemplist = tachesTemps.Select(t => new RapportValidationWindow.TacheRapport 
                        { 
                            Nom = t.Nom, 
                            Detail = t.Detail 
                        }).ToList();
                        
                        // Afficher le rapport dans une fenêtre dédiée
                        var rapportWindow = new RapportValidationWindow(nombreValidations, tachesRetardList, tachesTemplist);
                        rapportWindow.Owner = Window.GetWindow(this);
                        rapportWindow.ShowDialog();
                    }
                    else
                    {
                        MessageBox.Show(
                            $"Aucun CRA à valider (orange) pour {viewModel.DevSelectionne.Nom}.",
                            "Information",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Erreur lors de la validation : {ex.Message}",
                        "Erreur",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        private void BtnAnnulerValidation_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as CRACalendrierViewModel;
            if (viewModel == null || viewModel.DevSelectionne == null) return;

            var result = MessageBox.Show(
                $"Voulez-vous annuler la validation de TOUS les CRA validés de {viewModel.DevSelectionne.Nom} ?\n\n" +
                "Cette action remettra les CRA en mode non validé (modifiables).",
                "Confirmation annulation validation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var nombreAnnulations = viewModel.AnnulerValidationCRADuDev();
                    
                    if (nombreAnnulations > 0)
                    {
                        MessageBox.Show(
                            $"✅ {nombreAnnulations} CRA de {viewModel.DevSelectionne.Nom} ont été déverrouillés !",
                            "Annulation réussie",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show(
                            $"Aucun CRA validé à annuler pour {viewModel.DevSelectionne.Nom}.",
                            "Information",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Erreur lors de l'annulation : {ex.Message}",
                        "Erreur",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }
    }
}

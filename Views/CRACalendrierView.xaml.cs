using System;
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
    }
}

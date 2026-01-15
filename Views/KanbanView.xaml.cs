using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using BacklogManager.Domain;
using BacklogManager.ViewModels;

namespace BacklogManager.Views
{
    public partial class KanbanView : UserControl
    {
        private Point _dragStartPoint;
        private Border _draggedCard;
        private KanbanItemViewModel _draggedItem;
        private bool _isDragging;

        public KanbanView()
        {
            InitializeComponent();
            
            // Initialiser les textes localisés
            InitializeLocalizedTexts();
            
            // Rafraîchir quand la vue devient visible
            this.Loaded += (s, e) =>
            {
                var viewModel = DataContext as KanbanViewModel;
                viewModel?.LoadItems();
            };
        }
        
        private void InitializeLocalizedTexts()
        {
            var ls = Services.LocalizationService.Instance;
            
            // Header
            TxtKanbanTitle.Text = ls.GetString("Kanban_Title");
            TxtRealTimeView.Text = ls.GetString("Kanban_RealTimeView");
            
            // Admin Zone
            TxtAdminZone.Text = ls.GetString("Kanban_AdminZone");
            TxtRestrictedAccess.Text = ls.GetString("Kanban_RestrictedAccess");
            TxtOnHold.Text = ls.GetString("Kanban_OnHold");
            TxtToPrioritize.Text = ls.GetString("Kanban_ToPrioritize");
            
            // Filters
            TxtFilters.Text = ls.GetString("Kanban_Filters");
            TxtTeamFilter.Text = ls.GetString("Kanban_Team");
            TxtMembersFilter.Text = ls.GetString("Kanban_Members");
            TxtProjectFilter.Text = ls.GetString("Kanban_Project");
            TxtSearchFilter.Text = ls.GetString("Kanban_Search");
            
            // Buttons
            BtnRefresh.Content = ls.GetString("Kanban_BtnRefresh");
            BtnClearFilters.Content = ls.GetString("Kanban_BtnClearFilters");
            
            // Columns
            TxtToDo.Text = ls.GetString("Kanban_ToDo");
            TxtInProgress.Text = ls.GetString("Kanban_InProgress");
            TxtInTest.Text = ls.GetString("Kanban_InTest");
            TxtDone.Text = ls.GetString("Kanban_Done");
        }

        private void KanbanCard_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                return;
            }

            var border = sender as Border;
            if (border?.DataContext is KanbanItemViewModel kanbanItem)
            {
                var viewModel = DataContext as KanbanViewModel;
                if (viewModel != null)
                {
                    viewModel.OuvrirDetailsTache(kanbanItem.Item);
                }
            }
        }

        private void KanbanCard_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(null);
            _draggedCard = sender as Border;
            _draggedItem = _draggedCard?.DataContext as KanbanItemViewModel;
            _isDragging = false;
        }

        private void KanbanCard_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && _draggedCard != null)
            {
                Point currentPosition = e.GetPosition(null);
                Vector diff = _dragStartPoint - currentPosition;

                if (Math.Abs(diff.X) > 10 || Math.Abs(diff.Y) > 10)
                {
                    _isDragging = true;
                    
                    // Effet visuel BNP Paribas pendant le drag
                    _draggedCard.Opacity = 0.7;
                    _draggedCard.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00915A"));
                    _draggedCard.BorderThickness = new Thickness(3);
                    
                    DataObject dragData = new DataObject("KanbanItem", _draggedItem);
                    DragDrop.DoDragDrop(_draggedCard, dragData, DragDropEffects.Move);
                    
                    // Restaurer l'apparence normale
                    _draggedCard.Opacity = 1.0;
                    _draggedCard = null;
                    _draggedItem = null;
                }
            }
        }

        private void KanbanColumn_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("KanbanItem"))
            {
                var droppedItem = e.Data.GetData("KanbanItem") as KanbanItemViewModel;
                var targetBorder = sender as Border;
                
                if (droppedItem != null && targetBorder != null)
                {
                    string columnName = targetBorder.Tag as string;
                    var viewModel = DataContext as KanbanViewModel;
                    
                    if (viewModel != null && columnName != null)
                    {
                        Statut newStatus = Statut.Afaire;
                        switch (columnName)
                        {
                            case "EnAttente":
                                newStatus = Statut.EnAttente;
                                break;
                            case "APrioriser":
                                newStatus = Statut.APrioriser;
                                break;
                            case "AFaire":
                                newStatus = Statut.Afaire;
                                break;
                            case "EnCours":
                                newStatus = Statut.EnCours;
                                break;
                            case "EnTest":
                                newStatus = Statut.Test;
                                break;
                            case "Termine":
                                newStatus = Statut.Termine;
                                break;
                        }
                        
                        // Changer le statut et sauvegarder
                        viewModel.ChangerStatutTache(droppedItem.Item, newStatus);
                    }
                }
                
                // Animation visuelle de succès BNP
                AnimateDropSuccess(sender as Border);
            }
        }

        private void KanbanColumn_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("KanbanItem"))
            {
                var targetBorder = sender as Border;
                if (targetBorder != null)
                {
                    // Effet de survol BNP Paribas
                    targetBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E8F5E9"));
                }
            }
        }

        private void KanbanColumn_DragLeave(object sender, DragEventArgs e)
        {
            var targetBorder = sender as Border;
            if (targetBorder != null)
            {
                // Restaurer couleur normale
                targetBorder.Background = Brushes.White;
            }
        }

        private async void AnimateDropSuccess(Border border)
        {
            if (border == null) return;
            
            var originalBackground = border.Background;
            border.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00915A"));
            
            await System.Threading.Tasks.Task.Delay(200);
            
            border.Background = originalBackground;
        }

        private void DeleteTask_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var task = button?.Tag as BacklogItem;
            
            if (task == null) return;

            // Confirmation modale
            var result = MessageBox.Show(
                string.Format("Êtes-vous sûr de vouloir supprimer la tâche ?\n\nTitre : {0}\nStatut : {1}\n\nCette action est irréversible.", 
                    task.Titre, task.Statut),
                "Confirmation de suppression",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No);

            if (result == MessageBoxResult.Yes)
            {
                var viewModel = DataContext as KanbanViewModel;
                if (viewModel != null)
                {
                    try
                    {
                        viewModel.SupprimerTache(task);
                        MessageBox.Show("Tâche supprimée avec succès.", "Suppression", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(string.Format("Erreur lors de la suppression : {0}", ex.Message), 
                            "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
    }
}


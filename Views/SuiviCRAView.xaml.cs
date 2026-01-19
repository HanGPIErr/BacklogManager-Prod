using System.Windows.Controls;
using System.Windows;
using BacklogManager.ViewModels;
using System.Linq;
using System.Windows.Input;
using BacklogManager.Services;

namespace BacklogManager.Views
{
    public partial class SuiviCRAView : UserControl
    {
        public SuiviCRAView()
        {
            InitializeComponent();
            InitializeLocalizedTexts();
        }

        private void InitializeLocalizedTexts()
        {
            var loc = LocalizationService.Instance;

            // Header
            TxtTitle.Text = loc.GetString("SuiviCRA_Title");
            TxtSubtitle.Text = loc.GetString("SuiviCRA_Subtitle");
            TxtTeam.Text = loc.GetString("SuiviCRA_Team");
            TxtMembers.Text = loc.GetString("SuiviCRA_Members");

            // Mode buttons
            BtnModeMonth.Content = loc.GetString("SuiviCRA_ModeMonth");
            BtnModeList.Content = loc.GetString("SuiviCRA_ModeList");
            BtnModeTimelineProgramme.Content = loc.GetString("SuiviCRA_ModeTimelineProgramme");
            BtnModeTimelineProjet.Content = loc.GetString("SuiviCRA_ModeTimelineProjet");
            BtnToday.Content = loc.GetString("SuiviCRA_BtnToday");

            // Programme and Projet labels
            TxtProgramme.Text = loc.GetString("SuiviCRA_Programme");
            TxtProjet.Text = loc.GetString("SuiviCRA_Projet");

            // Programme section
            TxtStart.Text = loc.GetString("SuiviCRA_Start");
            TxtTarget.Text = loc.GetString("SuiviCRA_Target");
            TxtGreen.Text = loc.GetString("SuiviCRA_Green");
            TxtAmber.Text = loc.GetString("SuiviCRA_Amber");
            TxtRed.Text = loc.GetString("SuiviCRA_Red");

            // Statistics cards
            TxtProjects.Text = loc.GetString("SuiviCRA_Projects");
            TxtTotalTasks.Text = loc.GetString("SuiviCRA_TotalTasks");
            TxtProgress.Text = loc.GetString("SuiviCRA_Progress");
            TxtEstimatedBudget.Text = loc.GetString("SuiviCRA_EstimatedBudget");
            TxtRealTime.Text = loc.GetString("SuiviCRA_RealTime");

            // Global progress
            TxtGlobalProgress.Text = loc.GetString("SuiviCRA_GlobalProgress");
            TxtTasksCompleted.Text = loc.GetString("SuiviCRA_TasksCompleted");

            // Teams section
            TxtTeamsProgramme.Text = loc.GetString("SuiviCRA_TeamsProgramme");
            RunTeamsCount.Text = loc.GetString("SuiviCRA_TeamsCount");

            // Project Timeline
            TxtDatesToCome.Text = loc.GetString("SuiviCRA_DatesToCome");
            TxtTask.Text = " " + loc.GetString("SuiviCRA_Tasks"); // "tâches" / "tasks" / "tareas"
            TxtExtensionDetected.Text = loc.GetString("SuiviCRA_ExtensionDetected");
            BtnValidateExtension.Content = loc.GetString("SuiviCRA_ValidateExtension");
            TxtStart2.Text = loc.GetString("SuiviCRA_Start2");
            TxtObjective.Text = loc.GetString("SuiviCRA_Objective");
            TxtNoTask.Text = loc.GetString("SuiviCRA_NoTask");
            TxtSelectProjectWithTasks.Text = loc.GetString("SuiviCRA_SelectProjectWithTasks");

            // Calendar day headers
            TxtMon.Text = loc.GetString("SuiviCRA_Mon");
            TxtTue.Text = loc.GetString("SuiviCRA_Tue");
            TxtWed.Text = loc.GetString("SuiviCRA_Wed");
            TxtThu.Text = loc.GetString("SuiviCRA_Thu");
            TxtFri.Text = loc.GetString("SuiviCRA_Fri");
            TxtSat.Text = loc.GetString("SuiviCRA_Sat");
            TxtSun.Text = loc.GetString("SuiviCRA_Sun");

            // Statistics panel
            TxtStatistics.Text = loc.GetString("SuiviCRA_Statistics");
            TxtDayDetail.Text = loc.GetString("SuiviCRA_DayDetail");
            TxtNoCRA.Text = loc.GetString("SuiviCRA_NoCRA");

            // Tooltips for AI buttons
            BtnAnalyzeProgrammeIA.ToolTip = loc.GetString("SuiviCRA_AnalyzeIATooltipProgramme");
            BtnAnalyzeProjetIA.ToolTip = loc.GetString("SuiviCRA_AnalyzeIATooltipProjet");
            
            // Button content
            BtnAnalyzeProgrammeIA.Content = "🤖 " + loc.GetString("SuiviCRA_AnalyzeWithIA");
            BtnAnalyzeProjetIA.Content = "🤖 " + loc.GetString("SuiviCRA_AnalyzeWithIA");
        }

        private void TimelineScroll_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (sender is ScrollViewer scrollViewer && TimelineHeaderScroll != null)
            {
                TimelineHeaderScroll.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset);
            }
        }

        private void TacheTimeline_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is TacheTimelineViewModel tache)
            {
                // Ouvrir la fenêtre d'aperçu de la tâche
                var apercuWindow = new AperçuTacheTimelineWindow(tache);
                apercuWindow.Owner = Window.GetWindow(this);
                apercuWindow.ShowDialog();
            }
        }

        private void BtnAnalyserProjetIA_Click(object sender, RoutedEventArgs e)
        {
            var loc = LocalizationService.Instance;
            var viewModel = DataContext as SuiviCRAViewModel;
            if (viewModel == null || viewModel.ProjetSelectionne == null)
            {
                MessageBox.Show(
                    loc.GetString("SuiviCRA_NoProjectSelected"),
                    loc.GetString("SuiviCRA_NoProjectSelectedTitle"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (!viewModel.TachesProjetTimeline.Any())
            {
                MessageBox.Show(
                    loc.GetString("SuiviCRA_NoTaskToAnalyze"),
                    loc.GetString("SuiviCRA_NoTaskTitle"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            // Ouvrir la fenêtre d'analyse IA
            var tachesBacklogItems = viewModel.TachesProjetTimeline
                .Select(t => t.BacklogItem)
                .ToList();
                
            var analyseWindow = new AnalyseProjetIAWindow(
                viewModel.ProjetSelectionne,
                tachesBacklogItems);
            analyseWindow.Owner = Window.GetWindow(this);
            analyseWindow.ShowDialog();
        }

        private void BtnAnalyserProgrammeIA_Click(object sender, RoutedEventArgs e)
        {
            var loc = LocalizationService.Instance;
            var viewModel = DataContext as SuiviCRAViewModel;
            if (viewModel == null || viewModel.ProgrammeSelectionne == null)
            {
                MessageBox.Show(
                    loc.GetString("SuiviCRA_NoProgrammeSelected"),
                    loc.GetString("SuiviCRA_NoProgrammeSelectedTitle"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (!viewModel.ProjetsTimelineProgramme.Any())
            {
                MessageBox.Show(
                    loc.GetString("SuiviCRA_NoProgrammeProject"),
                    loc.GetString("SuiviCRA_NoProgrammeProjectTitle"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            // Ouvrir la fenêtre d'analyse IA pour le programme
            var analyseWindow = new AnalyseProgrammeIAWindow(viewModel.ProgrammeSelectionne);
            analyseWindow.Owner = Window.GetWindow(this);
            analyseWindow.ShowDialog();
        }

        private void Equipe_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is Domain.Equipe equipe)
            {
                var mainWindow = Window.GetWindow(this) as MainWindow;
                if (mainWindow != null)
                {
                    mainWindow.NaviguerVersDetailEquipe(equipe.Id);
                }
            }
        }
    }
}


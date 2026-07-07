using System.Collections.ObjectModel;
using System.Windows;
using TokenOmamoriTool.Models;
using TokenOmamoriTool.Services;

namespace TokenOmamoriTool
{
    public partial class ToolOperationWindow : Window
    {
        private readonly ObservableCollection<OperationStep> _steps;
        private bool _running;
        private bool _allSucceeded;

        public ToolOperationWindow(string title, IReadOnlyList<OperationStep> steps)
        {
            InitializeComponent();
            Title = title;
            TitleText.Text = title;
            _steps = new ObservableCollection<OperationStep>(steps);
            StepsList.ItemsSource = _steps;
        }

        private async void RunButton_Click(object sender, RoutedEventArgs e)
        {
            if (_allSucceeded)
            {
                Close();
                return;
            }
            if (_running) return;
            _running = true;
            RunButton.IsEnabled = false;
            RunButton.Content = LocalizationService.T("ToolOp_Running");

            try
            {
                foreach (var step in _steps)
                {
                    if (step.Status == OperationStepStatus.Success) continue;

                    step.Status = OperationStepStatus.Running;
                    OperationStepResult result;
                    try
                    {
                        result = await step.Run();
                    }
                    catch (Exception ex)
                    {
                        result = OperationStepResult.Fail(LocalizationService.F("ToolOp_UnexpectedError", ex.Message));
                    }

                    step.ResultMessage = result.Message;
                    step.Status = result.Success ? OperationStepStatus.Success : OperationStepStatus.Failed;

                    if (!result.Success)
                    {
                        RunButton.Content = LocalizationService.T("ToolOp_RetryFromFailed");
                        return;
                    }
                }

                _allSucceeded = true;
                RunButton.Content = LocalizationService.T("ToolOp_Done");
                CloseButton.Visibility = Visibility.Collapsed;
            }
            finally
            {
                _running = false;
                RunButton.IsEnabled = true;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
    }
}

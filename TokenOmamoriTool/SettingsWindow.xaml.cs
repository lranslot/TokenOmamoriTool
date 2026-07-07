using System.Windows;
using TokenOmamoriTool.Models;
using TokenOmamoriTool.Services;

namespace TokenOmamoriTool
{
    public partial class SettingsWindow : Window
    {
        private readonly AppSettings _settings;
        private readonly string _settingsPath;
        private readonly Action _onSaved;

        public SettingsWindow(AppSettings settings, string settingsPath, Action onSaved)
        {
            InitializeComponent();
            _settings = settings;
            _settingsPath = settingsPath;
            _onSaved = onSaved;

            MaxLinesBox.Text = _settings.InstructionFile.MaxLines.ToString();
            MaxSizeKBBox.Text = _settings.InstructionFile.MaxSizeKB.ToString();
            SessionLogMaxMBBox.Text = (_settings.SessionLog.MaxSizeBytes / 1024.0 / 1024.0).ToString("0.##");
            PollIntervalBox.Text = _settings.PollIntervalSeconds.ToString();
            ExternalToolsIntervalBox.Text = _settings.ExternalTools.PollIntervalSeconds.ToString();
            HideOnCloseBox.IsChecked = _settings.Tray.HideOnClose;
            ToastOnWarningBox.IsChecked = _settings.Tray.ToastOnWarning;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(MaxLinesBox.Text, out var maxLines) || maxLines <= 0 ||
                !int.TryParse(MaxSizeKBBox.Text, out var maxSizeKb) || maxSizeKb <= 0 ||
                !double.TryParse(SessionLogMaxMBBox.Text, out var sessionMb) || sessionMb <= 0 ||
                !int.TryParse(PollIntervalBox.Text, out var pollSeconds) || pollSeconds <= 0 ||
                !int.TryParse(ExternalToolsIntervalBox.Text, out var externalSeconds) || externalSeconds <= 0)
            {
                ErrorText.Text = LocalizationService.T("Settings_ErrorPositiveNumbers");
                ErrorText.Visibility = Visibility.Visible;
                return;
            }

            _settings.InstructionFile.MaxLines = maxLines;
            _settings.InstructionFile.MaxSizeKB = maxSizeKb;
            _settings.SessionLog.MaxSizeBytes = (long)(sessionMb * 1024 * 1024);
            _settings.PollIntervalSeconds = pollSeconds;
            _settings.ExternalTools.PollIntervalSeconds = externalSeconds;
            _settings.Tray.HideOnClose = HideOnCloseBox.IsChecked == true;
            _settings.Tray.ToastOnWarning = ToastOnWarningBox.IsChecked == true;

            SettingsLoader.Save(_settingsPath, _settings);
            _onSaved();
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e) => Close();

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            var confirm = MessageBox.Show(this,
                LocalizationService.T("Settings_ResetConfirm_Text"),
                LocalizationService.T("Settings_ResetConfirm_Caption"), MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            _settings.InstructionFile.MaxLines = 200;
            _settings.InstructionFile.MaxSizeKB = 25;
            _settings.SessionLog.MaxSizeBytes = 5L * 1024 * 1024;
            _settings.PollIntervalSeconds = 5;
            _settings.ExternalTools.PollIntervalSeconds = 60;
            // Tray defaults (FirstHideNoticeShown is internal state, deliberately left untouched).
            _settings.Tray.HideOnClose = true;
            _settings.Tray.ToastOnWarning = true;

            MaxLinesBox.Text = _settings.InstructionFile.MaxLines.ToString();
            MaxSizeKBBox.Text = _settings.InstructionFile.MaxSizeKB.ToString();
            SessionLogMaxMBBox.Text = (_settings.SessionLog.MaxSizeBytes / 1024.0 / 1024.0).ToString("0.##");
            PollIntervalBox.Text = _settings.PollIntervalSeconds.ToString();
            ExternalToolsIntervalBox.Text = _settings.ExternalTools.PollIntervalSeconds.ToString();
            HideOnCloseBox.IsChecked = _settings.Tray.HideOnClose;
            ToastOnWarningBox.IsChecked = _settings.Tray.ToastOnWarning;
            ErrorText.Visibility = Visibility.Collapsed;

            SettingsLoader.Save(_settingsPath, _settings);
            _onSaved();
        }
    }
}

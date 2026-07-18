using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using H.NotifyIcon;
using Microsoft.Toolkit.Uwp.Notifications;
using TokenOmamoriTool.Converters;
using TokenOmamoriTool.Models;
using TokenOmamoriTool.Services;

namespace TokenOmamoriTool
{
    public partial class MainWindow : Window
    {
        private readonly AppSettings _settings;
        private readonly string _settingsPath;
        private readonly ObservableCollection<ProjectHealth> _projects = new();
        private readonly ExternalToolsHealth _externalTools = new();
        private readonly DispatcherTimer _timer;
        private readonly DispatcherTimer _externalToolsTimer;
        private DispatcherTimer? _midnightTimer;
        private bool _externalToolsRefreshing;
        // A transient rtk/ccusage failure keeps showing the day's last good line (marked stale)
        // instead of wiping it to a bare failure message.
        private readonly LastGoodDisplayCache _rtkDisplayCache = new();
        private readonly LastGoodDisplayCache _ccusageDisplayCache = new();

        private readonly WarningEdgeDetector _edgeDetector = new();
        private TaskbarIcon? _trayIcon;
        private ImageSource? _normalTrayIcon;
        private ImageSource? _warningTrayIcon;
        private bool _exitRequested;
        private string? _rtkTodaySavedText;
        private double? _rtkCumulativeTokens;
        private long? _activeDonationMilestone;
        private ProjectHealth? _worstClaudeMdProject;
        private double _claudeMdWorstFraction;
        private double _sessionLogWorstFraction;

        public MainWindow()
        {
            InitializeComponent();

            // ContextMenuOpening only fires when a ContextMenu is already assigned; this placeholder
            // gets replaced with the real, freshly-built menu each time in Window_ContextMenuOpening.
            ContextMenu = new ContextMenu();

            _settingsPath = Path.Combine(AppContext.BaseDirectory, "settings.json");
            _settings = SettingsLoader.Load(_settingsPath);

            // Resolve and persist the language before any UI string is produced (spec §11.4:
            // first run follows the OS UI culture and saves the result; invalid values → en).
            var language = LocalizationService.ResolveInitialLanguage(_settings.Language);
            if (_settings.Language != language)
            {
                _settings.Language = language;
                SettingsLoader.Save(_settingsPath, _settings);
            }
            LocalizationService.ApplyLanguage(language);
            LocalizationService.LanguageChanged += OnLanguageChanged;

            DataContext = _externalTools;
            _externalTools.RtkDisplayText = LocalizationService.T("Common_Measuring");
            _externalTools.CcusageDisplayText = LocalizationService.T("Common_Measuring");

            ProjectsList.ItemsSource = _projects;

            InitializeTrayIcon();
            // Clicking any of this app's toasts restores the window (spec §8.6). OnActivated fires
            // on a background thread, so hop to the UI thread first.
            ToastNotificationManagerCompat.OnActivated += _ => Dispatcher.BeginInvoke(ShowFromTray);

            // Anchor bottom-right (spec §8.3: 表示時は画面右下に固定). ActualWidth/Height are only
            // valid after first render, and SizeToContent=Height means the height can change on
            // every poll, so re-anchor the bottom edge whenever the size changes too.
            ContentRendered += (_, _) => PositionBottomRight();
            SizeChanged += (_, _) => { if (IsVisible) PositionBottomRight(); };

            RefreshHealth();

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(Math.Max(1, _settings.PollIntervalSeconds)),
            };
            _timer.Tick += (_, _) => RefreshHealth();
            _timer.Start();

            _externalToolsTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(Math.Max(5, _settings.ExternalTools.PollIntervalSeconds)),
            };
            _externalToolsTimer.Tick += async (_, _) => await RefreshExternalToolsAsync();
            _externalToolsTimer.Start();

            _ = RefreshExternalToolsAsync();
            ScheduleMidnightRollover();
        }

        // Belt-and-suspenders daily rollover for the RTK baseline: this one-shot timer is the
        // primary trigger for a same-day change, but ResolveBaseline() is called on every 60s poll
        // and at startup too, so a missed/late fire here (sleep, timer coalescing) is still caught.
        private void ScheduleMidnightRollover()
        {
            var now = DateTime.Now;
            var nextMidnight = now.Date.AddDays(1);

            _midnightTimer?.Stop();
            _midnightTimer = new DispatcherTimer { Interval = nextMidnight - now };
            _midnightTimer.Tick += async (_, _) =>
            {
                _midnightTimer!.Stop();
                await RefreshExternalToolsAsync();
                ScheduleMidnightRollover();
            };
            _midnightTimer.Start();
        }

        private void RefreshHealth()
        {
            _projects.Clear();
            foreach (var project in _settings.Projects)
            {
                _projects.Add(new ProjectHealth
                {
                    Name = project.Name,
                    ClaudeMd = ClaudeMdMonitor.Check(project.Path, _settings.InstructionFile),
                    SessionLog = SessionLogMonitor.Check(project.Path, _settings.SessionLog),
                });
            }

            UpdateWorstFractions();
            UpdateTrayState();
            CheckWarningEdges();
        }

        // Worst-case usage fractions across all monitored projects, shared by the context menu
        // (diet item state/colors), the tray icon state, the tooltip, and the toast edge detector.
        private void UpdateWorstFractions()
        {
            _worstClaudeMdProject = _projects.Count == 0
                ? null
                : _projects.OrderByDescending(p => Math.Max(p.ClaudeMd.LinesBar.UsageFraction, p.ClaudeMd.SizeBar.UsageFraction)).First();
            _claudeMdWorstFraction = _worstClaudeMdProject is null
                ? 0.0
                : Math.Max(_worstClaudeMdProject.ClaudeMd.LinesBar.UsageFraction, _worstClaudeMdProject.ClaudeMd.SizeBar.UsageFraction);
            _sessionLogWorstFraction = _projects.Count == 0
                ? 0.0
                : _projects.Max(p => p.SessionLog.SizeBar.UsageFraction);
        }

        private async Task RefreshExternalToolsAsync()
        {
            if (_externalToolsRefreshing) return;
            _externalToolsRefreshing = true;
            try
            {
                var today = DateOnly.FromDateTime(DateTime.Now);

                var rtk = await RtkGainRunner.RunAsync();
                _externalTools.RtkDisplayText = _rtkDisplayCache.Resolve(rtk.ParsedOk, BuildRtkDisplayText(rtk), today);
                _externalTools.RtkInstalled = rtk.Installed;

                var ccusage = await CcusageRunner.RunAsync();
                _externalTools.CcusageDisplayText = _ccusageDisplayCache.Resolve(ccusage.ParsedOk, ccusage.DisplayText, today);

                _externalTools.ClaudeMemState = await ClaudeMemStatusChecker.CheckAsync();

                UpdateTrayState();

                // §9.4: donation code is fully isolated — a failure here must never take down the
                // monitoring/polling path.
                try { UpdateDonationBanner(); } catch { }
            }
            finally
            {
                _externalToolsRefreshing = false;
            }
        }

        private string BuildRtkDisplayText(RtkGainStatus rtk)
        {
            _rtkTodaySavedText = null;
            _rtkCumulativeTokens = null;
            if (!rtk.Installed || !rtk.ParsedOk || rtk.TokensSavedText is null) return rtk.DisplayText;

            var cumulative = TokenCountFormatter.TryParse(rtk.TokensSavedText);
            if (cumulative is null) return rtk.DisplayText;
            _rtkCumulativeTokens = cumulative;

            var stored = RtkDailyBaselineTracker.Load();
            var resolved = RtkDailyBaselineTracker.ResolveBaseline(stored, cumulative.Value, DateTime.Now);
            if (stored is null || stored.Date != resolved.Date)
            {
                RtkDailyBaselineTracker.Save(resolved);
            }

            _rtkTodaySavedText = RtkDailyDisplayBuilder.BuildTodayText(rtk, resolved);
            return RtkDailyDisplayBuilder.BuildDisplayText(rtk, resolved) ?? rtk.DisplayText;
        }

        private void Window_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            ContextMenu = BuildContextMenu();
        }

        // Single builder for both the window's right-click menu and the tray icon's menu
        // (spec §8.3: identical items incl. the trailing「終了」, no duplicated
        // enable/color/submenu logic).
        private ContextMenu BuildContextMenu()
        {
            var menu = new ContextMenu();

            var dietItem = new MenuItem { Header = LocalizationService.T("Menu_CopyDietInstruction") };
            if (_claudeMdWorstFraction >= 0.7 && _worstClaudeMdProject is not null)
            {
                ApplyWarningColor(dietItem, _claudeMdWorstFraction);
                var message = BuildClaudeMdDietMessage(_worstClaudeMdProject.ClaudeMd, _claudeMdWorstFraction);
                dietItem.Click += (_, _) => Clipboard.SetText(message);
            }
            else
            {
                dietItem.IsEnabled = false;
            }
            menu.Items.Add(dietItem);

            menu.Items.Add(BuildOpenClaudeMdItem());

            var compactItem = new MenuItem { Header = LocalizationService.T("Menu_CopyCompact") };
            ApplyWarningColor(compactItem, _sessionLogWorstFraction);
            compactItem.Click += (_, _) => Clipboard.SetText("/compact");
            menu.Items.Add(compactItem);

            menu.Items.Add(new Separator());
            menu.Items.Add(BuildToolManagementItem());
            menu.Items.Add(new Separator());

            // Directly above「設定」per spec §11.3; the label is the fixed bilingual form in both
            // languages so a mistaken switch can always be undone.
            menu.Items.Add(BuildLanguageItem());

            var settingsItem = new MenuItem { Header = LocalizationService.T("Menu_Settings") };
            settingsItem.Click += (_, _) => OpenSettingsWindow();
            menu.Items.Add(settingsItem);

            var helpItem = new MenuItem { Header = LocalizationService.T("Menu_Help") };
            helpItem.Click += (_, _) => new HelpWindow { Owner = this }.Show();
            menu.Items.Add(helpItem);

            // Below「説明」per spec §9.3 — permanent, discreet donation entry.
            var supportItem = new MenuItem { Header = LocalizationService.T("Menu_SupportDev") };
            supportItem.Click += (_, _) => OpenDonatePage();
            menu.Items.Add(supportItem);

            menu.Items.Add(new Separator());
            // Full exit from either menu — _exitRequested makes OnClosing skip the hide-to-tray
            // path, so this is a real quit even with tray.hideOnClose enabled.
            var exitItem = new MenuItem { Header = LocalizationService.T("Menu_Exit") };
            exitItem.Click += (_, _) =>
            {
                _exitRequested = true;
                Close();
            };
            menu.Items.Add(exitItem);

            return menu;
        }

        private MenuItem BuildLanguageItem()
        {
            var parent = new MenuItem { Header = LocalizationService.T("Menu_Language") };

            var jaItem = new MenuItem
            {
                Header = LocalizationService.T("Menu_LanguageJapanese"),
                IsChecked = LocalizationService.CurrentLanguage == "ja",
            };
            jaItem.Click += (_, _) => SwitchLanguage("ja");
            parent.Items.Add(jaItem);

            var enItem = new MenuItem
            {
                Header = LocalizationService.T("Menu_LanguageEnglish"),
                IsChecked = LocalizationService.CurrentLanguage == "en",
            };
            enItem.Click += (_, _) => SwitchLanguage("en");
            parent.Items.Add(enItem);

            return parent;
        }

        private void SwitchLanguage(string language)
        {
            if (LocalizationService.CurrentLanguage == language) return;

            LocalizationService.ApplyLanguage(language);
            _settings.Language = language;
            SettingsLoader.Save(_settingsPath, _settings);
        }

        // --- カンパ導線 (spec §9) ---

        private void UpdateDonationBanner()
        {
            if (_activeDonationMilestone is not null) return;

            var milestone = DonationMilestones.FindUnshownMilestone(
                _rtkCumulativeTokens, _settings.Donation.ShownMilestones);
            if (milestone is null) return;

            _activeDonationMilestone = milestone;
            SetDonationBannerText();
            DonationBanner.Visibility = Visibility.Visible;
        }

        private void SetDonationBannerText()
        {
            if (_activeDonationMilestone is not long milestone) return;
            DonationBannerText.Text = LocalizationService.F(
                "Donation_BannerMessage", LocalizationService.T(MilestoneKey(milestone)));
        }

        private static string MilestoneKey(long milestone) => milestone switch
        {
            100_000 => "Donation_Milestone_100K",
            1_000_000 => "Donation_Milestone_1M",
            _ => "Donation_Milestone_10M",
        };

        private void DonationSupport_Click(object sender, RoutedEventArgs e)
        {
            OpenDonatePage();
            // Acting on the banner counts as acknowledging it — same retirement as「閉じる」.
            CloseDonationBanner();
        }

        private void DonationClose_Click(object sender, RoutedEventArgs e) => CloseDonationBanner();

        private void CloseDonationBanner()
        {
            DonationBanner.Visibility = Visibility.Collapsed;
            if (_activeDonationMilestone is long milestone)
            {
                try
                {
                    _settings.Donation.ShownMilestones =
                        DonationMilestones.MarkShown(_settings.Donation.ShownMilestones, milestone);
                    SettingsLoader.Save(_settingsPath, _settings);
                }
                catch
                {
                    // §9.4: a failed save only risks showing the banner once more; never escalate.
                }
            }
            _activeDonationMilestone = null;
        }

        private static void OpenDonatePage()
        {
            try
            {
                Process.Start(new ProcessStartInfo(DonationMilestones.DonatePageUrl) { UseShellExecute = true });
            }
            catch
            {
                // §9.4: opening the browser is best-effort only.
            }
        }

        private void OnLanguageChanged()
        {
            SetDonationBannerText();
            // Rebuild everything assembled in code: monitor bar labels come back localized from
            // the next RefreshHealth, which also refreshes the tray icon tooltip; the RTK/ccusage
            // display lines are re-derived by re-polling. Menus are already rebuilt on every open.
            RefreshHealth();
            _ = RefreshExternalToolsAsync();
        }

        private static void ApplyWarningColor(MenuItem item, double usageFraction)
        {
            if (usageFraction >= 0.7)
            {
                item.Foreground = UsageFractionToBrushConverter.BrushFor(usageFraction);
            }
        }

        private string BuildClaudeMdDietMessage(ClaudeMdStatus status, double usageFraction)
        {
            var maxLines = _settings.InstructionFile.MaxLines;
            var maxSizeKB = _settings.InstructionFile.MaxSizeKB;

            return usageFraction >= 0.9
                ? LocalizationService.F("Diet_MessageOverLimit", status.LineCount, status.SizeKB, maxLines, maxSizeKB)
                : LocalizationService.F("Diet_MessageWarning", status.LineCount, status.SizeKB);
        }

        private MenuItem BuildOpenClaudeMdItem()
        {
            if (_settings.Projects.Count <= 1)
            {
                var project = _settings.Projects.FirstOrDefault();
                var item = new MenuItem { Header = LocalizationService.T("Menu_OpenClaudeMdFolder"), IsEnabled = project is not null };
                if (project is not null)
                {
                    item.Click += (_, _) => OpenClaudeMdFolder(project);
                }
                return item;
            }

            var parent = new MenuItem { Header = LocalizationService.T("Menu_OpenClaudeMdFolder") };
            foreach (var project in _settings.Projects)
            {
                var child = new MenuItem { Header = project.Name };
                child.Click += (_, _) => OpenClaudeMdFolder(project);
                parent.Items.Add(child);
            }
            return parent;
        }

        private void OpenClaudeMdFolder(MonitoredProject project)
        {
            var path = Path.Combine(project.Path, _settings.InstructionFile.Path);
            var folder = Path.GetDirectoryName(path) ?? project.Path;
            try
            {
                Process.Start(new ProcessStartInfo(folder) { UseShellExecute = true });
            }
            catch (Exception ex) when (ex is Win32Exception or FileNotFoundException or DirectoryNotFoundException)
            {
                MessageBox.Show(this, LocalizationService.F("Error_OpenFolderFailed", ex.Message),
                    LocalizationService.T("Caption_Error"), MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private MenuItem BuildToolManagementItem()
        {
            var parent = new MenuItem { Header = LocalizationService.T("Menu_ToolManagement") };

            var rtkInstalled = _externalTools.RtkInstalled;
            var rtkItem = new MenuItem
            {
                Header = LocalizationService.T(rtkInstalled ? "Menu_UninstallRtk" : "Menu_InstallRtk"),
            };
            rtkItem.Click += (_, _) =>
            {
                if (rtkInstalled) StartRtkUninstall(); else StartRtkInstall();
            };
            parent.Items.Add(rtkItem);

            var claudeMemState = _externalTools.ClaudeMemState;
            var claudeMemItem = new MenuItem
            {
                Header = LocalizationService.T(claudeMemState switch
                {
                    ClaudeMemInstallState.Installed => "Menu_UninstallClaudeMem",
                    ClaudeMemInstallState.CliMissing => "Menu_ClaudeCliMissing",
                    _ => "Menu_InstallClaudeMem",
                }),
                IsEnabled = claudeMemState != ClaudeMemInstallState.CliMissing,
            };
            claudeMemItem.Click += (_, _) =>
            {
                if (claudeMemState == ClaudeMemInstallState.Installed) StartClaudeMemUninstall(); else StartClaudeMemInstall();
            };
            parent.Items.Add(claudeMemItem);

            return parent;
        }

        private void StartRtkInstall()
        {
            new ToolOperationWindow(LocalizationService.T("OpTitle_RtkInstall"), RtkInstaller.BuildInstallSteps()) { Owner = this }.Show();
        }

        private void StartRtkUninstall()
        {
            var confirm = MessageBox.Show(this,
                LocalizationService.T("Confirm_RtkUninstall_Text"),
                LocalizationService.T("Confirm_RtkUninstall_Caption"), MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            new ToolOperationWindow(LocalizationService.T("OpTitle_RtkUninstall"), RtkUninstaller.BuildUninstallSteps()) { Owner = this }.Show();
        }

        private void StartClaudeMemInstall()
        {
            new ToolOperationWindow(LocalizationService.T("OpTitle_ClaudeMemInstall"), ClaudeMemInstaller.BuildInstallSteps()) { Owner = this }.Show();
        }

        private void StartClaudeMemUninstall()
        {
            var confirm = MessageBox.Show(this,
                LocalizationService.T("Confirm_ClaudeMemUninstall_Text"),
                LocalizationService.T("Confirm_ClaudeMemUninstall_Caption"), MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            new ToolOperationWindow(LocalizationService.T("OpTitle_ClaudeMemUninstall"), ClaudeMemUninstaller.BuildUninstallSteps()) { Owner = this }.Show();
        }

        private void OpenSettingsWindow()
        {
            var window = new SettingsWindow(_settings, _settingsPath, onSaved: () =>
            {
                _timer.Interval = TimeSpan.FromSeconds(Math.Max(1, _settings.PollIntervalSeconds));
                _externalToolsTimer.Interval = TimeSpan.FromSeconds(Math.Max(5, _settings.ExternalTools.PollIntervalSeconds));
                RefreshHealth();
            })
            { Owner = this };
            window.ShowDialog();
        }

        // --- トレイ常駐 (spec §8) ---

        private void InitializeTrayIcon()
        {
            try
            {
                _normalTrayIcon = TrayIconFactory.Create(warning: false);
                _warningTrayIcon = TrayIconFactory.Create(warning: true);

                var trayIcon = new TaskbarIcon
                {
                    IconSource = _normalTrayIcon,
                    ToolTipText = LocalizationService.T("AppTitle"),
                };
                trayIcon.TrayLeftMouseUp += (_, _) => ToggleWindowVisibility();
                // The menu is rebuilt fresh on every open (same pattern as Window_ContextMenuOpening);
                // TaskbarIcon shows its ContextMenu on right-mouse-up, so assigning on -down is early enough.
                trayIcon.TrayRightMouseDown += (_, _) => trayIcon.ContextMenu = BuildContextMenu();
                trayIcon.ForceCreate();
                _trayIcon = trayIcon;
            }
            catch
            {
                // No tray icon → hide-on-close would strand the app with no way back, so
                // OnClosing falls back to a normal exit when _trayIcon is null.
                _trayIcon = null;
            }
        }

        private void UpdateTrayState()
        {
            // 2-state (spec §8.5): warning when either metric is at/above 70%, back to normal
            // when both drop below. No blinking/animation.
            var warning = _claudeMdWorstFraction >= WarningEdgeDetector.Threshold
                || _sessionLogWorstFraction >= WarningEdgeDetector.Threshold;

            // The in-window mini omamori (spec §12.5) mirrors the tray state so the status is
            // visible even for users who never look at the tray.
            MiniOmamoriPouch.Fill = warning ? OmamoriTheme.WarningBrush : OmamoriTheme.HealthyBrush;

            if (_trayIcon is null) return;
            _trayIcon.IconSource = warning ? _warningTrayIcon : _normalTrayIcon;
            _trayIcon.ToolTipText = BuildTrayToolTip();
        }

        // Health summary for the hover tooltip (spec §8.4), e.g.
        // 「CLAUDE.md 45% / セッション 62% / 本日節約 12.3K」. Unavailable items are omitted.
        private string BuildTrayToolTip()
        {
            var parts = new List<string>();
            if (_projects.Count > 0)
            {
                parts.Add(LocalizationService.F("Tray_TooltipClaudeMd", $"{_claudeMdWorstFraction:0%}"));
                parts.Add(LocalizationService.F("Tray_TooltipSession", $"{_sessionLogWorstFraction:0%}"));
            }
            if (_rtkTodaySavedText is not null)
            {
                parts.Add(LocalizationService.F("Tray_TooltipTodaySaved", _rtkTodaySavedText));
            }
            return parts.Count == 0 ? LocalizationService.T("AppTitle") : string.Join(" / ", parts);
        }

        private void CheckWarningEdges()
        {
            // Always feed the detector so its below/above state stays current even while
            // notifications are turned off.
            var claudeMdCrossed = _edgeDetector.Update("claudeMd", _claudeMdWorstFraction);
            var sessionLogCrossed = _edgeDetector.Update("sessionLog", _sessionLogWorstFraction);

            if (!_settings.Tray.ToastOnWarning) return;
            if (claudeMdCrossed)
            {
                ShowToast(LocalizationService.T("Toast_ClaudeMdWarning"));
            }
            if (sessionLogCrossed)
            {
                ShowToast(LocalizationService.T("Toast_SessionLogWarning"));
            }
        }

        // Notification failures must never take down the monitoring timers, so all toast calls
        // funnel through this catch-all.
        private static void ShowToast(string message)
        {
            try
            {
                new ToastContentBuilder()
                    .AddText(LocalizationService.T("AppTitle"))
                    .AddText(message)
                    .Show();
            }
            catch
            {
            }
        }

        private void ToggleWindowVisibility()
        {
            if (IsVisible)
            {
                HideToTray();
            }
            else
            {
                ShowFromTray();
            }
        }

        private void ShowFromTray()
        {
            Show();
            if (WindowState == WindowState.Minimized)
            {
                WindowState = WindowState.Normal;
            }
            Activate();
            PositionBottomRight();
        }

        private void HideToTray()
        {
            Hide();
            if (!_settings.Tray.FirstHideNoticeShown)
            {
                _settings.Tray.FirstHideNoticeShown = true;
                SettingsLoader.Save(_settingsPath, _settings);
                ShowToast(LocalizationService.T("Toast_TrayResident"));
            }
        }

        private void PositionBottomRight()
        {
            var area = SystemParameters.WorkArea;
            Left = area.Right - ActualWidth - 8;
            Top = area.Bottom - ActualHeight - 8;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            // × hides to tray instead of exiting (spec §8.2); actual exit only via the tray menu's
            //「終了」(_exitRequested) or when hide-on-close is disabled / the tray icon failed.
            if (!_exitRequested && _settings.Tray.HideOnClose && _trayIcon is not null)
            {
                e.Cancel = true;
                HideToTray();
                return;
            }
            base.OnClosing(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            _trayIcon?.Dispose();
            base.OnClosed(e);
            // ShutdownMode is OnExplicitShutdown (so Hide() keeps the app alive) — shut down
            // explicitly once the main window really closes.
            Application.Current.Shutdown();
        }
    }
}

using System.Diagnostics;
using System.Windows;
using TokenOmamoriTool.Services;

namespace TokenOmamoriTool
{
    public partial class HelpWindow : Window
    {
        public HelpWindow()
        {
            InitializeComponent();
            // The separating space lives here, not in the resource — XAML whitespace handling
            // strips a trailing space from a sys:String, so "Version: " renders as "Version:".
            VersionRun.Text = " " + AppVersionInfo.CurrentDisplayVersion();
        }

        private void DonateLink_Click(object sender, RoutedEventArgs e)
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
    }
}

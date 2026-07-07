using System.ComponentModel;
using System.Runtime.CompilerServices;
using TokenOmamoriTool.Services;

namespace TokenOmamoriTool.Models;

public class ExternalToolsHealth : INotifyPropertyChanged
{
    // Initial "measuring..." text is assigned by MainWindow after the language is applied —
    // field initializers here would run before LocalizationService is ready.
    private string _rtkDisplayText = "";
    private string _ccusageDisplayText = "";
    private bool _rtkInstalled;
    private ClaudeMemInstallState _claudeMemState = ClaudeMemInstallState.NotInstalled;

    public string RtkDisplayText
    {
        get => _rtkDisplayText;
        set => SetField(ref _rtkDisplayText, value);
    }

    public string CcusageDisplayText
    {
        get => _ccusageDisplayText;
        set => SetField(ref _ccusageDisplayText, value);
    }

    public bool RtkInstalled
    {
        get => _rtkInstalled;
        set => SetField(ref _rtkInstalled, value);
    }

    public ClaudeMemInstallState ClaudeMemState
    {
        get => _claudeMemState;
        set => SetField(ref _claudeMemState, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

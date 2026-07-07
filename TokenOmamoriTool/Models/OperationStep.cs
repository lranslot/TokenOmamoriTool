using System.ComponentModel;
using System.Runtime.CompilerServices;
using TokenOmamoriTool.Services;

namespace TokenOmamoriTool.Models;

public enum OperationStepStatus
{
    Pending,
    Running,
    Success,
    Failed,
}

public class OperationStepResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = "";

    public static OperationStepResult Ok(string message = "") => new() { Success = true, Message = message };
    public static OperationStepResult Fail(string message) => new() { Success = false, Message = message };
}

public class OperationStep : INotifyPropertyChanged
{
    private OperationStepStatus _status = OperationStepStatus.Pending;
    private string _resultMessage = "";

    public required string Description { get; init; }
    public required string CommandPreview { get; init; }
    public required Func<Task<OperationStepResult>> Run { get; init; }

    public OperationStepStatus Status
    {
        get => _status;
        set => SetField(ref _status, value);
    }

    public string ResultMessage
    {
        get => _resultMessage;
        set => SetField(ref _resultMessage, value);
    }

    public string StatusIcon => Status switch
    {
        OperationStepStatus.Pending => "・",
        OperationStepStatus.Running => LocalizationService.T("ToolOp_StatusRunning"),
        OperationStepStatus.Success => "○",
        OperationStepStatus.Failed => "×",
        _ => "",
    };

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        if (propertyName == nameof(Status))
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StatusIcon)));
        }
    }
}

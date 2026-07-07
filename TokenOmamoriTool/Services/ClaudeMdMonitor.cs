using System.IO;
using System.Linq;
using TokenOmamoriTool.Models;

namespace TokenOmamoriTool.Services;

public static class ClaudeMdMonitor
{
    public static ClaudeMdStatus Check(string projectPath, InstructionFileSettings settings)
    {
        var filePath = Path.Combine(projectPath, settings.Path);

        if (!File.Exists(filePath))
        {
            return new ClaudeMdStatus
            {
                Exists = false,
                IsOk = false,
                Message = LocalizationService.F("Monitor_ClaudeMdNotFound", settings.Path),
            };
        }

        var lineCount = File.ReadLines(filePath).Count();
        var sizeKB = new FileInfo(filePath).Length / 1024.0;
        var isOk = lineCount <= settings.MaxLines && sizeKB <= settings.MaxSizeKB;

        return new ClaudeMdStatus
        {
            Exists = true,
            IsOk = isOk,
            LineCount = lineCount,
            SizeKB = sizeKB,
            Message = isOk
                ? LocalizationService.F("Monitor_ClaudeMdOk", lineCount, sizeKB)
                : LocalizationService.F("Monitor_ClaudeMdBloated", settings.Path, lineCount, sizeKB),
            LinesBar = MetricBar.Create(
                LocalizationService.T("Bar_Lines"), lineCount, settings.MaxLines,
                LocalizationService.F("Bar_LinesValue", lineCount, settings.MaxLines)),
            SizeBar = MetricBar.Create(
                LocalizationService.T("Bar_Size"), sizeKB, settings.MaxSizeKB,
                LocalizationService.F("Bar_SizeValue", sizeKB, settings.MaxSizeKB)),
        };
    }
}

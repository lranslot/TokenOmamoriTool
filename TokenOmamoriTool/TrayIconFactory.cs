using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TokenOmamoriTool;

/// <summary>
/// Loads the two tray icon states (normal green / warning amber, spec §8.5 + §12.3 omamori shape).
/// The .ico files are generated from OmamoriShape/OmamoriTheme by tools/generate-icons.ps1 and
/// embedded as Resources — URI-backed bitmaps are one of the only two IconSource types
/// H.NotifyIcon's pipeline supports (a plain RenderTargetBitmap throws NotImplementedException
/// inside TaskbarIcon.OnIconSourceChanged, so the omamori cannot be drawn at runtime; its old
/// GeneratedIconSource path only renders text on an ellipse/rectangle, not a custom geometry).
/// </summary>
public static class TrayIconFactory
{
    public static ImageSource Create(bool warning)
    {
        var name = warning ? "omamori-warning" : "omamori";
        var icon = new BitmapImage(new Uri($"pack://application:,,,/Assets/{name}.ico"));
        icon.Freeze();
        return icon;
    }
}

using System.Windows.Media;

namespace TokenOmamoriTool;

/// <summary>
/// The omamori silhouette (spec v0.3追補2 §12.3/§12.4) — the single place the shape is defined.
/// All parts share one 24×24 design canvas. The in-window mini omamori uses the Geometry
/// properties; tools/generate-icons.ps1 extracts the *PathData constants by regex to render the
/// tray/app .ico files, so keep them as plain `const string` literals (no concatenation).
///
/// Parts (paint order: pouch → knot → loop):
/// - Pouch: the bag itself, pointed top — filled with the status color (§12.3).
/// - Loop:  the cord ring above the bag — accent color. Even-odd ring (outer/inner circle).
/// - Knot:  simplified 叶結び diamond outline inside the bag top — accent color, only drawn at
///          32px and larger (§12.3: at 16px the details are dropped).
/// </summary>
public static class OmamoriShape
{
    /// <summary>Design coordinate space: all path data below lives in a 0..24 square.</summary>
    public const double CanvasSize = 24.0;

    public const string PouchPathData =
        "M 12,5 L 18.5,10.2 L 18.5,20.2 Q 18.5,21.8 16.9,21.8 L 7.1,21.8 Q 5.5,21.8 5.5,20.2 L 5.5,10.2 Z";

    public const string LoopPathData =
        "M 9.6,3.4 A 2.4,2.4 0 1 0 14.4,3.4 A 2.4,2.4 0 1 0 9.6,3.4 Z M 10.8,3.4 A 1.2,1.2 0 1 0 13.2,3.4 A 1.2,1.2 0 1 0 10.8,3.4 Z";

    public const string KnotPathData =
        "M 12,10 L 14.2,12.2 L 12,14.4 L 9.8,12.2 Z M 12,11.2 L 13,12.2 L 12,13.2 L 11,12.2 Z";

    public static readonly Geometry PouchGeometry = Parse(PouchPathData);
    public static readonly Geometry LoopGeometry = Parse(LoopPathData);
    public static readonly Geometry KnotGeometry = Parse(KnotPathData);

    private static Geometry Parse(string data)
    {
        var geometry = Geometry.Parse(data);
        geometry.Freeze();
        return geometry;
    }
}

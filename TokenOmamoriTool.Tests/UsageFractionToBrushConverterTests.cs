using TokenOmamoriTool.Converters;

namespace TokenOmamoriTool.Tests;

public class UsageFractionToBrushConverterTests
{
    [Theory]
    [InlineData(0.0)]
    [InlineData(0.5)]
    [InlineData(0.69)]
    public void BrushFor_BelowSeventyPercent_ReturnsOk(double fraction)
    {
        Assert.Same(UsageFractionToBrushConverter.OkBrush, UsageFractionToBrushConverter.BrushFor(fraction));
    }

    [Theory]
    [InlineData(0.7)]
    [InlineData(0.8)]
    [InlineData(0.89)]
    public void BrushFor_SeventyToNinetyPercent_ReturnsWarn(double fraction)
    {
        Assert.Same(UsageFractionToBrushConverter.WarnBrush, UsageFractionToBrushConverter.BrushFor(fraction));
    }

    [Theory]
    [InlineData(0.9)]
    [InlineData(1.0)]
    [InlineData(2.0)]
    public void BrushFor_NinetyPercentOrAbove_ReturnsDanger(double fraction)
    {
        Assert.Same(UsageFractionToBrushConverter.DangerBrush, UsageFractionToBrushConverter.BrushFor(fraction));
    }
}

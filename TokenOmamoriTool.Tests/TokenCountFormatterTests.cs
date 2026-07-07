using TokenOmamoriTool.Services;

namespace TokenOmamoriTool.Tests;

public class TokenCountFormatterTests
{
    [Theory]
    [InlineData("2.2M", 2_200_000)]
    [InlineData("998K", 998_000)]
    [InlineData("1.5B", 1_500_000_000)]
    [InlineData("123", 123)]
    [InlineData("1,234", 1_234)]
    public void TryParse_ParsesSuffixedCounts(string text, double expected)
    {
        Assert.Equal(expected, TokenCountFormatter.TryParse(text));
    }

    [Fact]
    public void TryParse_UnrecognizedText_ReturnsNull()
    {
        Assert.Null(TokenCountFormatter.TryParse("n/a"));
    }

    [Theory]
    [InlineData(2_200_000, "2.2M")]
    [InlineData(456_000, "456K")]
    [InlineData(1_500_000_000, "1.5B")]
    [InlineData(123, "123")]
    [InlineData(0, "0")]
    public void Format_RoundTripsCommonMagnitudes(double value, string expected)
    {
        Assert.Equal(expected, TokenCountFormatter.Format(value));
    }
}

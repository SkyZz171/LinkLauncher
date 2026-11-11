namespace LinkLauncher.Core.Tests;

using LinkLauncher.Core.Services;
using Xunit;

public sealed class RobloxVersionParserTests
{
    [Fact]
    public void TryParse_WindowsStyleVersion_ReturnsVersion()
    {
        var version = RobloxVersionParser.TryParse("0, 747, 0, 7471481");

        Assert.Equal(new Version(0, 747, 0, 7471481), version);
    }

    [Fact]
    public void TryParse_DottedVersion_ReturnsVersion()
    {
        Assert.Equal(new Version(0, 700, 2, 12), RobloxVersionParser.TryParse("0.700.2.12"));
    }

    [Fact]
    public void TryParse_SingleComponent_FillsMinorWithZero()
    {
        Assert.Equal(new Version(5, 0), RobloxVersionParser.TryParse("5"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData("1.2.3.4.5")]
    [InlineData("-1.2")]
    public void TryParse_UnusableValue_ReturnsNull(string? rawVersion)
    {
        Assert.Null(RobloxVersionParser.TryParse(rawVersion));
    }
}

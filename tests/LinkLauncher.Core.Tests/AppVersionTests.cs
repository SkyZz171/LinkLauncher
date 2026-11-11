namespace LinkLauncher.Core.Tests;

using LinkLauncher.Core.Models;
using Xunit;

public sealed class AppVersionTests
{
    [Theory]
    [InlineData("1.2.3", 1, 2, 3, 0)]
    [InlineData("v1.2.3", 1, 2, 3, 0)]
    [InlineData(" 0.1.0.4 ", 0, 1, 0, 4)]
    [InlineData("10", 10, 0, 0, 0)]
    public void TryParse_WithReadableVersion_ReturnsComponents(string input, int major, int minor, int patch, int revision)
    {
        Assert.True(AppVersion.TryParse(input, out var version));

        Assert.Equal(new AppVersion(major, minor, patch, revision), version);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("v")]
    [InlineData("1.2.3.4.5")]
    [InlineData("1.2.x")]
    [InlineData("-1.0.0")]
    public void TryParse_WithUnreadableVersion_Fails(string? input)
    {
        Assert.False(AppVersion.TryParse(input, out _));
    }

    [Fact]
    public void CompareTo_RanksNewerVersionsHigher()
    {
        Assert.True(AppVersion.Parse("0.2.0").IsNewerThan(AppVersion.Parse("0.1.9")));
        Assert.True(AppVersion.Parse("1.0.0").IsNewerThan(AppVersion.Parse("0.9.9")));
        Assert.True(AppVersion.Parse("0.1.0.1").IsNewerThan(AppVersion.Parse("0.1.0")));
        Assert.False(AppVersion.Parse("0.1.0").IsNewerThan(AppVersion.Parse("0.1.0")));
        Assert.False(AppVersion.Parse("0.1.0").IsNewerThan(AppVersion.Parse("0.2.0")));
    }

    [Fact]
    public void ToString_OmitsZeroRevision()
    {
        Assert.Equal("0.1.0", new AppVersion(0, 1, 0, 0).ToString());
        Assert.Equal("0.1.0.2", new AppVersion(0, 1, 0, 2).ToString());
    }
}

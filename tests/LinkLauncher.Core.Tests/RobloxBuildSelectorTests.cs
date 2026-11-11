namespace LinkLauncher.Core.Tests;

using LinkLauncher.Core.Models;
using LinkLauncher.Core.Services;
using Xunit;

public sealed class RobloxBuildSelectorTests
{
    private static readonly DateTime Older = new(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Newer = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void SelectPreferred_ChoosesHighestVersion()
    {
        var candidates = new[]
        {
            new RobloxBuildCandidate("version-old", "path-old", "0, 700, 0, 1", Newer),
            new RobloxBuildCandidate("version-new", "path-new", "0, 747, 0, 1", Older)
        };

        var preferred = RobloxBuildSelector.SelectPreferred(candidates);

        Assert.NotNull(preferred);
        Assert.Equal("version-new", preferred.FolderName);
    }

    [Fact]
    public void SelectPreferred_PrefersReadableVersionOverUnknown()
    {
        var candidates = new[]
        {
            new RobloxBuildCandidate("version-unknown", "path-unknown", null, Newer),
            new RobloxBuildCandidate("version-known", "path-known", "0, 700, 0, 1", Older)
        };

        var preferred = RobloxBuildSelector.SelectPreferred(candidates);

        Assert.NotNull(preferred);
        Assert.Equal("version-known", preferred.FolderName);
    }

    [Fact]
    public void SelectPreferred_WithoutReadableVersion_UsesMostRecentWriteTime()
    {
        var candidates = new[]
        {
            new RobloxBuildCandidate("version-a", "path-a", null, Older),
            new RobloxBuildCandidate("version-b", "path-b", null, Newer)
        };

        var preferred = RobloxBuildSelector.SelectPreferred(candidates);

        Assert.NotNull(preferred);
        Assert.Equal("version-b", preferred.FolderName);
    }

    [Fact]
    public void SelectPreferred_EmptyCollection_ReturnsNull()
    {
        Assert.Null(RobloxBuildSelector.SelectPreferred([]));
    }

    [Fact]
    public void SelectPreferred_NullCollection_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => RobloxBuildSelector.SelectPreferred(null!));
    }
}

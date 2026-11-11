namespace LinkLauncher.Core.Tests;

using LinkLauncher.Core.Services;
using Xunit;

public sealed class RobloxIntegrityRulesTests
{
    private static readonly string[] CompleteEntries =
    [
        "RobloxPlayerBeta.exe",
        "RobloxPlayerBeta.dll",
        "AppSettings.xml",
        "PlatformContent"
    ];

    [Fact]
    public void Evaluate_CompleteInstallation_IsComplete()
    {
        var report = RobloxIntegrityRules.Evaluate(
            @"C:\Roblox\Versions\version-abc",
            "version-abc",
            CompleteEntries,
            DateTimeOffset.Now);

        Assert.True(report.IsComplete);
        Assert.Empty(report.MissingFiles);
        Assert.Empty(report.MissingRecommendedEntries);
        Assert.EndsWith("RobloxPlayerBeta.exe", report.ExecutablePath!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Evaluate_MissingLibrary_IsIncomplete()
    {
        var report = RobloxIntegrityRules.Evaluate(
            @"C:\Roblox\Versions\version-abc",
            "version-abc",
            ["RobloxPlayerBeta.exe"],
            DateTimeOffset.Now);

        Assert.False(report.IsComplete);
        Assert.Contains("RobloxPlayerBeta.dll", report.MissingFiles);
    }

    [Fact]
    public void Evaluate_MissingRecommendedEntries_KeepsInstallationUsable()
    {
        var report = RobloxIntegrityRules.Evaluate(
            @"C:\Roblox\Versions\version-abc",
            "version-abc",
            ["RobloxPlayerBeta.exe", "RobloxPlayerBeta.dll"],
            DateTimeOffset.Now);

        Assert.True(report.IsComplete);
        Assert.NotEmpty(report.MissingRecommendedEntries);
    }

    [Fact]
    public void Evaluate_EntryNamesAreCaseInsensitive()
    {
        var report = RobloxIntegrityRules.Evaluate(
            @"C:\Roblox\Versions\version-abc",
            "version-abc",
            ["ROBLOXPLAYERBETA.EXE", "robloxplayerbeta.dll"],
            DateTimeOffset.Now);

        Assert.True(report.IsComplete);
    }

    [Fact]
    public void Evaluate_WithoutFolder_IsIncompleteAndEmpty()
    {
        var report = RobloxIntegrityRules.Evaluate(null, null, [], DateTimeOffset.Now);

        Assert.False(report.IsComplete);
        Assert.True(report.IsEmpty);
        Assert.Null(report.ExecutablePath);
    }
}

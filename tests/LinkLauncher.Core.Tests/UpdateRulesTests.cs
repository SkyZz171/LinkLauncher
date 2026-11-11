namespace LinkLauncher.Core.Tests;

using LinkLauncher.Core.Models;
using LinkLauncher.Core.Services;
using Xunit;

public sealed class UpdateRulesTests
{
    private static readonly AppVersion Current = AppVersion.Parse("0.1.0");

    [Fact]
    public void Evaluate_WithNewerRelease_SelectsHighestVersion()
    {
        var releases = new[]
        {
            Release("0.2.0", "abc"),
            Release("0.3.1", "def"),
            Release("0.1.5", "ghi")
        };

        var result = UpdateRules.Evaluate(Current, releases);

        Assert.Equal(UpdateCheckOutcome.UpdateAvailable, result.Outcome);
        Assert.Equal(AppVersion.Parse("0.3.1"), result.Release!.Version);
    }

    [Fact]
    public void Evaluate_WithOnlyOlderReleases_ReportsUpToDate()
    {
        var result = UpdateRules.Evaluate(Current, [Release("0.1.0", "abc"), Release("0.0.9", "def")]);

        Assert.Equal(UpdateCheckOutcome.UpToDate, result.Outcome);
        Assert.Null(result.Release);
    }

    [Fact]
    public void Evaluate_WithoutRelease_ReportsUpToDate()
    {
        Assert.Equal(UpdateCheckOutcome.UpToDate, UpdateRules.Evaluate(Current, []).Outcome);
        Assert.Equal(UpdateCheckOutcome.UpToDate, UpdateRules.Evaluate(Current, null).Outcome);
    }

    [Fact]
    public void TryExtractSha256_ReadsHashFromReleaseNotes()
    {
        const string hash = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";
        var notes = $"Installeur Windows.\n\nSHA-256 : {hash}\n";

        Assert.Equal(hash, UpdateRules.TryExtractSha256(notes));
        Assert.Equal(hash, UpdateRules.TryExtractSha256($"sha256:{hash.ToUpperInvariant()}"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Aucune empreinte ici.")]
    [InlineData("0123456789abcdef")]
    public void TryExtractSha256_WithoutHash_ReturnsNull(string? text)
    {
        Assert.Null(UpdateRules.TryExtractSha256(text));
    }

    [Fact]
    public void IsSha256_ValidatesLengthAndAlphabet()
    {
        Assert.True(UpdateRules.IsSha256(new string('a', 64)));
        Assert.False(UpdateRules.IsSha256(new string('a', 63)));
        Assert.False(UpdateRules.IsSha256(new string('z', 64)));
        Assert.False(UpdateRules.IsSha256(null));
    }

    private static UpdateRelease Release(string version, string hash) => new()
    {
        Version = AppVersion.Parse(version),
        Tag = $"v{version}",
        DownloadUrl = new Uri($"https://example.invalid/LinkLauncher-{version}-setup.exe"),
        Sha256 = hash
    };
}

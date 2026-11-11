namespace LinkLauncher.Integration.Tests;

using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LinkLauncher.Core.Abstractions;
using LinkLauncher.Core.Models;
using LinkLauncher.Infrastructure.Windows;
using LinkLauncher.Updater;
using Xunit;

public sealed class UpdatePipelineTests : IDisposable
{
    private const string Hash = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

    private readonly string _root = Path.Combine(Path.GetTempPath(), "linklauncher-tests", Guid.NewGuid().ToString("N"));
    private readonly CapturingLogger _logger = new();
    private readonly StubVersionProvider _version = new();

    [Fact]
    public void Parse_WithPublishedReleases_KeepsOnlyInstallableOnes()
    {
        using var document = JsonDocument.Parse(ReleasesJson);
        var releases = GitHubReleasesUpdateFeed.Parse(document.RootElement);

        var release = Assert.Single(releases);
        Assert.Equal(AppVersion.Parse("0.2.0"), release.Version);
        Assert.Equal(Hash, release.Sha256);
        Assert.EndsWith(".exe", release.DownloadUrl.AbsolutePath, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Checker_WithNewerRelease_ReportsAvailableVersion()
    {
        var feed = new StubUpdateFeed
        {
            Releases =
            [
                new UpdateRelease
                {
                    Version = AppVersion.Parse("0.2.0"),
                    Tag = "v0.2.0",
                    DownloadUrl = new Uri("https://example.invalid/LinkLauncher-0.2.0-setup.exe"),
                    Sha256 = Hash
                }
            ]
        };

        var checker = new UpdateChecker(feed, _version, _logger);
        var result = await checker.CheckAsync();

        Assert.Equal(UpdateCheckOutcome.UpdateAvailable, result.Outcome);
        Assert.Equal(AppVersion.Parse("0.2.0"), result.Release!.Version);
    }

    [Fact]
    public async Task Checker_WhenFeedFails_ReportsUnavailableWithoutThrowing()
    {
        var feed = new StubUpdateFeed { Failure = new HttpRequestException("hors ligne") };
        var checker = new UpdateChecker(feed, _version, _logger);

        var result = await checker.CheckAsync();

        Assert.Equal(UpdateCheckOutcome.FeedUnavailable, result.Outcome);
        Assert.Contains(_logger.Entries, entry => entry.Level == AppLogLevel.Warning);
    }

    [Fact]
    public async Task Downloader_WithMatchingChecksum_StoresVerifiedPackage()
    {
        var payload = Encoding.UTF8.GetBytes("contenu de l'installeur");
        var downloader = CreateDownloader(payload, HttpStatusCode.OK, HashFor(payload));

        var result = await downloader.DownloadAsync(Release(HashFor(payload)));

        Assert.True(result.Succeeded, result.Error);
        Assert.True(File.Exists(result.FilePath));
        Assert.Equal(payload, await File.ReadAllBytesAsync(result.FilePath!));
    }

    [Fact]
    public async Task Downloader_WithWrongChecksum_DeletesPackage()
    {
        var payload = Encoding.UTF8.GetBytes("contenu compromis");
        var downloader = CreateDownloader(payload, HttpStatusCode.OK, Hash);

        var result = await downloader.DownloadAsync(Release(Hash));

        Assert.False(result.Succeeded);
        Assert.Contains("empreinte", result.Error!, StringComparison.OrdinalIgnoreCase);
        Assert.False(File.Exists(Path.Combine(_root, "updates", "0.2.0", "LinkLauncher-0.2.0-setup.exe")));
    }

    [Fact]
    public async Task Downloader_WithoutPublishedChecksum_RefusesInstallation()
    {
        var downloader = CreateDownloader([], HttpStatusCode.OK, expectedHash: null);

        var result = await downloader.DownloadAsync(Release(null));

        Assert.False(result.Succeeded);
        Assert.Contains("SHA-256", result.Error!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Downloader_WithHttpFailure_ReportsError()
    {
        var downloader = CreateDownloader([], HttpStatusCode.NotFound, Hash);

        var result = await downloader.DownloadAsync(Release(Hash));

        Assert.False(result.Succeeded);
        Assert.Contains("404", result.Error!, StringComparison.Ordinal);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, recursive: true);
            }
        }
        catch (IOException)
        {
            // Le nettoyage reste best-effort.
        }
    }

    private UpdateDownloader CreateDownloader(byte[] payload, HttpStatusCode status, string? expectedHash)
    {
        _ = expectedHash;
        var handler = new FakeHttpHandler { Payload = payload, StatusCode = status };
        var paths = new WindowsAppPaths(Path.Combine(_root, "appdata"), _root);

        return new UpdateDownloader(new HttpClient(handler), paths, _logger);
    }

    private static UpdateRelease Release(string? hash) => new()
    {
        Version = AppVersion.Parse("0.2.0"),
        Tag = "v0.2.0",
        DownloadUrl = new Uri("https://example.invalid/LinkLauncher-0.2.0-setup.exe"),
        Sha256 = hash
    };

    private static string HashFor(byte[] payload) => Convert.ToHexStringLower(SHA256.HashData(payload));

    private const string ReleasesJson = """
[
  {
    "tag_name": "v0.2.0",
    "draft": false,
    "body": "Installeur Windows.\n\nSHA-256: 0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef",
    "assets": [
      {
        "name": "LinkLauncher-0.2.0-setup.exe",
        "browser_download_url": "https://example.invalid/LinkLauncher-0.2.0-setup.exe"
      }
    ]
  },
  {
    "tag_name": "v0.3.0",
    "draft": true,
    "body": "Brouillon",
    "assets": [
      {
        "name": "LinkLauncher-0.3.0-setup.exe",
        "browser_download_url": "https://example.invalid/LinkLauncher-0.3.0-setup.exe"
      }
    ]
  },
  {
    "tag_name": "v0.4.0",
    "draft": false,
    "body": "Publication sans installeur",
    "assets": [
      {
        "name": "source.zip",
        "browser_download_url": "https://example.invalid/source.zip"
      }
    ]
  }
]
""";
}

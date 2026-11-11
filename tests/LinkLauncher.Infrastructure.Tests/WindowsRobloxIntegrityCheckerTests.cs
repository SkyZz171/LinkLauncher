namespace LinkLauncher.Infrastructure.Tests;

using LinkLauncher.Core.Models;
using LinkLauncher.Infrastructure.FileSystem;
using LinkLauncher.Infrastructure.Roblox;
using Xunit;

public sealed class WindowsRobloxIntegrityCheckerTests : IDisposable
{
    private readonly string _root;
    private readonly string _versionFolder;
    private readonly CapturingLogger _logger = new();

    public WindowsRobloxIntegrityCheckerTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "linklauncher-tests", Guid.NewGuid().ToString("N"));
        _versionFolder = Path.Combine(_root, "version-abc");
    }

    [Fact]
    public async Task CheckAsync_WithAllRequiredFiles_ReportsComplete()
    {
        CreateEntries("RobloxPlayerBeta.exe", "RobloxPlayerBeta.dll", "AppSettings.xml");

        var report = await CreateChecker().CheckAsync(Installation());

        Assert.True(report.IsComplete);
        Assert.Empty(report.MissingFiles);
        Assert.Equal("version-abc", report.VersionFolderName);
    }

    [Fact]
    public async Task CheckAsync_WithoutLibrary_ReportsMissingFile()
    {
        CreateEntries("RobloxPlayerBeta.exe");

        var report = await CreateChecker().CheckAsync(Installation());

        Assert.False(report.IsComplete);
        Assert.Contains("RobloxPlayerBeta.dll", report.MissingFiles);
    }

    [Fact]
    public async Task CheckAsync_CountsDirectoriesAsEntries()
    {
        Directory.CreateDirectory(Path.Combine(_versionFolder, "PlatformContent"));
        CreateEntries("RobloxPlayerBeta.exe", "RobloxPlayerBeta.dll", "AppSettings.xml");

        var report = await CreateChecker().CheckAsync(Installation());

        Assert.True(report.IsComplete);
        Assert.Empty(report.MissingRecommendedEntries);
    }

    [Fact]
    public async Task CheckAsync_WithoutDetectedInstallation_ReportsNotComplete()
    {
        var report = await CreateChecker().CheckAsync(new RobloxInstallation
        {
            State = RobloxInstallationState.NotInstalled
        });

        Assert.False(report.IsComplete);
        Assert.True(report.IsEmpty);
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
            // Un nettoyage incomplet ne doit pas faire échouer les tests.
        }
    }

    private WindowsRobloxIntegrityChecker CreateChecker() =>
        new(new SystemFileSystem(), _logger);

    private RobloxInstallation Installation() => new()
    {
        State = RobloxInstallationState.Installed,
        PlayerExecutablePath = Path.Combine(_versionFolder, "RobloxPlayerBeta.exe"),
        VersionFolderName = "version-abc"
    };

    private void CreateEntries(params string[] names)
    {
        Directory.CreateDirectory(_versionFolder);

        foreach (var name in names)
        {
            File.WriteAllText(Path.Combine(_versionFolder, name), string.Empty);
        }
    }
}

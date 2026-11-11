namespace LinkLauncher.Infrastructure.Tests;

using LinkLauncher.Core.Models;
using LinkLauncher.Infrastructure.Diagnostics;
using LinkLauncher.Infrastructure.FileSystem;
using LinkLauncher.Infrastructure.Windows;
using Xunit;

public sealed class WindowsDiagnosticsProviderTests : IDisposable
{
    private readonly string _root;
    private readonly WindowsAppPaths _paths;
    private readonly CapturingLogger _logger = new();
    private readonly StubRobloxProbe _probe = new();
    private readonly StubIntegrityChecker _integrity = new();
    private readonly StubVersionProvider _version = new();

    public WindowsDiagnosticsProviderTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "linklauncher-tests", Guid.NewGuid().ToString("N"));
        _paths = new WindowsAppPaths(Path.Combine(_root, "appdata"), Path.Combine(_root, "local"));
    }

    [Fact]
    public async Task CreateAsync_WithInstalledClient_ReportsVersionsAndPaths()
    {
        _probe.Installation = new RobloxInstallation
        {
            State = RobloxInstallationState.Installed,
            PlayerVersion = "0.747.0.7471481",
            VersionFolderName = "version-abc",
            PlayerExecutablePath = @"C:\Roblox\Versions\version-abc\RobloxPlayerBeta.exe"
        };

        var report = await CreateProvider().CreateAsync();

        Assert.Equal("0.0.0-test", report.LauncherVersion);
        Assert.Equal("0.747.0.7471481", report.RobloxVersion);
        Assert.Equal("version-abc", report.RobloxVersionFolder);
        Assert.Equal("Complète", report.RobloxIntegrity);
        Assert.Equal(_paths.SettingsFilePath, report.SettingsFilePath);
        Assert.Equal(_paths.LibraryFilePath, report.LibraryFilePath);
        Assert.False(string.IsNullOrWhiteSpace(report.OperatingSystem));
        Assert.False(string.IsNullOrWhiteSpace(report.Runtime));
    }

    [Fact]
    public async Task CreateAsync_WithoutInstallation_ReportsPlaceholders()
    {
        _probe.Installation = new RobloxInstallation { State = RobloxInstallationState.NotInstalled };

        var report = await CreateProvider().CreateAsync();

        Assert.Equal("—", report.RobloxVersion);
        Assert.Equal("—", report.RobloxPath);
        Assert.Equal("Non vérifiée", report.RobloxIntegrity);
    }

    [Fact]
    public async Task CreateAsync_WithIncompleteInstallation_ListsMissingFiles()
    {
        _probe.Installation = new RobloxInstallation
        {
            State = RobloxInstallationState.Installed,
            PlayerExecutablePath = @"C:\Roblox\Versions\version-abc\RobloxPlayerBeta.exe"
        };
        _integrity.Report = new RobloxIntegrityReport
        {
            IsComplete = false,
            MissingFiles = ["RobloxPlayerBeta.dll"]
        };

        var report = await CreateProvider().CreateAsync();

        Assert.Contains("RobloxPlayerBeta.dll", report.RobloxIntegrity, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreateAsync_FindsLatestLogFile()
    {
        Directory.CreateDirectory(_paths.LogsDirectory);
        var older = Path.Combine(_paths.LogsDirectory, "linklauncher-20261110.log");
        var newer = Path.Combine(_paths.LogsDirectory, "linklauncher-20261111.log");
        await File.WriteAllTextAsync(older, "ancien");
        await File.WriteAllTextAsync(newer, "recent");
        File.SetLastWriteTimeUtc(older, DateTime.UtcNow.AddDays(-1));
        File.SetLastWriteTimeUtc(newer, DateTime.UtcNow);

        var report = await CreateProvider().CreateAsync();

        Assert.Equal(newer, report.LatestLogFilePath);
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

    private WindowsDiagnosticsProvider CreateProvider() =>
        new(_paths, _version, _probe, _integrity, new SystemFileSystem(), _logger);
}

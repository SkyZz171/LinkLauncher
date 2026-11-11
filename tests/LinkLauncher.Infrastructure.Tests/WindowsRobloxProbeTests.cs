namespace LinkLauncher.Infrastructure.Tests;

using LinkLauncher.Core.Models;
using LinkLauncher.Infrastructure.FileSystem;
using LinkLauncher.Infrastructure.Roblox;
using Xunit;

public sealed class WindowsRobloxProbeTests : IDisposable
{
    private readonly string _root;
    private readonly CapturingLogger _logger = new();
    private readonly StubVersionReader _versionReader = new();
    private readonly StubProtocolReader _protocolReader = new();

    public WindowsRobloxProbeTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "linklauncher-tests", Guid.NewGuid().ToString("N"), "Roblox");
    }

    [Fact]
    public async Task ProbeAsync_WhenRobloxRootIsMissing_ReportsNotInstalled()
    {
        var installation = await CreateProbe().ProbeAsync();

        Assert.Equal(RobloxInstallationState.NotInstalled, installation.State);
        Assert.False(installation.IsUsable);
        Assert.Null(installation.PlayerExecutablePath);
    }

    [Fact]
    public async Task ProbeAsync_WhenVersionsFolderIsMissing_ReportsBroken()
    {
        Directory.CreateDirectory(_root);

        var installation = await CreateProbe().ProbeAsync();

        Assert.Equal(RobloxInstallationState.Broken, installation.State);
        Assert.NotNull(installation.Issue);
    }

    [Fact]
    public async Task ProbeAsync_WhenOnlyStudioIsPresent_ReportsBroken()
    {
        var studioDirectory = Path.Combine(_root, "Versions", "version-studio");
        Directory.CreateDirectory(studioDirectory);
        await File.WriteAllTextAsync(Path.Combine(studioDirectory, "RobloxStudioBeta.exe"), string.Empty);

        var installation = await CreateProbe().ProbeAsync();

        Assert.Equal(RobloxInstallationState.Broken, installation.State);
    }

    [Fact]
    public async Task ProbeAsync_WithRegisteredPlayer_ReportsInstalled()
    {
        var playerPath = CreatePlayerBuild("version-abc", "0, 747, 0, 7471481");
        _protocolReader.Command = $"\"{playerPath}\" %1";

        var installation = await CreateProbe().ProbeAsync();

        Assert.Equal(RobloxInstallationState.Installed, installation.State);
        Assert.True(installation.IsUsable);
        Assert.Equal("0.747.0.7471481", installation.PlayerVersion);
        Assert.Equal("version-abc", installation.VersionFolderName);
        Assert.Equal(playerPath, installation.PlayerExecutablePath);
        Assert.Null(installation.Issue);
    }

    [Fact]
    public async Task ProbeAsync_WithUnregisteredProtocol_ReportsWarning()
    {
        CreatePlayerBuild("version-abc", "0, 747, 0, 1");
        _protocolReader.Command = null;

        var installation = await CreateProbe().ProbeAsync();

        Assert.Equal(RobloxInstallationState.Installed, installation.State);
        Assert.True(installation.IsUsable);
        Assert.NotNull(installation.Issue);
    }

    [Fact]
    public async Task ProbeAsync_WhenProtocolTargetsAnotherBuild_ReportsWarning()
    {
        CreatePlayerBuild("version-abc", "0, 747, 0, 1");
        _protocolReader.Command = "\"C:\\Ailleurs\\RobloxPlayerBeta.exe\" %1";

        var installation = await CreateProbe().ProbeAsync();

        Assert.True(installation.IsUsable);
        Assert.NotNull(installation.Issue);
    }

    [Fact]
    public async Task ProbeAsync_ChoosesMostRecentBuild()
    {
        CreatePlayerBuild("version-old", "0, 740, 0, 1");
        var newestPath = CreatePlayerBuild("version-new", "0, 747, 0, 1");
        _protocolReader.Command = $"\"{newestPath}\" %1";

        var installation = await CreateProbe().ProbeAsync();

        Assert.Equal("version-new", installation.VersionFolderName);
    }

    [Fact]
    public async Task ProbeAsync_WithoutVersionMetadata_KeepsFolderAndLeavesVersionEmpty()
    {
        CreatePlayerBuild("version-abc", null);

        var installation = await CreateProbe().ProbeAsync();

        Assert.Equal(RobloxInstallationState.Installed, installation.State);
        Assert.Null(installation.PlayerVersion);
    }

    public void Dispose()
    {
        var root = Directory.GetParent(_root)?.FullName;

        try
        {
            if (root is not null && Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
        catch (IOException)
        {
            // Un nettoyage incomplet ne doit pas faire échouer les tests.
        }
    }

    private WindowsRobloxProbe CreateProbe() =>
        new(new SystemFileSystem(), _versionReader, _protocolReader, _logger, _root);

    private string CreatePlayerBuild(string folderName, string? version)
    {
        var directory = Path.Combine(_root, "Versions", folderName);
        Directory.CreateDirectory(directory);

        var executablePath = Path.Combine(directory, "RobloxPlayerBeta.exe");
        File.WriteAllText(executablePath, string.Empty);
        _versionReader.With(executablePath, version);

        return executablePath;
    }
}

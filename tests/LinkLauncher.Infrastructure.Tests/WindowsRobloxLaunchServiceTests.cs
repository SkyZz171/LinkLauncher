namespace LinkLauncher.Infrastructure.Tests;

using LinkLauncher.Core.Abstractions;
using LinkLauncher.Core.Models;
using LinkLauncher.Core.Services;
using LinkLauncher.Infrastructure.Roblox;
using Xunit;

public sealed class WindowsRobloxLaunchServiceTests
{
    private readonly CapturingLogger _logger = new();
    private readonly StubRobloxProbe _probe = new();
    private readonly FakeProcessSource _processes = new();
    private readonly StubProtocolReader _protocol = new();
    private readonly FakeShellLauncher _shell = new();

    [Fact]
    public async Task LaunchClientAsync_WhenClientAlreadyRuns_DoesNotRequestLaunch()
    {
        _processes.Running.Add(new RobloxProcessInfo { ProcessId = 42 });

        var result = await CreateService().LaunchClientAsync();

        Assert.Equal(RobloxLaunchOutcome.AlreadyRunning, result.Outcome);
        Assert.Empty(_shell.Targets);
    }

    [Fact]
    public async Task LaunchClientAsync_WhenInstallationIsMissing_ReportsNotUsable()
    {
        _probe.Installation = new RobloxInstallation { State = RobloxInstallationState.NotInstalled };

        var result = await CreateService().LaunchClientAsync();

        Assert.Equal(RobloxLaunchOutcome.InstallationNotUsable, result.Outcome);
        Assert.Empty(_shell.Targets);
    }

    [Fact]
    public async Task LaunchClientAsync_WhenProtocolIsNotRegistered_ReportsProtocolNotRegistered()
    {
        _probe.Installation = InstalledInstallation();
        _protocol.Command = null;

        var result = await CreateService().LaunchClientAsync();

        Assert.Equal(RobloxLaunchOutcome.ProtocolNotRegistered, result.Outcome);
        Assert.Empty(_shell.Targets);
    }

    [Fact]
    public async Task LaunchClientAsync_WithUsableInstallation_UsesOfficialAppUri()
    {
        _probe.Installation = InstalledInstallation();
        _protocol.Command = "\"C:\\Roblox\\RobloxPlayerBeta.exe\" %1";

        var result = await CreateService().LaunchClientAsync();

        Assert.Equal(RobloxLaunchOutcome.Launched, result.Outcome);
        Assert.True(result.IsSuccess);

        var target = Assert.Single(_shell.Targets);
        Assert.StartsWith(RobloxLaunchUriBuilder.ClientLaunchUri, target, StringComparison.Ordinal);
        Assert.Contains("launchmode:app", target, StringComparison.Ordinal);
        Assert.DoesNotContain("gameinfo", target, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LaunchClientAsync_WhenShellFails_ReportsShellFailureWithDetail()
    {
        _probe.Installation = InstalledInstallation();
        _protocol.Command = "\"C:\\Roblox\\RobloxPlayerBeta.exe\" %1";
        _shell.Result = new ShellLaunchResult(false, "Accès refusé.");

        var result = await CreateService().LaunchClientAsync();

        Assert.Equal(RobloxLaunchOutcome.ShellFailure, result.Outcome);
        Assert.Equal("Accès refusé.", result.TechnicalDetail);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void OpenOfficialDownloadPage_UsesOfficialAddress()
    {
        var result = CreateService().OpenOfficialDownloadPage();

        Assert.True(result.IsSuccess);
        Assert.Equal("https://www.roblox.com/download", Assert.Single(_shell.Targets));
    }

    private WindowsRobloxLaunchService CreateService() =>
        new(_probe, _processes, _protocol, _shell, _logger);

    private static RobloxInstallation InstalledInstallation() => new()
    {
        State = RobloxInstallationState.Installed,
        PlayerExecutablePath = @"C:\Roblox\Versions\version-abc\RobloxPlayerBeta.exe",
        VersionFolderName = "version-abc",
        PlayerVersion = "0.747.0.1"
    };
}

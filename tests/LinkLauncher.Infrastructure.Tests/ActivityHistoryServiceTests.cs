namespace LinkLauncher.Infrastructure.Tests;

using LinkLauncher.Core.Abstractions;
using LinkLauncher.Core.Models;
using LinkLauncher.Infrastructure.Activity;
using LinkLauncher.Infrastructure.FileSystem;
using LinkLauncher.Infrastructure.Windows;
using Xunit;

public sealed class ActivityHistoryServiceTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "linklauncher-tests", Guid.NewGuid().ToString("N"));
    private readonly CapturingLogger _logger = new();

    [Fact]
    public async Task Sessions_ArePersistedAndReloaded()
    {
        var paths = CreatePaths();
        var service = CreateService(paths);
        await service.InitializeAsync();

        var changes = 0;
        service.Changed += (_, _) => changes++;

        var startedAt = new DateTimeOffset(2026, 11, 11, 10, 0, 0, TimeSpan.FromHours(1));
        await service.StartSessionAsync(Process(321, startedAt), null, "Anglais");
        await service.EndSessionAsync(Process(321, startedAt, exitCode: 0));

        Assert.Equal(2, changes);

        var reloaded = CreateService(paths);
        await reloaded.InitializeAsync();

        var session = Assert.Single(reloaded.Current.Sessions);
        Assert.Equal(321, session.ProcessId);
        Assert.Equal("Anglais", session.ProfileName);
        Assert.Equal(0, session.ExitCode);
        Assert.False(session.IsOpen);
        Assert.False(session.Interrupted);
    }

    [Fact]
    public async Task Initialize_WithLeftoverOpenSession_MarksItInterrupted()
    {
        var paths = CreatePaths();
        var store = new JsonActivityHistoryStore(new SystemFileSystem(), paths, _logger);
        await store.SaveAsync(new ActivityHistory
        {
            Sessions = [new PlaySession { StartedAt = DateTimeOffset.Now.AddHours(-2), ProcessId = 55 }]
        });

        var service = CreateService(paths);
        await service.InitializeAsync();

        var session = Assert.Single(service.Current.Sessions);
        Assert.True(session.Interrupted);
        Assert.False(session.IsOpen);
    }

    [Fact]
    public async Task Clear_RemovesEverySession()
    {
        var paths = CreatePaths();
        var service = CreateService(paths);
        await service.InitializeAsync();
        await service.StartSessionAsync(Process(1, DateTimeOffset.Now), null, null);

        await service.ClearAsync();

        Assert.Empty(service.Current.Sessions);

        var reloaded = CreateService(paths);
        await reloaded.InitializeAsync();
        Assert.Empty(reloaded.Current.Sessions);
    }

    [Fact]
    public async Task Initialize_WithCorruptedFile_StartsEmptyAndKeepsFile()
    {
        var paths = CreatePaths();
        Directory.CreateDirectory(paths.ApplicationDataDirectory);
        await File.WriteAllTextAsync(
            Path.Combine(paths.ApplicationDataDirectory, "activity.json"),
            "{ ceci n'est pas du json");

        var service = CreateService(paths);
        await service.InitializeAsync();

        Assert.Empty(service.Current.Sessions);
        Assert.Contains(
            Directory.GetFiles(paths.ApplicationDataDirectory),
            file => file.Contains("activity.invalide-", StringComparison.Ordinal));
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

    private WindowsAppPaths CreatePaths() =>
        new(Path.Combine(_root, "appdata"), Path.Combine(_root, "local"));

    private ActivityHistoryService CreateService(WindowsAppPaths paths) =>
        new(new JsonActivityHistoryStore(new SystemFileSystem(), paths, _logger), _logger);

    private static RobloxProcessInfo Process(int processId, DateTimeOffset startedAt, int? exitCode = null) =>
        new() { ProcessId = processId, StartedAt = startedAt, ExitCode = exitCode };
}

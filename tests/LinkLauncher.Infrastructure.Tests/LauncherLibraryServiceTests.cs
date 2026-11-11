namespace LinkLauncher.Infrastructure.Tests;

using LinkLauncher.Core.Abstractions;
using LinkLauncher.Core.Models;
using LinkLauncher.Core.Services;
using LinkLauncher.Infrastructure.FileSystem;
using LinkLauncher.Infrastructure.Library;
using LinkLauncher.Infrastructure.Windows;
using Xunit;

public sealed class LauncherLibraryServiceTests : IDisposable
{
    private readonly string _root;
    private readonly WindowsAppPaths _paths;
    private readonly CapturingLogger _logger = new();
    private readonly JsonLibraryStore _store;

    public LauncherLibraryServiceTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "linklauncher-tests", Guid.NewGuid().ToString("N"));
        _paths = new WindowsAppPaths(Path.Combine(_root, "appdata"), Path.Combine(_root, "local"));
        _store = new JsonLibraryStore(new SystemFileSystem(), _paths, _logger);
    }

    [Fact]
    public async Task InitializeAsync_LoadsLibraryAndMarksReady()
    {
        var service = new LauncherLibraryService(_store, _logger);

        await service.InitializeAsync();

        Assert.True(service.IsLoaded);
        Assert.Single(service.Current.Profiles);
        Assert.NotNull(service.GetActiveProfile());
    }

    [Fact]
    public async Task AddProfileAsync_PersistsAndRaisesChanged()
    {
        var service = new LauncherLibraryService(_store, _logger);
        await service.InitializeAsync();
        var raised = 0;
        service.Changed += (_, _) => raised++;

        var result = await service.AddProfileAsync("Anglais", "en_US");

        Assert.True(result.Succeeded);
        Assert.Equal(2, service.Current.Profiles.Count);
        Assert.Equal(1, raised);

        var reloaded = await _store.LoadAsync();
        Assert.Equal(2, reloaded.Profiles.Count);
    }

    [Fact]
    public async Task AddProfileAsync_WhenRejected_DoesNotPersistNorRaiseChanged()
    {
        var service = new LauncherLibraryService(_store, _logger);
        await service.InitializeAsync();
        var raised = 0;
        service.Changed += (_, _) => raised++;

        var result = await service.AddProfileAsync("Par défaut", null);

        Assert.False(result.Succeeded);
        Assert.NotNull(result.Error);
        Assert.Equal(0, raised);
        Assert.Single(service.Current.Profiles);
        Assert.False(File.Exists(_paths.LibraryFilePath));
    }

    [Fact]
    public async Task SetActiveProfileAsync_ChangesProfileUsedForLaunch()
    {
        var service = new LauncherLibraryService(_store, _logger);
        await service.InitializeAsync();
        var added = await service.AddProfileAsync("Anglais", "en_US");

        var result = await service.SetActiveProfileAsync(added.Library.Profiles[1].Id);

        Assert.True(result.Succeeded);
        Assert.Equal("en_US", service.GetActiveProfile()!.Locale);
    }

    [Fact]
    public async Task AddExperienceAsync_StoresOfficialPage()
    {
        var service = new LauncherLibraryService(_store, _logger);
        await service.InitializeAsync();

        var result = await service.AddExperienceAsync("Adopt Me", "https://www.roblox.com/games/920587237/Adopt-Me");

        Assert.True(result.Succeeded);
        var experience = Assert.Single(service.Current.Experiences);
        Assert.Equal("https://www.roblox.com/games/920587237", experience.Url);
    }

    [Fact]
    public async Task InitializeAsync_WhenAlreadyLoaded_DoesNotReload()
    {
        var service = new LauncherLibraryService(_store, _logger);
        await service.InitializeAsync();
        await service.AddProfileAsync("Anglais", null);

        await service.InitializeAsync();

        Assert.Equal(2, service.Current.Profiles.Count);
    }

    [Fact]
    public async Task InitializeAsync_WhenStoreFails_UsesDefaultLibrary()
    {
        var service = new LauncherLibraryService(new ThrowingLibraryStore(), _logger);

        await service.InitializeAsync();

        Assert.True(service.IsLoaded);
        Assert.Single(service.Current.Profiles);
        Assert.Contains(_logger.Entries, entry => entry.Level == AppLogLevel.Error);
    }

    [Fact]
    public async Task AddExperienceAsync_WhenStoreFails_PropagatesAndKeepsCurrent()
    {
        var service = new LauncherLibraryService(new ThrowingLibraryStore(), _logger);
        await service.InitializeAsync();

        await Assert.ThrowsAsync<IOException>(
            () => service.AddExperienceAsync("Adopt Me", "920587237"));

        Assert.Empty(service.Current.Experiences);
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
}

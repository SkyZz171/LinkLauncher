namespace LinkLauncher.Infrastructure.Tests;

using LinkLauncher.Core.Abstractions;
using LinkLauncher.Core.Models;
using LinkLauncher.Infrastructure.FileSystem;
using LinkLauncher.Infrastructure.Settings;
using LinkLauncher.Infrastructure.Windows;
using Xunit;

public sealed class SettingsServiceTests : IDisposable
{
    private readonly string _root;
    private readonly JsonSettingsStore _store;
    private readonly CapturingLogger _logger = new();

    public SettingsServiceTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "linklauncher-tests", Guid.NewGuid().ToString("N"));
        var paths = new WindowsAppPaths(Path.Combine(_root, "appdata"), Path.Combine(_root, "local"));
        _store = new JsonSettingsStore(new SystemFileSystem(), paths, _logger);
    }

    [Fact]
    public async Task InitializeAsync_LoadsStoreAndMarksReady()
    {
        await _store.SaveAsync(new AppSettings { Theme = AppTheme.Light });
        var service = new SettingsService(_store, _logger);

        await service.InitializeAsync();

        Assert.True(service.IsLoaded);
        Assert.Equal(AppTheme.Light, service.Current.Theme);
    }

    [Fact]
    public async Task UpdateAsync_PersistsAndRaisesChanged()
    {
        var service = new SettingsService(_store, _logger);
        await service.InitializeAsync();
        AppSettingsChangedEventArgs? captured = null;
        service.Changed += (_, args) => captured = args;

        var updated = await service.UpdateAsync(settings => settings with { Theme = AppTheme.Light });

        Assert.Equal(AppTheme.Light, updated.Theme);
        Assert.Equal(AppTheme.Light, service.Current.Theme);
        Assert.NotNull(captured);
        Assert.Equal(AppTheme.Dark, captured!.Previous.Theme);
        Assert.Equal(AppTheme.Light, captured.Current.Theme);

        var reloaded = await _store.LoadAsync();
        Assert.Equal(AppTheme.Light, reloaded.Theme);
    }

    [Fact]
    public async Task InitializeAsync_WhenStoreFails_UsesDefaults()
    {
        var service = new SettingsService(new ThrowingSettingsStore(), _logger);

        await service.InitializeAsync();

        Assert.True(service.IsLoaded);
        Assert.Equal(AppTheme.Dark, service.Current.Theme);
        Assert.Contains(_logger.Entries, entry => entry.Level == AppLogLevel.Error);
    }

    [Fact]
    public async Task UpdateAsync_WhenStoreFails_PropagatesAndKeepsCurrent()
    {
        var service = new SettingsService(new ThrowingSettingsStore(), _logger);
        await service.InitializeAsync();

        await Assert.ThrowsAsync<IOException>(
            () => service.UpdateAsync(settings => settings with { Theme = AppTheme.Light }));

        Assert.Equal(AppTheme.Dark, service.Current.Theme);
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

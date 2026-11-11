namespace LinkLauncher.Infrastructure.Tests;

using LinkLauncher.Core.Abstractions;
using LinkLauncher.Core.Models;
using LinkLauncher.Infrastructure.FileSystem;
using LinkLauncher.Infrastructure.Settings;
using LinkLauncher.Infrastructure.Windows;
using Xunit;

public sealed class JsonSettingsStoreTests : IDisposable
{
    private readonly string _root;
    private readonly WindowsAppPaths _paths;
    private readonly CapturingLogger _logger = new();
    private readonly JsonSettingsStore _store;

    public JsonSettingsStoreTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "linklauncher-tests", Guid.NewGuid().ToString("N"));
        _paths = new WindowsAppPaths(Path.Combine(_root, "appdata"), Path.Combine(_root, "local"));
        _store = new JsonSettingsStore(new SystemFileSystem(), _paths, _logger);
    }

    [Fact]
    public async Task LoadAsync_WhenFileIsMissing_ReturnsDefaults()
    {
        var settings = await _store.LoadAsync();

        Assert.Equal(AppTheme.Dark, settings.Theme);
        Assert.Equal(AccentColorKey.Aurora, settings.AccentColor);
        Assert.Equal(AppSettings.CurrentSchemaVersion, settings.SchemaVersion);
    }

    [Fact]
    public async Task SaveAsync_ThenLoadAsync_RoundTrips()
    {
        await _store.SaveAsync(new AppSettings { Theme = AppTheme.Light, AccentColor = AccentColorKey.Coral });

        var loaded = await _store.LoadAsync();

        Assert.Equal(AppTheme.Light, loaded.Theme);
        Assert.Equal(AccentColorKey.Coral, loaded.AccentColor);
    }

    [Fact]
    public async Task SaveAsync_SecondSave_PreservesPreviousFileAsBackup()
    {
        await _store.SaveAsync(new AppSettings { Theme = AppTheme.Light });
        await _store.SaveAsync(new AppSettings { Theme = AppTheme.Dark });

        Assert.True(File.Exists(_paths.SettingsBackupFilePath));
        var backup = await File.ReadAllTextAsync(_paths.SettingsBackupFilePath);
        Assert.Contains("Light", backup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SaveAsync_LeavesNoTemporaryFile()
    {
        await _store.SaveAsync(new AppSettings());

        Assert.False(File.Exists(_paths.SettingsFilePath + ".tmp"));
    }

    [Fact]
    public async Task LoadAsync_WithInvalidJson_ReturnsDefaultsAndPreservesFile()
    {
        Directory.CreateDirectory(_paths.ApplicationDataDirectory);
        await File.WriteAllTextAsync(_paths.SettingsFilePath, "{ ceci n'est pas du JSON }");

        var settings = await _store.LoadAsync();

        Assert.Equal(AppTheme.Dark, settings.Theme);
        Assert.Single(Directory.GetFiles(_paths.ApplicationDataDirectory, "settings.invalide-*.json"));
        Assert.Contains(_logger.Entries, entry => entry.Level == AppLogLevel.Error);
    }

    [Fact]
    public async Task SaveAsync_NormalizesUnknownValues()
    {
        await _store.SaveAsync(new AppSettings { Theme = (AppTheme)42, AccentColor = (AccentColorKey)7 });

        var loaded = await _store.LoadAsync();

        Assert.Equal(AppTheme.Dark, loaded.Theme);
        Assert.Equal(AccentColorKey.Aurora, loaded.AccentColor);
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

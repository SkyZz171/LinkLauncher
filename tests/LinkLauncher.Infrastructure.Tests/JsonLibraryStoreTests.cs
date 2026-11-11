namespace LinkLauncher.Infrastructure.Tests;

using LinkLauncher.Core.Abstractions;
using LinkLauncher.Core.Models;
using LinkLauncher.Core.Services;
using LinkLauncher.Infrastructure.FileSystem;
using LinkLauncher.Infrastructure.Library;
using LinkLauncher.Infrastructure.Windows;
using Xunit;

public sealed class JsonLibraryStoreTests : IDisposable
{
    private readonly string _root;
    private readonly WindowsAppPaths _paths;
    private readonly CapturingLogger _logger = new();
    private readonly JsonLibraryStore _store;

    public JsonLibraryStoreTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "linklauncher-tests", Guid.NewGuid().ToString("N"));
        _paths = new WindowsAppPaths(Path.Combine(_root, "appdata"), Path.Combine(_root, "local"));
        _store = new JsonLibraryStore(new SystemFileSystem(), _paths, _logger);
    }

    [Fact]
    public async Task LoadAsync_WhenFileIsMissing_ReturnsDefaultLibrary()
    {
        var library = await _store.LoadAsync();

        Assert.Single(library.Profiles);
        Assert.Equal(LauncherLibraryRules.DefaultProfileName, library.Profiles[0].Name);
        Assert.Equal(library.Profiles[0].Id, library.ActiveProfileId);
        Assert.False(File.Exists(_paths.LibraryFilePath));
    }

    [Fact]
    public async Task SaveAsync_ThenLoadAsync_RoundTripsProfilesAndExperiences()
    {
        var created = LauncherLibraryRules.AddProfile(
            LauncherLibraryRules.AddExperience(
                LauncherLibraryRules.CreateDefault(DateTimeOffset.Now),
                "Adopt Me",
                "920587237",
                DateTimeOffset.Now).Library,
            "Anglais",
            "en_US",
            DateTimeOffset.Now);

        await _store.SaveAsync(created.Library);

        var loaded = await _store.LoadAsync();

        Assert.Equal(2, loaded.Profiles.Count);
        Assert.Equal("Anglais", loaded.Profiles[1].Name);
        Assert.Equal("en_US", loaded.Profiles[1].Locale);
        var experience = Assert.Single(loaded.Experiences);
        Assert.Equal("Adopt Me", experience.Name);
        Assert.Equal("https://www.roblox.com/games/920587237", experience.Url);
        Assert.Equal(created.Library.ActiveProfileId, loaded.ActiveProfileId);
    }

    [Fact]
    public async Task SaveAsync_LeavesNoTemporaryFile()
    {
        await _store.SaveAsync(LauncherLibraryRules.CreateDefault(DateTimeOffset.Now));

        Assert.False(File.Exists(_paths.LibraryFilePath + ".tmp"));
        Assert.True(File.Exists(_paths.LibraryFilePath));
    }

    [Fact]
    public async Task LoadAsync_WithInvalidJson_ReturnsDefaultsAndPreservesFile()
    {
        Directory.CreateDirectory(_paths.ApplicationDataDirectory);
        await File.WriteAllTextAsync(_paths.LibraryFilePath, "{ ceci n'est pas du JSON }");

        var library = await _store.LoadAsync();

        Assert.Single(library.Profiles);
        Assert.Empty(library.Experiences);
        Assert.Single(Directory.GetFiles(_paths.ApplicationDataDirectory, "library.invalide-*.json"));
        Assert.Contains(_logger.Entries, entry => entry.Level == AppLogLevel.Error);
    }

    [Fact]
    public async Task LoadAsync_WithUnknownFields_NormalizesContent()
    {
        Directory.CreateDirectory(_paths.ApplicationDataDirectory);
        await File.WriteAllTextAsync(
            _paths.LibraryFilePath,
            """
            {
              "schemaVersion": 1,
              "profiles": [ { "id": "11111111-1111-1111-1111-111111111111", "name": "  Perso  ", "locale": "fr-FR" } ],
              "experiences": [
                { "id": "22222222-2222-2222-2222-222222222222", "name": "Hors site", "url": "https://example.com/games/1" }
              ],
              "activeProfileId": "11111111-1111-1111-1111-111111111111"
            }
            """);

        var library = await _store.LoadAsync();

        var profile = Assert.Single(library.Profiles);
        Assert.Equal("Perso", profile.Name);
        Assert.Equal("fr_FR", profile.Locale);
        Assert.Empty(library.Experiences);
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

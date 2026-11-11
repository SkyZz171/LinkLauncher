namespace LinkLauncher.Integration.Tests;

using LinkLauncher.Core.Abstractions;
using LinkLauncher.Core.Models;
using LinkLauncher.Infrastructure.FileSystem;
using LinkLauncher.Infrastructure.Settings;
using LinkLauncher.Infrastructure.Windows;
using Xunit;

/// <summary>Vérifie le cycle complet d'écriture puis de relecture des préférences.</summary>
public sealed class SettingsPersistenceTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "linklauncher-tests", Guid.NewGuid().ToString("N"));
    private readonly CapturingLogger _logger = new();

    [Fact]
    public async Task Preferences_SurviveRestart()
    {
        var paths = CreatePaths();
        var first = CreateService(paths);

        await first.InitializeAsync();
        Assert.True(first.Current.EnableNotifications);

        await first.UpdateAsync(settings => settings with
        {
            Theme = AppTheme.Dark,
            AccentColor = AccentColorKey.Emerald,
            EnableNotifications = false,
            EnableDiscordPresence = true,
            DiscordApplicationId = "123456789012345678"
        });

        var second = CreateService(paths);
        await second.InitializeAsync();

        Assert.Equal(AppTheme.Dark, second.Current.Theme);
        Assert.Equal(AccentColorKey.Emerald, second.Current.AccentColor);
        Assert.False(second.Current.EnableNotifications);
        Assert.True(second.Current.EnableDiscordPresence);
        Assert.Equal("123456789012345678", second.Current.DiscordApplicationId);
    }

    [Fact]
    public async Task Update_WithUnverifiableDiscordIdentifier_DisablesPresence()
    {
        var service = CreateService(CreatePaths());
        await service.InitializeAsync();

        var updated = await service.UpdateAsync(settings => settings with
        {
            EnableDiscordPresence = true,
            DiscordApplicationId = "identifiant-invalide"
        });

        Assert.Null(updated.DiscordApplicationId);
        Assert.False(updated.EnableDiscordPresence);
    }

    [Fact]
    public async Task Initialize_WithoutSettingsFile_StartsFromDefaults()
    {
        var service = CreateService(CreatePaths());

        await service.InitializeAsync();

        Assert.Equal(AppSettings.CreateDefault().Theme, service.Current.Theme);
        Assert.Equal(AppSettings.CreateDefault().AccentColor, service.Current.AccentColor);
        Assert.DoesNotContain(_logger.Entries, entry => entry.Level >= AppLogLevel.Warning);
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

    private SettingsService CreateService(WindowsAppPaths paths) =>
        new(new JsonSettingsStore(new SystemFileSystem(), paths, _logger), _logger);
}

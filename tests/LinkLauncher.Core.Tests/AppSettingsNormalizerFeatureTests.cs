namespace LinkLauncher.Core.Tests;

using LinkLauncher.Core.Models;
using LinkLauncher.Core.Services;
using Xunit;

public sealed class AppSettingsNormalizerFeatureTests
{
    [Fact]
    public void CreateDefault_EnablesNotificationsAndDisablesPresence()
    {
        var settings = AppSettings.CreateDefault();

        Assert.True(settings.EnableNotifications);
        Assert.False(settings.EnableDiscordPresence);
        Assert.Null(settings.DiscordApplicationId);
        Assert.Equal(2, AppSettings.CurrentSchemaVersion);
    }

    [Fact]
    public void Normalize_WithOlderSchema_AlignsVersionAndKeepsDefaults()
    {
        var normalized = AppSettingsNormalizer.Normalize(new AppSettings { SchemaVersion = 1 });

        Assert.Equal(AppSettings.CurrentSchemaVersion, normalized.SchemaVersion);
        Assert.True(normalized.EnableNotifications);
    }

    [Fact]
    public void Normalize_WithInvalidDiscordIdentifier_ClearsItAndDisablesPresence()
    {
        var normalized = AppSettingsNormalizer.Normalize(new AppSettings
        {
            DiscordApplicationId = "mon-identifiant",
            EnableDiscordPresence = true
        });

        Assert.Null(normalized.DiscordApplicationId);
        Assert.False(normalized.EnableDiscordPresence);
    }

    [Fact]
    public void Normalize_WithValidDiscordIdentifier_KeepsPresenceEnabled()
    {
        var normalized = AppSettingsNormalizer.Normalize(new AppSettings
        {
            DiscordApplicationId = "123456789012345678",
            EnableDiscordPresence = true
        });

        Assert.Equal("123456789012345678", normalized.DiscordApplicationId);
        Assert.True(normalized.EnableDiscordPresence);
    }

    [Fact]
    public void Normalize_KeepsNotificationsChoice()
    {
        var normalized = AppSettingsNormalizer.Normalize(new AppSettings { EnableNotifications = false });

        Assert.False(normalized.EnableNotifications);
    }
}

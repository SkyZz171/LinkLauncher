namespace LinkLauncher.Core.Tests;

using LinkLauncher.Core.Models;
using LinkLauncher.Core.Services;
using Xunit;

public sealed class AppSettingsNormalizerTests
{
    [Fact]
    public void Normalize_Null_ReturnsDefaults()
    {
        var settings = AppSettingsNormalizer.Normalize(null);

        Assert.Equal(AppTheme.Dark, settings.Theme);
        Assert.Equal(AccentColorKey.Aurora, settings.AccentColor);
        Assert.Equal(AppSettings.CurrentSchemaVersion, settings.SchemaVersion);
    }

    [Fact]
    public void Normalize_LegacySchemaVersion_UpgradesToCurrent()
    {
        var settings = AppSettingsNormalizer.Normalize(new AppSettings { SchemaVersion = 0 });

        Assert.Equal(AppSettings.CurrentSchemaVersion, settings.SchemaVersion);
    }

    [Fact]
    public void Normalize_UnknownTheme_FallsBackToDefault()
    {
        var settings = AppSettingsNormalizer.Normalize(new AppSettings { Theme = (AppTheme)99 });

        Assert.Equal(AppTheme.Dark, settings.Theme);
    }

    [Fact]
    public void Normalize_UnknownAccent_FallsBackToDefault()
    {
        var settings = AppSettingsNormalizer.Normalize(new AppSettings { AccentColor = (AccentColorKey)42 });

        Assert.Equal(AccentColorKey.Aurora, settings.AccentColor);
    }

    [Fact]
    public void Normalize_ValidSettings_KeepsValues()
    {
        var original = new AppSettings { Theme = AppTheme.Light, AccentColor = AccentColorKey.Emerald };

        var settings = AppSettingsNormalizer.Normalize(original);

        Assert.Equal(AppTheme.Light, settings.Theme);
        Assert.Equal(AccentColorKey.Emerald, settings.AccentColor);
    }
}

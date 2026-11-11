namespace LinkLauncher.Core.Tests;

using LinkLauncher.Core.Models;
using LinkLauncher.Core.Services;
using Xunit;

public sealed class LauncherLibraryRulesTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 11, 12, 0, 0, TimeSpan.FromHours(1));

    [Fact]
    public void CreateDefault_ContainsOneActiveProfile()
    {
        var library = LauncherLibraryRules.CreateDefault(Now);

        var profile = Assert.Single(library.Profiles);
        Assert.Equal(LauncherLibraryRules.DefaultProfileName, profile.Name);
        Assert.Null(profile.Locale);
        Assert.Equal(profile.Id, library.ActiveProfileId);
        Assert.Empty(library.Experiences);
        Assert.Equal(LauncherLibrary.CurrentSchemaVersion, library.SchemaVersion);
    }

    [Fact]
    public void Normalize_WithoutProfiles_CreatesDefaultProfile()
    {
        var library = LauncherLibraryRules.Normalize(new LauncherLibrary(), Now);

        var profile = Assert.Single(library.Profiles);
        Assert.Equal(profile.Id, library.ActiveProfileId);
    }

    [Fact]
    public void Normalize_RemovesInvalidAndDuplicateExperiences()
    {
        var library = new LauncherLibrary
        {
            Profiles = LauncherLibraryRules.CreateDefault(Now).Profiles,
            Experiences =
            [
                new Experience { Id = Guid.NewGuid(), Name = "Adopt Me", Url = "https://www.roblox.com/games/920587237", AddedAt = Now },
                new Experience { Id = Guid.NewGuid(), Name = "Doublon", Url = "920587237", AddedAt = Now },
                new Experience { Id = Guid.NewGuid(), Name = "Hors site", Url = "https://example.com/games/1", AddedAt = Now },
                new Experience { Id = Guid.NewGuid(), Name = "   ", Url = "920587237", AddedAt = Now }
            ]
        };

        var normalized = LauncherLibraryRules.Normalize(library, Now);

        var experience = Assert.Single(normalized.Experiences);
        Assert.Equal("Adopt Me", experience.Name);
        Assert.Equal(920587237L, experience.PlaceId);
    }

    [Fact]
    public void Normalize_NormalizesNameAndLocaleAndActiveProfile()
    {
        var profile = new LaunchProfile { Id = Guid.NewGuid(), Name = "  Français  ", Locale = "fr-FR" };
        var library = new LauncherLibrary
        {
            Profiles = [profile],
            ActiveProfileId = Guid.NewGuid()
        };

        var normalized = LauncherLibraryRules.Normalize(library, Now);

        var stored = Assert.Single(normalized.Profiles);
        Assert.Equal("Français", stored.Name);
        Assert.Equal("fr_FR", stored.Locale);
        Assert.Equal(stored.Id, normalized.ActiveProfileId);
    }

    [Fact]
    public void AddProfile_WithValidInput_AppendsProfile()
    {
        var library = LauncherLibraryRules.CreateDefault(Now);

        var result = LauncherLibraryRules.AddProfile(library, "  Anglais ", "en-US", Now);

        Assert.True(result.Succeeded);
        Assert.Null(result.Error);
        Assert.Equal(2, result.Library.Profiles.Count);
        Assert.Equal("Anglais", result.Library.Profiles[1].Name);
        Assert.Equal("en_US", result.Library.Profiles[1].Locale);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddProfile_WithoutName_IsRejected(string? name)
    {
        var library = LauncherLibraryRules.CreateDefault(Now);

        var result = LauncherLibraryRules.AddProfile(library, name!, null, Now);

        Assert.False(result.Succeeded);
        Assert.NotNull(result.Error);
        Assert.Single(result.Library.Profiles);
    }

    [Fact]
    public void AddProfile_WithDuplicateName_IsRejected()
    {
        var library = LauncherLibraryRules.CreateDefault(Now);

        var result = LauncherLibraryRules.AddProfile(library, "par défaut", null, Now);

        Assert.False(result.Succeeded);
        Assert.Single(result.Library.Profiles);
    }

    [Fact]
    public void AddProfile_WithInvalidLocale_IsRejected()
    {
        var library = LauncherLibraryRules.CreateDefault(Now);

        var result = LauncherLibraryRules.AddProfile(library, "Test", "fr_FR+launchmode:play", Now);

        Assert.False(result.Succeeded);
        Assert.Single(result.Library.Profiles);
    }

    [Fact]
    public void RemoveProfile_WhenLastProfile_IsRejected()
    {
        var library = LauncherLibraryRules.CreateDefault(Now);

        var result = LauncherLibraryRules.RemoveProfile(library, library.Profiles[0].Id);

        Assert.False(result.Succeeded);
        Assert.Single(result.Library.Profiles);
    }

    [Fact]
    public void RemoveProfile_ActiveProfile_SwitchesToRemainingProfile()
    {
        var added = LauncherLibraryRules.AddProfile(LauncherLibraryRules.CreateDefault(Now), "Anglais", "en_US", Now);
        var activeId = added.Library.Profiles[0].Id;

        var result = LauncherLibraryRules.RemoveProfile(added.Library, activeId);

        Assert.True(result.Succeeded);
        var remaining = Assert.Single(result.Library.Profiles);
        Assert.Equal("Anglais", remaining.Name);
        Assert.Equal(remaining.Id, result.Library.ActiveProfileId);
    }

    [Fact]
    public void SetActiveProfile_WithUnknownId_IsRejected()
    {
        var library = LauncherLibraryRules.CreateDefault(Now);

        var result = LauncherLibraryRules.SetActiveProfile(library, Guid.NewGuid());

        Assert.False(result.Succeeded);
        Assert.Equal(library.ActiveProfileId, result.Library.ActiveProfileId);
    }

    [Fact]
    public void SetActiveProfile_WithKnownId_ActivatesProfile()
    {
        var added = LauncherLibraryRules.AddProfile(LauncherLibraryRules.CreateDefault(Now), "Anglais", null, Now);
        var profileId = added.Library.Profiles[1].Id;

        var result = LauncherLibraryRules.SetActiveProfile(added.Library, profileId);

        Assert.True(result.Succeeded);
        Assert.Equal(profileId, result.Library.ActiveProfileId);
    }

    [Fact]
    public void AddExperience_WithPlaceId_StoresCanonicalUrl()
    {
        var library = LauncherLibraryRules.CreateDefault(Now);

        var result = LauncherLibraryRules.AddExperience(library, "Adopt Me", "https://www.roblox.com/games/920587237/Adopt-Me", Now);

        Assert.True(result.Succeeded);
        var experience = Assert.Single(result.Library.Experiences);
        Assert.Equal("Adopt Me", experience.Name);
        Assert.Equal("https://www.roblox.com/games/920587237", experience.Url);
        Assert.Equal(920587237L, experience.PlaceId);
        Assert.Null(experience.OpenedAt);
    }

    [Fact]
    public void AddExperience_WithDuplicate_IsRejected()
    {
        var library = LauncherLibraryRules
            .AddExperience(LauncherLibraryRules.CreateDefault(Now), "Adopt Me", "920587237", Now)
            .Library;

        var result = LauncherLibraryRules.AddExperience(library, "Autre nom", "https://www.roblox.com/games/920587237", Now);

        Assert.False(result.Succeeded);
        Assert.Single(result.Library.Experiences);
    }

    [Theory]
    [InlineData("Adopt Me", "https://example.com/games/1")]
    [InlineData("Adopt Me", "pas un lien")]
    [InlineData("", "920587237")]
    public void AddExperience_WithInvalidInput_IsRejected(string name, string link)
    {
        var library = LauncherLibraryRules.CreateDefault(Now);

        var result = LauncherLibraryRules.AddExperience(library, name, link, Now);

        Assert.False(result.Succeeded);
        Assert.Empty(result.Library.Experiences);
    }

    [Fact]
    public void MarkExperienceOpened_SetsTimestamp()
    {
        var added = LauncherLibraryRules.AddExperience(LauncherLibraryRules.CreateDefault(Now), "Adopt Me", "920587237", Now);
        var experienceId = added.Library.Experiences[0].Id;
        var openedAt = Now.AddHours(2);

        var result = LauncherLibraryRules.MarkExperienceOpened(added.Library, experienceId, openedAt);

        Assert.True(result.Succeeded);
        Assert.Equal(openedAt, result.Library.Experiences[0].OpenedAt);
    }

    [Fact]
    public void RemoveExperience_RemovesEntry()
    {
        var added = LauncherLibraryRules.AddExperience(LauncherLibraryRules.CreateDefault(Now), "Adopt Me", "920587237", Now);
        var experienceId = added.Library.Experiences[0].Id;

        var result = LauncherLibraryRules.RemoveExperience(added.Library, experienceId);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Library.Experiences);
    }
}

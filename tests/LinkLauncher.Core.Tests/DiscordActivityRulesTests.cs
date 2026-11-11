namespace LinkLauncher.Core.Tests;

using LinkLauncher.Core.Services;
using Xunit;

public sealed class DiscordActivityRulesTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 11, 12, 0, 0, TimeSpan.FromHours(1));

    [Fact]
    public void Create_WhenClientIsIdle_DescribesLauncher()
    {
        var activity = DiscordActivityRules.Create(clientRunning: false, experienceName: null, profileName: null, Now);

        Assert.Equal("Dans LinkLauncher", activity.Details);
        Assert.Equal("Aucun client lancé", activity.State);
        Assert.Equal(Now, activity.StartedAt);
    }

    [Fact]
    public void Create_WhenClientRuns_ReportsRunningClient()
    {
        var activity = DiscordActivityRules.Create(clientRunning: true, experienceName: null, profileName: null, Now);

        Assert.Equal("Client Roblox lancé", activity.Details);
        Assert.Equal("Sans expérience", activity.State);
    }

    [Fact]
    public void Create_WithExperienceAndProfile_MentionsBoth()
    {
        var activity = DiscordActivityRules.Create(true, "Adopt Me", "Anglais", Now);

        Assert.Contains("Adopt Me", activity.State, StringComparison.Ordinal);
        Assert.Contains("Anglais", activity.State, StringComparison.Ordinal);
    }

    [Fact]
    public void Create_WithBlankProfile_SkipsProfileSuffix()
    {
        var activity = DiscordActivityRules.Create(true, "Adopt Me", "   ", Now);

        Assert.Equal("Page : Adopt Me", activity.State);
    }

    [Fact]
    public void Create_WithVeryLongExperience_TruncatesState()
    {
        var activity = DiscordActivityRules.Create(true, new string('x', 400), null, Now);

        Assert.True(activity.State.Length <= DiscordActivityRules.MaximumTextLength);
    }
}

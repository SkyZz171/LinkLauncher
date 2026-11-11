namespace LinkLauncher.Core.Tests;

using LinkLauncher.Core.Models;
using LinkLauncher.Core.Services;
using Xunit;

public sealed class AppNotificationRulesTests
{
    [Fact]
    public void Create_WhenClientStarts_DescribesProcessIdentifier()
    {
        var transition = new RobloxProcessTransition(
            RobloxProcessChange.Started,
            new RobloxProcessInfo { ProcessId = 4321 });

        var notification = AppNotificationRules.Create(transition);

        Assert.NotNull(notification);
        Assert.Equal(NotificationKind.ClientStarted, notification!.Kind);
        Assert.Equal("Client Roblox lancé", notification.Title);
        Assert.Contains("4321", notification.Body, StringComparison.Ordinal);
    }

    [Fact]
    public void Create_WhenClientStopsWithExitCode_MentionsCode()
    {
        var transition = new RobloxProcessTransition(
            RobloxProcessChange.Stopped,
            new RobloxProcessInfo { ProcessId = 12, ExitCode = 0 });

        var notification = AppNotificationRules.Create(transition);

        Assert.NotNull(notification);
        Assert.Equal(NotificationKind.ClientStopped, notification!.Kind);
        Assert.Contains("code 0", notification.Body, StringComparison.Ordinal);
    }

    [Fact]
    public void Create_WhenClientStopsWithoutExitCode_StaysNeutral()
    {
        var transition = new RobloxProcessTransition(
            RobloxProcessChange.Stopped,
            new RobloxProcessInfo { ProcessId = 12 });

        var notification = AppNotificationRules.Create(transition);

        Assert.NotNull(notification);
        Assert.Equal("Le client Roblox s'est arrêté.", notification!.Body);
    }

    [Fact]
    public void Create_WithoutTransition_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => AppNotificationRules.Create(null!));
    }
}

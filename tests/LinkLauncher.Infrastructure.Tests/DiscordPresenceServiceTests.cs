namespace LinkLauncher.Infrastructure.Tests;

using LinkLauncher.Core.Abstractions;
using LinkLauncher.Core.Models;
using LinkLauncher.Core.Services;
using LinkLauncher.Infrastructure.Discord;
using Xunit;

public sealed class DiscordPresenceServiceTests
{
    private const string ValidApplicationId = "123456789012345678";

    private readonly CapturingLogger _logger = new();
    private readonly FakeDiscordIpcTransport _transport = new();

    [Fact]
    public async Task TryPublishAsync_WhenPresenceIsDisabled_DoesNotConnect()
    {
        var service = CreateService(enabled: false, applicationId: ValidApplicationId);

        var published = await service.TryPublishAsync(Activity());

        Assert.False(published);
        Assert.Equal(DiscordPresenceState.Disabled, service.State);
        Assert.Empty(_transport.ConnectedApplicationIds);
    }

    [Fact]
    public async Task TryPublishAsync_WithoutApplicationId_StaysDisabled()
    {
        var service = CreateService(enabled: true, applicationId: null);

        var published = await service.TryPublishAsync(Activity());

        Assert.False(published);
        Assert.Equal(DiscordPresenceState.Disabled, service.State);
        Assert.Empty(_transport.ConnectedApplicationIds);
    }

    [Fact]
    public async Task TryPublishAsync_WhenEnabled_ConnectsAndPublishesActivity()
    {
        var service = CreateService(enabled: true, applicationId: ValidApplicationId);

        var published = await service.TryPublishAsync(Activity());

        Assert.True(published);
        Assert.Equal(DiscordPresenceState.Connected, service.State);
        Assert.Equal([ValidApplicationId], _transport.ConnectedApplicationIds);
        var payload = Assert.Single(_transport.SentPayloads);
        Assert.Contains("SET_ACTIVITY", payload, StringComparison.Ordinal);
        Assert.Contains("Client Roblox", payload, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TryPublishAsync_WhenClearRequested_SendsCommandWithoutActivity()
    {
        var service = CreateService(enabled: true, applicationId: ValidApplicationId);

        var published = await service.TryPublishAsync(activity: null);

        Assert.True(published);
        Assert.DoesNotContain("activity", Assert.Single(_transport.SentPayloads), StringComparison.Ordinal);
    }

    [Fact]
    public async Task TryPublishAsync_WhenDiscordIsUnreachable_ReportsUnavailableOnce()
    {
        _transport.ConnectResult = false;
        var service = CreateService(enabled: true, applicationId: ValidApplicationId);

        await service.TryPublishAsync(Activity());
        await service.TryPublishAsync(Activity());

        Assert.Equal(DiscordPresenceState.Unavailable, service.State);
        Assert.Single(_logger.Entries, entry => entry.Level == AppLogLevel.Warning);
    }

    [Fact]
    public async Task TryPublishAsync_WhenSendFails_ReportsUnavailable()
    {
        _transport.SendResult = false;
        var service = CreateService(enabled: true, applicationId: ValidApplicationId);

        var published = await service.TryPublishAsync(Activity());

        Assert.False(published);
        Assert.Equal(DiscordPresenceState.Unavailable, service.State);
    }

    private DiscordPresenceService CreateService(bool enabled, string? applicationId) =>
        new(_transport, new StubSettingsService(new AppSettings
        {
            EnableDiscordPresence = enabled,
            DiscordApplicationId = applicationId
        }), _logger);

    private static DiscordActivity Activity() => new()
    {
        Details = "Client Roblox lancé",
        State = "Sans expérience"
    };
}

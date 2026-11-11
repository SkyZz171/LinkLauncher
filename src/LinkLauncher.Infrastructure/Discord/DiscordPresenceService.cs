namespace LinkLauncher.Infrastructure.Discord;

using LinkLauncher.Core.Abstractions;
using LinkLauncher.Core.Models;
using LinkLauncher.Core.Services;

/// <summary>Publie l'activité de LinkLauncher sur le client Discord local, si l'utilisateur l'a activé.</summary>
/// <remarks>
/// La présence reste locale et facultative : sans identifiant d'application Discord valide, ou si
/// Discord n'est pas démarré, le service n'expose aucune activité et se contente de journaliser.
/// </remarks>
public sealed class DiscordPresenceService : IDiscordPresenceService
{
    private const string Category = "Discord";

    private readonly IDiscordIpcTransport _transport;
    private readonly ISettingsService _settings;
    private readonly IAppLogger _logger;

    private bool _unavailableLogged;

    /// <summary>Crée le service de présence.</summary>
    public DiscordPresenceService(
        IDiscordIpcTransport transport,
        ISettingsService settings,
        IAppLogger logger)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public DiscordPresenceState State { get; private set; } = DiscordPresenceState.Disabled;

    /// <inheritdoc />
    public async Task<bool> TryPublishAsync(DiscordActivity? activity, CancellationToken cancellationToken = default)
    {
        var settings = _settings.Current;
        var applicationId = DiscordApplicationIdRules.Normalize(settings.DiscordApplicationId);

        if (!settings.EnableDiscordPresence || applicationId is null)
        {
            await DisconnectAsync(cancellationToken).ConfigureAwait(false);
            State = DiscordPresenceState.Disabled;
            return false;
        }

        if (!_transport.IsConnected && !await _transport.ConnectAsync(applicationId, cancellationToken).ConfigureAwait(false))
        {
            MarkUnavailable("Le client Discord n'est pas joignable (présence inactive).");
            return false;
        }

        var nonce = Guid.NewGuid().ToString("N");
        var frame = activity is null
            ? DiscordIpcMessages.EncodeClearActivity(Environment.ProcessId, nonce)
            : DiscordIpcMessages.EncodeSetActivity(Environment.ProcessId, activity, nonce);

        if (!await _transport.TrySendAsync(frame, cancellationToken).ConfigureAwait(false))
        {
            MarkUnavailable("L'activité Discord n'a pas pu être transmise.");
            return false;
        }

        State = DiscordPresenceState.Connected;
        _unavailableLogged = false;
        return true;
    }

    /// <inheritdoc />
    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        await _transport.DisconnectAsync(cancellationToken).ConfigureAwait(false);
    }

    private void MarkUnavailable(string message)
    {
        State = DiscordPresenceState.Unavailable;

        if (_unavailableLogged)
        {
            return;
        }

        _unavailableLogged = true;
        _logger.Warn(Category, message);
    }
}

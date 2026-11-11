namespace LinkLauncher.Features.Discord;

using LinkLauncher.Core.Abstractions;
using LinkLauncher.Core.Models;
using LinkLauncher.Core.Services;

/// <summary>Maintient l'activité Discord en accord avec l'état réel du client Roblox.</summary>
public sealed class DiscordPresenceCoordinator : IDisposable
{
    private readonly IDiscordPresenceService _presence;
    private readonly IRobloxProcessMonitor _processMonitor;
    private readonly ILauncherLibrary _library;
    private readonly ISettingsService _settings;
    private readonly IAppLogger _logger;

    private readonly DateTimeOffset _startedAt = DateTimeOffset.Now;

    private bool _started;

    /// <summary>Crée le coordinateur.</summary>
    public DiscordPresenceCoordinator(
        IDiscordPresenceService presence,
        IRobloxProcessMonitor processMonitor,
        ILauncherLibrary library,
        ISettingsService settings,
        IAppLogger logger)
    {
        _presence = presence ?? throw new ArgumentNullException(nameof(presence));
        _processMonitor = processMonitor ?? throw new ArgumentNullException(nameof(processMonitor));
        _library = library ?? throw new ArgumentNullException(nameof(library));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>État courant de la présence.</summary>
    public DiscordPresenceState State => _presence.State;

    /// <summary>Commence la publication de l'activité.</summary>
    public void Start()
    {
        if (_started)
        {
            return;
        }

        _started = true;
        _processMonitor.Changed += OnStateChanged;
        _library.Changed += OnStateChanged;
        _settings.Changed += OnStateChanged;

        _ = PublishAsync();
    }

    /// <summary>Arrête la publication et efface l'activité.</summary>
    public void Dispose()
    {
        if (!_started)
        {
            return;
        }

        _started = false;
        _processMonitor.Changed -= OnStateChanged;
        _library.Changed -= OnStateChanged;
        _settings.Changed -= OnStateChanged;
    }

    private void OnStateChanged(object? sender, EventArgs e) => _ = PublishAsync();

    private async Task PublishAsync()
    {
        try
        {
            var activity = DiscordActivityRules.Create(
                _processMonitor.Current is not null,
                experienceName: null,
                profileName: LauncherLibraryRules.ResolveActiveProfile(_library.Current)?.Name,
                _startedAt);

            await _presence.TryPublishAsync(activity);
        }
        catch (Exception ex)
        {
            // Discord absent ou fermé : la présence reste simplement inactive.
            _logger.Warn("Discord", $"Publication de l'activité impossible : {ex.Message}");
        }
    }
}

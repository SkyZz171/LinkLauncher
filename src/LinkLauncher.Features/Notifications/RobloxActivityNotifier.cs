namespace LinkLauncher.Features.Notifications;

using LinkLauncher.Core.Abstractions;
using LinkLauncher.Core.Models;
using LinkLauncher.Core.Services;

/// <summary>Présente une notification quand le client Roblox démarre ou s'arrête.</summary>
public sealed class RobloxActivityNotifier : IDisposable
{
    private readonly IRobloxProcessMonitor _processMonitor;
    private readonly ISettingsService _settings;
    private readonly INotificationPresenter _presenter;
    private readonly IAppLogger _logger;

    private bool _started;

    /// <summary>Crée le notificateur.</summary>
    public RobloxActivityNotifier(
        IRobloxProcessMonitor processMonitor,
        ISettingsService settings,
        INotificationPresenter presenter,
        IAppLogger logger)
    {
        _processMonitor = processMonitor ?? throw new ArgumentNullException(nameof(processMonitor));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Commence l'écoute des transitions du client Roblox.</summary>
    public void Start()
    {
        if (_started)
        {
            return;
        }

        _started = true;
        _processMonitor.Changed += OnProcessChanged;
    }

    /// <summary>Arrête l'écoute.</summary>
    public void Dispose()
    {
        if (!_started)
        {
            return;
        }

        _started = false;
        _processMonitor.Changed -= OnProcessChanged;
    }

    private async void OnProcessChanged(object? sender, RobloxProcessChangedEventArgs args)
    {
        if (!_settings.Current.EnableNotifications)
        {
            return;
        }

        var notification = AppNotificationRules.Create(args.Transition);
        if (notification is null)
        {
            return;
        }

        try
        {
            await _presenter.ShowAsync(notification);
        }
        catch (Exception ex)
        {
            // Une notification manquée ne doit jamais perturber l'application.
            _logger.Error(AppNotificationRules.LogCategory, "Notification non présentée.", ex);
        }
    }
}

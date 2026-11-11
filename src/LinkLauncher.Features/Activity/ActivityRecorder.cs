namespace LinkLauncher.Features.Activity;

using LinkLauncher.Core.Abstractions;
using LinkLauncher.Core.Models;
using LinkLauncher.Core.Services;

/// <summary>Enregistre les sessions du client Roblox observées par le moniteur de processus.</summary>
public sealed class ActivityRecorder : IDisposable
{
    private const string Category = "Activité";

    private readonly IRobloxProcessMonitor _processMonitor;
    private readonly IActivityHistory _history;
    private readonly ILauncherLibrary _library;
    private readonly IAppLogger _logger;

    private bool _started;

    /// <summary>Crée l'enregistreur d'activité.</summary>
    public ActivityRecorder(
        IRobloxProcessMonitor processMonitor,
        IActivityHistory history,
        ILauncherLibrary library,
        IAppLogger logger)
    {
        _processMonitor = processMonitor ?? throw new ArgumentNullException(nameof(processMonitor));
        _history = history ?? throw new ArgumentNullException(nameof(history));
        _library = library ?? throw new ArgumentNullException(nameof(library));
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

        // Un client déjà lancé avant LinkLauncher ouvre une session à partir de son heure réelle.
        if (_processMonitor.Current is { } current)
        {
            _ = BeginAsync(current);
        }
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
        try
        {
            switch (args.Transition.Change)
            {
                case RobloxProcessChange.Started:
                    await BeginAsync(args.Transition.Process);
                    break;

                case RobloxProcessChange.Stopped:
                    await _history.EndSessionAsync(args.Transition.Process);
                    break;
            }
        }
        catch (Exception ex)
        {
            // L'historique ne doit jamais perturber le suivi du client.
            _logger.Error(Category, "Enregistrement de la session impossible.", ex);
        }
    }

    private async Task BeginAsync(RobloxProcessInfo process)
    {
        var profile = LauncherLibraryRules.ResolveActiveProfile(_library.Current);

        // LinkLauncher ne sait pas quelle expérience le client a rejointe : la jointure se fait
        // dans le navigateur, hors de son périmètre. Aucun nom n'est donc supposé.
        await _history.StartSessionAsync(process, experienceName: null, profile?.Name);
    }
}

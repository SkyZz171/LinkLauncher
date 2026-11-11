namespace LinkLauncher.Infrastructure.Roblox;

using System.ComponentModel;
using LinkLauncher.Core.Abstractions;
using LinkLauncher.Core.Models;
using LinkLauncher.Core.Services;

/// <summary>
/// Surveille périodiquement la présence du client Roblox et publie les démarrages et arrêts.
/// </summary>
/// <remarks>
/// Le relevé s'exécute sur un timer, jamais sur le thread de l'interface.
/// Les abonnés sont responsables du retour sur leur propre thread.
/// </remarks>
public sealed class WindowsRobloxProcessMonitor : IRobloxProcessMonitor
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);

    private readonly IRobloxProcessSource _processSource;
    private readonly IAppLogger _logger;
    private readonly RobloxProcessTracker _tracker = new();
    private readonly object _gate = new();

    private Timer? _timer;
    private int _isPolling;
    private int _disposed;

    /// <summary>Crée le moniteur de processus.</summary>
    public WindowsRobloxProcessMonitor(IRobloxProcessSource processSource, IAppLogger logger)
    {
        _processSource = processSource ?? throw new ArgumentNullException(nameof(processSource));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public RobloxProcessInfo? Current => _tracker.Current;

    /// <inheritdoc />
    public event EventHandler<RobloxProcessChangedEventArgs>? Changed;

    /// <inheritdoc />
    public void Start()
    {
        if (Volatile.Read(ref _disposed) != 0)
        {
            return;
        }

        lock (_gate)
        {
            _timer ??= new Timer(_ => Poll(), null, TimeSpan.Zero, PollInterval);
        }
    }

    /// <inheritdoc />
    public void Stop()
    {
        lock (_gate)
        {
            _timer?.Dispose();
            _timer = null;
        }

        _tracker.Reset();
    }

    /// <summary>Arrête la surveillance et libère les ressources.</summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        Stop();
    }

    private void Poll()
    {
        if (Volatile.Read(ref _disposed) != 0)
        {
            return;
        }

        if (Interlocked.Exchange(ref _isPolling, 1) == 1)
        {
            return;
        }

        try
        {
            var transitions = _tracker.Update(
                _processSource.GetRunningProcesses(),
                _processSource.TryGetExitCode);

            foreach (var transition in transitions)
            {
                Publish(transition);
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException or Win32Exception or NotSupportedException)
        {
            _logger.Warn("Surveillance", $"Relevé des processus Roblox impossible : {ex.Message}");
        }
        finally
        {
            Volatile.Write(ref _isPolling, 0);
        }
    }

    private void Publish(RobloxProcessTransition transition)
    {
        var args = new RobloxProcessChangedEventArgs(transition, _tracker.Current);

        try
        {
            Changed?.Invoke(this, args);
        }
        catch (Exception ex)
        {
            // Une erreur d'un abonné ne doit pas interrompre la surveillance.
            _logger.Error("Surveillance", "Le traitement d'un changement de processus a échoué.", ex);
        }
    }
}

namespace LinkLauncher.Infrastructure.Activity;

using LinkLauncher.Core.Abstractions;
using LinkLauncher.Core.Models;
using LinkLauncher.Core.Services;

/// <summary>Expose l'historique des sessions et enregistre chaque modification.</summary>
public sealed class ActivityHistoryService : IActivityHistory
{
    private const string Category = "Activité";

    private readonly IActivityHistoryStore _store;
    private readonly IAppLogger _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private ActivityHistory _current = ActivityHistoryRules.CreateDefault();

    /// <summary>Crée le service d'historique.</summary>
    public ActivityHistoryService(IActivityHistoryStore store, IAppLogger logger)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public ActivityHistory Current => _current;

    /// <inheritdoc />
    public event EventHandler? Changed;

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _current = await _store.LoadAsync(cancellationToken).ConfigureAwait(false);
            _logger.Info(
                Category,
                $"Historique chargé depuis {_store.HistoryFilePath} ({_current.Sessions.Count} session(s)).");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _current = ActivityHistoryRules.CreateDefault();
            _logger.Error(Category, "Chargement de l'historique impossible, historique vide proposé.", ex);
        }
        finally
        {
            _gate.Release();
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    public Task StartSessionAsync(
        RobloxProcessInfo process,
        string? experienceName,
        string? profileName,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(process);

        var updated = ActivityHistoryRules.StartSession(
            _current,
            process,
            experienceName,
            profileName,
            DateTimeOffset.Now);

        return updated == _current ? Task.CompletedTask : CommitAsync(updated, cancellationToken);
    }

    /// <inheritdoc />
    public Task EndSessionAsync(RobloxProcessInfo process, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(process);

        var updated = ActivityHistoryRules.EndSession(_current, process, DateTimeOffset.Now);

        return updated == _current ? Task.CompletedTask : CommitAsync(updated, cancellationToken);
    }

    /// <inheritdoc />
    public Task ClearAsync(CancellationToken cancellationToken = default) =>
        _current.Sessions.Count == 0
            ? Task.CompletedTask
            : CommitAsync(ActivityHistoryRules.CreateDefault(), cancellationToken);

    private async Task CommitAsync(ActivityHistory history, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await _store.SaveAsync(history, cancellationToken).ConfigureAwait(false);
            _current = history;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.Error(Category, "Enregistrement de l'historique impossible.", ex);
            return;
        }
        finally
        {
            _gate.Release();
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }
}

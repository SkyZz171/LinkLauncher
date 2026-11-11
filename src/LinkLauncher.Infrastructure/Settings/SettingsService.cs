namespace LinkLauncher.Infrastructure.Settings;

using LinkLauncher.Core.Abstractions;
using LinkLauncher.Core.Models;
using LinkLauncher.Core.Services;

/// <summary>Expose la configuration chargée et sérialise ses modifications.</summary>
public sealed class SettingsService : ISettingsService
{
    private const string Category = "Configuration";

    private readonly ISettingsStore _store;
    private readonly IAppLogger _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private AppSettings _current = AppSettings.CreateDefault();

    /// <summary>Crée le service de configuration.</summary>
    public SettingsService(ISettingsStore store, IAppLogger logger)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public AppSettings Current => _current;

    /// <inheritdoc />
    public bool IsLoaded { get; private set; }

    /// <inheritdoc />
    public event EventHandler<AppSettingsChangedEventArgs>? Changed;

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _current = await _store.LoadAsync(cancellationToken).ConfigureAwait(false);
            _logger.Info(Category, $"Configuration chargée depuis {_store.SettingsFilePath}.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _current = AppSettings.CreateDefault();
            _logger.Error(Category, "Chargement de la configuration impossible, valeurs par défaut appliquées.", ex);
        }
        finally
        {
            _gate.Release();
        }

        IsLoaded = true;
    }

    /// <inheritdoc />
    public async Task<AppSettings> UpdateAsync(
        Func<AppSettings, AppSettings> mutation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mutation);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        AppSettings previous;
        AppSettings updated;
        try
        {
            previous = _current;
            updated = AppSettingsNormalizer.Normalize(mutation(previous));
            await _store.SaveAsync(updated, cancellationToken).ConfigureAwait(false);
            _current = updated;
        }
        finally
        {
            _gate.Release();
        }

        Changed?.Invoke(this, new AppSettingsChangedEventArgs(previous, updated));
        return updated;
    }
}

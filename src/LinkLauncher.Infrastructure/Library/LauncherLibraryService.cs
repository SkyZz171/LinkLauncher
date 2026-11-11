namespace LinkLauncher.Infrastructure.Library;

using LinkLauncher.Core.Abstractions;
using LinkLauncher.Core.Models;
using LinkLauncher.Core.Services;

/// <summary>Expose la bibliothèque chargée, sérialise ses modifications et applique les règles du domaine.</summary>
public sealed class LauncherLibraryService : ILauncherLibrary, IActiveProfileProvider
{
    private const string Category = "Bibliothèque";

    private readonly ILibraryStore _store;
    private readonly IAppLogger _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private LauncherLibrary _current = new();

    /// <summary>Crée le service de bibliothèque.</summary>
    public LauncherLibraryService(ILibraryStore store, IAppLogger logger)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public LauncherLibrary Current => _current;

    /// <inheritdoc />
    public bool IsLoaded { get; private set; }

    /// <inheritdoc />
    public event EventHandler? Changed;

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (IsLoaded)
            {
                return;
            }

            _current = await _store.LoadAsync(cancellationToken).ConfigureAwait(false);
            _logger.Info(
                Category,
                $"Bibliothèque chargée depuis {_store.LibraryFilePath} " +
                $"({_current.Profiles.Count} profil(s), {_current.Experiences.Count} expérience(s)).");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _current = LauncherLibraryRules.CreateDefault(DateTimeOffset.Now);
            _logger.Error(Category, "Chargement de la bibliothèque impossible, contenu par défaut proposé.", ex);
        }
        finally
        {
            _gate.Release();
        }

        IsLoaded = true;
    }

    /// <inheritdoc />
    public LaunchProfile? GetActiveProfile() => LauncherLibraryRules.ResolveActiveProfile(_current);

    /// <inheritdoc />
    public Task<LibraryMutation> AddProfileAsync(string name, string? locale, CancellationToken cancellationToken = default) =>
        MutateAsync(library => LauncherLibraryRules.AddProfile(library, name, locale, DateTimeOffset.Now), cancellationToken);

    /// <inheritdoc />
    public Task<LibraryMutation> RemoveProfileAsync(Guid profileId, CancellationToken cancellationToken = default) =>
        MutateAsync(library => LauncherLibraryRules.RemoveProfile(library, profileId), cancellationToken);

    /// <inheritdoc />
    public Task<LibraryMutation> SetActiveProfileAsync(Guid profileId, CancellationToken cancellationToken = default) =>
        MutateAsync(library => LauncherLibraryRules.SetActiveProfile(library, profileId), cancellationToken);

    /// <inheritdoc />
    public Task<LibraryMutation> AddExperienceAsync(string name, string link, CancellationToken cancellationToken = default) =>
        MutateAsync(library => LauncherLibraryRules.AddExperience(library, name, link, DateTimeOffset.Now), cancellationToken);

    /// <inheritdoc />
    public Task<LibraryMutation> RemoveExperienceAsync(Guid experienceId, CancellationToken cancellationToken = default) =>
        MutateAsync(library => LauncherLibraryRules.RemoveExperience(library, experienceId), cancellationToken);

    /// <inheritdoc />
    public Task<LibraryMutation> MarkExperienceOpenedAsync(Guid experienceId, CancellationToken cancellationToken = default) =>
        MutateAsync(library => LauncherLibraryRules.MarkExperienceOpened(library, experienceId, DateTimeOffset.Now), cancellationToken);

    private async Task<LibraryMutation> MutateAsync(
        Func<LauncherLibrary, LibraryMutation> mutation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(mutation);

        LibraryMutation result;

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            result = mutation(_current);

            if (!result.Succeeded)
            {
                _logger.Warn(Category, result.Error ?? "Modification de la bibliothèque refusée.");
                return result;
            }

            await _store.SaveAsync(result.Library, cancellationToken).ConfigureAwait(false);
            _current = result.Library;
        }
        finally
        {
            _gate.Release();
        }

        Changed?.Invoke(this, EventArgs.Empty);
        return result;
    }
}

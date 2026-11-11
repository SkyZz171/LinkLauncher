namespace LinkLauncher.Updater;

using LinkLauncher.Core.Abstractions;
using LinkLauncher.Core.Models;
using LinkLauncher.Core.Services;

/// <summary>Compare la version installée aux publications disponibles.</summary>
public sealed class UpdateChecker : IUpdateChecker
{
    private const string Category = "Mises à jour";

    private readonly IUpdateFeed _feed;
    private readonly IApplicationVersionProvider _versionProvider;
    private readonly IAppLogger _logger;

    /// <summary>Crée le vérificateur de mise à jour.</summary>
    public UpdateChecker(
        IUpdateFeed feed,
        IApplicationVersionProvider versionProvider,
        IAppLogger logger)
    {
        _feed = feed ?? throw new ArgumentNullException(nameof(feed));
        _versionProvider = versionProvider ?? throw new ArgumentNullException(nameof(versionProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<UpdateCheckResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        if (!AppVersion.TryParse(_versionProvider.Version, out var current))
        {
            _logger.Warn(Category, $"Version installée illisible : {_versionProvider.Version}.");
            return UpdateCheckResult.Unavailable("La version installée n'a pas pu être interprétée.");
        }

        IReadOnlyList<UpdateRelease> releases;

        try
        {
            releases = await _feed.GetReleasesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.Warn(Category, $"Recherche de mise à jour impossible : {ex.Message}");
            return UpdateCheckResult.Unavailable(
                "Impossible de joindre le dépôt des publications. Vérifiez votre connexion puis réessayez.");
        }

        var result = UpdateRules.Evaluate(current, releases);

        if (result.Outcome == UpdateCheckOutcome.UpdateAvailable)
        {
            _logger.Info(Category, $"Version {result.Release!.Version} disponible.");
        }

        return result;
    }
}

namespace LinkLauncher.Core.Abstractions;

using LinkLauncher.Core.Models;

/// <summary>Source des versions publiées de LinkLauncher.</summary>
public interface IUpdateFeed
{
    /// <summary>Liste les publications disponibles.</summary>
    /// <param name="cancellationToken">Jeton d'annulation.</param>
    Task<IReadOnlyList<UpdateRelease>> GetReleasesAsync(CancellationToken cancellationToken = default);
}

/// <summary>Recherche la dernière version publiée et la compare à la version installée.</summary>
public interface IUpdateChecker
{
    /// <summary>Recherche une mise à jour.</summary>
    /// <param name="cancellationToken">Jeton d'annulation.</param>
    Task<UpdateCheckResult> CheckAsync(CancellationToken cancellationToken = default);
}

/// <summary>Télécharge et vérifie un paquet de mise à jour.</summary>
public interface IUpdateDownloader
{
    /// <summary>Télécharge la publication et vérifie son empreinte SHA-256.</summary>
    /// <param name="release">Publication à télécharger.</param>
    /// <param name="cancellationToken">Jeton d'annulation.</param>
    Task<UpdateDownloadResult> DownloadAsync(UpdateRelease release, CancellationToken cancellationToken = default);
}

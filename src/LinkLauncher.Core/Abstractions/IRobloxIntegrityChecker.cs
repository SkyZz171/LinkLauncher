namespace LinkLauncher.Core.Abstractions;

using LinkLauncher.Core.Models;

/// <summary>Vérifie la présence des fichiers indispensables au lancement du client Roblox.</summary>
public interface IRobloxIntegrityChecker
{
    /// <summary>Examine le dossier de version d'une installation détectée.</summary>
    /// <param name="installation">Installation à vérifier, telle que retournée par la détection.</param>
    /// <param name="cancellationToken">Jeton d'annulation.</param>
    Task<RobloxIntegrityReport> CheckAsync(RobloxInstallation installation, CancellationToken cancellationToken = default);
}

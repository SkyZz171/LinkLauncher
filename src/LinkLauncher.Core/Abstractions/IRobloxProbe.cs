namespace LinkLauncher.Core.Abstractions;

using LinkLauncher.Core.Models;

/// <summary>Détection de l'installation Roblox présente sur la machine.</summary>
public interface IRobloxProbe
{
    /// <summary>Examine la machine et décrit l'installation Roblox constatée.</summary>
    /// <param name="cancellationToken">Jeton d'annulation.</param>
    Task<RobloxInstallation> ProbeAsync(CancellationToken cancellationToken = default);
}

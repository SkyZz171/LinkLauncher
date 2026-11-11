namespace LinkLauncher.Core.Abstractions;

using LinkLauncher.Core.Models;

/// <summary>
/// Lance le client Roblox par les mécanismes officiels enregistrés par Roblox.
/// </summary>
/// <remarks>
/// Le lancement s'appuie uniquement sur le protocole officiel <c>roblox-player</c> et, en
/// dernier recours, sur les pages officielles de Roblox. Aucun identifiant, cookie ou jeton
/// Roblox n'est lu, demandé ou transmis : rejoindre une expérience relève du navigateur de
/// l'utilisateur, qui gère sa propre session.
/// </remarks>
public interface IRobloxLaunchService
{
    /// <summary>Lance le client Roblox officiel sans rejoindre d'expérience.</summary>
    /// <param name="cancellationToken">Jeton d'annulation.</param>
    Task<RobloxLaunchResult> LaunchClientAsync(CancellationToken cancellationToken = default);

    /// <summary>Ouvre la page de téléchargement officielle de Roblox dans le navigateur par défaut.</summary>
    RobloxLaunchResult OpenOfficialDownloadPage();
}

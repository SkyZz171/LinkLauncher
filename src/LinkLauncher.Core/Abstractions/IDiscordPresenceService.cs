namespace LinkLauncher.Core.Abstractions;

using LinkLauncher.Core.Models;

/// <summary>Publie l'activité de LinkLauncher sur un client Discord local.</summary>
/// <remarks>
/// La présence est purement locale : elle passe par le canal IPC de Discord installé sur la
/// machine. Aucune donnée de compte Roblox ni de session n'est transmise.
/// </remarks>
public interface IDiscordPresenceService
{
    /// <summary>État courant de la présence.</summary>
    DiscordPresenceState State { get; }

    /// <summary>Publie une activité, ou l'efface si <paramref name="activity"/> vaut <see langword="null"/>.</summary>
    /// <param name="activity">Activité à publier.</param>
    /// <param name="cancellationToken">Jeton d'annulation.</param>
    /// <returns><see langword="true"/> si l'activité a été transmise.</returns>
    Task<bool> TryPublishAsync(DiscordActivity? activity, CancellationToken cancellationToken = default);

    /// <summary>Ferme la connexion au client Discord.</summary>
    Task DisconnectAsync(CancellationToken cancellationToken = default);
}

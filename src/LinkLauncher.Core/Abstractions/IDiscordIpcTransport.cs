namespace LinkLauncher.Core.Abstractions;

/// <summary>Canal IPC vers un client Discord local, isolé pour les tests.</summary>
/// <remarks>Seuls des messages JSON du protocole local sont échangés : aucune connexion réseau.</remarks>
public interface IDiscordIpcTransport : IDisposable
{
    /// <summary>Indique si le canal est ouvert.</summary>
    bool IsConnected { get; }

    /// <summary>Ouvre le canal et effectue la poignée de main du protocole Discord.</summary>
    /// <param name="applicationId">Identifiant d'application Discord.</param>
    /// <param name="cancellationToken">Jeton d'annulation.</param>
    Task<bool> ConnectAsync(string applicationId, CancellationToken cancellationToken = default);

    /// <summary>Transmet une trame déjà encodée.</summary>
    /// <param name="frame">Trame à envoyer.</param>
    /// <param name="cancellationToken">Jeton d'annulation.</param>
    Task<bool> TrySendAsync(byte[] frame, CancellationToken cancellationToken = default);

    /// <summary>Ferme le canal.</summary>
    Task DisconnectAsync(CancellationToken cancellationToken = default);
}

namespace LinkLauncher.Core.Abstractions;

/// <summary>Lecture de l'association d'un protocole Windows.</summary>
public interface IProtocolRegistrationReader
{
    /// <summary>
    /// Renvoie la commande d'ouverture associée à un protocole, ou <see langword="null"/>
    /// si le protocole n'est pas enregistré pour l'utilisateur courant.
    /// </summary>
    /// <param name="protocolScheme">Nom du protocole, par exemple <c>roblox-player</c>.</param>
    string? TryGetCommand(string protocolScheme);
}

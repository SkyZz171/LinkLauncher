namespace LinkLauncher.Core.Models;

/// <summary>Résultat observé d'une demande de lancement ou d'ouverture officielle.</summary>
public enum RobloxLaunchOutcome
{
    /// <summary>Le lancement a été demandé et relayé au mécanisme officiel.</summary>
    Launched = 0,

    /// <summary>Le client Roblox est déjà en cours d'exécution.</summary>
    AlreadyRunning = 1,

    /// <summary>Aucune installation Roblox exploitable n'a été trouvée.</summary>
    InstallationNotUsable = 2,

    /// <summary>Le protocole officiel <c>roblox-player</c> n'est pas enregistré pour cet utilisateur.</summary>
    ProtocolNotRegistered = 3,

    /// <summary>Windows n'a pas pu exécuter la demande.</summary>
    ShellFailure = 4,

    /// <summary>L'opération a été annulée.</summary>
    Cancelled = 5
}

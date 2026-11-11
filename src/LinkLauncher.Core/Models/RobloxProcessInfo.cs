namespace LinkLauncher.Core.Models;

/// <summary>Description d'un processus du client Roblox observé sur la machine.</summary>
public sealed record RobloxProcessInfo
{
    /// <summary>Identifiant du processus.</summary>
    public int ProcessId { get; init; }

    /// <summary>Nom du processus observé.</summary>
    public string ProcessName { get; init; } = "RobloxPlayerBeta";

    /// <summary>Date de démarrage, si elle est accessible.</summary>
    public DateTimeOffset? StartedAt { get; init; }

    /// <summary>Chemin de l'exécutable, si l'accès est autorisé.</summary>
    public string? ExecutablePath { get; init; }

    /// <summary>Code de sortie, renseigné uniquement après la fin du processus.</summary>
    public int? ExitCode { get; init; }
}

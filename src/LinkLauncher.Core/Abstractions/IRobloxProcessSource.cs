namespace LinkLauncher.Core.Abstractions;

using LinkLauncher.Core.Models;

/// <summary>Lecture des processus Roblox en cours d'exécution.</summary>
public interface IRobloxProcessSource
{
    /// <summary>Renvoie les processus du client Roblox actuellement en cours d'exécution.</summary>
    IReadOnlyList<RobloxProcessInfo> GetRunningProcesses();

    /// <summary>Renvoie le code de sortie d'un processus terminé, ou <see langword="null"/> s'il est inconnu.</summary>
    /// <param name="processId">Identifiant du processus.</param>
    int? TryGetExitCode(int processId);
}

namespace LinkLauncher.Core.Services;

using LinkLauncher.Core.Models;

/// <summary>
/// Compare deux relevés de processus Roblox et produit les transitions observées.
/// </summary>
/// <remarks>
/// Classe sans état système : elle est alimentée par un relevé et reste donc entièrement testable.
/// </remarks>
public sealed class RobloxProcessTracker
{
    private readonly Dictionary<int, RobloxProcessInfo> _knownProcesses = [];

    /// <summary>Processus courant, choisi parmi les processus en cours d'exécution.</summary>
    public RobloxProcessInfo? Current { get; private set; }

    /// <summary>Compare un relevé aux processus déjà connus.</summary>
    /// <param name="runningProcesses">Processus actuellement en cours d'exécution.</param>
    /// <param name="exitCodeLookup">Fonction renvoyant le code de sortie d'un processus terminé.</param>
    /// <returns>Transitions observées depuis le relevé précédent.</returns>
    public IReadOnlyList<RobloxProcessTransition> Update(
        IReadOnlyList<RobloxProcessInfo> runningProcesses,
        Func<int, int?> exitCodeLookup)
    {
        ArgumentNullException.ThrowIfNull(runningProcesses);
        ArgumentNullException.ThrowIfNull(exitCodeLookup);

        var transitions = new List<RobloxProcessTransition>();
        var runningIds = new HashSet<int>(runningProcesses.Select(process => process.ProcessId));

        foreach (var process in runningProcesses)
        {
            if (!_knownProcesses.ContainsKey(process.ProcessId))
            {
                transitions.Add(new RobloxProcessTransition(RobloxProcessChange.Started, process));
            }

            _knownProcesses[process.ProcessId] = process;
        }

        foreach (var processId in _knownProcesses.Keys.Where(id => !runningIds.Contains(id)).ToArray())
        {
            var known = _knownProcesses[processId];
            _knownProcesses.Remove(processId);

            transitions.Add(new RobloxProcessTransition(
                RobloxProcessChange.Stopped,
                known with { ExitCode = exitCodeLookup(processId) }));
        }

        Current = runningProcesses.Count == 0
            ? null
            : runningProcesses
                .OrderByDescending(process => process.StartedAt ?? DateTimeOffset.MinValue)
                .First();

        return transitions;
    }

    /// <summary>Oublie les processus connus, par exemple à l'arrêt de la surveillance.</summary>
    public void Reset()
    {
        _knownProcesses.Clear();
        Current = null;
    }
}

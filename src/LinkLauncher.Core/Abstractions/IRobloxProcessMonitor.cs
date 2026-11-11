namespace LinkLauncher.Core.Abstractions;

using LinkLauncher.Core.Models;

/// <summary>
/// Surveille l'exécution du client Roblox et signale les démarrages et arrêts.
/// </summary>
/// <remarks>
/// La surveillance se limite à la présence du processus et à son code de sortie :
/// aucune mémoire, aucun module et aucun fil du client ne sont examinés ou modifiés.
/// </remarks>
public interface IRobloxProcessMonitor : IDisposable
{
    /// <summary>Processus courant, ou <see langword="null"/> si le client n'est pas lancé.</summary>
    RobloxProcessInfo? Current { get; }

    /// <summary>Survient lors d'un démarrage ou d'un arrêt du client.</summary>
    event EventHandler<RobloxProcessChangedEventArgs>? Changed;

    /// <summary>Démarre la surveillance.</summary>
    void Start();

    /// <summary>Arrête la surveillance.</summary>
    void Stop();
}

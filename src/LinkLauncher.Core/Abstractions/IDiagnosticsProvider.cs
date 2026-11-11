namespace LinkLauncher.Core.Abstractions;

using LinkLauncher.Core.Models;

/// <summary>Construit un rapport de diagnostic de l'environnement réel.</summary>
public interface IDiagnosticsProvider
{
    /// <summary>Collecte l'état courant de l'application, de la machine et de Roblox.</summary>
    /// <param name="cancellationToken">Jeton d'annulation.</param>
    Task<DiagnosticsReport> CreateAsync(CancellationToken cancellationToken = default);
}

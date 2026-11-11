namespace LinkLauncher.Core.Abstractions;

/// <summary>Résultat d'une demande d'exécution adressée à Windows.</summary>
/// <param name="Succeeded">Indique que Windows a accepté la demande.</param>
/// <param name="ErrorMessage">Message d'erreur technique, le cas échéant.</param>
public sealed record ShellLaunchResult(bool Succeeded, string? ErrorMessage = null);

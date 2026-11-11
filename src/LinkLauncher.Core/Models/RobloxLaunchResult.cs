namespace LinkLauncher.Core.Models;

/// <summary>
/// Résultat d'une demande de lancement. Ne contient aucun message destiné à l'interface :
/// la mise en forme relève de la couche de présentation.
/// </summary>
public sealed record RobloxLaunchResult
{
    /// <summary>Résultat observé.</summary>
    public RobloxLaunchOutcome Outcome { get; init; } = RobloxLaunchOutcome.ShellFailure;

    /// <summary>Détail technique, par exemple le message d'erreur de Windows.</summary>
    public string? TechnicalDetail { get; init; }

    /// <summary>Indique si la demande a été acceptée par le système.</summary>
    public bool IsSuccess => Outcome is RobloxLaunchOutcome.Launched or RobloxLaunchOutcome.AlreadyRunning;

    /// <summary>Crée un résultat signifiant que la demande a abouti.</summary>
    public static RobloxLaunchResult Success(RobloxLaunchOutcome outcome) => new() { Outcome = outcome };

    /// <summary>Crée un résultat d'échec avec son détail technique.</summary>
    public static RobloxLaunchResult Failure(RobloxLaunchOutcome outcome, string? technicalDetail = null) =>
        new() { Outcome = outcome, TechnicalDetail = technicalDetail };
}

namespace LinkLauncher.Features.Dashboard;

/// <summary>Nature de l'état affiché par le tableau de bord.</summary>
public enum DashboardStatusKind
{
    /// <summary>Vérification en cours.</summary>
    Checking = 0,

    /// <summary>Installation utilisable.</summary>
    Ready = 1,

    /// <summary>Installation absente ou incomplète.</summary>
    Warning = 2,

    /// <summary>Vérification impossible.</summary>
    Error = 3
}

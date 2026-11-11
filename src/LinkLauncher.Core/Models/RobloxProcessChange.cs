namespace LinkLauncher.Core.Models;

/// <summary>Nature d'un changement observé sur les processus Roblox.</summary>
public enum RobloxProcessChange
{
    /// <summary>Un processus est apparu.</summary>
    Started = 0,

    /// <summary>Un processus suivi a disparu.</summary>
    Stopped = 1
}

/// <summary>Transition observée entre deux relevés de processus.</summary>
/// <param name="Change">Nature du changement.</param>
/// <param name="Process">Processus concerné.</param>
public sealed record RobloxProcessTransition(RobloxProcessChange Change, RobloxProcessInfo Process);

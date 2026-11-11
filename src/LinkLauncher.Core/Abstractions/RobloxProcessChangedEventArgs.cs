namespace LinkLauncher.Core.Abstractions;

using LinkLauncher.Core.Models;

/// <summary>Argument d'un changement observé sur les processus Roblox.</summary>
public sealed class RobloxProcessChangedEventArgs : EventArgs
{
    /// <summary>Crée l'argument du changement.</summary>
    /// <param name="transition">Transition observée.</param>
    /// <param name="current">Processus courant après le changement, ou <see langword="null"/>.</param>
    public RobloxProcessChangedEventArgs(RobloxProcessTransition transition, RobloxProcessInfo? current)
    {
        Transition = transition;
        Current = current;
    }

    /// <summary>Transition observée.</summary>
    public RobloxProcessTransition Transition { get; }

    /// <summary>Processus courant après le changement.</summary>
    public RobloxProcessInfo? Current { get; }
}

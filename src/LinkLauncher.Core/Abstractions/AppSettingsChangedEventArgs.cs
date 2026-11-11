namespace LinkLauncher.Core.Abstractions;

using LinkLauncher.Core.Models;

/// <summary>Argument d'un changement de configuration.</summary>
public sealed class AppSettingsChangedEventArgs : EventArgs
{
    /// <summary>Crée l'argument du changement.</summary>
    /// <param name="previous">Configuration avant modification.</param>
    /// <param name="current">Configuration après modification.</param>
    public AppSettingsChangedEventArgs(AppSettings previous, AppSettings current)
    {
        Previous = previous;
        Current = current;
    }

    /// <summary>Configuration avant modification.</summary>
    public AppSettings Previous { get; }

    /// <summary>Configuration après modification.</summary>
    public AppSettings Current { get; }
}

namespace LinkLauncher.Features.Navigation;

/// <summary>Argument d'un changement de page.</summary>
public sealed class NavigationChangedEventArgs : EventArgs
{
    /// <summary>Crée l'argument du changement de page.</summary>
    /// <param name="route">Route affichée.</param>
    /// <param name="page">Page affichée.</param>
    public NavigationChangedEventArgs(string route, object page)
    {
        Route = route;
        Page = page;
    }

    /// <summary>Route affichée.</summary>
    public string Route { get; }

    /// <summary>Page affichée.</summary>
    public object Page { get; }
}

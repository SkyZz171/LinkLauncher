namespace LinkLauncher.Features.Navigation;

/// <summary>Navigation centralisée entre les pages de l'application.</summary>
public interface INavigationService
{
    /// <summary>Route actuellement affichée, ou <see langword="null"/> tant qu'aucune page n'est ouverte.</summary>
    string? CurrentRoute { get; }

    /// <summary>Page actuellement affichée.</summary>
    object? CurrentPage { get; }

    /// <summary>Survient à chaque changement de page.</summary>
    event EventHandler<NavigationChangedEventArgs>? Navigated;

    /// <summary>Affiche la page associée à une route.</summary>
    /// <param name="route">Route de navigation.</param>
    void NavigateTo(string route);
}

namespace LinkLauncher.Features.Navigation;

/// <summary>Crée le ViewModel associé à une route.</summary>
public interface INavigationPageFactory
{
    /// <summary>Crée la page demandée.</summary>
    /// <param name="route">Route de navigation.</param>
    object CreatePage(string route);
}

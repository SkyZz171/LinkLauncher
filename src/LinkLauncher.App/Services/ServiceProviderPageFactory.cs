namespace LinkLauncher.App.Services;

using LinkLauncher.Features.Dashboard;
using LinkLauncher.Features.Navigation;
using LinkLauncher.Features.Settings;
using Microsoft.Extensions.DependencyInjection;

/// <summary>Résout les ViewModels de page depuis le conteneur d'injection de dépendances.</summary>
public sealed class ServiceProviderPageFactory : INavigationPageFactory
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>Crée la fabrique de pages.</summary>
    public ServiceProviderPageFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <inheritdoc />
    public object CreatePage(string route) => route switch
    {
        NavigationRoutes.Dashboard => _serviceProvider.GetRequiredService<DashboardViewModel>(),
        NavigationRoutes.Settings => _serviceProvider.GetRequiredService<SettingsViewModel>(),
        _ => throw new ArgumentOutOfRangeException(nameof(route), route, "Route de navigation inconnue.")
    };
}

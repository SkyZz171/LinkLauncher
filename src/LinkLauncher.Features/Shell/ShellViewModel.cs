namespace LinkLauncher.Features.Shell;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LinkLauncher.Core.Abstractions;
using LinkLauncher.Features.Common;
using LinkLauncher.Features.Navigation;

/// <summary>Coordonne la navigation, la barre latérale et la page affichée.</summary>
public sealed partial class ShellViewModel : ObservableObject
{
    private const string Category = "Navigation";
    private const double CompactThreshold = 960;

    private readonly INavigationService _navigation;
    private readonly IAppLogger _logger;

    /// <summary>Crée le ViewModel de la coque de l'application.</summary>
    public ShellViewModel(
        INavigationService navigation,
        IApplicationVersionProvider versionProvider,
        IAppLogger logger)
    {
        _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        ArgumentNullException.ThrowIfNull(versionProvider);

        AppVersion = versionProvider.Version;
        Items =
        [
            new NavigationItem(NavigationRoutes.Dashboard, "Dashboard", "\uE80F", "Ctrl+1"),
            new NavigationItem(NavigationRoutes.Library, "Bibliothèque", "\uE8F1", "Ctrl+2"),
            new NavigationItem(NavigationRoutes.Settings, "Paramètres", "\uE713", "Ctrl+3")
        ];

        _navigation.Navigated += OnNavigated;
    }

    /// <summary>Éléments de la barre latérale.</summary>
    public ObservableCollection<NavigationItem> Items { get; }

    /// <summary>Version de LinkLauncher affichée dans la barre latérale.</summary>
    public string AppVersion { get; }

    /// <summary>Page affichée.</summary>
    [ObservableProperty]
    private object? _currentPage;

    /// <summary>Titre de la page affichée.</summary>
    [ObservableProperty]
    private string _pageTitle = "LinkLauncher";

    /// <summary>Indique que la fenêtre est trop étroite pour afficher les libellés de navigation.</summary>
    [ObservableProperty]
    private bool _isCompact;

    /// <summary>Affiche la page d'accueil et charge ses données.</summary>
    /// <param name="cancellationToken">Jeton d'annulation.</param>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        Navigate(NavigationRoutes.Dashboard);
        await InitializeCurrentPageAsync(cancellationToken);
    }

    /// <summary>Adapte la présentation à la largeur de la fenêtre.</summary>
    /// <param name="width">Largeur courante en pixels indépendants.</param>
    public void UpdateLayoutWidth(double width) => IsCompact = width < CompactThreshold;

    [RelayCommand]
    private async Task NavigateAsync(NavigationItem? item)
    {
        if (item is null)
        {
            return;
        }

        Navigate(item.Route);
        await InitializeCurrentPageAsync();
    }

    [RelayCommand]
    private async Task NavigateToRouteAsync(string? route)
    {
        if (string.IsNullOrWhiteSpace(route))
        {
            return;
        }

        Navigate(route);
        await InitializeCurrentPageAsync();
    }

    private void Navigate(string route) => _navigation.NavigateTo(route);

    private async Task InitializeCurrentPageAsync(CancellationToken cancellationToken = default)
    {
        if (CurrentPage is not IAsyncPage page)
        {
            return;
        }

        try
        {
            await page.InitializeAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.Error(Category, "Initialisation de la page impossible.", ex);
        }
    }

    private void OnNavigated(object? sender, NavigationChangedEventArgs e)
    {
        CurrentPage = e.Page;
        PageTitle = Items.FirstOrDefault(item => item.Route == e.Route)?.Label ?? "LinkLauncher";

        foreach (var item in Items)
        {
            item.IsSelected = string.Equals(item.Route, e.Route, StringComparison.Ordinal);
        }
    }
}

namespace LinkLauncher.Features.Navigation;

/// <summary>
/// Navigation par route conservant l'état des pages déjà visitées.
/// </summary>
public sealed class NavigationService : INavigationService
{
    private readonly INavigationPageFactory _pageFactory;
    private readonly Dictionary<string, object> _pages = new(StringComparer.Ordinal);
    private string? _currentRoute;

    /// <summary>Crée le service de navigation.</summary>
    /// <param name="pageFactory">Fabrique des pages.</param>
    public NavigationService(INavigationPageFactory pageFactory)
    {
        _pageFactory = pageFactory ?? throw new ArgumentNullException(nameof(pageFactory));
    }

    /// <inheritdoc />
    public string? CurrentRoute => _currentRoute;

    /// <inheritdoc />
    public object? CurrentPage { get; private set; }

    /// <inheritdoc />
    public event EventHandler<NavigationChangedEventArgs>? Navigated;

    /// <inheritdoc />
    public void NavigateTo(string route)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(route);

        if (string.Equals(_currentRoute, route, StringComparison.Ordinal))
        {
            return;
        }

        if (!_pages.TryGetValue(route, out var page))
        {
            page = _pageFactory.CreatePage(route);
            _pages[route] = page;
        }

        _currentRoute = route;
        CurrentPage = page;
        Navigated?.Invoke(this, new NavigationChangedEventArgs(route, page));
    }
}

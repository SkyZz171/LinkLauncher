namespace LinkLauncher.App;

using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using LinkLauncher.App.Interop;
using LinkLauncher.Core.Abstractions;
using LinkLauncher.Features.Shell;

/// <summary>Fenêtre principale : chrome personnalisé et hébergement de la page courante.</summary>
public partial class MainWindow : Window
{
    private const string Category = "Interface";

    private readonly ShellViewModel _viewModel;
    private readonly IThemeService _themeService;
    private readonly IAppLogger _logger;

    /// <summary>Crée la fenêtre principale.</summary>
    public MainWindow(ShellViewModel viewModel, IThemeService themeService, IAppLogger logger)
    {
        InitializeComponent();

        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        DataContext = _viewModel;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        _themeService.ThemeApplied += OnThemeApplied;

        SourceInitialized += OnSourceInitialized;
        Loaded += OnLoaded;
        SizeChanged += OnSizeChanged;
        StateChanged += OnStateChanged;
        Closed += OnClosed;
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        WindowInterop.EnableWorkAreaClamp(this);
        WindowInterop.SetRoundedCorners(this, WindowState != WindowState.Maximized);
        WindowInterop.SetDarkBorder(this, _themeService.AppliedTheme);
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            _viewModel.UpdateLayoutWidth(ActualWidth);
            await _viewModel.StartAsync();
        }
        catch (Exception ex)
        {
            _logger.Error(Category, "Initialisation de la fenêtre principale impossible.", ex);
        }
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e) => _viewModel.UpdateLayoutWidth(e.NewSize.Width);

    private void OnStateChanged(object? sender, EventArgs e)
    {
        var isMaximized = WindowState == WindowState.Maximized;

        MaximizeRestoreButton.Content = isMaximized ? "\uE923" : "\uE922";
        MaximizeRestoreButton.ToolTip = isMaximized ? "Restaurer la fenêtre" : "Agrandir la fenêtre";
        MaximizeRestoreButton.SetValue(System.Windows.Automation.AutomationProperties.NameProperty, isMaximized ? "Restaurer" : "Agrandir");

        WindowInterop.SetRoundedCorners(this, !isMaximized);
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ShellViewModel.CurrentPage))
        {
            PlayPageTransition();
        }
    }

    private void OnThemeApplied(object? sender, EventArgs e) =>
        WindowInterop.SetDarkBorder(this, _themeService.AppliedTheme);

    private void PlayPageTransition()
    {
        if (TryFindResource("PageTransition") is Storyboard storyboard)
        {
            storyboard.Begin(PageHost);
        }
    }

    private void OnTitleBarMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            ToggleMaximizeRestore();
            return;
        }

        if (e.ButtonState != MouseButtonState.Pressed)
        {
            return;
        }

        try
        {
            DragMove();
        }
        catch (InvalidOperationException ex)
        {
            // Le déplacement peut être refusé lorsque le bouton a déjà été relâché.
            _logger.Log(AppLogLevel.Debug, Category, $"Déplacement de la fenêtre interrompu : {ex.Message}");
        }
    }

    private void OnMinimizeClick(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void OnMaximizeRestoreClick(object sender, RoutedEventArgs e) => ToggleMaximizeRestore();

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();

    private void ToggleMaximizeRestore() =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void OnClosed(object? sender, EventArgs e)
    {
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        _themeService.ThemeApplied -= OnThemeApplied;
        WindowInterop.DisableWorkAreaClamp(this);
    }
}

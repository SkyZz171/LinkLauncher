namespace LinkLauncher.App;

using System.Windows;
using LinkLauncher.App.Services;
using LinkLauncher.Core.Abstractions;
using LinkLauncher.Core.Services;
using LinkLauncher.Features.Dashboard;
using LinkLauncher.Features.Navigation;
using LinkLauncher.Features.Settings;
using LinkLauncher.Features.Shell;
using LinkLauncher.Infrastructure.FileSystem;
using LinkLauncher.Infrastructure.Logging;
using LinkLauncher.Infrastructure.Roblox;
using LinkLauncher.Infrastructure.Settings;
using LinkLauncher.Infrastructure.Windows;
using Microsoft.Extensions.DependencyInjection;

/// <summary>Point de composition de l'application et gestion des erreurs globales.</summary>
public partial class App : Application
{
    private const string Category = "Application";

    private ServiceProvider? _serviceProvider;
    private FileAppLogger? _logger;

    /// <inheritdoc />
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var paths = new WindowsAppPaths();
        var logger = new FileAppLogger(paths.LogsDirectory);
        _logger = logger;

        AttachExceptionHandlers(logger);
        logger.Info(Category, $"Démarrage de LinkLauncher {typeof(App).Assembly.GetName().Version}.");

        try
        {
            _serviceProvider = BuildServiceProvider(paths, logger);

            var settings = _serviceProvider.GetRequiredService<ISettingsService>();
            await settings.InitializeAsync();

            var themeService = _serviceProvider.GetRequiredService<IThemeService>();
            var systemThemeSource = _serviceProvider.GetRequiredService<ISystemThemeSource>();
            themeService.Apply(
                ThemeResolver.ResolveEffective(settings.Current.Theme, systemThemeSource.GetSystemTheme()),
                settings.Current.AccentColor);

            var window = _serviceProvider.GetRequiredService<MainWindow>();
            MainWindow = window;
            window.Show();
        }
        catch (Exception ex)
        {
            logger.Log(AppLogLevel.Critical, Category, "Démarrage de l'application impossible.", ex);
            MessageBox.Show(
                "LinkLauncher n'a pas pu démarrer.\n\n" + ex.Message +
                "\n\nLe détail technique figure dans le journal d'activité : " + paths.LogsDirectory,
                "LinkLauncher",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            Shutdown(1);
        }
    }

    /// <inheritdoc />
    protected override void OnExit(ExitEventArgs e)
    {
        _logger?.Info(Category, "Arrêt de LinkLauncher.");
        _serviceProvider?.Dispose();
        _logger?.Dispose();

        base.OnExit(e);
    }

    private static ServiceProvider BuildServiceProvider(IAppPaths paths, IAppLogger logger)
    {
        var services = new ServiceCollection();

        services.AddSingleton(paths);
        services.AddSingleton(logger);

        services.AddSingleton<IFileSystem, SystemFileSystem>();
        services.AddSingleton<ISettingsStore, JsonSettingsStore>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<ISystemThemeSource, WindowsThemeSource>();
        services.AddSingleton<IProtocolRegistrationReader, RegistryProtocolReader>();
        services.AddSingleton<IExecutableVersionReader, WindowsExecutableVersionReader>();
        services.AddSingleton<IFolderLauncher, ExplorerFolderLauncher>();
        services.AddSingleton<IThemeService, WpfThemeService>();
        services.AddSingleton<IApplicationVersionProvider, ApplicationVersionProvider>();

        services.AddSingleton<IRobloxProbe>(provider => new WindowsRobloxProbe(
            provider.GetRequiredService<IFileSystem>(),
            provider.GetRequiredService<IExecutableVersionReader>(),
            provider.GetRequiredService<IProtocolRegistrationReader>(),
            provider.GetRequiredService<IAppLogger>(),
            WindowsRobloxProbe.DefaultRobloxRootPath));

        services.AddTransient<DashboardViewModel>();
        services.AddTransient<SettingsViewModel>();

        services.AddSingleton<INavigationPageFactory, ServiceProviderPageFactory>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<ShellViewModel>();
        services.AddSingleton<MainWindow>();

        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });
    }

    private void AttachExceptionHandlers(IAppLogger logger)
    {
        DispatcherUnhandledException += (_, args) =>
        {
            logger.Log(AppLogLevel.Error, "Interface", "Exception non gérée dans l'interface.", args.Exception);
            MessageBox.Show(
                "Une erreur inattendue s'est produite dans l'interface. LinkLauncher reste ouvert ; " +
                "le détail technique est enregistré dans le journal d'activité.",
                "LinkLauncher",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            args.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            logger.Log(AppLogLevel.Critical, Category, "Exception non gérée.", args.ExceptionObject as Exception);

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            logger.Log(AppLogLevel.Error, Category, "Tâche asynchrone non observée.", args.Exception);
            args.SetObserved();
        };
    }
}

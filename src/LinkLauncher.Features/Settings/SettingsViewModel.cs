namespace LinkLauncher.Features.Settings;

using CommunityToolkit.Mvvm.ComponentModel;
using LinkLauncher.Core.Abstractions;
using LinkLauncher.Core.Models;
using LinkLauncher.Core.Services;
using LinkLauncher.Features.Common;

/// <summary>Expose les paramètres réellement pris en charge.</summary>
public sealed partial class SettingsViewModel : ObservableObject, IAsyncPage
{
    private const string Category = "Paramètres";

    private readonly ISettingsService _settings;
    private readonly IThemeService _themeService;
    private readonly ISystemThemeSource _systemTheme;
    private readonly IAppLogger _logger;

    private bool _isRestoringSelection;

    /// <summary>Crée le ViewModel des paramètres.</summary>
    public SettingsViewModel(
        ISettingsService settings,
        IThemeService themeService,
        ISystemThemeSource systemTheme,
        IAppPaths paths,
        IApplicationVersionProvider versionProvider,
        IAppLogger logger)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
        _systemTheme = systemTheme ?? throw new ArgumentNullException(nameof(systemTheme));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        ArgumentNullException.ThrowIfNull(paths);
        ArgumentNullException.ThrowIfNull(versionProvider);

        ThemeOptions =
        [
            new ThemeOption(AppTheme.System, "Système", "Suit le thème clair ou sombre configuré dans Windows."),
            new ThemeOption(AppTheme.Dark, "Sombre", "Interface sombre, adaptée aux environnements peu éclairés."),
            new ThemeOption(AppTheme.Light, "Clair", "Interface claire aux contrastes élevés.")
        ];

        AccentOptions =
        [
            new AccentOption(AccentColorKey.Aurora, "Aurore"),
            new AccentOption(AccentColorKey.Azure, "Azur"),
            new AccentOption(AccentColorKey.Emerald, "Émeraude"),
            new AccentOption(AccentColorKey.Amber, "Ambre"),
            new AccentOption(AccentColorKey.Coral, "Corail"),
            new AccentOption(AccentColorKey.Violet, "Violet")
        ];

        SettingsFilePath = paths.SettingsFilePath;
        AppVersion = versionProvider.Version;

        var current = _settings.Current;
        _selectedTheme = ResolveThemeOption(current.Theme);
        _selectedAccent = AccentOptions.First(option => option.Key == current.AccentColor);
    }

    /// <summary>Thèmes proposés.</summary>
    public IReadOnlyList<ThemeOption> ThemeOptions { get; }

    /// <summary>Couleurs d'accentuation proposées.</summary>
    public IReadOnlyList<AccentOption> AccentOptions { get; }

    /// <summary>Emplacement du fichier de configuration.</summary>
    public string SettingsFilePath { get; }

    /// <summary>Version de LinkLauncher.</summary>
    public string AppVersion { get; }

    /// <summary>Thème sélectionné.</summary>
    [ObservableProperty]
    private ThemeOption _selectedTheme = null!;

    /// <summary>Couleur d'accentuation sélectionnée.</summary>
    [ObservableProperty]
    private AccentOption _selectedAccent = null!;

    /// <summary>Message d'erreur présenté à l'utilisateur.</summary>
    [ObservableProperty]
    private string? _errorMessage;

    /// <inheritdoc />
    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _isRestoringSelection = true;
        SelectedTheme = ResolveThemeOption(_settings.Current.Theme);
        SelectedAccent = AccentOptions.First(option => option.Key == _settings.Current.AccentColor);
        _isRestoringSelection = false;

        return Task.CompletedTask;
    }

    partial void OnSelectedThemeChanged(ThemeOption value) => ApplyTheme(value);

    partial void OnSelectedAccentChanged(AccentOption value) => ApplyAccent(value);

    private void ApplyTheme(ThemeOption option)
    {
        if (_isRestoringSelection || option is null)
        {
            return;
        }

        ApplyAppearance(option.Value, _settings.Current.AccentColor, "enregistrement du thème");
    }

    private void ApplyAccent(AccentOption option)
    {
        if (_isRestoringSelection || option is null)
        {
            return;
        }

        ApplyAppearance(_settings.Current.Theme, option.Key, "enregistrement de la couleur d'accentuation");
    }

    private void ApplyAppearance(AppTheme requestedTheme, AccentColorKey accent, string operationName)
    {
        ErrorMessage = null;

        try
        {
            var effectiveTheme = ThemeResolver.ResolveEffective(requestedTheme, _systemTheme.GetSystemTheme());
            _themeService.Apply(effectiveTheme, accent);
        }
        catch (Exception ex)
        {
            _logger.Error(Category, $"Application du thème impossible ({operationName}).", ex);
            ErrorMessage = "L'apparence n'a pas pu être appliquée. Le détail technique figure dans le journal d'activité.";
            RestoreSelection();
            return;
        }

        PersistAsync(requestedTheme, accent, operationName);
    }

    private async void PersistAsync(AppTheme theme, AccentColorKey accent, string operationName)
    {
        try
        {
            await _settings.UpdateAsync(settings => settings with { Theme = theme, AccentColor = accent });
            _logger.Info(Category, $"Préférences enregistrées : thème={theme}, accent={accent} ({operationName}).");
        }
        catch (Exception ex)
        {
            _logger.Error(Category, $"Enregistrement des préférences impossible ({operationName}).", ex);
            ErrorMessage = "La préférence n'a pas pu être enregistrée sur le disque. L'apparence précédente est restaurée.";
            RestoreSelection();
        }
    }

    private void RestoreSelection()
    {
        var current = _settings.Current;

        _isRestoringSelection = true;
        try
        {
            SelectedTheme = ResolveThemeOption(current.Theme);
            SelectedAccent = AccentOptions.First(option => option.Key == current.AccentColor);
        }
        finally
        {
            _isRestoringSelection = false;
        }

        var effectiveTheme = ThemeResolver.ResolveEffective(current.Theme, _systemTheme.GetSystemTheme());
        _themeService.Apply(effectiveTheme, current.AccentColor);
    }

    private ThemeOption ResolveThemeOption(AppTheme theme) =>
        ThemeOptions.FirstOrDefault(option => option.Value == theme) ?? ThemeOptions[1];
}

namespace LinkLauncher.Features.Settings;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LinkLauncher.Core.Abstractions;
using LinkLauncher.Core.Models;
using LinkLauncher.Core.Services;
using LinkLauncher.Features.Common;
using LinkLauncher.Features.Updates;

/// <summary>Expose les paramètres réellement pris en charge.</summary>
public sealed partial class SettingsViewModel : ObservableObject, IAsyncPage
{
    private const string Category = "Paramètres";

    private readonly ISettingsService _settings;
    private readonly IThemeService _themeService;
    private readonly ISystemThemeSource _systemTheme;
    private readonly IDiagnosticsProvider _diagnosticsProvider;
    private readonly IFolderLauncher _folderLauncher;
    private readonly IClipboardService _clipboard;
    private readonly IDiscordPresenceService _discordPresence;
    private readonly IAppLogger _logger;

    private bool _isRestoringSelection;
    private DiagnosticsReport? _report;

    /// <summary>Crée le ViewModel des paramètres.</summary>
    public SettingsViewModel(
        ISettingsService settings,
        IThemeService themeService,
        ISystemThemeSource systemTheme,
        IAppPaths paths,
        IDiagnosticsProvider diagnosticsProvider,
        IFolderLauncher folderLauncher,
        IClipboardService clipboard,
        IDiscordPresenceService discordPresence,
        UpdateViewModel updates,
        IApplicationVersionProvider versionProvider,
        IAppLogger logger)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
        _systemTheme = systemTheme ?? throw new ArgumentNullException(nameof(systemTheme));
        _diagnosticsProvider = diagnosticsProvider ?? throw new ArgumentNullException(nameof(diagnosticsProvider));
        _folderLauncher = folderLauncher ?? throw new ArgumentNullException(nameof(folderLauncher));
        _clipboard = clipboard ?? throw new ArgumentNullException(nameof(clipboard));
        _discordPresence = discordPresence ?? throw new ArgumentNullException(nameof(discordPresence));
        Updates = updates ?? throw new ArgumentNullException(nameof(updates));
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
        LogsDirectory = paths.LogsDirectory;
        AppVersion = versionProvider.Version;

        var current = _settings.Current;
        _selectedTheme = ResolveThemeOption(current.Theme);
        _selectedAccent = AccentOptions.First(option => option.Key == current.AccentColor);
        _enableNotifications = current.EnableNotifications;
        _enableDiscordPresence = current.EnableDiscordPresence;
        _discordApplicationId = current.DiscordApplicationId ?? string.Empty;
    }

    /// <summary>Thèmes proposés.</summary>
    public IReadOnlyList<ThemeOption> ThemeOptions { get; }

    /// <summary>Couleurs d'accentuation proposées.</summary>
    public IReadOnlyList<AccentOption> AccentOptions { get; }

    /// <summary>Lignes du rapport de diagnostic.</summary>
    public ObservableCollection<DiagnosticsRow> Diagnostics { get; } = [];

    /// <summary>Emplacement du fichier de configuration.</summary>
    public string SettingsFilePath { get; }

    /// <summary>Emplacement des journaux d'activité.</summary>
    public string LogsDirectory { get; }
    /// <summary>Carte des mises à jour.</summary>
    public UpdateViewModel Updates { get; }

    /// <summary>Version de LinkLauncher.</summary>
    public string AppVersion { get; }

    /// <summary>Thème sélectionné.</summary>
    [ObservableProperty]
    private ThemeOption _selectedTheme = null!;

    /// <summary>Couleur d'accentuation sélectionnée.</summary>
    [ObservableProperty]
    private AccentOption _selectedAccent = null!;

    /// <summary>Notifications activées.</summary>
    [ObservableProperty]
    private bool _enableNotifications;

    /// <summary>Présence Discord activée.</summary>
    [ObservableProperty]
    private bool _enableDiscordPresence;

    /// <summary>Identifiant d'application Discord saisi.</summary>
    [ObservableProperty]
    private string _discordApplicationId = string.Empty;

    /// <summary>État lisible de la présence Discord.</summary>
    [ObservableProperty]
    private string _discordStatusText = "Présence désactivée.";

    /// <summary>Indique qu'un rapport de diagnostic est en cours de collecte.</summary>
    [ObservableProperty]
    private bool _isDiagnosticsBusy;

    /// <summary>Résumé du dernier rapport de diagnostic.</summary>
    [ObservableProperty]
    private string _diagnosticsSummary = "Diagnostic non collecté.";

    /// <summary>Message d'erreur présenté à l'utilisateur.</summary>
    [ObservableProperty]
    private string? _errorMessage;

    /// <summary>Message de confirmation présenté à l'utilisateur.</summary>
    [ObservableProperty]
    private string? _statusMessage;

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var current = _settings.Current;

        _isRestoringSelection = true;
        try
        {
            SelectedTheme = ResolveThemeOption(current.Theme);
            SelectedAccent = AccentOptions.First(option => option.Key == current.AccentColor);
            EnableNotifications = current.EnableNotifications;
            EnableDiscordPresence = current.EnableDiscordPresence;
            DiscordApplicationId = current.DiscordApplicationId ?? string.Empty;
        }
        finally
        {
            _isRestoringSelection = false;
        }

        RefreshDiscordStatus();
        await RefreshDiagnosticsAsync(cancellationToken);
    }

    partial void OnSelectedThemeChanged(ThemeOption value) => ApplyTheme(value);

    partial void OnSelectedAccentChanged(AccentOption value) => ApplyAccent(value);

    partial void OnEnableNotificationsChanged(bool value) => PersistPreferenceAsync(
        settings => settings with { EnableNotifications = value },
        value ? "Notifications activées." : "Notifications désactivées.");

    partial void OnEnableDiscordPresenceChanged(bool value)
    {
        PersistPreferenceAsync(
            settings => settings with { EnableDiscordPresence = value },
            value ? "Présence Discord activée." : "Présence Discord désactivée.");

        RefreshDiscordStatus();
    }

    [RelayCommand]
    private async Task SaveDiscordApplicationIdAsync(CancellationToken cancellationToken)
    {
        var candidate = DiscordApplicationId.Trim();

        if (candidate.Length > 0 && DiscordApplicationIdRules.Normalize(candidate) is null)
        {
            ErrorMessage = DiscordApplicationIdRules.InvalidMessage;
            return;
        }

        var normalized = DiscordApplicationIdRules.Normalize(candidate);

        var saved = await PersistPreferenceCoreAsync(
            settings => settings with { DiscordApplicationId = normalized },
            normalized is null
                ? "Identifiant d'application Discord effacé."
                : "Identifiant d'application Discord enregistré.");

        if (saved)
        {
            DiscordApplicationId = normalized ?? string.Empty;
            RefreshDiscordStatus();
        }
    }

    [RelayCommand]
    private async Task RefreshDiagnosticsAsync(CancellationToken cancellationToken)
    {
        if (IsDiagnosticsBusy)
        {
            return;
        }

        IsDiagnosticsBusy = true;
        ErrorMessage = null;

        try
        {
            _report = await _diagnosticsProvider.CreateAsync(cancellationToken);
            Diagnostics.Clear();

            foreach (var row in BuildRows(_report))
            {
                Diagnostics.Add(row);
            }

            DiagnosticsSummary = $"Rapport généré à {_report.GeneratedAt:HH:mm:ss}.";
            _logger.Info(Category, "Rapport de diagnostic collecté.");
        }
        catch (OperationCanceledException)
        {
            DiagnosticsSummary = "Collecte du diagnostic annulée.";
        }
        catch (Exception ex)
        {
            _logger.Error(Category, "Collecte du diagnostic impossible.", ex);
            ErrorMessage = "Le rapport de diagnostic n'a pas pu être collecté. Le détail technique figure dans le journal d'activité.";
            DiagnosticsSummary = "Collecte impossible.";
        }
        finally
        {
            IsDiagnosticsBusy = false;
        }
    }

    [RelayCommand]
    private void OpenLogsFolder()
    {
        if (!_folderLauncher.TryOpenFolder(LogsDirectory))
        {
            ErrorMessage = "Le dossier des journaux n'a pas pu être ouvert.";
        }
    }

    [RelayCommand]
    private void CopyDiagnostics()
    {
        if (Diagnostics.Count == 0)
        {
            ErrorMessage = "Collectez d'abord un rapport de diagnostic.";
            return;
        }

        var text = string.Join(Environment.NewLine, Diagnostics.Select(row => $"{row.Label} : {row.Value}"));

        if (!_clipboard.TrySetText(text))
        {
            ErrorMessage = "Le rapport n'a pas pu être copié dans le presse-papiers.";
            return;
        }

        StatusMessage = "Rapport de diagnostic copié dans le presse-papiers.";
    }

    private static IEnumerable<DiagnosticsRow> BuildRows(DiagnosticsReport report)
    {
        yield return new DiagnosticsRow("LinkLauncher", report.LauncherVersion);
        yield return new DiagnosticsRow("Windows", report.OperatingSystem);
        yield return new DiagnosticsRow("Moteur .NET", report.Runtime);
        yield return new DiagnosticsRow("Architecture", report.Architecture);
        yield return new DiagnosticsRow("Configuration", report.SettingsFilePath);
        yield return new DiagnosticsRow("Bibliothèque", report.LibraryFilePath);
        yield return new DiagnosticsRow("Journaux", report.LogsDirectory);
        yield return new DiagnosticsRow("Dernier journal", report.LatestLogFilePath ?? "—");
        yield return new DiagnosticsRow("Version Roblox", report.RobloxVersion);
        yield return new DiagnosticsRow("Dossier Roblox", report.RobloxVersionFolder);
        yield return new DiagnosticsRow("Client Roblox", report.RobloxPath);
        yield return new DiagnosticsRow("Intégrité", report.RobloxIntegrity);
    }

    private void RefreshDiscordStatus()
    {
        var current = _settings.Current;

        if (!current.EnableDiscordPresence)
        {
            DiscordStatusText = "Présence désactivée.";
            return;
        }

        if (DiscordApplicationIdRules.Normalize(current.DiscordApplicationId) is null)
        {
            DiscordStatusText = "Identifiant d'application requis : la présence reste inactive.";
            return;
        }

        DiscordStatusText = _discordPresence.State switch
        {
            DiscordPresenceState.Connected => "Activité publiée sur Discord.",
            DiscordPresenceState.Unavailable => "Discord n'est pas joignable : la présence s'activera dès qu'il démarrera.",
            _ => "Présence prête : elle s'activera à la prochaine activité."
        };
    }

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

        PersistAppearanceAsync(requestedTheme, accent, operationName);
    }

    private async void PersistAppearanceAsync(AppTheme theme, AccentColorKey accent, string operationName)
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

    private async void PersistPreferenceAsync(Func<AppSettings, AppSettings> mutation, string successMessage)
    {
        if (_isRestoringSelection)
        {
            return;
        }

        await PersistPreferenceCoreAsync(mutation, successMessage);
    }

    private async Task<bool> PersistPreferenceCoreAsync(Func<AppSettings, AppSettings> mutation, string successMessage)
    {
        ErrorMessage = null;

        try
        {
            var updated = await _settings.UpdateAsync(mutation);
            StatusMessage = successMessage;
            _logger.Info(Category, successMessage);

            if (updated.EnableDiscordPresence != EnableDiscordPresence)
            {
                _isRestoringSelection = true;
                try
                {
                    EnableDiscordPresence = updated.EnableDiscordPresence;
                }
                finally
                {
                    _isRestoringSelection = false;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.Error(Category, "Enregistrement de la préférence impossible.", ex);
            ErrorMessage = "La préférence n'a pas pu être enregistrée sur le disque.";
            return false;
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

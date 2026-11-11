namespace LinkLauncher.Features.Dashboard;

using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LinkLauncher.Core.Abstractions;
using LinkLauncher.Core.Models;
using LinkLauncher.Features.Common;
using LinkLauncher.Features.Navigation;

/// <summary>Présente l'état réel de l'installation Roblox et les actions disponibles.</summary>
public sealed partial class DashboardViewModel : ObservableObject, IAsyncPage
{
    private const string Category = "Dashboard";
    private const string Placeholder = "—";

    private readonly IRobloxProbe _probe;
    private readonly IFolderLauncher _folderLauncher;
    private readonly INavigationService _navigation;
    private readonly IAppLogger _logger;

    private RobloxInstallation? _installation;
    private bool _initialized;

    /// <summary>Crée le ViewModel du tableau de bord.</summary>
    public DashboardViewModel(
        IRobloxProbe probe,
        IFolderLauncher folderLauncher,
        INavigationService navigation,
        IApplicationVersionProvider versionProvider,
        IAppLogger logger)
    {
        _probe = probe ?? throw new ArgumentNullException(nameof(probe));
        _folderLauncher = folderLauncher ?? throw new ArgumentNullException(nameof(folderLauncher));
        _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        ArgumentNullException.ThrowIfNull(versionProvider);

        LauncherVersion = versionProvider.Version;
    }

    /// <summary>Indique qu'une vérification est en cours.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OpenInstallationFolderCommand))]
    private bool _isBusy;

    /// <summary>Nature de l'état affiché.</summary>
    [ObservableProperty]
    private DashboardStatusKind _statusKind = DashboardStatusKind.Checking;

    /// <summary>Titre de l'état affiché.</summary>
    [ObservableProperty]
    private string _statusHeadline = "Vérification de l'installation…";

    /// <summary>Description de l'état affiché.</summary>
    [ObservableProperty]
    private string _statusDescription = "Recherche du client Roblox officiel dans votre profil Windows.";

    /// <summary>Version du client Player détectée.</summary>
    [ObservableProperty]
    private string _robloxVersion = Placeholder;

    /// <summary>Nom du dossier de version détecté.</summary>
    [ObservableProperty]
    private string _versionFolderName = Placeholder;

    /// <summary>Racine de l'installation Roblox.</summary>
    [ObservableProperty]
    private string _installationPath = Placeholder;

    /// <summary>Version de LinkLauncher.</summary>
    [ObservableProperty]
    private string _launcherVersion;

    /// <summary>Moment de la dernière vérification.</summary>
    [ObservableProperty]
    private string _lastCheckedText = "Pas encore vérifié";

    /// <summary>Message d'erreur présenté à l'utilisateur.</summary>
    [ObservableProperty]
    private string? _errorMessage;

    /// <summary>Indique un avertissement sur l'installation détectée.</summary>
    [ObservableProperty]
    private string? _warningMessage;

    /// <summary>Explication de l'indisponibilité du lancement.</summary>
    public string PlayAvailabilityNote =>
        "Le lancement du client officiel est prévu à la phase 2 : il exige une session Roblox authentifiée.";

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        await RefreshAsync(cancellationToken);
    }

    [RelayCommand]
    private async Task RefreshAsync(CancellationToken cancellationToken)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        ErrorMessage = null;
        WarningMessage = null;
        StatusKind = DashboardStatusKind.Checking;
        StatusHeadline = "Vérification de l'installation…";
        StatusDescription = "Recherche du client Roblox officiel dans votre profil Windows.";

        try
        {
            var installation = await _probe.ProbeAsync(cancellationToken);
            _installation = installation;
            ApplyInstallation(installation);
        }
        catch (OperationCanceledException)
        {
            StatusKind = DashboardStatusKind.Warning;
            StatusHeadline = "Vérification interrompue";
            StatusDescription = "La vérification de l'installation a été annulée.";
        }
        catch (Exception ex)
        {
            _logger.Error(Category, "Vérification de l'installation Roblox impossible.", ex);
            StatusKind = DashboardStatusKind.Error;
            StatusHeadline = "Vérification impossible";
            StatusDescription = "L'examen de l'installation a échoué. Le détail technique figure dans le journal d'activité.";
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
            OnPropertyChanged(nameof(PlayAvailabilityNote));
        }
    }

    [RelayCommand(CanExecute = nameof(CanOpenInstallationFolder))]
    private void OpenInstallationFolder()
    {
        var path = _installation?.InstallationRootPath;
        if (path is null)
        {
            return;
        }

        if (!_folderLauncher.TryOpenFolder(path))
        {
            ErrorMessage = "Le dossier d'installation Roblox n'a pas pu être ouvert. Vérifiez qu'il existe toujours.";
        }
    }

    [RelayCommand]
    private void OpenSettings() => _navigation.NavigateTo(NavigationRoutes.Settings);

    private bool CanOpenInstallationFolder() =>
        !IsBusy && _installation?.InstallationRootPath is { Length: > 0 };

    private void ApplyInstallation(RobloxInstallation installation)
    {
        InstallationPath = installation.InstallationRootPath ?? Placeholder;
        VersionFolderName = installation.VersionFolderName ?? Placeholder;
        RobloxVersion = installation.PlayerVersion ?? Placeholder;
        LastCheckedText = $"Vérifié à {installation.CheckedAt.ToString("HH:mm:ss", CultureInfo.CurrentCulture)}";

        switch (installation.State)
        {
            case RobloxInstallationState.Installed:
                StatusKind = installation.Issue is null ? DashboardStatusKind.Ready : DashboardStatusKind.Warning;
                StatusHeadline = installation.Issue is null ? "Roblox est prêt" : "Installation détectée avec un avertissement";
                StatusDescription = installation.Issue ?? "Le client Roblox officiel est installé et son exécutable est présent.";
                WarningMessage = installation.Issue;
                break;

            case RobloxInstallationState.Broken:
                StatusKind = DashboardStatusKind.Warning;
                StatusHeadline = "Installation incomplète";
                StatusDescription = installation.Issue ?? "Le client Roblox officiel est incomplet.";
                break;

            case RobloxInstallationState.NotInstalled:
                StatusKind = DashboardStatusKind.Warning;
                StatusHeadline = "Roblox n'est pas installé";
                StatusDescription = installation.Issue ?? "Aucune installation du client Roblox n'a été détectée.";
                break;

            default:
                StatusKind = DashboardStatusKind.Error;
                StatusHeadline = "État inconnu";
                StatusDescription = "L'installation Roblox n'a pas pu être déterminée.";
                break;
        }

        OpenInstallationFolderCommand.NotifyCanExecuteChanged();
    }
}

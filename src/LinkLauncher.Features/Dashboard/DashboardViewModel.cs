namespace LinkLauncher.Features.Dashboard;

using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LinkLauncher.Core.Abstractions;
using LinkLauncher.Core.Models;
using LinkLauncher.Features.Common;
using LinkLauncher.Features.Navigation;

/// <summary>Présente l'état réel de l'installation Roblox, le lancement officiel et le suivi du client.</summary>
public sealed partial class DashboardViewModel : ObservableObject, IAsyncPage
{
    private const string Category = "Dashboard";
    private const string Placeholder = "—";

    private static readonly TimeSpan LaunchDetectionWindow = TimeSpan.FromSeconds(25);

    private readonly IRobloxProbe _probe;
    private readonly IRobloxIntegrityChecker _integrityChecker;
    private readonly IRobloxLaunchService _launchService;
    private readonly IRobloxProcessMonitor _processMonitor;
    private readonly IFolderLauncher _folderLauncher;
    private readonly INavigationService _navigation;
    private readonly IAppLogger _logger;
    private readonly SynchronizationContext? _uiContext = SynchronizationContext.Current;

    private RobloxInstallation? _installation;
    private bool _initialized;

    /// <summary>Crée le ViewModel du tableau de bord.</summary>
    public DashboardViewModel(
        IRobloxProbe probe,
        IRobloxIntegrityChecker integrityChecker,
        IRobloxLaunchService launchService,
        IRobloxProcessMonitor processMonitor,
        IFolderLauncher folderLauncher,
        INavigationService navigation,
        IApplicationVersionProvider versionProvider,
        IAppLogger logger)
    {
        _probe = probe ?? throw new ArgumentNullException(nameof(probe));
        _integrityChecker = integrityChecker ?? throw new ArgumentNullException(nameof(integrityChecker));
        _launchService = launchService ?? throw new ArgumentNullException(nameof(launchService));
        _processMonitor = processMonitor ?? throw new ArgumentNullException(nameof(processMonitor));
        _folderLauncher = folderLauncher ?? throw new ArgumentNullException(nameof(folderLauncher));
        _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        ArgumentNullException.ThrowIfNull(versionProvider);

        LauncherVersion = versionProvider.Version;
        ProcessDetails = DescribeProcess(null);

        _processMonitor.Changed += OnProcessChanged;
    }

    /// <summary>Indique qu'une vérification est en cours.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OpenInstallationFolderCommand))]
    private bool _isBusy;

    /// <summary>Indique qu'une demande de lancement est en cours.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PlayCommand))]
    private bool _isLaunching;

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

    /// <summary>État lisible du client Roblox.</summary>
    [ObservableProperty]
    private string _launchStateText = "Client arrêté";

    /// <summary>Détail du processus observé.</summary>
    [ObservableProperty]
    private string _processDetails = "Aucun processus Roblox détecté.";

    /// <summary>Indique que les fichiers indispensables au lancement sont présents.</summary>
    [ObservableProperty]
    private bool _isInstallationComplete;

    /// <summary>Résumé lisible de la vérification des fichiers.</summary>
    [ObservableProperty]
    private string _integritySummary = "Installation non vérifiée.";

    /// <summary>Éléments recommandés absents, s'il y en a.</summary>
    [ObservableProperty]
    private string? _recommendedMissingText;

    /// <summary>Libellé de la pastille d'intégrité.</summary>
    [ObservableProperty]
    private string _integrityBadgeText = "Non vérifiée";

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        _processMonitor.Start();
        ApplyCurrentProcess(_processMonitor.Current);

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

            var integrity = await _integrityChecker.CheckAsync(installation, cancellationToken);
            ApplyIntegrity(integrity);
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
            PlayCommand.NotifyCanExecuteChanged();
        }
    }

    [RelayCommand(CanExecute = nameof(CanPlay))]
    private async Task PlayAsync(CancellationToken cancellationToken)
    {
        if (IsLaunching)
        {
            return;
        }

        ErrorMessage = null;
        IsLaunching = true;
        LaunchStateText = "Démarrage du client…";
        ProcessDetails = "Demande transmise au mécanisme officiel de Roblox.";

        try
        {
            var result = await _launchService.LaunchClientAsync(cancellationToken);

            switch (result.Outcome)
            {
                case RobloxLaunchOutcome.Launched:
                    await WaitForClientStartAsync(cancellationToken);
                    break;

                case RobloxLaunchOutcome.AlreadyRunning:
                    LaunchStateText = "Client déjà en cours d'exécution";
                    ProcessDetails = DescribeProcess(_processMonitor.Current);
                    break;

                case RobloxLaunchOutcome.InstallationNotUsable:
                    ReportLaunchFailure(
                        "Le client Roblox n'est pas utilisable en l'état. Utilisez « Installer ou réparer (site officiel) ».",
                        result.TechnicalDetail);
                    break;

                case RobloxLaunchOutcome.ProtocolNotRegistered:
                    ReportLaunchFailure(
                        "Windows ne dispose d'aucune association pour le protocole roblox-player. " +
                        "Installez ou réparez Roblox depuis le site officiel.",
                        result.TechnicalDetail);
                    break;

                case RobloxLaunchOutcome.Cancelled:
                    LaunchStateText = "Lancement annulé";
                    ProcessDetails = "Aucune demande n'a été transmise.";
                    break;

                default:
                    ReportLaunchFailure(
                        "Windows n'a pas pu lancer le client Roblox. Le détail technique figure dans le journal d'activité.",
                        result.TechnicalDetail);
                    break;
            }
        }
        catch (OperationCanceledException)
        {
            LaunchStateText = "Lancement annulé";
            ProcessDetails = "Aucune demande n'a été transmise.";
        }
        catch (Exception ex)
        {
            _logger.Error(Category, "Lancement du client Roblox impossible.", ex);
            ReportLaunchFailure("Le lancement a échoué de façon inattendue.", ex.Message);
        }
        finally
        {
            IsLaunching = false;
        }
    }

    [RelayCommand]
    private void OpenDownloadPage()
    {
        ErrorMessage = null;

        var result = _launchService.OpenOfficialDownloadPage();
        if (!result.IsSuccess)
        {
            ErrorMessage = "La page officielle de Roblox n'a pas pu être ouverte. Vérifiez votre navigateur par défaut.";
            _logger.Error(Category, "Ouverture de la page officielle impossible.", new InvalidOperationException(result.TechnicalDetail ?? "Erreur inconnue"));
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

    private bool CanPlay() => !IsLaunching && _installation?.IsUsable == true;

    private bool CanOpenInstallationFolder() =>
        !IsBusy && _installation?.InstallationRootPath is { Length: > 0 };

    private async Task WaitForClientStartAsync(CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow + LaunchDetectionWindow;

        while (DateTimeOffset.UtcNow < deadline && _processMonitor.Current is null)
        {
            await Task.Delay(500, cancellationToken);
        }

        if (_processMonitor.Current is { } process)
        {
            LaunchStateText = "Roblox est ouvert";
            ProcessDetails = DescribeProcess(process);
            return;
        }

        LaunchStateText = "Démarrage en cours…";
        ProcessDetails = "Le client n'a pas encore été observé ; une mise à jour peut être en cours. La surveillance continue.";
        _logger.Warn(Category, "Le processus Roblox n'a pas été observé dans la fenêtre de détection.");
    }

    private void ReportLaunchFailure(string message, string? technicalDetail)
    {
        LaunchStateText = "Le client n'a pas démarré";
        ProcessDetails = "Aucun processus Roblox n'a été démarré.";
        ErrorMessage = message;

        if (!string.IsNullOrWhiteSpace(technicalDetail))
        {
            _logger.Warn(Category, $"Échec du lancement : {technicalDetail}");
        }
    }

    private void OnProcessChanged(object? sender, RobloxProcessChangedEventArgs args) =>
        PostToUi(() => ApplyTransition(args));

    private void ApplyTransition(RobloxProcessChangedEventArgs args)
    {
        switch (args.Transition.Change)
        {
            case RobloxProcessChange.Started:
                LaunchStateText = "Roblox est ouvert";
                ProcessDetails = DescribeProcess(args.Transition.Process);
                _logger.Info(Category, $"Client Roblox observé (PID {args.Transition.Process.ProcessId}).");
                break;

            case RobloxProcessChange.Stopped:
                LaunchStateText = "Client arrêté";
                ProcessDetails = DescribeStop(args.Transition.Process);
                _logger.Info(
                    Category,
                    $"Client Roblox fermé (PID {args.Transition.Process.ProcessId}, code de sortie " +
                    $"{args.Transition.Process.ExitCode?.ToString(CultureInfo.InvariantCulture) ?? "inconnu"}).");
                break;
        }
    }

    private void ApplyCurrentProcess(RobloxProcessInfo? process)
    {
        if (process is null)
        {
            LaunchStateText = "Client arrêté";
            ProcessDetails = "Aucun processus Roblox détecté.";
            return;
        }

        LaunchStateText = "Roblox est ouvert";
        ProcessDetails = DescribeProcess(process);
    }

    private void PostToUi(Action action)
    {
        if (_uiContext is null || SynchronizationContext.Current == _uiContext)
        {
            action();
            return;
        }

        _uiContext.Post(_ => action(), null);
    }

    private void ApplyIntegrity(RobloxIntegrityReport report)
    {
        IsInstallationComplete = report.IsComplete;

        if (report.IsEmpty)
        {
            IntegrityBadgeText = "Non vérifiée";
            IntegritySummary = "Aucune installation à vérifier.";
            RecommendedMissingText = null;
            return;
        }

        IntegrityBadgeText = report.IsComplete ? "Complète" : "Incomplète";
        IntegritySummary = report.IsComplete
            ? "Les fichiers indispensables au lancement sont présents."
            : "Éléments manquants : " + string.Join(", ", report.MissingFiles) + ".";

        RecommendedMissingText = report.MissingRecommendedEntries.Count == 0
            ? null
            : "Éléments facultatifs absents : " + string.Join(", ", report.MissingRecommendedEntries) + ".";
    }

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

    private static string DescribeProcess(RobloxProcessInfo? process)
    {
        if (process is null)
        {
            return "Aucun processus Roblox détecté.";
        }

        var startedAt = process.StartedAt?.ToString("HH:mm:ss", CultureInfo.CurrentCulture);
        var details = $"PID {process.ProcessId.ToString(CultureInfo.InvariantCulture)}";

        return startedAt is null ? $"{details}." : $"{details} · démarré à {startedAt}.";
    }

    private static string DescribeStop(RobloxProcessInfo process)
    {
        var processId = process.ProcessId.ToString(CultureInfo.InvariantCulture);
        var exitCode = process.ExitCode?.ToString(CultureInfo.InvariantCulture);

        return exitCode is null
            ? $"PID {processId} · code de sortie indisponible."
            : $"PID {processId} · code de sortie {exitCode}.";
    }
}

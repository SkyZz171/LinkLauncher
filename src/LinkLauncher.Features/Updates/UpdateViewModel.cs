namespace LinkLauncher.Features.Updates;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LinkLauncher.Core.Abstractions;
using LinkLauncher.Core.Models;

/// <summary>Recherche, télécharge et installe une mise à jour signée par empreinte.</summary>
public sealed partial class UpdateViewModel : ObservableObject
{
    private const string Category = "Mises à jour";

    private readonly IUpdateChecker _checker;
    private readonly IUpdateDownloader _downloader;
    private readonly IShellLauncher _shellLauncher;
    private readonly IAppLogger _logger;

    private UpdateRelease? _release;

    /// <summary>Crée le ViewModel des mises à jour.</summary>
    public UpdateViewModel(
        IUpdateChecker checker,
        IUpdateDownloader downloader,
        IShellLauncher shellLauncher,
        IApplicationVersionProvider versionProvider,
        IAppLogger logger)
    {
        _checker = checker ?? throw new ArgumentNullException(nameof(checker));
        _downloader = downloader ?? throw new ArgumentNullException(nameof(downloader));
        _shellLauncher = shellLauncher ?? throw new ArgumentNullException(nameof(shellLauncher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        ArgumentNullException.ThrowIfNull(versionProvider);

        CurrentVersion = versionProvider.Version;
    }

    /// <summary>Version installée.</summary>
    public string CurrentVersion { get; }

    /// <summary>État de la dernière recherche.</summary>
    [ObservableProperty]
    private string _statusText = "Recherche non lancée.";

    /// <summary>Version disponible, le cas échéant.</summary>
    [ObservableProperty]
    private string? _availableVersionText;

    /// <summary>Chemin du paquet téléchargé et vérifié.</summary>
    [ObservableProperty]
    private string? _packagePath;

    /// <summary>Indique qu'une opération est en cours.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CheckForUpdatesCommand))]
    [NotifyCanExecuteChangedFor(nameof(DownloadCommand))]
    [NotifyCanExecuteChangedFor(nameof(InstallCommand))]
    private bool _isBusy;

    /// <summary>Message d'erreur présenté à l'utilisateur.</summary>
    [ObservableProperty]
    private string? _errorMessage;

    [RelayCommand(CanExecute = nameof(CanCheck))]
    private async Task CheckForUpdatesAsync(CancellationToken cancellationToken)
    {
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var result = await _checker.CheckAsync(cancellationToken);
            _release = result.Release;
            PackagePath = null;

            AvailableVersionText = result.Release is null ? null : $"Version {result.Release.Version}";
            StatusText = result.Outcome switch
            {
                UpdateCheckOutcome.UpdateAvailable when !result.Release!.IsVerifiable =>
                    $"{result.Message} Cette publication ne fournit pas d'empreinte SHA-256 : l'installation est refusée.",
                _ => result.Message
            };
        }
        catch (OperationCanceledException)
        {
            StatusText = "Recherche annulée.";
        }
        catch (Exception ex)
        {
            _logger.Error(Category, "Recherche de mise à jour impossible.", ex);
            ErrorMessage = "La recherche de mise à jour a échoué. Le détail technique figure dans le journal d'activité.";
        }
        finally
        {
            IsBusy = false;
            NotifyCommands();
        }
    }

    [RelayCommand(CanExecute = nameof(CanDownload))]
    private async Task DownloadAsync(CancellationToken cancellationToken)
    {
        if (_release is null)
        {
            return;
        }

        IsBusy = true;
        ErrorMessage = null;
        StatusText = "Téléchargement du paquet…";

        try
        {
            var result = await _downloader.DownloadAsync(_release, cancellationToken);

            if (result.Succeeded)
            {
                PackagePath = result.FilePath;
                StatusText = "Paquet téléchargé et empreinte SHA-256 vérifiée.";
            }
            else
            {
                StatusText = "Téléchargement refusé.";
                ErrorMessage = result.Error;
            }
        }
        catch (OperationCanceledException)
        {
            StatusText = "Téléchargement annulé.";
        }
        catch (Exception ex)
        {
            _logger.Error(Category, "Téléchargement de la mise à jour impossible.", ex);
            ErrorMessage = "Le paquet n'a pas pu être téléchargé. Le détail technique figure dans le journal d'activité.";
        }
        finally
        {
            IsBusy = false;
            NotifyCommands();
        }
    }

    [RelayCommand(CanExecute = nameof(CanInstall))]
    private void Install()
    {
        if (PackagePath is null)
        {
            return;
        }

        ErrorMessage = null;

        try
        {
            var result = _shellLauncher.Launch(PackagePath);

            if (!result.Succeeded)
            {
                ErrorMessage = "L'installateur n'a pas pu être démarré depuis Windows.";
                return;
            }

            StatusText = "Installateur démarré. LinkLauncher va se fermer pour laisser la place à la mise à jour.";
            _logger.Info(Category, $"Installateur de la version {_release?.Version} démarré.");
        }
        catch (Exception ex)
        {
            _logger.Error(Category, "Démarrage de l'installateur impossible.", ex);
            ErrorMessage = "L'installateur n'a pas pu être démarré. Le détail technique figure dans le journal d'activité.";
        }
    }

    private bool CanCheck() => !IsBusy;

    private bool CanDownload() => !IsBusy && _release is not null && _release.IsVerifiable && PackagePath is null;

    private bool CanInstall() => !IsBusy && PackagePath is not null;

    private void NotifyCommands()
    {
        CheckForUpdatesCommand.NotifyCanExecuteChanged();
        DownloadCommand.NotifyCanExecuteChanged();
        InstallCommand.NotifyCanExecuteChanged();
    }
}

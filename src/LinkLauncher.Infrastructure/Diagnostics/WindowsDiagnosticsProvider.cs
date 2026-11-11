namespace LinkLauncher.Infrastructure.Diagnostics;

using System.Runtime.InteropServices;
using LinkLauncher.Core.Abstractions;
using LinkLauncher.Core.Models;

/// <summary>Collecte l'état réel de l'environnement pour le diagnostic.</summary>
/// <remarks>
/// Le rapport ne contient que des chemins, des versions et l'état de l'installation Roblox :
/// aucun jeton, aucun cookie et aucune donnée de compte n'y figure.
/// </remarks>
public sealed class WindowsDiagnosticsProvider : IDiagnosticsProvider
{
    private const string Category = "Diagnostic";
    private const string LogSearchPattern = "linklauncher-*.log";

    private readonly IAppPaths _paths;
    private readonly IApplicationVersionProvider _versionProvider;
    private readonly IRobloxProbe _probe;
    private readonly IRobloxIntegrityChecker _integrityChecker;
    private readonly IFileSystem _fileSystem;
    private readonly IAppLogger _logger;

    /// <summary>Crée le fournisseur de diagnostic.</summary>
    public WindowsDiagnosticsProvider(
        IAppPaths paths,
        IApplicationVersionProvider versionProvider,
        IRobloxProbe probe,
        IRobloxIntegrityChecker integrityChecker,
        IFileSystem fileSystem,
        IAppLogger logger)
    {
        _paths = paths ?? throw new ArgumentNullException(nameof(paths));
        _versionProvider = versionProvider ?? throw new ArgumentNullException(nameof(versionProvider));
        _probe = probe ?? throw new ArgumentNullException(nameof(probe));
        _integrityChecker = integrityChecker ?? throw new ArgumentNullException(nameof(integrityChecker));
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<DiagnosticsReport> CreateAsync(CancellationToken cancellationToken = default)
    {
        var installation = await ProbeAsync(cancellationToken).ConfigureAwait(false);
        var integrity = await CheckIntegrityAsync(installation, cancellationToken).ConfigureAwait(false);

        return new DiagnosticsReport
        {
            LauncherVersion = _versionProvider.Version,
            OperatingSystem = RuntimeInformation.OSDescription,
            Runtime = RuntimeInformation.FrameworkDescription,
            Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
            SettingsFilePath = _paths.SettingsFilePath,
            LibraryFilePath = _paths.LibraryFilePath,
            LogsDirectory = _paths.LogsDirectory,
            LatestLogFilePath = FindLatestLog(),
            RobloxVersion = installation.PlayerVersion ?? "—",
            RobloxVersionFolder = installation.VersionFolderName ?? "—",
            RobloxPath = installation.PlayerExecutablePath ?? "—",
            RobloxIntegrity = DescribeIntegrity(integrity),
            GeneratedAt = DateTimeOffset.Now
        };
    }

    private async Task<RobloxInstallation> ProbeAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await _probe.ProbeAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.Error(Category, "Détection Roblox impossible pour le diagnostic.", ex);
            return new RobloxInstallation();
        }
    }

    private async Task<RobloxIntegrityReport?> CheckIntegrityAsync(
        RobloxInstallation installation,
        CancellationToken cancellationToken)
    {
        if (installation.PlayerExecutablePath is null)
        {
            return null;
        }

        try
        {
            return await _integrityChecker.CheckAsync(installation, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.Error(Category, "Vérification d'intégrité impossible pour le diagnostic.", ex);
            return null;
        }
    }

    private static string DescribeIntegrity(RobloxIntegrityReport? integrity) => integrity switch
    {
        null => "Non vérifiée",
        { IsComplete: true } => "Complète",
        _ => "Incomplète : " + string.Join(", ", integrity.MissingFiles)
    };

    private string? FindLatestLog()
    {
        try
        {
            if (!_fileSystem.DirectoryExists(_paths.LogsDirectory))
            {
                return null;
            }

            return _fileSystem.EnumerateFiles(_paths.LogsDirectory)
                .Where(path => Path.GetFileName(path).StartsWith("linklauncher-", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(_fileSystem.GetLastWriteTimeUtc)
                .FirstOrDefault();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.Warn(Category, $"Lecture du dossier des journaux impossible : {ex.Message}");
            return null;
        }
    }
}

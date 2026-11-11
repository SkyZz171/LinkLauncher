namespace LinkLauncher.Infrastructure.Roblox;

using LinkLauncher.Core.Abstractions;
using LinkLauncher.Core.Models;
using LinkLauncher.Core.Services;

/// <summary>
/// Détecte l'installation du client Roblox officiel dans le profil de l'utilisateur.
/// </summary>
/// <remarks>
/// Seuls les dossiers de version officiels et l'exécutable Player sont examinés.
/// Aucun fichier Roblox n'est modifié, aucun processus n'est manipulé.
/// </remarks>
public sealed class WindowsRobloxProbe : IRobloxProbe
{
    private const string VersionsFolderName = "Versions";
    private const string PlayerExecutableName = "RobloxPlayerBeta.exe";
    private const string PlayerProtocolScheme = "roblox-player";

    private readonly IFileSystem _fileSystem;
    private readonly IExecutableVersionReader _versionReader;
    private readonly IProtocolRegistrationReader _protocolReader;
    private readonly IAppLogger _logger;
    private readonly string _robloxRootPath;

    /// <summary>Crée la sonde de détection.</summary>
    /// <param name="fileSystem">Accès au système de fichiers.</param>
    /// <param name="versionReader">Lecture des versions d'exécutable.</param>
    /// <param name="protocolReader">Lecture du protocole <c>roblox-player</c>.</param>
    /// <param name="logger">Journalisation.</param>
    /// <param name="robloxRootPath">Racine de l'installation Roblox à examiner.</param>
    public WindowsRobloxProbe(
        IFileSystem fileSystem,
        IExecutableVersionReader versionReader,
        IProtocolRegistrationReader protocolReader,
        IAppLogger logger,
        string robloxRootPath)
    {
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _versionReader = versionReader ?? throw new ArgumentNullException(nameof(versionReader));
        _protocolReader = protocolReader ?? throw new ArgumentNullException(nameof(protocolReader));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        ArgumentException.ThrowIfNullOrWhiteSpace(robloxRootPath);

        _robloxRootPath = robloxRootPath;
    }

    /// <summary>Racine d'installation Roblox par défaut pour l'utilisateur courant.</summary>
    public static string DefaultRobloxRootPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Roblox");

    /// <inheritdoc />
    public Task<RobloxInstallation> ProbeAsync(CancellationToken cancellationToken = default)
        => Task.Run(() => Probe(cancellationToken), cancellationToken);

    private RobloxInstallation Probe(CancellationToken cancellationToken)
    {
        var checkedAt = DateTimeOffset.Now;

        if (!_fileSystem.DirectoryExists(_robloxRootPath))
        {
            return new RobloxInstallation
            {
                State = RobloxInstallationState.NotInstalled,
                InstallationRootPath = _robloxRootPath,
                Issue = "Le dossier Roblox est introuvable sur ce PC.",
                CheckedAt = checkedAt
            };
        }

        var versionsPath = Path.Combine(_robloxRootPath, VersionsFolderName);
        if (!_fileSystem.DirectoryExists(versionsPath))
        {
            return new RobloxInstallation
            {
                State = RobloxInstallationState.Broken,
                InstallationRootPath = _robloxRootPath,
                Issue = "Le dossier Roblox existe mais aucun dossier de version n'a été trouvé.",
                CheckedAt = checkedAt
            };
        }

        var candidates = CollectCandidates(versionsPath, cancellationToken);
        var preferred = RobloxBuildSelector.SelectPreferred(candidates);
        if (preferred is null)
        {
            return new RobloxInstallation
            {
                State = RobloxInstallationState.Broken,
                InstallationRootPath = _robloxRootPath,
                Issue = "Des versions Roblox existent mais le client Player est absent.",
                CheckedAt = checkedAt
            };
        }

        var installation = new RobloxInstallation
        {
            State = RobloxInstallationState.Installed,
            VersionFolderName = preferred.FolderName,
            PlayerVersion = RobloxVersionParser.TryParse(preferred.FileVersion)?.ToString(),
            PlayerExecutablePath = preferred.ExecutablePath,
            InstallationRootPath = _robloxRootPath,
            Issue = CheckProtocolRegistration(preferred.ExecutablePath),
            CheckedAt = checkedAt
        };

        _logger.Info(
            "Roblox",
            $"Client Player détecté : {installation.VersionFolderName} ({installation.PlayerVersion ?? "version inconnue"}).");

        return installation;
    }

    private List<RobloxBuildCandidate> CollectCandidates(string versionsPath, CancellationToken cancellationToken)
    {
        var candidates = new List<RobloxBuildCandidate>();

        IEnumerable<string> directories;
        try
        {
            directories = _fileSystem.EnumerateDirectories(versionsPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.Error("Roblox", "Énumération des versions Roblox impossible.", ex);
            return candidates;
        }

        foreach (var directory in directories)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var executablePath = Path.Combine(directory, PlayerExecutableName);
            if (!_fileSystem.FileExists(executablePath))
            {
                continue;
            }

            try
            {
                candidates.Add(new RobloxBuildCandidate(
                    Path.GetFileName(directory),
                    executablePath,
                    _versionReader.ReadFileVersion(executablePath),
                    _fileSystem.GetLastWriteTimeUtc(executablePath)));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _logger.Warn("Roblox", $"Version illisible ignorée : {directory} ({ex.Message}).");
            }
        }

        return candidates;
    }

    private string? CheckProtocolRegistration(string executablePath)
    {
        var command = _protocolReader.TryGetCommand(PlayerProtocolScheme);

        if (string.IsNullOrWhiteSpace(command))
        {
            return "Le protocole roblox-player n'est pas enregistré pour cet utilisateur.";
        }

        return command.Contains(executablePath, StringComparison.OrdinalIgnoreCase)
            ? null
            : "Le protocole roblox-player pointe vers une autre version que celle détectée.";
    }
}

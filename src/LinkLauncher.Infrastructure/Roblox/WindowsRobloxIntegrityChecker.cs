namespace LinkLauncher.Infrastructure.Roblox;

using LinkLauncher.Core.Abstractions;
using LinkLauncher.Core.Models;
using LinkLauncher.Core.Services;

/// <summary>Vérifie la présence des fichiers indispensables dans le dossier de version détecté.</summary>
public sealed class WindowsRobloxIntegrityChecker : IRobloxIntegrityChecker
{
    private const string Category = "Intégrité";

    private readonly IFileSystem _fileSystem;
    private readonly IAppLogger _logger;

    /// <summary>Crée le vérificateur d'intégrité.</summary>
    public WindowsRobloxIntegrityChecker(IFileSystem fileSystem, IAppLogger logger)
    {
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public Task<RobloxIntegrityReport> CheckAsync(
        RobloxInstallation installation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(installation);

        return Task.Run(() => Check(installation, cancellationToken), cancellationToken);
    }

    private RobloxIntegrityReport Check(RobloxInstallation installation, CancellationToken cancellationToken)
    {
        var checkedAt = DateTimeOffset.Now;

        if (installation.PlayerExecutablePath is null)
        {
            return new RobloxIntegrityReport
            {
                IsComplete = false,
                MissingFiles = [.. RobloxIntegrityRules.RequiredPlayerEntries],
                CheckedAt = checkedAt
            };
        }

        cancellationToken.ThrowIfCancellationRequested();

        var versionFolderPath = Path.GetDirectoryName(installation.PlayerExecutablePath);
        var entries = new List<string>();

        try
        {
            if (versionFolderPath is not null)
            {
                entries.AddRange(_fileSystem.EnumerateFiles(versionFolderPath).Select(Path.GetFileName).OfType<string>());
                entries.AddRange(_fileSystem.EnumerateDirectories(versionFolderPath).Select(Path.GetFileName).OfType<string>());
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.Error(Category, "Lecture du dossier de version impossible.", ex);
        }

        var report = RobloxIntegrityRules.Evaluate(
            versionFolderPath,
            installation.VersionFolderName,
            entries,
            checkedAt);

        if (report.IsComplete)
        {
            _logger.Info(Category, $"Installation complète : {installation.VersionFolderName}.");
        }
        else
        {
            _logger.Warn(
                Category,
                $"Éléments manquants dans {installation.VersionFolderName} : {string.Join(", ", report.MissingFiles)}.");
        }

        return report;
    }
}

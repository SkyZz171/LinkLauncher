namespace LinkLauncher.Core.Services;

using LinkLauncher.Core.Models;

/// <summary>Évalue la complétude d'un dossier de version Roblox.</summary>
public static class RobloxIntegrityRules
{
    /// <summary>Nom du client Player officiel.</summary>
    public const string PlayerExecutableName = "RobloxPlayerBeta.exe";

    /// <summary>Éléments sans lesquels le client Player ne peut pas démarrer.</summary>
    public static IReadOnlyList<string> RequiredPlayerEntries { get; } =
    [
        PlayerExecutableName,
        "RobloxPlayerBeta.dll"
    ];

    /// <summary>Éléments attendus mais non indispensables au démarrage.</summary>
    public static IReadOnlyList<string> RecommendedPlayerEntries { get; } =
    [
        "AppSettings.xml",
        "PlatformContent"
    ];

    /// <summary>
    /// Confronte le contenu observé d'un dossier de version aux éléments attendus.
    /// </summary>
    /// <param name="versionFolderPath">Chemin du dossier de version examiné.</param>
    /// <param name="versionFolderName">Nom du dossier de version.</param>
    /// <param name="existingEntries">Fichiers et sous-dossiers présents.</param>
    /// <param name="checkedAt">Moment de la vérification.</param>
    public static RobloxIntegrityReport Evaluate(
        string? versionFolderPath,
        string? versionFolderName,
        IReadOnlyCollection<string> existingEntries,
        DateTimeOffset checkedAt)
    {
        ArgumentNullException.ThrowIfNull(existingEntries);

        var available = new HashSet<string>(existingEntries, StringComparer.OrdinalIgnoreCase);

        var missingRequired = RequiredPlayerEntries
            .Where(entry => !available.Contains(entry))
            .ToArray();

        var missingRecommended = RecommendedPlayerEntries
            .Where(entry => !available.Contains(entry))
            .ToArray();

        return new RobloxIntegrityReport
        {
            IsComplete = versionFolderPath is not null && missingRequired.Length == 0,
            MissingFiles = missingRequired,
            MissingRecommendedEntries = missingRecommended,
            ExecutablePath = versionFolderPath is null
                ? null
                : Path.Combine(versionFolderPath, PlayerExecutableName),
            VersionFolderName = versionFolderName,
            CheckedAt = checkedAt
        };
    }
}

namespace LinkLauncher.Core.Models;

/// <summary>Rapport de diagnostic : état réel de l'environnement, sans donnée sensible.</summary>
public sealed record DiagnosticsReport
{
    /// <summary>Version de LinkLauncher.</summary>
    public string LauncherVersion { get; init; } = string.Empty;

    /// <summary>Version de Windows.</summary>
    public string OperatingSystem { get; init; } = string.Empty;

    /// <summary>Version du moteur d'exécution .NET.</summary>
    public string Runtime { get; init; } = string.Empty;

    /// <summary>Architecture du processus.</summary>
    public string Architecture { get; init; } = string.Empty;

    /// <summary>Fichier de configuration.</summary>
    public string SettingsFilePath { get; init; } = string.Empty;

    /// <summary>Fichier de bibliothèque.</summary>
    public string LibraryFilePath { get; init; } = string.Empty;

    /// <summary>Dossier des journaux d'activité.</summary>
    public string LogsDirectory { get; init; } = string.Empty;

    /// <summary>Dernier journal écrit, s'il existe.</summary>
    public string? LatestLogFilePath { get; init; }

    /// <summary>Version du client Roblox détectée.</summary>
    public string RobloxVersion { get; init; } = string.Empty;

    /// <summary>Dossier de version Roblox détecté.</summary>
    public string RobloxVersionFolder { get; init; } = string.Empty;

    /// <summary>Chemin du client Roblox.</summary>
    public string RobloxPath { get; init; } = string.Empty;

    /// <summary>Résultat de la vérification de l'installation.</summary>
    public string RobloxIntegrity { get; init; } = string.Empty;

    /// <summary>Moment de la génération du rapport.</summary>
    public DateTimeOffset GeneratedAt { get; init; } = DateTimeOffset.Now;
}

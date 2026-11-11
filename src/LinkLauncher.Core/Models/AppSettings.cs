namespace LinkLauncher.Core.Models;

/// <summary>
/// Configuration persistée de LinkLauncher.
/// </summary>
/// <remarks>
/// Chaque évolution du schéma incrémente <see cref="CurrentSchemaVersion"/> afin que
/// les fichiers écrits par une version antérieure puissent être normalisés au chargement.
/// </remarks>
public sealed record AppSettings
{
    /// <summary>Version du schéma écrite par cette version de l'application.</summary>
    public const int CurrentSchemaVersion = 2;

    /// <summary>Version du schéma lue depuis le fichier de configuration.</summary>
    public int SchemaVersion { get; init; } = CurrentSchemaVersion;

    /// <summary>Thème de l'interface.</summary>
    public AppTheme Theme { get; init; } = AppTheme.Dark;

    /// <summary>Couleur d'accentuation de l'interface.</summary>
    public AccentColorKey AccentColor { get; init; } = AccentColorKey.Aurora;

    /// <summary>Notifications Windows lors du démarrage et de l'arrêt du client Roblox.</summary>
    public bool EnableNotifications { get; init; } = true;

    /// <summary>Publication de l'activité sur Discord.</summary>
    public bool EnableDiscordPresence { get; init; }

    /// <summary>Identifiant d'application Discord, public ; <see langword="null"/> désactive la présence.</summary>
    public string? DiscordApplicationId { get; init; }

    /// <summary>Crée une configuration par défaut.</summary>
    public static AppSettings CreateDefault() => new();
}

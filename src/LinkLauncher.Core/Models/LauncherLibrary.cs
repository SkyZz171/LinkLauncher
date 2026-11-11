namespace LinkLauncher.Core.Models;

/// <summary>Contenu persistant de la bibliothèque : profils de lancement et expériences.</summary>
/// <remarks>
/// Chaque évolution du schéma incrémente <see cref="CurrentSchemaVersion"/> ; les fichiers écrits
/// par une version antérieure sont normalisés au chargement.
/// </remarks>
public sealed record LauncherLibrary
{
    /// <summary>Version du schéma écrite par cette version de l'application.</summary>
    public const int CurrentSchemaVersion = 1;

    /// <summary>Version du schéma lue depuis le fichier de bibliothèque.</summary>
    public int SchemaVersion { get; init; } = CurrentSchemaVersion;

    /// <summary>Profils de lancement disponibles.</summary>
    public IReadOnlyList<LaunchProfile> Profiles { get; init; } = [];

    /// <summary>Expériences enregistrées, dans l'ordre d'ajout.</summary>
    public IReadOnlyList<Experience> Experiences { get; init; } = [];

    /// <summary>Profil actif, appliqué aux lancements du client.</summary>
    public Guid? ActiveProfileId { get; init; }
}

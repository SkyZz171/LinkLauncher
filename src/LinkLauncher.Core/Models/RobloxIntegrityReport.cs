namespace LinkLauncher.Core.Models;

/// <summary>Résultat de la vérification des fichiers d'une installation Roblox.</summary>
public sealed record RobloxIntegrityReport
{
    /// <summary>Indique que tous les fichiers indispensables au lancement sont présents.</summary>
    public bool IsComplete { get; init; }

    /// <summary>Fichiers indispensables absents.</summary>
    public IReadOnlyList<string> MissingFiles { get; init; } = [];

    /// <summary>Éléments recommandés absents, qui n'empêchent pas le lancement.</summary>
    public IReadOnlyList<string> MissingRecommendedEntries { get; init; } = [];

    /// <summary>Chemin de l'exécutable attendu.</summary>
    public string? ExecutablePath { get; init; }

    /// <summary>Dossier de version examiné.</summary>
    public string? VersionFolderName { get; init; }

    /// <summary>Moment de la vérification.</summary>
    public DateTimeOffset CheckedAt { get; init; } = DateTimeOffset.Now;

    /// <summary>Indique que rien n'a pu être examiné.</summary>
    public bool IsEmpty => ExecutablePath is null && VersionFolderName is null;
}

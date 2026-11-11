namespace LinkLauncher.Core.Models;

/// <summary>Version publiée, téléchargeable et vérifiable.</summary>
/// <remarks>
/// Une publication sans empreinte SHA-256 n'est jamais installée : LinkLauncher refuse alors le
/// téléchargement plutôt que d'exécuter un binaire non vérifié.
/// </remarks>
public sealed record UpdateRelease
{
    /// <summary>Version publiée.</summary>
    public required AppVersion Version { get; init; }

    /// <summary>Étiquette de la publication, par exemple <c>v0.2.0</c>.</summary>
    public required string Tag { get; init; }

    /// <summary>Adresse du paquet à télécharger.</summary>
    public required Uri DownloadUrl { get; init; }

    /// <summary>Empreinte SHA-256 attendue, en hexadécimal minuscule.</summary>
    public string? Sha256 { get; init; }

    /// <summary>Notes de version, si la publication en fournit.</summary>
    public string? Notes { get; init; }

    /// <summary>Indique que le paquet peut être vérifié avant installation.</summary>
    public bool IsVerifiable => !string.IsNullOrWhiteSpace(Sha256);
}

/// <summary>Résultat d'une recherche de mise à jour.</summary>
public enum UpdateCheckOutcome
{
    /// <summary>La version installée est à jour.</summary>
    UpToDate = 0,

    /// <summary>Une version plus récente est publiée.</summary>
    UpdateAvailable = 1,

    /// <summary>La source de publication n'est pas joignable.</summary>
    FeedUnavailable = 2
}

/// <summary>Résultat complet d'une recherche de mise à jour.</summary>
/// <param name="Outcome">Conclusion de la recherche.</param>
/// <param name="Release">Publication plus récente, le cas échéant.</param>
/// <param name="Message">Message explicite destiné à l'utilisateur.</param>
public sealed record UpdateCheckResult(UpdateCheckOutcome Outcome, UpdateRelease? Release, string Message)
{
    /// <summary>Résultat « à jour ».</summary>
    public static UpdateCheckResult UpToDate(string message) => new(UpdateCheckOutcome.UpToDate, null, message);

    /// <summary>Résultat « mise à jour disponible ».</summary>
    public static UpdateCheckResult Available(UpdateRelease release) =>
        new(UpdateCheckOutcome.UpdateAvailable, release, $"Version {release.Version} disponible.");

    /// <summary>Résultat « source indisponible ».</summary>
    public static UpdateCheckResult Unavailable(string message) =>
        new(UpdateCheckOutcome.FeedUnavailable, null, message);
}

/// <summary>Résultat d'un téléchargement de mise à jour.</summary>
/// <param name="Succeeded">Indique si le paquet a été téléchargé et vérifié.</param>
/// <param name="FilePath">Chemin du paquet vérifié.</param>
/// <param name="Error">Message explicite en cas d'échec.</param>
public sealed record UpdateDownloadResult(bool Succeeded, string? FilePath, string? Error)
{
    /// <summary>Téléchargement réussi.</summary>
    public static UpdateDownloadResult Success(string filePath) => new(true, filePath, null);

    /// <summary>Téléchargement refusé ou échoué.</summary>
    public static UpdateDownloadResult Failure(string error) => new(false, null, error);
}

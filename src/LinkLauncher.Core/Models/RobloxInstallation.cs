namespace LinkLauncher.Core.Models;

/// <summary>
/// Résultat d'une vérification de l'installation Roblox.
/// </summary>
public sealed record RobloxInstallation
{
    /// <summary>État constaté.</summary>
    public RobloxInstallationState State { get; init; } = RobloxInstallationState.Unknown;

    /// <summary>Nom du dossier de version, par exemple <c>version-4ea8e85481e34f46</c>.</summary>
    public string? VersionFolderName { get; init; }

    /// <summary>Version du client Player lue dans les métadonnées du fichier exécutable.</summary>
    public string? PlayerVersion { get; init; }

    /// <summary>Chemin complet du client Player officiel.</summary>
    public string? PlayerExecutablePath { get; init; }

    /// <summary>Racine de l'installation Roblox examinée.</summary>
    public string? InstallationRootPath { get; init; }

    /// <summary>Description du problème constaté quand l'installation n'est pas utilisable.</summary>
    public string? Issue { get; init; }

    /// <summary>Moment de la vérification.</summary>
    public DateTimeOffset CheckedAt { get; init; } = DateTimeOffset.Now;

    /// <summary>Indique si le client officiel peut être utilisé.</summary>
    public bool IsUsable => State == RobloxInstallationState.Installed;
}

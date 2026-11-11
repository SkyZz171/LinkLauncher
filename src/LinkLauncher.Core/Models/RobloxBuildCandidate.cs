namespace LinkLauncher.Core.Models;

/// <summary>
/// Candidat d'installation Roblox : un dossier de version contenant un client Player.
/// </summary>
/// <param name="FolderName">Nom du dossier de version.</param>
/// <param name="ExecutablePath">Chemin du client Player du dossier.</param>
/// <param name="FileVersion">Version déclarée par l'exécutable, si disponible.</param>
/// <param name="LastWriteTimeUtc">Date de dernière écriture de l'exécutable.</param>
public sealed record RobloxBuildCandidate(
    string FolderName,
    string ExecutablePath,
    string? FileVersion,
    DateTime LastWriteTimeUtc);

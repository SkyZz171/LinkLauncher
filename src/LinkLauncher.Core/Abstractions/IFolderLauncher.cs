namespace LinkLauncher.Core.Abstractions;

/// <summary>Ouverture d'un dossier dans l'explorateur Windows.</summary>
public interface IFolderLauncher
{
    /// <summary>Ouvre un dossier ; renvoie <see langword="false"/> si l'ouverture a échoué.</summary>
    /// <param name="path">Chemin du dossier à ouvrir.</param>
    bool TryOpenFolder(string path);
}

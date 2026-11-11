namespace LinkLauncher.Core.Abstractions;

/// <summary>Lecture de la version déclarée par un exécutable Windows.</summary>
public interface IExecutableVersionReader
{
    /// <summary>Renvoie la version déclarée par le fichier, ou <see langword="null"/> si elle est indisponible.</summary>
    /// <param name="path">Chemin de l'exécutable.</param>
    string? ReadFileVersion(string path);
}

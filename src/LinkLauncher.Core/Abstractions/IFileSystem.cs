namespace LinkLauncher.Core.Abstractions;

/// <summary>Accès au système de fichiers, isolé pour permettre les tests.</summary>
public interface IFileSystem
{
    /// <summary>Indique si un fichier existe.</summary>
    bool FileExists(string path);

    /// <summary>Indique si un répertoire existe.</summary>
    bool DirectoryExists(string path);

    /// <summary>Crée un répertoire et ses parents si nécessaire.</summary>
    void CreateDirectory(string path);

    /// <summary>Énumère les sous-répertoires d'un chemin.</summary>
    IEnumerable<string> EnumerateDirectories(string path);

    /// <summary>Énumère les fichiers d'un chemin.</summary>
    IEnumerable<string> EnumerateFiles(string path);

    /// <summary>Lit un fichier texte.</summary>
    Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>Écrit un fichier texte.</summary>
    Task WriteAllTextAsync(string path, string contents, CancellationToken cancellationToken = default);

    /// <summary>Déplace un fichier.</summary>
    void MoveFile(string sourcePath, string destinationPath, bool overwrite);

    /// <summary>Copie un fichier.</summary>
    void CopyFile(string sourcePath, string destinationPath, bool overwrite);

    /// <summary>Supprime un fichier s'il existe.</summary>
    void DeleteFile(string path);

    /// <summary>Renvoie la date de dernière écriture d'un fichier en UTC.</summary>
    DateTime GetLastWriteTimeUtc(string path);
}

namespace LinkLauncher.Core.Abstractions;

using LinkLauncher.Core.Models;

/// <summary>Accès persistant à la bibliothèque d'expériences et aux profils de lancement.</summary>
public interface ILibraryStore
{
    /// <summary>Chemin du fichier de bibliothèque.</summary>
    string LibraryFilePath { get; }

    /// <summary>Charge la bibliothèque, ou une bibliothèque par défaut si elle est absente ou illisible.</summary>
    Task<LauncherLibrary> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>Enregistre la bibliothèque de façon atomique.</summary>
    Task SaveAsync(LauncherLibrary library, CancellationToken cancellationToken = default);
}

namespace LinkLauncher.Core.Models;

/// <summary>Résultat d'une modification de la bibliothèque.</summary>
/// <param name="Succeeded">Indique si la modification a été acceptée.</param>
/// <param name="Library">Bibliothèque résultante ; inchangée en cas d'échec.</param>
/// <param name="Error">Message explicite en cas d'échec, sinon <see langword="null"/>.</param>
public sealed record LibraryMutation(bool Succeeded, LauncherLibrary Library, string? Error)
{
    /// <summary>Modification acceptée.</summary>
    public static LibraryMutation Success(LauncherLibrary library) => new(true, library, null);

    /// <summary>Modification refusée, avec un message destiné à l'utilisateur.</summary>
    public static LibraryMutation Failure(LauncherLibrary library, string error) => new(false, library, error);
}

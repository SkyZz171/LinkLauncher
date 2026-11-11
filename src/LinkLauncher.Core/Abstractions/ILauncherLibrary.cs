namespace LinkLauncher.Core.Abstractions;

using LinkLauncher.Core.Models;

/// <summary>Bibliothèque d'expériences et profils de lancement partagée par l'application.</summary>
public interface ILauncherLibrary
{
    /// <summary>Contenu courant de la bibliothèque.</summary>
    LauncherLibrary Current { get; }

    /// <summary>Indique si le chargement initial a abouti.</summary>
    bool IsLoaded { get; }

    /// <summary>Survient après chaque modification enregistrée.</summary>
    event EventHandler? Changed;

    /// <summary>Charge la bibliothèque ; ne lève pas pour un fichier illisible.</summary>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>Ajoute un profil de lancement.</summary>
    Task<LibraryMutation> AddProfileAsync(string name, string? locale, CancellationToken cancellationToken = default);

    /// <summary>Supprime un profil de lancement.</summary>
    Task<LibraryMutation> RemoveProfileAsync(Guid profileId, CancellationToken cancellationToken = default);

    /// <summary>Active un profil de lancement.</summary>
    Task<LibraryMutation> SetActiveProfileAsync(Guid profileId, CancellationToken cancellationToken = default);

    /// <summary>Ajoute une expérience à partir d'un lien officiel ou d'un identifiant de lieu.</summary>
    Task<LibraryMutation> AddExperienceAsync(string name, string link, CancellationToken cancellationToken = default);

    /// <summary>Retire une expérience de la bibliothèque.</summary>
    Task<LibraryMutation> RemoveExperienceAsync(Guid experienceId, CancellationToken cancellationToken = default);

    /// <summary>Note l'ouverture de la page officielle d'une expérience.</summary>
    Task<LibraryMutation> MarkExperienceOpenedAsync(Guid experienceId, CancellationToken cancellationToken = default);
}

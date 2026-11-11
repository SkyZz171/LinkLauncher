namespace LinkLauncher.Core.Abstractions;

using LinkLauncher.Core.Models;

/// <summary>Configuration applicative partagée entre les fonctionnalités.</summary>
public interface ISettingsService
{
    /// <summary>Configuration courante.</summary>
    AppSettings Current { get; }

    /// <summary>Indique si le chargement initial a abouti.</summary>
    bool IsLoaded { get; }

    /// <summary>Survient après chaque enregistrement réussi.</summary>
    event EventHandler<AppSettingsChangedEventArgs>? Changed;

    /// <summary>Charge la configuration depuis le stockage ; ne lève pas pour un fichier illisible.</summary>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>Applique une modification, l'enregistre puis publie le changement.</summary>
    /// <param name="mutation">Fonction pure produisant la configuration modifiée.</param>
    /// <param name="cancellationToken">Jeton d'annulation.</param>
    /// <returns>La configuration enregistrée.</returns>
    Task<AppSettings> UpdateAsync(Func<AppSettings, AppSettings> mutation, CancellationToken cancellationToken = default);
}

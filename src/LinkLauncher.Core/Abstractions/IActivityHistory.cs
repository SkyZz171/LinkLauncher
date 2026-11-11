namespace LinkLauncher.Core.Abstractions;

using LinkLauncher.Core.Models;

/// <summary>Historique local des sessions du client Roblox.</summary>
public interface IActivityHistory
{
    /// <summary>Sessions connues.</summary>
    ActivityHistory Current { get; }

    /// <summary>Survient après chaque modification enregistrée.</summary>
    event EventHandler? Changed;

    /// <summary>Charge l'historique et marque les sessions laissées ouvertes comme interrompues.</summary>
    /// <param name="cancellationToken">Jeton d'annulation.</param>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>Ouvre une session pour le processus observé.</summary>
    /// <param name="process">Processus Roblox observé.</param>
    /// <param name="experienceName">Expérience ouverte depuis LinkLauncher, si connue.</param>
    /// <param name="profileName">Profil de lancement actif, si connu.</param>
    /// <param name="cancellationToken">Jeton d'annulation.</param>
    Task StartSessionAsync(
        RobloxProcessInfo process,
        string? experienceName,
        string? profileName,
        CancellationToken cancellationToken = default);

    /// <summary>Clôt la session correspondant au processus observé.</summary>
    /// <param name="process">Processus Roblox fermé.</param>
    /// <param name="cancellationToken">Jeton d'annulation.</param>
    Task EndSessionAsync(RobloxProcessInfo process, CancellationToken cancellationToken = default);

    /// <summary>Efface tout l'historique enregistré.</summary>
    /// <param name="cancellationToken">Jeton d'annulation.</param>
    Task ClearAsync(CancellationToken cancellationToken = default);
}

/// <summary>Conservation de l'historique des sessions.</summary>
public interface IActivityHistoryStore
{
    /// <summary>Chemin du fichier d'historique.</summary>
    string HistoryFilePath { get; }

    /// <summary>Lit l'historique, ou un historique vide s'il n'existe pas.</summary>
    /// <param name="cancellationToken">Jeton d'annulation.</param>
    Task<ActivityHistory> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>Enregistre l'historique de façon atomique.</summary>
    /// <param name="history">Historique à enregistrer.</param>
    /// <param name="cancellationToken">Jeton d'annulation.</param>
    Task SaveAsync(ActivityHistory history, CancellationToken cancellationToken = default);
}

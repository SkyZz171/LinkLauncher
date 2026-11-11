namespace LinkLauncher.Core.Models;

/// <summary>Session d'utilisation du client Roblox observée par LinkLauncher.</summary>
/// <remarks>
/// Une session ne contient que ce que Windows expose publiquement : identifiant de processus,
/// heures de démarrage et d'arrêt, code de sortie. Aucune donnée de jeu n'est enregistrée.
/// </remarks>
public sealed record PlaySession
{
    /// <summary>Identifiant local de la session.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Moment où le client a été observé pour la première fois.</summary>
    public required DateTimeOffset StartedAt { get; init; }

    /// <summary>Moment de la fermeture observée, ou <see langword="null"/> si la session est ouverte.</summary>
    public DateTimeOffset? EndedAt { get; init; }

    /// <summary>Identifiant du processus Roblox observé.</summary>
    public int ProcessId { get; init; }

    /// <summary>Profil de lancement appliqué au moment du démarrage.</summary>
    public string? ProfileName { get; init; }

    /// <summary>Expérience ouverte depuis LinkLauncher au moment du démarrage, si connue.</summary>
    public string? ExperienceName { get; init; }

    /// <summary>Code de sortie rapporté par Windows, lorsqu'il est disponible.</summary>
    public int? ExitCode { get; init; }

    /// <summary>Indique que LinkLauncher n'a pas pu observer la fin de la session.</summary>
    public bool Interrupted { get; init; }

    /// <summary>Indique que la session est encore en cours d'observation.</summary>
    public bool IsOpen => EndedAt is null && !Interrupted;

    /// <summary>Durée observée, ou <see langword="null"/> si elle n'est pas connue.</summary>
    public TimeSpan? ObservedDuration => EndedAt is { } ended && ended >= StartedAt ? ended - StartedAt : null;
}

/// <summary>Historique local des sessions Roblox.</summary>
public sealed record ActivityHistory
{
    /// <summary>Version du schéma écrite par cette version de l'application.</summary>
    public const int CurrentSchemaVersion = 1;

    /// <summary>Version du schéma lue depuis le fichier.</summary>
    public int SchemaVersion { get; init; } = CurrentSchemaVersion;

    /// <summary>Sessions observées, de la plus ancienne à la plus récente.</summary>
    public List<PlaySession> Sessions { get; init; } = [];
}

/// <summary>Totaux calculés à partir des sessions observées.</summary>
/// <param name="Today">Temps observé depuis le début de la journée locale.</param>
/// <param name="LastSevenDays">Temps observé sur les sept derniers jours.</param>
/// <param name="Total">Temps observé depuis le début de l'historique.</param>
/// <param name="SessionCount">Nombre de sessions enregistrées, interrompues comprises.</param>
/// <param name="LastSessionAt">Moment de la dernière fermeture observée.</param>
public sealed record ActivitySummary(
    TimeSpan Today,
    TimeSpan LastSevenDays,
    TimeSpan Total,
    int SessionCount,
    DateTimeOffset? LastSessionAt);

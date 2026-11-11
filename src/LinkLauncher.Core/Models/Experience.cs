namespace LinkLauncher.Core.Models;

/// <summary>Expérience enregistrée dans la bibliothèque, sous forme de lien officiel.</summary>
/// <remarks>
/// LinkLauncher ne conserve qu'un nom et l'adresse de la page officielle Roblox : aucune donnée
/// de session, aucun jeton et aucun identifiant de compte n'y figure.
/// </remarks>
public sealed record Experience
{
    /// <summary>Identifiant stable de l'entrée.</summary>
    public Guid Id { get; init; }

    /// <summary>Nom donné par l'utilisateur.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Adresse de la page officielle de l'expérience sur roblox.com.</summary>
    public string Url { get; init; } = string.Empty;

    /// <summary>Identifiant de lieu, quand il a pu être déduit du lien.</summary>
    public long? PlaceId { get; init; }

    /// <summary>Moment de l'ajout à la bibliothèque.</summary>
    public DateTimeOffset AddedAt { get; init; }

    /// <summary>Dernière ouverture de la page officielle depuis LinkLauncher.</summary>
    public DateTimeOffset? OpenedAt { get; init; }
}

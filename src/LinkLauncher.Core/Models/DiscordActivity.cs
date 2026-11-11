namespace LinkLauncher.Core.Models;

/// <summary>Activité transmise à Discord : deux lignes de texte et un horodatage.</summary>
/// <remarks>
/// Aucune donnée de compte, aucun jeton et aucune information de session Roblox ne figure ici.
/// </remarks>
public sealed record DiscordActivity
{
    /// <summary>Première ligne affichée.</summary>
    public required string Details { get; init; }

    /// <summary>Seconde ligne affichée.</summary>
    public required string State { get; init; }

    /// <summary>Moment de départ affiché dans l'activité.</summary>
    public DateTimeOffset StartedAt { get; init; } = DateTimeOffset.Now;
}

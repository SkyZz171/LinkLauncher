namespace LinkLauncher.Core.Models;

/// <summary>
/// Profil de lancement : un nom lisible et la langue transmise au client Roblox.
/// </summary>
/// <remarks>
/// Un profil ne contient aucun identifiant : il ne décrit qu'un paramétrage local de lancement.
/// </remarks>
public sealed record LaunchProfile
{
    /// <summary>Identifiant stable du profil.</summary>
    public Guid Id { get; init; }

    /// <summary>Nom affiché, unique dans la bibliothèque.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Langue transmise au client, par exemple <c>fr_FR</c> ; <see langword="null"/> suit Windows.</summary>
    public string? Locale { get; init; }

    /// <summary>Moment de la création du profil.</summary>
    public DateTimeOffset CreatedAt { get; init; }
}

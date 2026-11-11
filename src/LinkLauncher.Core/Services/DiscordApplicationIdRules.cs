namespace LinkLauncher.Core.Services;

/// <summary>Valide l'identifiant d'application Discord saisi par l'utilisateur.</summary>
/// <remarks>
/// Discord identifie une application par un nombre de 17 à 20 chiffres. Aucun jeton, aucun secret
/// ni aucune donnée de compte n'est demandé : l'identifiant est public.
/// </remarks>
public static class DiscordApplicationIdRules
{
    private const int MinimumLength = 17;
    private const int MaximumLength = 20;

    /// <summary>Message affiché lorsque l'identifiant n'est pas exploitable.</summary>
    public const string InvalidMessage =
        "L'identifiant d'application Discord doit contenir de 17 à 20 chiffres.";

    /// <summary>Normalise un identifiant d'application, ou renvoie <see langword="null"/> s'il est invalide.</summary>
    /// <param name="applicationId">Identifiant saisi.</param>
    public static string? Normalize(string? applicationId)
    {
        if (string.IsNullOrWhiteSpace(applicationId))
        {
            return null;
        }

        var trimmed = applicationId.Trim();

        if (trimmed.Length is < MinimumLength or > MaximumLength ||
            !trimmed.All(char.IsAsciiDigit))
        {
            return null;
        }

        return trimmed;
    }
}

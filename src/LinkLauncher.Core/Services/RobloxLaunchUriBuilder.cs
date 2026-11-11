namespace LinkLauncher.Core.Services;

/// <summary>Construit les URI officielles de lancement du client Roblox.</summary>
/// <remarks>
/// Seul le mode <c>app</c> est utilisé : il démarre le client Roblox sans exiger de session,
/// contrairement au mode <c>play</c> qui nécessite un ticket d'authentification.
/// </remarks>
public static class RobloxLaunchUriBuilder
{
    /// <summary>URI officielle lançant le client Roblox sans rejoindre d'expérience.</summary>
    public const string ClientLaunchUri = "roblox-player:1+launchmode:app";

    private const int MaximumLocaleLength = 16;

    /// <summary>Construit l'URI de lancement du client, avec les paramètres de langue si la locale est exploitable.</summary>
    /// <param name="locale">Locale courante, par exemple <c>fr_FR</c>.</param>
    public static string BuildClientLaunchUri(string? locale)
    {
        var normalizedLocale = NormalizeLocale(locale);

        return normalizedLocale is null
            ? ClientLaunchUri
            : $"{ClientLaunchUri}+robloxLocale:{normalizedLocale}+gameLocale:{normalizedLocale}";
    }

    /// <summary>
    /// Valide une locale avant de l'insérer dans une URI : seuls les caractères attendus
    /// sont acceptés, afin qu'aucun paramètre supplémentaire ne puisse être injecté.
    /// </summary>
    /// <param name="locale">Locale à valider.</param>
    public static bool IsSafeLocale(string? locale) => NormalizeLocale(locale) is not null;

    private static string? NormalizeLocale(string? locale)
    {
        if (string.IsNullOrWhiteSpace(locale))
        {
            return null;
        }

        var normalized = locale.Trim().Replace('-', '_');

        if (normalized.Length is 0 or > MaximumLocaleLength)
        {
            return null;
        }

        var isSafe = normalized.All(character => char.IsAsciiLetterOrDigit(character) || character == '_');

        return isSafe ? normalized : null;
    }
}

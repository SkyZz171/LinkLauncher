namespace LinkLauncher.Core.Services;

using System.Globalization;

/// <summary>Référence canonique vers une expérience officielle Roblox.</summary>
/// <param name="PlaceId">Identifiant de lieu, quand il figure dans le lien.</param>
/// <param name="Page">Adresse canonique de la page officielle.</param>
public sealed record RobloxExperienceReference(long? PlaceId, Uri Page);

/// <summary>Interprète les liens d'expériences Roblox saisis par l'utilisateur.</summary>
/// <remarks>
/// Seuls les hôtes officiels et les liens de la forme <c>/games/&lt;identifiant&gt;</c> sont
/// acceptés : un lien saisi ne peut donc pas détourner l'ouverture vers un autre site.
/// </remarks>
public static class RobloxExperienceLink
{
    /// <summary>Message affiché lorsque la saisie n'est pas un lien Roblox exploitable.</summary>
    public const string InvalidMessage =
        "Indiquez un lien roblox.com/games/… ou un identifiant de lieu numérique.";

    private const int MaximumPlaceIdDigits = 19;

    private static readonly string[] AllowedHosts =
    [
        "roblox.com",
        "www.roblox.com",
        "web.roblox.com"
    ];

    /// <summary>Interprète une saisie utilisateur (identifiant ou lien) en référence canonique.</summary>
    /// <param name="input">Saisie de l'utilisateur.</param>
    /// <param name="reference">Référence obtenue en cas de succès.</param>
    public static bool TryCreate(string? input, out RobloxExperienceReference? reference)
    {
        reference = null;

        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var candidate = input.Trim();

        if (long.TryParse(candidate, NumberStyles.None, CultureInfo.InvariantCulture, out var directPlaceId))
        {
            return TryCreateFromPlaceId(directPlaceId, out reference);
        }

        if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
        {
            return false;
        }

        if (!AllowedHosts.Contains(uri.Host, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 2 || !string.Equals(segments[0], "games", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return long.TryParse(segments[1], NumberStyles.None, CultureInfo.InvariantCulture, out var placeId) &&
               TryCreateFromPlaceId(placeId, out reference);
    }

    private static bool TryCreateFromPlaceId(long placeId, out RobloxExperienceReference? reference)
    {
        reference = null;

        if (placeId <= 0 || placeId.ToString(CultureInfo.InvariantCulture).Length > MaximumPlaceIdDigits)
        {
            return false;
        }

        reference = new RobloxExperienceReference(placeId, RobloxWebUrls.ExperiencePage(placeId));
        return true;
    }
}

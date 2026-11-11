namespace LinkLauncher.Core.Services;

using System.Globalization;

/// <summary>Adresses officielles de Roblox utilisées par LinkLauncher.</summary>
public static class RobloxWebUrls
{
    /// <summary>Page officielle de téléchargement du client Roblox.</summary>
    public static Uri OfficialDownloadPage { get; } = new("https://www.roblox.com/download");

    /// <summary>Page officielle d'une expérience identifiée par son lieu.</summary>
    /// <param name="placeId">Identifiant de lieu Roblox.</param>
    public static Uri ExperiencePage(long placeId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(placeId);

        return new Uri($"https://www.roblox.com/games/{placeId.ToString(CultureInfo.InvariantCulture)}");
    }
}

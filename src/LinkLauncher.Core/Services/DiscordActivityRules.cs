namespace LinkLauncher.Core.Services;

using LinkLauncher.Core.Models;

/// <summary>Construit l'activité Discord reflétant l'état réel de LinkLauncher.</summary>
public static class DiscordActivityRules
{
    /// <summary>Longueur maximale acceptée par Discord pour une ligne de texte.</summary>
    public const int MaximumTextLength = 128;

    private const string IdleDetails = "Dans LinkLauncher";
    private const string ClientDetails = "Client Roblox lancé";

    /// <summary>Crée une activité décrivant l'état courant.</summary>
    /// <param name="clientRunning">Indique que le client Roblox est observé.</param>
    /// <param name="experienceName">Expérience ouverte depuis la bibliothèque, si connue.</param>
    /// <param name="profileName">Profil de lancement actif, si connu.</param>
    /// <param name="startedAt">Moment de départ affiché.</param>
    public static DiscordActivity Create(
        bool clientRunning,
        string? experienceName,
        string? profileName,
        DateTimeOffset startedAt)
    {
        if (!clientRunning)
        {
            return new DiscordActivity
            {
                Details = IdleDetails,
                State = BuildState(profileName, "Aucun client lancé"),
                StartedAt = startedAt
            };
        }

        return new DiscordActivity
        {
            Details = ClientDetails,
            State = BuildState(profileName, string.IsNullOrWhiteSpace(experienceName)
                ? "Sans expérience"
                : $"Page : {experienceName}"),
            StartedAt = startedAt
        };
    }

    private static string BuildState(string? profileName, string fallback) =>
        Truncate(string.IsNullOrWhiteSpace(profileName) ? fallback : $"{fallback} · profil {profileName}");

    private static string Truncate(string text) =>
        text.Length <= MaximumTextLength ? text : text[..MaximumTextLength];
}

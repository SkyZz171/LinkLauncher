namespace LinkLauncher.Core.Models;

/// <summary>État de la présence Discord.</summary>
public enum DiscordPresenceState
{
    /// <summary>Présence désactivée par l'utilisateur ou sans identifiant d'application.</summary>
    Disabled = 0,

    /// <summary>Présence activée mais Discord n'est pas joignable.</summary>
    Unavailable = 1,

    /// <summary>Présence connectée à Discord.</summary>
    Connected = 2
}

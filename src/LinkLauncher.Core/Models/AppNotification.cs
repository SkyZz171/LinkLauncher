namespace LinkLauncher.Core.Models;

/// <summary>Nature d'une notification présentée à l'utilisateur.</summary>
public enum NotificationKind
{
    /// <summary>Le client Roblox a démarré.</summary>
    ClientStarted = 0,

    /// <summary>Le client Roblox s'est arrêté.</summary>
    ClientStopped = 1
}

/// <summary>Notification locale, sans contenu distant.</summary>
/// <param name="Kind">Nature de la notification.</param>
/// <param name="Title">Titre affiché.</param>
/// <param name="Body">Texte affiché.</param>
public sealed record AppNotification(NotificationKind Kind, string Title, string Body);

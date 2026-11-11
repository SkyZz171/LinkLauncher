namespace LinkLauncher.Core.Services;

using LinkLauncher.Core.Models;

/// <summary>Construit les notifications présentées pour les changements du client Roblox.</summary>
/// <remarks>
/// Les textes sont produits par des règles pures : l'interface ne fait que les afficher.
/// </remarks>
public static class AppNotificationRules
{
    private const string Category = "Roblox";

    /// <summary>Notification correspondant à une transition observée, ou <see langword="null"/> si rien n'est présenté.</summary>
    /// <param name="transition">Transition observée sur les processus Roblox.</param>
    public static AppNotification? Create(RobloxProcessTransition transition)
    {
        ArgumentNullException.ThrowIfNull(transition);

        var processId = transition.Process.ProcessId;

        return transition.Change switch
        {
            RobloxProcessChange.Started => new AppNotification(
                NotificationKind.ClientStarted,
                "Client Roblox lancé",
                $"Le client Roblox vient de démarrer (PID {processId})."),

            RobloxProcessChange.Stopped => new AppNotification(
                NotificationKind.ClientStopped,
                "Client Roblox fermé",
                transition.Process.ExitCode is { } exitCode
                    ? $"Le client Roblox s'est arrêté (code {exitCode})."
                    : "Le client Roblox s'est arrêté."),

            _ => null
        };
    }

    /// <summary>Catégorie de journal associée aux notifications.</summary>
    public static string LogCategory => Category;
}

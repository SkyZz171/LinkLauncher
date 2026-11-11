namespace LinkLauncher.Core.Services;

using LinkLauncher.Core.Models;

/// <summary>Ramène une configuration lue sur disque à un état valide et à jour.</summary>
public static class AppSettingsNormalizer
{
    /// <summary>
    /// Applique les valeurs par défaut aux champs absents ou inconnus, puis aligne
    /// la version du schéma sur celle de l'application.
    /// </summary>
    /// <param name="settings">Configuration lue, éventuellement incomplète.</param>
    public static AppSettings Normalize(AppSettings? settings)
    {
        if (settings is null)
        {
            return AppSettings.CreateDefault();
        }

        var defaults = AppSettings.CreateDefault();
        var discordApplicationId = DiscordApplicationIdRules.Normalize(settings.DiscordApplicationId);

        return settings with
        {
            SchemaVersion = AppSettings.CurrentSchemaVersion,
            Theme = Enum.IsDefined(settings.Theme) ? settings.Theme : defaults.Theme,
            AccentColor = Enum.IsDefined(settings.AccentColor) ? settings.AccentColor : defaults.AccentColor,
            DiscordApplicationId = discordApplicationId,
            // La présence ne peut pas être active sans identifiant d'application exploitable.
            EnableDiscordPresence = settings.EnableDiscordPresence && discordApplicationId is not null
        };
    }
}

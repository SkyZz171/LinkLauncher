namespace LinkLauncher.Core.Models;

/// <summary>État d'une installation Roblox détectée sur la machine.</summary>
public enum RobloxInstallationState
{
    /// <summary>Aucune vérification n'a encore abouti.</summary>
    Unknown = 0,

    /// <summary>Aucune installation Roblox n'a été détectée.</summary>
    NotInstalled = 1,

    /// <summary>Une installation existe mais n'est pas exploitable (fichiers manquants).</summary>
    Broken = 2,

    /// <summary>Une installation complète et utilisable a été détectée.</summary>
    Installed = 3
}

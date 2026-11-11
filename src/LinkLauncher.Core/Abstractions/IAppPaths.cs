namespace LinkLauncher.Core.Abstractions;

/// <summary>Emplacements utilisés par LinkLauncher sur le disque.</summary>
public interface IAppPaths
{
    /// <summary>Répertoire de configuration roaming.</summary>
    string ApplicationDataDirectory { get; }

    /// <summary>Chemin du fichier de configuration.</summary>
    string SettingsFilePath { get; }

    /// <summary>Chemin de la sauvegarde de la configuration.</summary>
    string SettingsBackupFilePath { get; }

    /// <summary>Répertoire de données local.</summary>
    string LocalDataDirectory { get; }

    /// <summary>Répertoire des journaux d'activité.</summary>
    string LogsDirectory { get; }
}

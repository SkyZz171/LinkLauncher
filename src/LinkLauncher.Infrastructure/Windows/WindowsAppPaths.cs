namespace LinkLauncher.Infrastructure.Windows;

using LinkLauncher.Core.Abstractions;

/// <summary>Emplacements des données de LinkLauncher sous Windows.</summary>
public sealed class WindowsAppPaths : IAppPaths
{
    private const string ProductFolderName = "LinkLauncher";
    private const string SettingsFileName = "settings.json";
    private const string SettingsBackupFileName = "settings.bak.json";

    /// <summary>Crée les chemins par défaut, sous les répertoires de données de l'utilisateur.</summary>
    public WindowsAppPaths()
        : this(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ProductFolderName),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ProductFolderName))
    {
    }

    /// <summary>Crée les chemins à partir de répertoires explicites.</summary>
    /// <param name="applicationDataDirectory">Répertoire de configuration.</param>
    /// <param name="localDataDirectory">Répertoire de données local.</param>
    public WindowsAppPaths(string applicationDataDirectory, string localDataDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationDataDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(localDataDirectory);

        ApplicationDataDirectory = applicationDataDirectory;
        LocalDataDirectory = localDataDirectory;
    }

    /// <inheritdoc />
    public string ApplicationDataDirectory { get; }

    /// <inheritdoc />
    public string SettingsFilePath => Path.Combine(ApplicationDataDirectory, SettingsFileName);

    /// <inheritdoc />
    public string SettingsBackupFilePath => Path.Combine(ApplicationDataDirectory, SettingsBackupFileName);

    /// <inheritdoc />
    public string LocalDataDirectory { get; }

    /// <inheritdoc />
    public string LogsDirectory => Path.Combine(LocalDataDirectory, "logs");
}

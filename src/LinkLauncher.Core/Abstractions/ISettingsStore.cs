namespace LinkLauncher.Core.Abstractions;

using LinkLauncher.Core.Models;

/// <summary>Accès persistant à la configuration de l'application.</summary>
public interface ISettingsStore
{
    /// <summary>Chemin du fichier de configuration.</summary>
    string SettingsFilePath { get; }

    /// <summary>Charge la configuration, ou renvoie les valeurs par défaut si elle est absente ou illisible.</summary>
    Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>Enregistre la configuration de façon atomique.</summary>
    Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default);
}

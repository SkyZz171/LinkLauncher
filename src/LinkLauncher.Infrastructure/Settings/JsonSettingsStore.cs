namespace LinkLauncher.Infrastructure.Settings;

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using LinkLauncher.Core.Abstractions;
using LinkLauncher.Core.Models;
using LinkLauncher.Core.Services;

/// <summary>
/// Conserve la configuration dans un fichier JSON unique, écrit de façon atomique.
/// </summary>
/// <remarks>
/// Un fichier illisible n'empêche jamais le démarrage : il est conservé à côté de la
/// configuration, les valeurs par défaut sont appliquées et l'incident est journalisé.
/// </remarks>
public sealed class JsonSettingsStore : ISettingsStore
{
    private const string Category = "Configuration";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly IFileSystem _fileSystem;
    private readonly IAppPaths _paths;
    private readonly IAppLogger _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>Crée le stockage de configuration.</summary>
    public JsonSettingsStore(IFileSystem fileSystem, IAppPaths paths, IAppLogger logger)
    {
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _paths = paths ?? throw new ArgumentNullException(nameof(paths));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public string SettingsFilePath => _paths.SettingsFilePath;

    /// <inheritdoc />
    public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!_fileSystem.FileExists(SettingsFilePath))
            {
                return AppSettings.CreateDefault();
            }

            string json;
            try
            {
                json = await _fileSystem.ReadAllTextAsync(SettingsFilePath, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _logger.Error(Category, "Lecture de la configuration impossible, valeurs par défaut appliquées.", ex);
                return AppSettings.CreateDefault();
            }

            try
            {
                var settings = JsonSerializer.Deserialize<AppSettings>(json, SerializerOptions);
                if (settings is null)
                {
                    _logger.Warn(Category, "Le fichier de configuration est vide, valeurs par défaut appliquées.");
                    PreserveUnreadableFile("vide");
                    return AppSettings.CreateDefault();
                }

                return AppSettingsNormalizer.Normalize(settings);
            }
            catch (JsonException ex)
            {
                _logger.Error(Category, "Le fichier de configuration est illisible, valeurs par défaut appliquées.", ex);
                PreserveUnreadableFile("invalide");
                return AppSettings.CreateDefault();
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        var temporaryPath = SettingsFilePath + ".tmp";
        try
        {
            _fileSystem.CreateDirectory(_paths.ApplicationDataDirectory);

            var json = JsonSerializer.Serialize(settings, SerializerOptions);
            await _fileSystem.WriteAllTextAsync(temporaryPath, json, cancellationToken).ConfigureAwait(false);

            if (_fileSystem.FileExists(SettingsFilePath))
            {
                _fileSystem.CopyFile(SettingsFilePath, _paths.SettingsBackupFilePath, overwrite: true);
            }

            _fileSystem.MoveFile(temporaryPath, SettingsFilePath, overwrite: true);
        }
        finally
        {
            try
            {
                _fileSystem.DeleteFile(temporaryPath);
            }
            catch (IOException ex)
            {
                _logger.Warn(Category, $"Suppression du fichier temporaire impossible : {ex.Message}");
            }

            _gate.Release();
        }
    }

    private void PreserveUnreadableFile(string reason)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        var targetPath = Path.Combine(_paths.ApplicationDataDirectory, $"settings.{reason}-{timestamp}.json");

        try
        {
            _fileSystem.CopyFile(SettingsFilePath, targetPath, overwrite: false);
            _logger.Warn(Category, $"Configuration illisible conservée dans {targetPath}.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.Warn(Category, $"Copie de la configuration illisible impossible : {ex.Message}");
        }
    }
}

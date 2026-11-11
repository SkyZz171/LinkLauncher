namespace LinkLauncher.Infrastructure.Activity;

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using LinkLauncher.Core.Abstractions;
using LinkLauncher.Core.Models;
using LinkLauncher.Core.Services;

/// <summary>Conserve l'historique des sessions dans un fichier JSON unique, écrit de façon atomique.</summary>
/// <remarks>
/// Un fichier illisible n'empêche jamais le démarrage : il est conservé à côté de l'historique,
/// un historique vide est proposé et l'incident est journalisé.
/// </remarks>
public sealed class JsonActivityHistoryStore : IActivityHistoryStore
{
    private const string Category = "Activité";
    private const string HistoryFileName = "activity.json";

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

    /// <summary>Crée le stockage de l'historique.</summary>
    public JsonActivityHistoryStore(IFileSystem fileSystem, IAppPaths paths, IAppLogger logger)
    {
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _paths = paths ?? throw new ArgumentNullException(nameof(paths));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public string HistoryFilePath => Path.Combine(_paths.ApplicationDataDirectory, HistoryFileName);

    /// <inheritdoc />
    public async Task<ActivityHistory> LoadAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!_fileSystem.FileExists(HistoryFilePath))
            {
                return ActivityHistoryRules.CreateDefault();
            }

            string json;
            try
            {
                json = await _fileSystem.ReadAllTextAsync(HistoryFilePath, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _logger.Error(Category, "Lecture de l'historique impossible, historique vide proposé.", ex);
                return ActivityHistoryRules.CreateDefault();
            }

            try
            {
                var history = JsonSerializer.Deserialize<ActivityHistory>(json, SerializerOptions);
                if (history is null)
                {
                    _logger.Warn(Category, "Le fichier d'historique est vide, historique vide proposé.");
                    PreserveUnreadableFile("vide");
                    return ActivityHistoryRules.CreateDefault();
                }

                return ActivityHistoryRules.Normalize(history, DateTimeOffset.Now);
            }
            catch (JsonException ex)
            {
                _logger.Error(Category, "Le fichier d'historique est illisible, historique vide proposé.", ex);
                PreserveUnreadableFile("invalide");
                return ActivityHistoryRules.CreateDefault();
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task SaveAsync(ActivityHistory history, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(history);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        var temporaryPath = HistoryFilePath + ".tmp";
        try
        {
            _fileSystem.CreateDirectory(_paths.ApplicationDataDirectory);

            var json = JsonSerializer.Serialize(history, SerializerOptions);
            await _fileSystem.WriteAllTextAsync(temporaryPath, json, cancellationToken).ConfigureAwait(false);
            _fileSystem.MoveFile(temporaryPath, HistoryFilePath, overwrite: true);
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
        var targetPath = Path.Combine(_paths.ApplicationDataDirectory, $"activity.{reason}-{timestamp}.json");

        try
        {
            _fileSystem.CopyFile(HistoryFilePath, targetPath, overwrite: false);
            _logger.Warn(Category, $"Historique illisible conservé dans {targetPath}.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.Warn(Category, $"Copie de l'historique illisible impossible : {ex.Message}");
        }
    }
}

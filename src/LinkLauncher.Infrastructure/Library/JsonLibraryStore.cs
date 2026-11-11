namespace LinkLauncher.Infrastructure.Library;

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using LinkLauncher.Core.Abstractions;
using LinkLauncher.Core.Models;
using LinkLauncher.Core.Services;

/// <summary>Conserve la bibliothèque dans un fichier JSON unique, écrit de façon atomique.</summary>
/// <remarks>
/// Un fichier illisible n'empêche jamais le démarrage : il est conservé à côté de la bibliothèque,
/// une bibliothèque par défaut est proposée et l'incident est journalisé.
/// </remarks>
public sealed class JsonLibraryStore : ILibraryStore
{
    private const string Category = "Bibliothèque";

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

    /// <summary>Crée le stockage de la bibliothèque.</summary>
    public JsonLibraryStore(IFileSystem fileSystem, IAppPaths paths, IAppLogger logger)
    {
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _paths = paths ?? throw new ArgumentNullException(nameof(paths));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public string LibraryFilePath => _paths.LibraryFilePath;

    /// <inheritdoc />
    public async Task<LauncherLibrary> LoadAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!_fileSystem.FileExists(LibraryFilePath))
            {
                return LauncherLibraryRules.CreateDefault(DateTimeOffset.Now);
            }

            string json;
            try
            {
                json = await _fileSystem.ReadAllTextAsync(LibraryFilePath, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _logger.Error(Category, "Lecture de la bibliothèque impossible, contenu par défaut proposé.", ex);
                return LauncherLibraryRules.CreateDefault(DateTimeOffset.Now);
            }

            try
            {
                var library = JsonSerializer.Deserialize<LauncherLibrary>(json, SerializerOptions);
                if (library is null)
                {
                    _logger.Warn(Category, "Le fichier de bibliothèque est vide, contenu par défaut proposé.");
                    PreserveUnreadableFile("vide");
                    return LauncherLibraryRules.CreateDefault(DateTimeOffset.Now);
                }

                return LauncherLibraryRules.Normalize(library, DateTimeOffset.Now);
            }
            catch (JsonException ex)
            {
                _logger.Error(Category, "Le fichier de bibliothèque est illisible, contenu par défaut proposé.", ex);
                PreserveUnreadableFile("invalide");
                return LauncherLibraryRules.CreateDefault(DateTimeOffset.Now);
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task SaveAsync(LauncherLibrary library, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(library);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        var temporaryPath = LibraryFilePath + ".tmp";
        try
        {
            _fileSystem.CreateDirectory(_paths.ApplicationDataDirectory);

            var json = JsonSerializer.Serialize(library, SerializerOptions);
            await _fileSystem.WriteAllTextAsync(temporaryPath, json, cancellationToken).ConfigureAwait(false);
            _fileSystem.MoveFile(temporaryPath, LibraryFilePath, overwrite: true);
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
        var targetPath = Path.Combine(_paths.ApplicationDataDirectory, $"library.{reason}-{timestamp}.json");

        try
        {
            _fileSystem.CopyFile(LibraryFilePath, targetPath, overwrite: false);
            _logger.Warn(Category, $"Bibliothèque illisible conservée dans {targetPath}.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.Warn(Category, $"Copie de la bibliothèque illisible impossible : {ex.Message}");
        }
    }
}

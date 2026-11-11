namespace LinkLauncher.Updater;

using System.Net.Http;
using System.Security.Cryptography;
using LinkLauncher.Core.Abstractions;
using LinkLauncher.Core.Models;
using LinkLauncher.Core.Services;

/// <summary>Télécharge un paquet de mise à jour et vérifie son empreinte SHA-256.</summary>
/// <remarks>
/// Une publication sans empreinte est refusée : LinkLauncher n'exécute jamais un binaire qu'il
/// n'a pas pu vérifier. Le paquet est conservé sous <c>%LOCALAPPDATA%\LinkLauncher\updates</c>.
/// </remarks>
public sealed class UpdateDownloader : IUpdateDownloader
{
    private const string Category = "Mises à jour";
    private const string UpdatesFolderName = "updates";

    private readonly HttpClient _httpClient;
    private readonly IAppPaths _paths;
    private readonly IAppLogger _logger;

    /// <summary>Crée le téléchargeur de mise à jour.</summary>
    public UpdateDownloader(HttpClient httpClient, IAppPaths paths, IAppLogger logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _paths = paths ?? throw new ArgumentNullException(nameof(paths));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<UpdateDownloadResult> DownloadAsync(
        UpdateRelease release,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(release);

        if (!release.IsVerifiable)
        {
            return UpdateDownloadResult.Failure(
                "Cette publication ne fournit pas d'empreinte SHA-256 : l'installation est refusée par sécurité.");
        }

        var fileName = Path.GetFileName(release.DownloadUrl.LocalPath);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return UpdateDownloadResult.Failure("Le paquet de mise à jour n'a pas de nom de fichier exploitable.");
        }

        var directory = Path.Combine(_paths.LocalDataDirectory, UpdatesFolderName, release.Version.ToString());
        var targetPath = Path.Combine(directory, fileName);

        try
        {
            Directory.CreateDirectory(directory);

            using var response = await _httpClient
                .GetAsync(release.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.Warn(Category, $"Téléchargement refusé (HTTP {(int)response.StatusCode}).");
                return UpdateDownloadResult.Failure($"Le téléchargement a échoué (HTTP {(int)response.StatusCode}).");
            }

            await using (var source = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false))
            await using (var destination = File.Create(targetPath))
            {
                await source.CopyToAsync(destination, cancellationToken).ConfigureAwait(false);
            }

            var actualHash = await ComputeSha256Async(targetPath, cancellationToken).ConfigureAwait(false);

            if (!string.Equals(actualHash, release.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                File.Delete(targetPath);
                _logger.Error(
                    Category,
                    $"Empreinte du paquet invalide : attendue {release.Sha256}, obtenue {actualHash}.",
                    new InvalidDataException("Empreinte SHA-256 différente."));
                return UpdateDownloadResult.Failure(
                    "L'empreinte du paquet ne correspond pas à celle publiée : le fichier a été supprimé.");
            }

            _logger.Info(Category, $"Paquet {release.Version} téléchargé et vérifié : {targetPath}.");
            return UpdateDownloadResult.Success(targetPath);
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or UnauthorizedAccessException)
        {
            _logger.Error(Category, "Téléchargement de la mise à jour impossible.", ex);
            return UpdateDownloadResult.Failure("Le paquet n'a pas pu être téléchargé. Le détail figure dans le journal d'activité.");
        }
    }

    private static async Task<string> ComputeSha256Async(string filePath, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(filePath);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);

        return Convert.ToHexStringLower(hash);
    }
}

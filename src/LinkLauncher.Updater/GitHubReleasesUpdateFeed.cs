namespace LinkLauncher.Updater;

using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using LinkLauncher.Core.Abstractions;
using LinkLauncher.Core.Models;
using LinkLauncher.Core.Services;

/// <summary>Lit les publications GitHub du dépôt de LinkLauncher.</summary>
/// <remarks>
/// Aucune authentification n'est nécessaire : seules les publications publiques sont lues, et
/// l'empreinte SHA-256 doit figurer dans les notes de version pour que le paquet soit installable.
/// </remarks>
public sealed class GitHubReleasesUpdateFeed : IUpdateFeed
{
    /// <summary>Dépôt surveillé.</summary>
    public const string RepositorySlug = "SkyZz171/LinkLauncher";

    private const string Category = "Mises à jour";
    private const string InstallerExtension = ".exe";

    private readonly HttpClient _httpClient;
    private readonly IAppLogger _logger;

    /// <summary>Crée la source de publications.</summary>
    public GitHubReleasesUpdateFeed(HttpClient httpClient, IAppLogger logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Adresse de l'API des publications.</summary>
    public static Uri ReleasesApi { get; } =
        new($"https://api.github.com/repos/{RepositorySlug}/releases");

    /// <inheritdoc />
    public async Task<IReadOnlyList<UpdateRelease>> GetReleasesAsync(CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, ReleasesApi);
        request.Headers.UserAgent.ParseAdd("LinkLauncher");

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            _logger.Warn(Category, $"Publication des versions inaccessible (HTTP {(int)response.StatusCode}).");
            throw new HttpRequestException($"Réponse HTTP {(int)response.StatusCode} de GitHub.");
        }

        var payload = await response.Content
            .ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return Parse(payload);
    }

    /// <summary>Interprète la réponse JSON de GitHub.</summary>
    /// <param name="payload">Tableau des publications.</param>
    public static IReadOnlyList<UpdateRelease> Parse(JsonElement payload)
    {
        if (payload.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var releases = new List<UpdateRelease>();

        foreach (var item in payload.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            if (item.TryGetProperty("draft", out var draft) && draft.ValueKind == JsonValueKind.True)
            {
                continue;
            }

            var tag = item.TryGetProperty("tag_name", out var tagElement) ? tagElement.GetString() : null;
            if (!AppVersion.TryParse(tag, out var version))
            {
                continue;
            }

            var downloadUrl = FindInstallerUrl(item);
            if (downloadUrl is null)
            {
                continue;
            }

            var notes = item.TryGetProperty("body", out var body) ? body.GetString() : null;

            releases.Add(new UpdateRelease
            {
                Version = version,
                Tag = tag!,
                DownloadUrl = downloadUrl,
                Sha256 = UpdateRules.TryExtractSha256(notes),
                Notes = notes
            });
        }

        return releases;
    }

    private static Uri? FindInstallerUrl(JsonElement release)
    {
        if (!release.TryGetProperty("assets", out var assets) || assets.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var asset in assets.EnumerateArray())
        {
            if (asset.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var name = asset.TryGetProperty("name", out var nameElement) ? nameElement.GetString() : null;
            var url = asset.TryGetProperty("browser_download_url", out var urlElement) ? urlElement.GetString() : null;

            if (name is null || url is null ||
                !name.EndsWith(InstallerExtension, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (Uri.TryCreate(url, UriKind.Absolute, out var parsed))
            {
                return parsed;
            }
        }

        return null;
    }
}

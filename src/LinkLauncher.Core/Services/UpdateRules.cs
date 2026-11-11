namespace LinkLauncher.Core.Services;

using LinkLauncher.Core.Models;

/// <summary>Règles pures de sélection et de vérification des mises à jour.</summary>
public static class UpdateRules
{
    private const int Sha256HexLength = 64;

    /// <summary>Choisit la publication la plus récente, si elle dépasse la version installée.</summary>
    /// <param name="current">Version installée.</param>
    /// <param name="releases">Publications disponibles.</param>
    public static UpdateCheckResult Evaluate(AppVersion current, IReadOnlyList<UpdateRelease>? releases)
    {
        if (releases is null || releases.Count == 0)
        {
            return UpdateCheckResult.UpToDate("Aucune publication disponible : vous utilisez la version connue la plus récente.");
        }

        var latest = releases
            .Where(release => release is not null)
            .OrderByDescending(release => release.Version)
            .FirstOrDefault();

        if (latest is null || !latest.Version.IsNewerThan(current))
        {
            return UpdateCheckResult.UpToDate($"LinkLauncher {current} est à jour.");
        }

        return UpdateCheckResult.Available(latest);
    }

    /// <summary>Extrait une empreinte SHA-256 d'un texte de publication.</summary>
    /// <param name="text">Texte examiné, par exemple <c>sha256:abc…</c>.</param>
    public static string? TryExtractSha256(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        foreach (var token in text.Split([' ', '\t', '\r', '\n', '`', '*', ':', '=', '(', ')'], StringSplitOptions.RemoveEmptyEntries))
        {
            if (token.Length == Sha256HexLength && token.All(character => char.IsAsciiHexDigit(character)))
            {
                return token.ToLowerInvariant();
            }
        }

        return null;
    }

    /// <summary>Indique qu'une valeur est une empreinte SHA-256 hexadécimale exploitable.</summary>
    /// <param name="value">Valeur examinée.</param>
    public static bool IsSha256(string? value) =>
        value is { Length: Sha256HexLength } && value.All(character => char.IsAsciiHexDigit(character));

    /// <summary>Formate une version pour l'affichage.</summary>
    /// <param name="version">Version à afficher.</param>
    public static string Format(AppVersion version) => version.ToString();
}

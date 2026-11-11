namespace LinkLauncher.Core.Services;

using System.Globalization;

/// <summary>
/// Analyse les chaînes de version produites par Windows pour les exécutables Roblox,
/// par exemple <c>0, 747, 0, 7471481</c>.
/// </summary>
public static class RobloxVersionParser
{
    /// <summary>Convertit une chaîne de version en <see cref="Version"/> comparable.</summary>
    /// <param name="rawVersion">Chaîne brute issue des métadonnées du fichier.</param>
    /// <returns>La version analysée, ou <see langword="null"/> si la chaîne n'est pas exploitable.</returns>
    public static Version? TryParse(string? rawVersion)
    {
        if (string.IsNullOrWhiteSpace(rawVersion))
        {
            return null;
        }

        var tokens = rawVersion
            .Replace(',', '.')
            .Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (tokens.Length is 0 or > 4)
        {
            return null;
        }

        var numbers = new int[tokens.Length];
        for (var index = 0; index < tokens.Length; index++)
        {
            if (!int.TryParse(tokens[index], NumberStyles.None, CultureInfo.InvariantCulture, out numbers[index]))
            {
                return null;
            }
        }

        return numbers.Length switch
        {
            1 => new Version(numbers[0], 0),
            2 => new Version(numbers[0], numbers[1]),
            3 => new Version(numbers[0], numbers[1], numbers[2]),
            _ => new Version(numbers[0], numbers[1], numbers[2], numbers[3])
        };
    }
}

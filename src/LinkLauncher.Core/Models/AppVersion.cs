namespace LinkLauncher.Core.Models;

using System.Globalization;

/// <summary>Version applicative comparable, de 1 à 4 nombres séparés par des points.</summary>
/// <param name="Major">Composant majeur.</param>
/// <param name="Minor">Composant mineur.</param>
/// <param name="Patch">Correctif.</param>
/// <param name="Revision">Révision, facultative.</param>
public readonly record struct AppVersion(int Major, int Minor, int Patch, int Revision) : IComparable<AppVersion>
{
    /// <summary>Interprète une version, avec ou sans préfixe <c>v</c>.</summary>
    /// <param name="value">Texte à interpréter, par exemple <c>v1.2.3</c>.</param>
    /// <param name="version">Version obtenue.</param>
    public static bool TryParse(string? value, out AppVersion version)
    {
        version = default;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var candidate = value.Trim();
        if (candidate.StartsWith('v') || candidate.StartsWith('V'))
        {
            candidate = candidate[1..];
        }

        var parts = candidate.Split('.', StringSplitOptions.TrimEntries);
        if (parts.Length is 0 or > 4)
        {
            return false;
        }

        var numbers = new int[4];

        for (var index = 0; index < parts.Length; index++)
        {
            if (!int.TryParse(parts[index], NumberStyles.None, CultureInfo.InvariantCulture, out var number))
            {
                return false;
            }

            numbers[index] = number;
        }

        version = new AppVersion(numbers[0], numbers[1], numbers[2], numbers[3]);
        return true;
    }

    /// <summary>Interprète une version ou lève une exception.</summary>
    /// <param name="value">Texte à interpréter.</param>
    public static AppVersion Parse(string value) =>
        TryParse(value, out var version)
            ? version
            : throw new FormatException($"Version illisible : « {value} ».");

    /// <summary>Indique que cette version est plus récente que l'autre.</summary>
    /// <param name="other">Version comparée.</param>
    public bool IsNewerThan(AppVersion other) => CompareTo(other) > 0;

    /// <inheritdoc />
    public int CompareTo(AppVersion other)
    {
        var comparison = Major.CompareTo(other.Major);
        if (comparison != 0)
        {
            return comparison;
        }

        comparison = Minor.CompareTo(other.Minor);
        if (comparison != 0)
        {
            return comparison;
        }

        comparison = Patch.CompareTo(other.Patch);
        return comparison != 0 ? comparison : Revision.CompareTo(other.Revision);
    }

    /// <inheritdoc />
    public override string ToString() =>
        Revision > 0
            ? $"{Major.ToString(CultureInfo.InvariantCulture)}.{Minor.ToString(CultureInfo.InvariantCulture)}.{Patch.ToString(CultureInfo.InvariantCulture)}.{Revision.ToString(CultureInfo.InvariantCulture)}"
            : $"{Major.ToString(CultureInfo.InvariantCulture)}.{Minor.ToString(CultureInfo.InvariantCulture)}.{Patch.ToString(CultureInfo.InvariantCulture)}";
}

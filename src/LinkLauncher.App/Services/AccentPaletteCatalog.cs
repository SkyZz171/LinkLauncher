namespace LinkLauncher.App.Services;

using System.Globalization;
using System.Windows.Media;
using LinkLauncher.Core.Models;

/// <summary>Couleurs d'accentuation de LinkLauncher et dérivés utilisés par l'interface.</summary>
public static class AccentPaletteCatalog
{
    private static readonly Color AuroraPrimary = Color.FromRgb(0x7C, 0x5C, 0xFF);

    /// <summary>Couleur principale d'une palette.</summary>
    public static Color GetPrimaryColor(AccentColorKey key) => key switch
    {
        AccentColorKey.Azure => Color.FromRgb(0x2F, 0x80, 0xED),
        AccentColorKey.Emerald => Color.FromRgb(0x1F, 0xA9, 0x7A),
        AccentColorKey.Amber => Color.FromRgb(0xD9, 0x8B, 0x00),
        AccentColorKey.Coral => Color.FromRgb(0xE0, 0x5C, 0x55),
        AccentColorKey.Violet => Color.FromRgb(0xA8, 0x55, 0xF7),
        _ => AuroraPrimary
    };

    /// <summary>Fond des surfaces sombres, utilisé pour composer les teintes translucides.</summary>
    public static Color DarkSurface => Color.FromRgb(0x17, 0x1A, 0x21);

    /// <summary>Fond des surfaces claires, utilisé pour composer les teintes translucides.</summary>
    public static Color LightSurface => Color.FromRgb(0xFF, 0xFF, 0xFF);

    /// <summary>Éclaircit une couleur.</summary>
    public static Color Lighten(Color color, double amount) => Blend(color, Colors.White, amount);

    /// <summary>Assombrit une couleur.</summary>
    public static Color Darken(Color color, double amount) => Blend(color, Colors.Black, amount);

    /// <summary>Mélange deux couleurs selon un ratio.</summary>
    public static Color Blend(Color from, Color to, double ratio)
    {
        ratio = Math.Clamp(ratio, 0d, 1d);

        return Color.FromRgb(
            BlendChannel(from.R, to.R, ratio),
            BlendChannel(from.G, to.G, ratio),
            BlendChannel(from.B, to.B, ratio));
    }

    /// <summary>Convertit une couleur en chaîne hexadécimale, utile pour le diagnostic.</summary>
    public static string ToHex(Color color) => string.Format(
        CultureInfo.InvariantCulture,
        "#{0:X2}{1:X2}{2:X2}",
        color.R,
        color.G,
        color.B);

    private static byte BlendChannel(byte from, byte to, double ratio) =>
        (byte)Math.Round(from + ((to - from) * ratio), MidpointRounding.AwayFromZero);
}

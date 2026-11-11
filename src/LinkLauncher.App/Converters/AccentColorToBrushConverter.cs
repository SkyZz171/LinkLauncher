namespace LinkLauncher.App.Converters;

using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using LinkLauncher.App.Services;
using LinkLauncher.Core.Models;

/// <summary>Convertit une couleur d'accentuation en pinceau pour les pastilles de sélection.</summary>
public sealed class AccentColorToBrushConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var key = value is AccentColorKey accent ? accent : AccentColorKey.Aurora;
        var brush = new SolidColorBrush(AccentPaletteCatalog.GetPrimaryColor(key));
        brush.Freeze();
        return brush;
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

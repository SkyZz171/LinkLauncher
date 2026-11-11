namespace LinkLauncher.App.Converters;

using System.Globalization;
using System.Windows;
using System.Windows.Data;

/// <summary>Affiche un élément seulement lorsqu'une valeur texte ou un objet est présent.</summary>
public sealed class NullToVisibilityConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var isEmpty = value switch
        {
            null => true,
            string text => string.IsNullOrWhiteSpace(text),
            _ => false
        };

        return isEmpty ? Visibility.Collapsed : Visibility.Visible;
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

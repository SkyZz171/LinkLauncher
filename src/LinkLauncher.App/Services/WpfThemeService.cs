namespace LinkLauncher.App.Services;

using System.Windows;
using System.Windows.Media;
using LinkLauncher.Core.Abstractions;
using LinkLauncher.Core.Models;

/// <summary>
/// Applique le thème et la couleur d'accentuation aux ressources WPF de l'application.
/// </summary>
/// <remarks>
/// Les styles de l'application référencent les pinceaux par <c>DynamicResource</c> :
/// le changement est donc immédiat, sans redémarrage.
/// </remarks>
public sealed class WpfThemeService : IThemeService
{
    private const string DarkDictionaryFileName = "Dark.xaml";
    private const string LightDictionaryFileName = "Light.xaml";

    /// <inheritdoc />
    public AppTheme AppliedTheme { get; private set; } = AppTheme.Dark;

    /// <inheritdoc />
    public AccentColorKey AppliedAccent { get; private set; } = AccentColorKey.Aurora;

    /// <inheritdoc />
    public event EventHandler? ThemeApplied;

    /// <inheritdoc />
    public void Apply(AppTheme theme, AccentColorKey accent)
    {
        if (theme == AppTheme.System)
        {
            throw new ArgumentException("Le thème appliqué doit être explicite.", nameof(theme));
        }

        var application = Application.Current
            ?? throw new InvalidOperationException("Aucune application WPF n'est active.");

        ReplaceThemeDictionary(application, theme);
        ReplaceAccentBrushes(application, accent, theme);

        AppliedTheme = theme;
        AppliedAccent = accent;
        ThemeApplied?.Invoke(this, EventArgs.Empty);
    }

    private static void ReplaceThemeDictionary(Application application, AppTheme theme)
    {
        var fileName = theme == AppTheme.Light ? LightDictionaryFileName : DarkDictionaryFileName;
        var source = new Uri($"Themes/{fileName}", UriKind.Relative);
        var dictionaries = application.Resources.MergedDictionaries;

        for (var index = 0; index < dictionaries.Count; index++)
        {
            var existingSource = dictionaries[index].Source?.OriginalString;
            if (existingSource is null)
            {
                continue;
            }

            if (existingSource.EndsWith(DarkDictionaryFileName, StringComparison.OrdinalIgnoreCase) ||
                existingSource.EndsWith(LightDictionaryFileName, StringComparison.OrdinalIgnoreCase))
            {
                dictionaries[index] = new ResourceDictionary { Source = source };
                return;
            }
        }

        dictionaries.Insert(0, new ResourceDictionary { Source = source });
    }

    private static void ReplaceAccentBrushes(Application application, AccentColorKey accent, AppTheme theme)
    {
        var primary = AccentPaletteCatalog.GetPrimaryColor(accent);
        var surface = theme == AppTheme.Dark ? AccentPaletteCatalog.DarkSurface : AccentPaletteCatalog.LightSurface;

        application.Resources["Brush.Accent"] = CreateBrush(primary);
        application.Resources["Brush.Accent.Hover"] = CreateBrush(AccentPaletteCatalog.Lighten(primary, 0.14));
        application.Resources["Brush.Accent.Pressed"] = CreateBrush(AccentPaletteCatalog.Darken(primary, 0.12));
        application.Resources["Brush.Accent.Subtle"] = CreateBrush(
            AccentPaletteCatalog.Blend(surface, primary, theme == AppTheme.Dark ? 0.22 : 0.12));
        application.Resources["Brush.Accent.Border"] = CreateBrush(
            AccentPaletteCatalog.Blend(surface, primary, theme == AppTheme.Dark ? 0.45 : 0.32));
    }

    private static SolidColorBrush CreateBrush(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }
}

namespace LinkLauncher.Core.Abstractions;

using LinkLauncher.Core.Models;

/// <summary>Application du thème et de la couleur d'accentuation à l'interface.</summary>
public interface IThemeService
{
    /// <summary>Thème effectivement appliqué.</summary>
    AppTheme AppliedTheme { get; }

    /// <summary>Couleur d'accentuation effectivement appliquée.</summary>
    AccentColorKey AppliedAccent { get; }

    /// <summary>Survient après chaque application d'un thème.</summary>
    event EventHandler? ThemeApplied;

    /// <summary>
    /// Applique un thème explicite. Doit être appelée depuis le thread de l'interface.
    /// </summary>
    /// <param name="theme">Thème effectif (clair ou sombre), jamais <see cref="AppTheme.System"/>.</param>
    /// <param name="accent">Couleur d'accentuation.</param>
    void Apply(AppTheme theme, AccentColorKey accent);
}

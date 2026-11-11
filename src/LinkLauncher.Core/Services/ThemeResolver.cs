namespace LinkLauncher.Core.Services;

using LinkLauncher.Core.Models;

/// <summary>Détermine le thème réellement appliqué à partir du thème demandé.</summary>
public static class ThemeResolver
{
    /// <summary>
    /// Résout le thème effectif : un thème explicite est conservé, <see cref="AppTheme.System"/>
    /// reprend le thème du système et retombe sur le thème sombre si celui-ci est indéterminé.
    /// </summary>
    /// <param name="requested">Thème demandé par l'utilisateur.</param>
    /// <param name="systemTheme">Thème constaté dans Windows.</param>
    public static AppTheme ResolveEffective(AppTheme requested, AppTheme systemTheme) => requested switch
    {
        AppTheme.Light => AppTheme.Light,
        AppTheme.Dark => AppTheme.Dark,
        _ => systemTheme == AppTheme.Light ? AppTheme.Light : AppTheme.Dark
    };
}

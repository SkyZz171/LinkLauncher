namespace LinkLauncher.Core.Abstractions;

using LinkLauncher.Core.Models;

/// <summary>Source du thème configuré dans Windows.</summary>
public interface ISystemThemeSource
{
    /// <summary>Renvoie le thème clair ou sombre actuellement configuré dans Windows.</summary>
    AppTheme GetSystemTheme();
}

namespace LinkLauncher.Infrastructure.Windows;

using System.Security;
using LinkLauncher.Core.Abstractions;
using LinkLauncher.Core.Models;
using Microsoft.Win32;

/// <summary>Lit le thème clair ou sombre configuré pour les applications Windows.</summary>
public sealed class WindowsThemeSource : ISystemThemeSource
{
    private const string PersonalizeKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string AppsThemeValueName = "AppsUseLightTheme";

    /// <inheritdoc />
    public AppTheme GetSystemTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(PersonalizeKeyPath);
            return key?.GetValue(AppsThemeValueName) switch
            {
                1 => AppTheme.Light,
                _ => AppTheme.Dark
            };
        }
        catch (Exception ex) when (ex is SecurityException or UnauthorizedAccessException or IOException)
        {
            return AppTheme.Dark;
        }
    }
}

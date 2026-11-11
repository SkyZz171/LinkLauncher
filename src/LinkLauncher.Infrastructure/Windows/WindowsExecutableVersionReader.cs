namespace LinkLauncher.Infrastructure.Windows;

using System.Diagnostics;
using LinkLauncher.Core.Abstractions;

/// <summary>Lit la version déclarée dans les métadonnées d'un exécutable Windows.</summary>
public sealed class WindowsExecutableVersionReader : IExecutableVersionReader
{
    /// <inheritdoc />
    public string? ReadFileVersion(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        try
        {
            var versionInfo = FileVersionInfo.GetVersionInfo(path);
            return string.IsNullOrWhiteSpace(versionInfo.FileVersion) ? null : versionInfo.FileVersion;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return null;
        }
    }
}

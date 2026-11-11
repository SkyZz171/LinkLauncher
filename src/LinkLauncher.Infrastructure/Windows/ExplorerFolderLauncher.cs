namespace LinkLauncher.Infrastructure.Windows;

using System.ComponentModel;
using System.Diagnostics;
using LinkLauncher.Core.Abstractions;

/// <summary>Ouvre un dossier dans l'explorateur Windows.</summary>
public sealed class ExplorerFolderLauncher : IFolderLauncher
{
    private const string Category = "Explorateur";

    private readonly IAppLogger _logger;

    /// <summary>Crée le service d'ouverture de dossier.</summary>
    public ExplorerFolderLauncher(IAppLogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public bool TryOpenFolder(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        try
        {
            if (!Directory.Exists(path))
            {
                _logger.Warn(Category, $"Dossier introuvable : {path}");
                return false;
            }

            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{path}\"",
                UseShellExecute = true
            });

            return process is not null;
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or IOException)
        {
            _logger.Error(Category, $"Ouverture du dossier impossible : {path}", ex);
            return false;
        }
    }
}

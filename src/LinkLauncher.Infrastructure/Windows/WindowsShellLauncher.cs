namespace LinkLauncher.Infrastructure.Windows;

using System.ComponentModel;
using System.Diagnostics;
using LinkLauncher.Core.Abstractions;

/// <summary>Transmet une demande d'ouverture au shell Windows (protocole associé, URL, exécutable).</summary>
public sealed class WindowsShellLauncher : IShellLauncher
{
    private const string Category = "Shell";

    private readonly IAppLogger _logger;

    /// <summary>Crée le lanceur système.</summary>
    public WindowsShellLauncher(IAppLogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public ShellLaunchResult Launch(string target)
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            return new ShellLaunchResult(false, "La cible à ouvrir est vide.");
        }

        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = target,
                UseShellExecute = true
            });

            return new ShellLaunchResult(true);
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or FileNotFoundException or PlatformNotSupportedException)
        {
            _logger.Error(Category, $"Windows n'a pas pu ouvrir : {target}", ex);
            return new ShellLaunchResult(false, ex.Message);
        }
    }
}

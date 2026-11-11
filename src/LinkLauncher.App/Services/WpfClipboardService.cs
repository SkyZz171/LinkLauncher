namespace LinkLauncher.App.Services;

using System.Runtime.InteropServices;
using System.Windows;
using LinkLauncher.Core.Abstractions;

/// <summary>Copie du texte dans le presse-papiers Windows.</summary>
public sealed class WpfClipboardService : IClipboardService
{
    /// <inheritdoc />
    public bool TrySetText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        try
        {
            Clipboard.SetText(text);
            return true;
        }
        catch (Exception ex) when (ex is COMException or ExternalException or InvalidOperationException)
        {
            // Le presse-papiers peut être momentanément verrouillé par un autre programme.
            return false;
        }
    }
}

namespace LinkLauncher.Infrastructure.Windows;

using System.Security;
using LinkLauncher.Core.Abstractions;
using Microsoft.Win32;

/// <summary>Lit l'association d'un protocole dans le registre de l'utilisateur courant.</summary>
public sealed class RegistryProtocolReader : IProtocolRegistrationReader
{
    /// <inheritdoc />
    public string? TryGetCommand(string protocolScheme)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(protocolScheme);

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey($@"Software\Classes\{protocolScheme}\shell\open\command");
            return key?.GetValue(null) as string;
        }
        catch (Exception ex) when (ex is SecurityException or UnauthorizedAccessException or IOException)
        {
            return null;
        }
    }
}

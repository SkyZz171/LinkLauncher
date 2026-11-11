namespace LinkLauncher.Infrastructure.Windows;

using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

/// <summary>
/// Lit le code de sortie d'un processus à partir d'un handle ouvert de façon durable.
/// </summary>
/// <remarks>
/// Après la fermeture d'un processus, il n'est plus possible d'en rouvrir un handle par son
/// identifiant, et <see cref="System.Diagnostics.Process.ExitCode"/> refuse d'autre part de
/// répondre pour un processus que l'application n'a pas démarré elle-même. Le handle est donc
/// ouvert dès la première observation, en droits de consultation limités
/// (<c>PROCESS_QUERY_LIMITED_INFORMATION</c>), et seul le code de sortie est lu : aucune mémoire,
/// aucun module et aucun fil du processus ne sont examinés.
/// </remarks>
internal static class NativeProcess
{
    private const uint ProcessQueryLimitedInformation = 0x1000;
    private const uint StillActive = 259;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(
        uint desiredAccess,
        [MarshalAs(UnmanagedType.Bool)] bool inheritHandle,
        int processId);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetExitCodeProcess(SafeProcessHandle processHandle, out uint exitCode);

    /// <summary>Ouvre un handle restreint à la consultation, ou <see langword="null"/> si l'accès est refusé.</summary>
    public static SafeProcessHandle? TryOpen(int processId)
    {
        var handle = OpenProcess(ProcessQueryLimitedInformation, false, processId);

        return handle == IntPtr.Zero ? null : new SafeProcessHandle(handle, ownsHandle: true);
    }

    /// <summary>Code de sortie du processus, ou <see langword="null"/> s'il est encore actif ou indisponible.</summary>
    public static int? TryGetExitCode(SafeProcessHandle? handle)
    {
        if (handle is null || handle.IsInvalid || handle.IsClosed)
        {
            return null;
        }

        try
        {
            if (!GetExitCodeProcess(handle, out var exitCode) || exitCode == StillActive)
            {
                return null;
            }

            return unchecked((int)exitCode);
        }
        catch (ObjectDisposedException)
        {
            return null;
        }
    }
}

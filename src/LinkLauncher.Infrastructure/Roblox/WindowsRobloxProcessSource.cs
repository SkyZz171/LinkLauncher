namespace LinkLauncher.Infrastructure.Roblox;

using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Win32.SafeHandles;
using LinkLauncher.Core.Abstractions;
using LinkLauncher.Core.Models;
using LinkLauncher.Infrastructure.Windows;

/// <summary>
/// Lit les processus du client Roblox et conserve un handle restreint à la consultation afin de
/// pouvoir rapporter un code de sortie après leur fermeture.
/// </summary>
/// <remarks>
/// Seules l'identité, la date de démarrage, le chemin et le code de sortie sont lus.
/// Aucun module, aucune mémoire et aucun fil du client ne sont inspectés.
/// </remarks>
public sealed class WindowsRobloxProcessSource : IRobloxProcessSource, IDisposable
{
    /// <summary>Nom du processus officiel du client Roblox pour Windows.</summary>
    public const string DefaultProcessName = "RobloxPlayerBeta";

    private readonly Dictionary<int, TrackedProcess> _tracked = [];
    private readonly IAppLogger _logger;
    private readonly string _processName;

    private int _disposed;

    /// <summary>Crée la source de processus.</summary>
    /// <param name="logger">Journal de l'application.</param>
    /// <param name="processName">Nom du processus recherché ; par défaut, celui du client Roblox.</param>
    public WindowsRobloxProcessSource(IAppLogger logger, string processName = DefaultProcessName)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (string.IsNullOrWhiteSpace(processName))
        {
            throw new ArgumentException("Le nom du processus est obligatoire.", nameof(processName));
        }

        _processName = processName;
    }

    /// <inheritdoc />
    public IReadOnlyList<RobloxProcessInfo> GetRunningProcesses()
    {
        if (Volatile.Read(ref _disposed) != 0)
        {
            return [];
        }

        var processes = new List<RobloxProcessInfo>();

        foreach (var process in Process.GetProcessesByName(_processName))
        {
            try
            {
                // L'identifiant est lu avant toute libération : Process.Id reste valable
                // uniquement tant que l'objet Process n'a pas été libéré.
                var processId = process.Id;

                if (_tracked.TryGetValue(processId, out var tracked))
                {
                    process.Dispose();
                    processes.Add(CreateInfo(tracked.Process, processId));
                    continue;
                }

                _tracked[processId] = new TrackedProcess(process, OpenExitCodeHandle(processId));
                processes.Add(CreateInfo(process, processId));
            }
            catch (Exception ex) when (ex is InvalidOperationException or Win32Exception or NotSupportedException)
            {
                process.Dispose();
                _logger.Warn("Surveillance", $"Processus Roblox illisible ignoré : {ex.Message}");
            }
        }

        return processes;
    }

    /// <inheritdoc />
    public int? TryGetExitCode(int processId)
    {
        if (!_tracked.Remove(processId, out var tracked))
        {
            return null;
        }

        try
        {
            return NativeProcess.TryGetExitCode(tracked.ExitCodeHandle);
        }
        finally
        {
            tracked.ExitCodeHandle?.Dispose();
            tracked.Process.Dispose();
        }
    }

    /// <summary>Libère les handles conservés.</summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        foreach (var tracked in _tracked.Values)
        {
            tracked.ExitCodeHandle?.Dispose();
            tracked.Process.Dispose();
        }

        _tracked.Clear();
    }

    private SafeProcessHandle? OpenExitCodeHandle(int processId)
    {
        var handle = NativeProcess.TryOpen(processId);

        if (handle is null)
        {
            _logger.Warn("Surveillance", $"Handle de consultation refusé pour le PID {processId} : le code de sortie restera indisponible.");
        }

        return handle;
    }

    private static RobloxProcessInfo CreateInfo(Process process, int processId) => new()
    {
        ProcessId = processId,
        ProcessName = process.ProcessName,
        StartedAt = TryReadStartTime(process),
        ExecutablePath = TryReadExecutablePath(process)
    };

    private static DateTimeOffset? TryReadStartTime(Process process)
    {
        try
        {
            return new DateTimeOffset(process.StartTime);
        }
        catch (Exception ex) when (ex is InvalidOperationException or Win32Exception or NotSupportedException)
        {
            return null;
        }
    }

    private static string? TryReadExecutablePath(Process process)
    {
        try
        {
            return process.MainModule?.FileName;
        }
        catch (Exception ex) when (ex is InvalidOperationException or Win32Exception or NotSupportedException)
        {
            return null;
        }
    }

    /// <summary>Processus suivi et handle conservé pour lire son code de sortie.</summary>
    private sealed record TrackedProcess(Process Process, SafeProcessHandle? ExitCodeHandle);
}

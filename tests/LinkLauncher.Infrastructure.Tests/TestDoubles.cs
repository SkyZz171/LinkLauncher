using LinkLauncher.Core.Services;

namespace LinkLauncher.Infrastructure.Tests;

using LinkLauncher.Core.Abstractions;
using LinkLauncher.Core.Models;

internal sealed record LogRecord(AppLogLevel Level, string Category, string Message, Exception? Exception);

internal sealed class CapturingLogger : IAppLogger
{
    private readonly List<LogRecord> _entries = [];

    public IReadOnlyList<LogRecord> Entries
    {
        get
        {
            lock (_entries)
            {
                return _entries.ToList();
            }
        }
    }

    public void Log(AppLogLevel level, string category, string message, Exception? exception = null)
    {
        lock (_entries)
        {
            _entries.Add(new LogRecord(level, category, message, exception));
        }
    }
}

internal sealed class StubVersionReader : IExecutableVersionReader
{
    private readonly Dictionary<string, string?> _versions = new(StringComparer.OrdinalIgnoreCase);

    public StubVersionReader With(string path, string? version)
    {
        _versions[path] = version;
        return this;
    }

    public string? ReadFileVersion(string path) =>
        _versions.TryGetValue(path, out var version) ? version : null;
}

internal sealed class StubProtocolReader : IProtocolRegistrationReader
{
    public string? Command { get; set; }

    public string? TryGetCommand(string protocolScheme) => Command;
}

internal sealed class StubRobloxProbe : IRobloxProbe
{
    public RobloxInstallation Installation { get; set; } = new();

    public Task<RobloxInstallation> ProbeAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Installation);
}

internal sealed class FakeShellLauncher : IShellLauncher
{
    public List<string> Targets { get; } = [];

    public ShellLaunchResult Result { get; set; } = new(true);

    public ShellLaunchResult Launch(string target)
    {
        Targets.Add(target);
        return Result;
    }
}

internal sealed class FakeProcessSource : IRobloxProcessSource
{
    public List<RobloxProcessInfo> Running { get; } = [];

    public int? ExitCode { get; set; }

    public IReadOnlyList<RobloxProcessInfo> GetRunningProcesses() => Running;

    public int? TryGetExitCode(int processId) => ExitCode;
}

internal sealed class ThrowingSettingsStore : ISettingsStore
{
    public string SettingsFilePath => "settings.json";

    public Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default) =>
        throw new IOException("Lecture impossible.");

    public Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default) =>
        throw new IOException("Écriture impossible.");
}

internal sealed class StubSettingsService : ISettingsService
{
    public StubSettingsService(AppSettings? initial = null) =>
        Current = AppSettingsNormalizer.Normalize(initial ?? AppSettings.CreateDefault());

    public AppSettings Current { get; private set; }

    public bool IsLoaded => true;

    public event EventHandler<AppSettingsChangedEventArgs>? Changed;

    public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task<AppSettings> UpdateAsync(
        Func<AppSettings, AppSettings> mutation,
        CancellationToken cancellationToken = default)
    {
        var previous = Current;
        Current = AppSettingsNormalizer.Normalize(mutation(previous));
        Changed?.Invoke(this, new AppSettingsChangedEventArgs(previous, Current));

        return Task.FromResult(Current);
    }
}

internal sealed class StubVersionProvider : IApplicationVersionProvider
{
    public string Version { get; set; } = "0.0.0-test";
}

internal sealed class StubIntegrityChecker : IRobloxIntegrityChecker
{
    public RobloxIntegrityReport Report { get; set; } = new() { IsComplete = true };

    public Task<RobloxIntegrityReport> CheckAsync(
        RobloxInstallation installation,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Report);
}

internal sealed class FakeDiscordIpcTransport : IDiscordIpcTransport
{
    public bool IsConnected { get; private set; }

    public bool ConnectResult { get; set; } = true;

    public bool SendResult { get; set; } = true;

    public List<string> ConnectedApplicationIds { get; } = [];

    public List<string> SentPayloads { get; } = [];

    public int DisconnectCount { get; private set; }

    public Task<bool> ConnectAsync(string applicationId, CancellationToken cancellationToken = default)
    {
        ConnectedApplicationIds.Add(applicationId);
        IsConnected = ConnectResult;

        return Task.FromResult(ConnectResult);
    }

    public Task<bool> TrySendAsync(byte[] frame, CancellationToken cancellationToken = default)
    {
        SentPayloads.Add(System.Text.Encoding.UTF8.GetString(frame, 8, frame.Length - 8));

        return Task.FromResult(SendResult);
    }

    public Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        DisconnectCount++;
        IsConnected = false;

        return Task.CompletedTask;
    }

    public void Dispose() => IsConnected = false;
}

internal sealed class FakeActiveProfileProvider : IActiveProfileProvider
{
    public LaunchProfile? Profile { get; set; }

    public LaunchProfile? GetActiveProfile() => Profile;
}

internal sealed class ThrowingLibraryStore : ILibraryStore
{
    public string LibraryFilePath => "library.json";

    public Task<LauncherLibrary> LoadAsync(CancellationToken cancellationToken = default) =>
        throw new IOException("Lecture impossible.");

    public Task SaveAsync(LauncherLibrary library, CancellationToken cancellationToken = default) =>
        throw new IOException("Écriture impossible.");
}

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

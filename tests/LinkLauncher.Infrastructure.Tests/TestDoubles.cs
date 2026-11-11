namespace LinkLauncher.Infrastructure.Tests;

using LinkLauncher.Core.Abstractions;

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

internal sealed class ThrowingSettingsStore : ISettingsStore
{
    public string SettingsFilePath => "settings.json";

    public Task<Core.Models.AppSettings> LoadAsync(CancellationToken cancellationToken = default) =>
        throw new IOException("Lecture impossible.");

    public Task SaveAsync(Core.Models.AppSettings settings, CancellationToken cancellationToken = default) =>
        throw new IOException("Écriture impossible.");
}

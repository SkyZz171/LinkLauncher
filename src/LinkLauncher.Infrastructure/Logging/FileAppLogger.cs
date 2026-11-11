namespace LinkLauncher.Infrastructure.Logging;

using System.Globalization;
using System.Text;
using System.Threading.Channels;
using LinkLauncher.Core.Abstractions;

/// <summary>
/// Écrit les entrées de journal dans un fichier quotidien, sur un thread dédié,
/// afin de ne jamais bloquer le thread de l'interface.
/// </summary>
/// <remarks>
/// Chaque écriture ouvre puis referme le fichier : le journal reste lisible par un autre
/// outil pendant que LinkLauncher fonctionne, et aucune donnée n'est perdue à l'arrêt.
/// </remarks>
public sealed class FileAppLogger : IAppLogger, IDisposable
{
    private const string FilePrefix = "linklauncher-";
    private const string FileExtension = ".log";

    private static readonly Encoding Utf8WithoutBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    private readonly string _directory;
    private readonly Channel<LogEntry> _channel;
    private readonly Task _writerTask;
    private int _disposed;

    /// <summary>Crée le journal pour un répertoire donné.</summary>
    /// <param name="logsDirectory">Répertoire d'écriture des journaux.</param>
    public FileAppLogger(string logsDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(logsDirectory);

        _directory = logsDirectory;
        _channel = Channel.CreateUnbounded<LogEntry>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });
        _writerTask = Task.Run(ProcessQueueAsync);
    }

    /// <inheritdoc />
    public void Log(AppLogLevel level, string category, string message, Exception? exception = null)
    {
        if (Volatile.Read(ref _disposed) != 0)
        {
            return;
        }

        var entry = new LogEntry(
            DateTimeOffset.Now,
            level,
            category ?? string.Empty,
            message ?? string.Empty,
            exception?.ToString(),
            null);

        _channel.Writer.TryWrite(entry);
    }

    /// <summary>Attend que les entrées déjà remises au journal soient écrites sur disque.</summary>
    /// <param name="cancellationToken">Jeton d'annulation de l'attente.</param>
    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        if (Volatile.Read(ref _disposed) != 0)
        {
            return;
        }

        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var entry = new LogEntry(default, AppLogLevel.Debug, string.Empty, string.Empty, null, completion);

        if (!_channel.Writer.TryWrite(entry))
        {
            return;
        }

        using var registration = cancellationToken.Register(
            static state => ((TaskCompletionSource)state!).TrySetCanceled(),
            completion);

        await completion.Task.ConfigureAwait(false);
    }

    /// <summary>Arrête le journal et attend l'écriture des dernières entrées.</summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        _channel.Writer.TryComplete();

        try
        {
            _writerTask.Wait(TimeSpan.FromSeconds(2));
        }
        catch (AggregateException)
        {
            // L'écriture du journal ne doit jamais faire échouer la fermeture de l'application.
        }
    }

    private async Task ProcessQueueAsync()
    {
        while (await _channel.Reader.WaitToReadAsync().ConfigureAwait(false))
        {
            while (_channel.Reader.TryRead(out var entry))
            {
                try
                {
                    if (entry.Completion is null)
                    {
                        await AppendAsync(entry).ConfigureAwait(false);
                    }
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
                {
                    // Un journal indisponible ne doit pas interrompre l'application.
                }
                finally
                {
                    entry.Completion?.TrySetResult();
                }
            }
        }
    }

    private async Task AppendAsync(LogEntry entry)
    {
        Directory.CreateDirectory(_directory);

        var path = GetLogFilePath(DateOnly.FromDateTime(entry.Timestamp.LocalDateTime));
        var line = Format(entry) + Environment.NewLine;

        await File.AppendAllTextAsync(path, line, Utf8WithoutBom).ConfigureAwait(false);
    }

    private string GetLogFilePath(DateOnly day) =>
        Path.Combine(
            _directory,
            FilePrefix + day.ToString("yyyyMMdd", CultureInfo.InvariantCulture) + FileExtension);

    private static string Format(LogEntry entry)
    {
        var builder = new StringBuilder();

        builder.Append(entry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture));
        builder.Append(" | ");
        builder.Append(entry.Level.ToString().ToUpperInvariant().PadRight(11));
        builder.Append(" | ");
        builder.Append(entry.Category);
        builder.Append(" | ");
        builder.Append(entry.Message);

        if (!string.IsNullOrEmpty(entry.ExceptionText))
        {
            builder.AppendLine();
            builder.Append("    ");
            builder.Append(entry.ExceptionText.Replace("\n", "\n    ", StringComparison.Ordinal));
        }

        return builder.ToString();
    }

    private readonly record struct LogEntry(
        DateTimeOffset Timestamp,
        AppLogLevel Level,
        string Category,
        string Message,
        string? ExceptionText,
        TaskCompletionSource? Completion);
}

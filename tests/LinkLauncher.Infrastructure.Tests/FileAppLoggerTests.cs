namespace LinkLauncher.Infrastructure.Tests;

using LinkLauncher.Core.Abstractions;
using LinkLauncher.Infrastructure.Logging;
using Xunit;

public sealed class FileAppLoggerTests : IDisposable
{
    private readonly string _logsDirectory;

    public FileAppLoggerTests()
    {
        _logsDirectory = Path.Combine(Path.GetTempPath(), "linklauncher-tests", Guid.NewGuid().ToString("N"), "logs");
    }

    [Fact]
    public async Task Log_WritesEntryToDailyFile()
    {
        using var logger = new FileAppLogger(_logsDirectory);

        logger.Info("Test", "message d'essai");
        await logger.FlushAsync();

        var file = Assert.Single(Directory.GetFiles(_logsDirectory, "linklauncher-*.log"));
        var content = await File.ReadAllTextAsync(file);

        Assert.Contains("message d'essai", content, StringComparison.Ordinal);
        Assert.Contains("INFORMATION", content, StringComparison.Ordinal);
        Assert.Contains("Test", content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Log_WithException_IncludesExceptionMessage()
    {
        using var logger = new FileAppLogger(_logsDirectory);

        logger.Error("Test", "échec contrôlé", new InvalidOperationException("boum"));
        await logger.FlushAsync();

        var file = Assert.Single(Directory.GetFiles(_logsDirectory, "linklauncher-*.log"));
        var content = await File.ReadAllTextAsync(file);

        Assert.Contains("boum", content, StringComparison.Ordinal);
        Assert.Contains("échec contrôlé", content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Log_WritesMultipleEntriesInOrder()
    {
        using var logger = new FileAppLogger(_logsDirectory);

        logger.Info("Test", "premier");
        logger.Warn("Test", "deuxieme");
        await logger.FlushAsync();

        var file = Assert.Single(Directory.GetFiles(_logsDirectory, "linklauncher-*.log"));
        var content = await File.ReadAllTextAsync(file);

        Assert.True(content.IndexOf("premier", StringComparison.Ordinal) < content.IndexOf("deuxieme", StringComparison.Ordinal));
    }

    [Fact]
    public async Task FlushAsync_AfterDispose_DoesNotThrow()
    {
        var logger = new FileAppLogger(_logsDirectory);
        logger.Dispose();

        await logger.FlushAsync();
    }

    public void Dispose()
    {
        var root = Directory.GetParent(_logsDirectory)?.FullName;

        try
        {
            if (root is not null && Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
        catch (IOException)
        {
            // Un nettoyage incomplet ne doit pas faire échouer les tests.
        }
    }
}

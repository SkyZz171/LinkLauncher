namespace LinkLauncher.Infrastructure.Tests;

using System.Diagnostics;
using LinkLauncher.Core.Abstractions;
using LinkLauncher.Infrastructure.Roblox;
using Xunit;

public sealed class WindowsRobloxProcessSourceTests
{
    private readonly CapturingLogger _logger = new();

    [Fact]
    public void Constructor_WithoutProcessName_Throws()
    {
        Assert.Throws<ArgumentException>(() => new WindowsRobloxProcessSource(_logger, "  "));
    }

    [Fact]
    public async Task GetRunningProcesses_RepeatedCalls_KeepReportingTheSameProcess()
    {
        using var child = StartChildProcess();

        using var source = new WindowsRobloxProcessSource(_logger, "cmd");
        await Task.Delay(500);

        var first = source.GetRunningProcesses();
        var second = source.GetRunningProcesses();
        var third = source.GetRunningProcesses();

        Assert.Contains(first, process => process.ProcessId == child.Id);
        Assert.Contains(second, process => process.ProcessId == child.Id);
        Assert.Contains(third, process => process.ProcessId == child.Id);
        Assert.DoesNotContain(_logger.Entries, entry => entry.Level == AppLogLevel.Warning);
    }

    [Fact]
    public async Task TryGetExitCode_AfterProcessExit_ReturnsReportedCode()
    {
        var child = StartChildProcess();
        var processId = child.Id;

        using var source = new WindowsRobloxProcessSource(_logger, "cmd");
        await Task.Delay(500);

        source.GetRunningProcesses();

        child.Kill();
        child.WaitForExit(10_000);
        child.Dispose();

        await Task.Delay(300);

        var exitCode = source.TryGetExitCode(processId);
        var journal = string.Join(" | ", _logger.Entries.Select(entry => $"{entry.Level}: {entry.Message}"));
        Assert.True(exitCode.HasValue, $"Code de sortie indisponible. Journal : {journal}");
    }

    [Fact]
    public void TryGetExitCode_ForUnknownProcess_ReturnsNull()
    {
        using var source = new WindowsRobloxProcessSource(_logger, "cmd");

        Assert.Null(source.TryGetExitCode(123456));
    }

    private static Process StartChildProcess() =>
        Process.Start(new ProcessStartInfo("cmd.exe", "/c ping -n 20 127.0.0.1 > nul")
        {
            CreateNoWindow = true,
            UseShellExecute = false
        }) ?? throw new InvalidOperationException("Le processus de test n'a pas pu être démarré.");
}

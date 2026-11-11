namespace LinkLauncher.Integration.Tests;

using System.Net;
using System.Net.Http;
using LinkLauncher.Core.Abstractions;
using LinkLauncher.Core.Models;

internal sealed class CapturingLogger : IAppLogger
{
    public List<LogEntry> Entries { get; } = [];

    public void Info(string category, string message) =>
        Entries.Add(new LogEntry(AppLogLevel.Information, category, message));

    public void Warn(string category, string message) =>
        Entries.Add(new LogEntry(AppLogLevel.Warning, category, message));

    public void Error(string category, string message, Exception? exception = null) =>
        Entries.Add(new LogEntry(AppLogLevel.Error, category, message, exception));

    public void Log(AppLogLevel level, string category, string message, Exception? exception = null) =>
        Entries.Add(new LogEntry(level, category, message, exception));

    internal sealed record LogEntry(AppLogLevel Level, string Category, string Message, Exception? Exception = null);
}

internal sealed class StubVersionProvider : IApplicationVersionProvider
{
    public string Version { get; set; } = "0.1.0";
}

internal sealed class StubUpdateFeed : IUpdateFeed
{
    public IReadOnlyList<UpdateRelease> Releases { get; set; } = [];

    public Exception? Failure { get; set; }

    public Task<IReadOnlyList<UpdateRelease>> GetReleasesAsync(CancellationToken cancellationToken = default) =>
        Failure is null ? Task.FromResult(Releases) : Task.FromException<IReadOnlyList<UpdateRelease>>(Failure);
}

internal sealed class FakeHttpHandler : HttpMessageHandler
{
    public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;

    public byte[] Payload { get; set; } = [];

    public int Requests { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Requests++;
        var response = new HttpResponseMessage(StatusCode)
        {
            Content = new ByteArrayContent(Payload)
        };

        return Task.FromResult(response);
    }
}

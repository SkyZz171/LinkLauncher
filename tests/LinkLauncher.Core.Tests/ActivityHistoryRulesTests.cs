namespace LinkLauncher.Core.Tests;

using LinkLauncher.Core.Models;
using LinkLauncher.Core.Services;
using Xunit;

public sealed class ActivityHistoryRulesTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 11, 12, 0, 0, TimeSpan.FromHours(2));

    [Fact]
    public void StartSession_OpensSessionWithObservedStartTime()
    {
        var startedAt = Now.AddMinutes(-20);
        var history = ActivityHistoryRules.StartSession(
            ActivityHistoryRules.CreateDefault(),
            Process(4242, startedAt),
            experienceName: null,
            profileName: "Anglais",
            Now);

        var session = Assert.Single(history.Sessions);
        Assert.Equal(startedAt, session.StartedAt);
        Assert.Equal(4242, session.ProcessId);
        Assert.Equal("Anglais", session.ProfileName);
        Assert.True(session.IsOpen);
        Assert.Null(session.ObservedDuration);
    }

    [Fact]
    public void StartSession_ForSameProcessTwice_IsIgnored()
    {
        var history = ActivityHistoryRules.StartSession(
            ActivityHistoryRules.CreateDefault(), Process(7, Now.AddMinutes(-1)), null, null, Now);

        var again = ActivityHistoryRules.StartSession(history, Process(7, Now), null, null, Now);

        Assert.Same(history, again);
        Assert.Single(again.Sessions);
    }

    [Fact]
    public void EndSession_ClosesMatchingSessionWithExitCode()
    {
        var history = ActivityHistoryRules.StartSession(
            ActivityHistoryRules.CreateDefault(), Process(7, Now.AddMinutes(-30)), null, null, Now);

        var closed = ActivityHistoryRules.EndSession(history, Process(7, Now.AddMinutes(-30), exitCode: 0), Now);

        var session = Assert.Single(closed.Sessions);
        Assert.False(session.IsOpen);
        Assert.Equal(Now, session.EndedAt);
        Assert.Equal(0, session.ExitCode);
        Assert.Equal(TimeSpan.FromMinutes(30), session.ObservedDuration);
    }

    [Fact]
    public void EndSession_ForUnknownProcess_LeavesHistoryUnchanged()
    {
        var history = ActivityHistoryRules.StartSession(
            ActivityHistoryRules.CreateDefault(), Process(7, Now), null, null, Now);

        var closed = ActivityHistoryRules.EndSession(history, Process(99, Now), Now);

        Assert.Same(history, closed);
        Assert.True(Assert.Single(closed.Sessions).IsOpen);
    }

    [Fact]
    public void Normalize_MarksLeftoverOpenSessionsAsInterrupted()
    {
        var history = new ActivityHistory
        {
            Sessions = [new PlaySession { StartedAt = Now.AddHours(-3), ProcessId = 12 }]
        };

        var normalized = ActivityHistoryRules.Normalize(history, Now);

        var session = Assert.Single(normalized.Sessions);
        Assert.True(session.Interrupted);
        Assert.False(session.IsOpen);
        Assert.Null(session.ObservedDuration);
    }

    [Fact]
    public void Normalize_KeepsOnlyMostRecentSessions()
    {
        var sessions = Enumerable.Range(0, ActivityHistoryRules.MaximumSessions + 15)
            .Select(index => new PlaySession
            {
                StartedAt = Now.AddHours(-index - 1),
                EndedAt = Now.AddHours(-index - 1).AddMinutes(10),
                ProcessId = index
            })
            .ToList();

        var normalized = ActivityHistoryRules.Normalize(new ActivityHistory { Sessions = sessions }, Now);

        Assert.Equal(ActivityHistoryRules.MaximumSessions, normalized.Sessions.Count);
        Assert.Equal(sessions[0].StartedAt, normalized.Sessions[^1].StartedAt);
    }

    [Fact]
    public void Summarize_ComputesTodayWeekAndTotalWithoutInterruptedSessions()
    {
        var history = new ActivityHistory
        {
            Sessions =
            [
                new PlaySession
                {
                    StartedAt = Now.AddHours(-2),
                    EndedAt = Now.AddHours(-1),
                    ProcessId = 1,
                    ExitCode = 0
                },
                new PlaySession
                {
                    StartedAt = Now.AddDays(-3),
                    EndedAt = Now.AddDays(-3).AddMinutes(30),
                    ProcessId = 2,
                    ExitCode = 0
                },
                new PlaySession
                {
                    StartedAt = Now.AddDays(-30),
                    EndedAt = Now.AddDays(-30).AddMinutes(15),
                    ProcessId = 3,
                    ExitCode = 0
                },
                new PlaySession
                {
                    StartedAt = Now.AddHours(-5),
                    ProcessId = 4,
                    Interrupted = true
                }
            ]
        };

        var summary = ActivityHistoryRules.Summarize(history, Now);

        Assert.Equal(TimeSpan.FromHours(1), summary.Today);
        Assert.Equal(TimeSpan.FromMinutes(90), summary.LastSevenDays);
        Assert.Equal(TimeSpan.FromMinutes(105), summary.Total);
        Assert.Equal(4, summary.SessionCount);
        Assert.Equal(Now.AddHours(-1), summary.LastSessionAt);
    }

    [Fact]
    public void Summarize_ClipsSessionsCrossingMidnight()
    {
        var dayStart = new DateTimeOffset(Now.Date, Now.Offset);
        var history = new ActivityHistory
        {
            Sessions =
            [
                new PlaySession
                {
                    StartedAt = dayStart.AddHours(-1),
                    EndedAt = dayStart.AddMinutes(30),
                    ProcessId = 5
                }
            ]
        };

        var summary = ActivityHistoryRules.Summarize(history, Now);

        Assert.Equal(TimeSpan.FromMinutes(30), summary.Today);
        Assert.Equal(TimeSpan.FromMinutes(90), summary.Total);
    }

    [Theory]
    [InlineData(0, "0 min")]
    [InlineData(59, "moins d'une minute")]
    [InlineData(600, "10 min")]
    [InlineData(3600, "1 h")]
    [InlineData(3900, "1 h 05")]
    public void FormatDuration_ProducesReadableText(int seconds, string expected)
    {
        Assert.Equal(expected, ActivityHistoryRules.FormatDuration(TimeSpan.FromSeconds(seconds)));
    }

    private static RobloxProcessInfo Process(int processId, DateTimeOffset startedAt, int? exitCode = null) =>
        new() { ProcessId = processId, StartedAt = startedAt, ExitCode = exitCode };
}

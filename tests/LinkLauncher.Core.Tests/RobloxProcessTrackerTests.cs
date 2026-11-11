namespace LinkLauncher.Core.Tests;

using LinkLauncher.Core.Models;
using LinkLauncher.Core.Services;
using Xunit;

public sealed class RobloxProcessTrackerTests
{
    [Fact]
    public void Update_FirstObservation_ReportsStarted()
    {
        var tracker = new RobloxProcessTracker();

        var transitions = tracker.Update([Process(10)], _ => null);

        var transition = Assert.Single(transitions);
        Assert.Equal(RobloxProcessChange.Started, transition.Change);
        Assert.Equal(10, transition.Process.ProcessId);
        Assert.NotNull(tracker.Current);
    }

    [Fact]
    public void Update_SameProcessTwice_ReportsNothingTheSecondTime()
    {
        var tracker = new RobloxProcessTracker();
        tracker.Update([Process(10)], _ => null);

        Assert.Empty(tracker.Update([Process(10)], _ => null));
    }

    [Fact]
    public void Update_ProcessDisappeared_ReportsStoppedWithExitCode()
    {
        var tracker = new RobloxProcessTracker();
        tracker.Update([Process(10)], _ => null);

        var transitions = tracker.Update([], _ => 0);

        var transition = Assert.Single(transitions);
        Assert.Equal(RobloxProcessChange.Stopped, transition.Change);
        Assert.Equal(0, transition.Process.ExitCode);
        Assert.Null(tracker.Current);
    }

    [Fact]
    public void Update_WithoutExitCode_KeepsExitCodeNull()
    {
        var tracker = new RobloxProcessTracker();
        tracker.Update([Process(10)], _ => null);

        var transition = Assert.Single(tracker.Update([], _ => null));

        Assert.Null(transition.Process.ExitCode);
    }

    [Fact]
    public void Update_SeveralProcesses_CurrentIsMostRecentlyStarted()
    {
        var tracker = new RobloxProcessTracker();
        var older = DateTimeOffset.Now.AddMinutes(-5);
        var newer = DateTimeOffset.Now;

        tracker.Update([Process(1, older), Process(2, newer)], _ => null);

        Assert.NotNull(tracker.Current);
        Assert.Equal(2, tracker.Current.ProcessId);
    }

    [Fact]
    public void Reset_ForgetsKnownProcesses()
    {
        var tracker = new RobloxProcessTracker();
        tracker.Update([Process(10)], _ => null);

        tracker.Reset();

        Assert.Null(tracker.Current);
        Assert.Single(tracker.Update([Process(10)], _ => null));
    }

    [Fact]
    public void Update_WithNullArguments_Throws()
    {
        var tracker = new RobloxProcessTracker();

        Assert.Throws<ArgumentNullException>(() => tracker.Update(null!, _ => null));
        Assert.Throws<ArgumentNullException>(() => tracker.Update([], null!));
    }

    private static RobloxProcessInfo Process(int processId, DateTimeOffset? startedAt = null) =>
        new() { ProcessId = processId, StartedAt = startedAt };
}

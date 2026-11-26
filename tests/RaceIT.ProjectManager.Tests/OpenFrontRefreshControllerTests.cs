using System.Threading.Channels;
using RaceIT.ProjectManager.Domain;
using RaceIT.ProjectManager.Services;

namespace RaceIT.ProjectManager.Tests;

public sealed class OpenFrontRefreshControllerTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    [Fact]
    public async Task Visual_schedule_ticks_even_when_busy_or_read_fails_and_stops_on_disposal()
    {
        var clock = new ManualClock();
        var dispatches = Channel.CreateUnbounded<bool>();
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var ticks = 0;
        var reads = 0;
        var failures = 0;
        await using var refresh = new OpenFrontRefreshController(_ =>
        {
            reads++;
            throw new InvalidOperationException("Synthetic read failure.");
        }, _ => { }, Dispatch, _ => failures++, clock, () => ticks++);
        async Task Dispatch(Func<Task> action) { await action(); dispatches.Writer.TryWrite(true); }
        var local = refresh.TryRunAsync(async _ => await release.Task);
        refresh.Start();
        clock.Advance(TimeSpan.FromSeconds(29));
        Assert.Equal(0, ticks);
        clock.Advance(TimeSpan.FromSeconds(1));
        await dispatches.Reader.ReadAsync().AsTask().WaitAsync(Timeout);
        Assert.Equal(1, ticks);
        Assert.Equal(0, reads);

        release.TrySetResult();
        await local.WaitAsync(Timeout);
        clock.Advance(TimeSpan.FromSeconds(30));
        await dispatches.Reader.ReadAsync().AsTask().WaitAsync(Timeout);
        Assert.Equal(2, ticks);
        Assert.Equal(1, reads);
        Assert.Equal(1, failures);

        await refresh.DisposeAsync();
        await refresh.Completion.WaitAsync(Timeout);
        clock.Advance(TimeSpan.FromSeconds(30));
        Assert.Equal(2, ticks);
    }

    [Fact]
    public async Task One_schedule_reads_at_thirty_seconds_and_continues_without_restarting()
    {
        var clock = new ManualClock();
        var dispatches = Channel.CreateUnbounded<bool>();
        var reads = 0;
        IReadOnlyList<OpenFront> visible = [];
        await using var refresh = new OpenFrontRefreshController(
            _ => Task.FromResult<IReadOnlyList<OpenFront>>([new() { Id = 1, LastAdvance = $"Read {++reads}" }]),
            fronts => visible = fronts, Dispatch, exception => throw new Xunit.Sdk.XunitException(exception.ToString()), clock);
        async Task Dispatch(Func<Task> action) { await action(); dispatches.Writer.TryWrite(true); }

        refresh.Start();
        refresh.Start();
        Assert.Equal(1, clock.CreatedTimers);
        clock.Advance(TimeSpan.FromSeconds(29));
        Assert.Equal(0, reads);
        Assert.Empty(visible);

        clock.Advance(TimeSpan.FromSeconds(1));
        await dispatches.Reader.ReadAsync().AsTask().WaitAsync(Timeout);
        Assert.Equal("Read 1", Assert.Single(visible).LastAdvance);
        clock.Advance(TimeSpan.FromSeconds(30));
        await dispatches.Reader.ReadAsync().AsTask().WaitAsync(Timeout);
        Assert.Equal("Read 2", Assert.Single(visible).LastAdvance);
    }

    [Theory]
    [InlineData("state save")]
    [InlineData("explicit conflict reload")]
    public async Task Polling_skips_an_active_page_operation_and_resumes_after_it_finishes(string operation)
    {
        var clock = new ManualClock();
        var dispatches = Channel.CreateUnbounded<bool>();
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var reads = 0;
        var localResult = "not finished";
        await using var refresh = new OpenFrontRefreshController(
            _ => { Interlocked.Increment(ref reads); return Task.FromResult<IReadOnlyList<OpenFront>>([]); },
            _ => { }, Dispatch, _ => { }, clock);
        async Task Dispatch(Func<Task> action) { await action(); dispatches.Writer.TryWrite(true); }
        var local = refresh.TryRunAsync(async _ => { await release.Task; localResult = operation; });
        Assert.True(refresh.IsBusy);
        Assert.False(await refresh.TryRunAsync(_ => throw new InvalidOperationException("Reentry must be declined.")));
        refresh.Start();

        clock.Advance(TimeSpan.FromSeconds(30));
        await dispatches.Reader.ReadAsync().AsTask().WaitAsync(Timeout);
        Assert.Equal(0, reads);
        Assert.Equal("not finished", localResult);
        release.TrySetResult();
        Assert.True(await local.WaitAsync(Timeout));
        Assert.Equal(operation, localResult);

        clock.Advance(TimeSpan.FromSeconds(30));
        await dispatches.Reader.ReadAsync().AsTask().WaitAsync(Timeout);
        Assert.Equal(1, reads);
    }

    [Fact]
    public async Task Slow_refresh_never_overlaps_another_read_or_local_write()
    {
        var clock = new ManualClock();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var dispatches = Channel.CreateUnbounded<bool>();
        var reads = 0;
        var activeReads = 0;
        var maximumReads = 0;
        await using var refresh = new OpenFrontRefreshController(Read, _ => { }, Dispatch, _ => { }, clock);
        async Task<IReadOnlyList<OpenFront>> Read(CancellationToken _)
        {
            reads++;
            maximumReads = Math.Max(maximumReads, ++activeReads);
            if (reads == 1) { started.TrySetResult(); await release.Task; }
            activeReads--;
            return [];
        }
        async Task Dispatch(Func<Task> action) { await action(); dispatches.Writer.TryWrite(true); }
        refresh.Start();
        clock.Advance(TimeSpan.FromSeconds(30));
        await started.Task.WaitAsync(Timeout);
        clock.Advance(TimeSpan.FromSeconds(60));
        Assert.Equal(1, reads);
        Assert.False(await refresh.TryRunAsync(_ => throw new InvalidOperationException("Refresh owns the gate.")));
        release.TrySetResult();
        await dispatches.Reader.ReadAsync().AsTask().WaitAsync(Timeout);
        await dispatches.Reader.ReadAsync().AsTask().WaitAsync(Timeout);
        Assert.Equal(1, maximumReads);
        Assert.Equal(2, reads); // PeriodicTimer coalesces missed ticks; it never overlaps or queues every missed read.
    }

    [Fact]
    public async Task Failed_refresh_keeps_the_last_successful_snapshot_and_a_later_tick_recovers()
    {
        var clock = new ManualClock();
        var dispatches = Channel.CreateUnbounded<bool>();
        var reads = 0;
        var failures = 0;
        var applied = 0;
        IReadOnlyList<OpenFront> visible = [];
        await using var refresh = new OpenFrontRefreshController(_ =>
        {
            reads++;
            if (reads == 2) throw new InvalidOperationException("Synthetic read failure.");
            return Task.FromResult<IReadOnlyList<OpenFront>>([new() { Id = 1, Revision = reads, LastAdvance = $"Read {reads}" }]);
        }, fronts => { visible = fronts; applied++; }, Dispatch, _ => failures++, clock);
        async Task Dispatch(Func<Task> action) { await action(); dispatches.Writer.TryWrite(true); }
        refresh.Start();
        clock.Advance(TimeSpan.FromSeconds(30));
        await dispatches.Reader.ReadAsync().AsTask().WaitAsync(Timeout);
        var lastGood = Assert.Single(visible);
        clock.Advance(TimeSpan.FromSeconds(30));
        await dispatches.Reader.ReadAsync().AsTask().WaitAsync(Timeout);
        Assert.Same(lastGood, Assert.Single(visible));
        Assert.Equal(1, applied);
        Assert.Equal(1, failures);
        clock.Advance(TimeSpan.FromSeconds(30));
        await dispatches.Reader.ReadAsync().AsTask().WaitAsync(Timeout);
        Assert.Equal("Read 3", Assert.Single(visible).LastAdvance);
        Assert.Equal(2, applied);
    }

    [Fact]
    public async Task Disposal_does_not_wait_on_inflight_read_and_its_late_result_cannot_apply()
    {
        var clock = new ManualClock();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<IReadOnlyList<OpenFront>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var reads = 0;
        var applied = 0;
        var refresh = new OpenFrontRefreshController(_ =>
        {
            reads++;
            started.TrySetResult();
            return release.Task;
        }, _ => applied++, action => action(), _ => { }, clock);
        refresh.Start();
        clock.Advance(TimeSpan.FromSeconds(30));
        await started.Task.WaitAsync(Timeout);
        await refresh.DisposeAsync().AsTask().WaitAsync(Timeout);
        Assert.True(refresh.IsDisposed);
        Assert.Equal(0, clock.ActiveTimers);
        release.TrySetResult([new() { Id = 1, LastAdvance = "Too late" }]);
        await refresh.Completion.WaitAsync(Timeout);
        clock.Advance(TimeSpan.FromMinutes(1));
        Assert.Equal(1, reads);
        Assert.Equal(0, applied);
        Assert.False(await refresh.TryRunAsync(_ => throw new InvalidOperationException("Disposed page must stay stopped.")));
        refresh.Start();
        Assert.Equal(1, clock.CreatedTimers);
    }

    [Fact]
    public async Task A_new_page_gets_one_new_schedule_after_the_old_page_is_disposed()
    {
        var clock = new ManualClock();
        var oldReads = 0;
        var newReads = 0;
        var dispatches = Channel.CreateUnbounded<bool>();
        var oldPage = new OpenFrontRefreshController(_ => { oldReads++; return Task.FromResult<IReadOnlyList<OpenFront>>([]); },
            _ => { }, action => action(), _ => { }, clock);
        oldPage.Start();
        await oldPage.DisposeAsync();
        await oldPage.Completion.WaitAsync(Timeout);
        await using var newPage = new OpenFrontRefreshController(_ => { newReads++; return Task.FromResult<IReadOnlyList<OpenFront>>([]); },
            _ => { }, Dispatch, _ => { }, clock);
        async Task Dispatch(Func<Task> action) { await action(); dispatches.Writer.TryWrite(true); }
        newPage.Start();
        newPage.Start();
        Assert.Equal(2, clock.CreatedTimers);
        Assert.Equal(1, clock.ActiveTimers);
        clock.Advance(TimeSpan.FromSeconds(30));
        await dispatches.Reader.ReadAsync().AsTask().WaitAsync(Timeout);
        Assert.Equal(0, oldReads);
        Assert.Equal(1, newReads);
    }

    private sealed class ManualClock : TimeProvider
    {
        private readonly List<ManualTimer> timers = [];
        private TimeSpan elapsed;
        public int CreatedTimers => timers.Count;
        public int ActiveTimers => timers.Count(timer => !timer.Disposed);
        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
        public override long GetTimestamp() => elapsed.Ticks;
        public override DateTimeOffset GetUtcNow() => DateTimeOffset.UnixEpoch + elapsed;
        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            var timer = new ManualTimer(this, callback, state, dueTime, period);
            timers.Add(timer);
            return timer;
        }
        public void Advance(TimeSpan duration)
        {
            elapsed += duration;
            foreach (var timer in timers.ToArray()) timer.FireDue();
        }
        private sealed class ManualTimer(ManualClock clock, TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period) : ITimer
        {
            private TimeSpan next = clock.elapsed + dueTime;
            private TimeSpan interval = period;
            public bool Disposed { get; private set; }
            public bool Change(TimeSpan due, TimeSpan repeated)
            {
                if (Disposed) return false;
                next = due == global::System.Threading.Timeout.InfiniteTimeSpan ? TimeSpan.MaxValue : clock.elapsed + due;
                interval = repeated;
                return true;
            }
            public void FireDue()
            {
                while (!Disposed && next <= clock.elapsed)
                {
                    next = interval <= TimeSpan.Zero ? TimeSpan.MaxValue : next + interval;
                    callback(state);
                }
            }
            public void Dispose() => Disposed = true;
            public ValueTask DisposeAsync() { Dispose(); return ValueTask.CompletedTask; }
        }
    }
}

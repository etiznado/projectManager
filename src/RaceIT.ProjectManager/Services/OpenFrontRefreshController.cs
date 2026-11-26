using RaceIT.ProjectManager.Domain;

namespace RaceIT.ProjectManager.Services;

/// <summary>One page's read-only refresh schedule and its shared local-operation gate.</summary>
public sealed class OpenFrontRefreshController : IAsyncDisposable
{
    public static readonly TimeSpan Interval = TimeSpan.FromSeconds(30);
    private readonly object lifecycle = new();
    private readonly CancellationTokenSource cancellation = new();
    private readonly CancellationToken lifetime;
    private readonly Func<CancellationToken, Task<IReadOnlyList<OpenFront>>> read;
    private readonly Action<IReadOnlyList<OpenFront>> apply;
    private readonly Func<Func<Task>, Task> dispatch;
    private readonly Action<Exception> reportFailure;
    private readonly TimeProvider timeProvider;
    private readonly Action? scheduleTick;
    private PeriodicTimer? timer;
    private Task? loop;
    private int busy;
    private int disposed;

    public OpenFrontRefreshController(Func<CancellationToken, Task<IReadOnlyList<OpenFront>>> read,
        Action<IReadOnlyList<OpenFront>> apply, Func<Func<Task>, Task> dispatch,
        Action<Exception> reportFailure, TimeProvider? timeProvider = null, Action? scheduleTick = null)
    {
        this.read = read;
        this.apply = apply;
        this.dispatch = dispatch;
        this.reportFailure = reportFailure;
        this.timeProvider = timeProvider ?? TimeProvider.System;
        this.scheduleTick = scheduleTick;
        lifetime = cancellation.Token;
    }

    public bool IsDisposed => Volatile.Read(ref disposed) != 0;
    public bool IsBusy => Volatile.Read(ref busy) != 0;
    public Task Completion { get { lock (lifecycle) return loop ?? Task.CompletedTask; } }

    public void Start()
    {
        lock (lifecycle)
        {
            if (IsDisposed || loop is not null) return;
            timer = new PeriodicTimer(Interval, timeProvider);
            loop = RunLoopAsync(timer, lifetime);
        }
    }

    /// <summary>Declines reentry instead of queuing work against a possibly replaced card.</summary>
    public async Task<bool> TryRunAsync(Func<CancellationToken, Task> operation)
    {
        if (IsDisposed || Interlocked.CompareExchange(ref busy, 1, 0) != 0) return false;
        try
        {
            if (IsDisposed) return false;
            await operation(lifetime);
            return !IsDisposed;
        }
        catch (OperationCanceledException) when (IsDisposed) { return false; }
        finally { Volatile.Write(ref busy, 0); }
    }

    private async Task RunLoopAsync(PeriodicTimer pageTimer, CancellationToken token)
    {
        try
        {
            while (await pageTimer.WaitForNextTickAsync(token).ConfigureAwait(false))
            {
                if (IsDisposed) break;
                await dispatch(async () =>
                {
                    if (IsDisposed) return;
                    scheduleTick?.Invoke();
                    await RefreshAsync();
                }).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (ObjectDisposedException) when (IsDisposed) { }
        catch (Exception exception) { if (!IsDisposed) reportFailure(exception); }
        finally { pageTimer.Dispose(); }
    }

    private async Task RefreshAsync()
    {
        await TryRunAsync(async token =>
        {
            try
            {
                var fronts = await read(token);
                if (!token.IsCancellationRequested && !IsDisposed) apply(fronts);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { }
            catch (Exception exception) { if (!IsDisposed) reportFailure(exception); }
        });
    }

    public ValueTask DisposeAsync()
    {
        PeriodicTimer? pageTimer;
        lock (lifecycle)
        {
            if (IsDisposed) return ValueTask.CompletedTask;
            Volatile.Write(ref disposed, 1);
            pageTimer = timer;
        }
        cancellation.Cancel();
        pageTimer?.Dispose();
        cancellation.Dispose();
        // Do not await a dispatcher operation from the disposing component's dispatcher.
        return ValueTask.CompletedTask;
    }
}

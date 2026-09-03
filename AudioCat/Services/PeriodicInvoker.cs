using AudioCat.Models;

namespace AudioCat.Services;

internal sealed class PeriodicInvoker(Func<Task> callback, TimeSpan interval) : IDisposable, IAsyncDisposable
{
    private bool IsDisposed { get; set; }
    private Task EventInvokerTask { get; set; } = Task.CompletedTask;
    private CancellationTokenSource Cts { get; } = new();
    private AsyncLocal<bool> IsInEventInvokerLoop { get; } = new();

    public void Start()
    {
        if (IsDisposed)
            throw new ObjectDisposedException(nameof(PeriodicInvoker));
        // Task.Run keeps the first callback off the caller's thread; a synchronous first
        // invocation re-enters event handlers while the caller may still hold its locks.
        EventInvokerTask = Task.Run(() => EventInvokerLoop(Cts.Token));
    }

    private async Task EventInvokerLoop(CancellationToken ctx)
    {
        IsInEventInvokerLoop.Value = true; // Flows into the callback; lets Dispose detect it was called from inside this very loop
        do
        {
            try { await callback.Invoke().ConfigureAwait(false); }
            catch { /* ignore */ }
            await Task.Delay(interval, ctx).ConfigureAwait(false);
        } while (!ctx.IsCancellationRequested);
    }

    public void Dispose()
    {
        if (IsDisposed)
            return;
        IsDisposed = true;
        Cts.Cancel();
        if (IsInEventInvokerLoop.Value)
        {
            // Called from inside our own callback; waiting on EventInvokerTask here is a self-join deadlock.
            // The loop exits right after the callback returns; dispose Cts once it does.
            _ = EventInvokerTask.ContinueWith(_ => Cts.Dispose(), TaskContinuationOptions.ExecuteSynchronously);
            return;
        }
        try { EventInvokerTask.Wait(); }
        catch { /* ignore */ }
        try { EventInvokerTask.Dispose(); }
        catch { /* ignore */ }
        try { Cts.Dispose(); }
        catch { /* ignore */ }
    }

    public async ValueTask DisposeAsync()
    {
        if (IsDisposed)
            return;
        IsDisposed = true;
        await Cts.CancelAsync();
        if (IsInEventInvokerLoop.Value)
        {
            // Same self-join hazard as Dispose: awaiting the loop from inside its callback never completes.
            _ = EventInvokerTask.ContinueWith(_ => Cts.Dispose(), TaskContinuationOptions.ExecuteSynchronously);
            return;
        }
        try { await EventInvokerTask.WaitAsync(CancellationToken.None); }
        catch { /* ignore */ }
        try { EventInvokerTask.Dispose(); }
        catch { /* ignore */ }
        try { Cts.Dispose(); }
        catch { /* ignore */ }
    }
}
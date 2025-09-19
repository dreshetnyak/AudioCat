using AudioCat.Models;

namespace AudioCat.Services;

internal sealed class PeriodicInvoker(Func<Task> callback, TimeSpan interval) : IDisposable, IAsyncDisposable
{
    private bool IsDisposed { get; set; }
    private Task EventInvokerTask { get; set; } = Task.CompletedTask;
    private CancellationTokenSource Cts { get; } = new();

    public void Start()
    {
        if (IsDisposed)
            throw new ObjectDisposedException(nameof(PeriodicInvoker));
        EventInvokerTask = EventInvokerLoop(Cts.Token);
    }

    private async Task EventInvokerLoop(CancellationToken ctx)
    {
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
        try { await EventInvokerTask.WaitAsync(CancellationToken.None); }
        catch { /* ignore */ }
        try { EventInvokerTask.Dispose(); }
        catch { /* ignore */ }
        try { Cts.Dispose(); }
        catch { /* ignore */ }
    }
}
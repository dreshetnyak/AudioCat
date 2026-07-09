using System.Diagnostics;
using System.IO;
using System.Text;

namespace AudioCat.Services;

internal static class Process
{
    public enum OutputType { Standard, Error }

    public static async Task Run(string executable, string arguments, Func<string, Task> onOutput, OutputType outputType, CancellationToken ctx)
    {
        using System.Diagnostics.Process process = CreateProcess(executable, arguments);
        process.Start();
        var outTask = ReadOutStream(process, onOutput, outputType, ctx);
        // Both streams are redirected, so the non-selected one must be drained too, otherwise the child stalls once that pipe buffer fills
        var drainTask = ReadOutStream(process, static _ => Task.CompletedTask, outputType == OutputType.Error ? OutputType.Standard : OutputType.Error, ctx);
        try { await process.WaitForExitAsync(ctx); }
        catch (OperationCanceledException)
        {
            await KillAndWaitForExit(process);
            try { await Task.WhenAll(outTask, drainTask); }
            catch { /* ignore */ }
            throw;
        }
        await outTask;
        await drainTask;
    }

    public static async Task<string> Run(string executable, string arguments, OutputType outputType, CancellationToken ctx)
    {
        using var process = CreateProcess(executable, arguments);
        process.Start();

        var outErrorTask = ReadOutputStream(process, OutputType.Error, ctx);
        var outStandardTask = ReadOutputStream(process, OutputType.Standard, ctx);

        try { await process.WaitForExitAsync(ctx); }
        catch (OperationCanceledException)
        {
            await KillAndWaitForExit(process);
            try { await Task.WhenAll(outErrorTask, outStandardTask); }
            catch { /* ignore */ }
            throw;
        }
        var errorOutput = await outErrorTask;
        var standardOutput = await outStandardTask;

        return outputType == OutputType.Standard
            ? standardOutput
            : errorOutput;
    }

    // The child process must be fully terminated before the cancellation propagates to the caller,
    // otherwise it keeps write locks on its output files and they can't be cleaned up.
    private static async Task KillAndWaitForExit(System.Diagnostics.Process process)
    {
        try { process.Kill(entireProcessTree: true); }
        catch { /* the process has already exited */ }
        try { await process.WaitForExitAsync(CancellationToken.None); }
        catch { /* ignore */ }
    }

    private static async Task ReadOutStream(System.Diagnostics.Process process, Func<string, Task> onOutput, OutputType outputType, CancellationToken ctx)
    {
        // ReSharper disable AccessToDisposedClosure
        TextReader textReader = outputType == OutputType.Error
            ? process.StandardError
            : process.StandardOutput;
        // ReSharper restore AccessToDisposedClosure
        while (!ctx.IsCancellationRequested)
        {
            try
            {
                var line = await textReader.ReadLineAsync(ctx);
                if (line == "")
                    continue;
                if (line == null)
                    break;
                try { await onOutput(line); }
                catch { /* ignore */ }
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        ctx.ThrowIfCancellationRequested();
    }

    private static async Task<string> ReadOutputStream(System.Diagnostics.Process process, OutputType outputType, CancellationToken ctx)
    {
        var responseBuilder = new StringBuilder(1024);
        TextReader textReader = outputType == OutputType.Error 
            ? process.StandardError 
            : process.StandardOutput;
        
        while (!ctx.IsCancellationRequested)
        {
            try
            {
                var line = await textReader.ReadLineAsync(ctx);
                if (line == "")
                    continue;
                if (line == null)
                    break;
                responseBuilder.AppendLine(line);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        ctx.ThrowIfCancellationRequested();
        return responseBuilder.ToString();
    }

    private static System.Diagnostics.Process CreateProcess(string executable, string arguments)
    {
        return new System.Diagnostics.Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = executable, 
                Arguments = arguments,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardInput = false,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            }
        };
    }
}
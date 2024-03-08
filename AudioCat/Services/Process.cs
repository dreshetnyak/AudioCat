using System.Diagnostics;
using System.IO;
using System.Text;

namespace AudioCat.Services;

internal static class Process
{
    public enum OutputType { Standard, Error }

    public static async Task Run(string executable, string arguments, Action<string> onOutput, OutputType outputType, CancellationToken ctx)
    {
        using var process = CreateProcess(executable, arguments);
        process.Start();
        var outTask = ReadOutputStream();
        await process.WaitForExitAsync(ctx);
        await outTask;

        return;

        async Task ReadOutputStream()
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
                    onOutput(line);
                }
                catch
                {
                    break;
                }
            }

            ctx.ThrowIfCancellationRequested();
        }
    }

    public static async Task<string> Run(string executable, string arguments, OutputType outputType, CancellationToken ctx)
    {
        using var process = CreateProcess(executable, arguments);
        process.Start();
        var responseBuilder = new StringBuilder(1024);
        var outTask = ReadOutputStream();
        await process.WaitForExitAsync(ctx);
        await outTask;

        return responseBuilder.ToString();

        async Task ReadOutputStream()
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
                    responseBuilder.AppendLine(line);
                }
                catch
                {
                    break;
                }
            }

            ctx.ThrowIfCancellationRequested();
        }
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
                RedirectStandardError = true,
                Verb = "runas"
            }
        };
    }
}
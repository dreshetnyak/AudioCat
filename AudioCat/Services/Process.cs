using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using static AudioCat.Services.Process;

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
        
        var outErrorTask = ReadOutputStream(process, OutputType.Error, ctx);
        var outStandardTask = ReadOutputStream(process, OutputType.Standard, ctx);

        await process.WaitForExitAsync(ctx);
        var errorOutput = await outErrorTask;
        var standardOutput = await outStandardTask;

        return outputType == OutputType.Standard 
            ? standardOutput 
            : errorOutput;
    }

    private static async Task<string> ReadOutputStream(System.Diagnostics.Process process, OutputType outputType, CancellationToken ctx)
    {
        var responseBuilder = new StringBuilder(1024);
        TextReader textReader = outputType == OutputType.Error ? process.StandardError : process.StandardOutput;
        
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
                RedirectStandardInput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
                Verb = "runas"
            }
        };
    }
}
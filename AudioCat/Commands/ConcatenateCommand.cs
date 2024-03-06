using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;

namespace AudioCat.Commands
{
    internal sealed class MessageEventArgs(string message) : EventArgs
    {
        public string Message { get; } = message;
    }
    internal delegate void MessageEventHandler(object sender, MessageEventArgs eventArgs);

    internal sealed class ConcatenateCommand(ObservableCollection<AudioFile> audioFiles) : CommandBase
    {
        private ObservableCollection<AudioFile> AudioFiles { get; } = audioFiles;
        private CancellationTokenSource? Cts { get; set; }

        public event MessageEventHandler? Output;

        protected override async Task Command(object? parameter)
        {
            try
            {
                Cts = new CancellationTokenSource();

                if (AudioFiles.Count == 0)
                {
                    OnOutput("No files to concatenate");
                    return;
                }

                var outputFileName = FileSystemSelect.FileToSave("MP3 Audio|*.mp3", GetSuggestedFileName());
                if (outputFileName == "")
                {
                    OnOutput("");
                    return;
                }
                
                var listFile = await Task.Run(CreateFilesList);
                var process = CreateProcess(listFile, outputFileName);

                await ConcatenateFiles(process, Cts.Token);
            }
            finally
            {
                var cts = Cts;
                Cts = null;
                try { cts?.Dispose(); }
                catch { /* ignore */ }
            }
        }

        public void Cancel()
        {
            try { Cts?.Cancel(); }
            catch { /* ignore */ }
        }

        private string GetSuggestedFileName()
        {
            var firstFile = AudioFiles.First().Path;

            var fileInfo = new FileInfo(firstFile);
            var suggestedName = fileInfo.Directory?.Name ?? "";
            if (suggestedName != "")
            {
                return suggestedName + ".mp3";
            }
            
            suggestedName = fileInfo.Name;
            if (suggestedName == "")
                return ".mp3";
            var withoutExtension = Path.GetFileNameWithoutExtension(suggestedName);

            return withoutExtension + ".Cat.mp3";
        }

        private string CreateFilesList()
        {
            var listFile = Path.GetTempFileName();
            var sb = new StringBuilder();
            foreach (var audioFile in AudioFiles) 
                sb.AppendLine($"file \'{audioFile.Path}\'");
            File.WriteAllText(listFile, sb.ToString());
            return listFile;
        }

        private static Process CreateProcess(string listFile, string outputFileName)
        {
            return new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg.exe",
                    Arguments = $"-y -loglevel quiet -stats -stats_period 0.1 -f concat -safe 0 -i \"{listFile}\" -c copy \"{outputFileName}\"",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    Verb = "runas"
                }
            };
        }

        private async Task ConcatenateFiles(Process process, CancellationToken cancellationToken)
        {
            process.Start();
            var errorOutTask = ReadOutputStream(process.StandardError, cancellationToken);
            await process.WaitForExitAsync(cancellationToken);
            await errorOutTask;
        }

        private async Task ReadOutputStream(TextReader textReader, CancellationToken ctx)
        {
            while (!ctx.IsCancellationRequested)
            {
                try
                {
                    var line = await textReader.ReadLineAsync(ctx);
                    if (line == "")
                        continue;
                    if (line == null)
                        break;
                    OnOutput(line);
                }
                catch
                {
                    break;
                }
            }

            ctx.ThrowIfCancellationRequested();
        }

        private void OnOutput(string message) => Output?.Invoke(this, new MessageEventArgs(message));
    }
}

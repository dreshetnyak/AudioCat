using System.Runtime.CompilerServices;
using AudioCat.Models;
using AudioCat.ViewModels;

namespace AudioCat.Commands;

public interface ISilenceScanArgs
{
    IReadOnlyList<IMediaFileViewModel> Files { get; }
    int SilenceDuration { get; }
    int SilenceThreshold { get; }
}

public sealed class ScanForSilenceCommand(IMediaFileToolkitService mediaFileToolkitService) : CommandBase
{
    public IMediaFileToolkitService MediaFileToolkitService { get; } = mediaFileToolkitService;

    private object CancelSync { get; } = new();
    private CancellationTokenSource? Cts { get; set; }

    protected override async Task<IResponse<object>> Command(object? parameter)
    {
        try
        {
            lock (CancelSync)
                Cts = new CancellationTokenSource();

            if (parameter is not ISilenceScanArgs args)
                return Response<object>.Failure("ScanForSilenceCommand invalid parameter");

            var startTime = TimeSpan.Zero;
            var intervals = new List<IInterval>();
            foreach (var file in args.Files)
            {
                if (file.IsImage || file.Duration == null)
                    continue;

                var intervalsResponse = await MediaFileToolkitService.ScanForSilence(file.FilePath, args.SilenceDuration, args.SilenceThreshold, Cts.Token);
                if (intervalsResponse.IsFailure)
                    return intervalsResponse.Message is nameof(OperationCanceledException) or nameof(TaskCanceledException)
                        ? Response<object>.Success()
                        : Response<object>.Failure(intervalsResponse.Message);

                var fileIntervals = intervalsResponse.Data!;
                AddFileIntervals(intervals, fileIntervals, startTime);
                var fileDuration = file.Duration!.Value;
                intervals.Add(new Interval(file.FilePath, fileDuration, fileDuration));
                startTime += fileDuration;
            }

            return Response<object>.Success(intervals);
        }
        catch (Exception ex)
        {
            return Response<object>.Failure(ex.Message);
        }
        finally
        {
            lock (CancelSync)
            {
                try { Cts?.Dispose(); }
                catch { /* ignore */ }
                Cts = null;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void AddFileIntervals(List<IInterval> intervals, IReadOnlyList<IInterval> fileIntervals, TimeSpan startTime)
    {
        foreach (var fileInterval in fileIntervals)
            intervals.Add(new Interval(fileInterval.FileFullName, startTime + fileInterval.Start, startTime + fileInterval.End));
    }

    public void Cancel()
    {
        lock (CancelSync)
            Cts?.Cancel();
    }
}
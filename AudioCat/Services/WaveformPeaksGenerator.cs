using AudioCat.ViewModels;
using NAudio.Wave;

namespace AudioCat.Services;

/// <summary>
/// Generates waveform peaks for the chapters wizard strip control.
/// Decodes audio in-process via NAudio and emits peaks in timeline order,
/// aligned to the ffprobe-reported durations the application already stores.
/// UI-agnostic: <c>deliverBatch</c> is invoked on a worker thread; marshaling
/// to the UI thread is the caller's responsibility. No caching of any kind —
/// peaks are regenerated on every invocation.
/// </summary>
internal static class WaveformPeaksGenerator
{
    /// <summary>Waveform resolution: each peak covers 1/10 of a second of audio.</summary>
    private const int PEAKS_PER_SECOND = 10;
    /// <summary>Peaks are scaled to the 0..10000 range expected by the strip control.</summary>
    private const float PEAK_SCALE = 10000f;
    /// <summary>Peaks are delivered in batches of 100 (10 seconds of audio), plus a final partial batch per file.</summary>
    private const int PEAKS_PER_BATCH = 100;
    /// <summary>Number of float samples requested per decode read.</summary>
    private const int READ_BUFFER_SIZE = 16384;

    /// <summary>
    /// Generates waveform peaks for <paramref name="files"/> in timeline order, skipping image
    /// and unknown-duration files. For each file exactly <c>round(Duration.TotalSeconds × 10)</c>
    /// peaks are emitted — extra decoded buckets are truncated, missing ones are padded with 0 —
    /// so the global strip index mapping <c>index = seconds × 10</c> stays aligned with the
    /// playback engine's ffprobe-duration offset math. The decode runs on a background thread.
    /// Cancellation is honored between read blocks and completes the returned task normally
    /// (no <see cref="OperationCanceledException"/> reaches the caller). If a file fails to
    /// decode, generation stops silently, leaving whatever was already delivered.
    /// </summary>
    public static async Task Generate(IReadOnlyList<IMediaFileViewModel> files, Action<IReadOnlyList<float>> deliverBatch, CancellationToken token)
    {
        try
        {
            await Task.Run(() => GeneratePeaks(files, deliverBatch, token), CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            // Cancellation is a normal outcome; the strip simply stays partial.
        }
    }

    private static void GeneratePeaks(IReadOnlyList<IMediaFileViewModel> files, Action<IReadOnlyList<float>> deliverBatch, CancellationToken token)
    {
        foreach (var file in files)
        {
            token.ThrowIfCancellationRequested();
            if (file.IsImage || file.Duration is not { } duration)
                continue;
            var peaksCount = (int)Math.Round(duration.TotalSeconds * PEAKS_PER_SECOND);
            if (peaksCount <= 0)
                continue;
            if (!TryGenerateFilePeaks(file.FilePath, peaksCount, deliverBatch, token))
                return; // A file failed to decode: stop silently, keep whatever was delivered so far
        }
    }

    /// <returns><c>true</c> if the file was fully processed; <c>false</c> on a decode failure.</returns>
    private static bool TryGenerateFilePeaks(string filePath, int peaksCount, Action<IReadOnlyList<float>> deliverBatch, CancellationToken token)
    {
        var batch = new List<float>(PEAKS_PER_BATCH);
        var emittedCount = 0;

        void Emit(float peak)
        {
            batch.Add(peak);
            emittedCount++;
            if (batch.Count < PEAKS_PER_BATCH)
                return;
            deliverBatch(batch);
            batch = new List<float>(PEAKS_PER_BATCH);
        }

        try
        {
            using var reader = new AudioFileReader(filePath);
            var bucketSize = reader.WaveFormat.SampleRate * reader.WaveFormat.Channels / PEAKS_PER_SECOND;
            if (bucketSize < 1)
                bucketSize = 1;
            var buffer = new float[READ_BUFFER_SIZE];
            var samplesInBucket = 0;
            var bucketMax = 0f;

            while (emittedCount < peaksCount)
            {
                token.ThrowIfCancellationRequested();
                var samplesRead = reader.Read(buffer, 0, buffer.Length);
                if (samplesRead == 0)
                    break; // End of stream
                for (var i = 0; i < samplesRead && emittedCount < peaksCount; i++)
                {
                    var magnitude = Math.Abs(buffer[i]);
                    if (magnitude > bucketMax)
                        bucketMax = magnitude;
                    if (++samplesInBucket < bucketSize)
                        continue;
                    Emit(ToPeak(bucketMax));
                    samplesInBucket = 0;
                    bucketMax = 0f;
                }
            }

            if (samplesInBucket > 0 && emittedCount < peaksCount)
                Emit(ToPeak(bucketMax)); // Trailing bucket the decode only partially filled

            while (emittedCount < peaksCount)
                Emit(0f); // ffprobe reported a longer duration than the decode produced; pad to keep the timeline aligned

            if (batch.Count > 0)
                deliverBatch(batch); // Final partial batch for this file
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return false;
        }
    }

    private static float ToPeak(float maxMagnitude) =>
        Math.Clamp(maxMagnitude, 0f, 1f) * PEAK_SCALE;
}

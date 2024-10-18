using AudioCat.Models;
using System;
using System.Globalization;
using System.Text;

namespace AudioCat.FFmpeg;

public sealed class FFmpegStats(string message) : IProcessingStats
{
    public string Message { get; } = message;
    public ulong? Size { get; } = GetSize(message);
    public TimeSpan Time { get; } = GetTime(message);
    public double? Bitrate { get; } = GetBitrate(message);
    public decimal? Speed { get; } = GetSpeed(message);

    private const string NO_VALUE = "N/A";

    private static ulong? GetSize(string message)
    {
        var valueSpan = ExtractValue(message, "size=", "KiB");
        return valueSpan != default && ulong.TryParse(valueSpan, out var value)
            ? value * 1024UL
            : null;
    }

    private static TimeSpan GetTime(string message)
    {
        var valueSpan = ExtractValue(message, "time=", ".");
        var hoursEnd = valueSpan.IndexOf(':');
        if (hoursEnd < 0 || !int.TryParse(valueSpan[..hoursEnd], out var hours))
            return default;

        var minutesSpan = valueSpan[(hoursEnd + 1)..];
        var minutesEnd = minutesSpan.IndexOf(':');
        if (minutesEnd < 0 || !int.TryParse(minutesSpan[..minutesEnd], out var minutes))
            return default;

        var secondsSpan = minutesSpan[(minutesEnd + 1)..];
        return int.TryParse(secondsSpan, out var seconds)
            ? new TimeSpan(hours, minutes, seconds)
            : default;
    }

    private static double? GetBitrate(string message)
    {
        var valueSpan = ExtractValue(message, "bitrate=", "kbits/s");
        return valueSpan != default && double.TryParse(valueSpan, out var value)
            ? Math.Round(value, 1, MidpointRounding.AwayFromZero)
            : null;
    }

    private static decimal? GetSpeed(string message)
    {
        var valueSpan = ExtractValue(message, "speed=", "x");
        return valueSpan != default && decimal.TryParse(valueSpan, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
    }

    private static ReadOnlySpan<char> ExtractValue(string message, string start, string end)
    {
        var startOffset = message.IndexOf(start, StringComparison.OrdinalIgnoreCase);
        if (startOffset < 0)
            return default;

        startOffset += start.Length;
        var valueSpan = message.AsSpan(startOffset);
        if (valueSpan.StartsWith(NO_VALUE, StringComparison.OrdinalIgnoreCase))
            return default;

        var endOffset = message.IndexOf(end, StringComparison.OrdinalIgnoreCase);
        if (endOffset < 0)
            return default;

        while (startOffset < message.Length && message[startOffset] == ' ') // Skip leading spaces, avoiding trim for performance
            startOffset++;

        return startOffset < message.Length
            ? message.AsSpan(startOffset, endOffset - startOffset)
            : default;
    }

    public override string ToString()
    {
        const string prefix = "Processing: ";
        var sb = new StringBuilder(prefix);

        if (Size is > 0)
            sb.Append($"Size: {Size.Value / 1024:N0}KiB");
        if (Time != default)
        {
            if (sb.Length > prefix.Length)
                sb.Append("; ");
            sb.Append($"Time: {Math.Truncate(Time.TotalHours):00}:{Time.Minutes:00}:{Time.Seconds:00}");
        }
        if (Bitrate is > 0)
        {
            if (sb.Length > prefix.Length)
                sb.Append("; ");
            sb.Append($"Bitrate: {Bitrate:0.0}Kb/s");
        }
        if (Speed is > 0)
        {
            if (sb.Length > prefix.Length)
                sb.Append("; ");
            sb.Append($"Speed: {Speed:N0}x");
        }

        if (sb.Length == prefix.Length)
            sb.Append("...");

        return sb.ToString();
    }
}
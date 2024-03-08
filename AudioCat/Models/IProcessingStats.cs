namespace AudioCat.Models;

public interface IProcessingStats
{
    ulong? Size { get; }       // Processed data size in bytes
    TimeSpan Time { get; }     // Processed time
    double? Bitrate { get; }   // Kilobits per second
    decimal? Speed { get; }    // Processing speed 
    string Message { get; }    // Unprocessed status message
}
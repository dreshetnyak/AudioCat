namespace AudioCat.Services;

public class Progress(TimeSpan totalDuration, TimeSpan processedDuration, string message = "")
{
    public string Message { get; } = message;
    public TimeSpan TotalDuration { get; } = totalDuration;
    public TimeSpan ProcessedDuration { get; } = processedDuration;

    public int CalculatePercentage() => CalculatePercentage(TotalDuration, ProcessedDuration);

    public static int CalculatePercentage(TimeSpan totalDuration, TimeSpan processedDuration)
    {
        if (totalDuration == TimeSpan.Zero)
            return Constants.PROGRESS_BAR_MAX_VALUE;
        var percentage = (int)((decimal)processedDuration.TotalSeconds * Constants.PROGRESS_BAR_MAX_VALUE / (decimal)totalDuration.TotalSeconds);
        if (percentage < 0)
            percentage = 0;
        if (percentage > Constants.PROGRESS_BAR_MAX_VALUE)
            percentage = Constants.PROGRESS_BAR_MAX_VALUE;
        return percentage;
    }
}
using System.Globalization;
using System.Windows.Data;

namespace AudioCat.Converters;

internal class TimeSpanConverter : IValueConverter
{
    //Source to target
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var timeSpan = value switch
        {
            decimal seconds => TimeSpan.FromSeconds((double)seconds),
            TimeSpan time => time,
            _ => TimeSpan.Zero
        };
        return $"{timeSpan.TotalHours:00}:{timeSpan.Minutes:00}:{timeSpan.Seconds:00}.{timeSpan.Milliseconds:000}";
    }

    //Target to source
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
using System.Globalization;
using System.Windows.Data;

namespace AudioCat.Converters;

internal class DurationConverter : IValueConverter
{
    //Source to target
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is TimeSpan { TotalSeconds: > 0 } time
            ? $"{time.TotalHours:00}:{time.Minutes:00}:{time.Seconds:00}"
            : "N/A";

    //Target to source
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => 
        throw new NotImplementedException();
}
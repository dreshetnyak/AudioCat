using System.Globalization;
using System.Windows.Data;

namespace AudioCat.Converters;

internal class BitrateConverter : IValueConverter
{
    //Source to target
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is decimal bitrate and > 0
            ? $"{bitrate / 1000m:0.0} Kb/s"
            : "N/A";

    //Target to source
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => 
        throw new NotImplementedException();
}
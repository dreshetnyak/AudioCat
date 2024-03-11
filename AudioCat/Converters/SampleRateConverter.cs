using System.Globalization;
using System.Windows.Data;

namespace AudioCat.Converters;

internal class SampleRateConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is decimal sampleRate
            ? $"{sampleRate / 1000:N1} KHz"
            : "";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => 
        throw new NotImplementedException();
}
using System.Globalization;
using System.Windows.Data;

namespace AudioCat.Converters;

internal class BytesCountConverter : IValueConverter
{
    //Source to target
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is long count and > 0
            ? count.GetBytesCountToText()
            : 0L.GetBytesCountToText();

    //Target to source
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => 
        throw new NotImplementedException();
}
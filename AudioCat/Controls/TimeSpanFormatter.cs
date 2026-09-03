using System;
using System.Globalization;
using System.Windows.Data;

namespace AudioCat.Controls;

[ValueConversion(typeof(TimeSpan), typeof(string))]
internal sealed class TimeSpanFormatter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not TimeSpan ts)
            return "0:00";

        return ts switch
        {
            { TotalDays: >= 1 } => $"{(int)ts.TotalDays}.{ts.Hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}",
            { TotalHours: >= 1 } => $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}",
            _ => $"{ts.Minutes:D2}:{ts.Seconds:D2}"
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
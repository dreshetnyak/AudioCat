using System.Globalization;
using System.Windows.Data;
using System.Windows;

namespace AudioCat.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not bool sourceBool)
            return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;

        var (invert, collapse) = GetParameters(parameter);
        if (invert)
            sourceBool = !sourceBool;

        if (sourceBool)
            return Visibility.Visible;

        return collapse
            ? Visibility.Collapsed
            : Visibility.Hidden;
    }

    private static (bool invert, bool collapse) GetParameters(object? parametersObject) => parametersObject is string parameters
        ? (parameters.Contains("Invert", StringComparison.OrdinalIgnoreCase), parameters.Contains("Collapse", StringComparison.OrdinalIgnoreCase))
        : (false, false);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => 
        value is Visibility.Visible;
}
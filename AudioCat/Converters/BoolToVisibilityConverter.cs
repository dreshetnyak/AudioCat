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

        return sourceBool
            ? Visibility.Visible
            : collapse
                ? Visibility.Collapsed
                : Visibility.Hidden;
    }

    private static (bool invert, bool collapse) GetParameters(object? parametersObject) => parametersObject is string parameters
        ? (parameters.IndexOf("Invert", StringComparison.OrdinalIgnoreCase) != -1, parameters.IndexOf("Collapse", StringComparison.OrdinalIgnoreCase) != -1)
        : (false, false);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => 
        value is Visibility.Visible;
}
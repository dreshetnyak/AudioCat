using System.Globalization;
using System.Windows.Data;
using AudioCat.Models;

namespace AudioCat.Converters;

internal class TagsConcatConverter : IValueConverter
{
    //Source to target
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => 
        (value as IReadOnlyList<IMediaTag>).ConcatenateTags();

    //Target to source
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => 
        throw new NotImplementedException();
}
using System.Globalization;
using System.Text;

namespace AudioCat;

internal static class Extensions
{
    public static string ToQuoted(this string str) => str switch { null => "[null]", "" => "[empty]", _ => $"'{str}'" };
    
    public static decimal? ToDecimal(this string str) =>  decimal.TryParse(str, NumberStyles.Number, CultureInfo.CurrentCulture, out var value) ? value : null;

    public static int? ToInt(this string str) => int.TryParse(str, NumberStyles.Integer, CultureInfo.CurrentCulture, out var value) ? value : null;

    public static long? ToLong(this string str) => long.TryParse(str, NumberStyles.Integer, CultureInfo.CurrentCulture, out var value) ? value : null;

    public static TimeSpan? SecondsToTimeSpan(this string str) =>  
        decimal.TryParse(str, NumberStyles.Number, CultureInfo.CurrentCulture, out var value) 
            ? TimeSpan.FromSeconds((double)value)
            : null;

    public static string GetBytesCountToText(this long size)
    {
        return size switch
        {
            < 1024 => $"{size:N0} B",
            < 1_048_576 => $"{size / 1024m:N0} KB",
            < 1_073_741_824 => $"{size / 1_048_576m:N1} MB",
            _ => $"{size / 1_073_741_824m:N2} GB"
        };
    }

    public static string ConcatenateTags(this IReadOnlyList<KeyValuePair<string, string>>? tags)
    {
        if (tags == null)
            return "";

        var sb = new StringBuilder();
        foreach (var tag in tags)
        {
            if (sb.Length > 0)
                sb.Append("; ");
            sb.Append(tag.Key);
            sb.Append(": ");
            sb.Append(tag.Value.ToQuoted());
        }

        return sb.ToString();
    }
}
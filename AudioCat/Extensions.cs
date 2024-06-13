using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using AudioCat.Models;
using AudioCat.ViewModels;

namespace AudioCat;

internal static class Extensions
{
    public static string ToQuoted(this string str) => str switch { null => "[null]", "" => "[empty]", _ => $"'{str}'" };
    
    public static decimal? ToDecimal(this string str) =>  decimal.TryParse(str, NumberStyles.Number, CultureInfo.CurrentCulture, out var value) ? value : null;

    public static int? ToInt(this string str) => int.TryParse(str, NumberStyles.Integer, CultureInfo.CurrentCulture, out var value) ? value : null;

    public static long? ToLong(this string str) => long.TryParse(str, NumberStyles.Integer, CultureInfo.CurrentCulture, out var value) ? value : null;

    public static string TrimStartNonChars(this string source)
    {
        var startIndex = 0;
        for (; startIndex < source.Length; startIndex++)
        {
            var ch = source[startIndex];
            if (char.IsLetter(ch))
                return source[startIndex..];
        }

        return "";
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Is(this string left, string right) =>
        left.Equals(right, StringComparison.OrdinalIgnoreCase);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNot(this string left, string right) =>
        !left.Equals(right, StringComparison.OrdinalIgnoreCase);

    public static bool Has(this IEnumerable<string> enumerable, string str)
    {
        foreach (var item in enumerable)
        {
            if (item.Equals(str, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    public static bool IsPrintable(this char ch) => char.GetUnicodeCategory(ch) switch
    {
        UnicodeCategory.Control or UnicodeCategory.Format or UnicodeCategory.Surrogate or UnicodeCategory.PrivateUse or UnicodeCategory.OtherNotAssigned => false,
        _ => true
    };

    public static string FilterPrintable(this string str)
    {
        var nameBuilder = new StringBuilder(str.Length);
        foreach (var ch in str)
        {
            if (ch.IsPrintable())
                nameBuilder.Append(ch);
        }

        return nameBuilder.ToString();
    }

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

    public static string ConcatenateTags(this IReadOnlyList<IMediaTag>? tags)
    {
        if (tags == null)
            return "";

        var sb = new StringBuilder();
        foreach (var tag in tags)
        {
            if (sb.Length > 0)
                sb.Append("; ");
            sb.Append(tag.Name);
            sb.Append(": ");
            sb.Append(tag.Value.ToQuoted());
        }

        return sb.ToString();
    }

    public static int GetTagIndex(this IReadOnlyList<IMediaTag> tags, string name)
    {
        for (var index = 0; index < tags.Count; index++)
        {
            if (tags[index].Name.Is(name))
                return index;
        }

        return -1;
    }

    public static IMediaTag? GetTag(this IReadOnlyList<IMediaTag> tags, string name)
    {
        foreach (var tag in tags)
        {
            if (tag.Name.Is(name))
                return tag;

        }

        return null;
    }

    public static string GetTagValue(this IReadOnlyList<IMediaTag> tags, string name, string defaultValue = "")
    {
        var tag = GetTag(tags, name);
        return tag != null 
            ? tag.Value 
            : defaultValue;
    }

    public static bool ChaptersAlreadyExist(this IEnumerable<IMediaFileViewModel> mediaFiles)
    {
        foreach (var file in mediaFiles)
        {
            if (file.Chapters.Count > 0)
                return true;
        }

        return false;
    }
}
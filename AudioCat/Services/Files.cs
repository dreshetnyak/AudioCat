using System.Text.RegularExpressions;

namespace AudioCat.Services;

internal static partial class Files
{
    public static IReadOnlyList<string> Sort(IReadOnlyList<string> sourceFiles)
    {
        return [..sourceFiles.OrderBy(fileName => DigitRegex().Replace(fileName, match => match.Value.PadLeft(4, '0')))];
    }

    [GeneratedRegex(@"\d+")]
    private static partial Regex DigitRegex();
}
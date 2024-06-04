using System.IO;
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

    public static bool IsDirectory(this string path)
    {
        try { return Directory.Exists(path) && (File.GetAttributes(path) & FileAttributes.Directory) == FileAttributes.Directory; }
        catch { return false; }
    }

    public static bool IsAllDirectories(this IEnumerable<string> paths)
    {
        foreach (var path in paths)
        {
            if (!path.IsDirectory())
                return false;
        }

        return true;
    }
}
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

    public static async Task<IReadOnlyList<string>> GetFilesFromDirectories(IReadOnlyList<string> directories)
    {
        if (directories.Count == 0)
            return [];

        var files = new List<string>(1024);
        var sortedDirs = Files.Sort(directories);
        foreach (var dir in sortedDirs)
        {
            var dirFiles = await GetFilesFromDirectory(dir);
            if (dirFiles.Count > 0)
                files.AddRange(Files.Sort(dirFiles));
        }

        return files;
    }

    private static async Task<IReadOnlyList<string>> GetFilesFromDirectory(string directory)
    {
        var subDirectories = new List<string>();
        foreach (var subDirectory in new DirectoryInfo(directory).EnumerateDirectories("*", SearchOption.TopDirectoryOnly))
        {
            // Junctions and directory symlinks are not traversed: a link pointing at an ancestor
            // creates a cycle, and Windows aborts path resolution after 63 chained reparse points,
            // which would fail the whole walk partway through with an unrelated IO error
            if ((subDirectory.Attributes & FileAttributes.ReparsePoint) == 0)
                subDirectories.Add(subDirectory.FullName);
            await Task.Yield();
        }

        var subDirFiles = await GetFilesFromDirectories(subDirectories);
        var files = subDirFiles.Count > 0
            ? [.. subDirFiles]
            : new List<string>(1024);

        var dirFiles = new List<string>(1024);
        foreach (var file in Directory.EnumerateFiles(directory, "*", SearchOption.TopDirectoryOnly))
        {
            dirFiles.Add(file);
            await Task.Yield();
        }

        if (dirFiles.Count > 0)
            files.AddRange(Files.Sort(dirFiles));

        return files;
    }
}
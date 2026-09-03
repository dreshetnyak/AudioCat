using System.IO;
using System.Management;

namespace AudioCat.Services;

/// <summary>
/// All temporary files of a single operation are created inside one per-run directory,
/// so cleanup is a single recursive delete and nothing can be missed.
/// </summary>
internal static class TempDirectory
{
    private static string Root { get; } = Path.Combine(Path.GetTempPath(), "AudioCat");

    public static string Create()
    {
        var dir = Path.Combine(Root, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    public static void Delete(string dir)
    {
        try { Directory.Delete(dir, true); }
        catch { /* ignore */ }
    }

    /// <summary>Removes per-run directories created before the current OS boot.</summary>
    public static void Sweep()
    {
        try
        {
            if (!Directory.Exists(Root))
                return;

            var lastBootTimeUtc = GetLastBootTimeUtc();
            foreach (var dir in Directory.EnumerateDirectories(Root))
            {
                if (Directory.GetCreationTimeUtc(dir) >= lastBootTimeUtc)
                    continue;

                try { Directory.Delete(dir, true); }
                catch { /* ignore */ }
            }
        }
        catch { /* ignore */ }
    }

    private static DateTime GetLastBootTimeUtc()
    {
        using var searcher = new ManagementObjectSearcher(
            "SELECT LastBootUpTime FROM Win32_OperatingSystem");
        using var results = searcher.Get();

        foreach (ManagementObject operatingSystem in results)
        {
            using (operatingSystem)
            {
                var value = operatingSystem["LastBootUpTime"]?.ToString();
                if (value is not null)
                    return ManagementDateTimeConverter.ToDateTime(value).ToUniversalTime();
            }
        }

        throw new InvalidOperationException("Could not determine the last boot time.");
    }
}

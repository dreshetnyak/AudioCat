using System.IO;

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

    /// <summary>Removes per-run directories left behind by crashed or killed instances.</summary>
    public static void Sweep()
    {
        try
        {
            if (!Directory.Exists(Root))
                return;
            foreach (var dir in Directory.EnumerateDirectories(Root))
            {
                try { Directory.Delete(dir, true); }
                catch { /* the directory may belong to another running instance */ }
            }
        }
        catch { /* ignore */ }
    }
}

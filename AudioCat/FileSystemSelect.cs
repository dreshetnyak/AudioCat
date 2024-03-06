using Microsoft.Win32;

namespace AudioCat;

internal static class FileSystemSelect
{
    public static string[] FilesToOpen(string filter, bool multiSelect = false)
    {
        var selectFilesDialog = new OpenFileDialog { Filter = filter, Multiselect = multiSelect };
        return selectFilesDialog.ShowDialog() ?? false
            ? selectFilesDialog.FileNames
            : [];
    }

    public static string FileToSave(string filter, string fileName = "")
    {
        var selectFileDialog = new SaveFileDialog { Filter = filter, FileName = fileName };
        return selectFileDialog.ShowDialog() ?? false
            ? selectFileDialog.FileName ?? ""
            : "";
    }

    public static string Folder(bool multiSelect = false)
    {
        var selectFolderDialog = new OpenFolderDialog { Multiselect = multiSelect };
        return selectFolderDialog.ShowDialog() ?? false
            ? selectFolderDialog.FolderNames?.FirstOrDefault() ?? ""
            : "";
    }
}
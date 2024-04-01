using System.IO;
using Microsoft.Win32;

namespace AudioCat.Services;

internal static class SelectionDialog
{
    public static string[] ChooseFilesToOpen(string filter, bool multiSelect = false)
    {
        var selectFilesDialog = new OpenFileDialog { Filter = filter, Multiselect = multiSelect };
        return selectFilesDialog.ShowDialog() ?? false
            ? selectFilesDialog.FileNames
            : [];
    }

    public static string ChooseFileToSave(string filter, string fileName = "", string initialDirectory = "")
    {
        var selectFileDialog = Directory.Exists(initialDirectory)
            ? new SaveFileDialog { Filter = filter, FileName = fileName, RestoreDirectory = true, InitialDirectory = initialDirectory }
            : new SaveFileDialog { Filter = filter, FileName = fileName };
        return selectFileDialog.ShowDialog() ?? false
            ? selectFileDialog.FileName ?? ""
            : "";
    }

    public static string ChooseFolder(bool multiSelect = false)
    {
        var selectFolderDialog = new OpenFolderDialog { Multiselect = multiSelect };
        return selectFolderDialog.ShowDialog() ?? false
            ? selectFolderDialog.FolderNames?.FirstOrDefault() ?? ""
            : "";
    }
}
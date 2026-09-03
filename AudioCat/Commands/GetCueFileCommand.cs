using AudioCat.Models;
using AudioCat.Services;
using System.Windows;

namespace AudioCat.Commands;

internal class GetCueFileCommand : CommandBase
{
    protected override async Task<IResponse<object>> Command(object? parameter)
    {
        try
        {
            var fileNames = SelectionDialog.ChooseFilesToOpen("Cue Sheet|*.cue", true);
            if (fileNames.Length == 0)
                return Response<object>.Success();

            var sortedFileNames = fileNames.Length > 1 ? Files.Sort(fileNames) : fileNames;

            var cueFiles = new List<Cue.ICue>(sortedFileNames.Count);
            foreach (var fileName in sortedFileNames)
            {
                var parseResponse = await Cue.Parser.Parse(fileName);
                if (parseResponse.IsSuccess)
                    cueFiles.Add(parseResponse.Data!);
                else
                {
                    var result = MessageBox.Show($"Failed to parse '{fileName}'; Error: {parseResponse.Message}; Abort adding?", "Cue parsing error", MessageBoxButton.YesNo, MessageBoxImage.Error);
                    if (result == MessageBoxResult.Yes)
                        return Response<object>.Success();
                }
            }
            
            return Response<object>.Success(cueFiles.AsReadOnly());
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Files selection error", MessageBoxButton.OK, MessageBoxImage.Error);
            return Response<object>.Failure(ex.Message);
        }
    }
}
using AudioCat.Models;
using AudioCat.ViewModels;
using AudioCat.Windows;

namespace AudioCat.Commands;

public sealed class CreateChaptersCommand(
    IMediaFilesContainer mediaFilesContainer,
    FixItemEncodingCommand fixItemEncodingCommand, 
    FixItemsEncodingCommand fixItemsEncodingCommand,
    ScanForSilenceCommand scanForSilence) : CommandBase
{
    protected override Task<IResponse<object>> Command(object? parameter)
    {
        var viewModel = new CreateChaptersViewModel(mediaFilesContainer.Files, fixItemEncodingCommand, fixItemsEncodingCommand, scanForSilence);
        var createChaptersWindow = new CreateChaptersWindow(viewModel);
        var result = createChaptersWindow.ShowDialog();
        return result.HasValue && result.Value 
            ? Task.FromResult(Response<object>.Success(viewModel.CreatedChapters)) 
            : Task.FromResult(Response<object>.Success());
    }
}
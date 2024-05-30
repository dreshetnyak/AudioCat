using AudioCat.Models;

namespace AudioCat.Commands;

internal class ToggleEnableCommand : CommandBase
{
    protected override Task<IResponse<object>> Command(object? parameter)
    {
        if (parameter is IEnableCapable enableSource)
            enableSource.IsEnabled = !enableSource.IsEnabled;
        return Task.FromResult(Response<object>.Success());
    }
}
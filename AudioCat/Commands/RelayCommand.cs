using AudioCat.Models;

namespace AudioCat.Commands;

internal class RelayCommand(Action commandAction) : CommandBase
{
    public Action CommandAction { get; } = commandAction;

    protected override Task<IResult> Command(object? parameter)
    {
        CommandAction();
        return Task.FromResult(Result.Success());
    }
}
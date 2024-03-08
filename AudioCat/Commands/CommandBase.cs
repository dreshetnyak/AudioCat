using AudioCat.Models;
using System.Windows.Input;

namespace AudioCat.Commands;

public sealed class ResultEventArgs(IResult result) : EventArgs
{
    public IResult Result { get; } = result;
}
public delegate void ResultEventHandler(object sender, ResultEventArgs eventArgs);

public abstract class CommandBase : ICommand
{
    protected abstract Task<IResult> Command(object? parameter);

    public event EventHandler? Starting;
    public event ResultEventHandler? Finished;

    public async void Execute(object? parameter)
    {
        var result = Result.Failure("Unknown error");
        try
        {
            CanBeExecuted = false;
            OnStarting();
            result = await Command(parameter);
        }
        catch
        {
            /* ignore */
        }
        finally
        {
            OnFinished(result);
            CanBeExecuted = true;
        }
    }

    #region Can Execute Implementation
    private bool _canBeExecuted = true;
    public bool CanBeExecuted
    {
        get => _canBeExecuted;
        set
        {
            if (_canBeExecuted == value) 
                return;
            _canBeExecuted = value;
            OnCanExecuteChanged();
        }
    }

    public bool CanExecute(object? parameter) => CanBeExecuted;
        
    public event EventHandler? CanExecuteChanged;

    private void OnCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);

    #endregion

    protected virtual void OnStarting() => Starting?.Invoke(this, EventArgs.Empty);
    protected virtual void OnFinished(IResult result) => Finished?.Invoke(this, new ResultEventArgs(result));
}
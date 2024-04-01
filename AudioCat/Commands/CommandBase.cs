using AudioCat.Models;
using System.Windows.Input;

namespace AudioCat.Commands;

public sealed class ResponseEventArgs(IResponse<object> response) : EventArgs
{
    public IResponse<object> Response { get; } = response;
}
public delegate void ResponseEventHandler(object sender, ResponseEventArgs eventArgs);

public abstract class CommandBase : ICommand
{
    protected abstract Task<IResponse<object>> Command(object? parameter);

    public event EventHandler? Starting;
    public event ResponseEventHandler? Finished;

    public async void Execute(object? parameter)
    {
        var response = Response<object>.Failure("Unknown error");
        try
        {
            CanBeExecuted = false;
            OnStarting();
            response = await Command(parameter);
        }
        catch
        {
            /* ignore */
        }
        finally
        {
            OnFinished(response);
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
    protected virtual void OnFinished(IResponse<object> response) => Finished?.Invoke(this, new ResponseEventArgs(response));
}
using System.Windows.Input;

namespace AudioCat.Commands
{
    internal abstract class CommandBase : ICommand
    {
        protected abstract Task Command(object? parameter);

        public event EventHandler? Starting;
        public event EventHandler? Finished;

        public async void Execute(object? parameter)
        {
            try
            {
                CanBeExecuted = false;
                OnStarting();
                await Command(parameter);
            }
            catch
            {
                /* ignore */
            }
            finally
            {
                OnFinished();
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
        protected virtual void OnFinished() => Finished?.Invoke(this, EventArgs.Empty);
    }
}

namespace AudioCat.Commands
{
    internal class RelayCommand(Action commandAction) : CommandBase
    {
        public Action CommandAction { get; } = commandAction;

        protected override Task Command(object? parameter)
        {
            CommandAction();
            return Task.CompletedTask;
        }
    }
}

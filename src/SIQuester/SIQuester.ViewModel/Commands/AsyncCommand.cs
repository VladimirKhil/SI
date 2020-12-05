using SIQuester.ViewModel.Core;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SIQuester.ViewModel.Commands
{
    public sealed class AsyncCommand : IAsyncCommand
    {
        private readonly Func<object, Task> _execute = null;
        private bool _canBeExecuted = true;

        public event EventHandler CanExecuteChanged;

        public bool CanBeExecuted
        {
            get { return _canBeExecuted; }
            set
            {
                if (_canBeExecuted != value)
                {
                    _canBeExecuted = value;
                    if (CanExecuteChanged != null)
                    {
                        if (SynchronizationContext.Current == null)
                        {
                            Task.Factory.StartNew(() => CanExecuteChanged?.Invoke(this, EventArgs.Empty), CancellationToken.None, TaskCreationOptions.None, UI.Scheduler);
                        }
                        else
                            CanExecuteChanged(this, EventArgs.Empty);
                    }
                }
            }
        }

        public AsyncCommand(Func<object, Task> execute)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        }

        public bool CanExecute(object parameter) => _canBeExecuted;

        public async void Execute(object parameter) => await _execute(parameter);

        public Task ExecuteAsync(object parameter) => _execute(parameter);
    }
}

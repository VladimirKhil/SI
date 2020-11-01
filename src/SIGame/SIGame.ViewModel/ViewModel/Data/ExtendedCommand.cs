using SIUI.ViewModel.Core;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SIGame.ViewModel
{
    public sealed class ExtendedCommand : ICommand
    {
        private readonly Action<object> _execute = null;

        public HashSet<object> ExecutionArea { get; } = new HashSet<object>();

        public void OnCanBeExecutedChanged()
        {
            if (CanExecuteChanged != null)
            {
                if (SynchronizationContext.Current == null)
                {
                    Task.Factory.StartNew(() => CanExecuteChanged(this, EventArgs.Empty), CancellationToken.None, TaskCreationOptions.None, UI.Scheduler);
                }
                else
                    CanExecuteChanged(this, EventArgs.Empty);
            }
        }

        public ExtendedCommand(Action<object> execute)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        }

        public bool CanExecute(object parameter)
        {
            return ExecutionArea.Contains(parameter);
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter)
        {
            _execute(parameter);
        }
    }
}

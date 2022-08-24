using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Utils;

namespace SIQuester.ViewModel
{
    /// <summary>
    /// Упрощённая реализация команды
    /// </summary>
    public sealed class SimpleCommand : ICommand
    {
        private bool _canBeExecuted = true;

        /// <summary>
        /// Можно ли выполнить команду в настоящий момент
        /// </summary>
        public bool CanBeExecuted
        {
            get => _canBeExecuted;
            set
            {
                if (_canBeExecuted != value)
                {
                    _canBeExecuted = value;
                    if (CanExecuteChanged != null)
                    {
                        if (SynchronizationContext.Current == null)
                        {
                            Task.Factory.StartNew(
                                () => CanExecuteChanged?.Invoke(this, EventArgs.Empty),
                                CancellationToken.None,
                                TaskCreationOptions.None,
                                UI.Scheduler);
                        }
                        else
                        {
                            CanExecuteChanged(this, EventArgs.Empty);
                        }
                    }
                }
            }
        }

        private readonly Action<object> _action = null;

        public bool CanExecute(object parameter) => _canBeExecuted;

        /// <summary>
        /// Возможность выполнения команды изменилась
        /// </summary>
        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter) => _action?.Invoke(parameter);

        public SimpleCommand(Action<object> action)
        {
            _action = action;
        }
    }
}

using SIUI.ViewModel.Core;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SICore
{
    // TODO: в перспективе перенести в SIGame
    /// <summary>
    /// Оптимизированная команда (не использует проверку исполнения с параметром)
    /// </summary>
    public class CustomCommand : ICommand
    {
        #region Fields

        private readonly Action<object> _execute = null;
        private bool _canBeExecuted = true;

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

        #endregion // Fields

        #region Constructors

        public CustomCommand(Action<object> execute)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        }

        #endregion // Constructors

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parameter">Этот параметр игнорируется</param>
        /// <returns></returns>
        public bool CanExecute(object parameter)
        {
            return _canBeExecuted;
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter)
        {
            _execute(parameter);
        }
    }

}

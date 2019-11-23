using SIQuester.ViewModel.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SIQuester.ViewModel
{
    /// <summary>
    /// Рабочая область приложения
    /// </summary>
    public abstract class WorkspaceViewModel: ModelViewBase
    {
        /// <summary>
        /// Закрыть рабочую область
        /// </summary>
        public IAsyncCommand Close { get; private set; }
        /// <summary>
        /// Название рабочей области
        /// </summary>
        public abstract string Header { get; }

        /// <summary>
        /// Ошибка, возникшая в момент выполнения какой-либо операции
        /// </summary>
        public event Action<Exception> Error;
        public event Action<WorkspaceViewModel> Closed;
        public event Action<WorkspaceViewModel> NewItem;

        protected WorkspaceViewModel()
        {
            Close = new AsyncCommand(Close_Executed);
        }

        protected virtual Task Close_Executed(object arg)
        {
            OnClosed();

            return Task.CompletedTask;
        }

        protected internal void OnError(Exception exc)
        {
            Error?.Invoke(exc);
        }

        protected void OnClosed()
        {
            Closed?.Invoke(this);
        }

        protected void OnNewItem(WorkspaceViewModel viewModel)
        {
            NewItem?.Invoke(viewModel);
        }

        protected internal virtual Task SaveIfNeeded(bool temp)
        {
            return Task.CompletedTask;
        }
    }
}

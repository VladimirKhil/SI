using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SIQuester.ViewModel.Commands
{
    public interface IAsyncCommand : ICommand
    {
        bool CanBeExecuted { get; set; }

        Task ExecuteAsync(object parameter);
    }
}

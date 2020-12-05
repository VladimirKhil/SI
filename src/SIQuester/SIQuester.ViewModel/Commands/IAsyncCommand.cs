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

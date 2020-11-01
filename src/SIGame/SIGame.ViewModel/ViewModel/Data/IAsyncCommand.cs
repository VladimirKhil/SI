using System.Threading.Tasks;
using System.Windows.Input;

namespace SIGame.ViewModel
{
    public interface IAsyncCommand: ICommand
    {
        bool CanBeExecuted { get; set; }

        Task ExecuteAsync(object parameter);
    }
}

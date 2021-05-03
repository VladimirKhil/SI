using System.Threading.Tasks;
using System.Windows.Input;

namespace SIGame.ViewModel
{
    /// <summary>
    /// Provides a UI command that is executed asynchronously.
    /// </summary>
    public interface IAsyncCommand: ICommand
    {
        /// <summary>
        /// Can command be executed flag.
        /// </summary>
        bool CanBeExecuted { get; set; }

        /// <summary>
        /// Executes the command.
        /// </summary>
        Task ExecuteAsync(object parameter);
    }
}

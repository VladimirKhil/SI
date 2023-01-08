using System.Windows.Input;

namespace SIGame.ViewModel;

/// <summary>
/// Provides a command that is executed asynchronously.
/// </summary>
public interface IAsyncCommand : ICommand
{
    /// <summary>
    /// Can the command be executed.
    /// </summary>
    bool CanBeExecuted { get; set; }

    /// <summary>
    /// Executes the command.
    /// </summary>
    Task ExecuteAsync(object? parameter);
}

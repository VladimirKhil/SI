using System.Windows.Input;

namespace Utils.Commands;

/// <summary>
/// Defines a command that could be executed asynchronously.
/// </summary>
public interface IAsyncCommand : ICommand
{
    /// <summary>
    /// Can the command be executed now.
    /// </summary>
    bool CanBeExecuted { get; set; }

    /// <summary>
    /// Executes the command asynchronously.
    /// </summary>
    /// <param name="parameter">Command parameter.</param>
    Task ExecuteAsync(object? parameter);
}

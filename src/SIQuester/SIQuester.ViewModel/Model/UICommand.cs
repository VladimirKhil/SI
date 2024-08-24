using System.Windows.Input;

namespace SIQuester.ViewModel.Model;

/// <summary>
/// Defines a UI-bound command.
/// </summary>
/// <param name="Command">Command to execute.</param>
/// <param name="Name">Command display name.</param>
/// <param name="Parameter">Command parameter.</param>
public sealed record UICommand(ICommand Command, string Name, object Parameter);

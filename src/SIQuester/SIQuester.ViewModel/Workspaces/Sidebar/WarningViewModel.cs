using System.Windows.Input;
using Utils.Commands;

namespace SIQuester.ViewModel.Workspaces.Sidebar;

/// <summary>
/// Defines a statistic item warning view model.
/// </summary>
public sealed class WarningViewModel
{
    /// <summary>
    /// Model title.
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// Allows to navigate to item that caused the warning.
    /// </summary>
    public ICommand NavigateToSource { get; set; }

    public WarningViewModel(string title, Action navigateToSource)
    {
        Title = title;
        NavigateToSource = new SimpleCommand(arg => navigateToSource());
    }
}

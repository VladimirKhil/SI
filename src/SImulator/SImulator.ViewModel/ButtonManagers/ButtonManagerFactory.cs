using SImulator.ViewModel.Model;
using SImulator.ViewModel.Core;

namespace SImulator.ViewModel.ButtonManagers;

/// <summary>
/// Defines a factory for creating button managers.
/// </summary>
public class ButtonManagerFactory
{
    /// <summary>
    /// Creates an instance of <see cref="IButtonManager" />.
    /// </summary>
    /// <param name="settings">Button manager settings.</param>
    /// <returns>Created button manager.</returns>
    public virtual IButtonManager? Create(AppSettings settings, IButtonManagerListener buttonManagerListener) =>
        settings.UsePlayersKeys switch
        {
            PlayerKeysModes.External => new EmptyButtonManager(buttonManagerListener),
            _ => null,
        };
}

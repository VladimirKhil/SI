using SImulator.ViewModel.Contracts;
using SImulator.ViewModel.Core;
using SImulator.ViewModel.Model;

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
    /// <param name="buttonManagerListener">Button manager listener.</param>
    /// <param name="platformService">Platform service.</param>
    /// <returns>Created button manager.</returns>
    public virtual Task<IButtonManager?> CreateAsync(AppSettings settings, IButtonManagerListener buttonManagerListener, IPlatformService platformService) =>
        Task.FromResult<IButtonManager?>(settings.UsePlayersKeys switch
        {
            PlayerKeysModes.External => new EmptyButtonManager(buttonManagerListener),
            _ => null,
        });
}

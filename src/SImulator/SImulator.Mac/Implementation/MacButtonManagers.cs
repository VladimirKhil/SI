using Avalonia.Input;
using SImulator.Implementation.ButtonManagers.WebNew;
using SImulator.ViewModel.ButtonManagers;
using SImulator.ViewModel.Contracts;
using SImulator.ViewModel.Core;
using SImulator.ViewModel.Model;

namespace SImulator.Implementation;

/// <summary>
/// Delivers keys pressed in SImulator windows to the keyboard button manager.
/// </summary>
/// <remarks>
/// macOS does not allow global keyboard hooks without accessibility permissions,
/// so player keys work while a SImulator window (the board or the control window) is active.
/// </remarks>
internal static class KeyboardHub
{
    internal static event Func<Key, bool>? KeyPressed;

    /// <summary>
    /// Processes a key pressed in a SImulator window.
    /// </summary>
    /// <returns>True if the key has been handled.</returns>
    internal static bool OnKeyPressed(Key key) => KeyPressed?.Invoke(key) ?? false;
}

/// <summary>
/// Provides keyboard-based player buttons.
/// </summary>
internal sealed class KeyboardButtonManager(IButtonManagerListener buttonManagerListener) : ButtonManagerBase(buttonManagerListener)
{
    public override bool Start()
    {
        KeyboardHub.KeyPressed += OnKeyPressed;
        return true;
    }

    public override void Stop() => KeyboardHub.KeyPressed -= OnKeyPressed;

    private bool OnKeyPressed(Key key) => Listener.OnKeyPressed((GameKey)key);
}

/// <inheritdoc cref="ButtonManagerFactory" />
internal sealed class MacButtonManagerFactory : ButtonManagerFactory
{
    public override async Task<IButtonManager?> CreateAsync(AppSettings settings, IButtonManagerListener buttonManagerListener, IPlatformService platformService) =>
        settings.UsePlayersKeys switch
        {
            PlayerKeysModes.Keyboard => new KeyboardButtonManager(buttonManagerListener),
            PlayerKeysModes.WebNew => await WebManagerNew.CreateAsync(settings.WebPort, buttonManagerListener, platformService),
            _ => await base.CreateAsync(settings, buttonManagerListener, platformService),
        };
}

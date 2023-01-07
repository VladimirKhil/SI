namespace SImulator.ViewModel.Core;

public enum GameMode
{
    Start,
    Moderator,
}

public enum ModeTransition
{
    StartToModerator,
    ModeratorToStart,
}

/// <summary>
/// Defines supported player key managers.
/// </summary>
public enum PlayerKeysModes
{
    None,
    External,
    Keyboard,
    Joystick,
    Com,
    Web
}

/// <summary>
/// Defines players table display mode.
/// </summary>
public enum PlayersViewMode
{
    Hidden,
    Visible,
    Separate
}

/// <summary>
/// Defines a platform-independent key value.
/// </summary>
public enum GameKey { }

namespace SImulator.ViewModel.Core;

public enum GameMode
{
    Start,
    Moderator,
    View
}

public enum ModeTransition
{
    None,
    StartToModerator,
    StartToView,
    ModeratorToStart,
    ViewToStart
}

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
/// Режим отображения таблицы игроков
/// </summary>
public enum PlayersViewMode
{
    Hidden,
    Visible,
    Separate
}

/// <summary>
/// Платформенно-независимый аналог перечисления Key
/// </summary>
public enum GameKey { }

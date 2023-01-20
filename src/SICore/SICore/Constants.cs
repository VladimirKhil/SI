namespace SICore;

/// <summary>
/// Contains well-known unlocalizable game constants.
/// </summary>
public static class Constants
{
    /// <summary>
    /// Game host name placeholder in game host Uri.
    /// </summary>
    public const string GameHost = "<GAMEHOST>";

    /// <summary>
    /// Server host name placeholder in server host Uri.
    /// </summary>
    public const string ServerHost = "<SERVERHOST>";

    /// <summary>
    /// Свободный (пустой аккаунт)
    /// </summary>
    public const string FreePlace = " ";

    /// <summary>
    /// Частичный текст вопроса
    /// </summary>
    public const string PartialText = "partial";

    /// <summary>
    /// Максимальное количество игроков в игре
    /// </summary>
    public const int MaxPlayers = 12;

    /// <summary>
    /// Интервал запуска автоматической игры
    /// </summary>
    public const int AutomaticGameStartDuration = 300;

    /// <summary>
    /// Ведущий
    /// </summary>
    public const string Showman = "showman";

    /// <summary>
    /// Игрок
    /// </summary>
    public const string Player = "player";

    /// <summary>
    /// Viewer.
    /// </summary>
    public const string Viewer = "viewer";

    /// <summary>
    /// Место для подстановки ответа
    /// </summary>
    public const string AnswerPlaceholder = "#";

    /// <summary>
    /// Number of ingame timers.
    /// </summary>
    public const int TimersCount = 3;
}

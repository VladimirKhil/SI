using SIData;

namespace SIGame.ViewModel.Models;

/// <summary>
/// Contains game server info.
/// </summary>
public sealed class GameInfo
{
    public int GameID { get; set; }

    public string GameName { get; set; }

    public string Owner { get; set; }

    public string PackageName { get; set; }

    /// <summary>
    /// Дата создания
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Дата старта
    /// </summary>
    public DateTime RealStartTime { get; set; }

    /// <summary>
    /// Текущая стадия игры
    /// </summary>
    public string Stage { get; set; }

    /// <summary>
    /// Localized collection of game rules.
    /// </summary>
    public string[] Rules { get; set; }

    public bool PasswordRequired { get; set; }

    public ConnectionPersonData[] Persons { get; set; }

    public bool Started { get; set; }

    public GameModes Mode { get; set; }

    /// <summary>
    /// Minimum client protocol version required to join this game.
    /// </summary>
    public int MinimumClientProtocolVersion { get; set; }
}

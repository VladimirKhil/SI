using SIData;

namespace SI.GameServer.Contract;

/// <summary>
/// Describes a server game.
/// </summary>
public sealed class GameInfo : SimpleGameInfo
{
    /// <summary>
    /// Game owner.
    /// </summary>
    public string Owner { get; set; } = "";

    /// <summary>
    /// Game package human-readable name.
    /// </summary>
    public string PackageName { get; set; } = "";

    /// <summary>
    /// Game package restrictions.
    /// </summary>
    public string Restriction { get; set; } = "";

    /// <summary>
    /// Game creation (!) time.
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Game start time.
    /// </summary>
    public DateTime RealStartTime { get; set; }

    /// <summary>
    /// Current game stage.
    /// </summary>
    public GameStages Stage { get; set; }

    /// <summary>
    /// Current game stage human-readable name.
    /// </summary>
    public string StageName { get; set; } = "";

    /// <summary>
    /// Game rules.
    /// </summary>
    public GameRules Rules { get; set; }

    /// <summary>
    /// Game participants.
    /// </summary>
    public ConnectionPersonData[] Persons { get; set; } = Array.Empty<ConnectionPersonData>();

    /// <summary>
    /// Has game already started.
    /// </summary>
    public bool Started { get; set; }

    /// <summary>
    /// Game mode.
    /// </summary>
    public GameModes Mode { get; set; }

    /// <summary>
    /// Game language.
    /// </summary>
    public string Language { get; set; } = "";

    /// <summary>
    /// Minimum client protocol version required to join this game.
    /// </summary>
    public int MinimumClientProtocolVersion { get; set; }
}

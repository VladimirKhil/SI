namespace SIData;

/// <summary>
/// Defines game basic settings.
/// </summary>
public interface IGameSettingsCore<out T> where T: IAppSettingsCore
{
    /// <summary>
    /// Game host name.
    /// </summary>
    string HumanPlayerName { get; }

    Account Showman { get; }

    Account[] Players { get; }

    Account[] Viewers { get; }

    /// <summary>
    /// Core settings.
    /// </summary>
    T AppSettings { get; }

    /// <summary>
    /// Generate random special questions in every round.
    /// </summary>
    bool RandomSpecials { get; }

    string NetworkGameName { get; }

    string NetworkGamePassword { get; }

    string NetworkVoiceChat { get; }

    /// <summary>
    /// Defines a private game.
    /// </summary>
    /// <remarks>
    /// Private games are invisible in lobby game lists. They also do not allow to join for anybody except the host.
    /// In private games game name and password do not matter.
    /// </remarks>
    bool IsPrivate { get; }

    /// <summary>
    /// Marks an autogame.
    /// </summary>
    /// <remarks>
    /// Auto games starts automatically by timer or when they are full.
    /// Human players join these games automatically when they decide to play with random opponents.
    /// </remarks>
    bool IsAutomatic { get; }
}

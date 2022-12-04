namespace SImulator.Implementation.ButtonManagers.Web;

/// <summary>
/// Manages a button host.
/// </summary>
public interface IButtonProcessor
{
    /// <summary>
    /// Handles button press. Resolves a player name by their token.
    /// </summary>
    /// <param name="token">Player unique token.</param>
    /// <returns>Player name.</returns>
    PressResponse Press(string token);
}

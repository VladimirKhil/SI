namespace SImulator.Implementation.ButtonManagers.Web
{
    /// <summary>
    /// Manages a button host.
    /// </summary>
    public interface IButtonProcessor
    {
        /// <summary>
        /// Handles button press. Resolves a player name by their connection identifier.
        /// </summary>
        /// <param name="connectionId">Player unique connection identifier.</param>
        /// <returns>Player name.</returns>
        string Press(string connectionId);
    }
}

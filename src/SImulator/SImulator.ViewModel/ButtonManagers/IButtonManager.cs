using SImulator.ViewModel.Contracts;

namespace SImulator.ViewModel.ButtonManagers;

/// <summary>
/// Supports players buttons.
/// </summary>
public interface IButtonManager : IAsyncDisposable
{
    /// <summary>
    /// Does button manager manage player connections.
    /// </summary>
    bool ArePlayersManaged();

    /// <summary>
    /// Removes player by identifier.
    /// </summary>
    /// <param name="id">Player identifier.</param>
    /// <param name="name">Player name.</param>
    /// <param name="manually">Has the player been removed manually.</param>
    void RemovePlayerById(string id, string name, bool manually = true);

    /// <summary>
    /// Enables players buttons.
    /// </summary>
    /// <returns>Has the start been successfull.</returns>
    bool Start();

    /// <summary>
    /// Disables players buttons.
    /// </summary>
    void Stop();

    /// <summary>
    /// Gets command executor for current manager.
    /// </summary>
    ICommandExecutor? TryGetCommandExecutor();
}

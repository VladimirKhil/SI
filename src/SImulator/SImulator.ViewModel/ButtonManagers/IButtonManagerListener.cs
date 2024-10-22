using SImulator.ViewModel.Core;
using SImulator.ViewModel.Model;

namespace SImulator.ViewModel.ButtonManagers;

/// <summary>
/// Listens to button managers events and provides player info to button managers.
/// </summary>
public interface IButtonManagerListener
{
    /// <summary>
    /// Button blocking time in milliseconds.
    /// </summary>
    int ButtonBlockTime { get; }

    bool OnKeyPressed(GameKey key);

    bool OnPlayerPressed(PlayerInfo player);

    void OnPlayerPressed(string playerName) { }

    void OnPlayerPassed(string playerName) { }

    PlayerInfo? GetPlayerById(string playerId, bool strict);
    
    void OnPlayerAdded(string? id, string playerName) { }

    void OnPlayerRemoved(string playerName) { }

    void OnPlayerAnswered(string playerName, string answer) { }
}

using SImulator.ViewModel.Core;
using SImulator.ViewModel.Model;

namespace SImulator.ViewModel.ButtonManagers;

/// <summary>
/// Listens to button managers events and provide player info to button managers.
/// </summary>
public interface IButtonManagerListener
{
    bool OnKeyPressed(GameKey key);

    bool OnPlayerPressed(PlayerInfo player);

    PlayerInfo? GetPlayerById(string playerId, bool strict);
}

using SImulator.ViewModel.Core;
using SImulator.ViewModel.Model;

namespace SImulator.ViewModel.ButtonManagers;

/// <inheritdoc cref="IButtonManager" />
public abstract class ButtonManagerBase : IButtonManager
{
    public abstract bool Start();

    public abstract void Stop();

    public event Func<GameKey, bool>? KeyPressed;

    public event Func<string, bool, PlayerInfo>? GetPlayerById;

    public event Func<PlayerInfo, bool>? PlayerPressed;

    public virtual ValueTask DisposeAsync() => new();

    protected bool OnKeyPressed(GameKey key) => KeyPressed != null && KeyPressed(key);

    protected PlayerInfo? OnGetPlayerById(string playerId, bool strict) => GetPlayerById != null ? GetPlayerById(playerId, strict) : null;

    protected bool OnPlayerPressed(PlayerInfo player) => PlayerPressed != null && PlayerPressed(player);
}

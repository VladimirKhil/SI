namespace SICore.Clients.Game.Plugins.Stakes;

internal sealed class StakesPlugin
{
    private readonly GameData _gameData;
    private readonly StakesState _state;

    public StakesPlugin(GameData gameData)
    {
        _gameData = gameData;
        _state = gameData.Stakes;
    }

    internal void Reset(int initialStakerIndex) => _state.StakerIndex = initialStakerIndex;

    internal bool HandlePlayerDrop(int playerIndex)
    {
        if (_state.StakerIndex > playerIndex)
        {
            _state.StakerIndex--;
            return true;
        }

        if (_state.StakerIndex == playerIndex)
        {
            DropCurrentStaker();
            return true;
        }

        return false;
    }

    internal void DropCurrentStaker()
    {
        var stakerCount = _gameData.Players.Count(p => p.StakeMaking);

        if (stakerCount == 1)
        {
            for (var i = 0; i < _gameData.Players.Count; i++)
            {
                if (_gameData.Players[i].StakeMaking)
                {
                    _state.StakerIndex = i;
                    break;
                }
            }
        }
        else
        {
            _state.StakerIndex = -1;
        }
    }
}

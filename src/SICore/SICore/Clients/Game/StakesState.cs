namespace SICore.Clients.Game;

/// <summary>
/// Contains state regarding making stakes.
/// </summary>
internal sealed class StakesState
{
    private readonly List<GamePlayerAccount> _players;

    private int _stakerIndex = -1;

    /// <summary>
    /// Index of player making a stake.
    /// </summary>
    public int StakerIndex
    {
        get => _stakerIndex;
        set
        {
            if (value < -1 && value >= _players.Count)
            {
                throw new ArgumentException($"{nameof(value)} {value} must be greater or equal to -1 and less than {_players.Count}!");
            }

            _stakerIndex = value;
        }
    }

    public StakesState(List<GamePlayerAccount> players) => _players = players;

    internal void Reset(int initialStakerIndex)
    {
        StakerIndex = initialStakerIndex;
    }

    internal bool HandlePlayerDrop(int playerIndex)
    {
        if (StakerIndex > playerIndex)
        {
            StakerIndex--;
            return true;
        }
        
        if (StakerIndex == playerIndex)
        {
            DropCurrentStaker();
            return true;
        }

        return false;
    }

    internal void DropCurrentStaker()
    {
        var stakerCount = _players.Count(p => p.StakeMaking);

        if (stakerCount == 1)
        {
            for (var i = 0; i < _players.Count; i++)
            {
                if (_players[i].StakeMaking)
                {
                    StakerIndex = i;
                    break;
                }
            }
        }
        else
        {
            StakerIndex = -1;
        }
    }
}

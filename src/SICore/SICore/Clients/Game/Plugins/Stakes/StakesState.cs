namespace SICore.Clients.Game.Plugins.Stakes;

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
}

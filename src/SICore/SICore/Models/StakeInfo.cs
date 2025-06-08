using SIPackages.Core;

namespace SICore.Models;

public sealed class StakeInfo : NumberSet
{
    private int _stake = 0;

    public int Stake
    {
        get => _stake;
        set { _stake = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// Name of player making stake.
    /// </summary>
    public string? PlayerName { get; set; }

    /// <summary>
    /// Stake reason.
    /// </summary>
    public StakeReason Reason { get; internal set; }

    /// <summary>
    /// Allowed stake modes.
    /// </summary>
    public StakeModes Modes { get; internal set; }
}

using SIPackages.Core;

namespace SICore;

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
}

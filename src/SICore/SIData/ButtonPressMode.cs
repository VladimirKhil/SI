namespace SIData;

/// <summary>
/// Defines well-known button press handler modes.
/// </summary>
public enum ButtonPressMode
{
    /// <summary>
    /// Select winner randomly from all pressers withing an interval.
    /// </summary>
    RandomWithinInterval,

    /// <summary>
    /// First to press wins the button.
    /// </summary>
    FirstWins,

    /// <summary>
    /// Players with good ping get penalty.
    /// </summary>
    UsePingPenalty,
}

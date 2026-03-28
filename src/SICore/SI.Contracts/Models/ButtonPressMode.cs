namespace SI.Contracts;

/// <summary>
/// Defines well-known button press handler modes.
/// </summary>
public enum ButtonPressMode
{
    /// <summary>
    /// Select winner randomly from all pressers within an interval.
    /// </summary>
    RandomWithinInterval,

    /// <summary>
    /// First to press wins the button.
    /// </summary>
    FirstWins,

    /// <summary>
    /// First to press wins the button, with reaction calculated on the client side.
    /// </summary>
    FirstWinsClient,
}

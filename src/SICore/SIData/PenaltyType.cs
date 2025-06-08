namespace SIData;

/// <summary>
/// Defines types of penalties for giving wrong answers.
/// </summary>
public enum PenaltyType
{
    /// <summary>
    /// No penalty applied (question without risk).
    /// </summary>
    None = 0,

    /// <summary>
    /// Subtract points from the player for giving a wrong answer.
    /// </summary>
    SubtractPoints = 1,
}

namespace SICore;

/// <summary>
/// Defines player data.
/// </summary>
public sealed class PlayerData
{
    /// <summary>
    /// Продолжается ли чтение вопроса
    /// </summary>
    internal bool IsQuestionInProgress { get; set; }

    /// <summary>
    /// Defines time stamp when game buttons have been activated.
    /// </summary>
    public DateTimeOffset? TryStartTime { get; set; }
}

namespace SIEngine.QuestionSelectionStrategies;

/// <summary>
/// Defines a question selection strategy.
/// </summary>
internal interface ISelectionStrategy
{
    /// <summary>
    /// Should the current round be played (are there players and questions for it).
    /// </summary>
    bool ShouldPlayRound() => true;

    /// <summary>
    /// Can the game move forward.
    /// </summary>
    bool CanMoveNext() => false;

    /// <summary>
    /// Moves the game forward.
    /// </summary>
    void MoveNext() { }

    /// <summary>
    /// Can the game move backwards.
    /// </summary>
    bool CanMoveBack() => false;

    /// <summary>
    /// Moves the game backwards.
    /// </summary>
    (int themeIndex, int questionIndex) MoveBack() => (-1, -1);
}

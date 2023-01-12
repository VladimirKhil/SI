namespace SICore;

/// <summary>
/// Defines a showman behavior.
/// </summary>
public interface IShowmanLogic : IPersonLogic
{
    /// <summary>
    /// Selects person to select the question.
    /// </summary>
    void StarterChoose();

    /// <summary>
    /// Selects next person to make a stake.
    /// </summary>
    void FirstStake();

    /// <summary>
    /// Validates the answer.
    /// </summary>
    void IsRight();

    /// <summary>
    /// Selects next person to delete a theme.
    /// </summary>
    void FirstDelete();

    /// <summary>
    /// Clears all possible selection options.
    /// </summary>
    /// <param name="full">Should game table selection be cleared too.</param>
    void ClearSelections(bool full = false);

    /// <summary>
    /// Toggles game table edit mode.
    /// </summary>
    /// <param name="mode">Should the edit be enabled. If null, edit mode is reversed.</param>
    void ManageTable(bool? mode = null) { }
}

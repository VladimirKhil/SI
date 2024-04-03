using SIPackages;

namespace SIEngine;

/// <summary>
/// Handles SIEngine play events.
/// </summary>
public interface ISIEnginePlayHandler
{
    /// <summary>
    /// Detects whether current game situation supports playing question for all (there is at least one player capable for that).
    /// </summary>
    bool ShouldPlayQuestionForAll();

    /// <summary>
    /// Handles round themes list.
    /// </summary>
    /// <param name="themes">Round themes.</param>
    /// <param name="tableController">Round table controller.</param>
    void OnRoundThemes(IReadOnlyList<Theme> themes, IRoundTableController tableController);

    /// <summary>
    /// Handles question selection request.
    /// </summary>
    /// <param name="options">Possible selection options.</param>
    /// <param name="selectCallback">Selection callbacks.</param>
    void AskForQuestionSelection(IReadOnlyCollection<(int, int)> options, Action<int, int> selectCallback);

    /// <summary>
    /// Cancels question selection request.
    /// </summary>
    void CancelQuestionSelection();

    /// <summary>
    /// Handles question selection.
    /// </summary>
    /// <remarks>
    /// This handler is required because a question can be selected not only by a callback but also by the engine internally.
    /// </remarks>
    /// <param name="themeIndex">Question theme index.</param>
    /// <param name="questionIndex">Question index.</param>
    void OnQuestionSelected(int themeIndex, int questionIndex);
}

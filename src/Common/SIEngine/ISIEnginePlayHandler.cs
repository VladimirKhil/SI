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
    /// Handles final round themes list. 
    /// </summary>
    /// <param name="themes">Round themes.</param>
    /// <param name="willPlayAllThemes">Will all the themes be played.</param>
    /// <param name="isFirstPlay">Is this the first theme in this round.</param>
    /// <remarks>Could be called multiple times per round.</remarks>
    void OnFinalThemes(IReadOnlyList<Theme> themes, bool willPlayAllThemes, bool isFirstPlay);

    /// <summary>
    /// Handles question selection request.
    /// </summary>
    /// <param name="options">Possible selection options.</param>
    /// <param name="selectCallback">Selection callbacks.</param>
    void AskForQuestionSelection(IReadOnlyCollection<(int, int)> options, Action<int, int> selectCallback);

    /// <summary>
    /// Handles question selection.
    /// </summary>
    /// <remarks>
    /// This handler is required because a question can be selected not only by a callback but also by the engine internally.
    /// </remarks>
    /// <param name="themeIndex">Question theme index.</param>
    /// <param name="questionIndex">Question index.</param>
    void OnQuestionSelected(int themeIndex, int questionIndex);

    /// <summary>
    /// Handles theme deletion request.
    /// </summary>
    /// <param name="deleteCallback">Deletion callback.</param>
    void AskForThemeDelete(Action<int> deleteCallback);

    /// <summary>
    /// Handles theme deletion.
    /// </summary>
    /// <param name="themeIndex">Index of theme being deleted.</param>
    void OnThemeDeleted(int themeIndex);

    /// <summary>
    /// Handles final theme selection (the theme that is left).
    /// </summary>
    /// <param name="themeIndex">Selected theme index.</param>
    void OnThemeSelected(int themeIndex);

    /// <summary>
    /// Handles theme play start.
    /// </summary>
    /// <param name="theme">Theme to play.</param>
    void OnTheme(Theme theme);

    /// <summary>
    /// Handles question play start.
    /// </summary>
    /// <param name="question">Question to play.</param>
    void OnQuestion(Question question);
}

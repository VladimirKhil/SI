namespace SIEngine;

/// <summary>
/// Defines SI engine states.
/// </summary>
public enum GameStage
{
    /// <summary>
    /// Initial stage.
    /// </summary>
    Begin,

    /// <summary>
    /// Showing all game themes.
    /// </summary>
    GameThemes,

    /// <summary>
    /// Starting round.
    /// </summary>
    Round,

    /// <summary>
    /// Question to play selection.
    /// </summary>
    SelectingQuestion,

    /// <summary>
    /// Question type is announced.
    /// </summary>
    QuestionType,
    
    /// <summary>
    /// Question is played.
    /// </summary>
    Question,

    /// <summary>
    /// Final stage.
    /// </summary>
    EndGame,

    /// <summary>
    /// Empty stage.
    /// </summary>
    None,
}
namespace SIEngine;

/// <summary>
/// Defines SI engine states.
/// </summary>
public enum GameStage
{
    Begin,
    GameThemes,
    Round,

    /// <summary>
    /// Question to play selection.
    /// </summary>
    SelectingQuestion,

    Theme,
    NextQuestion,
    Score, // ?
    Special,
    Question,
    EndQuestion,
    FinalThemes,
    WaitDelete,
    AfterDelete,
    FinalQuestion,
    AfterFinalThink,
    End
}
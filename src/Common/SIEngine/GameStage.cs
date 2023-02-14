namespace SIEngine;

// TODO: internal

/// <summary>
/// Defines SI engine states.
/// </summary>
public enum GameStage
{
    Begin,
    GameThemes,
    Round,
    RoundThemes,
    RoundTable,
    Theme,
    NextQuestion,
    Score, // ?
    Special,
    Question,
    RightAnswer,
    RightAnswerProceed,
    QuestionPostInfo,
    EndQuestion,
    FinalThemes,
    WaitDelete,
    AfterDelete,
    FinalQuestion,
    /// <summary>
    /// Thinking in final round.
    /// </summary>
    FinalThink,
    RightFinalAnswer,
    AfterFinalThink,
    End
}
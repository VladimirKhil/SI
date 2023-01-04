namespace SIData;

/// <summary>
/// Defines well-known game stages.
/// </summary>
public enum GameStage
{
    /// <summary>
    /// Before game start.
    /// </summary>
    Before,

    /// <summary>
    /// Game start.
    /// </summary>
    Begin,

    /// <summary>
    /// Standard round.
    /// </summary>
    Round,

    /// <summary>
    /// Final round.
    /// </summary>
    Final,

    /// <summary>
    /// After game finish.
    /// </summary>
    After
}

public enum TimeSettingsTypes
{
    ChoosingQuestion,
    ThinkingOnQuestion,
    PrintingAnswer,
    GivingCat,
    MakingStake,
    ThinkingOnSpecial,
    Round,
    ChoosingFinalTheme,
    FinalThinking,
    ShowmanDecisions,
    RightAnswer,
    MediaDelay
}

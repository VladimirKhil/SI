namespace SIData
{
    /// <summary>
    /// Состояние игры
    /// </summary>
    public enum GameStage
    {
        /// <summary>
        /// Перед игрой
        /// </summary>
        Before,
        /// <summary>
        /// Начало игры
        /// </summary>
        Begin,
        /// <summary>
        /// Во время раунда
        /// </summary>
        Round,
        /// <summary>
        /// Финал
        /// </summary>
        Final,
        /// <summary>
        /// После игры
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
}

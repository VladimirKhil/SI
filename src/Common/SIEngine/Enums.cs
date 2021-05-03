using System.ComponentModel.DataAnnotations;

namespace SIEngine
{
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
        /// Размышление в финале
        /// </summary>
        FinalThink,
        RightFinalAnswer,
        AfterFinalThink,
        End
    }

    public enum QuestionPlayMode
    {
        InProcess,
        JustFinished,
        AlreadyFinished
    }

    /// <summary>
    /// Тип игры
    /// </summary>
    public enum GameModes
    {
        /// <summary>
        /// Классическая
        /// </summary>
        [Display(Description = "GameModes_Tv")]
        Tv,
        /// <summary>
        /// Упрощённая
        /// </summary>
        [Display(Description = "GameModes_Sport")]
        Sport
    }
}

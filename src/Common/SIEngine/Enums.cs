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
        /// Thinking in final round.
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
    /// Defines game modes.
    /// </summary>
    public enum GameModes
    {
        /// <summary>
        /// Classic mode (with final round and special questions support).
        /// </summary>
        [Display(Description = "GameModes_Tv")]
        Tv,
        /// <summary>
        /// Simplified mode (without final round and special questions).
        /// </summary>
        [Display(Description = "GameModes_Sport")]
        Sport
    }
}

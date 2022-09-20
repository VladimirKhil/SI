namespace SIEngine.Core
{
    /// <summary>
    /// Defines <see cref="QuestionProcessor" /> states.
    /// </summary>
    internal enum QuestionProcessorStates
    {
        /// <summary>
        /// Final state.
        /// </summary>
        None,

        /// <summary>
        /// Question state.
        /// </summary>
        Question,

        /// <summary>
        /// Asking answer state.
        /// </summary>
        AskAnswer,

        /// <summary>
        /// Answer state.
        /// </summary>
        Answer,
    }
}

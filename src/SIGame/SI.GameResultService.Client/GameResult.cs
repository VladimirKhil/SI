namespace SI.GameResultService.Client
{
    /// <summary>
    /// Defines a game result.
    /// </summary>
    public sealed class GameResult
    {
        /// <summary>
        /// Game package name.
        /// </summary>
        public string PackageName { get; set; }

        /// <summary>
        /// Game package unique id.
        /// </summary>
        public string PackageID { get; set; }

        /// <summary>
        /// Player results.
        /// </summary>
        public List<PersonResult> Results { get; set; } = new List<PersonResult>();

        /// <summary>
        /// Apellated right answers.
        /// </summary>
        public List<AnswerInfo> ApellatedQuestions { get; set; } = new List<AnswerInfo>();

        /// <summary>
        /// Apellated wrong answers.
        /// </summary>
        public List<AnswerInfo> WrongVersions { get; set; } = new List<AnswerInfo>();

        /// <summary>
        /// Marked questions.
        /// </summary>
        public List<AnswerInfo> MarkedQuestions { get; set; } = new List<AnswerInfo>();

        /// <summary>
        /// Error log.
        /// </summary>
        public string ErrorLog { get; set; } = "";

        /// <summary>
        /// Players comments.
        /// </summary>
        public string Comments { get; set; } = "";
    }
}

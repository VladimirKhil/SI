namespace SI.GameResultService.Client
{
    /// <summary>
    /// Defines a game answer.
    /// </summary>
    public sealed class AnswerInfo
    {
        public int Round { get; set; }
        public int Theme { get; set; }
        public int Question { get; set; }

        public string Answer { get; set; }
    }
}

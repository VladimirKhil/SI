namespace SI.GameResultService.Client
{
    /// <summary>
    /// Defines a game player result.
    /// </summary>
    public sealed class PersonResult
    {
        /// <summary>
        /// Player name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Player score.
        /// </summary>
        public int Sum { get; set; }
    }
}

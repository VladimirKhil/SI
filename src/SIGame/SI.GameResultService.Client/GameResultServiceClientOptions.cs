namespace SI.GameResultService.Client
{
    /// <summary>
    /// Provides options for game result service client.
    /// </summary>
    public sealed class GameResultServiceClientOptions
    {
        public const string ConfigurationSectionName = "GameResultServiceClient";

        /// <summary>
        /// GameResultService address.
        /// </summary>
        public Uri? ServiceUri { get; set; }
    }
}

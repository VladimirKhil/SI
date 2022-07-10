namespace SI.GameResultService.Client
{
    /// <summary>
    /// Allows to save game result to server.
    /// </summary>
    public interface IGameResultServiceClient
    {
        /// <summary>
        /// Sends game result to server.
        /// </summary>
        /// <param name="gameResult">Result contents.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task SendGameReportAsync(GameResult gameResult, CancellationToken cancellationToken = default);
    }
}

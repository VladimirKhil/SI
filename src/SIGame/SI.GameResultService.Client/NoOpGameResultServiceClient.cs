namespace SI.GameResultService.Client
{
    /// <summary>
    /// Provides a no-op implementation for <see cref="IGameResultServiceClient" />.
    /// It is used when AppSerive Uri has not been specified.
    /// </summary>
    /// <inheritdoc />
    internal sealed class NoOpGameResultServiceClient : IGameResultServiceClient
    {
        public Task SendGameReportAsync(GameResult gameResult, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}

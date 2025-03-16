namespace SI.GameServer.Client.Discovery;

/// <summary>
/// Allows to detect game server Uri.
/// </summary>
internal interface IGameServerLocator
{
    /// <summary>
    /// Gets all available game servers info.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<ServerInfo[]> GetServerInfoAsync(CancellationToken cancellationToken = default);
}

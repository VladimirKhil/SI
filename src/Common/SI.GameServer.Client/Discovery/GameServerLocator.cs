using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace SI.GameServer.Client.Discovery;

/// <inheritdoc cref="IGameServerLocator" />
internal sealed class GameServerLocator : IGameServerLocator
{
    private readonly HttpClient _client;

    public GameServerLocator(HttpClient client) => _client = client;

    public async Task<ServerInfo[]> GetServerInfoAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var serverInfos = await _client.GetFromJsonAsync<ServerInfo[]>("", cancellationToken);
            return serverInfos ?? Array.Empty<ServerInfo>();
        }
        catch (SocketException exc)
        {
            throw new Exception($"GameServerLocator exception: {exc.ErrorCode}", exc);
        }
    }
}

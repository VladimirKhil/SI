using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace SI.GameServer.Client.Discovery
{
    /// <inheritdoc cref="IGameServerLocator" />
    internal sealed class GameServerLocator : IGameServerLocator
    {
        private static readonly JsonSerializer Serializer = new();

        private readonly HttpClient _client;

        public GameServerLocator(HttpClient client)
        {
            _client = client;
        }

        public async Task<ServerInfo[]> GetServerInfoAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                using var responseMessage = await _client.GetAsync("", cancellationToken);

                if (!responseMessage.IsSuccessStatusCode)
                {
                    throw new Exception(
                        $"GetServerInfoAsync error ({responseMessage.StatusCode}): " +
                        $"{await responseMessage.Content.ReadAsStringAsync(cancellationToken)}");
                }

                using var responseStream = await responseMessage.Content.ReadAsStreamAsync(cancellationToken);
                using var reader = new StreamReader(responseStream);

                return (ServerInfo[]?)Serializer.Deserialize(reader, typeof(ServerInfo[])) ?? Array.Empty<ServerInfo>();
            }
            catch (SocketException exc)
            {
                throw new Exception($"GameServerLocator exception: {exc.ErrorCode}", exc);
            }
        }
    }
}

using SI.GameServer.Contract;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SI.GameServer.Client;

internal sealed class InfoApi : IInfoApi
{
    private readonly HttpClient _client;

    public InfoApi(HttpClient client) => _client = client;

    public Task<HostInfo?> GetHostInfoAsync(CancellationToken cancellationToken = default) =>
        _client.GetFromJsonAsync<HostInfo>("api/v1/info/host", cancellationToken);

    public Task<string[]?> GetBotsNamesAsync(CancellationToken cancellationToken = default) =>
        _client.GetFromJsonAsync<string[]>("api/v1/info/bots", cancellationToken);
}

using SI.GameServer.Contract;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SI.GameServer.Client;

internal sealed class GamesApi : IGamesApi
{
    private readonly HttpClient _client;

    public GamesApi(HttpClient client) => _client = client;

    public Task<GetGameByPinResponse?> GetGameByPinAsync(int pin, CancellationToken cancellationToken = default) =>
        _client.GetFromJsonAsync<GetGameByPinResponse>($"/api/v1/games?pin={pin}", cancellationToken);

    public async Task<RunGameResponse?> RunGameAsync(RunGameRequest runGameRequest, CancellationToken cancellationToken = default)
    {
        var response = await _client.PostAsJsonAsync("/api/v1/games", runGameRequest, cancellationToken);
        return await response.Content.ReadFromJsonAsync<RunGameResponse>(cancellationToken: cancellationToken);
    }
}

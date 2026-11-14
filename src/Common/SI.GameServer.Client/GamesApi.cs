using SI.GameServer.Contract;
using System.Net.Http.Json;

namespace SI.GameServer.Client;

internal sealed class GamesApi(HttpClient client) : IGamesApi
{
    public Task<GetGameByPinResponse?> GetGameByPinAsync(int pin, CancellationToken cancellationToken = default) =>
        client.GetFromJsonAsync<GetGameByPinResponse>($"api/v1/games?pin={pin}", cancellationToken);

    public async Task<RunGameResponse> RunGameAsync(RunGameRequest runGameRequest, CancellationToken cancellationToken = default)
    {
        var response = await client.PostAsJsonAsync("api/v1/games", runGameRequest, cancellationToken);
        
        return await response.Content.ReadFromJsonAsync<RunGameResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Unknown run game error");
    }

    public async Task<RunGameResponse2> RunGame2Async(RunGameRequest2 runGameRequest, CancellationToken cancellationToken = default)
    {
        var response = await client.PostAsJsonAsync("api/v2/games", runGameRequest, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<RunGameResponse2>(cancellationToken: cancellationToken)
                ?? throw new InvalidOperationException("Unknown run game error");
        }

        var error = await response.Content.ReadFromJsonAsync<RunGameError2>(cancellationToken: cancellationToken);

        throw error != null
            ? new GameCreationException(error.Code)
            : new GameCreationException(GameCreationResultCode.UnknownError);
    }
}

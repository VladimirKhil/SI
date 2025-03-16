using SI.GameServer.Contract;

namespace SI.GameServer.Client;

public interface IGamesApi
{
    Task<GetGameByPinResponse?> GetGameByPinAsync(int pin, CancellationToken cancellationToken = default);

    Task<RunGameResponse> RunGameAsync(RunGameRequest runGameRequest, CancellationToken cancellationToken = default);
}

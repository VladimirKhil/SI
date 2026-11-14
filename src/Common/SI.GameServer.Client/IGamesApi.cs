using SI.GameServer.Contract;

namespace SI.GameServer.Client;

public interface IGamesApi
{
    Task<GetGameByPinResponse?> GetGameByPinAsync(int pin, CancellationToken cancellationToken = default);

    Task<RunGameResponse> RunGameAsync(RunGameRequest runGameRequest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs a new game.
    /// </summary>
    /// <exception cref="GameCreationException">Thrown when the server rejects the request.</exception>
    Task<RunGameResponse2> RunGame2Async(RunGameRequest2 runGameRequest, CancellationToken cancellationToken = default);
}

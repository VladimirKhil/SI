using SI.GameServer.Contract;
using System.Threading.Tasks;
using System.Threading;

namespace SI.GameServer.Client;

public interface IGamesApi
{
    Task<GetGameByPinResponse?> GetGameByPinAsync(int pin, CancellationToken cancellationToken = default);

    Task<RunGameResponse?> RunGameAsync(RunGameRequest runGameRequest, CancellationToken cancellationToken = default);
}

using SI.GameServer.Contract;
using System.Threading.Tasks;
using System.Threading;

namespace SI.GameServer.Client;

public interface IInfoApi
{
    Task<HostInfo?> GetHostInfoAsync(CancellationToken cancellationToken = default);

    Task<string[]?> GetBotsNamesAsync(CancellationToken cancellationToken = default);

    Task<GetGameByPinResponse?> GetGameByPinAsync(int pin, CancellationToken cancellationToken = default);
}

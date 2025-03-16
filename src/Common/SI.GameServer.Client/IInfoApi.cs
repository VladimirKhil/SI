using SI.GameServer.Contract;

namespace SI.GameServer.Client;

public interface IInfoApi
{
    Task<HostInfo?> GetHostInfoAsync(CancellationToken cancellationToken = default);

    Task<string[]?> GetBotsNamesAsync(CancellationToken cancellationToken = default);
}

using System.Threading;
using System.Threading.Tasks;

namespace SI.GameServer.Client
{
    /// <summary>
    /// Allows to create <see cref="IGameServerClient" />.
    /// </summary>
    public interface IGameServerClientFactory
    {
        /// <summary>
        /// Creates a game server client.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<IGameServerClient> CreateClientAsync(CancellationToken cancellationToken);
    }
}

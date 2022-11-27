using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SI.GameServer.Client.Discovery;
using SI.GameServer.Client.Properties;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SI.GameServer.Client
{
    /// <inheritdoc cref="IGameServerClientFactory" />
    internal sealed class GameServerClientFactory : IGameServerClientFactory
    {
        /// <summary>
        /// Currently supported client version.
        /// </summary>
        private const int ClientProtocolVersion = 1;

        private readonly IOptions<GameServerClientOptions> _options;
        private readonly IUIThreadExecutor _uIThreadExecutor;
        private readonly IServiceProvider _serviceProvider;

        public GameServerClientFactory(
            IOptions<GameServerClientOptions> options,
            IUIThreadExecutor uIThreadExecutor,
            IServiceProvider serviceProvider)
        {
            _options = options;
            _uIThreadExecutor = uIThreadExecutor;
            _serviceProvider = serviceProvider;
        }

        public async Task<IGameServerClient> CreateClientAsync(CancellationToken cancellationToken = default)
        {
            var options = _options;

            if (string.IsNullOrEmpty(_options.Value.ServiceUri))
            {
                // Server Uri is undefined. We should locate it
                var serverUri = await LocateServerUriAsync(cancellationToken);

                options = new OptionsWrapper<GameServerClientOptions>(
                    new GameServerClientOptions
                    {
                        ServiceUri = serverUri + (serverUri.EndsWith("/") ? "" : "/"),
                        Timeout = _options.Value.Timeout
                    });
            }

            return new GameServerClient(options, _uIThreadExecutor);
        }

        /// <summary>
        /// Locates server Uri.
        /// </summary>
        /// <remarks>The result is not cached because server configuration may change at any time.</remarks>
        /// <param name="cancellationToken">Cancellation token.</param>
        private async Task<string> LocateServerUriAsync(CancellationToken cancellationToken)
        {
            var locator = _serviceProvider.GetRequiredService<IGameServerLocator>();

            ServerInfo[] serverInfos;

            try
            {
                serverInfos = await locator.GetServerInfoAsync(cancellationToken);
            }
            catch (Exception exc)
            {
                throw new Exception($"{Resources.CannotGetServerAddress} {exc.Message}");
            }

            if (serverInfos.Length == 0)
            {
                throw new Exception(Resources.EmptyServerList);
            }

            var acceptableByVersion = serverInfos.Where(info => info.ProtocolVersion <= ClientProtocolVersion).ToList();

            if (!acceptableByVersion.Any())
            {
                throw new Exception(Resources.ObsoleClient);
            }

            var serverInfo = acceptableByVersion.FirstOrDefault(info => info.Uri != null);

            if (serverInfo == null)
            {
                throw new Exception(Resources.EmptyServerUri);
            }

            return serverInfo.Uri!;
        }
    }
}

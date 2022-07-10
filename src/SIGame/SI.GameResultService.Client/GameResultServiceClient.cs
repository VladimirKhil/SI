using Newtonsoft.Json;
using System.Text;

namespace SI.GameResultService.Client
{
    /// <inheritdoc cref="IGameResultServiceClient" />
    internal sealed class GameResultServiceClient : IGameResultServiceClient
    {
        private static readonly JsonSerializer Serializer = new();
        private readonly HttpClient _client;

        public GameResultServiceClient(HttpClient client)
        {
            _client = client;
        }

        public async Task SendGameReportAsync(GameResult gameResult, CancellationToken cancellationToken = default)
        {
            var sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
            {
                Serializer.Serialize(writer, gameResult);
            }

            var content = new StringContent(sb.ToString(), Encoding.UTF8, "application/json");

            var responseMessage = await _client.PostAsync("GameReport", content, cancellationToken);

            if (!responseMessage.IsSuccessStatusCode)
            {
                throw new Exception(
                    $"SendGameReportAsync error ({responseMessage.StatusCode}): " +
                    $"{await responseMessage.Content.ReadAsStringAsync(cancellationToken)}");
            }
        }
    }
}

using AppService.Client.Models;
using Newtonsoft.Json;
using System.Reflection;
using System.Text;

namespace AppService.Client
{
    /// <inheritdoc cref="IAppServiceClient" />
    public sealed class AppServiceClient : IAppServiceClient
    {
        private static readonly JsonSerializer Serializer = new();

        private readonly HttpClient _client;

        public AppServiceClient(HttpClient client)
        {
            _client = client;
        }

        public async Task<AppInfo?> GetProductAsync(string name, CancellationToken cancellationToken = default)
        {
            var appInfo = await CallAsync<AppInfo>("Product?name=" + name + "&osVersion=" + Environment.OSVersion.Version, cancellationToken);
            
            if (appInfo == null)
            {
                throw new Exception($"Product {name} not found");
            }
            
            if (!appInfo.Uri.IsAbsoluteUri)
            {
                appInfo.Uri = new Uri("https://vladimirkhil.com/" + appInfo.Uri.OriginalString);
            }

            return appInfo;
        }

        public async Task<ErrorStatus?> SendErrorReportAsync(
            string application,
            string errorMessage,
            Version? appVersion = null,
            DateTime? time = null,
            CancellationToken cancellationToken = default)
        {
            var version = appVersion ?? Assembly.GetEntryAssembly()?.GetName().Version;

            var errorInfo = new ErrorInfo
            {
                Application = application,
                Error = errorMessage,
                Version = version?.ToString() ?? "",
                Time = time ?? DateTime.UtcNow,
                OSVersion = Environment.OSVersion.Version.ToString()
            };

            var sb = new StringBuilder();

            using (var writer = new StringWriter(sb))
            {
                Serializer.Serialize(writer, errorInfo);
            }

            var content = new StringContent(sb.ToString(), Encoding.UTF8, "application/json");

            using var response = await _client.PostAsync("SendErrorReport2", content, cancellationToken);
            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);

            return (ErrorStatus?)Serializer.Deserialize(reader, typeof(ErrorStatus));
        }

        private async Task<T?> CallAsync<T>(string request, CancellationToken cancellationToken = default)
        {
            using var stream = await _client.GetStreamAsync(request, cancellationToken);
            using var reader = new StreamReader(stream);

            return (T?)Serializer.Deserialize(reader, typeof(T));
        }

        public void Dispose() => _client.Dispose();
    }
}

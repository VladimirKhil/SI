using AppService.Client.Models;
using Newtonsoft.Json;
using System.Reflection;
using System.Text;

namespace AppService.Client
{
    public sealed class AppServiceClient
    {
        private static readonly JsonSerializer Serializer = new();
        private static readonly HttpClient Client = new();

        private readonly string _address;

        public AppServiceClient(string address)
        {
            _address = address;
        }

        public async Task<AppInfo> GetProductAsync(string name)
        {
            var appInfo = await CallAsync<AppInfo>("Product?name=" + name + "&osVersion=" + Environment.OSVersion.Version);
            
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
            DateTime? time = null)
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

            using var response = await Client.PostAsync(_address + "/SendErrorReport2", content);
            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            return (ErrorStatus?)Serializer.Deserialize(reader, typeof(ErrorStatus));
        }

        private async Task<T?> CallAsync<T>(string request)
        {
            using var stream = await Client.GetStreamAsync(_address + "/" + request);
            using var reader = new StreamReader(stream);

            return (T?)Serializer.Deserialize(reader, typeof(T));
        }
    }
}

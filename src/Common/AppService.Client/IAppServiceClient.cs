using AppService.Client.Models;

namespace AppService.Client
{
    public interface IAppServiceClient : IDisposable
    {
        Task<AppInfo> GetProductAsync(string name);

        Task<ErrorStatus?> SendErrorReportAsync(
            string application,
            string errorMessage,
            Version? appVersion = null,
            DateTime? time = null);
    }
}

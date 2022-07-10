using AppService.Client.Models;

namespace AppService.Client
{
    /// <summary>
    /// Provides a no-op implementation for <see cref="IAppServiceClient" />.
    /// It is used when AppSerive Uri has not been specified.
    /// </summary>
    /// <inheritdoc />
    internal sealed class NoOpAppServiceClient : IAppServiceClient
    {
        public Task<AppInfo?> GetProductAsync(string name) => Task.FromResult<AppInfo?>(null);

        public Task<ErrorStatus?> SendErrorReportAsync(string application, string errorMessage, Version? appVersion = null, DateTime? time = null) =>
            Task.FromResult<ErrorStatus?>(ErrorStatus.NotFixed);
        public void Dispose() { }
    }
}

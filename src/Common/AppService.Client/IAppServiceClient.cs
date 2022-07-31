using AppService.Client.Models;

namespace AppService.Client
{
    /// <summary>
    /// Provides API for working with published products.
    /// </summary>
    public interface IAppServiceClient : IDisposable
    {
        /// <summary>
        /// Gets product info.
        /// </summary>
        /// <param name="name">Product name.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Product info.</returns>
        Task<AppInfo?> GetProductAsync(string name, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends product execution error information.
        /// </summary>
        /// <param name="application">Application name.</param>
        /// <param name="errorMessage">Error message.</param>
        /// <param name="appVersion">Application version.</param>
        /// <param name="time">Error time.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Current status of this error.</returns>
        Task<ErrorStatus?> SendErrorReportAsync(
            string application,
            string errorMessage,
            Version? appVersion = null,
            DateTime? time = null,
            CancellationToken cancellationToken = default);
    }
}

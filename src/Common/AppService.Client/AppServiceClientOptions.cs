namespace AppService.Client
{
    /// <summary>
    /// Provides options for <see cref="AppServiceClient" />.
    /// </summary>
    public sealed class AppServiceClientOptions
    {
        public const string ConfigurationSectionName = "AppServiceClient";

        /// <summary>
        /// AppService address.
        /// </summary>
        public Uri? ServiceUri { get; set; }
    }
}

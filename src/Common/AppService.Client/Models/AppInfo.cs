namespace AppService.Client.Models
{
    /// <summary>
    /// Defines an application info.
    /// </summary>
    public sealed class AppInfo
    {
        /// <summary>
        /// Current application version.
        /// </summary>
        public Version Version { get; set; }

        /// <summary>
        /// Application setup uri.
        /// </summary>
        public Uri Uri { get; set; }
    }
}

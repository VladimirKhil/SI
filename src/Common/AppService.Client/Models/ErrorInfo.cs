namespace AppService.Client.Models
{
    /// <summary>
    /// Contains information about application error.
    /// </summary>
    public sealed class ErrorInfo
    {
        /// <summary>
        /// Application name.
        /// </summary>
        public string Application { get; set; }

        /// <summary>
        /// Application version.
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Error time.
        /// </summary>
        public DateTime Time { get; set; }

        /// <summary>
        /// Error text.
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        /// Operation system version.
        /// </summary>
        public string OSVersion { get; set; }
    }
}

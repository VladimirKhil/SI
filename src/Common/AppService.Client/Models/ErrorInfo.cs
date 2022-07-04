namespace AppService.Client.Models
{
    public sealed class ErrorInfo
    {
        public string Application { get; set; }
        public string Version { get; set; }
        public DateTime Time { get; set; }
        public string Error { get; set; }
        public string OSVersion { get; set; }
    }
}

namespace SIQuester.Model;

public sealed class ErrorInfo
{
    public Version Version { get; set; }
    public DateTime Time { get; set; }
    public string Error { get; set; }
}

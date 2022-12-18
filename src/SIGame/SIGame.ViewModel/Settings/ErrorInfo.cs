namespace SIGame;

public sealed class ErrorInfo
{
    public string Version { get; set; }
    public DateTime Time { get; set; }
    public string Error { get; set; }
}

public sealed class ErrorInfoList : List<ErrorInfo> { }

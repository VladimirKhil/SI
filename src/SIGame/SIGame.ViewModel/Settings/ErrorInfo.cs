namespace SIGame;

/// <summary>
/// Defines an error saved for sending to AppRegistry service.
/// </summary>
public sealed class ErrorInfo
{
    public string? Version { get; set; }

    public DateTime Time { get; set; }

    public string? Error { get; set; }

    public string? UserNotes { get; set; }
}

public sealed class ErrorInfoList : List<ErrorInfo> { }

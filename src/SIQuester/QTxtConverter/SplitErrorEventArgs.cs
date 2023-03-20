namespace QTxtConverter;

/// <summary>
/// Represents split error event arguments.
/// </summary>
public sealed class SplitErrorEventArgs : EventArgs
{
    /// <summary>
    /// Should import be cancelled.
    /// </summary>
    public bool Cancel { get; set; } = false;

    /// <summary>
    /// Skip current problematic position.
    /// </summary>
    public bool Skip { get; set; }

    /// <summary>
    /// Problematic position index.
    /// </summary>
    public int SourcePosition { get; set; }

    /// <summary>
    /// Split source.
    /// </summary>
    public string Source { get; set; }

    public SplitErrorEventArgs(string source) => Source = source;
}

namespace QTxtConverter;

/// <summary>
/// Ошибка разбора входного текста на вопросы
/// </summary>
public sealed class ParseErrorEventArgs: EventArgs
{
    /// <summary>
    /// Отменить распознавание?
    /// </summary>
    public bool Cancel { get; set; }

    public bool Skip { get; set; }

    /// <summary>
    /// Позиция в источнике, в которой не найдена следующая тема
    /// </summary>
    public int SourcePosition { get; set; }

    public string Source { get; set; }

    public ParseErrorEventArgs()
    {
        Cancel = false;
    }
}

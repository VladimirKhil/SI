namespace QTxtConverter;

/// <summary>
/// Вопрос из текстового файла
/// </summary>
public sealed class SIPart
{
    /// <summary>
    /// Позиция вопроса в тексте
    /// </summary>
    [Obsolete]
    public int Index { get; set; }

    /// <summary>
    /// Текст вопроса
    /// </summary>
    public string Value { get; set; }

    public override string ToString() => Value;
}

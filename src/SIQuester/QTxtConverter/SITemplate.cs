namespace QTxtConverter;

/// <summary>
/// Шаблоны для распознавателя СИ
/// </summary>
public sealed class SITemplate
{
    /// <summary>
    /// Использовать стандартную логику
    /// </summary>
    public bool StandartLogic { get; internal set; }

    /// <summary>
    /// Мультипликатор стоимостей вопросов
    /// </summary>
    public int Multiplier { get; internal set; }

    /// <summary>
    /// Шаблон пакета
    /// </summary>
    public List<string> PackageTemplate { get; internal set; }

    /// <summary>
    /// Шаблон раунда
    /// </summary>
    public List<string> RoundTemplate { get; internal set; }

    /// <summary>
    /// Шаблон темы
    /// </summary>
    public List<string> ThemeTemplate { get; internal set; }

    /// <summary>
    /// Шаблон вопроса
    /// </summary>
    public List<string> QuestionTemplate { get; internal set; }

    /// <summary>
    /// Шаблон ответа (только для нестандартной логики распознавания)
    /// </summary>
    public List<string> AnswerTemplate { get; internal set; }

    /// <summary>
    /// Шаблон разделителя группы вопросов и ответов (только для нестандартной логики распознавания)
    /// </summary>
    public List<string> SeparatorTemplate { get; internal set; }
}

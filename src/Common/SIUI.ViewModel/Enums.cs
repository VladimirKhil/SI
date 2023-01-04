using System.Runtime.Serialization;

namespace SIUI.ViewModel;

/// <summary>
/// Состояние табло
/// </summary>
[DataContract]
public enum TableStage
{
    /// <summary>
    /// Пустое
    /// </summary>
    [EnumMember]
    Void,
    /// <summary>
    /// Эмблема игры
    /// </summary>
    [EnumMember]
    Sign,
    /// <summary>
    /// Темы игры
    /// </summary>
    [EnumMember]
    GameThemes,
    /// <summary>
    /// Раунд
    /// </summary>
    [EnumMember]
    Round,
    /// <summary>
    /// Темы раунда
    /// </summary>
    [EnumMember]
    RoundThemes,
    /// <summary>
    /// Игровое табло раунда
    /// </summary>
    [EnumMember]
    RoundTable,
    /// <summary>
    /// Тема
    /// </summary>
    [EnumMember]
    Theme,
    /// <summary>
    /// Цена вопроса
    /// </summary>
    [EnumMember]
    QuestionPrice,
    /// <summary>
    /// Текст вопроса
    /// </summary>
    [EnumMember]
    Question,
    /// <summary>
    /// Правильный ответ
    /// </summary>
    [EnumMember]
    Answer,
    /// <summary>
    /// Спецвопрос
    /// </summary>
    [EnumMember]
    Special,
    /// <summary>
    /// Список финальных тем
    /// </summary>
    [EnumMember]
    Final,
    /// <summary>
    /// Счёт
    /// </summary>
    [EnumMember]
    Score
}

/// <summary>
/// Defines well-known content types displayed on the table.
/// </summary>
[DataContract]
public enum QuestionContentType
{
    /// <summary>
    /// Empty content.
    /// </summary>
    [EnumMember]
    None,

    /// <summary>
    /// Text content.
    /// </summary>
    [EnumMember]
    Text,

    /// <summary>
    /// Image content.
    /// </summary>
    [EnumMember]
    Image,

    /// <summary>
    /// Video content.
    /// </summary>
    [EnumMember]
    Video,

    /// <summary>
    /// Special text content.
    /// </summary>
    [EnumMember]
    SpecialText,

    /// <summary>
    /// Html content.
    /// </summary>
    [EnumMember]
    Html,

    /// <summary>
    /// Loading content mode.
    /// </summary>
    [EnumMember]
    Loading
}

/// <summary>
/// Стиль отображения вопроса
/// </summary>
[DataContract]
public enum QuestionStyle
{
    /// <summary>
    /// Просто вопрос
    /// </summary>
    [EnumMember]
    Normal,
    /// <summary>
    /// Подсвеченный текст вопроса
    /// </summary>
    [EnumMember]
    WaitingForPress,
    /// <summary>
    /// Вопрос + выигравший кнопку игрок
    /// </summary>
    [EnumMember]
    Pressed
}

/// <summary>
/// Defines a question table cell state.
/// </summary>
public enum QuestionInfoStages
{
    /// <summary>
    /// Normal state.
    /// </summary>
    None,

    /// <summary>
    /// Blinking state.
    /// </summary>
    Blinking,

    /// <summary>
    /// Active state.
    /// </summary>
    Active
}

using System.Runtime.Serialization;

namespace SIUI.ViewModel
{
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
    /// Тип содержимого вопроса, отображаемого на табло
    /// </summary>
    [DataContract]
    public enum QuestionContentType
    {
        /// <summary>
        /// Пустое содержимое
        /// </summary>
        [EnumMember]
        None,
        /// <summary>
        /// Текст
        /// </summary>
        [EnumMember]
        Text,
        /// <summary>
        /// Изображение
        /// </summary>
        [EnumMember]
        Image,
        /// <summary>
        /// Видео
        /// </summary>
        [EnumMember]
        Video,
        /// <summary>
        /// Специальный текст
        /// </summary>
        [EnumMember]
        SpecialText
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
    /// Состояние ячейки на табло
    /// </summary>
    public enum QuestionInfoStages
    {
        /// <summary>
        /// Обычное
        /// </summary>
        None,
        /// <summary>
        /// Мигающее
        /// </summary>
        Blinking,
        /// <summary>
        /// Выбранное
        /// </summary>
        Active
    }
}

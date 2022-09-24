using System.Collections.Generic;

namespace SICore.Results
{
    /// <summary>
    /// Результат игры
    /// </summary>
    public sealed class GameResult
    {
        /// <summary>
        /// Имя пакета
        /// </summary>
        public string PackageName { get; set; }

        /// <summary>
        /// Уникальный идентификатор пакета
        /// </summary>
        public string PackageID { get; set; }

        /// <summary>
        /// Выигрыши участников
        /// </summary>
        public List<PersonResult> Results { get; set; } = new List<PersonResult>();

        /// <summary>
        /// Апеллированные верные ответы
        /// </summary>
        public List<AnswerInfo> ApellatedQuestions { get; set; } = new List<AnswerInfo>();

        /// <summary>
        /// Полученные неверные ответы
        /// </summary>
        public List<AnswerInfo> WrongVersions { get; set; } = new List<AnswerInfo>();

        /// <summary>
        /// Помеченные вопросы
        /// </summary>
        public List<AnswerInfo> MarkedQuestions { get; set; } = new List<AnswerInfo>();

        /// <summary>
        /// Лог ошибок
        /// </summary>
        public string ErrorLog { get; set; } = "";

        /// <summary>
        /// Комментарии участника
        /// </summary>
        public string Comments { get; set; } = "";
    }
}

using System.Collections.Generic;
using System.Xml.Serialization;

namespace SICore.Results
{
    /// <summary>
    /// Результаты игры
    /// </summary>
    [XmlRoot("LocalGameResult")]
    public sealed class SIGameResult
    {
        /// <summary>
        /// Имя игрового пакета
        /// </summary>
        public string PackageName { get; set; }
        /// <summary>
        /// Лог произошедших ошибок
        /// </summary>
        public string ErrorLog { get; set; }
        /// <summary>
        /// Комментарии
        /// </summary>
        public string Comments { get; set; }
        /// <summary>
        /// Результаты участников
        /// </summary>
        public List<PersonResult> PersonResults { get; set; }
        /// <summary>
        /// Правильные (апеллированные) ответы
        /// </summary>
        public List<AnswerInfo> RightAnswers { get; set; }
        /// <summary>
        /// Неправильные ответы
        /// </summary>
        public List<AnswerInfo> WrongAnswers { get; set; }
		/// <summary>
		/// Помеченные вопросы
		/// </summary>
		public List<AnswerInfo> MarkedQuestions { get; } = new List<AnswerInfo>();

		public SIGameResult()
        {
            PersonResults = new List<PersonResult>();
            RightAnswers = new List<AnswerInfo>();
            WrongAnswers = new List<AnswerInfo>();

            ErrorLog = "";
            Comments = "";
        }

        public GameResult CreateResult()
        {
            return new GameResult
            {
                PackageName = PackageName,
                Results = PersonResults,
                ApellatedQuestions = RightAnswers,
                WrongVersions = WrongAnswers,
				MarkedQuestions = MarkedQuestions,
                ErrorLog = ErrorLog,
                Comments = Comments
            };
        }

        
    }
}

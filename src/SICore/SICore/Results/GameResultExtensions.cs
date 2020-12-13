using SICore.BusinessLogic;
using SIPackages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using R = SICore.Properties.Resources;

namespace SICore.Results
{
    /// <summary>
    /// Класс, позволяющий вывести результат игры в виде текстового отчёта
    /// </summary>
    public static class GameResultExtensions
    {
        private const string DescriptionFormat = "{0}: {1}";

        /// <summary>
        /// Получить строковое представление результатов игры
        /// </summary>
        /// <param name="gameResult">Результаты игры</param>
        /// <param name="doc">Игровой пакет, используемый для извлечения текстов вопросов и ответов</param>
        /// <param name="localizer">Локализатор</param>
        /// <returns>Строковое представление результатов игры</returns>
        public static string ToString(this GameResult gameResult, SIDocument doc, ILocalizer localizer)
        {
            var result = new StringBuilder();
            result.AppendFormat(DescriptionFormat, localizer[nameof(R.PackageName)], gameResult.PackageName).AppendLine().AppendLine();
            result.Append(localizer[nameof(R.GameResults)]).AppendLine(":");

            foreach (var item in gameResult.Results)
            {
                result.AppendFormat(DescriptionFormat, item.Name, item.Sum).AppendLine();
            }

            result.AppendLine().Append(localizer[nameof(R.ApellatedAnswers)]).AppendLine(":");
            PrintCollection(doc, gameResult.ApellatedQuestions, result, localizer[nameof(R.Apellation)], localizer);

            result.AppendLine().Append(localizer[nameof(R.WrongAnswers)]).AppendLine(":");
            PrintCollection(doc, gameResult.WrongVersions, result, localizer[nameof(R.WrongAns)], localizer);

            result.AppendLine().Append(localizer[nameof(R.ErrorMessages)]).AppendLine(":");
            result.AppendLine(gameResult.ErrorLog);

            return result.ToString().Replace(Environment.NewLine, "\r");
        }

        private static void PrintCollection(SIDocument doc, IEnumerable<AnswerInfo> collection,
            StringBuilder result, string answerTitle, ILocalizer localizer)
        {
            foreach (var answerInfo in collection)
            {
                if (answerInfo.Round < 0 || answerInfo.Round >= doc.Package.Rounds.Count)
                {
                    continue;
                }

                var round = doc.Package.Rounds[answerInfo.Round];

                if (answerInfo.Theme < 0 || answerInfo.Theme >= round.Themes.Count)
                {
                    continue;
                }

                var theme = round.Themes[answerInfo.Theme];

                if (answerInfo.Question < 0 || answerInfo.Question >= theme.Questions.Count)
                {
                    continue;
                }

                var quest = theme.Questions[answerInfo.Question];

                result.AppendFormat(DescriptionFormat, localizer[nameof(R.Question)], quest.Scenario.ToString()).AppendLine();
                var right = quest.GetRightAnswers();
                result.AppendFormat(DescriptionFormat, localizer[nameof(R.Answer)], right.FirstOrDefault()).AppendLine();
                result.AppendFormat(DescriptionFormat, answerTitle, answerInfo.Answer).AppendLine();

                result.AppendLine();
            }
        }
    }
}

using SIPackages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SICore.Results
{
    /// <summary>
    /// TODO: перевод
    /// </summary>
    public static class GameResultExtensions
    {
        /// <summary>
        /// Получить строковое представление результатов игры
        /// </summary>
        /// <param name="doc">Игровой пакет, используемый для извлечения текстов вопросов и ответов</param>
        /// <returns>Строковое представление результатов игры</returns>
        public static string ToString(this GameResult gameResult, SIDocument doc)
        {
            var result = new StringBuilder();
            result.AppendFormat("Имя пакета: {0}", gameResult.PackageName).AppendLine().AppendLine();
            result.AppendLine("Результаты игры:");

            foreach (var item in gameResult.Results)
            {
                result.AppendFormat("{0}: {1}", item.Name, item.Sum).AppendLine();
            }

            result.AppendLine().AppendLine("Апеллированные ответы:");
            PrintCollection(doc, gameResult.ApellatedQuestions, result, "Апелляция");

            result.AppendLine().AppendLine("Неверные ответы:");
            PrintCollection(doc, gameResult.WrongVersions, result, "Неверный ответ");

            result.AppendLine().AppendLine("Сообщения об ошибках:");
            result.AppendLine(gameResult.ErrorLog);

            return result.ToString().Replace(Environment.NewLine, "\r");
        }

        private static void PrintCollection(SIDocument doc, IEnumerable<AnswerInfo> collection, StringBuilder result, string text)
        {
            foreach (var item in collection)
            {
                if (item.Round < 0 || item.Round >= doc.Package.Rounds.Count)
                    continue;

                var round = doc.Package.Rounds[item.Round];

                if (item.Theme < 0 || item.Theme >= round.Themes.Count)
                    continue;

                var theme = round.Themes[item.Theme];

                if (item.Question < 0 || item.Question >= theme.Questions.Count)
                    continue;

                var quest = theme.Questions[item.Question];

                result.AppendFormat("Вопрос: {0}", quest.Scenario.ToString()).AppendLine();
                var right = quest.GetRightAnswers();
                result.AppendFormat("Ответ: {0}", right.FirstOrDefault()).AppendLine();
                result.AppendFormat("{1}: {0}", item.Answer, text).AppendLine();

                result.AppendLine();
            }
        }
    }
}

using Notions;

namespace SICore
{
    internal static class AnswerChecker
    {
        /// <summary>
        /// Проверка совпадения ответа с верным ответом
        /// </summary>
        /// <param name="answer">Ответ игрока</param>
        /// <param name="rightAnswer">Верный ответ</param>
        /// <returns></returns>
        internal static bool IsAnswerRight(string answer, string rightAnswer)
        {
            var res = Notion.AnswerValidatingCommon2(answer.NotDigitPart(), rightAnswer.NotDigitPart());
            var res2 = Notion.AnswerValidatingCommon2(answer.DigitPart(), rightAnswer.DigitPart());
            return res > 0.81 && res2 > 0.99;
        }
    }
}

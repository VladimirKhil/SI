using System.Text.RegularExpressions;

namespace SIPackages.Core
{
    public static class BagCatHelper
    {
        private static readonly Regex CatCostRegex = new Regex(@"\[(?'min'\d+);(?'max'\d+)\](/(?'step'\d+))?", RegexOptions.Compiled);

        /// <summary>
        /// Разобрать стоимость Обобщённого вопроса с секретом (см. спецификацию типа)
        /// Стоимость может иметь формат [100;500] или [200;1000]/200
        /// </summary>
        /// <param name="cost">Текст стоимости</param>
        /// <returns>Разобранный вариант стоимости</returns>
        public static BagCatInfo ParseCatCost(string cost)
        {
            var m = CatCostRegex.Match(cost);
            if (!m.Success)
                return null;

            int.TryParse(m.Groups["min"].ToString(), out var minimum);
            int.TryParse(m.Groups["max"].ToString(), out var maximum);
            var stepString = m.Groups["step"].ToString();

            return new BagCatInfo
            {
                Minimum = minimum,
                Maximum = maximum,
                Step = GetStepValue(minimum, maximum, stepString)
            };
        }

        private static int GetStepValue(int minimum, int maximum, string stepString)
        {
            if (stepString.Length > 0)
            {
                int.TryParse(stepString, out var step);
                return step;
            }

            return maximum - minimum;
        }
    }
}

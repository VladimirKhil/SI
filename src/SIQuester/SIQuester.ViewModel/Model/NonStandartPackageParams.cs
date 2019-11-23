using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SIQuester.Model
{
    /// <summary>
    /// Параметры нестандартного пакета
    /// </summary>
    public sealed class NonStandartPackageParams
    {
        /// <summary>
        /// Число стандартных раундов
        /// </summary>
        public int NumOfRounds { get; set; }

        /// <summary>
        /// Число тем в стандартном раунде
        /// </summary>
        public int NumOfThemes { get; set; }

        /// <summary>
        /// Число вопросов в теме
        /// </summary>
        public int NumOfQuestions { get; set; }

        /// <summary>
        /// Базовая стоимость вопроса в теме 1-го раунда
        /// </summary>
        public int NumOfPoints { get; set; }

        /// <summary>
        /// Имеется ли в пакете финальный раунд
        /// </summary>
        public bool HasFinal { get; set; }

        /// <summary>
        /// Число тем в финальном раунде
        /// </summary>
        public int NumOfFinalThemes { get; set; }

        public NonStandartPackageParams()
        {
            NumOfRounds = 3;
            NumOfThemes = 6;
            NumOfQuestions = 5;
            NumOfPoints = 100;
            HasFinal = true;
            NumOfFinalThemes = 7;
        }
    }
}

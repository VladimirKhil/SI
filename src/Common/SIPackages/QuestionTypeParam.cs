using SIPackages.Core;
using System;
using System.Diagnostics;

namespace SIPackages
{
    /// <summary>
    /// Параметр типа вопроса
    /// </summary>
    public sealed class QuestionTypeParam : Named
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string _value = "";

        /// <summary>
        /// Значение параметра
        /// </summary>
        public string Value
        {
            get { return _value; }
            set
            {
                var oldValue = _value;
                if (oldValue != value)
                {
                    _value = value;
                    OnPropertyChanged(oldValue);
                }
            }
        }

        /// <summary>
        /// Строковое представление параметра типа
        /// </summary>
        /// <returns></returns>
        public override string ToString() => $"{Name}: {Value}";
    }
}

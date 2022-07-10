using SIPackages.Core;
using System.Diagnostics;

namespace SIPackages
{
    /// <summary>
    /// Defines a question type parameter.
    /// </summary>
    public sealed class QuestionTypeParam : Named
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string _value = "";

        /// <summary>
        /// Parameter value.
        /// </summary>
        public string Value
        {
            get => _value;
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

        public override string ToString() => $"{Name}: {Value}";
    }
}

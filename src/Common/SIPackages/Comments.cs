using SIPackages.Core;
using System.Diagnostics;

namespace SIPackages
{
    /// <summary>
    /// Комментарии к объекту пакета
    /// </summary>
    public sealed class Comments: PropertyChangedNotifier
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string _text = "";

        /// <summary>
        /// Текст комментария
        /// </summary>
        public string Text
        {
            get { return _text; }
            set
            {
                var oldValue = _text;
                if (oldValue != value)
                {
                    _text = value;
                    OnPropertyChanged(oldValue);
                }
            }
        }

        public override string ToString() => $"Комментарии: {_text}";
    }
}

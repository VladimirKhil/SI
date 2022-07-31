using SIPackages.Core;
using SIPackages.Properties;
using System.Diagnostics;

namespace SIPackages
{
    /// <summary>
    /// Defines a package item comments.
    /// </summary>
    public sealed class Comments : PropertyChangedNotifier
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string _text = "";

        /// <summary>
        /// Comments text.
        /// </summary>
        public string Text
        {
            get => _text;
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

        /// <inheritdoc />
        public override string ToString() => $"{Resources.Comments}: {_text}";
    }
}

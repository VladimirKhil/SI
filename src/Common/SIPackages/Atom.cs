using SIPackages.Core;
using SIPackages.Properties;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace SIPackages
{
    /// <summary>
    /// Defines a question scenario minimal item.
    /// </summary>
    public sealed class Atom : PropertyChangedNotifier, ITyped
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string _type = AtomTypes.Text;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int _atomTime;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string _text = "";

        /// <summary>
        /// Is this atom a link to a file.
        /// </summary>
        public bool IsLink => _text.Length > 0 && _text[0] == '@';

        /// <summary>
        /// Atom type.
        /// </summary>
        public string Type
        {
            get => _type;
            set
            {
                if (_type != value)
                {
                    var oldValue = _type;
                    _type = value;
                    OnPropertyChanged(oldValue);
                }
            }
        }

        /// <summary>
        /// Localized string representation for a atom.
        /// </summary>
        public string TypeString => _type switch
        {
            AtomTypes.Image => Resources.Image,
            AtomTypes.Video => Resources.Video,
            AtomTypes.Audio => Resources.Audio,
            _ => _type,
        };

        /// <summary>
        /// Atom duration in seconds.
        /// </summary>
        [DefaultValue(0)]
        public int AtomTime
        {
            get => _atomTime;
            set
            {
                if (_atomTime != value)
                {
                    var oldValue = _atomTime;
                    _atomTime = value;
                    OnPropertyChanged(oldValue);
                }
            }
        }

        /// <summary>
        /// Текст единицы
        /// </summary>
        public string Text
        {
            get => _text;
            set
            {
                if (_text != value)
                {
                    var oldValue = _text;
                    _text = value;
                    OnPropertyChanged(oldValue);
                }
            }
        }

        /// <summary>
        /// Does atom text contain specified value.
        /// </summary>
        /// <param name="value">Text value.</param>
        public bool Contains(string value) => _text.IndexOf(value, StringComparison.CurrentCultureIgnoreCase) > -1;

        /// <inheritdoc />
        public override string ToString()
        {
            if (_type == AtomTypes.Text)
            {
                return _text;
            }

            var res = new StringBuilder();
            res.AppendFormat("#{0} ", _type);
            res.Append(_text);

            return res.ToString();
        }
    }
}

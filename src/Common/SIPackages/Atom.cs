using SIPackages.Core;
using SIPackages.Properties;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace SIPackages
{
    /// <summary>
    /// Элементарная единица сценария
    /// </summary>
    public sealed class Atom : PropertyChangedNotifier, ITyped
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string _type = AtomTypes.Text;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int _atomTime;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string _text = "";

        public bool IsLink => _text.Length > 0 && _text[0] == '@';

        /// <summary>
        /// Тип единицы
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
        /// Строковое представление некоторых типов
        /// </summary>
        public string TypeString
        {
            get
            {
                switch (_type)
                {
                    case AtomTypes.Image:
                        return Resources.Image;

                    case AtomTypes.Video:
                        return Resources.Video;

                    case AtomTypes.Audio:
                        return Resources.Audio;

                    default:
                        return _type;
                }
            }
        }

        /// <summary>
        /// Время действия атома
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

        public bool Contains(string value) => _text.IndexOf(value, StringComparison.CurrentCultureIgnoreCase) > -1;

        /// <summary>
        /// Строковое представление единицы
        /// </summary>
        /// <returns>Тип единицы и её содержимое</returns>
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

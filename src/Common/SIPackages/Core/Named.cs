using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SIPackages.Core
{
    /// <summary>
    /// Именованный объект пакета
    /// </summary>
    public class Named : PropertyChangedNotifier, INamed
    {
        // TODO: private
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        protected string _name;

        /// <summary>
        /// Имя
        /// </summary>
        public virtual string Name
        {
            get { return _name; }
            set
            {
                if (_name != value)
                {
                    var oldValue = _name;
                    _name = value;
                    OnPropertyChanged(oldValue);
                }
            }
        }

        /// <summary>
        /// Создание именованного объекта
        /// </summary>
        public Named() { }

        /// <summary>
        /// Создание именованного объекта
        /// </summary>
        public Named(string name) { _name = name; }

        public virtual bool Contains(string value)
        {
            return _name != null && _name.IndexOf(value, StringComparison.CurrentCultureIgnoreCase) > -1;
        }

        public virtual IEnumerable<SearchData> Search(string value)
        {
            return SearchExtensions.Search(ResultKind.Name, _name, value);
        }
    }
}

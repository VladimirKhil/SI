using SIPackages.Core;
using System.Runtime.Serialization;
using System.Text;

namespace SIPackages
{
    /// <summary>
    /// Источник
    /// </summary>
    [DataContract]
    public sealed class SourceInfo : IdOwner
    {
        private string _author;
        private string _title;
        private int _year;
        private string _publish;
        private string _city;

        /// <summary>
        /// Автор источника
        /// </summary>
        [DataMember]
        public string Author
        {
            get { return _author; }
            set { var oldValue = _author; if (oldValue != value) { _author = value; OnPropertyChanged(oldValue); } }
        }

        /// <summary>
        /// Название источника
        /// </summary>
        [DataMember]
        public string Title
        {
            get { return _title; }
            set { var oldValue = _title; if (oldValue != value) { _title = value; OnPropertyChanged(oldValue); } }
        }

        /// <summary>
        /// Год издания
        /// </summary>
        [DataMember]
        public int Year
        {
            get { return _year; }
            set { var oldValue = _year; if (oldValue != value) { _year = value; OnPropertyChanged(oldValue); } }
        }

        /// <summary>
        /// Издательство
        /// </summary>
        [DataMember]
        public string Publish
        {
            get { return _publish; }
            set { var oldValue = _publish; if (oldValue != value) { _publish = value; OnPropertyChanged(oldValue); } }
        }

        /// <summary>
        /// Город
        /// </summary>
        [DataMember]
        public string City
        {
            get { return _city; }
            set { var oldValue = _city; if (oldValue != value) { _city = value; OnPropertyChanged(oldValue); } }
        }

        /// <summary>
        /// Строковое представление источника
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var result = new StringBuilder();

            if (!string.IsNullOrEmpty(_author))
            {
                result.Append(_author);
                result.Append(". ");
            }

            result.Append(_title ?? "");

            if (!string.IsNullOrEmpty(_city))
            {
                result.Append(".: ");
                result.Append(_city);
            }

            if (!string.IsNullOrEmpty(_publish))
            {
                result.Append(" - ");
                result.Append(_publish);
            }

            if (_year > 0)
            {
                result.Append(", ");
                result.Append(_year);
            }

            return result.ToString();
        }

        public SourceInfo Clone() => new SourceInfo { _author = _author, _city = _city, _publish = _publish, _title = _title, _year = _year, Id = Id };
    }
}

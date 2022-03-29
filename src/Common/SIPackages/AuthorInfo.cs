using SIPackages.Core;
using System.Runtime.Serialization;
using System.Text;

namespace SIPackages
{
    /// <summary>
    /// Автор
    /// </summary>
    [DataContract]
    public sealed class AuthorInfo: IdOwner
    {
        private string _name;
        private string _surname;
        private string _secondName;
        private string _country;
        private string _city;

        /// <summary>
        /// Имя
        /// </summary>
        [DataMember]
        public string Name
        {
            get { return _name; }
            set { var oldValue = _name; if (oldValue != value) { _name = value; OnPropertyChanged(oldValue); } }
        }

        /// <summary>
        /// Фамилия
        /// </summary>
        [DataMember]
        public string Surname
        {
            get { return _surname; }
            set { var oldValue = _surname; if (oldValue != value) { _surname = value; OnPropertyChanged(oldValue); } }
        }

        /// <summary>
        /// Отчество
        /// </summary>
        [DataMember]
        public string SecondName
        {
            get { return _secondName; }
            set { var oldValue = _secondName; if (oldValue != value) { _secondName = value; OnPropertyChanged(oldValue); } }
        }

        /// <summary>
        /// Страна
        /// </summary>
        [DataMember]
        public string Country
        {
            get { return _country; }
            set { var oldValue = _country; if (oldValue != value) { _country = value; OnPropertyChanged(oldValue); } }
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
        /// Строковое представление автора
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var result = new StringBuilder();

            result.Append(_name);
            if (!string.IsNullOrEmpty(_secondName))
            {
                result.Append(' ');
                result.Append(_secondName);
            }

            if (!string.IsNullOrEmpty(_surname))
            {
                result.Append(' ');
                result.Append(_surname);
            }

            if (!string.IsNullOrEmpty(_city) || !string.IsNullOrEmpty(_country))
            {
                result.Append(" (");
                if (!string.IsNullOrEmpty(_city))
                {
                    result.Append(_city);
                    if (!string.IsNullOrEmpty(_country))
                    {
                        result.Append(", ");
                        result.Append(_country);
                    }
                    else
                        result.Append(_country);
                }
                result.Append(")");
            }

            return result.ToString();
        }

        /// <summary>
        /// Creates a copy of this object.
        /// </summary>
        public AuthorInfo Clone() => new()
        {
            _city = _city,
            _country = _country,
            _name = _name,
            _secondName = _secondName,
            _surname = _surname,
            Id = Id
        };
    }
}

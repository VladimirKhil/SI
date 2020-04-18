using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Serialization;
using System.Runtime.Serialization;
using System.Runtime.CompilerServices;
using System;

namespace SIData
{
    /// <summary>
    /// Аккаунт участника
    /// </summary>
    [DataContract]
    public class Account: INotifyPropertyChanged
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string _name = "";
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _isMale = true;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string _picture = "";
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _canBeDeleted = false;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _isHuman = false;

        /// <summary>
        /// Имя
        /// </summary>
        [XmlAttribute]
        [DataMember]
        public string Name
        {
            get { return _name; }
            set { _name = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Мужской пол
        /// </summary>
        [XmlAttribute]
        [DefaultValue(true)]
        [DataMember]
        public bool IsMale
        {
            get { return _isMale; }
            set { _isMale = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Для обратной совместимости (название переменной не позволяет понять, чему соответсвуют true и false)
        /// </summary>
        [Obsolete]
        [XmlAttribute]
        [DefaultValue(true)]
        [DataMember]
        public bool Sex
        {
            get => IsMale;
            set { IsMale = value; }
        }

        /// <summary>
        /// Адрес картинки
        /// </summary>
        [XmlAttribute]
        [DefaultValue("")]
        [DataMember]
        public virtual string Picture
        {
            get { return _picture; }
            set { if (_picture != value) { _picture = value; OnPropertyChanged(); } }
        }

        /// <summary>
        /// Может ли аккаунт быть удалён (встроенные аккаунты не могут быть удалены)
        /// </summary>
        [XmlAttribute]
        [DefaultValue(false)]
        [DataMember]
        public bool CanBeDeleted
        {
            get { return _canBeDeleted; }
            set { if (_canBeDeleted != value) { _canBeDeleted = value; OnPropertyChanged(); } }
        }

        [XmlIgnore]
        [DataMember]
        public bool IsHuman
        {
            get { return _isHuman; }
            set { if (_isHuman != value) { _isHuman = value; OnPropertyChanged(); } }
        }

        public Account()
        {
        }

        public Account(string name, bool sex)
        {
            _name = name;
            _isMale = sex;
        }

        public Account(Account account)
        {
            _name = account.Name;
            _isMale = account.IsMale;
            _picture = account.Picture;
            _isHuman = account._isHuman;
        }

        public override string ToString()
        {
            return _name;
        }

        /// <summary>
        /// Изменилось значение свойства
        /// </summary>
        /// <param name="name">Имя свойства</param>
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}

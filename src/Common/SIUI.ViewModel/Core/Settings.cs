using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace SIUI.ViewModel.Core
{
    [DataContract]
    public sealed class Settings : INotifyPropertyChanged
    {
        public const string DefaultTableFontFamily = "_Default";
        public const string DefaultTableColorString = "#FFFFE682";
        public const string DefaultTableBackColorString = "#FF000451";
        public const double DefaultQuestionLineSpacing = 1.5;

        private string _tableFontFamily = DefaultTableFontFamily;

        [DefaultValue(DefaultTableFontFamily)]
        [XmlAttribute]
        [DataMember]
        public string TableFontFamily
        {
            get { return _tableFontFamily; }
            set { if (_tableFontFamily != value) { _tableFontFamily = value; OnPropertyChanged(); } }
        }

        private string _tableColorString = DefaultTableColorString;

        [XmlAttribute]
        [DefaultValue(DefaultTableColorString)]
        [DataMember]
        public string TableColorString
        {
            get { return _tableColorString; }
            set { if (_tableColorString != value) { _tableColorString = value; OnPropertyChanged(); } }
        }

        private string _tableBackColorString = DefaultTableBackColorString;

        [XmlAttribute]
        [DefaultValue(DefaultTableBackColorString)]
        [DataMember]
        public string TableBackColorString
        {
            get { return _tableBackColorString; }
            set { if (_tableBackColorString != value) { _tableBackColorString = value; OnPropertyChanged(); } }
        }

        private double _questionLineSpacing = DefaultQuestionLineSpacing;

        [DefaultValue(DefaultQuestionLineSpacing)]
        [XmlAttribute]
        [DataMember]
        public double QuestionLineSpacing
        {
            get { return _questionLineSpacing; }
            set { if (Math.Abs(_questionLineSpacing - value) < double.Epsilon) { _questionLineSpacing = value; OnPropertyChanged(); } }
        }

        private bool _showScore = false;

        [DefaultValue(false)]
        [XmlAttribute]
        [DataMember]
        public bool ShowScore
        {
            get { return _showScore; }
            set { if (_showScore != value) { _showScore = value; OnPropertyChanged(); } }
        }

        private bool _animate3D = true;

        [DefaultValue(true)]
        [XmlAttribute]
        [DataMember]
        public bool Animate3D
        {
            get { return _animate3D; }
            set { if (_animate3D != value) { _animate3D = value; OnPropertyChanged(); } }
        }

        private bool _keyboardControl;

        [DefaultValue(false)]
        [XmlAttribute]
        [DataMember]
        public bool KeyboardControl
        {
            get { return _keyboardControl; }
            set { if (_keyboardControl != value) { _keyboardControl = value; OnPropertyChanged(); } }
        }

        private string _logoUri = "";

        /// <summary>
        /// Адрес заставки
        /// </summary>
        [DefaultValue("")]
        [XmlAttribute]
        [DataMember]
        public string LogoUri
        {
            get { return _logoUri; }
            set
            {
                if (_logoUri != value)
                {
                    _logoUri = value;
                    OnPropertyChanged();
                }
            }
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void Initialize(Settings uiSettings)
        {
            TableFontFamily = uiSettings._tableFontFamily;
            QuestionLineSpacing = uiSettings._questionLineSpacing;
            TableColorString = uiSettings._tableColorString;
            TableBackColorString = uiSettings._tableBackColorString;
            ShowScore = uiSettings._showScore;
            KeyboardControl = uiSettings._keyboardControl;
            Animate3D = uiSettings._animate3D;
            LogoUri = uiSettings._logoUri;
        }
    }
}

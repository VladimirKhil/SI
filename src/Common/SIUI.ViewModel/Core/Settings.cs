using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace SIUI.ViewModel.Core;

/// <summary>
/// Defines a UI settings.
/// </summary>
[DataContract]
public sealed class Settings : INotifyPropertyChanged
{
    public const string DefaultTableFontFamily = "_Default";
    public const string DefaultTableColorString = "White";
    public const string DefaultTableBackColorString = "#FF000451";
    public const string DefaultTableGridColorString = null;
    public const string DefaultAnswererColorString = "#DD1D1F77";
    public const double DefaultQuestionLineSpacing = 1.5;
    public const string DefaultBackgroundImageUri = null;
    public const string DefaultBackgroundVideoUri = null;

    private string _tableFontFamily = DefaultTableFontFamily;

    [DefaultValue(DefaultTableFontFamily)]
    [XmlAttribute]
    [DataMember]
    public string TableFontFamily
    {
        get => _tableFontFamily;
        set { if (_tableFontFamily != value) { _tableFontFamily = value; OnPropertyChanged(); } }
    }

    private string _tableColorString = DefaultTableColorString;

    [XmlAttribute]
    [DefaultValue(DefaultTableColorString)]
    [DataMember]
    public string TableColorString
    {
        get => _tableColorString;
        set { if (_tableColorString != value) { _tableColorString = value; OnPropertyChanged(); } }
    }

    private string _tableBackColorString = DefaultTableBackColorString;

    [XmlAttribute]
    [DefaultValue(DefaultTableBackColorString)]
    [DataMember]
    public string TableBackColorString
    {
        get => _tableBackColorString;
        set { if (_tableBackColorString != value) { _tableBackColorString = value; OnPropertyChanged(); } }
    }

    private string _tableGridColorString = DefaultTableGridColorString;

    [XmlAttribute]
    [DefaultValue(DefaultTableGridColorString)]
    [DataMember]
    public string TableGridColorString
    {
        get => _tableGridColorString;
        set { if (_tableGridColorString != value) { _tableGridColorString = value; OnPropertyChanged(); } }
    }

    private string _answererColorString = DefaultAnswererColorString;

    [XmlAttribute]
    [DefaultValue(DefaultAnswererColorString)]
    [DataMember]
    public string AnswererColorString
    {
        get => _answererColorString;
        set { if (_answererColorString != value) { _answererColorString = value; OnPropertyChanged(); } }
    }

    private double _questionLineSpacing = DefaultQuestionLineSpacing;

    [DefaultValue(DefaultQuestionLineSpacing)]
    [XmlAttribute]
    [DataMember]
    public double QuestionLineSpacing
    {
        get => _questionLineSpacing;
        set { if (Math.Abs(_questionLineSpacing - value) > double.Epsilon) { _questionLineSpacing = value; OnPropertyChanged(); } }
    }

    private bool _showScore = false;

    [DefaultValue(false)]
    [XmlAttribute]
    [DataMember]
    [Obsolete]
    public bool ShowScore
    {
        get => _showScore;
        set { if (_showScore != value) { _showScore = value; OnPropertyChanged(); } }
    }

    private bool _animate3D = true;

    [DefaultValue(true)]
    [XmlAttribute]
    [DataMember]
    public bool Animate3D
    {
        get => _animate3D;
        set { if (_animate3D != value) { _animate3D = value; OnPropertyChanged(); } }
    }

    private bool _keyboardControl;

    [DefaultValue(false)]
    [XmlAttribute]
    [DataMember]
    public bool KeyboardControl
    {
        get => _keyboardControl;
        set { if (_keyboardControl != value) { _keyboardControl = value; OnPropertyChanged(); } }
    }

    private string _logoUri = "";

    /// <summary>
    /// Table logo image.
    /// </summary>
    [DefaultValue("")]
    [XmlAttribute]
    [DataMember]
    public string LogoUri
    {
        get => _logoUri;
        set
        {
            if (_logoUri != value)
            {
                _logoUri = value;
                OnPropertyChanged();
            }
        }
    }

    private string _backgroundImageUri = DefaultBackgroundImageUri;

    /// <summary>
    /// Изображение-фон
    /// </summary>
    [DefaultValue(DefaultBackgroundImageUri)]
    [XmlAttribute]
    [DataMember]
    public string BackgroundImageUri
    {
        get => _backgroundImageUri;
        set
        {
            if (_backgroundImageUri != value)
            {
                _backgroundImageUri = value;
                OnPropertyChanged();
            }
        }
    }

    private string _backgroundVideoUri = DefaultBackgroundVideoUri;

    /// <summary>
    /// Видеофайл-фон
    /// </summary>
    [DefaultValue(DefaultBackgroundVideoUri)]
    [XmlAttribute]
    [DataMember]
    public string BackgroundVideoUri
    {
        get => _backgroundVideoUri;
        set
        {
            if (_backgroundVideoUri != value)
            {
                _backgroundVideoUri = value;
                OnPropertyChanged();
            }
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Initialize(Settings uiSettings)
    {
        TableFontFamily = uiSettings._tableFontFamily;
        QuestionLineSpacing = uiSettings._questionLineSpacing;
        TableColorString = uiSettings._tableColorString;
        TableBackColorString = uiSettings._tableBackColorString;
        TableGridColorString = uiSettings.TableGridColorString;
        AnswererColorString = uiSettings.AnswererColorString;
        ShowScore = uiSettings._showScore;
        KeyboardControl = uiSettings._keyboardControl;
        Animate3D = uiSettings._animate3D;
        LogoUri = uiSettings._logoUri;
        BackgroundImageUri = uiSettings.BackgroundImageUri;
        BackgroundVideoUri = uiSettings.BackgroundVideoUri;
    }
}

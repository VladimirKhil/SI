using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace SIUI.ViewModel.Core;

/// <summary>
/// Defines UI settings.
/// </summary>
[DataContract]
public sealed class Settings : INotifyPropertyChanged
{
    public const string DefaultTableFontFamily = "_Default";
    public const string DefaultTableColorString = "White";
    public const string DefaultTableBackColorString = "#FF0A0E30";
    public const string DefaultTableGridColorString = null;
    public const string DefaultAnswererColorString = "#DD1D1F77";
    public const double DefaultQuestionLineSpacing = 1.5;
    public const string DefaultBackgroundImageUri = null;
    public const string DefaultBackgroundVideoUri = null;
    public const bool DefaultDisplayAnswerOptionsLabels = true;

    private string _tableFontFamily = DefaultTableFontFamily;
    private string _tableColorString = DefaultTableColorString;
    private string _tableBackColorString = DefaultTableBackColorString;
    private string _tableGridColorString = DefaultTableGridColorString;
    private string _answererColorString = DefaultAnswererColorString;
    private double _questionLineSpacing = DefaultQuestionLineSpacing;
    private bool _keyboardControl;
    private string _logoUri = "";
    private string _backgroundImageUri = DefaultBackgroundImageUri;
    private string _backgroundVideoUri = DefaultBackgroundVideoUri;

    [DefaultValue(DefaultTableFontFamily)]
    [XmlAttribute]
    [DataMember]
    public string TableFontFamily
    {
        get => _tableFontFamily;
        set { if (_tableFontFamily != value) { _tableFontFamily = value; OnPropertyChanged(); } }
    }

    [XmlAttribute]
    [DefaultValue(DefaultTableColorString)]
    [DataMember]
    public string TableColorString
    {
        get => _tableColorString;
        set { if (_tableColorString != value) { _tableColorString = value; OnPropertyChanged(); } }
    }

    [XmlAttribute]
    [DefaultValue(DefaultTableBackColorString)]
    [DataMember]
    public string TableBackColorString
    {
        get => _tableBackColorString;
        set { if (_tableBackColorString != value) { _tableBackColorString = value; OnPropertyChanged(); } }
    }

    [XmlAttribute]
    [DefaultValue(DefaultTableGridColorString)]
    [DataMember]
    public string TableGridColorString
    {
        get => _tableGridColorString;
        set { if (_tableGridColorString != value) { _tableGridColorString = value; OnPropertyChanged(); } }
    }

    [XmlAttribute]
    [DefaultValue(DefaultAnswererColorString)]
    [DataMember]
    public string AnswererColorString
    {
        get => _answererColorString;
        set { if (_answererColorString != value) { _answererColorString = value; OnPropertyChanged(); } }
    }

    [DefaultValue(DefaultQuestionLineSpacing)]
    [XmlAttribute]
    [DataMember]
    public double QuestionLineSpacing
    {
        get => _questionLineSpacing;
        set { if (Math.Abs(_questionLineSpacing - value) > double.Epsilon) { _questionLineSpacing = value; OnPropertyChanged(); } }
    }

    [DefaultValue(false)]
    [XmlAttribute]
    [DataMember]
    public bool KeyboardControl
    {
        get => _keyboardControl;
        set { if (_keyboardControl != value) { _keyboardControl = value; OnPropertyChanged(); } }
    }

    [DefaultValue("")]
    [XmlAttribute]
    [DataMember]
    public string LogoUri
    {
        get => _logoUri;
        set { if (_logoUri != value) { _logoUri = value; OnPropertyChanged(); } }
    }

    [DefaultValue(DefaultBackgroundImageUri)]
    [XmlAttribute]
    [DataMember]
    public string BackgroundImageUri
    {
        get => _backgroundImageUri;
        set { if (_backgroundImageUri != value) { _backgroundImageUri = value; OnPropertyChanged(); } }
    }

    [DefaultValue(DefaultBackgroundVideoUri)]
    [XmlAttribute]
    [DataMember]
    public string BackgroundVideoUri
    {
        get => _backgroundVideoUri;
        set { if (_backgroundVideoUri != value) { _backgroundVideoUri = value; OnPropertyChanged(); } }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

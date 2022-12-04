using SIUI.ViewModel.Core;

namespace SIUI.ViewModel;

/// <summary>
/// Defines a settings view model.
/// </summary>
public sealed class SettingsViewModel : ViewModelBase<Settings>
{
    public double TableColorR
    {
        get => Convert.ToByte(_model.TableColorString.Substring(3, 2), 16);
        set
        {
            _model.TableColorString = _model.TableColorString.Substring(0, 3)
                + ((byte)value).ToString("x2")
                + _model.TableColorString.Substring(5);

            OnPropertyChanged(nameof(TableColorString));
        }
    }

    public double TableColorG
    {
        get => Convert.ToByte(_model.TableColorString.Substring(5, 2), 16);
        set
        {
            _model.TableColorString = _model.TableColorString.Substring(0, 5)
                + ((byte)value).ToString("x2")
                + _model.TableColorString.Substring(7);

            OnPropertyChanged(nameof(TableColorString));
        }
    }

    public double TableColorB
    {
        get => Convert.ToByte(_model.TableColorString.Substring(7, 2), 16);
        set
        {
            _model.TableColorString = _model.TableColorString.Substring(0, 7) + ((byte)value).ToString("x2");
            OnPropertyChanged(nameof(TableColorString));
        }
    }

    public string TableColorString => _model.TableColorString;

    public double TableBackColorR
    {
        get => Convert.ToByte(_model.TableBackColorString.Substring(3, 2), 16);
        set
        {
            _model.TableBackColorString = _model.TableBackColorString.Substring(0, 3)
                + ((byte)value).ToString("x2")
                + _model.TableBackColorString.Substring(5);

            OnPropertyChanged(nameof(TableBackColorString));
        }
    }

    public double TableBackColorG
    {
        get => Convert.ToByte(_model.TableBackColorString.Substring(5, 2), 16);
        set
        {
            _model.TableBackColorString = _model.TableBackColorString.Substring(0, 5)
                + ((byte)value).ToString("x2")
                + _model.TableBackColorString.Substring(7);

            OnPropertyChanged(nameof(TableBackColorString));
        }
    }

    public double TableBackColorB
    {
        get => Convert.ToByte(_model.TableBackColorString.Substring(7, 2), 16);
        set
        {
            _model.TableBackColorString = _model.TableBackColorString.Substring(0, 7) + ((byte)value).ToString("x2");
            OnPropertyChanged(nameof(TableBackColorString));
        }
    }

    public string TableBackColorString => _model.TableBackColorString;

    public string TableGridColorString => _model.TableGridColorString;

    public string AnswererColorString => _model.AnswererColorString;

    public string TableFontFamily
    {
        get => _model.TableFontFamily;
        set { if (_model.TableFontFamily != value) { _model.TableFontFamily = value; OnPropertyChanged(); } }
    }

    public double QuestionLineSpacing
    {
        get => _model.QuestionLineSpacing;
        set { if (_model.QuestionLineSpacing != value) { _model.QuestionLineSpacing = value; OnPropertyChanged(); } }
    }

    public string[] FontFamilies { get; } = new string[] { Settings.DefaultTableFontFamily }
        .Concat(new string[] { "Arial", "Segoe UI" })
        .ToArray();

    public double[] LineSpaces { get; } = new double[] { 1.0, Settings.DefaultQuestionLineSpacing };

    public SettingsViewModel()
    {

    }

    public SettingsViewModel(Settings settings)
        : base(settings)
    {

    }

    public void Initialize(Settings uiSettings)
    {
        TableFontFamily = uiSettings.TableFontFamily;
        QuestionLineSpacing = uiSettings.QuestionLineSpacing;
        _model.TableColorString = uiSettings.TableColorString;
        _model.TableBackColorString = uiSettings.TableBackColorString;
        _model.TableGridColorString = uiSettings.TableGridColorString;
        _model.AnswererColorString = uiSettings.AnswererColorString;
        _model.ShowScore = uiSettings.ShowScore;
        _model.KeyboardControl = uiSettings.KeyboardControl;
        _model.Animate3D = uiSettings.Animate3D;
        _model.LogoUri = uiSettings.LogoUri;
        _model.BackgroundImageUri = uiSettings.BackgroundImageUri;
        _model.BackgroundVideoUri = uiSettings.BackgroundVideoUri;
    }

    public void Reset()
    {
        _model.TableColorString = Settings.DefaultTableColorString;
        _model.TableBackColorString = Settings.DefaultTableBackColorString;
        _model.TableGridColorString = Settings.DefaultTableGridColorString;
        _model.AnswererColorString = Settings.DefaultAnswererColorString;
        QuestionLineSpacing = Settings.DefaultQuestionLineSpacing;
        TableFontFamily = Settings.DefaultTableFontFamily;
        _model.Animate3D = true;
        _model.LogoUri = "";
        _model.BackgroundImageUri = Settings.DefaultBackgroundImageUri;
        _model.BackgroundVideoUri = Settings.DefaultBackgroundVideoUri;
    }
}

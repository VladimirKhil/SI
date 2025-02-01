using SIUI.ViewModel.Core;

namespace SIUI.ViewModel;

/// <summary>
/// Defines a settings view model.
/// </summary>
public sealed class SettingsViewModel : ViewModelBase<Settings>
{
    public string TableColorString => _model.TableColorString;

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

    public bool DisplayAnswerOptionsLabels
    {
        get => _model.DisplayAnswerOptionsLabels;
        set
        {
            if (_model.DisplayAnswerOptionsLabels != value)
            {
                _model.DisplayAnswerOptionsLabels = value;
                OnPropertyChanged();
            }
        }
    }

    public string[] FontFamilies { get; } = new string[] { Settings.DefaultTableFontFamily, "Arial", "Segoe UI" };

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
        _model.KeyboardControl = uiSettings.KeyboardControl;
        _model.Animate3D = uiSettings.Animate3D;
        _model.LogoUri = uiSettings.LogoUri;
        _model.BackgroundImageUri = uiSettings.BackgroundImageUri;
        _model.BackgroundVideoUri = uiSettings.BackgroundVideoUri;
    }

    public void Reset() => Initialize(new Settings());
}

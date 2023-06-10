using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace SIGame.ViewModel;

/// <summary>
/// Provides application theme settings.
/// </summary>
public sealed class ThemeSettings : INotifyPropertyChanged
{
    internal const int DefaultMaximumTableTextLength = 1200;
    internal const int DefaultMaximumReplicTextLength = 400;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private SIUI.ViewModel.Core.Settings _uiSettings = new();

    /// <summary>
    /// Настройки отображения табло
    /// </summary>
    public SIUI.ViewModel.Core.Settings UISettings
    {
        get => _uiSettings;
        set { _uiSettings = value; OnPropertyChanged(); }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private int _maximumTableTextLength = DefaultMaximumTableTextLength;

    /// <summary>
    /// Maximum length for text on game table.
    /// </summary>
    [DefaultValue(DefaultMaximumTableTextLength)]
    public int MaximumTableTextLength
    {
        get { return _maximumTableTextLength; }
        set { if (_maximumTableTextLength != value) { _maximumTableTextLength = value; OnPropertyChanged(); } }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private int _maximumReplicTextLength = DefaultMaximumReplicTextLength;

    /// <summary>
    /// Maximum length for replic text.
    /// </summary>
    [DefaultValue(DefaultMaximumReplicTextLength)]
    public int MaximumReplicTextLength
    {
        get { return _maximumReplicTextLength; }
        set { if (_maximumReplicTextLength != value) { _maximumReplicTextLength = value; OnPropertyChanged(); } }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string _customMainBackgroundUri = null;

    /// <summary>
    /// Настроенное главное фоновое изображение
    /// </summary>
    public string CustomMainBackgroundUri
    {
        get { return _customMainBackgroundUri; }
        set { _customMainBackgroundUri = value; OnPropertyChanged(); }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string _customBackgroundUri = null;

    /// <summary>
    /// Настроенное фоновое изображение
    /// </summary>
    public string CustomBackgroundUri
    {
        get { return _customBackgroundUri; }
        set { _customBackgroundUri = value; OnPropertyChanged(); }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string _soundMainMenuUri = null;

    /// <summary>
    /// Мелодия главного меню
    /// </summary>
    public string SoundMainMenuUri
    {
        get { return _soundMainMenuUri; }
        set { _soundMainMenuUri = value; OnPropertyChanged(); }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string _soundBeginRoundUri = null;

    /// <summary>
    /// Мелодия начала раунда
    /// </summary>
    public string SoundBeginRoundUri
    {
        get { return _soundBeginRoundUri; }
        set { _soundBeginRoundUri = value; OnPropertyChanged(); }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string _soundRoundThemesUri = null;

    /// <summary>
    /// Мелодия тем раунда
    /// </summary>
    public string SoundRoundThemesUri
    {
        get { return _soundRoundThemesUri; }
        set { _soundRoundThemesUri = value; OnPropertyChanged(); }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string _soundNoAnswerUri = null;

    /// <summary>
    /// Мелодия окончания времени на нажатие кнопки
    /// </summary>
    public string SoundNoAnswerUri
    {
        get { return _soundNoAnswerUri; }
        set { _soundNoAnswerUri = value; OnPropertyChanged(); }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string _soundQuestionStakeUri = null;

    /// <summary>
    /// Мелодия вопроса со ставкой
    /// </summary>
    public string SoundQuestionStakeUri
    {
        get { return _soundQuestionStakeUri; }
        set { _soundQuestionStakeUri = value; OnPropertyChanged(); }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string _soundQuestionGiveUri = null;

    /// <summary>
    /// Мелодия вопроса с передачей
    /// </summary>
    public string SoundQuestionGiveUri
    {
        get { return _soundQuestionGiveUri; }
        set { _soundQuestionGiveUri = value; OnPropertyChanged(); }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string _soundQuestionNoRiskUri = null;

    /// <summary>
    /// Мелодия вопроса без риска
    /// </summary>
    public string SoundQuestionNoRiskUri
    {
        get { return _soundQuestionNoRiskUri; }
        set { _soundQuestionNoRiskUri = value; OnPropertyChanged(); }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string _soundFinalThinkUri = null;

    /// <summary>
    /// Мелодия размышления в финале
    /// </summary>
    public string SoundFinalThinkUri
    {
        get { return _soundFinalThinkUri; }
        set { _soundFinalThinkUri = value; OnPropertyChanged(); }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string _soundTimeoutUri = null;

    /// <summary>
    /// Мелодия окончания времени раунда
    /// </summary>
    public string SoundTimeoutUri
    {
        get { return _soundTimeoutUri; }
        set { _soundTimeoutUri = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    internal void Initialize(ThemeSettings themeSettings)
    {
        _uiSettings.Initialize(themeSettings._uiSettings);

        MaximumTableTextLength = themeSettings.MaximumTableTextLength;
        MaximumReplicTextLength = themeSettings.MaximumReplicTextLength;

        CustomMainBackgroundUri = themeSettings.CustomMainBackgroundUri;
        CustomBackgroundUri = themeSettings.CustomBackgroundUri;

        SoundMainMenuUri = themeSettings.SoundMainMenuUri;
        SoundBeginRoundUri = themeSettings.SoundBeginRoundUri;
        SoundRoundThemesUri = themeSettings.SoundRoundThemesUri;
        SoundNoAnswerUri = themeSettings.SoundNoAnswerUri;
        SoundQuestionStakeUri = themeSettings.SoundQuestionStakeUri;
        SoundQuestionGiveUri = themeSettings.SoundQuestionGiveUri;
        SoundQuestionNoRiskUri = themeSettings.SoundQuestionNoRiskUri;
        SoundFinalThinkUri = themeSettings.SoundFinalThinkUri;
        SoundTimeoutUri = themeSettings.SoundTimeoutUri;
    }

    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

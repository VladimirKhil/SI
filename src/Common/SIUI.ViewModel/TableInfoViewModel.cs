using SIUI.Model;
using SIUI.ViewModel.Core;
using System.Collections.ObjectModel;
using Utils.Commands;

namespace SIUI.ViewModel;

/// <summary>
/// Defines game table view model.
/// </summary>
public sealed class TableInfoViewModel : ViewModelBase<TableInfo>
{
    private TableStage _tStage = TableStage.Void;

    /// <summary>
    /// Current table stage.
    /// </summary>
    public TableStage TStage
    {
        get => _tStage;
        set
        {
            if (_tStage != value)
            {
                _tStage = value;

                try
                {
                    OnPropertyChanged();
                }
                catch (NotImplementedException exc) when (exc.Message.Contains("The Source property cannot be set to null"))
                {
                    // https://github.com/MicrosoftEdge/WebView2Feedback/issues/1136
                }
            }
        }
    }

    public object TStageLock { get; } = new object();

    /// <summary>
    /// Is game paused.
    /// </summary>
    public bool Pause
    {
        get => _model.Pause;
        set
        {
            if (_model.Pause != value)
            {
                _model.Pause = value;
                OnPropertyChanged();
                UpdateMediaState();
            }
        }
    }

    private bool _isMediaStopped;

    public bool IsMediaStopped
    {
        get => _isMediaStopped;
        set
        {
            if (_isMediaStopped != value)
            {
                _isMediaStopped = value;
                UpdateMediaState();
            }
        }
    }

    private void UpdateMediaState()
    {
        if (_model.Pause || _isMediaStopped)
        {
            OnMediaPause();
        }
        else
        {
            OnMediaResume();
        }
    }

    private string _caption = "";

    /// <summary>
    /// Table caption.
    /// </summary>
    public string Caption
    {
        get => _caption;
        set
        {
            if (_caption != value)
            {
                _caption = value;
                OnPropertyChanged();
            }
        }
    }

    private string _text = "";

    /// <summary>
    /// Displayed text.
    /// </summary>
    public string Text
    {
        get => _text;
        set { if (_text != value) { _text = value; OnPropertyChanged(); } }
    }

    private int _textLength;

    /// <summary>
    /// Длина текста. При использовании частичного текста свойство Text содержит не только частичный текст, но и форму остального текста вопроса.
    /// Её отображать не надо
    /// </summary>
    public int TextLength
    {
        get => _textLength;
        set { if (_textLength != value) { _textLength = value; OnPropertyChanged(); } }
    }

    private string _hint = "";

    /// <summary>
    /// Additional hint shown over other content with transparency for a limited time.
    /// </summary>
    public string Hint
    {
        get => _hint;
        set 
        {
            if (_hint != value)
            { 
                _hint = value;

                try
                {
                    OnPropertyChanged();
                }
                catch (NullReferenceException)
                {
                    // Occurs very rarely. Investigation required
                    // System.NullReferenceException: Object reference not set to an instance of an object.
                    // at void MS.Internal.Data.ClrBindingWorker.OnSourcePropertyChanged(object o, string propName)
                    // at bool System.Windows.WeakEventManager + ListenerList.DeliverEvent(object sender, EventArgs e, Type managerType)
                    // at void System.Windows.WeakEventManager.DeliverEventToList(object sender, EventArgs args, ListenerList list)
                    // at void System.ComponentModel.PropertyChangedEventManager.OnPropertyChanged(object sender, PropertyChangedEventArgs args)
                    // at void SIUI.ViewModel.TableInfoViewModel.set_Hint(string value)
                    // at async void SICore.ViewerHumanLogic.OnAtomHint(string hint) + (?) => { }
                }
            }
        }
    }

    private int _playerIndex = -1;

    /// <summary>
    /// Index of player who wins the button competition.
    /// </summary>
    [Obsolete("Implement players view model outside the table")]
    public int PlayerIndex
    {
        get => _playerIndex;
        set { _playerIndex = value; OnPropertyChanged(nameof(ActivePlayer)); }
    }

    [Obsolete("Implement players view model outside the table")]
    public string? ActivePlayer => (_playerIndex < 0 || _playerIndex >= Players.Count) ? "" : Players[_playerIndex].Name;

    /// <summary>
    /// Players lost the button chase.
    /// </summary>
    public ObservableCollection<string> LostButtonPlayers { get; } = new();

    private bool _animateText = false;

    // TODO: consider joining AnimateText and PartialText into an Enum property

    /// <summary>
    /// Should the question text be animated as it is reading aloud.
    /// </summary>
    public bool AnimateText
    {
        get => _animateText;
        set { _animateText = value; OnPropertyChanged(); }
    }

    private double _textSpeed = 0.05;

    /// <summary>
    /// Text reading speed (chars / second). Zero speed disables the reading.
    /// </summary>
    public double TextSpeed
    {
        get => _textSpeed;
        set
        {
            if (_textSpeed != value && _textSpeed >= 0.0)
            {
                _textSpeed = value;
                OnPropertyChanged();
            }
        }
    }

    private double _timeLeft = 1.0;

    /// <summary>
    /// Left time percentage for pressing game button (from 0.0 to 1.0).
    /// </summary>
    public double TimeLeft
    {
        get => _timeLeft;
        set { if (_timeLeft != value) { _timeLeft = value; OnPropertyChanged(); } }
    }

    private bool _selectable = false;

    /// <summary>
    /// Can a theme/question be selected right now.
    /// </summary>
    public bool Selectable
    {
        get => _selectable;
        set
        {
            if (_selectable != value)
            {
                _selectable = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _isEditable = false;

    /// <summary>
    /// Can the table be edited.
    /// </summary>
    public bool IsEditable
    {
        get => _isEditable;
        set
        {
            if (_isEditable != value)
            {
                _isEditable = value;
                OnPropertyChanged();
            }
        }
    }

    public SimpleCommand SelectQuestion { get; private set; }

    public SimpleCommand SelectTheme { get; private set; }

    public SimpleCommand ToggleQuestion { get; private set; }

    /// <summary>
    /// Selects question answer.
    /// </summary>
    public SimpleCommand SelectAnswer { get; private set; }

    public event Action<QuestionInfoViewModel>? QuestionSelected;
    public event Action<ThemeInfoViewModel>? ThemeSelected;
    public event Action<QuestionInfoViewModel>? QuestionToggled;
    public event Action<ItemViewModel>? AnswerSelected;

    public void SelectQuestion_Executed(object? arg) => QuestionSelected?.Invoke((QuestionInfoViewModel)arg);

    public void SelectTheme_Executed(object? arg) => ThemeSelected?.Invoke((ThemeInfoViewModel)arg);

    public void ToggleQuestion_Executed(object? arg) => QuestionToggled?.Invoke((QuestionInfoViewModel)arg);

    public void SelectAnswer_Executed(object? arg)
    {
        if (arg is not ItemViewModel itemViewModel)
        {
            return;
        }

        AnswerSelected?.Invoke(itemViewModel);
    }

    private IReadOnlyCollection<ContentViewModel>? _content;

    /// <summary>
    /// Table collection content.
    /// </summary>
    public IReadOnlyCollection<ContentViewModel>? Content
    {
        get => _content;
        set
        {
            _content = value;
            OnPropertyChanged();
        }
    }

    private MediaSource? _mediaSource;

    /// <summary>
    /// Multimedia source.
    /// </summary>
    public MediaSource? MediaSource
    {
        get => _mediaSource;
        set { _mediaSource = value; OnPropertyChanged(); }
    }

    private MediaSource? _soundSource;

    /// <summary>
    /// Background sound source.
    /// </summary>
    public MediaSource? SoundSource
    {
        get => _soundSource;
        set { _soundSource = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// Game themes collection.
    /// </summary>
    public List<string> GameThemes => _model.GameThemes;

    private QuestionContentType _questionContentType = QuestionContentType.Text;

    public QuestionContentType QuestionContentType
    {
        get => _questionContentType;
        set { if (_questionContentType != value) { _questionContentType = value; OnPropertyChanged(); } }
    }

    private QuestionStyle _questionStyle = QuestionStyle.Normal;

    public QuestionStyle QuestionStyle
    {
        get => _questionStyle;
        set { if (_questionStyle != value) { _questionStyle = value; OnPropertyChanged(); } }
    }

    private bool _sound = false;

    public bool Sound
    {
        get => _sound;
        set { _sound = value; OnPropertyChanged(); }
    }

    private bool _enabled = false;

    /// <summary>
    /// Is table interactive.
    /// </summary>
    public bool Enabled
    {
        get => _enabled;
        set { _enabled = value; OnPropertyChanged(); }
    }

    private double _volume = 0.5;

    /// <summary>
    /// Sound volume level.
    /// </summary>
    public double Volume
    {
        get => _volume;
        set
        {
            if (_volume != value)
            {
                var oldValue = _volume;
                _volume = value;
                VolumeChanged?.Invoke(_volume / oldValue);
            }
        }
    }

    public event Action<double>? VolumeChanged;

    /// <summary>
    /// Questions prices table.
    /// </summary>
    public IList<ThemeInfoViewModel> RoundInfo { get; } = new ObservableCollection<ThemeInfoViewModel>();

    public object RoundInfoLock { get; } = new object();

    /// <summary>
    /// Game players.
    /// </summary>
    [Obsolete("Implement players view model outside the table")]
    public IList<SimplePlayerInfo> Players { get; private set; } = new ObservableCollection<SimplePlayerInfo>();

    private SettingsViewModel _settings;

    public SettingsViewModel Settings { get => _settings; set { _settings = value; OnPropertyChanged(); } }

    private bool _partialText = false;

    public bool PartialText { get => _partialText; set { _partialText = value; OnPropertyChanged(); } }

    private LayoutMode _layoutMode = LayoutMode.Simple;

    /// <summary>
    /// Table layout mode.
    /// </summary>
    public LayoutMode LayoutMode
    {
        get => _layoutMode;
        set
        {
            if (_layoutMode != value)
            {
                _layoutMode = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Answer options.
    /// </summary>
    public AnswerOptionsViewModel AnswerOptions { get; } = new();

    public TableInfoViewModel()
    {
        _settings = new SettingsViewModel();

        Init();
    }

    [Obsolete("Implement players view model outside the table")]
    public TableInfoViewModel(IList<SimplePlayerInfo> players)
    {
        _settings = new SettingsViewModel();
        Players = players;

        Init();
    }

    public TableInfoViewModel(TableInfo model, SettingsViewModel settings)
    {
        _model = model;
        _settings = settings;

        Init();
    }

    private void Init()
    {
        SelectQuestion = new SimpleCommand(SelectQuestion_Executed);
        SelectTheme = new SimpleCommand(SelectTheme_Executed);
        ToggleQuestion = new SimpleCommand(ToggleQuestion_Executed);
        SelectAnswer = new SimpleCommand(SelectAnswer_Executed);
    }

    public void PlaySelection(int themeIndex)
    {
        RoundInfo[themeIndex].State = QuestionInfoStages.Blinking;
        RoundInfo[themeIndex].SilentFlashOut();
    }

    /// <summary>
    /// Plays simple question selection animation.
    /// </summary>
    /// <param name="themeIndex">Question theme index.</param>
    /// <param name="questionIndex">Question index.</param>
    public Task PlaySimpleSelectionAsync(int themeIndex, int questionIndex)
    {
        RoundInfo[themeIndex].Questions[questionIndex].State = QuestionInfoStages.Blinking;
        return RoundInfo[themeIndex].Questions[questionIndex].SilentFlashOutAsync();
    }

    /// <summary>
    /// Plays special question selection animation.
    /// </summary>
    /// <param name="themeIndex">Question theme index.</param>
    /// <param name="questionIndex">Question index.</param>
    /// <param name="setActive">Should question theme be highlighted.</param>
    public async Task PlayComplexSelectionAsync(int themeIndex, int questionIndex, bool setActive)
    {
        for (var k = 0; k < RoundInfo.Count; k++)
        {
            RoundInfo[k].Active = k == themeIndex && setActive;
        }

        RoundInfo[themeIndex].Questions[questionIndex].State = QuestionInfoStages.Blinking;
        await RoundInfo[themeIndex].Questions[questionIndex].SilentFlashOutAsync();

        lock (TStageLock)
        {
            if (_tStage == TableStage.RoundTable)
            {
                TStage = TableStage.Special;
            }
        }
    }

    public event Action? MediaStart;
    public event Action? MediaEnd;
    public event Action<double>? MediaProgress;

    public event Action<int>? MediaSeek;
    public event Action? MediaPause;
    public event Action? MediaResume;

    public event Action? MediaLoad;
    public event Action<Exception>? MediaLoadError;

    public void OnMediaStart() => MediaStart?.Invoke();

    public void OnMediaEnd() => MediaEnd?.Invoke();

    public bool HasMediaProgress() => MediaProgress != null;

    public void OnMediaProgress(double? progress) => MediaProgress?.Invoke(progress.Value);

    public void OnMediaSeek(int position) => MediaSeek?.Invoke(position);

    public void OnMediaResume() => MediaResume?.Invoke();

    public void OnMediaPause() => MediaPause?.Invoke();

    public void OnMediaLoad() => MediaLoad?.Invoke();

    public void OnMediaLoadError(Exception exc) => MediaLoadError?.Invoke(exc);
}

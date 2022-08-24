using SIUI.Model;
using SIUI.ViewModel.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace SIUI.ViewModel
{
    /// <summary>
    /// Информация для табло
    /// </summary>
    public sealed class TableInfoViewModel : ViewModelBase<TableInfo>
    {
        public event EventHandler Ready;

        private TableStage _tStage = TableStage.Void;

        /// <summary>
        /// Текущее состояние табло
        /// </summary>
        public TableStage TStage
        {
            get { return _tStage; }
            set { if (_tStage != value) { _tStage = value; OnPropertyChanged(); } }
        }

        public object TStageLock { get; } = new object();

        /// <summary>
        /// Пауза в игре
        /// </summary>
        public bool Pause
        {
            get { return _model.Pause; }
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
            get { return _isMediaStopped; }
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

        private string _text = "";

        /// <summary>
        /// Текст вопроса (заголовка)
        /// </summary>
        public string Text
        {
            get { return _text; }
            set { if (_text != value) { _text = value; OnPropertyChanged(); } }
        }

        private int _textLength;

        /// <summary>
        /// Длина текста. При использовании частичного текста свойство Text содержит не только частичный текст, но и форму остального текста вопроса.
        /// Её отображать не надо
        /// </summary>
        public int TextLength
        {
            get { return _textLength; }
            set { if (_textLength != value) { _textLength = value; OnPropertyChanged(); } }
        }

        private string _hint = "";

        /// <summary>
        /// Additional hint shown over other content with transparency for a limited time.
        /// </summary>
        public string Hint
        {
            get => _hint;
            set { if (_hint != value) { _hint = value; OnPropertyChanged(); } }
        }

        private int _playerIndex = -1;

        /// <summary>
        /// Номер выигравшего кнопку игрока
        /// </summary>
        public int PlayerIndex
        {
            get { return _playerIndex; }
            set { _playerIndex = value; OnPropertyChanged(nameof(ActivePlayer)); }
        }

        public string ActivePlayer => _playerIndex < 0 || _playerIndex >= Players.Count ? "" : Players[_playerIndex].Name;

        /// <summary>
        /// Players lost the button chase.
        /// </summary>
        public ObservableCollection<string> LostButtonPlayers { get; } = new ObservableCollection<string>();

        private bool _animateText = false;

        /// <summary>
        /// Анимировать ли текст вопроса
        /// </summary>
        public bool AnimateText
        {
            get { return _animateText; }
            set { _animateText = value; OnPropertyChanged(); }
        }

        private double _textSpeed = 0.05;

        /// <summary>
        /// Скорость чтения текста
        /// </summary>
        public double TextSpeed
        {
            get { return _textSpeed; }
            set { _textSpeed = value; OnPropertyChanged(); }
        }

        private double _timeLeft = 1.0;

        /// <summary>
        /// Оставшаяся доля времени на нажатие кнопки (от 0.0 до 1.0)
        /// </summary>
        public double TimeLeft
        {
            get { return _timeLeft; }
            set { if (_timeLeft != value) { _timeLeft = value; OnPropertyChanged(); } }
        }

        private bool _selectable = false;

        /// <summary>
        /// Можно ли выбирать на табло тему/вопрос
        /// </summary>
        public bool Selectable
        {
            get { return _selectable; }
            set
            {
                if (_selectable != value)
                {
                    _selectable = value;
                    OnPropertyChanged();
                }
            }
        }

        public SimpleCommand SelectQuestion { get; private set; }
        public SimpleCommand SelectTheme { get; private set; }

        public event Action<QuestionInfoViewModel> QuestionSelected;
        public event Action<ThemeInfoViewModel> ThemeSelected;

        public void SelectQuestion_Executed(object arg)
        {
            QuestionSelected?.Invoke((QuestionInfoViewModel)arg);
        }

        public void SelectTheme_Executed(object arg)
        {
            ThemeSelected?.Invoke((ThemeInfoViewModel)arg);
        }

        private bool _finished = false;

        public bool Finished
        {
            get => _finished;
            set
            {
                _finished = value;

                if (value)
                {
                    OnReady();
                }

                OnPropertyChanged();
            }
        }

        private void OnReady()
        {
            if (_isComplex)
            {
                lock (TStageLock)
                {
                    if (_tStage == TableStage.RoundTable)
                    {
                        TStage = TableStage.Special;
                    }
                }
            }

            Ready?.Invoke(this, EventArgs.Empty);
        }

        private MediaSource _mediaSource;

        /// <summary>
        /// Мультимедийный источник
        /// </summary>
        public MediaSource MediaSource
        {
            get { return _mediaSource; }
            set { _mediaSource = value; OnPropertyChanged(); }
        }

        private MediaSource _soundSource;

        /// <summary>
        /// Звуковой источник
        /// </summary>
        public MediaSource SoundSource
        {
            get { return _soundSource; }
            set { _soundSource = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Темы игры
        /// </summary>
        public List<string> GameThemes => _model.GameThemes;

        private QuestionContentType _questionContentType = QuestionContentType.Text;

        public QuestionContentType QuestionContentType
        {
            get { return _questionContentType; }
            set { _questionContentType = value; OnPropertyChanged(); }
        }

        private QuestionStyle _questionStyle = QuestionStyle.Normal;

        public QuestionStyle QuestionStyle
        {
            get { return _questionStyle; }
            set { _questionStyle = value; OnPropertyChanged(); }
        }

        private bool _sound = false;

        public bool Sound
        {
            get { return _sound; }
            set { _sound = value; OnPropertyChanged(); }
        }

        private bool _enabled = false;

        /// <summary>
        /// Можно ли выбирать что-то на табло
        /// </summary>
        public bool Enabled
        {
            get { return _enabled; }
            set { _enabled = value; OnPropertyChanged(); }
        }

        private double _volume = 0.5;

        /// <summary>
        /// Громкость звука
        /// </summary>
        public double Volume
        {
            get { return _volume; }
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

        public event Action<double> VolumeChanged;

        /// <summary>
        /// Стоимости вопросов в раунде
        /// </summary>
        public IList<ThemeInfoViewModel> RoundInfo { get; } = new ObservableCollection<ThemeInfoViewModel>();

        public object RoundInfoLock { get; } = new object();

        /// <summary>
        /// Список игроков, отображаемых на табло в особом режиме игры
        /// </summary>
        public IList<SimplePlayerInfo> Players { get; private set; } = new ObservableCollection<SimplePlayerInfo>();

        private SettingsViewModel _settings;

        public SettingsViewModel Settings { get => _settings; set { _settings = value; OnPropertyChanged(); } }

        private bool _partialText = false;

        public bool PartialText { get => _partialText; set { _partialText = value; OnPropertyChanged(); } }

        public TableInfoViewModel()
        {
            _settings = new SettingsViewModel();

            Init();
        }

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
        }

        public void PlaySelection(int i)
        {
            Finished = false;
            _isComplex = false;
            RoundInfo[i].State = QuestionInfoStages.Blinking;
            RoundInfo[i].SilentFlashOut();
        }

        /// <summary>
        /// Отобразить простой выбор
        /// </summary>
        /// <param name="i"></param>
        /// <param name="j"></param>
        public Task PlaySimpleSelectionAsync(int i, int j)
        {
            Finished = false;
            _isComplex = false;

            RoundInfo[i].Questions[j].State = QuestionInfoStages.Blinking;
            return RoundInfo[i].Questions[j].SilentFlashOutAsync();
        }

        /// <summary>
        /// Отобразить сложный выбор (спецвопроса)
        /// </summary>
        public Task PlayComplexSelectionAsync(int themeIndex, int questionIndex, bool setActive)
        {
            Finished = false;
            _isComplex = true;

            for (var k = 0; k < RoundInfo.Count; k++)
            {
                RoundInfo[k].Active = k == themeIndex && setActive;
            }

            RoundInfo[themeIndex].Questions[questionIndex].State = QuestionInfoStages.Blinking;
            return RoundInfo[themeIndex].Questions[questionIndex].SilentFlashOutAsync();
        }

        public event Action MediaStart;
        public event Action MediaEnd;
        public event Action<double> MediaProgress;

        public event Action<int> MediaSeek;
        public event Action MediaPause;
        public event Action MediaResume;

        public event Action<Exception> MediaLoadError;

        private bool _isComplex;

        public void OnMediaStart() => MediaStart?.Invoke();

        public void OnMediaEnd() => MediaEnd?.Invoke();

        public bool HasMediaProgress() => MediaProgress != null;

        public void OnMediaProgress(double? progress) => MediaProgress?.Invoke(progress.Value);

        public void OnMediaSeek(int position) => MediaSeek?.Invoke(position);

        public void OnMediaResume() => MediaResume?.Invoke();

        public void OnMediaPause() => MediaPause?.Invoke();

        public void OnMediaLoadError(Exception exc) => MediaLoadError?.Invoke(exc);
    }
}

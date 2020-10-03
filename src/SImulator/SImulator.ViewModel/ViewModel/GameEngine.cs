using SIEngine;
using SImulator.ViewModel.Model;
using SImulator.ViewModel.ButtonManagers;
using SImulator.ViewModel.Core;
using SImulator.ViewModel.PlatformSpecific;
using SImulator.ViewModel.Properties;
using SIPackages;
using SIPackages.Core;
using SIUI.ViewModel;
using SIUI.ViewModel.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SImulator.ViewModel
{
    /// <summary>
    /// Класс, хранящий общую игровую информацию и отвечающий за проведение игры
    /// </summary>
    public sealed class GameEngine: INotifyPropertyChanged, IDisposable
    {
        #region Fields
        private readonly bool _isRemoteControlling;

        internal event Action<string> Error;
        internal event Action RequestStop;

        private readonly Stack<Tuple<PlayerInfo, int, bool>> _answeringHistory = new Stack<Tuple<PlayerInfo, int, bool>>();

        private readonly EngineBase _engine;

        /// <summary>
        /// Менеджер игровых кнопок
        /// </summary>
        private IButtonManager _buttonManager;

        /// <summary>
        /// Менеджер записи лога
        /// </summary>
        private ILogger _logger;

        private readonly Timer _roundTimer;
        private readonly Timer _thinkingTimer;

        private bool _timerStopped;
        private bool _mediaStopped;

        private PlayerInfo _selectedPlayer = null;

        private readonly List<PlayerInfo> _selectedPlayers = new List<PlayerInfo>();
        private readonly Dictionary<Guid, PlayerInfo> _playersTable = new Dictionary<Guid, PlayerInfo>();

        #endregion

        #region Commands

        private readonly SimpleCommand _stop;
        private readonly SimpleUICommand _next;
        private readonly SimpleCommand _back;

        private readonly SimpleCommand _addRight;
        private readonly SimpleCommand _addWrong;

        private readonly SimpleCommand _nextRound;
        private readonly SimpleCommand _previousRound;

        public ICommand Stop => _stop;
        public ICommand Next => _next;
        public ICommand Back => _back;

        public ICommand RunRoundTimer { get; private set; }
        public ICommand StopRoundTimer { get; private set; }

        public ICommand RunQuestionTimer { get; private set; }
        public ICommand StopQuestionTimer { get; private set; }

        public ICommand RunMediaTimer { get; private set; }
        public ICommand StopMediaTimer { get; private set; }

        public ICommand AddPlayer { get; private set; }
        public ICommand RemovePlayer { get; private set; }
        public ICommand ClearPlayers { get; private set; }

        public ICommand AddRight => _addRight;
        public ICommand AddWrong => _addWrong;

        public ICommand NextRound => _nextRound;
        public ICommand PreviousRound => _previousRound;

        private ICommand _activeRoundCommand;

        public ICommand ActiveRoundCommand
        {
            get { return _activeRoundCommand; }
            set { _activeRoundCommand = value; OnPropertyChanged(); }
        }

        private ICommand _activeQuestionCommand;

        public ICommand ActiveQuestionCommand
        {
            get { return _activeQuestionCommand; }
            set { _activeQuestionCommand = value; OnPropertyChanged(); }
        }

        private ICommand _activeMediaCommand;

        public ICommand ActiveMediaCommand
        {
            get { return _activeMediaCommand; }
            set { if (_activeMediaCommand != value) { _activeMediaCommand = value; OnPropertyChanged(); } }
        }

        #endregion

        #region Properties

        public AppSettingsViewModel Settings { get; }

        /// <summary>
        /// Ссылка на интерфейсную часть. Может находиться в текущем процессе или на удалённом компьютере
        /// </summary>
        public IRemoteGameUI UserInterface { get; } = null;

        /// <summary>
        /// Список игроков, отображаемых на табло в особом режиме игры
        /// </summary>
        public TableInfoViewModel LocalInfo { get; set; }

        private bool _showingRoundThemes = false;

        private bool ShowingRoundThemes
        {
            set
            {
                if (_showingRoundThemes != value)
                {
                    _showingRoundThemes = value;
                    UpdateNextCommand();
                }
            }
        }

        private int _price;

        public int Price
        {
            get { return _price; }
            set { _price = value; OnPropertyChanged(); }
        }

        private int _rountTime = 0;

        public int RoundTime
        {
            get { return _rountTime; }
            set { _rountTime = value; OnPropertyChanged(); }
        }

        private int _questionTime = 0;

        public int QuestionTime
        {
            get { return _questionTime; }
            set { _questionTime = value; OnPropertyChanged(); }
        }

        private int _questionTimeMax = int.MaxValue;

        public int QuestionTimeMax
        {
            get { return _questionTimeMax; }
            set { _questionTimeMax = value; OnPropertyChanged(); }
        }

        private Round _activeRound;

        private Question _activeQuestion;

        public Question ActiveQuestion
        {
            get { return _activeQuestion; }
            set { _activeQuestion = value; OnPropertyChanged(); }
        }

        private Theme _activeTheme;

        public Theme ActiveTheme
        {
            get { return _activeTheme; }
            set { _activeTheme = value; OnPropertyChanged(); }
        }

        private int _mediaProgress;
        private bool _mediaProgressBlock = false;

        public int MediaProgress
        {
            get { return _mediaProgress; }
            set
            {
                if (_mediaProgress != value)
                {
                    _mediaProgress = value;
                    OnPropertyChanged();

                    if (!_mediaProgressBlock)
                    {
                        try
                        {
                            UserInterface.SeekMedia(_mediaProgress);
                        }
                        catch (TimeoutException exc)
                        {
                            PlatformManager.Instance.ShowMessage(string.Format("Ошибка связи: {0}", exc.Message));
                        }
                        catch (CommunicationException exc)
                        {
                            PlatformManager.Instance.ShowMessage(string.Format("Ошибка связи: {0}", exc.Message));
                        }

                        if (_gameHost.IsMediaEnded)
                        {
                            ActiveMediaCommand = StopMediaTimer;
                            _gameHost.IsMediaEnded = false;
                        }
                    }
                }
            }
        }

        private bool _isMediaControlled;

        public bool IsMediaControlled
        {
            get { return _isMediaControlled; }
            set
            {
                if (_isMediaControlled != value)
                {
                    _isMediaControlled = value;
                    OnPropertyChanged();
                }
            }
        }

        private readonly IExtendedGameHost _gameHost;

        #endregion
        
        public GameEngine(AppSettingsViewModel settings, EngineBase engine, IExtendedGameHost gameHost, IRemoteGameUI ui, IList<SimplePlayerInfo> players, bool isRemoteControlling)
        {
            Settings = settings;
            _engine = engine;
            _gameHost = gameHost;
            UserInterface = ui;
            _isRemoteControlling = isRemoteControlling;

            LocalInfo = new TableInfoViewModel(players);

            foreach (PlayerInfo item in LocalInfo.Players)
            {
                item.IsRegistered = false;
                item.PropertyChanged += Info_PropertyChanged;
            }

            LocalInfo.QuestionSelected += QuestionInfo_Selected;
            LocalInfo.ThemeSelected += ThemeInfo_Selected;

            #region Command creation

            _gameHost.Next = _next = new SimpleUICommand(Next_Executed) { Name = "Дальше" };
            _gameHost.Back = _back = new SimpleCommand(Back_Executed) { CanBeExecuted = false };
            _gameHost.Stop = _stop = new SimpleCommand(Stop_Executed);

            AddPlayer = new SimpleCommand(AddPlayer_Executed);
            RemovePlayer = new SimpleCommand(RemovePlayer_Executed);
            ClearPlayers = new SimpleCommand(ClearPlayers_Executed);
            _addRight = new SimpleCommand(AddRight_Executed) { CanBeExecuted = false };
            _addWrong = new SimpleCommand(AddWrong_Executed) { CanBeExecuted = false };

            RunRoundTimer = new SimpleUICommand(RunRoundTimer_Executed) { Name = "Запустить" };
            StopRoundTimer = new SimpleUICommand(StopRoundTimer_Executed) { Name = "Приостановить" };

            RunQuestionTimer = new SimpleUICommand(RunQuestionTimer_Executed) { Name = "Запустить" };
            StopQuestionTimer = new SimpleUICommand(StopQuestionTimer_Executed) { Name = "Приостановить" };

            RunMediaTimer = new SimpleUICommand(RunMediaTimer_Executed) { Name = "Запустить" };
            StopMediaTimer = new SimpleUICommand(StopMediaTimer_Executed) { Name = "Приостановить" };

            _gameHost.NextRound = _nextRound = new SimpleCommand(NextRound_Executed) { CanBeExecuted = false };
            _gameHost.PreviousRound = _previousRound = new SimpleCommand(PreviousRound_Executed) { CanBeExecuted = false };

            #endregion

            UpdateNextCommand();

            _roundTimer = new Timer(RoundTimer_Elapsed, null, Timeout.Infinite, Timeout.Infinite);
            _thinkingTimer = new Timer(ThinkingTimer_Elapsed, null, Timeout.Infinite, Timeout.Infinite);

            settings.Model.SIUISettings.PropertyChanged += Default_PropertyChanged;
            settings.SIUISettings.PropertyChanged += Default_PropertyChanged;

            _engine.Package += Engine_Package;
            _engine.GameThemes += Engine_GameThemes;
            _engine.NextRound += Engine_NextRound;
            _engine.Round += Engine_Round;
            _engine.RoundThemes += Engine_RoundThemes;
            _engine.Theme += Engine_Theme;
            _engine.Question += Engine_Question;
            _engine.QuestionSelected += Engine_QuestionSelected;

            _engine.QuestionAtom += Engine_QuestionAtom;
            _engine.QuestionText += Engine_QuestionText;
            _engine.QuestionOral += Engine_QuestionOral;
            _engine.QuestionImage += Engine_QuestionImage;
            _engine.QuestionSound += Engine_QuestionSound;
            _engine.QuestionVideo += Engine_QuestionVideo;
            _engine.QuestionOther += Engine_QuestionOther;
            _engine.QuestionProcessed += Engine_QuestionProcessed;
            _engine.WaitTry += Engine_WaitTry;

            _engine.SimpleAnswer += Engine_SimpleAnswer;
            _engine.RightAnswer += Engine_RightAnswer;
            _engine.ShowScore += Engine_ShowScore;
            _engine.LogScore += LogScore;
            _engine.EndQuestion += Engine_EndQuestion;
            _engine.RoundTimeout += Engine_RoundTimeout;
            _engine.NextQuestion += Engine_NextQuestion;
            _engine.RoundEmpty += Engine_RoundEmpty;
            _engine.FinalThemes += Engine_FinalThemes;
            _engine.ThemeSelected += Engine_ThemeSelected;
            _engine.PrepareFinalQuestion += Engine_PrepareFinalQuestion;
            _engine.Error += OnError;
            _engine.EndGame += Engine_EndGame;

            _engine.PropertyChanged += engine_PropertyChanged;

            _gameHost.MediaStart += GameHost_MediaStart;
            _gameHost.MediaProgress += GameHost_MediaProgress;
            _gameHost.MediaEnd += GameHost_MediaEnd;
            _gameHost.RoundThemesFinished += GameHost_RoundThemesFinished;
            _gameHost.ThemeDeleted += GameHost_ThemeDeleted;
        }

        private void GameHost_ThemeDeleted(int themeIndex)
        {
            LocalInfo.RoundInfo[themeIndex].Name = null;
        }

        private void GameHost_RoundThemesFinished()
        {
            ShowingRoundThemes = false;
            UserInterface.SetStage(TableStage.RoundTable);
        }

        private void GameHost_MediaEnd()
        {
            ActiveMediaCommand = RunMediaTimer;

            if ((!Settings.Model.FalseStart || !Settings.Model.FalseStartMultimedia || Settings.Model.UsePlayersKeys == PlayerKeysModes.None) && _engine.IsQuestionFinished())
            {
                if (Settings.Model.ThinkingTime > 0)
                {
                    // Запуск таймера при игре на мультимедиа без фальстартов
                    // С фальстартами - по дополнительному нажатию кнопки
                    QuestionTimeMax = Settings.Model.ThinkingTime;
                    RunQuestionTimer_Executed(0);
                }
            }
        }

        private void GameHost_MediaProgress(double progress)
        {
            _mediaProgressBlock = true;
            try
            {
                MediaProgress = (int)(progress * 100);
            }
            finally
            {
                _mediaProgressBlock = false;
            }
        }

        private void GameHost_MediaStart()
        {
            IsMediaControlled = true;
        }

        #region Event handlers

        private void Default_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            try
            {
                if (UserInterface != null)
                    UserInterface.UpdateSettings(Settings.SIUISettings.Model);
            }
            catch (TimeoutException exc)
            {
                PlatformManager.Instance.ShowMessage(string.Format("Ошибка связи: {0}", exc.Message));
            }
            catch (CommunicationException exc)
            {
                PlatformManager.Instance.ShowMessage(string.Format("Ошибка связи: {0}", exc.Message));
            }
            catch (ObjectDisposedException exc)
            {
                PlatformManager.Instance.ShowMessage(string.Format("Ошибка связи: {0}", exc.Message));
            }
        }

        private void Info_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PlayerInfo.IsSelected) || e.PropertyName == nameof(PlayerInfo.IsRegistered))
                return;

            var player = (PlayerInfo)sender;

            if (e.PropertyName == nameof(PlayerInfo.WaitForRegistration))
            {
                if (player.WaitForRegistration)
                {
                    foreach (PlayerInfo item in LocalInfo.Players)
                    {
                        if (item != sender)
                            item.WaitForRegistration = false;
                    }
                }

                return;
            }

            try
            {
                if (UserInterface != null)
                    UserInterface.UpdatePlayerInfo(LocalInfo.Players.IndexOf(player), player);
            }
            catch (TimeoutException exc)
            {
                PlatformManager.Instance.ShowMessage(string.Format("Ошибка связи: {0}", exc.Message));
            }
            catch (CommunicationException exc)
            {
                PlatformManager.Instance.ShowMessage(string.Format("Ошибка связи: {0}", exc.Message));
            }
            catch (ObjectDisposedException)
            {

            }
        }

        private void ThinkingTimer_Elapsed(object state)
        {
            Task.Factory.StartNew(() =>
                {
                    try
                    {
                        QuestionTime++;
                        if (QuestionTime >= QuestionTimeMax)
                        {
                            UserInterface.SetSound(Settings.Model.Sounds.NoAnswer);
                            StopQuestionTimer_Executed(null);
                            ActiveQuestionCommand = null;

                            if (!Settings.Model.SignalsAfterTimer && _buttonManager != null)
                                _buttonManager.Stop();
                        }
                    }
                    catch (TimeoutException exc)
                    {
                        PlatformManager.Instance.ShowMessage($"{Resources.ConnectionError}: {exc.Message}");
                    }
                    catch (CommunicationException exc)
                    {
                        PlatformManager.Instance.ShowMessage($"{Resources.ConnectionError}: {exc.Message}");
                    }
                    catch (Exception exc)
                    {
                        PlatformManager.Instance.ShowMessage($"{Resources.Error}: {exc.Message}");
                    }
                }, CancellationToken.None, TaskCreationOptions.None, UI.Scheduler);
        }

        private void RoundTimer_Elapsed(object state)
        {
            UI.Execute(() =>
                {
                    RoundTime++;
                    if (RoundTime >= Settings.Model.RoundTime)
                    {
                        _engine.SetTimeout();
                        StopRoundTimer_Executed(null);
                    }
                }, exc => OnError(exc.ToString()));
        }

        private void ThemeInfo_Selected(ThemeInfoViewModel theme)
        {
            int themeIndex;
            for (themeIndex = 0; themeIndex < LocalInfo.RoundInfo.Count; themeIndex++)
            {
                if (LocalInfo.RoundInfo[themeIndex] == theme)
                    break;
            }

            _gameHost.OnThemeSelected(themeIndex);
        }

        private void QuestionInfo_Selected(QuestionInfoViewModel question)
        {
            if (!((TvEngine)_engine).CanSelectQuestion || _showingRoundThemes)
                return;

            int questionIndex = -1;
            int themeIndex;
            for (themeIndex = 0; themeIndex < LocalInfo.RoundInfo.Count; themeIndex++)
            {
                bool found = false;
                for (questionIndex = 0; questionIndex < LocalInfo.RoundInfo[themeIndex].Questions.Count; questionIndex++)
                {
                    if (LocalInfo.RoundInfo[themeIndex].Questions[questionIndex] == question)
                    {
                        found = true;
                        break;
                    }
                }

                if (found)
                    break;
            }

            _gameHost.OnQuestionSelected(themeIndex, questionIndex);
        }

        #endregion

        #region Command handlers

        private void NextRound_Executed(object arg)
        {
            try
            {
                StopRoundTimer_Executed(0);
                StopQuestionTimer_Executed(0);
                _engine.MoveNextRound();
            }
            catch (TimeoutException exc)
            {
                PlatformManager.Instance.ShowMessage(string.Format("Ошибка связи: {0}", exc.Message));
            }
            catch (CommunicationException exc)
            {
                PlatformManager.Instance.ShowMessage(string.Format("Ошибка связи: {0}", exc.Message));
            }
        }

        private void PreviousRound_Executed(object arg)
        {
            try
            {
                StopRoundTimer_Executed(0);
                StopQuestionTimer_Executed(0);
                ActiveRoundCommand = null;
                UserInterface.SetStage(TableStage.Sign);

                _engine.MoveBackRound();
            }
            catch (TimeoutException exc)
            {
                PlatformManager.Instance.ShowMessage(string.Format("Ошибка связи: {0}", exc.Message));
            }
            catch (CommunicationException exc)
            {
                PlatformManager.Instance.ShowMessage(string.Format("Ошибка связи: {0}", exc.Message));
            }
        }

        private void RunRoundTimer_Executed(object arg)
        {
            if (arg != null)
                RoundTime = 0;

            _roundTimer.Change(1000, 1000);

            ActiveRoundCommand = StopRoundTimer;
        }

        private void StopRoundTimer_Executed(object arg)
        {
            if (arg != null)
                RoundTime = 0;

            _roundTimer.Change(Timeout.Infinite, Timeout.Infinite);
            
            ActiveRoundCommand = RunRoundTimer;
        }

        private void RunQuestionTimer_Executed(object arg)
        {
            if (arg != null)
                QuestionTime = 0;

            _thinkingTimer.Change(1000, 1000);

            ActiveQuestionCommand = StopQuestionTimer;
        }

        private void StopQuestionTimer_Executed(object arg)
        {
            if (arg != null)
                QuestionTime = 0;

            _thinkingTimer.Change(Timeout.Infinite, Timeout.Infinite);

            ActiveQuestionCommand = RunQuestionTimer;
        }

        private void RunMediaTimer_Executed(object arg)
        {
            try
            {
                UserInterface.RunMedia();
                ActiveMediaCommand = StopMediaTimer;
            }
            catch (TimeoutException exc)
            {
                PlatformManager.Instance.ShowMessage(string.Format("Ошибка связи: {0}", exc.Message));
            }
            catch (CommunicationException exc)
            {
                PlatformManager.Instance.ShowMessage(string.Format("Ошибка связи: {0}", exc.Message));
            }
        }

        private void StopMediaTimer_Executed(object arg)
        {
            try
            {
                UserInterface.StopMedia();
                ActiveMediaCommand = RunMediaTimer;
            }
            catch (TimeoutException exc)
            {
                PlatformManager.Instance.ShowMessage(string.Format("Ошибка связи: {0}", exc.Message));
            }
            catch (CommunicationException exc)
            {
                PlatformManager.Instance.ShowMessage(string.Format("Ошибка связи: {0}", exc.Message));
            }
        }

        private void AddPlayer_Executed(object arg)
        {
            var info = new PlayerInfo();
            LocalInfo.Players.Add(info);

            info.PropertyChanged += Info_PropertyChanged;

            try
            {
                if (UserInterface != null)
                    UserInterface.AddPlayer();
            }
            catch (TimeoutException exc)
            {
                PlatformManager.Instance.ShowMessage(string.Format("Ошибка связи: {0}", exc.Message));
            }
            catch (CommunicationException exc)
            {
                PlatformManager.Instance.ShowMessage(string.Format("Ошибка связи: {0}", exc.Message));
            }
        }

        private void RemovePlayer_Executed(object arg)
        {
            if (!(arg is SimplePlayerInfo player))
                return;

            player.PropertyChanged -= Info_PropertyChanged;
            LocalInfo.Players.Remove(player);

            try
            {
                if (UserInterface != null)
                    UserInterface.RemovePlayer(player.Name);
            }
            catch (TimeoutException exc)
            {
                PlatformManager.Instance.ShowMessage(string.Format("Ошибка связи: {0}", exc.Message));
            }
            catch (CommunicationException exc)
            {
                PlatformManager.Instance.ShowMessage(string.Format("Ошибка связи: {0}", exc.Message));
            }
        }

        private void ClearPlayers_Executed(object arg)
        {
            LocalInfo.Players.Clear();
            try
            {
                UserInterface.ClearPlayers();
            }
            catch (TimeoutException exc)
            {
                PlatformManager.Instance.ShowMessage(string.Format("Ошибка связи: {0}", exc.Message));
            }
            catch (CommunicationException exc)
            {
                PlatformManager.Instance.ShowMessage(string.Format("Ошибка связи: {0}", exc.Message));
            }
        }

        private void AddRight_Executed(object arg)
        {
            if (!(arg is PlayerInfo player))
            {
                if (_selectedPlayer == null)
                {
                    return;
                }

                player = _selectedPlayer;
            }

            player.Right++;
            player.Sum += Price;

            UserInterface.SetSound(Settings.Model.Sounds.AnswerRight);

            _logger.Write("{0} +{1}", player.Name, Price);

            _answeringHistory.Push(Tuple.Create(player, Price, true));

            if (Settings.Model.EndQuestionOnRightAnswer)
            {
                Next_Executed();
            }
            else
            {
                ReturnToQuestion();
            }
        }

        private void AddWrong_Executed(object arg)
        {
            if (!(arg is PlayerInfo player))
            {
                if (_selectedPlayer == null)
                {
                    return;
                }

                player = _selectedPlayer;
            }

            player.Wrong++;

            var substract = Settings.Model.SubstractOnWrong ? Price : 0;
            player.Sum -= substract;

            UserInterface.SetSound(Settings.Model.Sounds.AnswerWrong);

            _logger.Write("{0} -{1}", player.Name, substract);

            _answeringHistory.Push(Tuple.Create(player, Price, false));

            ReturnToQuestion();
        }

        internal void Start()
        {
            UpdateNextCommand();

            try
            {
                UserInterface.ClearPlayers();

                for (int i = 0; i < LocalInfo.Players.Count; i++)
                {
                    UserInterface.AddPlayer();
                    UserInterface.UpdatePlayerInfo(i, (PlayerInfo)LocalInfo.Players[i]);
                }

                UserInterface.Start();
            }
            catch (TimeoutException exc)
            {
                PlatformManager.Instance.ShowMessage(string.Format("Ошибка связи: {0}", exc.Message));
            }
            catch (CommunicationException exc)
            {
                PlatformManager.Instance.ShowMessage(string.Format("Ошибка связи: {0}", exc.Message));
            }

            ShowingRoundThemes = false;

            _buttonManager = PlatformManager.Instance.ButtonManagerFactory.Create(Settings.Model);
            if (_buttonManager != null)
            {
                _buttonManager.KeyPressed += OnPlayerKeyPressed;
                _buttonManager.PlayerPressed += OnPlayerPressed;
                _buttonManager.GetPlayerByGuid += OnGetPlayerByGuid;
            }

            if (Settings.Model.SaveLogs)
            {
                var logsFolder = Settings.Model.LogsFolder;
                if (string.IsNullOrWhiteSpace(logsFolder))
                {
                    PlatformManager.Instance.ShowMessage("Папка для записи логов не задана! Логи вестись не будут.");
                    _logger = PlatformManager.Instance.CreateLogger(null);
                }
                else
                {
                    try
                    {
                        _logger = PlatformManager.Instance.CreateLogger(logsFolder);
                    }
                    catch (Exception exc)
                    {
                        PlatformManager.Instance.ShowMessage(string.Format("Ошибка создания файла лога: {0}.\r\n\r\nЛог записываться не будет.", exc.Message), false);
                        _logger = PlatformManager.Instance.CreateLogger(null);
                    }
                }
            }
            else
            {
                _logger = PlatformManager.Instance.CreateLogger(null);
            }

            _logger.Write("Игра начата {0}", DateTime.Now);
            _logger.Write("Пакет: {0}", _engine.PackageName);

            _selectedPlayers.Clear();
            UserInterface.ClearLostButtonPlayers();

            if (Settings.Model.AutomaticGame)
                Next_Executed();
        }

        private void Engine_Question(Question question)
        {
            UserInterface.SetText(question.Price.ToString());
            UserInterface.SetStage(TableStage.QuestionPrice);

            LocalInfo.Text = question.Price.ToString();
            LocalInfo.TStage = TableStage.QuestionPrice;
            ActiveQuestion = question;
        }

        private void Engine_Theme(Theme theme)
        {
            UserInterface.SetText("Тема: " + theme.Name);
            UserInterface.SetStage(TableStage.Theme);

            LocalInfo.Text = "Тема: " + theme.Name;
            LocalInfo.TStage = TableStage.Theme;

            ActiveTheme = theme;
        }

        private void Engine_EndGame()
        {
            UserInterface.SetStage(TableStage.Sign);
        }

        private void Stop_Executed(object arg = null)
        {
            RequestStop?.Invoke();
        }

        void engine_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(EngineBase.CanMoveNext):
                    UpdateNextCommand();
                    break;

                case nameof(EngineBase.CanMoveBack):
                    _back.CanBeExecuted = _engine.CanMoveBack;
                    break;

                case nameof(EngineBase.CanMoveNextRound):
                    _nextRound.CanBeExecuted = _engine.CanMoveNextRound;
                    break;

                case nameof(EngineBase.CanMoveBackRound):
                    _previousRound.CanBeExecuted = _engine.CanMoveBackRound;
                    break;

                case "Stage":
                    _addRight.CanBeExecuted = _addWrong.CanBeExecuted = _engine.CanChangeSum();
                    break;
            }
        }

        /// <summary>
        /// Следующий шаг игры
        /// </summary>
        /// <param name="arg"></param>
        private void Next_Executed(object arg = null)
        {
            try
            {
                if (_showingRoundThemes)
                {
                    UserInterface.SetStage(TableStage.RoundTable);
                    ShowingRoundThemes = false;
                    return;
                }

                _engine.MoveNext();
            }
            catch (TimeoutException exc)
            {
                PlatformManager.Instance.ShowMessage(string.Format("Ошибка связи: {0}", exc.Message));
            }
            catch (CommunicationException exc)
            {
                PlatformManager.Instance.ShowMessage(string.Format("Ошибка связи: {0}", exc.Message));
            }
            catch (Exception exc)
            {
                PlatformManager.Instance.ShowMessage(string.Format("Ошибка: {0}", exc.Message));
            }
        }

        private async void Engine_Package(Package package)
        {
            if (UserInterface == null)
                return;

            var videoUrl = Settings.Model.VideoUrl;
            if (!string.IsNullOrWhiteSpace(videoUrl))
            {
                await SetMedia(new Media(videoUrl));
                UserInterface.SetStage(TableStage.Question);
                UserInterface.SetQuestionContentType(QuestionContentType.Video);
            }
            else
            {
                UserInterface.SetSound(Settings.Model.Sounds.BeginGame);
            }

            LocalInfo.TStage = TableStage.Sign;
        }

        private void Engine_GameThemes(string[] themes)
        {
            UserInterface.SetGameThemes(themes);
            LocalInfo.TStage = TableStage.GameThemes;

            UserInterface.SetSound(Settings.Model.Sounds.GameThemes);
        }

        private void Engine_Round(Round round)
        {
            _activeRound = round ?? throw new ArgumentNullException(nameof(round));

            if (UserInterface == null)
                return;

            UserInterface.SetText(round.Name);
            UserInterface.SetStage(TableStage.Round);
            UserInterface.SetSound(Settings.Model.Sounds.RoundBegin);
            LocalInfo.TStage = TableStage.Round;

            _logger.Write("\r\nРаунд {0}", round.Name);

            if (round.Type == RoundTypes.Standart)
            {
                if (Settings.Model.RoundTime > 0)
                {
                    RunRoundTimer_Executed(0);
                }
            }
        }

        private void Engine_RoundThemes(Theme[] roundThemes)
        {
            LocalInfo.RoundInfo.Clear();

            _logger.Write("Темы раунда:");

            int maxQuestion = roundThemes.Max(theme => theme.Questions.Count);
            foreach (var theme in roundThemes)
            {
                var themeInfo = new ThemeInfoViewModel() { Name = theme.Name };
                LocalInfo.RoundInfo.Add(themeInfo);

                _logger.Write(theme.Name);

                for (int i = 0; i < maxQuestion; i++)
                {
                    var questionInfo = new QuestionInfoViewModel() { Price = i < theme.Questions.Count ? theme.Questions[i].Price : -1 };
                    themeInfo.Questions.Add(questionInfo);
                }
            }

            UserInterface.SetRoundThemes(LocalInfo.RoundInfo.ToArray(), false);
            UserInterface.SetSound(Settings.Model.Sounds.RoundThemes);
            ShowingRoundThemes = true;
            LocalInfo.TStage = TableStage.RoundTable;
        }

        private void Engine_QuestionProcessed(Question question, bool finished, bool pressMode)
        {
            if (question.Type.Name == QuestionTypes.Simple && _buttonManager != null)
                _buttonManager.Run(); // Кнопки активируются заранее, чтобы работали фальстарты

            if (finished)
            {
                if (!pressMode && Settings.Model.ThinkingTime > 0 && ActiveMediaCommand == null)
                {
                    // Запуск таймера при игре:
                    // 1) без фальстартов - на фальстартах он активируется доп. нажатием (см. WaitTry)
                    // 2) не мультимедиа-элемент - таймер будет запущен только по его завершении
                    QuestionTimeMax = Settings.Model.ThinkingTime + question.Scenario.ToString().Length / 20;
                    RunQuestionTimer_Executed(0);
                }
            }
        }

        private void Engine_WaitTry(Question question, bool final)
        {
            if (final)
            {
                UserInterface.SetSound(Settings.Model.Sounds.FinalThink);
                return;
            }

            if (question.Type.Name == QuestionTypes.Simple)
            {
                UserInterface.SetQuestionStyle(QuestionStyle.WaitingForPress);
            }

            if (ActiveMediaCommand == StopMediaTimer)
            {
                StopMediaTimer_Executed(null);
                MediaProgress = 100;
            }

            if (Settings.Model.ThinkingTime > 0)
            {
                // Запуск таймера при игре с фальстартами
                QuestionTimeMax = Settings.Model.ThinkingTime;
                RunQuestionTimer_Executed(0);
            }
        }

        private void Engine_RightAnswer()
        {
            StopQuestionTimer.Execute(0);

            if (_buttonManager != null)
                _buttonManager.Stop();

            UserInterface.SetQuestionStyle(QuestionStyle.Normal);
            UserInterface.SetSound("");
        }

        private void Engine_RoundEmpty()
        {
            StopRoundTimer_Executed(0);
        }

        private void Engine_NextQuestion()
        {
            if (Settings.Model.GameMode == GameModes.Tv)
            {
                UserInterface.SetStage(TableStage.RoundTable);
                LocalInfo.TStage = TableStage.RoundTable;
            }
            else
                Next_Executed();
        }

        private void Engine_RoundTimeout()
        {
            UserInterface.SetSound(Settings.Model.Sounds.RoundTimeout);
            _logger.Write("Время раунда вышло.");
        }

        private void Engine_EndQuestion(int themeIndex, int questionIndex)
        {
            StopQuestionTimer_Executed(0);

            if (_buttonManager != null)
                _buttonManager.Stop();

            UnselectPlayer();
            _selectedPlayers.Clear();

            foreach (var player in LocalInfo.Players)
            {
                ((PlayerInfo)player).BlockedTime = null;
            }

            ActiveQuestionCommand = null;
            ActiveMediaCommand = null;

            if (themeIndex > -1 && themeIndex < LocalInfo.RoundInfo.Count)
            {
                var themeInfo = LocalInfo.RoundInfo[themeIndex];
                if (questionIndex > -1 && questionIndex < themeInfo.Questions.Count)
                    themeInfo.Questions[questionIndex].Price = -1;
            }
        }

        private void Engine_FinalThemes(Theme[] finalThemes)
        {
            LocalInfo.RoundInfo.Clear();

            foreach (var theme in finalThemes)
            {
                if (theme.Questions.Count == 0)
                    continue;

                var themeInfo = new ThemeInfoViewModel() { Name = theme.Name };
                LocalInfo.RoundInfo.Add(themeInfo);
            }

            UserInterface.SetRoundThemes(LocalInfo.RoundInfo.ToArray(), true);
            UserInterface.SetSound("");
            LocalInfo.TStage = TableStage.Final;
        }

        private void Engine_SimpleAnswer(string answer)
        {
            UserInterface.SetText(answer);
            UserInterface.SetStage(TableStage.Answer);
            UserInterface.SetSound("");
        }

        /// <summary>
        /// "Шагнуть" назад
        /// </summary>
        private void Back_Executed(object arg = null)
        {
            var data = _engine.MoveBack();

            if (Settings.Model.GameMode == GameModes.Tv)
            {
                LocalInfo.RoundInfo[data.Item1].Questions[data.Item2].Price = data.Item3;

                UserInterface.RestoreQuestion(data.Item1, data.Item2, data.Item3);
                UserInterface.SetStage(TableStage.RoundTable);
                LocalInfo.TStage = TableStage.RoundTable;
            }
            else
            {
                UserInterface.SetText(data.Item3.ToString());
                UserInterface.SetStage(TableStage.QuestionPrice);
            }

            StopQuestionTimer_Executed(0);

            if (_buttonManager != null)
                _buttonManager.Stop();

            UnselectPlayer();
            _selectedPlayers.Clear();

            foreach (var player in LocalInfo.Players)
            {
                ((PlayerInfo)player).BlockedTime = null;
            }

            ActiveQuestionCommand = null;
            ActiveMediaCommand = null;

            _engine.UpdateCanNext();

            if (Settings.Model.DropStatsOnBack)
            {
                while (_answeringHistory.Count > 0)
                {
                    var item = _answeringHistory.Pop();
                    if (item == null)
                        break;

                    if (item.Item3)
                    {
                        item.Item1.Right--;
                        item.Item1.Sum -= item.Item2;
                    }
                    else
                    {
                        item.Item1.Wrong--;
                        item.Item1.Sum += item.Item2;
                    }
                }
            }
        }

        #endregion

        private void Engine_ThemeSelected(int themeIndex)
        {
            UserInterface.PlaySelection(themeIndex);
            UserInterface.SetSound(Settings.Model.Sounds.FinalDelete);
        }

        private void UpdateNextCommand()
        {
            _next.CanBeExecuted = _engine != null && _engine.CanMoveNext || _showingRoundThemes;
        }

        private void Engine_ShowScore()
        {
            UserInterface.SetStage(TableStage.Score);
            LocalInfo.TStage = TableStage.Score;
        }

        private void ReturnToQuestion()
        {
            try
            {
                if (Settings.Model.UsePlayersKeys != PlayerKeysModes.None && _engine.CanReturnToQuestion())
                {
                    if (_timerStopped)
                    {
                        RunQuestionTimer_Executed(null);
                    }

                    UnselectPlayer();

                    if (Settings.Model.FalseStart && (_activeMediaCommand == null || Settings.Model.FalseStartMultimedia) && _engine.CanPress())
                    {
                        UserInterface.SetQuestionStyle(QuestionStyle.WaitingForPress);
                    }
                    else
                    {
                        UserInterface.SetQuestionStyle(QuestionStyle.Normal);

                        if (_mediaStopped)
                        {
                            RunMediaTimer_Executed(null);
                        }
                    }
                }
            }
            catch (TimeoutException exc)
            {
                PlatformManager.Instance.ShowMessage(string.Format("Ошибка связи: {0}", exc.Message));
            }
            catch (CommunicationException exc)
            {
                PlatformManager.Instance.ShowMessage(string.Format("Ошибка связи: {0}", exc.Message));
            }
        }

        /// <summary>
        /// Завершить игру
        /// </summary>
        public void Dispose()
        {
            try
            {
                lock (_engine.SyncRoot)
                {
                    _engine.PropertyChanged -= engine_PropertyChanged;
                    _engine.Dispose();

                    if (_buttonManager != null)
                    {
                        _buttonManager.Stop();
                        _buttonManager.Dispose();
                        _buttonManager = null;
                    }

                    if (_logger != null)
                    {
                        _logger.Dispose();
                        _logger = null;
                    }

                    try
                    {
                        UserInterface.SetSound("");
                    }
                    catch (CommunicationException) { }
                    catch (TimeoutException) { }
                    catch (ObjectDisposedException) { }

                    PlatformManager.Instance.ClearMedia();

                    StopRoundTimer_Executed(null);
                    _roundTimer.Dispose();

                    StopQuestionTimer_Executed(0);
                    _thinkingTimer.Dispose();
                }
            }
            catch (Exception exc)
            {
                PlatformManager.Instance.ShowMessage(string.Format("Ошибка завершения игры: {0}", exc.Message));
            }
        }

        private void Engine_PrepareFinalQuestion(Theme theme, Question question)
        {
            ActiveTheme = theme;
            ActiveQuestion = question;

            UserInterface.SetSound("");
        }

        /// <summary>
        /// Вывести счёт в лог
        /// </summary>
        private void LogScore()
        {
            if (Settings.Model.SaveLogs && LocalInfo.Players.Count > 0)
            {
                var sb = new StringBuilder("\r\nСчёт: ");
                var first = true;
                foreach (var player in LocalInfo.Players)
                {
                    if (!first)
                        sb.Append(", ");

                    first = false;
                    sb.AppendFormat("{0}:{1}", player.Name, player.Sum);
                }

                _logger.Write(sb.ToString());
            }
        }

        private void Engine_NextRound(bool showSign)
        {
            ActiveRoundCommand = null;
            UserInterface.SetSound("");

            if (showSign)
                UserInterface.SetStage(TableStage.Sign);
        }

        private void Engine_QuestionOther(Atom atom)
        {
            UserInterface.SetText("");
            UserInterface.SetQuestionSound(false);

            ActiveMediaCommand = null;
        }

        private void InitMedia()
        {
            ActiveMediaCommand = StopMediaTimer;
            IsMediaControlled = false;
            MediaProgress = 0;
        }

        private async void Engine_QuestionSound(IMedia sound)
        {
            await SetMedia(sound, true);

            UserInterface.SetQuestionSound(true);
            UserInterface.SetQuestionContentType(QuestionContentType.None);

            InitMedia();
        }

        private async Task SetMedia(IMedia media, bool background = false)
        {
            if (_isRemoteControlling)
            {
                if (UserInterface != null)
                {
                    if (media.GetStream != null)
                    {
                        UserInterface.ClearBuffer();

                        var buffer = new byte[32768];
                        int i = 0;
                        while ((i = media.GetStream().Stream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            if (i < buffer.Length)
                                Array.Resize(ref buffer, i);

                            UserInterface.AppendToBuffer(buffer);
                        }

                        UserInterface.SetMediaFromBuffer(media.Uri, background);
                    }
                }
            }
            else
            {
                var mediaPrepared = await PlatformManager.Instance.PrepareMedia(media);
                if (UserInterface != null && mediaPrepared != null)
                    UserInterface.SetMedia(new MediaSource(mediaPrepared.GetStream?.Invoke().Stream, mediaPrepared.Uri), background);
            }
        }

        private async void Engine_QuestionVideo(IMedia video)
        {
            await SetMedia(video);

            UserInterface.SetQuestionSound(false);
            UserInterface.SetQuestionContentType(QuestionContentType.Video);

            InitMedia();
        }

        private async void Engine_QuestionImage(IMedia image, IMedia sound)
        {
            await SetMedia(image);
            UserInterface.SetQuestionContentType(QuestionContentType.Image);

            UserInterface.SetQuestionSound(sound != null);

            if (sound != null)
                await SetMedia(sound, true);

            if (sound != null)
                InitMedia();
            else
                ActiveMediaCommand = null;
        }

        private async void Engine_QuestionText(string text, IMedia sound)
        {
            // Если без фальстартов, то выведем тему и стоимость
            var displayedText = Settings.Model.FalseStart || Settings.Model.ShowTextNoFalstart || _activeRound?.Type == RoundTypes.Final ? text
                : $"{ActiveTheme?.Name}\n{ActiveQuestion?.Price}";

            UserInterface.SetText(displayedText);

            UserInterface.SetQuestionContentType(QuestionContentType.Text);

            if (sound != null)
                await SetMedia(sound, true);

            UserInterface.SetQuestionSound(sound != null);

            if (sound != null)
                InitMedia();
            else
                ActiveMediaCommand = null;
        }

        private void Engine_QuestionOral(string oralText)
        {
            UserInterface.SetText("");
            UserInterface.SetQuestionSound(false);

            ActiveMediaCommand = null;
        }

        private void Engine_QuestionAtom(Atom atom)
        {
            LocalInfo.TStage = TableStage.Question;
            UserInterface.SetStage(TableStage.Question);
        }

        private void Engine_QuestionSelected(int themeIndex, int questionIndex, Theme theme, Question question)
        {
            _answeringHistory.Push(null);

            ActiveTheme = theme;
            ActiveQuestion = question;

            LogScore();
            _logger.Write("\r\n{0}, {1}", theme.Name, question.Price);

            if (question.Type.Name == QuestionTypes.Simple)
            {
                UserInterface.SetSound(Settings.Model.Sounds.QuestionSelected);
                UserInterface.PlaySimpleSelection(themeIndex, questionIndex);
            }
            else
            {
                var setActive = true;
                switch (question.Type.Name)
                {
                    case QuestionTypes.Cat:
                    case QuestionTypes.BagCat:
                        UserInterface.SetSound(Settings.Model.Sounds.SecretQuestion);
                        UserInterface.SetText("ВОПРОС С СЕКРЕТОМ");
                        _logger.Write("ВОПРОС С СЕКРЕТОМ");
                        setActive = false;
                        break;

                    case QuestionTypes.Auction:
                        UserInterface.SetSound(Settings.Model.Sounds.StakeQuestion);
                        UserInterface.SetText("ВОПРОС СО СТАВКОЙ");
                        _logger.Write("ВОПРОС СО СТАВКОЙ");
                        break;

                    case QuestionTypes.Sponsored:
                        UserInterface.SetSound(Settings.Model.Sounds.NoRiskQuestion);
                        UserInterface.SetText("ВОПРОС БЕЗ РИСКА");
                        _logger.Write("ВОПРОС БЕЗ РИСКА");
                        setActive = false;
                        break;

                    default:
                        UserInterface.SetText(question.Type.Name);
                        break;
                }

                LocalInfo.TStage = TableStage.Special;
                UserInterface.PlayComplexSelection(themeIndex, questionIndex, setActive);
            }

            _logger.Write(question.Scenario.ToString());
            Price = question.Price;
        }

        private void OnError(string error)
        {
            Error?.Invoke(error);
        }

        internal PlayerInfo OnGetPlayerByGuid(Guid guid, bool strict)
        {
            if (Settings.Model.UsePlayersKeys == PlayerKeysModes.Web)
            {
                lock (_playersTable)
                {
                    if (_playersTable.TryGetValue(guid, out PlayerInfo player))
                        return player;

                    if (!strict)
                    {
                        foreach (PlayerInfo item in LocalInfo.Players)
                        {
                            if (item.WaitForRegistration)
                            {
                                item.WaitForRegistration = false;
                                item.IsRegistered = true;

                                _playersTable[guid] = item;

                                return item;
                            }
                        }
                    }
                }
            }

            return null;
        }

        internal bool OnPlayerKeyPressed(GameKey key)
        {
            var index = Settings.Model.PlayerKeys2.IndexOf(key);
            if (index == -1 || index >= LocalInfo.Players.Count)
                return false;

            var player = (PlayerInfo)LocalInfo.Players[index];

            return ProcessPlayerPress(index, player);
        }

        internal bool OnPlayerPressed(PlayerInfo player)
        {
            // Нет такого игрока
            var index = LocalInfo.Players.IndexOf(player);
            if (index == -1)
                return false;

            return ProcessPlayerPress(index, player);
        }

        private bool ProcessPlayerPress(int index, PlayerInfo player)
        {
            if (!_engine.IsWaitingForPress())
                return false;

            // Уже кто-то отвечает
            if (_selectedPlayer != null)
            {
                if (Settings.Model.ShowLostButtonPlayers && _selectedPlayer != player && !_selectedPlayers.Contains(player))
                {
                    UserInterface.AddLostButtonPlayer(player.Name);
                }

                return false;
            }

            // Уже нажимал
            if (_selectedPlayers.Contains(player))
                return false;

            // Не время нажимать
            if (_engine.IsQuestion() && Settings.Model.FalseStart && (_activeMediaCommand == null || Settings.Model.FalseStartMultimedia))
            {
                player.BlockedTime = DateTime.Now;
                return false;
            }

            // Заблокирован
            if (player.BlockedTime.HasValue && DateTime.Now.Subtract(player.BlockedTime.Value).TotalSeconds < Settings.Model.BlockingTime)
                return false;

            // Все проверки пройдены, фиксируем нажатие
            player.IsSelected = true;
            _selectedPlayer = player;
            _selectedPlayers.Add(_selectedPlayer);

            try
            {
                UserInterface.SetSound(Settings.Model.Sounds.PlayerPressed);
                UserInterface.SetPlayer(index);
            }
            catch (Exception exc) when (exc is TimeoutException || exc is CommunicationException)
            {
                PlatformManager.Instance.ShowMessage($"{Resources.ConnectionError}: {exc.Message}");
            }

            _timerStopped = ActiveQuestionCommand == StopQuestionTimer;
            if (_timerStopped)
            {
                StopQuestionTimer.Execute(null);
            }

            _mediaStopped = ActiveMediaCommand == StopMediaTimer;
            if (_mediaStopped)
            {
                StopMediaTimer_Executed(null);
            }

            return true;
        }

        private void UnselectPlayer()
        {
            if (_selectedPlayer != null)
            {
                _selectedPlayer.IsSelected = false;
                _selectedPlayer = null;
            }

            if (Settings.Model.ShowLostButtonPlayers)
            {
                UserInterface.ClearLostButtonPlayers();
            }            
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        internal void CloseMainView()
        {
            try
            {
                if (UserInterface != null)
                    UserInterface.StopGame();
            }
            catch (TimeoutException)
            {

            }
            catch (CommunicationException)
            {

            }
        }
    }
}

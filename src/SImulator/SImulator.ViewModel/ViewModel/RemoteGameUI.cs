﻿using System;
using System.Linq;
using System.ServiceModel;
using System.IO;
using System.Windows.Input;
using System.ServiceModel.Channels;
using SIUI.ViewModel;
using SIUI.ViewModel.Core;
using System.Threading.Tasks;
using System.Threading;
using SImulator.ViewModel.Core;
using SImulator.Model;
using SImulator.ViewModel.PlatformSpecific;
using SIPackages.Core;

namespace SImulator.ViewModel
{
    /// <summary>
    /// Класс, реализующий удалённое проведение игры
    /// </summary>
    public class RemoteGameUI : IRemoteGameUI
    {
        private int _previousCode = -1;

        private IGameHost _gameHost;

        internal IGameHost GameHost
        {
            get { return _gameHost; }
            set
            {
                if (_gameHost != value)
                {
                    _gameHost = value;

                    if (_gameHost != null)
                    {
                        TInfo.MediaStart += () =>
                            {
                                try
                                {
                                    _gameHost.OnMediaStart();
                                }
                                catch (CommunicationObjectAbortedException)
                                {

                                }
                            };

                        TInfo.MediaEnd += () =>
                            {
                                try
                                {
                                    _gameHost.OnMediaEnd();
                                }
                                catch (ObjectDisposedException)
                                {

                                }
                            };
                        TInfo.MediaProgress += progress =>
                        {
                            try
                            {
                                _gameHost.OnMediaProgress(progress);
                            }
                            catch (CommunicationObjectAbortedException)
                            {

                            }
                        };
                    }
                }
            }
        }
        
        public TableInfoViewModel TInfo { get; private set; }

        public ICommand Next { get; private set; }
        public ICommand Back { get; private set; }
        public ICommand NextRound { get; private set; }
        public ICommand BackRound { get; private set; }
        public ICommand Stop { get; private set; }

        private string _sound;

        private bool _stageCallbackBlock = false;

        public int ScreenIndex { get; set; }

        public RemoteGameUI()
        {
            TInfo = new TableInfoViewModel { Enabled = true };

            TInfo.TStage = TableStage.Sign;
            TInfo.PropertyChanged += TInfo_PropertyChanged;
            TInfo.Ready += TInfo_Ready;

            TInfo.QuestionSelected += QuestionInfo_Selected;
            TInfo.ThemeSelected += ThemeInfo_Selected;

            Next = new SimpleCommand(arg => { if (_gameHost != null) _gameHost.AskNext(); });
            Back = new SimpleCommand(arg => { if (_gameHost != null) _gameHost.AskBack(); });
            NextRound = new SimpleCommand(arg => { if (_gameHost != null) _gameHost.AskNextRound(); });
            BackRound = new SimpleCommand(arg => { if (_gameHost != null) _gameHost.AskBackRound(); });
            Stop = new SimpleCommand(arg => { if (_gameHost != null) _gameHost.AskStop(); });
        }

        private void TInfo_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TableInfoViewModel.TStage))
            {
                if (TInfo.TStage == TableStage.RoundTable)
                {
                    SetSound();

                    if (GameHost != null && !_stageCallbackBlock)
                        GameHost.OnRoundThemesFinished();
                }
            }
        }

        private void TInfo_Ready(object sender, EventArgs e)
        {
            if (GameHost != null)
                GameHost.OnReady();
        }

        public void SetSound(string sound = "")
        {
            _sound = sound;
            Play();
        }

        private void Play()
        {
            if (TaskScheduler.Current != UI.Scheduler)
            {
                Task.Factory.StartNew(Play, CancellationToken.None, TaskCreationOptions.None, UI.Scheduler);
                return;
            }

            PlatformManager.Instance.PlaySound(_sound, SoundFinished);
        }

        private void SoundFinished()
        {
            if (TInfo.TStage == TableStage.Sign)
                _gameHost.OnIntroFinished();
        }

        public async void Start()
        {
            if (_gameHost == null)
            {
                // Удалённое воспроизведение
                GameHost = PlatformManager.Instance.GetCallback<IGameHost>();
                ((IChannel)GameHost).Closed += RemoteGameUI_Closed;
                ((IChannel)GameHost).Faulted += RemoteGameUI_Closed;
            }

            await PlatformManager.Instance.CreateMainView(this, ScreenIndex);
            TInfo.TStage = TableStage.Sign;
        }

        void RemoteGameUI_Closed(object sender, EventArgs e)
        {
            Task.Factory.StartNew(StopGame, CancellationToken.None, TaskCreationOptions.None, UI.Scheduler);
        }

        public async void StopGame()
        {
            await PlatformManager.Instance.CloseMainView();

            lock (TInfo.TStageLock)
            {
                TInfo.TStage = TableStage.Void;
            }

            SetSound();
            GameHost = null;
        }

        private MemoryStream _buffer = null;

        public void ClearBuffer()
        {
            if (_buffer != null)
                _buffer.Dispose();

            _buffer = new MemoryStream();
        }

        public void AppendToBuffer(byte[] data)
        {
            _buffer.Write(data, 0, data.Length);
        }

        public async void SetMediaFromBuffer(string uri, bool background)
        {
            _buffer.Position = 0;
            var media = new Media(() => new StreamInfo { Stream = _buffer, Length = _buffer.Length }, uri);
            var mediaPrepared = await PlatformManager.Instance.PrepareMedia(media);
            if (mediaPrepared == null)
                return;

            var source = new MediaSource(mediaPrepared.GetStream().Stream, mediaPrepared.Uri);

            if (background)
                TInfo.SoundSource = source;
            else
                TInfo.MediaSource = source;
        }

        public void SetMedia(MediaSource media, bool background)
        {
            if (background)
                TInfo.SoundSource = media;
            else
                TInfo.MediaSource = media;
        }

        public void SetGameThemes(string[] themes)
        {
            TInfo.GameThemes.Clear();
            TInfo.GameThemes.AddRange(themes);

            lock (TInfo.TStageLock)
            {
                TInfo.TStage = TableStage.GameThemes;
            }
        }
        
        public void SetStage(TableStage stage)
        {
            if (stage == TableStage.RoundTable)
                _stageCallbackBlock = true;

            lock (TInfo.TStageLock)
            {
                TInfo.TStage = stage;
            }

            if (stage == TableStage.RoundTable)
            {
                _stageCallbackBlock = false;
                _previousCode = -1;
                TInfo.QuestionStyle = QuestionStyle.Normal;
            }
        }
        
        public void SetText(string text)
        {
            TInfo.Text = text;
        }

        public void SetQuestionContentType(QuestionContentType questionContentType)
        {
            TInfo.QuestionContentType = questionContentType;
        }

        public void SetQuestionStyle(QuestionStyle questionStyle)
        {
            TInfo.QuestionStyle = questionStyle;
        }

        public void SetQuestionSound(bool sound)
        {
            TInfo.Sound = sound;
        }

        public void AddPlayer()
        {
            TInfo.Players.Add(new SimplePlayerInfo());
        }

        public void RemovePlayer(string playerName)
        {
            var player = TInfo.Players.FirstOrDefault(info => info.Name == playerName);
            if (player != null)
                TInfo.Players.Remove(player);
        }

        public void ClearPlayers()
        {
            TInfo.Players.Clear();
        }

        public void UpdatePlayerInfo(int index, PlayerInfo player)
        {
            if (index > -1 && index < TInfo.Players.Count)
            {
                var p = TInfo.Players[index];
                p.Sum = player.Sum;
                p.Name = player.Name;
            }
        }

        public void SetRoundThemes(ThemeInfoViewModel[] themes, bool isFinal)
        {
            TInfo.RoundInfo.Clear();
            foreach (var theme in themes)
            {
                TInfo.RoundInfo.Add(theme);
            }

            lock (TInfo.TStageLock)
            {
                TInfo.TStage = isFinal ? TableStage.Final : TableStage.RoundThemes;
            }
        }

        void ThemeInfo_Selected(ThemeInfoViewModel theme)
        {
            int themeIndex;
            for (themeIndex = 0; themeIndex < TInfo.RoundInfo.Count; themeIndex++)
            {
                if (TInfo.RoundInfo[themeIndex] == theme)
                    break;
            }

            GameHost.OnThemeSelected(themeIndex);
        }

        void QuestionInfo_Selected(QuestionInfoViewModel question)
        {
            lock (TInfo.TStageLock)
            {
                if (TInfo.TStage != TableStage.RoundTable)
                    return;
            }

            int questionIndex = -1;
            int themeIndex;
            for (themeIndex = 0; themeIndex < TInfo.RoundInfo.Count; themeIndex++)
            {
                bool found = false;
                for (questionIndex = 0; questionIndex < TInfo.RoundInfo[themeIndex].Questions.Count; questionIndex++)
                {
                    if (TInfo.RoundInfo[themeIndex].Questions[questionIndex] == question)
                    {
                        found = true;
                        break;
                    }
                }

                if (found)
                    break;
            }

            GameHost.OnQuestionSelected(themeIndex, questionIndex);
        }
        
        public void PlaySimpleSelection(int theme, int quest)
        {
            TInfo.PlaySimpleSelection(theme, quest);
        }

        public void PlayComplexSelection(int theme, int quest, bool setActive)
        {
            TInfo.PlayComplexSelection(theme, quest, setActive);
        }

        public void PlaySelection(int theme)
        {
            TInfo.PlaySelection(theme);
        }
        
        public void UpdateSettings(Settings settings)
        {
            TInfo.Settings.Initialize(settings);
        }

        public bool OnKeyPressed(GameKey key)
        {
            lock (TInfo.TStageLock)
            {
                switch (TInfo.TStage)
                {
                    case TableStage.RoundTable:
                        if (TInfo.Settings.Model.KeyboardControl)
                        {
                            if (_previousCode > -1 && PlatformManager.Instance.IsEscapeKey(key))
                            {
                                _previousCode = -1;
                                return true;
                            }

                            var code = PlatformManager.Instance.GetKeyNumber(key);
                            if (code == -1)
                            {
                                _previousCode = -1;
                                return false;
                            }

                            if (_previousCode == -1)
                            {
                                _previousCode = code;
                                return true;
                            }
                            else
                            {
                                if (_previousCode < TInfo.RoundInfo.Count && code < TInfo.RoundInfo[_previousCode].Questions.Count
                                    && TInfo.RoundInfo[_previousCode].Questions[code].Price > -1)
                                {
                                    GameHost.OnQuestionSelected(_previousCode, code);
                                    _previousCode = -1;
                                    return true;
                                }

                                _previousCode = -1;
                            }

                        }
                        break;

                    case TableStage.Final:
                        if (TInfo.Settings.Model.KeyboardControl)
                        {
                            var code = PlatformManager.Instance.GetKeyNumber(key);
                            if (code == -1)
                                return false;

                            if (code < TInfo.RoundInfo.Count && TInfo.RoundInfo[code].Name != null)
                            {
                                GameHost.OnThemeSelected(code);
                                return true;
                            }
                        }
                        break;
                }
            }
            return false;
        }
        
        public void SetPlayer(int playerIndex)
        {
            TInfo.PlayerIndex = playerIndex;
            TInfo.QuestionStyle = QuestionStyle.Pressed;
        }

        public void AddLostButtonPlayer(string name)
        {
            lock (TInfo.LostButtonPlayers)
            {
                if (!TInfo.LostButtonPlayers.Contains(name))
                {
                    TInfo.LostButtonPlayers.Add(name);
                }
            }
        }

        public void ClearLostButtonPlayers()
        {
            lock (TInfo.LostButtonPlayers)
            {
                TInfo.LostButtonPlayers.Clear();
            }
        }

        public void SeekMedia(int position)
        {
            TInfo.OnMediaSeek(position);
        }

        public void RunMedia()
        {
            TInfo.OnMediaResume();
        }

        public void StopMedia()
        {
            TInfo.OnMediaPause();
        }

        public void RestoreQuestion(int themeIndex, int questionIndex, int price)
        {
            TInfo.RoundInfo[themeIndex].Questions[questionIndex].Price = price;
        }
    }
}

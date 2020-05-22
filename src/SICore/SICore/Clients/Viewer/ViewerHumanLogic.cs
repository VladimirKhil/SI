using SICore.Connections;
using SICore.Network;
using SIData;
using SIPackages.Core;
using SIUI.ViewModel;
using SIUI.ViewModel.Core;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using R = SICore.Properties.Resources;

namespace SICore
{
    /// <summary>
    /// Логика зрителя-человека
    /// </summary>
    public abstract class ViewerHumanLogic<P> : Logic<P, ViewerData>, IViewer
        where P : IViewerClient
    {
        private bool _disposed = false;

        public TableInfoViewModel TInfo { get; }

        protected ViewerHumanLogic(P client, ViewerData data)
            : base(client, data)
        {
            TInfo = new TableInfoViewModel(_data.TInfo, _data.BackLink.GetSettings()) { AnimateText = true, Enabled = true };

            TInfo.PropertyChanged += TInfo_PropertyChanged;
            TInfo.Ready += TInfo_Ready;

            TInfo.MediaLoadError += TInfo_MediaLoadError;
        }

        private void TInfo_MediaLoadError(Exception exc) =>
            _data.OnAddString(null, $"MEDIA {TInfo.MediaSource?.Uri} LOAD ERROR: {exc}{Environment.NewLine}", LogMode.Log);

        private void TInfo_Ready(object sender, EventArgs e)
        {
            if (TInfo.TStage == TableStage.RoundTable || TInfo.TStage == TableStage.Special || TInfo.TStage == TableStage.Question)
            {
                lock (_data.ChoiceLock)
                lock (_data.TInfoLock)
                lock (TInfo.RoundInfoLock)
                {
                    if (_data.ThemeIndex > -1 && _data.ThemeIndex < _data.TInfo.RoundInfo.Count && _data.QuestionIndex > -1 && _data.QuestionIndex < _data.TInfo.RoundInfo[_data.ThemeIndex].Questions.Count)
                        TInfo.RoundInfo[_data.ThemeIndex].Questions[_data.QuestionIndex].Price = -1;
                }
            }
            else
            {
                lock (_data.ChoiceLock)
                lock (_data.TInfoLock)
                lock (TInfo.RoundInfoLock)
                {
                    if (_data.ThemeIndex > -1 && _data.ThemeIndex < _data.TInfo.RoundInfo.Count)
                        TInfo.RoundInfo[_data.ThemeIndex].Name = null;
                }
            }
        }

        void TInfo_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TableInfoViewModel.TStage))
            {
                if (TInfo.TStage == TableStage.RoundTable)
                    _data.BackLink.PlaySound();
            }
        }

        #region ViewerInterface Members

        virtual public void ReceiveText(Message m)
        {
            _data.AddToChat(m);
            if (_data.BackLink.MakeLogs)
                AddToFileLog(m);
        }

        private static readonly XmlReaderSettings ReaderSettings = new XmlReaderSettings { ConformanceLevel = ConformanceLevel.Fragment };

        /// <summary>
        /// Вывод текста в протокол и в форму
        /// </summary>
        /// <param name="text">Выводимый текст</param>
        virtual public void Print(string text)
        {
            var chatMessageBuilder = new StringBuilder();
            var logMessageBuilder = new StringBuilder();

            var isPrintable = false;
            var special = false;

            try
            {
                using var reader = new StringReader(text);
                using var xmlReader = XmlReader.Create(reader, ReaderSettings);

                xmlReader.Read();
                while (!xmlReader.EOF)
                {
                    if (xmlReader.NodeType == XmlNodeType.Element)
                    {
                        ParseMessageToPrint(xmlReader, chatMessageBuilder, logMessageBuilder, ref isPrintable, ref special);
                    }
                    else
                    {
                        xmlReader.Read();
                    }
                }
            }
            catch (XmlException exc)
            {
                throw new Exception($"{_actor.LO[nameof(R.StringParseError)]} {text}.", exc);
            }

            var toFormStr = chatMessageBuilder.ToString();
            if (isPrintable)
            {
                var pair = toFormStr.Split(':');
                var speech = (pair.Length > 1 && pair[0].Length + 2 < toFormStr.Length) ? toFormStr.Substring(pair[0].Length + 2) : toFormStr;

                if (_data.Speaker != null)
                {
                    _data.Speaker.Replic = "";
                }

                _data.Speaker = _data.MainPersons.FirstOrDefault(item => item.Name == pair[0]);
                if (_data.Speaker != null)
                {
                    _data.Speaker.Replic = speech.Trim();
                }
            }

            if (_data.BackLink.TranslateGameToChat || special)
            {
                _data.OnAddString(null, toFormStr, LogMode.Protocol);
            }

            if (_data.BackLink.MakeLogs)
            {
                AddToFileLog(logMessageBuilder.ToString());
            }
        }

        internal void AddToFileLog(Message message) =>
            AddToFileLog($"<span style=\"color: gray; font-weight: bold\">{message.Sender}:</span> <span style=\"font-weight: bold\">{message.Text}</span><br />");

        internal void AddToFileLog(string text)
        {
            if (_data.ProtocolWriter == null)
            {
                if (_data.ProtocolPath != null)
                {
                    try
                    {
                        var stream = _data.BackLink.CreateLog(_actor.Client.Name, out var path);
                        _data.ProtocolPath = path;
                        _data.ProtocolWriter = new StreamWriter(stream);
                        _data.ProtocolWriter.Write(text);
                    }
                    catch (IOException)
                    {
                    }
                }
            }
            else
            {
                try
                {
                    _data.ProtocolWriter.Write(text);
                }
                catch (IOException exc)
                {
                    _data.OnAddString(null, _actor.LO[nameof(R.ErrorWritingLogToDisc)] + ": " + exc.Message, LogMode.Log);
                    try
                    {
                        _data.ProtocolWriter.Dispose();
                    }
                    catch
                    {
                        // Из-за недостатка места на диске плохо закрывается
                    }

                    _data.ProtocolPath = null;
                    _data.ProtocolWriter = null;
                }
            }
        }

        private void ParseMessageToPrint(XmlReader reader, StringBuilder chatMessageBuilder,
            StringBuilder logMessageBuilder, ref bool isPrintable, ref bool special)
        {
            var name = reader.Name;
            var content = reader.ReadElementContentAsString();

            switch (name)
            {
                case "this.client":
                    chatMessageBuilder.AppendFormat("{0}: ", content);
                    logMessageBuilder.AppendFormat("<span style=\"color: #646464; font-weight: bold\">{0}: </span>", content);
                    break;

                case "player":
                    {
                        isPrintable = true;

                        var n = int.Parse(content);
                        string s;
                        if (n == 0)
                            s = "<span style=\"color: #EF21A9; font-weight:bold\">";
                        else if (n == 1)
                            s = "<span style=\"color: #0BE6CF; font-weight:bold\">";
                        else if (n == 2)
                            s = "<span style=\"color: #EF9F21; font-weight:bold\">";
                        else if (n == 3)
                            s = "<span style=\"color: #FF0000; font-weight:bold\">";
                        else if (n == 4)
                            s = "<span style=\"color: #00FF00; font-weight:bold\">";
                        else if (n == 5)
                            s = "<span style=\"color: #0000FF; font-weight:bold\">";
                        else if (n < Constants.MaxPlayers)
                            s = "<span style=\"color: #00FFFF; font-weight:bold\">";
                        else
                        {
                            _data.SystemLog.AppendLine(_actor.LO[nameof(R.BadTextInLog)] + ": " + logMessageBuilder);
                            return;
                        }

                        var playerName = n < _data.Players.Count ? _data.Players[n].Name : "<" + _actor.LO[nameof(R.UnknownPerson)] + ">";
                        chatMessageBuilder.AppendFormat("{0}: ", playerName);
                        logMessageBuilder.AppendFormat("{0}{1}: </span>", s, playerName);
                    }
                    break;

                case "showman":
                    isPrintable = true;
                    chatMessageBuilder.AppendFormat("{0}: ", content);
                    logMessageBuilder.AppendFormat("<span style=\"color: #0AEA2A; font-weight: bold\">{0}: </span>", content);
                    break;

                case "replic":
                    chatMessageBuilder.Append(content);
                    logMessageBuilder.AppendFormat("<span style=\"font-weight: bold\">{0}</span>", content);
                    break;

                case "system":
                    chatMessageBuilder.Append(content);
                    logMessageBuilder.AppendFormat("<span style=\"font-style: italic\">{0}</span>", content);
                    break;

                case "special":
                    special = true;
                    chatMessageBuilder.AppendFormat("* {0}", content.ToUpper());
                    logMessageBuilder.AppendFormat("<span style=\"font-style: italic; font-weight: bold\">{0}</span>", content);
                    break;

                case "line":
                    chatMessageBuilder.Append('\r');
                    logMessageBuilder.Append("<br />");
                    break;
            }
        }

        virtual public void Stage()
        {
            switch (_data.Stage)
            {
                case GameStage.Before:
                    break;

                case GameStage.Begin:
                    TInfo.TStage = TableStage.Sign;

                    if (_data.BackLink.MakeLogs && _data.ProtocolWriter == null)
                    {
                        try
                        {
                            var stream = _data.BackLink.CreateLog(_actor.Client.Name, out string path);
                            _data.ProtocolPath = path;
                            _data.ProtocolWriter = new StreamWriter(stream);
                            _data.ProtocolWriter.Write("<!DOCTYPE html><html><head><meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\"/><title>" + _actor.LO[nameof(R.LogTitle)] + "</title></head><body style=\"font-face: Verdana\">");
                        }
                        catch (IOException)
                        {

                        }
                        catch (ArgumentException exc)
                        {
                            _data.BackLink.OnError(exc);
                        }
                        catch (UnauthorizedAccessException exc)
                        {
                            _data.BackLink.OnError(exc);
                        }
                    }

                    Print(ReplicManager.Special(_actor.LO[nameof(R.GameStarted)] + " " + DateTime.Now.ToString()));
                    break;

                case GameStage.Round:
                case GameStage.Final:
                    TInfo.TStage = TableStage.Round;
                    _data.Sound = "beginround";

                    foreach (var item in _data.Players)
                    {
                        item.State = PlayerState.None;
                        item.Stake = 0;

                        item.SafeStake = false;
                    }
                    break;

                case GameStage.After:
                    if (_data.ProtocolWriter != null)
                        _data.ProtocolWriter.Write("</body></html>");
                    else
                        _data.OnAddString(null, _actor.LO[nameof(R.ErrorWritingLogs)], LogMode.Chat);
                    break;

                default:
                    break;
            }

        }

        virtual public void GameThemes()
        {
            TInfo.TStage = TableStage.GameThemes;
        }

        virtual public void RoundThemes(bool print)
        {
            if (SynchronizationContext.Current == null)
            {
                ExecuteInUIThread(() => RoundThemesUI(print)).Wait();
                return;
            }

            RoundThemesUI(print);
        }

        private void RoundThemesUI(bool print)
        {
            lock (_data.TInfoLock)
            lock (TInfo.RoundInfoLock)
            {
                TInfo.RoundInfo.Clear();

                foreach (var item in _data.TInfo.RoundInfo.Select(themeInfo => new ThemeInfoViewModel(themeInfo)))
                {
                    TInfo.RoundInfo.Add(item);
                }
            }

            if (print)
            {
                if (_data.Stage == GameStage.Round)
                {
                    TInfo.TStage = TableStage.RoundThemes;
                    _data.BackLink.PlaySound("cathegories");
                }
                else
                    TInfo.TStage = TableStage.Final;
            }
        }

        private Task ExecuteInUIThread(Action action)
        {
            if (UI.Scheduler == null)
            {
                try
                {
                    action();
                }
                catch (Exception exc)
                {
                    _data.BackLink.SendError(exc);
                }

                return Task.CompletedTask;
            }

            return Task.Factory.StartNew(
                () =>
                {
                    try
                    {
                        action();
                    }
                    catch (Exception exc)
                    {
                        _data.BackLink.SendError(exc);
                    }
                }, CancellationToken.None, TaskCreationOptions.None, UI.Scheduler);
        }

        virtual public async void Choice()
        {
            TInfo.Text = "";
            TInfo.MediaSource = null;
            TInfo.QuestionContentType = QuestionContentType.Text;
            TInfo.Sound = false;

            foreach (var item in _data.Players)
            {
                item.State = PlayerState.None;
            }

            var select = false;

            lock (_data.ChoiceLock)
            {
                lock (TInfo.RoundInfoLock)
                {
                    if (_data.ThemeIndex > -1 && _data.ThemeIndex < TInfo.RoundInfo.Count &&
                        _data.QuestionIndex > -1 && _data.QuestionIndex < TInfo.RoundInfo[_data.ThemeIndex].Questions.Count)
                    {
                        select = true;
                    }
                }
            }

            if (!select)
            {
                return;
            }

            try
            {
                await TInfo.PlaySimpleSelectionAsync(_data.ThemeIndex, _data.QuestionIndex);
            }
            catch (Exception exc)
            {
                _actor.Client.CurrentServer.OnError(exc, false);
            }
        }

        public void TextShape(string[] mparams)
        {
            var text = new StringBuilder();
            for (var i = 1; i < mparams.Length; i++)
            {
                text.Append(mparams[i]);
                if (i < mparams.Length - 1)
                {
                    text.Append('\n');
                }
            }

            TInfo.TextLength = 0;
            TInfo.PartialText = true;
            TInfo.Text = text.ToString();
        }

        virtual public void SetAtom(string[] mparams)
        {
            _data._atomType = mparams[1];

            var isPartial = _data._atomType == Constants.PartialText;

            if (isPartial)
            {
                if (!_data.IsPartial)
                {
                    _data.IsPartial = true;
                    _data.AtomIndex++;
                }
            }
            else
            {
                _data.IsPartial = false;
                _data.AtomIndex++;
                TInfo.Text = "";
                TInfo.PartialText = false;
            }

            TInfo.TStage = TableStage.Question;
            TInfo.IsMediaStopped = false;

            switch (_data._atomType)
            {
                case AtomTypes.Text:
                case Constants.PartialText:
                    var text = new StringBuilder();
                    for (int i = 2; i < mparams.Length; i++)
                    {
                        text.Append(mparams[i]);
                        if (i < mparams.Length - 1)
                        {
                            text.Append('\n');
                        }
                    }

                    if (isPartial)
                    {
                        var currentText = TInfo.Text;
                        var newTextLength = text.Length;

                        var tailIndex = TInfo.TextLength + newTextLength;

                        TInfo.Text = currentText.Substring(0, TInfo.TextLength)
                            + text
                            + (currentText.Length > tailIndex ? currentText.Substring(tailIndex) : "");

                        TInfo.TextLength += newTextLength;
                    }
                    else
                    {
                        TInfo.Text = text.ToString();
                    }

                    TInfo.QuestionContentType = QuestionContentType.Text;
                    TInfo.Sound = false;
                    _data.BackLink.OnText(text.ToString());
                    break;

                case AtomTypes.Video:
                case AtomTypes.Audio:
                case AtomTypes.Image:
                    string uri = null;
                    switch (mparams[2])
                    {
                        case MessageParams.Atom_Uri:
                            uri = mparams[3];
                            if (uri.Contains(Constants.GameHost))
                            {
                                var address = _actor.Connector.ServerAddress;
                                if (!string.IsNullOrWhiteSpace(address))
                                {
                                    if (Uri.TryCreate(address, UriKind.Absolute, out Uri hostUri))
                                        uri = uri.Replace(Constants.GameHost, hostUri.Host);
                                }
                            }
                            else if (uri.Contains(Constants.ServerHost))
                            {
                                uri = uri.Replace(Constants.ServerHost, _actor.ServerPublicPackageUrl ?? _actor.Connector.ServerAddress);
                            }
                            else if (!uri.StartsWith("http://localhost") && !Data.BackLink.LoadExternalMedia && !ExternalUrlOk(uri))
                            {
                                TInfo.Text = string.Format(_actor.LO[nameof(R.ExternalLink)], uri);
                                TInfo.QuestionContentType = QuestionContentType.SpecialText;
                                TInfo.Sound = false;
                                return;
                            }

                            break;
                    }

                    Uri mediaUri;
                    if (!Uri.TryCreate(uri, UriKind.RelativeOrAbsolute, out mediaUri))
                        return;

                    if (mediaUri.IsAbsoluteUri && mediaUri.Scheme == "https")
                    {
                        Print(_actor.LO[nameof(R.HttpsProtocolIsNotSupported)]);
                        return;
                    }

                    if (_data._atomType == AtomTypes.Image)
                    {
                        TInfo.MediaSource = new MediaSource(uri);
                        TInfo.QuestionContentType = QuestionContentType.Image;
                        TInfo.Sound = false;
                    }
                    else
                    {
                        if (_data._atomType == AtomTypes.Audio)
                        {
                            TInfo.SoundSource = new MediaSource(mediaUri.ToString());
                            TInfo.QuestionContentType = QuestionContentType.None;
                            TInfo.Sound = true;
                        }
                        else
                        {
                            TInfo.MediaSource = new MediaSource(mediaUri.ToString());
                            TInfo.QuestionContentType = QuestionContentType.Video;
                            TInfo.Sound = false;
                        }
                    }

                    break;
            }
        }

        private bool ExternalUrlOk(string uri) => 
            _actor.ContentPublicBaseUrls != null && _actor.ContentPublicBaseUrls.Any(publicUrl => uri.StartsWith(publicUrl));

        virtual public void SetSecondAtom(string[] mparams)
        {
            var atomType = mparams[1];

            switch (atomType)
            {
                case AtomTypes.Audio:
                    string uri = null;
                    switch (mparams[2])
                    {
                        case MessageParams.Atom_Uri:
                            uri = mparams[3];
                            if (uri.Contains(Constants.GameHost))
                            {
                                var address = _actor.Connector.ServerAddress;
                                if (!string.IsNullOrWhiteSpace(address))
                                {
                                    if (Uri.TryCreate(address, UriKind.Absolute, out Uri hostUri))
                                        uri = uri.Replace(Constants.GameHost, hostUri.Host);
                                }
                            }
                            else if (uri.Contains(Constants.ServerHost))
                            {
                                uri = uri.Replace(Constants.ServerHost, _actor.ServerPublicPackageUrl ?? _actor.Connector.ServerAddress);
                            }
                            else if (!uri.StartsWith("http://localhost") && !Data.BackLink.LoadExternalMedia && !ExternalUrlOk(uri))
                            {
                                TInfo.Text = string.Format(_actor.LO[nameof(R.ExternalLink)], uri);
                                TInfo.QuestionContentType = QuestionContentType.SpecialText;
                                TInfo.Sound = false;
                            }

                            break;
                    }

                    Uri mediaUri;
                    if (!Uri.TryCreate(uri, UriKind.RelativeOrAbsolute, out mediaUri))
                        return;

                    if (mediaUri.IsAbsoluteUri && mediaUri.Scheme == "https")
                    {
                        Print(_actor.LO[nameof(R.HttpsProtocolIsNotSupported)]);
                        return;
                    }

                    TInfo.SoundSource = new MediaSource(mediaUri.ToString());
                    TInfo.Sound = true;

                    break;
            }
        }

        public void SetRight(string answer)
        {
            try
            {
                TInfo.TStage = TableStage.Answer;
                TInfo.Text = answer;
            }
            catch (NullReferenceException)
            {
                // Странная ошибка в привязках WPF иногда возникает
            }
        }

        virtual public void Try()
        {
            TInfo.QuestionStyle = QuestionStyle.WaitingForPress;
        }

        /// <summary>
        /// Нельзя жать на кнопку
        /// </summary>
        /// <param name="text">Кто уже нажал или время вышло</param>
        virtual public void EndTry(string text)
        {
            TInfo.QuestionStyle = QuestionStyle.Normal;
            if (_data._atomType == AtomTypes.Audio || _data._atomType == AtomTypes.Video)
            {
                TInfo.IsMediaStopped = true;
            }

            if (!int.TryParse(text, out int number))
            {
                _data.Sound = "noanswer";
                return;
            }

            if (number < 0 || number >= _data.Players.Count)
                return;

            _data.Players[number].State = PlayerState.Press;
        }

        virtual public void ShowTablo()
        {
            TInfo.TStage = TableStage.RoundTable;
        }

        /// <summary>
        /// Игрок получил или потерял деньги
        /// </summary>
        virtual public void Person(int playerIndex, bool isRight)
        {
            if (isRight)
            {
                if (_data.CurPriceRight >= 2000)
                    _data.Sound = Notions.Notion.RandomString("APPLAUSE", "RINGIN");
                else
                    _data.Sound = Notions.Notion.RandomString("CLAP", "TADA");

                _data.Players[playerIndex].State = PlayerState.Right;
            }
            else
            {
                _data.Players[playerIndex].Pass = true;
                _data.Players[playerIndex].State = PlayerState.Wrong;
            }
        }

        public void OnPersonFinalStake(int playerIndex)
        {
            if (playerIndex < 0 || playerIndex >= _data.Players.Count)
                return;

            _data.Players[playerIndex].Stake = -4;
        }

        public void OnPersonFinalAnswer(int playerIndex)
        {
            if (playerIndex < 0 || playerIndex >= _data.Players.Count)
                return;

            _data.Players[playerIndex].State = PlayerState.HasAnswered;
        }

        public void OnPersonApellated(int playerIndex)
        {
            if (playerIndex < 0 || playerIndex >= _data.Players.Count)
                return;

            _data.Players[playerIndex].State = PlayerState.HasAnswered;
        }

        public void QType()
        {
            if (_data._qtype == QuestionTypes.Auction)
            {
                TInfo.Text = _actor.LO[nameof(R.Label_Auction)];

                lock (TInfo.RoundInfoLock)
                {
                    for (int i = 0; i < TInfo.RoundInfo.Count; i++)
                    {
                        TInfo.RoundInfo[i].Active = i == _data.ThemeIndex;
                    }
                }

                TInfo.TStage = TableStage.Special;
                _data.Sound = "auction";
            }
            else if (_data._qtype == QuestionTypes.Cat || _data._qtype == QuestionTypes.BagCat)
            {
                TInfo.Text = _actor.LO[nameof(R.Label_CatInBag)];
                lock (TInfo.RoundInfoLock)
                {
                    foreach (var item in TInfo.RoundInfo)
                    {
                        item.Active = false;
                    }
                }

                TInfo.TStage = TableStage.Special;
                _data.Sound = "catinbag";
            }
            else if (_data._qtype == QuestionTypes.Sponsored)
            {
                TInfo.Text = _actor.LO[nameof(R.Label_Sponsored)];
                lock (TInfo.RoundInfoLock)
                {
                    foreach (var item in TInfo.RoundInfo)
                    {
                        item.Active = false;
                    }
                }

                TInfo.TStage = TableStage.Special;
                _data.Sound = "norisk";
            }
            else if (_data._qtype != QuestionTypes.Simple)
            {
                foreach (var item in TInfo.RoundInfo)
                {
                    item.Active = false;
                }
            }
            else
            {
                TInfo.TimeLeft = 1.0;
            }
        }

        public void StopRound()
        {
            TInfo.TStage = TableStage.Sign;
        }

        virtual public void Out(int themeIndex)
        {
            TInfo.PlaySelection(themeIndex);
            _data.Sound = "shrink";
        }

        public void Winner()
        {
            if (SynchronizationContext.Current == null)
            {
                ExecuteInUIThread(WinnerUI);
                return;
            }

            WinnerUI();
        }

        private void WinnerUI()
        {
            if (_data.Winner > -1)
                _data.Sound = Notions.Notion.RandomString("ESC_2", "VIPER2");

            // Лучшие игроки
            _data.BackLink.SaveBestPlayers(_data.Players);
        }

        public void TimeOut()
        {
            _data.Sound = "timeout";
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            if (_data.ProtocolWriter != null)
            {
                _data.ProtocolWriter.Dispose();
                _data.ProtocolWriter = null;
            }

            _data.BackLink.ClearTempFile();

            base.Dispose(disposing);
        }

        public void FinalThink()
        {
            _data.BackLink.PlaySound("finalthink");
        }

        public void UpdatePicture(Account account, string path)
        {
            if (path.Contains(Constants.GameHost))
            {
                if (_actor.Connector != null && !string.IsNullOrWhiteSpace(_actor.Connector.ServerAddress))
                {
                    var remoteUri = _actor.Connector.ServerAddress;
                    if (Uri.TryCreate(remoteUri, UriKind.Absolute, out Uri hostUri))
                    {
                        account.Picture = path.Replace(Constants.GameHost, hostUri.Host);
                    }
                    else
                    {
                        // Блок для отлавливания специфической ошибки
                        _data.BackLink.OnPictureError(remoteUri);
                    }
                }
            }
            else if (path.Contains(Constants.ServerHost))
            {
                if (_actor.Connector != null && !string.IsNullOrWhiteSpace(_actor.Connector.ServerAddress))
                {
                    account.Picture = path.Replace(Constants.ServerHost, _actor.ServerPublicPackageUrl ?? _actor.Connector.ServerAddress);
                }
            }
            else
            {
                account.Picture = path;
            }
        }

        /// <summary>
        /// Попытка осуществить повторное подключение к серверу
        /// </summary>
        public async void TryConnect(IConnector connector)
        {
            try
            {
                Print(ReplicManager.Special(_actor.LO[nameof(R.TryReconnect)]));

                var result = await connector.ReconnectToServer();
                if (!result)
                {
                    AnotherTry(connector);
                    return;
                }

                Print(ReplicManager.Special(_actor.LO[nameof(R.ReconnectOK)]));
                await connector.RejoinGame();

                if (!string.IsNullOrEmpty(connector.Error))
                {
                    if (connector.CanRetry)
                        AnotherTry(connector);
                    else
                        Print(ReplicManager.Special(connector.Error));
                }
                else
                    Print(ReplicManager.Special(_actor.LO[nameof(R.ReconnectEntered)]));
            }
            catch (Exception exc)
            {
                try { _data.BackLink.OnError(exc); }
                catch { }
            }
        }

        private async void AnotherTry(IConnector connector)
        {
            Print(ReplicManager.Special(connector.Error));
            if (!_disposed)
            {
                await Task.Delay(10000);
                TryConnect(connector);
            }
        }

        #endregion

        public void OnTextSpeed(double speed)
        {
            TInfo.TextSpeed = speed;
        }

        public void SetText(string text)
        {
            TInfo.Text = text;
            TInfo.TStage = TableStage.Round;
        }

        public void OnPauseChanged(bool isPaused) => TInfo.Pause = isPaused;

        public void TableLoaded()
        {
            if (SynchronizationContext.Current == null)
            {
                ExecuteInUIThread(TableLoadedUI);
                return;
            }

            TableLoadedUI();
        }

        private void TableLoadedUI()
        {
            lock (TInfo.RoundInfoLock)
            {
                for (int i = 0; i < _data.TInfo.RoundInfo.Count; i++)
                {
                    if (TInfo.RoundInfo.Count <= i)
                        break;

                    TInfo.RoundInfo[i].Questions.Clear();

                    foreach (var item in _data.TInfo.RoundInfo[i].Questions.Select(questionInfo => new QuestionInfoViewModel(questionInfo)).ToArray())
                    {
                        TInfo.RoundInfo[i].Questions.Add(item);
                    }
                }
            }
        }

        public void Resume() => TInfo.IsMediaStopped = false;

        public async void PrintGreeting()
        {
            await Task.Delay(1000);
            _data.OnAddString(null, _actor.LO[nameof(R.Greeting)] + Environment.NewLine, LogMode.Protocol);
        }

        public void OnTimeChanged()
        {
            
        }

        public void OnTimerChanged(int timerIndex, string timerCommand, string arg, string person)
        {
            if (timerIndex == 1 && timerCommand == "RESUME")
            {
                TInfo.QuestionStyle = QuestionStyle.WaitingForPress;
            }

            if (timerIndex == 2)
            {
                if (timerCommand == "GO")
                {
                    if (person != null && int.TryParse(person, out int personIndex))
                    {
                        if (_data.DialogMode == DialogModes.ChangeSum || _data.DialogMode == DialogModes.Manage
                            || _data.DialogMode == DialogModes.None)
                        {
                            if (personIndex == -1)
                            {
                                _data.ShowMan.IsDeciding = true;
                            }
                            else if (personIndex > -1 && personIndex < _data.Players.Count)
                            {
                                _data.Players[personIndex].IsDeciding = true;
                            }
                        }

                        if (personIndex == -2)
                        {
                            _data.ShowMainTimer = true;
                        }
                    }
                }
                else if (timerCommand == "STOP")
                {
                    _data.ShowMan.IsDeciding = false;
                    foreach (var player in _data.Players)
                    {
                        player.IsDeciding = false;
                    }

                    _data.ShowMainTimer = false;
                }
            }
        }

        public void OnPackageLogo(string uri)
        {
            TInfo.TStage = TableStage.Question;

            if (uri.Contains(Constants.GameHost))
            {
                var address = _actor.Connector.ServerAddress;
                if (!string.IsNullOrWhiteSpace(address))
                {
                    if (Uri.TryCreate(address, UriKind.Absolute, out Uri hostUri))
                        uri = uri.Replace(Constants.GameHost, hostUri.Host);
                }
            }
            else if (uri.Contains(Constants.ServerHost))
            {
                uri = uri.Replace(Constants.ServerHost, _actor.ServerPublicPackageUrl ?? _actor.Connector.ServerAddress);
            }

            if (!Uri.TryCreate(uri, UriKind.RelativeOrAbsolute, out Uri mediaUri))
            {
                return;
            }

            if (mediaUri.IsAbsoluteUri && mediaUri.Scheme == "https")
            {
                Print(_actor.LO[nameof(R.HttpsProtocolIsNotSupported)]);
                return;
            }

            TInfo.MediaSource = new MediaSource(uri);
            TInfo.QuestionContentType = QuestionContentType.Image;
            TInfo.Sound = false;
        }

        public void OnPersonPass(int playerIndex)
        {
            if (playerIndex < 0 || playerIndex >= _data.Players.Count)
                return;

            _data.Players[playerIndex].State = PlayerState.Pass;
        }
    }
}

using Notions;
using SICore.Contracts;
using SICore.Models;
using SICore.Network;
using SICore.Network.Clients;
using SICore.Network.Contracts;
using SICore.PlatformSpecific;
using SICore.Special;
using SIData;
using SIPackages.Core;
using SIUI.Model;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using R = SICore.Properties.Resources;

namespace SICore;

/// <summary>
/// Implements a game viewer.
/// </summary>
public class Viewer : Actor, IViewerClient, INotifyPropertyChanged
{
    protected readonly ViewerActions _viewerActions;

    public ViewerActions Actions => _viewerActions;

    public virtual GameRole Role => GameRole.Viewer;

    private readonly IPersonController _logic;

    protected IPersonController Logic => _logic;

    private bool _isHost;

    /// <summary>
    /// Is current person a game host (has rights to manage the game process).
    /// </summary>
    public bool IsHost
    {
        get => _isHost;

        private set
        {
            if (_isHost != value)
            {
                _isHost = value;
                _logic.OnHostChanged();
                OnPropertyChanged();
            }
        }
    }

    public IPersonController MyLogic => _logic;

    public ViewerData MyData => ClientData;

    public string? Avatar { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Initialize(bool isHost)
    {
        IsHost = isHost;
        ClientData.Name = _client.Name;
    }

    private ILocalizer LO { get; }

    public ViewerData ClientData { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="Viewer" /> class.
    /// </summary>
    public Viewer(
        Client client,
        Account personData,
        bool isHost,
        IPersonController logic,
        ViewerActions viewerActions,
        ILocalizer localizer,
        ViewerData data)
        : base(client)
    {
        _viewerActions = viewerActions;
        _logic = logic;
        LO = localizer;
        ClientData = data;

        Initialize(isHost);

        ClientData.Picture = personData.Picture;
    }

    public void Move(object arg) => _viewerActions.SendMessageWithArgs(Messages.Move, arg);

    // TODO: get rid of async and await here
    /// <summary>
    /// Processes received system message.
    /// </summary>
    /// <param name="mparams">Message arguments.</param>
    protected virtual async ValueTask OnSystemMessageReceivedAsync(string[] mparams)
    {
        try
        {
            switch (mparams[0])
            {
                case Messages.Connected:
                    await OnConnectedAsync(mparams);
                    break;

                case SystemMessages.Disconnect:
                    {
                        _logic.OnReplic(ReplicCodes.System.ToString(), LO[nameof(R.DisconnectMessage)]);
                        break;
                    }

                case SystemMessages.GameClosed:
                    _logic.OnGameClosed();
                    break;

                case Messages.Disconnected:
                    await OnDisconnectedAsync(mparams);
                    break;

                case Messages.GameMetadata:
                    if (mparams.Length > 3)
                    {
                        _logic.OnGameMetadata(mparams[1], mparams[2], mparams[3], mparams.Length > 4 ? mparams[4] : "");
                    }
                    break;

                case Messages.Info2:
                    await ProcessInfoAsync(mparams);
                    break;

                case Messages.Config:
                    if (ClientData.Me == null)
                    {
                        break;
                    }

                    ProcessConfig(mparams);
                    break;

                case Messages.Options:
                    _logic.OnOptions(mparams);
                    break;

                case Messages.ReadingSpeed:
                    {
                        #region ReadingSpeed
                        if (mparams.Length > 1)
                        {
                            if (int.TryParse(mparams[1], out int readingSpeed))
                            {
                                if (readingSpeed > 0)
                                {
                                    _logic.OnTextSpeed(1.0 / readingSpeed);
                                }
                                else
                                {
                                    _logic.OnTextSpeed(0.0);
                                }
                            }
                        }
                        break;
                        #endregion
                    }

                case Messages.ButtonBlockingTime:
                    if (mparams.Length > 1)
                    {
                        if (int.TryParse(mparams[1], out var buttonBlockingTime) && buttonBlockingTime > 0)
                        {
                            ClientData.ButtonBlockingTime = buttonBlockingTime;
                        }
                    }
                    break;

                case Messages.ApellationEnabled:
                    if (mparams.Length > 1)
                    {
                        ClientData.ApellationEnabled = mparams[1] == "+";
                    }
                    break;

                case Messages.Hostname:
                    if (mparams.Length > 1)
                    {
                        ClientData.HostName = mparams[1];
                        IsHost = _client.Name == ClientData.HostName;

                        if (mparams.Length > 2) // Host has been changed
                        {
                            _logic.OnReplic(
                                ReplicCodes.Special.ToString(),
                                string.Format(LO[nameof(R.HostChanged)],
                                    mparams[2].Length > 0 ? mparams[2] : LO[nameof(R.ByGame)],
                                    mparams[1]));
                        }
                    }
                    break;

                case Messages.BannedList:
                    OnBannedList(mparams);
                    break;

                case Messages.Banned:
                    OnBanned(mparams);
                    break;

                case Messages.Unbanned:
                    OnUnbanned(mparams);
                    break;

                case Messages.ComputerAccounts:
                    {
                        ClientData.DefaultComputerPlayers = mparams.Skip(1).Select(name => new Account { Name = name }).ToArray();
                        break;
                    }

                case Messages.SetJoinMode:
                    if (mparams.Length < 2 || !Enum.TryParse<JoinMode>(mparams[1], out var joinMode))
                    {
                        break;
                    }

                    _logic.OnSetJoinMode(joinMode);
                    break;

                case Messages.PackageId:
                    {
                        if (mparams.Length > 1)
                        {
                            ClientData.PackageId = mparams[1];
                        }
                        break;
                    }

                case Messages.FalseStart:
                    {
                        if (mparams.Length > 1)
                        {
                            ClientData.FalseStart = mparams[1] != "-";
                        }
                        break;
                    }

                case Messages.Replic:
                    OnReplic(mparams);
                    break;

                case Messages.Pause:
                    {
                        #region Pause

                        var isPaused = mparams[1] == "+";
                        _logic.OnPauseChanged(isPaused);

                        if (mparams.Length > 4)
                        {
                            var message = ClientData.TInfo.Pause ? MessageParams.Timer_UserPause : MessageParams.Timer_UserResume;

                            _logic.OnTimerChanged(0, message, mparams[2]);
                            _logic.OnTimerChanged(1, message, mparams[3]);
                            _logic.OnTimerChanged(2, message, mparams[4]);
                        }

                        break;

                        #endregion
                    }

                case Messages.Sums:
                    {
                        #region Sums

                        var players = ClientData.Players;
                        var max = Math.Min(players.Count, mparams.Length - 1);

                        for (int i = 0; i < max; i++)
                        {
                            if (int.TryParse(mparams[i + 1], out int sum))
                                players[i].Sum = sum;
                        }

                        break;

                        #endregion
                    }

                case Messages.Ready:
                    {
                        #region Ready

                        if (ClientData == null || ClientData.Players.Count == 0)
                        {
                            return;
                        }

                        var ready = mparams.Length == 2 || mparams[2] == "+";

                        if (ClientData.ShowMan == null)
                        {
                            return;
                        }

                        var person = ClientData.MainPersons.FirstOrDefault(item => item.Name == mparams[1]);
                        
                        if (person != null)
                        {
                            person.Ready = ready;
                        }

                        #endregion
                        break;
                    }

                case Messages.RoundsNames:
                    ClientData.RoundNames = mparams.Skip(1).ToArray();
                    break;

                case Messages.Stage:
                case Messages.StageInfo:
                    {
                        #region Stage

                        ClientData.Stage = (GameStage)Enum.Parse(typeof(GameStage), mparams[1]);

                        if (ClientData.Stage != GameStage.Before)
                        {
                            for (int i = 0; i < ClientData.Players.Count; i++)
                            {
                                ClientData.Players[i].GameStarted = true;
                            }

                            if (ClientData.ShowMan != null)
                            {
                                ClientData.ShowMan.GameStarted = true;
                            }
                        }

                        if (mparams.Length > 3)
                        {
                            if (int.TryParse(mparams[3], out var roundIndex))
                            {
                                if (roundIndex > -1 && roundIndex < ClientData.RoundNames.Length)
                                {
                                    ClientData.StageName = ClientData.RoundNames[roundIndex];
                                    ClientData.RoundIndex = roundIndex;
                                }
                            }
                        }

                        if (mparams[0] == Messages.Stage)
                        {
                            _logic.SetCaption("");

                            switch (ClientData.Stage)
                            {
                                case GameStage.Round:
                                case GameStage.Final:
                                    if (mparams.Length > 2)
                                    {
                                        _logic.SetText(mparams[2]);
                                    }

                                    for (int i = 0; i < ClientData.Players.Count; i++)
                                    {
                                        ClientData.Players[i].InGame = true;
                                        ClientData.Players[i].IsChooser = false;
                                    }

                                    break;

                                case GameStage.After:
                                    ClientData.Host.OnGameFinished(ClientData.PackageId);
                                    ClientData.StageName = "";
                                    break;
                            }

                            _logic.Stage();
                        }

                        #endregion
                        break;
                    }

                case Messages.Timer:
                    {
                        if (!int.TryParse(mparams[1], out var timerIndex) || timerIndex < 0 || timerIndex > 2)
                        {
                            return;
                        }

                        var timerCommand = mparams[2];

                        _logic.OnTimerChanged(
                            timerIndex,
                            timerCommand,
                            mparams.Length > 3 ? mparams[3] : "",
                            mparams.Length > 4 ? mparams[4] : null);

                        break;
                    }

                case Messages.GameThemes:
                    {
                        #region GameThemes

                        for (var i = 1; i < mparams.Length; i++)
                        {
                            ClientData.TInfo.GameThemes.Add(mparams[i]);
                            _logic.OnReplic(ReplicCodes.System.ToString(), mparams[i]);
                        }

                        _logic.GameThemes();

                        #endregion
                        break;
                    }

                case Messages.RoundThemes2:
                    OnRoundThemes(mparams);
                    break;

                case Messages.RoundThemesComments:
                    OnRoundThemesComments(mparams);
                    break;

                case Messages.RoundContent:
                    _logic.OnRoundContent(mparams);
                    break;

                case Messages.Theme:
                    OnTheme(mparams);
                    break;

                case Messages.ThemeInfo:
                    OnThemeInfo(mparams);
                    break;

                case Messages.Question:
                    if (mparams.Length > 1)
                    {
                        _logic.ClearQuestionState();
                        _logic.SetText(mparams[1], TableStage.QuestionPrice);
                        OnThemeOrQuestion();
                        _logic.SetCaption($"{ClientData.ThemeName}, {mparams[1]}");
                    }
                    break;

                case Messages.Table:
                    {
                        #region Table

                        // TODO: clear existing table and renew it
                        if (ClientData.TInfo.RoundInfo.Any(t => t.Questions.Any()))
                        {
                            break;
                        }

                        var index = 1;

                        for (int i = 0; i < ClientData.TInfo.RoundInfo.Count; i++)
                        {
                            if (index == mparams.Length)
                            {
                                break;
                            }

                            while (index < mparams.Length && mparams[index].Length > 0) // empty value separates the themes content
                            {
                                if (!int.TryParse(mparams[index++], out int price))
                                {
                                    price = -1;
                                }

                                ClientData.TInfo.RoundInfo[i].Questions.Add(new QuestionInfo { Price = price });
                            }

                            index++;
                        }

                        _logic.TableLoaded();

                        #endregion
                        break;
                    }

                case Messages.Toggle:
                    OnToggle(mparams);
                    break;

                case Messages.ShowTable:
                    _logic.ShowTablo();
                    break;

                case Messages.Choice:
                    OnChoice(mparams);
                    break;

                case Messages.QuestionCaption:
                    if (mparams.Length > 1)
                    {
                        _logic.SetCaption(mparams[1]);
                    }
                    break;

                case Messages.ThemeComments:
                    if (mparams.Length > 1)
                    {
                        _logic.OnThemeComments(mparams[1]);
                    }
                    break;

                case Messages.QType:
                    OnQuestionType(mparams);
                    break;

                case Messages.Layout:
                    OnLayout(mparams);
                    break;

                case Messages.TextShape: // TODO: remove after v7.11.0 deprecation
                    _logic.OnTextShape(mparams);
                    break;

                case Messages.ContentShape:
                    if (mparams.Length > 4
                        && mparams[1] == ContentPlacements.Screen
                        && mparams[2] == "0"
                        && mparams[3] == ContentTypes.Text)
                    {
                        _logic.OnContentShape(mparams[4].UnescapeNewLines());
                    }
                    break;

                case Messages.Content:
                    _logic.OnContent(mparams);
                    break;

                case Messages.ContentAppend:
                    _logic.OnContentAppend(mparams);
                    break;

                case Messages.ContentState:
                    _logic.OnContentState(mparams);
                    break;

                case Messages.Atom_Hint:
                    if (mparams.Length > 1)
                    {
                        _logic.OnAtomHint(mparams[1]);
                    }
                    break;

                case Messages.MediaLoaded:
                    OnMediaLoaded(mparams);
                    break;

                case Messages.RightAnswer:
                    _logic.OnRightAnswer(mparams[2]);
                    break;

                case Messages.RightAnswerStart:
                    if (mparams.Length > 2)
                    {
                        _logic.OnRightAnswerStart(mparams[2]);
                    }
                    break;

                case Messages.Resume:
                    _logic.Resume();
                    break;

                case Messages.Try:
                    {
                        #region Try

                        if (mparams.Length > 1 && mparams[1] == MessageParams.Try_NotFinished)
                        {
                            // Здесь можно не показывать рамку
                            if (!ClientData.Host.ShowBorderOnFalseStart)
                            {
                                return;
                            }
                        }

                        _logic.Try();

                        #endregion
                        break;
                    }

                case Messages.EndTry:
                    if (mparams.Length > 1)
                    {
                        if (mparams[1] == MessageParams.EndTry_All)
                        {
                            _logic.OnTimerChanged(1, MessageParams.Timer_Stop, "");
                        }

                        _logic.EndTry(mparams[1]);
                    }
                    break;

                case Messages.StopPlay:
                    _logic.OnStopPlay();
                    break;

                case Messages.WrongTry:
                    OnWrongTry(mparams);
                    break;

                case Messages.Person:
                    {
                        #region Person

                        if (mparams.Length < 4)
                        {
                            break;
                        }

                        var isRight = mparams[1] == "+";
                        
                        if (!int.TryParse(mparams[2], out var playerIndex)
                            || playerIndex < 0
                            || playerIndex >= ClientData.Players.Count)
                        {
                            break;
                        }

                        if (!int.TryParse(mparams[3], out var price))
                        {
                            break;
                        }

                        _logic.OnPersonScoreChanged(playerIndex, isRight, price);

                        #endregion
                        break;
                    }

                case Messages.Pass:
                    _logic.OnPersonPass(int.Parse(mparams[1]));
                    break;

                case Messages.PersonFinalAnswer:
                    {
                        if (mparams.Length > 1 && int.TryParse(mparams[1], out int playerIndex))
                        {
                            _logic.OnPersonFinalAnswer(playerIndex);
                        }
                        break;
                    }
                case Messages.PersonApellated:
                    {
                        if (int.TryParse(mparams[1], out int playerIndex))
                        {
                            _logic.OnPersonApellated(playerIndex);
                        }
                        break;
                    }
                case Messages.PersonFinalStake:
                    {
                        if (int.TryParse(mparams[1], out int playerIndex))
                        {
                            _logic.OnPersonFinalStake(playerIndex);
                        }
                        break;
                    }

                case Messages.PlayerState:
                    OnPlayerState(mparams);
                    break;

                case Messages.PersonStake:
                    OnPersonStake(mparams);
                    break;

                case Messages.Answers:
                    OnAnswers(mparams);
                    break;

                case Messages.Stop:
                    _logic.StopRound();

                    _logic.OnTimerChanged(0, MessageParams.Timer_Stop, "");
                    _logic.OnTimerChanged(1, MessageParams.Timer_Stop, "");
                    _logic.OnTimerChanged(2, MessageParams.Timer_Stop, "");
                    break;

                case Messages.Out:
                    OnOut(mparams);
                    break;

                case Messages.Winner:
                    if (mparams.Length > 1 && int.TryParse(mparams[1], out int winnerIndex))
                    {
                        _logic.OnWinner(winnerIndex);
                    }
                    break;

                case Messages.Timeout:
                    {
                        #region Timeout

                        _logic.TimeOut();

                        #endregion
                        break;
                    }

                case Messages.FinalThink:
                    _logic.FinalThink();
                    break;

                case Messages.Avatar:
                    OnAvatar(mparams);
                    break;

                case Messages.SetChooser:
                    OnSetChooser(mparams);
                    break;

                case Messages.Ads:
                    if (mparams.Length > 1)
                    {
                        _logic.OnAd(mparams[1]);
                    }
                    break;
            }
        }
        catch (Exception exc)
        {
            throw new Exception(string.Join(Message.ArgsSeparator, mparams), exc);
        }
    }

    private void OnOut(string[] mparams)
    {
        if (mparams.Length < 2 || !int.TryParse(mparams[1], out var themeIndex))
        {
            return;
        }

        ClientData.ThemeIndex = themeIndex;

        if (ClientData.ThemeIndex > -1 && ClientData.ThemeIndex < ClientData.TInfo.RoundInfo.Count)
        {
            _logic.Out(ClientData.ThemeIndex);
        }
    }

    private void OnRoundThemesComments(string[] mparams)
    {
        // TODO: save round themes comments
    }

    private void OnTheme(string[] mparams)
    {
        if (mparams.Length <= 4)
        {
            return;
        }

        _logic.SetText(mparams[1], TableStage.Theme);
        OnThemeOrQuestion();
        ClientData.ThemeName = mparams[1];
        ClientData.ThemeComments = mparams[4];
    }

    private void OnThemeInfo(string[] mparams)
    {
        if (mparams.Length <= 3)
        {
            return;
        }

        ClientData.ThemeName = mparams[1];
        ClientData.ThemeComments = mparams[3];
    }

    private void OnSetChooser(string[] mparams)
    {
        if (mparams.Length < 2 || !int.TryParse(mparams[1], out var index))
        {
            return;
        }

        for (int i = 0; i < ClientData.Players.Count; i++)
        {
            ClientData.Players[i].IsChooser = i == index;

            if (mparams.Length > 2)
            {
                ClientData.Players[i].State = i == index ? PlayerState.Answering : PlayerState.Pass;
            }
        }
    }

    private async ValueTask OnConnectedAsync(string[] mparams)
    {
        if (mparams.Length < 5 || mparams[3] == _client.Name)
        {
            return;
        }

        var role = mparams[1];

        if (role != Constants.Showman && role != Constants.Player && role != Constants.Viewer)
        {
            return;
        }

        if (!_client.Node.IsMain) // TODO: this should be handled on node level
        {
            await _client.Node.ConnectionsLock.WithLockAsync(() =>
            {
                var currentConnection = ((ISecondaryNode)_client.Node).HostServer;

                if (currentConnection != null)
                {
                    lock (currentConnection.ClientsSync)
                    {
                        currentConnection.Clients.Add(mparams[3]);
                    }
                }
            });
        }

        var account = new Account(mparams[3], mparams[4] == "m");
        _ = int.TryParse(mparams[2], out var connectedIndex);

        InsertPerson(role, account, connectedIndex);
        _logic.OnPersonConnected();
    }

    private void OnPlayerState(string[] mparams)
    {
        if (mparams.Length <= 2 || !Enum.TryParse<PlayerState>(mparams[1], out var state))
        {
            return;
        }

        for (var i = 2; i < mparams.Length; i++)
        {
            if (int.TryParse(mparams[i], out int playerIndex)
                && playerIndex >= 0
                && playerIndex < ClientData.Players.Count)
            {
                ClientData.Players[playerIndex].State = state;
            }
        }

        if (state == PlayerState.Lost)
        {
            Task.Run(async () =>
            {
                await Task.Delay(200);

                foreach (var player in ClientData.Players)
                {
                    if (player.State == PlayerState.Lost)
                    {
                        player.State = PlayerState.None;
                    }
                }
            });
        }
    }

    private void OnAvatar(string[] mparams)
    {
        if (mparams.Length < 4)
        {
            return;
        }

        var person = ClientData.MainPersons.FirstOrDefault(person => person.Name == mparams[1]);

        if (person != null)
        {
            _logic.UpdateAvatar(person, mparams[2], mparams[3]);
        }
    }

    private void OnAnswers(string[] mparams)
    {
        for (var i = 1; i < mparams.Length && i - 1 < ClientData.Players.Count; i++)
        {
            ClientData.Players[i - 1].Answer = mparams[i];
        }
    }

    private void OnWrongTry(string[] mparams)
    {
        if (!int.TryParse(mparams[1], out var playerIndex) || playerIndex <= 0 || playerIndex >= ClientData.Players.Count)
        {
            return;
        }

        var player = ClientData.Players[playerIndex];

        if (player.State != PlayerState.None)
        {
            return;
        }

        player.State = PlayerState.Lost;
        
        Task.Run(async () =>
        {
            await Task.Delay(200);

            if (player.State == PlayerState.Lost)
            {
                player.State = PlayerState.None;
            }
        });
    }

    private void OnLayout(string[] mparams)
    {
        if (mparams.Length < 5)
        {
            return;
        }
        
        if (mparams[1] != MessageParams.Layout_AnswerOptions)
        {
            return;
        }

        var questionHasScreenContent = mparams[2] == "+";

        var optionsTypes = new List<string>();

        for (var i = 3; i < mparams.Length; i++)
        {
            optionsTypes.Add(mparams[i]);
        }

        _logic.OnAnswerOptions(questionHasScreenContent, optionsTypes);
    }

    private void OnQuestionType(string[] mparams)
    {
        ClientData.QuestionType = mparams[1];
        _logic.OnQuestionStart(mparams.Length > 2 && bool.TryParse(mparams[2], out var isDefault) && isDefault);
    }

    private void OnChoice(string[] mparams)
    {
        ClientData.ThemeIndex = int.Parse(mparams[1]);
        ClientData.QuestionIndex = int.Parse(mparams[2]);

        if (ClientData.ThemeIndex > -1
            && ClientData.ThemeIndex < ClientData.TInfo.RoundInfo.Count
            && ClientData.QuestionIndex > -1
            && ClientData.QuestionIndex < ClientData.TInfo.RoundInfo[ClientData.ThemeIndex].Questions.Count)
        {
            var selectedTheme = ClientData.TInfo.RoundInfo[ClientData.ThemeIndex];
            var selectedQuestion = selectedTheme.Questions[ClientData.QuestionIndex];
            _logic.SetCaption($"{selectedTheme.Name}, {selectedQuestion.Price}");
        }

        foreach (var player in ClientData.Players.ToArray())
        {
            player.ClearState();
        }

        _logic.ClearQuestionState();
        _logic.Choice();
    }

    private void OnMediaLoaded(string[] mparams)
    {
        if (mparams.Length < 2)
        {
            return;
        }

        var player = ClientData.Players.FirstOrDefault(p => p.Name == mparams[1]);

        if (player == null)
        {
            return;
        }

        player.MediaLoaded = true;
    }

    private void OnToggle(string[] mparams)
    {
        if (mparams.Length < 4)
        {
            return;
        }

        if (!int.TryParse(mparams[1], out var themeIndex)
            || !int.TryParse(mparams[2], out var questionIndex)
            || !int.TryParse(mparams[3], out var price))
        {
            return;
        }

        if (themeIndex < 0 || themeIndex >= ClientData.TInfo.RoundInfo.Count)
        {
            return;
        }

        var theme = ClientData.TInfo.RoundInfo[themeIndex];

        if (questionIndex < 0 || questionIndex >= theme.Questions.Count)
        {
            return;
        }

        theme.Questions[questionIndex].Price = price;
        _logic.OnToggle(themeIndex, questionIndex, price);
    }

    private void OnUnbanned(string[] mparams)
    {
        if (mparams.Length < 2)
        {
            return;
        }

        _logic.OnUnbanned(mparams[1]);
    }

    private void OnBanned(string[] mparams)
    {
        if (mparams.Length < 3)
        {
            return;
        }

        _logic.OnBanned(new BannedInfo(mparams[1], mparams[2]));
    }

    private void OnBannedList(string[] mparams)
    {
        var banned = new List<BannedInfo>();

        for (int i = 1; i < mparams.Length - 1; i += 2)
        {
            banned.Add(new BannedInfo(mparams[i], mparams[i + 1]));
        }

        _logic.OnBannedList(banned);
    }

    private async ValueTask OnDisconnectedAsync(string[] mparams)
    {
        if (mparams.Length < 2)
        {
            return;
        }

        var name = mparams[1];

        if (ClientData.AllPersons.TryGetValue(name, out var person))
        {
            ClientData.BeginUpdatePersons($"Disconnected {name}");

            try
            {
                person.IsConnected = false;
                person.Name = Constants.FreePlace;
                person.Picture = "";

                var personAccount = person as PersonAccount;

                if (ClientData.Stage == GameStage.Before && personAccount != null)
                {
                    personAccount.Ready = false;
                }

                if (personAccount == null)
                {
                    ClientData.Viewers.Remove(person);
                }
                else if (personAccount is PlayerAccount player)
                {
                    UpdateOthers(player);
                }
                else
                {
                    UpdateShowmanCommands();
                }
            }
            finally
            {
                ClientData.EndUpdatePersons();
            }
        }

        if (!_client.Node.IsMain) // TODO: this should be handled on node level
        {
            await _client.Node.ConnectionsLock.WithLockAsync(() =>
            {
                var currentConnection = ((ISecondaryNode)_client.Node).HostServer;

                if (currentConnection != null)
                {
                    lock (currentConnection.ClientsSync)
                    {
                        if (currentConnection.Clients.Contains(name))
                        {
                            currentConnection.Clients.Remove(name);
                        }
                    }
                }
            });
        }

        _logic.OnPersonDisconnected();
    }

    private void OnRoundThemes(string[] mparams)
    {
        if (mparams.Length < 2 || !Enum.TryParse<ThemesPlayMode>(mparams[1], out var playMode))
        {
            return;
        }

        ClientData.TInfo.RoundInfo.Clear();

        for (var i = 2; i < mparams.Length; i++)
        {
            ClientData.TInfo.RoundInfo.Add(new ThemeInfo { Name = mparams[i] });

            if (playMode != ThemesPlayMode.None)
            {
                _logic.OnReplic(ReplicCodes.System.ToString(), mparams[i]);
            }
        }

        try
        {
            _logic.RoundThemes(playMode);
        }
        catch (InvalidProgramException exc)
        {
            ClientData.Host.SendError(exc, true);
        }
    }

    private void OnThemeOrQuestion()
    {
        foreach (var player in ClientData.Players)
        {
            player.ClearState();
        }
    }

    private void OnPersonStake(string[] mparams)
    {
        if (mparams.Length < 3 ||
            !int.TryParse(mparams[1], out var lastStakerIndex) ||
            lastStakerIndex < 0 ||
            lastStakerIndex >= ClientData.Players.Count ||
            !int.TryParse(mparams[2], out var stakeType))
        {
            return;
        }

        ClientData.LastStakerIndex = lastStakerIndex;

        int stake;

        if (stakeType == 0)
        {
            stake = -1;
        }
        else if (stakeType == 2)
        {
            stake = -2;
            ClientData.Players[ClientData.LastStakerIndex].State = PlayerState.Pass;
        }
        else if (stakeType == 3)
        {
            stake = -3;
        }
        else
        {
            if (stakeType != 1 || mparams.Length < 4 || !int.TryParse(mparams[3], out stake))
            {
                return;
            }

            if (mparams.Length > 4)
            {
                ClientData.Players[ClientData.LastStakerIndex].SafeStake = true;
            }
        }

        ClientData.Players[ClientData.LastStakerIndex].Stake = stake;
        _logic.OnPersonStake();
    }

    private void OnReplic(string[] mparams)
    {
        var personCode = mparams[1];

        var text = new StringBuilder();

        for (var i = 2; i < mparams.Length; i++)
        {
            if (text.Length > 0)
            {
                text.Append(Message.ArgsSeparatorChar);
            }

            text.Append(mparams[i]);
        }

        _logic.OnReplic(personCode, text.ToString().Trim());
    }

    private void ProcessConfig(string[] mparams)
    {
        switch (mparams[1])
        {
            case MessageParams.Config_AddTable:
                OnConfigAddTable(mparams);
                break;

            case MessageParams.Config_Free:
                OnConfigFree(mparams);
                break;

            case MessageParams.Config_DeleteTable:
                OnConfigDeleteTable(mparams);
                break;

            case MessageParams.Config_Set:
                OnConfigSet(mparams);
                break;

            case MessageParams.Config_ChangeType:
                OnConfigChangeType(mparams);
                break;
        }

        foreach (var player in ClientData.Players)
        {
            UpdateOthers(player);
        }

        UpdateShowmanCommands();
    }

    private void OnConfigAddTable(string[] mparams)
    {
        ClientData.BeginUpdatePersons($"Config_AddTable {string.Join(" ", mparams)}");

        try
        {
            var account = new PlayerAccount(mparams[2], mparams[3] == "+", mparams[4] == "+", ClientData.Stage != GameStage.Before)
            {
                IsHuman = mparams[5] == "+",
                Ready = mparams[6] == "+"
            };

            ClientData.Players.Add(account);
            UpdateOthers(account);
            _logic.AddPlayer(account);
        }
        finally
        {
            ClientData.EndUpdatePersons();
        }
    }

    private void OnConfigFree(string[] mparams)
    {
        if (mparams.Length < 4)
        {
            return;
        }

        var personType = mparams[2];
        var indexString = mparams[3];

        var me = ClientData.Me;

        PersonAccount account;

        var isPlayer = personType == Constants.Player;
        
        if (isPlayer)
        {
            if (!int.TryParse(indexString, out int index) || index < 0 || index >= ClientData.Players.Count)
            {
                return;
            }

            account = ClientData.Players[index];
        }
        else
        {
            account = ClientData.ShowMan;
        }

        var clone = new List<ViewerAccount>(ClientData.Viewers);
        var newAccount = new ViewerAccount(account) { IsConnected = true };

        clone.Add(newAccount);

        ClientData.BeginUpdatePersons($"Config_Free {string.Join(" ", mparams)}");
        
        try
        {
            ClientData.Viewers = clone;

            account.Name = Constants.FreePlace;
            account.IsConnected = false;
            account.Ready = false;
            account.Picture = "";
        }
        finally
        {
            ClientData.EndUpdatePersons();
        }

        if (account == me)
        {
            // Should move to viewers itself
            SwitchToNewType(GameRole.Viewer, newAccount);
        }
    }

    private void OnConfigChangeType(string[] mparams)
    {
        if (mparams.Length < 7)
        {
            return;
        }

        var personType = mparams[2];
        var indexString = mparams[3];
        var newTypeHuman = mparams[4] == "+";
        var newName = mparams[5];
        var newSex = mparams[6] == "+";

        var me = ClientData.Me;

        PersonAccount account;
        var isPlayer = personType == Constants.Player;

        if (isPlayer)
        {
            if (!int.TryParse(indexString, out var index) || index < 0 || index >= ClientData.Players.Count)
            {
                return;
            }

            account = ClientData.Players[index];
        }
        else
        {
            account = ClientData.ShowMan;
        }

        if (account.IsHuman == newTypeHuman)
        {
            return;
        }

        if (newTypeHuman)
        {
            ClientData.BeginUpdatePersons($"Config_ChangeType {string.Join(" ", mparams)}");

            try
            {
                if (account.Name == ClientData.Name /* for computer accounts being deleted */)
                {
                    ThrowComputerAccountError();
                }

                account.IsHuman = true;
                account.Name = Constants.FreePlace;
                account.Picture = "";
                account.IsConnected = false;
                account.Ready = false;
            }
            finally
            {
                ClientData.EndUpdatePersons();
            }
        }
        else
        {
            ClientData.BeginUpdatePersons($"Config_ChangeType {string.Join(" ", mparams)}");
            ViewerAccount? newAccount = null;

            try
            {
                if (account.IsConnected)
                {
                    var clone = new List<ViewerAccount>(ClientData.Viewers);
                    newAccount = new ViewerAccount(account) { IsConnected = true };

                    clone.Add(newAccount);

                    ClientData.Viewers = clone;
                }
                else if (account == ClientData.Me)
                {
                    throw new InvalidOperationException("I am not connected!");
                }

                account.IsHuman = false;
                account.Name = newName;
                account.IsMale = newSex;
                account.Picture = "";
                account.IsConnected = true;
                account.Ready = false;
            }
            finally
            {
                ClientData.EndUpdatePersons();
            }

            if (account == me && newAccount != null)
            {
                // Необходимо самого себя перевести в зрители
                SwitchToNewType(GameRole.Viewer, newAccount);
            }
        }
    }

    private static void ThrowComputerAccountError() =>
        throw new InvalidOperationException($"Computer account should never receive this");

    private void OnConfigDeleteTable(string[] mparams)
    {
        if (mparams.Length < 3)
        {
            return;
        }

        var indexString = mparams[2];

        var me = ClientData.Me;

        if (!int.TryParse(indexString, out int index) || index < 0 || index >= ClientData.Players.Count)
        {
            return;
        }

        PlayerAccount account;
        ViewerAccount? newAccount = null;

        ClientData.BeginUpdatePersons($"Config_DeleteTable {string.Join(" ", mparams)}");

        try
        {
            account = ClientData.Players[index];

            ClientData.Players.RemoveAt(index);

            if (!account.IsHuman && account.Name == ClientData.Name /* for computer accounts being deleted */)
            {
                ThrowComputerAccountError();
            }

            if (account.IsConnected && account.IsHuman)
            {
                newAccount = new ViewerAccount(account) { IsConnected = true };

                var cloneV = new List<ViewerAccount>(ClientData.Viewers)
                {
                    newAccount
                };

                ClientData.Viewers = cloneV;
            }

            _logic.RemovePlayerAt(index);
        }
        finally
        {
            ClientData.EndUpdatePersons();
        }

        if (account == me && newAccount != null && _logic.CanSwitchType)
        {
            // Необходимо самого себя перевести в зрители
            SwitchToNewType(GameRole.Viewer, newAccount);
        }
    }

    private PersonAccount GetAccountByType(bool isPlayer, string indexString)
    {
        if (isPlayer)
        {
            if (!int.TryParse(indexString, out int index) || index < 0 || index >= ClientData.Players.Count)
            {
                return null;
            }

            return ClientData.Players[index];
        }
        
        return ClientData.ShowMan;
    }

    private void OnConfigSet(string[] mparams)
    {
        if (mparams.Length < 6)
        {
            return;
        }

        var personType = mparams[2];
        var indexString = mparams[3];
        var replacer = mparams[4];
        var replacerIsMale = mparams[5] == "+";

        var isPlayer = personType == Constants.Player;

        var me = ClientData.Me;

        // Кого заменяем
        var account = GetAccountByType(isPlayer, indexString);

        if (account == null || account.Name == replacer)
        {
            return;
        }

        if (!account.IsHuman)
        {
            ClientData.BeginUpdatePersons($"Config_Set {string.Join(" ", mparams)}");

            try
            {
                if (account.Name == ClientData.Name /* for computer accounts being deleted */)
                {
                    ThrowComputerAccountError();
                }

                account.Name = replacer;
                account.IsMale = replacerIsMale;
                account.Ready = false;
            }
            finally
            {
                ClientData.EndUpdatePersons();
            }
        }
        else
        {
            var name = account.Name;
            var sex = account.IsMale;
            var picture = account.Picture;
            var videoAvatar = account.AvatarVideoUri;
            var ready = account.Ready;
            var isOnline = account.IsConnected;

            // Кто садится на его место
            ViewerAccount other = null;
            GameRole role = GameRole.Viewer;

            ClientData.BeginUpdatePersons($"Config_Set {string.Join(" ", mparams)}");

            try
            {
                if (isPlayer && ClientData.ShowMan.Name == replacer)
                {
                    // Ведущего сажаем на место игрока
                    var showman = ClientData.ShowMan;
                    other = showman;
                    role = GameRole.Showman;

                    account.Name = showman.Name;
                    account.IsMale = showman.IsMale;
                    account.Picture = showman.Picture;
                    account.AvatarVideoUri = showman.AvatarVideoUri;
                    account.Ready = showman.Ready;
                    account.IsConnected = showman.IsConnected;

                    showman.Name = name;
                    showman.IsMale = sex;
                    showman.Picture = picture;
                    showman.AvatarVideoUri = videoAvatar;
                    showman.Ready = ready;
                    showman.IsConnected = isOnline;
                }
                else
                {
                    var found = false;

                    foreach (var item in ClientData.Players)
                    {
                        if (item.Name == replacer)
                        {
                            other = item;
                            role = GameRole.Player;

                            account.Name = item.Name;
                            account.IsMale = item.IsMale;
                            account.Picture = item.Picture;
                            account.AvatarVideoUri = item.AvatarVideoUri;
                            account.Ready = item.Ready;
                            account.IsConnected = item.IsConnected;

                            item.Name = name;
                            item.IsMale = sex;
                            item.Picture = picture;
                            item.AvatarVideoUri = videoAvatar;
                            item.Ready = ready;
                            item.IsConnected = isOnline;

                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        foreach (var item in ClientData.Viewers)
                        {
                            if (item.Name == replacer)
                            {
                                other = item;
                                role = GameRole.Viewer;

                                account.Name = item.Name;
                                account.IsMale = item.IsMale;
                                account.Picture = item.Picture;
                                account.AvatarVideoUri = item.AvatarVideoUri;
                                account.Ready = false;

                                if (isOnline)
                                {
                                    item.Name = name;
                                    item.IsMale = sex;
                                    item.Picture = picture;
                                    item.AvatarVideoUri = videoAvatar;
                                }
                                else
                                {
                                    // место было пустым, зрителя нужо удалить
                                    ClientData.Viewers.Remove(item);
                                    account.IsConnected = true;
                                }

                                found = true;
                                break;
                            }
                        }
                    }
                }
            }
            finally
            {
                ClientData.EndUpdatePersons();
            }

            if (account == me)
            {
                var newRole = isPlayer ? GameRole.Player : GameRole.Showman;

                if (newRole != role)
                {
                    SwitchToNewType(role, other);
                }
            }
            else if (other == me)
            {
                var newRole = isPlayer ? GameRole.Player : GameRole.Showman;

                if (newRole != role)
                {
                    SwitchToNewType(newRole, account);
                }
            }
        }
    }

    /// <summary>
    /// Сменить тип своего аккаунта
    /// </summary>
    /// <param name="role">Целевой тип</param>
    private void SwitchToNewType(GameRole role, ViewerAccount newAccount)
    {
        if (newAccount == null)
        {
            throw new ArgumentNullException(nameof(newAccount));
        }

        if (!_logic.CanSwitchType)
        {
            throw new InvalidOperationException($"Trying to switch type of computer account:\n{ClientData.Name}");
        }

        IViewerClient viewer = role switch
        {
            GameRole.Viewer => new Viewer(_client, newAccount, IsHost, _logic, _viewerActions, LO, ClientData),
            GameRole.Player => new Player(_client, newAccount, IsHost, _logic, _viewerActions, LO, ClientData),
            _ => new Showman(_client, newAccount, IsHost, _logic, _viewerActions, LO, ClientData),
        };

        viewer.Avatar = Avatar;

        Dispose(); // TODO: do not dispose anything here

        viewer.RecreateCommands();

        _logic.OnClientSwitch(viewer);

        SendAvatar();
    }

    private async ValueTask ProcessInfoAsync(string[] mparams)
    {
        _ = int.TryParse(mparams[1], out var numOfPlayers);
        var numOfViewers = (mparams.Length - 2) / 5 - 1 - numOfPlayers;

        var gameStarted = ClientData.Stage != GameStage.Before;

        var mIndex = 2;
        ClientData.BeginUpdatePersons($"ProcessInfo {string.Join(" ", mparams)}");

        try
        {
            ClientData.ShowMan = new PersonAccount(mparams[mIndex++], mparams[mIndex++] == "+", mparams[mIndex++] == "+", gameStarted)
            {
                IsShowman = true,
                IsHuman = mparams[mIndex++] == "+",
                Ready = mparams[mIndex++] == "+"
            };

            var newPlayers = new List<PlayerAccount>();

            for (int i = 0; i < numOfPlayers; i++)
            {
                var account = new PlayerAccount(mparams[mIndex++], mparams[mIndex++] == "+", mparams[mIndex++] == "+", gameStarted)
                {
                    IsHuman = mparams[mIndex++] == "+",
                    Ready = mparams[mIndex++] == "+"
                };

                newPlayers.Add(account);
            }

            ClientData.Players.Clear();
            ClientData.Players.AddRange(newPlayers);

            var newViewers = new List<ViewerAccount>();

            for (int i = 0; i < numOfViewers; i++)
            {
                var viewerName = mparams[mIndex++];
                var isMale = mparams[mIndex++] == "+";
                var isConnected = mparams[mIndex++] == "+";
                var isHuman = mparams[mIndex++] == "+";

                if (!isConnected)
                {
                    continue; // Viewer cannot be disconnected. There is something wrong on server side
                }

                newViewers.Add(new ViewerAccount(viewerName, isMale, isConnected)
                {
                    IsHuman = isHuman
                });

                mIndex++; // пропускаем Ready
            }

            ClientData.Viewers = newViewers;
            ClientData.IsInfoInitialized = true;

            _logic.OnInfo();
        }
        finally
        {
            ClientData.EndUpdatePersons();
        }

        UpdateShowmanCommands();

        foreach (var account in ClientData.Players)
        {
            UpdateOthers(account);
        }

        if (ClientData.Me != null)
        {
            ClientData.Me.Picture = ClientData.Picture;
        }

        if (!_client.Node.IsMain) // TODO: this should be handled on node level
        {
            foreach (var item in ClientData.AllPersons.Values)
            {
                if (item != ClientData.Me && item.Name != NetworkConstants.GameName)
                {
                    await _client.Node.ConnectionsLock.WithLockAsync(() =>
                    {
                        var connection = ((ISecondaryNode)_client.Node).HostServer;

                        if (connection != null)
                        {
                            lock (connection.ClientsSync)
                            {
                                if (!connection.Clients.Contains(item.Name))
                                {
                                    connection.Clients.Add(item.Name);
                                }
                            }
                        }
                    });
                }
            }
        }

        SendAvatar();
        _viewerActions.SendMessage(Messages.Moveable);
    }

    public void RecreateCommands()
    {
        UpdateShowmanCommands();

        foreach (var player in ClientData.Players)
        {
            UpdateOthers(player);
        }
    }

    private void UpdateShowmanCommands()
    {
        if (ClientData.ShowMan != null)
        {
            UpdateOthers(ClientData.ShowMan);
        }
    }

    private Account[] GetDefaultComputerPlayers() => MyData.DefaultComputerPlayers
        ?? StoredPersonsRegistry.GetDefaultPlayers(LO, MyData.Host.PhotoUri);

    private void UpdateOthers(PlayerAccount player)
    {
        player.Others = player.IsHuman ?
            MyData.AllPersons.Values.Where(p => p.IsHuman)
                .Except(new ViewerAccount[] { player })
                .ToArray()
            : GetDefaultComputerPlayers()
                .Where(a => !MyData.AllPersons.Values.Any(p => !p.IsHuman && p.Name == a.Name))
                .ToArray();
    }

    private void UpdateOthers(PersonAccount showman)
    {
        showman.Others = showman.IsHuman ?
            MyData.AllPersons.Values.Where(p => p.IsHuman).Except(new ViewerAccount[] { showman }).ToArray()
            : Array.Empty<ViewerAccount>();
    }

    private void InsertPerson(string role, Account account, int index)
    {
        ClientData.BeginUpdatePersons($"InsertPerson {role} {account.Name} {index}");

        try
        {
            switch (role)
            {
                case Constants.Showman:
                    ClientData.ShowMan = new PersonAccount(account)
                    {
                        IsHuman = true,
                        IsConnected = true,
                        Ready = false,
                        GameStarted = ClientData.Stage != GameStage.Before,
                        IsShowman = true
                    };

                    UpdateShowmanCommands();
                    break;

                case Constants.Player:
                    var playersWereUpdated = false;

                    while (index >= ClientData.Players.Count)
                    {
                        var p = new PlayerAccount(
                            Constants.FreePlace,
                            true,
                            false,
                            ClientData.Stage != GameStage.Before)
                        {
                            IsHuman = true,
                            Ready = false
                        };

                        UpdateOthers(p);
                        ClientData.Players.Add(p);
                        playersWereUpdated = true;
                    }

                    if (playersWereUpdated)
                    {
                        ClientData.UpdatePlayers();
                    }

                    var player = ClientData.Players[index];

                    if (player.Name == ClientData.Name)
                    {
                        break;
                    }

                    player.Name = account.Name;
                    player.Picture = account.Picture;
                    player.IsMale = account.IsMale;
                    player.IsHuman = true;
                    player.IsConnected = true;
                    player.Ready = false;
                    player.GameStarted = ClientData.Stage != GameStage.Before;
                    break;

                case Constants.Viewer:
                    var viewer = new ViewerAccount(account) { IsHuman = true, IsConnected = true };

                    var existingViewer = ClientData.Viewers.FirstOrDefault(v => v.Name == viewer.Name);

                    if (existingViewer != null)
                    {
                        throw new Exception($"Duplicate viewer name: \"{viewer.Name}\" ({existingViewer.IsConnected})!\n" + ClientData.PersonsUpdateHistory);
                    }

                    ClientData.Viewers.Add(viewer);
                    ClientData.UpdateViewers();
                    break;

                default:
                    throw new ArgumentException($"Unsupported role {role}");
            }
        }
        finally
        {
            ClientData.EndUpdatePersons();
        }

        if (ClientData.Stage == GameStage.Before)
        {
            UpdateShowmanCommands();

            foreach (var player in ClientData.Players)
            {
                UpdateOthers(player);
            }
        }
    }

    protected void OnAskStake(string[] mparams)
    {
        if (mparams.Length < 6
            || !Enum.TryParse<StakeModes>(mparams[1], out var stakeModes)
            || !int.TryParse(mparams[2], out var minimumStake)
            || !int.TryParse(mparams[3], out var maximumStake)
            || !int.TryParse(mparams[4], out var step)
            || !Enum.TryParse<StakeReason>(mparams[5], out var reason))
        {
            return;
        }

        var playerName = mparams.Length > 6 ? mparams[6] : null;

        var personData = ClientData.PersonDataExtensions;

        personData.Var[0] = false;
        personData.Var[1] = stakeModes.HasFlag(StakeModes.Stake);
        personData.Var[2] = stakeModes.HasFlag(StakeModes.Pass);
        personData.Var[3] = stakeModes.HasFlag(StakeModes.AllIn);

        personData.StakeInfo = new StakeInfo
        {
            Minimum = minimumStake,
            Maximum = maximumStake,
            Step = step,
            Stake = minimumStake,
            PlayerName = playerName,
            Reason = reason,
        };

        _logic.MakeStake();
    }

    /// <summary>
    /// Получение сообщения
    /// </summary>
    public override async ValueTask OnMessageReceivedAsync(Message message)
    {
        if (message.IsSystem)
        {
            _logic.AddLog($"[{message.Text}]");

            await ClientData.TaskLock.WithLockAsync(
                async () => await OnSystemMessageReceivedAsync(message.Text.Split('\n')),
                ViewerData.LockTimeoutMs);
        }
        else
        {
            _logic.ReceiveText(message);
        }
    }

    /// <summary>
    /// Сказать в чат
    /// </summary>
    /// <param name="text">Текст сообщения</param>
    /// <param name="whom">Кому</param>
    /// <param name="isPrivate">Приватно ли</param>
    public void Say(string text, string whom = NetworkConstants.Everybody)
    {
        if (whom != NetworkConstants.Everybody)
        {
            text = $"{whom}, {text}";
        }

        whom = NetworkConstants.Everybody;

        _client.SendMessage(text, false, whom);
        _logic.ReceiveText(new Message(text, _client.Name, whom, false));
    }

    public void Pause() => _viewerActions.SendMessage(Messages.Pause, ClientData.TInfo.Pause ? "-" : "+");

    /// <summary>
    /// Sends user avatar (file or uri) to server.
    /// </summary>
    public void SendAvatar()
    {
        if (Avatar != null)
        {
            _viewerActions.SendMessage(Messages.Picture, Avatar);
            return;
        }

        if (!string.IsNullOrWhiteSpace(ClientData.Picture))
        {
            if (!Uri.TryCreate(ClientData.Picture, UriKind.RelativeOrAbsolute, out var uri))
            {
                return;
            }

            if (!uri.IsAbsoluteUri)
            {
                return;
            }

            if (uri.Scheme == "file" && !_client.Node.Contains(NetworkConstants.GameName)) // We should send local file over network
            {
                byte[]? data;

                try
                {
                    data = CoreManager.Instance.GetData(ClientData.Picture);
                }
                catch (Exception exc)
                {
                    _logic.OnReplic(ReplicCodes.Special.ToString(), exc.Message);
                    return;
                }

                if (data == null)
                {
                    _logic.OnReplic(ReplicCodes.Special.ToString(), string.Format(LO[nameof(R.AvatarNotFound)], ClientData.Picture));
                    return;
                }

                _viewerActions.SendMessage(Messages.Picture, ClientData.Picture, Convert.ToBase64String(data));
            }
            else
            {
                _viewerActions.SendMessage(Messages.Picture, ClientData.Picture);
            }
        }
    }

    /// <summary>
    /// Sends game info request.
    /// </summary>
    public void GetInfo() => _viewerActions.SendMessage(Messages.Info);

    protected void OnAskSelectPlayer(string[] mparams)
    {
        if (mparams.Length < 3)
        {
            return;
        }

        for (var i = 0; i < ClientData.Players.Count; i++)
        {
            ClientData.Players[i].CanBeSelected = i + 2 < mparams.Length && mparams[i + 2] == "+";
        }

        _ = Enum.TryParse<SelectPlayerReason>(mparams[1], out var reason);
        _logic.OnSelectPlayer(reason);
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

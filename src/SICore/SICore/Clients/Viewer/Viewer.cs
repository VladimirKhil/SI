using Notions;
using SICore.Models;
using SICore.Network;
using SICore.Network.Clients;
using SICore.Network.Contracts;
using SICore.PlatformSpecific;
using SIData;
using SIEngine.Rules;
using SIPackages.Core;
using SIUI.Model;
using System.Text;

namespace SICore;

/// <summary>
/// Implements a game viewer.
/// </summary>
public class Viewer : MessageHandler, IViewerClient
{
    protected readonly ViewerActions _actions;

    public ViewerActions Actions => _actions;

    public virtual GameRole Role => GameRole.Viewer;

    private readonly IPersonController _controller;

    protected IPersonController Logic => _controller;

    protected ViewerData State { get; }

    public string? Avatar { get; set; }

    /// <summary>
    /// Initializes a new instance of <see cref="Viewer" /> class.
    /// </summary>
    public Viewer(
        Client client,
        Account personData,
        IPersonController controller,
        ViewerActions actions,
        ViewerData state)
        : base(client)
    {
        _actions = actions;
        _controller = controller;
        State = state;
        State.Name = client.Name;
        State.Picture = personData.Picture;
    }

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
                    _controller.OnSelfDisconnected();
                    break;

                case SystemMessages.GameClosed:
                    _controller.OnGameClosed();
                    break;

                case Messages.Disconnected:
                    await OnDisconnectedAsync(mparams);
                    break;

                case Messages.GameMetadata:
                    if (mparams.Length > 3)
                    {
                        _controller.OnGameMetadata(mparams[1], mparams[2], mparams[3], mparams.Length > 4 ? mparams[4] : "");
                    }
                    break;

                case Messages.Info2:
                    await ProcessInfoAsync(mparams);
                    break;

                case Messages.Config:
                    OnConfig(mparams);
                    break;

                case Messages.Options2:
                    _controller.OnOptions2(mparams);
                    break;

                case Messages.Hostname:
                    OnHostName(mparams);
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
                        State.DefaultComputerPlayers = mparams.Skip(1).Select(name => new Account { Name = name }).ToArray();
                        break;
                    }

                case Messages.SetJoinMode:
                    if (mparams.Length < 2 || !Enum.TryParse<JoinMode>(mparams[1], out var joinMode))
                    {
                        break;
                    }

                    _controller.OnSetJoinMode(joinMode);
                    break;

                case Messages.PackageId:
                    {
                        if (mparams.Length > 1)
                        {
                            State.PackageId = mparams[1];
                        }
                        break;
                    }

                case Messages.PackageAuthors:
                    _controller.OnPackageAuthors(mparams.Skip(1));
                    break;

                case Messages.Package:
                    OnPackage(mparams);
                    break;

                case Messages.PackageSources:
                    _controller.OnPackageSources(mparams.Skip(1));
                    break;

                case Messages.PackageComments:
                    OnPackageComments(mparams);
                    break;

                case Messages.Replic:
                    OnReplic(mparams);
                    break;

                case Messages.ShowmanReplic:
                    OnShowmanReplic(mparams);
                    break;

                case Messages.Pause:
                    {
                        #region Pause

                        var isPaused = mparams[1] == "+";
                        _controller.OnPauseChanged(isPaused);

                        if (mparams.Length > 4)
                        {
                            var message = State.TInfo.Pause ? MessageParams.Timer_UserPause : MessageParams.Timer_UserResume;

                            _controller.OnTimerChanged(0, message, mparams[2]);
                            _controller.OnTimerChanged(1, message, mparams[3]);
                            _controller.OnTimerChanged(2, message, mparams[4]);
                        }

                        break;

                        #endregion
                    }

                case Messages.Sums:
                    {
                        #region Sums

                        var players = State.Players;
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

                        if (State.Players.Count == 0)
                        {
                            return;
                        }

                        var ready = mparams.Length == 2 || mparams[2] == "+";

                        var person = State.MainPersons.FirstOrDefault(item => item.Name == mparams[1]);
                        
                        if (person != null)
                        {
                            person.Ready = ready;
                        }

                        #endregion
                        break;
                    }

                case Messages.RoundsNames:
                    State.RoundNames = mparams.Skip(1).ToArray();
                    break;

                case Messages.Stage:
                case Messages.StageInfo:
                    OnStage(mparams);
                    break;

                case Messages.Timer:
                    {
                        if (!int.TryParse(mparams[1], out var timerIndex) || timerIndex < 0 || timerIndex > 2)
                        {
                            return;
                        }

                        var timerCommand = mparams[2];

                        _controller.OnTimerChanged(
                            timerIndex,
                            timerCommand,
                            mparams.Length > 3 ? mparams[3] : "",
                            mparams.Length > 4 ? mparams[4] : null);

                        break;
                    }

                case Messages.GameThemes:
                    OnGameThemes(mparams);
                    break;

                case Messages.RoundThemes2:
                    OnRoundThemes(mparams);
                    break;

                case Messages.RoundThemesComments:
                    OnRoundThemesComments(mparams);
                    break;

                case Messages.RoundContent:
                    _controller.OnRoundContent(mparams);
                    break;

                case Messages.Theme:
                case Messages.Theme2:
                    OnTheme(mparams);
                    break;

                case Messages.ThemeInfo:
                    OnThemeInfo(mparams);
                    break;

                case Messages.Question:
                    if (mparams.Length > 1)
                    {
                        _controller.ClearQuestionState();
                        _controller.SetText(mparams[1], false, TableStage.QuestionPrice);
                        OnThemeOrQuestion();
                        _controller.SetCaption($"{State.ThemeName}, {mparams[1]}");
                    }
                    break;

                case Messages.Table:
                    OnTable(mparams);
                    break;

                case Messages.Toggle:
                    OnToggle(mparams);
                    break;

                case Messages.ShowTable:
                    _controller.ShowTablo();
                    break;

                case Messages.Choice:
                    OnQuestionSelected(mparams);
                    break;

                case Messages.QuestionCaption:
                    if (mparams.Length > 1)
                    {
                        _controller.SetCaption(mparams[1]);
                    }
                    break;

                case Messages.ThemeComments:
                    if (mparams.Length > 1)
                    {
                        _controller.OnThemeComments(mparams[1]);
                    }
                    break;

                case Messages.QuestionAuthors:
                    OnQuestionAuthors(mparams);
                    break;

                case Messages.QuestionSources:
                    OnQuestionSources(mparams);
                    break;

                case Messages.QuestionComments:
                    OnQuestionComments(mparams);
                    break;

                case Messages.QType:
                    OnQuestionType(mparams);
                    break;

                case Messages.Layout:
                    OnLayout(mparams);
                    break;

                case Messages.TextShape: // TODO: remove after v7.11.0 deprecation
                    _controller.OnTextShape(mparams);
                    break;

                case Messages.ContentShape:
                    if (mparams.Length > 4
                        && mparams[1] == ContentPlacements.Screen
                        && mparams[2] == "0"
                        && mparams[3] == ContentTypes.Text)
                    {
                        _controller.OnContentShape(mparams[4].UnescapeNewLines());
                    }
                    break;

                case Messages.Content:
                    _controller.OnContent(mparams);
                    break;

                case Messages.ContentAppend:
                    _controller.OnContentAppend(mparams);
                    break;

                case Messages.ContentState:
                    _controller.OnContentState(mparams);
                    break;

                case Messages.Atom_Hint:
                    if (mparams.Length > 1)
                    {
                        _controller.OnAtomHint(mparams[1]);
                    }
                    break;

                case Messages.MediaLoaded:
                    OnMediaLoaded(mparams);
                    break;

                case Messages.MediaPreloadProgress:
                    OnMediaPreloadProgress(mparams);
                    break;

                case Messages.RightAnswer:
                    _controller.OnRightAnswer(mparams[2]);
                    break;

                case Messages.RightAnswerStart:
                    if (mparams.Length > 2)
                    {
                        _controller.OnRightAnswerStart(mparams[2]);
                    }
                    break;

                case Messages.Resume:
                    _controller.Resume();
                    break;

                case Messages.Try:
                    _controller.Try(mparams.Length > 1 && mparams[1] == MessageParams.Try_NotFinished);
                    break;

                case Messages.EndTry:
                    if (mparams.Length > 1)
                    {
                        if (mparams[1] == MessageParams.EndTry_All)
                        {
                            _controller.OnTimerChanged(1, MessageParams.Timer_Stop, "");
                        }

                        _controller.EndTry(mparams[1]);
                    }
                    break;

                case Messages.StopPlay:
                    _controller.OnStopPlay();
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
                            || playerIndex >= State.Players.Count)
                        {
                            break;
                        }

                        if (!int.TryParse(mparams[3], out var price))
                        {
                            break;
                        }

                        _controller.OnPersonScoreChanged(playerIndex, isRight, price);

                        #endregion
                        break;
                    }

                case Messages.Pass:
                    _controller.OnPersonPass(int.Parse(mparams[1]));
                    break;

                case Messages.PlayerAnswer:
                    OnPlayerAnswer(mparams);
                    break;

                case Messages.PersonFinalStake:
                    OnPersonFinalStake(mparams);
                    break;

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
                    _controller.StopRound();

                    _controller.OnTimerChanged(0, MessageParams.Timer_Stop, "");
                    _controller.OnTimerChanged(1, MessageParams.Timer_Stop, "");
                    _controller.OnTimerChanged(2, MessageParams.Timer_Stop, "");
                    break;

                case Messages.Out:
                    OnOut(mparams);
                    break;

                case Messages.Winner:
                    if (mparams.Length > 1 && int.TryParse(mparams[1], out int winnerIndex))
                    {
                        _controller.OnWinner(winnerIndex);
                    }
                    break;

                case Messages.GameStatistics:
                    OnGameStatistics(mparams);
                    break;

                case Messages.Timeout:
                    _controller.TimeOut();
                    break;

                case Messages.FinalThink:
                    _controller.FinalThink();
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
                        _controller.OnAd(mparams[1]);
                    }
                    break;
            }
        }
        catch (Exception exc)
        {
            _client.Node.OnError(exc, true);
        }
    }

    private void OnGameThemes(string[] mparams)
    {
        for (var i = 1; i < mparams.Length; i++)
        {
            State.TInfo.GameThemes.Add(mparams[i]);
        }

        _controller.OnGameThemes(State.TInfo.GameThemes);
    }

    private void OnPackageComments(string[] mparams)
    {
        if (mparams.Length < 2)
        {
            return;
        }

        _controller.OnPackageComments(mparams[1].UnescapeNewLines());
    }

    private void OnPackage(string[] mparams)
    {
        if (mparams.Length < 2)
        {
            return;
        }

        var logoUri = mparams.Length > 3 ? mparams[3] : null;

        _controller.OnPackage(mparams[1], logoUri);
    }

    private void OnTable(string[] mparams)
    {
        // TODO: clear existing table and renew it
        if (State.TInfo.RoundInfo.Any(t => t.Questions.Any()))
        {
            return;
        }

        var index = 1;

        for (int i = 0; i < State.TInfo.RoundInfo.Count; i++)
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

                State.TInfo.RoundInfo[i].Questions.Add(new QuestionInfo { Price = price });
            }

            index++;
        }

        _controller.TableLoaded(State.TInfo.RoundInfo);
    }

    private void OnPersonFinalStake(string[] mparams)
    {
        if (mparams.Length < 2 || !int.TryParse(mparams[1], out int playerIndex))
        {
            return;
        }

        _controller.OnPersonFinalStake(playerIndex);
    }

    private void OnQuestionComments(string[] mparams)
    {
        if (mparams.Length < 2)
        {
            return;
        }

        _controller.OnReplic(ReplicCodes.Showman.ToString(), mparams[1].UnescapeNewLines());
    }

    private void OnQuestionSources(string[] mparams)
    {
        if (mparams.Length < 2)
        {
            return;
        }

        _controller.OnQuestionSources(mparams.Skip(1));
    }

    private void OnQuestionAuthors(string[] mparams)
    {
        if (mparams.Length < 2)
        {
            return;
        }

        _controller.OnQuestionAuthors(mparams.Skip(1));
    }

    private void OnShowmanReplic(string[] mparams)
    {
        if (mparams.Length < 3
            || !int.TryParse(mparams[1], out var messageIndex)
            || messageIndex < 0
            || !Enum.TryParse<MessageCode>(mparams[2], out var messageCode))
        {
            return;
        }

        _controller.OnShowmanReplic(messageIndex, messageCode);
    }

    private void OnPlayerAnswer(string[] mparams)
    {
        if (mparams.Length < 3 || !int.TryParse(mparams[1], out var playerIndex) || playerIndex < 0 || playerIndex >= State.Players.Count)
        {
            return;
        }

        // ClientData.Players[playerIndex].Answer = mparams[2]; // TODO: for the future use
        _controller.OnReplic(ReplicCodes.Player.ToString() + playerIndex, mparams[2]);
    }

    private void OnMediaPreloadProgress(string[] mparams)
    {
        // TODO: display media preload progress
    }

    private void OnGameStatistics(string[] mparams)
    {
        // TODO: display game statistics
    }

    private void OnStage(string[] mparams)
    {
        if (mparams.Length < 2)
        {
            return;
        }

        State.Stage = (GameStage)Enum.Parse(typeof(GameStage), mparams[1]);

        if (State.Stage != GameStage.Before)
        {
            for (int i = 0; i < State.Players.Count; i++)
            {
                State.Players[i].GameStarted = true;
            }

            State.ShowMan.GameStarted = true;
        }

        var roundIndex = -1;

        if (mparams.Length > 3)
        {
            _ = int.TryParse(mparams[3], out roundIndex);
        }

        if (mparams[0] == Messages.Stage && State.Stage == GameStage.Round)
        {
            for (int i = 0; i < State.Players.Count; i++)
            {
                State.Players[i].InGame = true;
                State.Players[i].IsChooser = false;
            }
        }

        _controller.OnStage(
            mparams[0] == Messages.StageInfo,
            State.Stage,
            mparams.Length > 2 ? mparams[2] : "",
            roundIndex,
            mparams.Length > 4 ? Enum.Parse<QuestionSelectionStrategyType>(mparams[4]) : null);
    }

    private void OnHostName(string[] mparams)
    {
        if (mparams.Length <= 1)
        {
            return;
        }

        State.HostName = mparams[1];
        _controller.OnHostChanged(mparams.Length > 2 ? mparams[2] : null, mparams[1]);
    }

    private void OnOut(string[] mparams)
    {
        if (mparams.Length < 2 || !int.TryParse(mparams[1], out var themeIndex))
        {
            return;
        }

        State.ThemeIndex = themeIndex;

        if (State.ThemeIndex > -1 && State.ThemeIndex < State.TInfo.RoundInfo.Count)
        {
            _controller.Out(State.ThemeIndex);
        }
    }

    private void OnRoundThemesComments(string[] mparams)
    {
        // TODO: save round themes comments
    }

    private void OnTheme(string[] mparams)
    {
        if (mparams.Length <= 5)
        {
            return;
        }

        var themeName = mparams[1];
        
        if (!int.TryParse(mparams[2], out var questionCount))
        {
            questionCount = -1;
        }
        
        var animate = mparams[3] == "+";

        if (!animate)
        {
            OnThemeOrQuestion();
            State.ThemeName = themeName;
            State.ThemeComments = mparams[4].UnescapeNewLines();
        }

        var themeComments = mparams[4].UnescapeNewLines();

        _controller.OnTheme(themeName, themeComments, questionCount, animate);
    }

    private void OnThemeInfo(string[] mparams)
    {
        if (mparams.Length <= 3)
        {
            return;
        }
        
        var themeName = mparams[1];

        State.ThemeName = themeName;
        State.ThemeComments = mparams[3];

        _controller.OnThemeInfo(themeName);
    }

    private void OnSetChooser(string[] mparams)
    {
        if (mparams.Length < 2 || !int.TryParse(mparams[1], out var index))
        {
            return;
        }

        for (int i = 0; i < State.Players.Count; i++)
        {
            State.Players[i].IsChooser = i == index;

            if (mparams.Length > 2 && mparams[2] == "+")
            {
                State.Players[i].State = i == index ? PlayerState.Answering : PlayerState.Pass;
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
        _controller.OnPersonConnected();
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
                && playerIndex < State.Players.Count)
            {
                State.Players[playerIndex].State = state;
            }
        }

        if (state == PlayerState.Lost)
        {
            Task.Run(async () =>
            {
                await Task.Delay(200);

                foreach (var player in State.Players)
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

        var person = State.MainPersons.FirstOrDefault(person => person.Name == mparams[1]);

        if (person != null)
        {
            _controller.UpdateAvatar(person, mparams[2], mparams[3]);
        }
    }

    private void OnAnswers(string[] mparams)
    {
        for (var i = 1; i < mparams.Length && i - 1 < State.Players.Count; i++)
        {
            State.Players[i - 1].Answer = mparams[i];
        }
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

        _controller.OnAnswerOptions(questionHasScreenContent, optionsTypes);
    }

    private void OnQuestionType(string[] mparams)
    {
        State.QuestionType = mparams[1];
        _controller.OnQuestionStart(mparams.Length > 2 && bool.TryParse(mparams[2], out var isDefault) && isDefault);
    }

    private void OnQuestionSelected(string[] mparams)
    {
        if (mparams.Length < 3 || !int.TryParse(mparams[1], out var themeIndex) || !int.TryParse(mparams[2], out var questionIndex))
        {
            return;
        }

        if (themeIndex < 0 || themeIndex >= State.TInfo.RoundInfo.Count)
        {
            return;
        }

        var selectedTheme = State.TInfo.RoundInfo[themeIndex];

        if (questionIndex < 0 || questionIndex >= selectedTheme.Questions.Count)
        {
            return;
        }

        var selectedQuestion = selectedTheme.Questions[questionIndex];

        State.ThemeIndex = themeIndex;
        State.QuestionIndex = questionIndex;

        var questionPrice = mparams.Length > 3 && int.TryParse(mparams[3], out var price) ? price : selectedQuestion.Price;

        _controller.SetCaption($"{selectedTheme.Name}, {questionPrice}");

        foreach (var player in State.Players.ToArray())
        {
            player.ClearState();
        }

        _controller.ClearQuestionState();
        _controller.OnQuestionSelected(themeIndex, questionIndex);
    }

    private void OnMediaLoaded(string[] mparams)
    {
        if (mparams.Length < 2)
        {
            return;
        }

        var player = State.Players.FirstOrDefault(p => p.Name == mparams[1]);

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

        if (themeIndex < 0 || themeIndex >= State.TInfo.RoundInfo.Count)
        {
            return;
        }

        var theme = State.TInfo.RoundInfo[themeIndex];

        if (questionIndex < 0 || questionIndex >= theme.Questions.Count)
        {
            return;
        }

        theme.Questions[questionIndex].Price = price;
        _controller.OnToggle(themeIndex, questionIndex, price);
    }

    private void OnUnbanned(string[] mparams)
    {
        if (mparams.Length < 2)
        {
            return;
        }

        _controller.OnUnbanned(mparams[1]);
    }

    private void OnBanned(string[] mparams)
    {
        if (mparams.Length < 3)
        {
            return;
        }

        _controller.OnBanned(new BannedInfo(mparams[1], mparams[2]));
    }

    private void OnBannedList(string[] mparams)
    {
        var banned = new List<BannedInfo>();

        for (int i = 1; i < mparams.Length - 1; i += 2)
        {
            banned.Add(new BannedInfo(mparams[i], mparams[i + 1]));
        }

        _controller.OnBannedList(banned);
    }

    private async ValueTask OnDisconnectedAsync(string[] mparams)
    {
        if (mparams.Length < 2)
        {
            return;
        }

        var name = mparams[1];

        if (State.AllPersons.TryGetValue(name, out var person))
        {
            State.BeginUpdatePersons($"Disconnected {name}");

            try
            {
                person.IsConnected = false;
                person.Name = Constants.FreePlace;
                person.Picture = "";

                var personAccount = person as PersonAccount;

                if (State.Stage == GameStage.Before && personAccount != null)
                {
                    personAccount.Ready = false;
                }

                if (personAccount == null)
                {
                    State.Viewers.Remove(person);
                }
            }
            finally
            {
                State.EndUpdatePersons();
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

        _controller.OnPersonDisconnected();
    }

    private void OnRoundThemes(string[] mparams)
    {
        if (mparams.Length < 2 || !Enum.TryParse<ThemesPlayMode>(mparams[1], out var playMode))
        {
            return;
        }

        State.TInfo.RoundInfo.Clear();

        for (var i = 2; i < mparams.Length; i++)
        {
            State.TInfo.RoundInfo.Add(new ThemeInfo { Name = mparams[i] });
        }

        _controller.RoundThemes(State.TInfo.RoundInfo.Select(t => t.Name).ToList(), playMode);
    }

    private void OnThemeOrQuestion()
    {
        foreach (var player in State.Players)
        {
            player.ClearState();
        }
    }

    private void OnPersonStake(string[] mparams)
    {
        if (mparams.Length < 3 ||
            !int.TryParse(mparams[1], out var stakerIndex) ||
            stakerIndex < 0 ||
            stakerIndex >= State.Players.Count ||
            !int.TryParse(mparams[2], out var stakeType))
        {
            return;
        }

        int stake;

        if (stakeType == 0)
        {
            stake = -1;
        }
        else if (stakeType == 2)
        {
            stake = -2;
            State.Players[stakerIndex].State = PlayerState.Pass;
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
                State.Players[stakerIndex].SafeStake = true;
            }
        }

        State.Players[stakerIndex].Stake = stake;
        _controller.OnPersonStake(stakerIndex);
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

        _controller.OnReplic(personCode, text.ToString().Trim());
    }

    private void OnConfig(string[] mparams)
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

        _controller.OnPersonsUpdated();
    }

    private void OnConfigAddTable(string[] mparams)
    {
        State.BeginUpdatePersons($"Config_AddTable {string.Join(" ", mparams)}");

        try
        {
            var account = new PlayerAccount(mparams[2], mparams[3] == "+", mparams[4] == "+", State.Stage != GameStage.Before)
            {
                IsHuman = mparams[5] == "+",
                Ready = mparams[6] == "+"
            };

            State.Players.Add(account);
            _controller.AddPlayer(account);
        }
        finally
        {
            State.EndUpdatePersons();
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

        var me = State.Me;

        PersonAccount account;

        var isPlayer = personType == Constants.Player;
        
        if (isPlayer)
        {
            if (!int.TryParse(indexString, out int index) || index < 0 || index >= State.Players.Count)
            {
                return;
            }

            account = State.Players[index];
        }
        else
        {
            account = State.ShowMan;
        }

        var clone = new List<ViewerAccount>(State.Viewers);
        var newAccount = new ViewerAccount(account) { IsConnected = true };

        clone.Add(newAccount);

        State.BeginUpdatePersons($"Config_Free {string.Join(" ", mparams)}");
        
        try
        {
            State.Viewers = clone;

            account.Name = Constants.FreePlace;
            account.IsConnected = false;
            account.Ready = false;
            account.Picture = "";
        }
        finally
        {
            State.EndUpdatePersons();
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

        var me = State.Me;

        PersonAccount account;
        var isPlayer = personType == Constants.Player;

        if (isPlayer)
        {
            if (!int.TryParse(indexString, out var index) || index < 0 || index >= State.Players.Count)
            {
                return;
            }

            account = State.Players[index];
        }
        else
        {
            account = State.ShowMan;
        }

        if (account.IsHuman == newTypeHuman)
        {
            return;
        }

        if (newTypeHuman)
        {
            State.BeginUpdatePersons($"Config_ChangeType {string.Join(" ", mparams)}");

            try
            {
                if (account.Name == State.Name /* for computer accounts being deleted */)
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
                State.EndUpdatePersons();
            }
        }
        else
        {
            State.BeginUpdatePersons($"Config_ChangeType {string.Join(" ", mparams)}");
            ViewerAccount? newAccount = null;

            try
            {
                if (account.IsConnected)
                {
                    var clone = new List<ViewerAccount>(State.Viewers);
                    newAccount = new ViewerAccount(account) { IsConnected = true };

                    clone.Add(newAccount);

                    State.Viewers = clone;
                }
                else if (account == State.Me)
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
                State.EndUpdatePersons();
            }

            if (account == me && newAccount != null)
            {
                // Needs to switch to viewer type
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

        var me = State.Me;

        if (!int.TryParse(indexString, out int index) || index < 0 || index >= State.Players.Count)
        {
            return;
        }

        PlayerAccount account;
        ViewerAccount? newAccount = null;

        State.BeginUpdatePersons($"Config_DeleteTable {string.Join(" ", mparams)}");

        try
        {
            account = State.Players[index];

            State.Players.RemoveAt(index);

            if (!account.IsHuman && account.Name == State.Name /* for computer accounts being deleted */)
            {
                ThrowComputerAccountError();
            }

            if (account.IsConnected && account.IsHuman)
            {
                newAccount = new ViewerAccount(account) { IsConnected = true };

                var cloneV = new List<ViewerAccount>(State.Viewers)
                {
                    newAccount
                };

                State.Viewers = cloneV;
            }

            _controller.RemovePlayerAt(index);
        }
        finally
        {
            State.EndUpdatePersons();
        }

        if (account == me && newAccount != null && _controller.CanSwitchType)
        {
            // Необходимо самого себя перевести в зрители
            SwitchToNewType(GameRole.Viewer, newAccount);
        }
    }

    private PersonAccount? GetAccountByType(bool isPlayer, string indexString)
    {
        if (isPlayer)
        {
            if (!int.TryParse(indexString, out int index) || index < 0 || index >= State.Players.Count)
            {
                return null;
            }

            return State.Players[index];
        }
        
        return State.ShowMan;
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

        var me = State.Me;

        // Кого заменяем
        var account = GetAccountByType(isPlayer, indexString);

        if (account == null || account.Name == replacer)
        {
            return;
        }

        if (!account.IsHuman)
        {
            State.BeginUpdatePersons($"Config_Set {string.Join(" ", mparams)}");

            try
            {
                if (account.Name == State.Name /* for computer accounts being deleted */)
                {
                    ThrowComputerAccountError();
                }

                account.Name = replacer;
                account.IsMale = replacerIsMale;
                account.Ready = false;
            }
            finally
            {
                State.EndUpdatePersons();
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

            // Replacer
            ViewerAccount? other = null;
            GameRole role = GameRole.Viewer;

            State.BeginUpdatePersons($"Config_Set {string.Join(" ", mparams)}");

            try
            {
                if (isPlayer && State.ShowMan.Name == replacer)
                {
                    // Put the showman in the place of the player
                    var showman = State.ShowMan;
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

                    foreach (var item in State.Players)
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
                        foreach (var item in State.Viewers)
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
                                    // the place was empty, the viewer needs to be deleted
                                    State.Viewers.Remove(item);
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
                State.EndUpdatePersons();
            }

            if (other == null)
            {
                return;
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

        if (!_controller.CanSwitchType)
        {
            throw new InvalidOperationException($"Trying to switch type of computer account:\n{State.Name}");
        }

        IViewerClient viewer = role switch
        {
            GameRole.Viewer => new Viewer(_client, newAccount, _controller, _actions, State),
            GameRole.Player => new Player(_client, newAccount, _controller, _actions, State),
            _ => new Showman(_client, newAccount, _controller, _actions, State),
        };

        viewer.Avatar = Avatar;

        Dispose(); // TODO: do not dispose anything here

        _controller.OnClientSwitch(viewer);

        SendAvatar();
    }

    private async ValueTask ProcessInfoAsync(string[] mparams)
    {
        _ = int.TryParse(mparams[1], out var playerCount);
        var viewerCount = (mparams.Length - 2) / 5 - 1 - playerCount;

        var gameStarted = State.Stage != GameStage.Before;

        var mIndex = 2;
        State.BeginUpdatePersons($"ProcessInfo {string.Join(" ", mparams)}");

        try
        {
            var showman = State.ShowMan;

            showman.Name = mparams[mIndex++];
            showman.IsMale = mparams[mIndex++] == "+";
            showman.IsConnected = mparams[mIndex++] == "+";
            showman.GameStarted = gameStarted;
            showman.IsHuman = mparams[mIndex++] == "+";
            showman.Ready = mparams[mIndex++] == "+";

            var newPlayers = new List<PlayerAccount>();

            for (var i = 0; i < playerCount; i++)
            {
                var account = new PlayerAccount(mparams[mIndex++], mparams[mIndex++] == "+", mparams[mIndex++] == "+", gameStarted)
                {
                    IsHuman = mparams[mIndex++] == "+",
                    Ready = mparams[mIndex++] == "+"
                };

                newPlayers.Add(account);
            }

            State.Players.Clear();
            State.Players.AddRange(newPlayers);

            var newViewers = new List<ViewerAccount>();

            for (var i = 0; i < viewerCount; i++)
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

                mIndex++; // skipping "Ready" status
            }

            State.Viewers = newViewers;
            State.IsInfoInitialized = true;

            _controller.OnInfo();
        }
        finally
        {
            State.EndUpdatePersons();
        }

        if (State.Me != null)
        {
            State.Me.Picture = State.Picture;
        }

        if (!_client.Node.IsMain) // TODO: this should be handled on node level
        {
            foreach (var item in State.AllPersons.Values)
            {
                if (item != State.Me && item.Name != NetworkConstants.GameName)
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
        _actions.SendMessage(Messages.Moveable);
    }

    private void InsertPerson(string role, Account account, int index)
    {
        State.BeginUpdatePersons($"InsertPerson {role} {account.Name} {index}");

        try
        {
            switch (role)
            {
                case Constants.Showman:
                    var showman = State.ShowMan;

                    showman.Name = account.Name;
                    showman.IsMale = account.IsMale;
                    showman.Picture = account.Picture;
                    showman.IsHuman = true;
                    showman.IsConnected = true;
                    showman.Ready = false;
                    showman.GameStarted = State.Stage != GameStage.Before;
                    break;

                case Constants.Player:
                    var playersWereUpdated = false;

                    while (index >= State.Players.Count)
                    {
                        var p = new PlayerAccount(
                            Constants.FreePlace,
                            true,
                            false,
                            State.Stage != GameStage.Before)
                        {
                            IsHuman = true,
                            Ready = false
                        };

                        State.Players.Add(p);
                        playersWereUpdated = true;
                    }

                    if (playersWereUpdated)
                    {
                        State.UpdatePlayers();
                    }

                    var player = State.Players[index];

                    if (player.Name == State.Name)
                    {
                        break;
                    }

                    player.Name = account.Name;
                    player.Picture = account.Picture;
                    player.IsMale = account.IsMale;
                    player.IsHuman = true;
                    player.IsConnected = true;
                    player.Ready = false;
                    player.GameStarted = State.Stage != GameStage.Before;
                    break;

                case Constants.Viewer:
                    var viewer = new ViewerAccount(account) { IsHuman = true, IsConnected = true };

                    var existingViewer = State.Viewers.FirstOrDefault(v => v.Name == viewer.Name);

                    if (existingViewer != null)
                    {
                        throw new Exception($"Duplicate viewer name: \"{viewer.Name}\" ({existingViewer.IsConnected})!\n" + State.PersonsUpdateHistory);
                    }

                    State.Viewers.Add(viewer);
                    State.UpdateViewers();
                    break;

                default:
                    throw new ArgumentException($"Unsupported role {role}");
            }
        }
        finally
        {
            State.EndUpdatePersons();
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

        State.StakeInfo = new StakeInfo
        {
            Minimum = minimumStake,
            Maximum = maximumStake,
            Step = step,
            Stake = minimumStake,
            PlayerName = playerName,
            Reason = reason,
            Modes = stakeModes,
        };

        _controller.MakeStake();
    }

    /// <summary>
    /// Получение сообщения
    /// </summary>
    public override async ValueTask OnMessageReceivedAsync(Message message)
    {
        if (message.IsSystem)
        {
            _controller.AddLog($"[{message.Text}]");

            await State.TaskLock.WithLockAsync(
                async () => await OnSystemMessageReceivedAsync(message.Text.Split('\n')),
                ViewerData.LockTimeoutMs);
        }
        else
        {
            _controller.ReceiveText(message);
        }
    }

    /// <summary>
    /// Сказать в чат
    /// </summary>
    /// <param name="text">Текст сообщения</param>
    /// <param name="whom">Кому</param>
    public void Say(string text, string whom = NetworkConstants.Everybody)
    {
        if (whom != NetworkConstants.Everybody)
        {
            text = $"{whom}, {text}";
        }

        whom = NetworkConstants.Everybody;

        _client.SendMessage(text, false, whom);
        _controller.ReceiveText(new Message(text, _client.Name, whom, false));
    }

    /// <summary>
    /// Sends user avatar (file or uri) to server.
    /// </summary>
    public void SendAvatar()
    {
        if (Avatar != null)
        {
            _actions.SendMessage(Messages.Picture, Avatar);
            return;
        }

        if (!string.IsNullOrWhiteSpace(State.Picture))
        {
            if (!Uri.TryCreate(State.Picture, UriKind.RelativeOrAbsolute, out var uri))
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
                    data = CoreManager.Instance.GetData(State.Picture);
                }
                catch (Exception exc)
                {
                    _controller.OnReplic(ReplicCodes.Special.ToString(), exc.Message);
                    return;
                }

                if (data == null)
                {
                    _client.Node.OnError(new Exception($"Avatar file not found: {State.Picture}"), true);
                    return;
                }

                _actions.SendMessage(Messages.Picture, State.Picture, Convert.ToBase64String(data));
            }
            else
            {
                _actions.SendMessage(Messages.Picture, State.Picture);
            }
        }
    }

    protected void OnAskSelectPlayer(string[] mparams)
    {
        if (mparams.Length < 3)
        {
            return;
        }

        for (var i = 0; i < State.Players.Count; i++)
        {
            State.Players[i].CanBeSelected = i + 2 < mparams.Length && mparams[i + 2] == "+";
        }

        _ = Enum.TryParse<SelectPlayerReason>(mparams[1], out var reason);
        _controller.OnSelectPlayer(reason);
    }
}

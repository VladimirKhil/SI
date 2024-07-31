﻿using Notions;
using SICore.BusinessLogic;
using SICore.Clients;
using SICore.Clients.Game;
using SICore.Contracts;
using SICore.Extensions;
using SICore.Models;
using SICore.Results;
using SICore.Utils;
using SIData;
using SIEngine.Rules;
using SIPackages;
using SIPackages.Core;
using SIPackages.Providers;
using SIUI.Model;
using System.Text;
using System.Text.RegularExpressions;
using Utils.Timers;
using R = SICore.Properties.Resources;

namespace SICore;

// TODO: all logic based on RoundTypes.Final or GameModes.Tv/Sport must be eliminated
// All rules must be handled by game engine

/// <summary>
/// Executes SIGame logic implemented as a state machine.
/// </summary>
public sealed class GameLogic : Logic<GameData>, ITaskRunHandler<Tasks>, IDisposable
{
    /// <summary>
    /// Random package marker.
    /// </summary>
    private const string RandomIndicator = "@{random}";

    private const string OfObjectPropertyFormat = "{0} {1}: {2}";

    private const int MaxAnswerLength = 350;

    private const int DefaultAudioVideoTime = 1200; // maximum audio/video duration (120 s)

    private const int DefaultImageTime = 50;

    /// <summary>
    /// Maximum number of oversized media notifications.
    /// </summary>
    public const int MaxMediaNotifications = 15;

    /// <summary>
    /// Maximum penalty value for a player.
    /// </summary>
    private const int MaxPenalty = 10;

    /// <summary>
    /// Value of penalty increment for each hit.
    /// </summary>
    private const int PenaltyIncrement = 3;

    /// <summary>
    /// Frequency of partial prints per second.
    /// </summary>
    private const double PartialPrintFrequencyPerSecond = 0.5;

    /// <summary>
    /// Represents character used to form content shape.
    /// </summary>
    private const string ContentShapeCharacter = "&";

    /// <summary>
    /// Execution continuation.
    /// </summary>
    private Action? _continuation;

    internal void SetContinuation(Action continuation) => _continuation = continuation;

    internal void ClearContinuation() => _continuation = null;

    /// <summary>
    /// Minimum price in round.
    /// </summary>
    private int _minRoundPrice = 1;

    /// <summary>
    /// Maximum price in round.
    /// </summary>
    private int _maxRoundPrice = 1;

    public object? UserState { get; set; }

    private readonly GameActions _gameActions;

    private readonly ILocalizer LO;

    internal event Action? AutoGame;

    private readonly HistoryLog _tasksHistory = new();

    public SIEngine.EngineBase Engine { get; }

    public event Action<GameLogic, GameStages, string, int, int>? StageChanged;

    public event Action<string, int, int>? AdShown;

    internal void OnStageChanged(
        GameStages stage,
        string stageName,
        int progressCurrent = 0,
        int progressTotal = 0) => StageChanged?.Invoke(this, stage, stageName, progressCurrent, progressTotal);

    internal void OnAdShown(int adId) =>
        AdShown?.Invoke(LO.Culture.TwoLetterISOLanguageName, adId, ClientData.AllPersons.Values.Count(p => p.IsHuman));

    private readonly IFileShare _fileShare;
    private readonly TaskRunner<Tasks> _taskRunner;

    protected StopReason _stopReason = StopReason.None;

    internal StopReason StopReason => _stopReason;

    internal TaskRunner<Tasks> Runner => _taskRunner;

    public GameLogic(GameData data, GameActions gameActions, SIEngine.EngineBase engine, ILocalizer localizer, IFileShare fileShare)
        : base(data)
    {
        _gameActions = gameActions;
        Engine = engine;
        LO = localizer;
        _fileShare = fileShare;
        _taskRunner = new(this);
    }

    internal void Run()
    {
        Engine.Package += Engine_Package;
        Engine.GameThemes += Engine_GameThemes;
        Engine.RoundSkip += Engine_RoundSkip;

        Engine.QuestionPostInfo += Engine_QuestionPostInfo;
        Engine.QuestionFinish += Engine_QuestionFinish;
        Engine.NextQuestion += Engine_NextQuestion;
        Engine.RoundEmpty += Engine_RoundEmpty;
        Engine.RoundTimeout += Engine_RoundTimeout;

        Engine.EndGame += Engine_EndGame;

        _data.PackageDoc = Engine.Document;

        _data.GameResultInfo.Name = _data.GameName;
        _data.GameResultInfo.Language = _data.Settings.AppSettings.Culture;
        _data.GameResultInfo.PackageName = Engine.PackageName;
        _data.GameResultInfo.PackageHash = ""; // Will not use hash for now
        _data.GameResultInfo.PackageAuthors = Engine.Document.Package.Info.Authors.ToArray();
        _data.GameResultInfo.PackageAuthorsContacts = Engine.Document.Package.ContactUri;

        if (_data.Settings.IsAutomatic)
        {
            // The game should be started automatically
            ScheduleExecution(Tasks.AutoGame, Constants.AutomaticGameStartDuration);
            _data.TimerStartTime[2] = DateTime.UtcNow;

            _gameActions.SendMessageWithArgs(Messages.Timer, 2, MessageParams.Timer_Go, Constants.AutomaticGameStartDuration, -2);
        }
    }

    private void Engine_RoundSkip()
    {
        _gameActions.ShowmanReplic(LO[nameof(R.NobodyInFinal)]);
        ScheduleExecution(Tasks.MoveNext, 15 + Random.Shared.Next(10), 1);
    }

    private void Engine_QuestionFinish()
    {
        if (_data.IsRoundEnding)
        {
            return;
        }

        var roundDuration = DateTime.UtcNow.Subtract(_data.TimerStartTime[0]).TotalMilliseconds / 100;

        if (_data.Stage == GameStage.Round && roundDuration >= _data.Settings.AppSettings.TimeSettings.TimeOfRound * 10)
        {
            // Round timeout
            _gameActions.SendMessageWithArgs(Messages.Timer, 0, MessageParams.Timer_Stop);
            Engine.SetTimeout();
        }
    }

    internal void OnContentScreenHtml(ContentItem contentItem)
    {
        _data.IsPartial = false;
        _data.MediaOk = ShareMedia(contentItem);

        int atomTime = GetContentItemDuration(contentItem, DefaultImageTime + _data.Settings.AppSettings.TimeSettings.TimeForMediaDelay * 10);

        _data.AtomTime = atomTime;
        _data.AtomStart = DateTime.UtcNow;

        ScheduleExecution(Tasks.MoveNext, atomTime);

        _data.TimeThinking = 0.0;
    }

    internal void ProcessApellationRequest()
    {
        ClientData.AppellationCallerIndex = -1;
        ClientData.AppelaerIndex = -1;

        if (ClientData.IsAppelationForRightAnswer)
        {
            for (var i = 0; i < ClientData.Players.Count; i++)
            {
                if (ClientData.Players[i].Name == ClientData.AppellationSource)
                {
                    for (var j = 0; j < ClientData.QuestionHistory.Count; j++)
                    {
                        var index = ClientData.QuestionHistory[j].PlayerIndex;

                        if (index == i)
                        {
                            if (!ClientData.QuestionHistory[j].IsRight)
                            {
                                ClientData.AppelaerIndex = index;
                            }

                            break;
                        }
                    }

                    break;
                }
            }
        }
        else
        {
            if (ClientData.Players.Count <= 3)
            {
                // If there are 2 or 3 players, there are already 2 positive votes for the answer
                // from answered player and showman. And only 1 or 2 votes left.
                // So there is no chance to win a vote against the answer
                _gameActions.SpecialReplic(string.Format(LO[nameof(R.FailedToAppellateForWrongAnswer)], ClientData.AppellationSource));
                return;
            }

            for (var i = 0; i < ClientData.Players.Count; i++)
            {
                if (ClientData.Players[i].Name == ClientData.AppellationSource)
                {
                    ClientData.AppellationCallerIndex = i;
                    break;
                }
            }

            if (ClientData.AppellationCallerIndex == -1)
            {
                // Only players can appellate
                return;
            }

            // Last person has right answer and is responsible for appellation
            var count = ClientData.QuestionHistory.Count;

            if (count > 0 && ClientData.QuestionHistory[count - 1].IsRight)
            {
                ClientData.AppelaerIndex = ClientData.QuestionHistory[count - 1].PlayerIndex;
            }
        }

        if (ClientData.AppelaerIndex != -1)
        {
            // Appellation started
            ClientData.AllowAppellation = false;
            Stop(StopReason.Appellation);
        }
    }

    private void Engine_QuestionPostInfo()
    {
        _tasksHistory.AddLogEntry("Engine_QuestionPostInfo: Appellation activated");

        _data.AllowAppellation = _data.Settings.AppSettings.UseApellations;
        _data.IsPlayingMedia = false;
        _gameActions.SendMessageWithArgs(Messages.QuestionEnd);
        ScheduleExecution(Tasks.QuestSourComm, 1, 1, force: true);

        if (_data.AllowAppellation && _data.PendingApellation)
        {
            ProcessApellationRequest();
        }
    }

    private void Engine_Package(Package package)
    {
        _data.Package = package;

        _data.Rounds = _data.Package.Rounds
            .Select((round, index) => new RoundInfo { Index = index, Name = round.Name })
            .ToArray();

        if (_data.Package.Info.Comments.Text.StartsWith(PackageHelper.RandomIndicator))
        {
            _data.GameResultInfo.PackageName += string.Concat("\n", _data.Package.Info.Comments.Text.AsSpan(8));
        }

        _gameActions.SendMessage(string.Join(Message.ArgsSeparator, Messages.PackageId, package.ID));
        _gameActions.InformRoundsNames();

        OnPackage(package, 1);
    }

    private void Engine_GameThemes(IEnumerable<string> gameThemes)
    {
        _gameActions.ShowmanReplic(GetRandomString(LO[nameof(R.GameThemes)]));
        var msg = new MessageBuilder(Messages.GameThemes).AddRange(gameThemes);
        _gameActions.SendMessage(msg.Build());
        _ = gameThemes.TryGetNonEnumeratedCount(out var count);
        ScheduleExecution(Tasks.MoveNext, Math.Max(40, 10 + 10 * count));
    }

    internal void OnRoundStart(Round round, QuestionSelectionStrategyType strategyType)
    {
        _data.AppellationOpened = false;
        _data.AllowAppellation = false;
        _data.Round = round;
        _data.CanMarkQuestion = false;
        _data.AnswererIndex = -1;
        _data.QuestionPlayState.Clear();
        _data.StakerIndex = -1;
        _data.ThemeDeleters = null;
        _data.RoundStrategy = strategyType;

        OnRound(round, 1);
        SetRoundPrices(round);
        RunRoundTimer();
    }

    private void SetRoundPrices(Round round)
    {
        _minRoundPrice = -1;
        _maxRoundPrice = 1;

        foreach (var theme in round.Themes)
        {
            foreach (var quest in theme.Questions)
            {
                var price = quest.Price;

                if (price > 0 && (price < _minRoundPrice || _minRoundPrice == -1))
                {
                    _minRoundPrice = price;
                }

                if (price > _maxRoundPrice)
                {
                    _maxRoundPrice = price;
                }
            }
        }

        _minRoundPrice = Math.Max(1, _minRoundPrice);
        ClientData.StakeStep = (int)Math.Pow(10, Math.Floor(Math.Log10(_minRoundPrice))); // Maximum power of 10 <= _minRoundPrice
    }

    internal void InitThemes(IEnumerable<Theme> themes, bool willPlayAllThemes, bool isFirstPlay, ThemesPlayMode playMode)
    {
        _data.TInfo.RoundInfo.Clear();

        foreach (var theme in themes)
        {
            _data.TInfo.RoundInfo.Add(new ThemeInfo { Name = theme.Name });
        }

        string themesReplic;

        if (willPlayAllThemes)
        {
            themesReplic = isFirstPlay
                ? $"{GetRandomString(LO[nameof(R.RoundThemes)])}. {LO[nameof(R.WeWillPlayAllOfThem)]}"
                : LO[nameof(R.LetsPlayNextTheme)];
        }
        else
        {
            themesReplic = GetRandomString(LO[nameof(R.RoundThemes)]);
        }

        _gameActions.ShowmanReplic(themesReplic);

        _data.TableInformStageLock.WithLock(() =>
        {
            _gameActions.InformRoundThemes(playMode: playMode);
            _data.TableInformStage = 1;
        },
        5000);
    }

    /// <summary>
    /// Gets count of questions left to play in current round.
    /// </summary>
    internal int GetRoundActiveQuestionsCount() => _data.TInfo.RoundInfo.Sum(theme => theme.Questions.Count(QuestionHelper.IsActive));

    internal void OnQuestion(Question question)
    {
        _data.Question = question;

        _gameActions.ShowmanReplic($"{_data.Theme.Name}, {question.Price}");
        _gameActions.SendMessageWithArgs(Messages.Question, question.Price);

        InitQuestionState(_data.Question);
        ProceedToThemeAndQuestion();
    }

    internal void OnQuestionSelected(int themeIndex, int questionIndex)
    {
        if (_data.Round == null)
        {
            throw new InvalidOperationException("_data.Round == null");
        }

        _gameActions.SendMessageWithArgs(Messages.Choice, themeIndex, questionIndex);

        _data.Theme = _data.Round.Themes[themeIndex];
        _data.Question = _data.Theme.Questions[questionIndex];

        _data.TInfo.RoundInfo[themeIndex].Questions[questionIndex].Price = Question.InvalidPrice;

        InitQuestionState(_data.Question);
        ProceedToThemeAndQuestion(20);
    }

    private void ProceedToThemeAndQuestion(int delay = 10)
    {
        if (!_data.ThemeInfoShown.Contains(_data.Theme))
        {
            ScheduleExecution(Tasks.Theme, delay, 1, true);
        }
        else
        {
            ScheduleExecution(Tasks.QuestionType, delay, 1, true);
        }
    }

    private void InitQuestionState(Question question)
    {
        _data.AppellationOpened = false;
        _data.AllowAppellation = false;
        _data.IsAnswer = false;
        _data.QuestionHistory.Clear();
        _data.PendingAnswererIndex = -1;
        _data.AnswererPressDuration = -1;
        _data.PendingAnswererIndicies.Clear();
        _data.IsQuestionPlaying = true;
        _data.IsPlayingMedia = false;
        _data.IsPlayingMediaPaused = false;
        _data.CurPriceRight = _data.CurPriceWrong = question.Price;
        _data.Order = Array.Empty<int>();
        _data.OrderIndex = -1;

        if (_data.Settings.AppSettings.HintShowman)
        {
            var rightAnswers = question.Right;
            var rightAnswer = rightAnswers.FirstOrDefault() ?? LO[nameof(R.NotSet)];

            _gameActions.SendMessage(string.Join(Message.ArgsSeparator, Messages.Hint, rightAnswer), _data.ShowMan.Name);
        }
    }

    internal void OnContentScreenText(string text, bool waitForFinish, TimeSpan duration)
    {
        var atomTime = duration > TimeSpan.Zero ? (int)(duration.TotalMilliseconds / 100) : GetReadingDurationForTextLength(text.Length);

        _data.AtomTime = atomTime;
        _data.AtomStart = DateTime.UtcNow;
        _data.UseBackgroundAudio = !waitForFinish;

        _data.IsPartial = waitForFinish && IsPartial();

        if (_data.IsPartial)
        {
            // ContentShapeCharacter symbol is used as an arbitrary symbol with medium width to define the question text shape
            // Real question text is sent later and it sequentially replaces test shape
            // Text shape is required to display partial question on the screen correctly
            // (font size and number of lines must be calculated in the beginning to prevent UI flickers on question text growth)
            var shape = Regex.Replace(text, "[^\r\n\t\f ]", ContentShapeCharacter);
            _gameActions.SendMessageWithArgs(Messages.TextShape, shape);
            _gameActions.SendMessageWithArgs(Messages.ContentShape, ContentPlacements.Screen, 0, ContentTypes.Text, shape.EscapeNewLines());

            _data.Text = text;
            _data.InitialPartialTextLength = 0;
            _data.PartialIterationCounter = 0;
            _data.TextLength = 0;
            ScheduleExecution(Tasks.PrintPartial, 1);
            return;
        }

        _gameActions.SendMessageWithArgs(Messages.Content, ContentPlacements.Screen, 0, ContentTypes.Text, text.EscapeNewLines());
        _gameActions.SendMessageWithArgs(Messages.Atom, AtomTypes.Text, text);
        _gameActions.SystemReplic(text);

        var nextTime = !waitForFinish ? 1 : atomTime;

        ScheduleExecution(Tasks.MoveNext, nextTime);

        _data.TimeThinking = 0.0;
    }

    /// <summary>
    /// Should the question be displayed partially.
    /// </summary>
    private bool IsPartial() =>
        !IsSpecialQuestion()
            && _data.Settings != null
            && !_data.Settings.AppSettings.FalseStart
            && _data.Settings.AppSettings.PartialText
            && !_data.IsAnswer;

    internal void OnContentReplicText(string text, bool waitForFinish, TimeSpan duration)
    {
        _data.IsPartial = false;
        // There is no need to send content for now, as we can send replic directly
        //_gameActions.SendMessageWithArgs(Messages.Content, ContentPlacements.Replic, 0, ContentTypes.Text, text.EscapeNewLines());
        _gameActions.SendMessageWithArgs(Messages.Atom, AtomTypes.Oral, text);
        _gameActions.ShowmanReplic(text);

        var atomTime = !waitForFinish ? 1 : (duration > TimeSpan.Zero ? (int)(duration.TotalMilliseconds / 100) : GetReadingDurationForTextLength(text.Length));

        _data.AtomTime = atomTime;
        _data.AtomStart = DateTime.UtcNow;

        ScheduleExecution(Tasks.MoveNext, atomTime);

        _data.TimeThinking = 0.0;
        _data.UseBackgroundAudio = !waitForFinish;
    }

    private (bool success, string? globalUri, string? localUri) TryShareContent(ContentItem contentItem)
    {
        if (!contentItem.IsRef) // External link
        {
            var link = contentItem.Value;

            if (Uri.TryCreate(link, UriKind.Absolute, out _))
            {
                return (true, link, null);
            }

            // There is no file in the package and it's name is not a valid absolute uri.
            // So, considering that the file is missing

            return (false, link, null);
        }

        var contentType = contentItem.Type;
        var mediaCategory = CollectionNames.TryGetCollectionName(contentType) ?? contentType;
        var media = _data.PackageDoc.TryGetMedia(contentItem);

        if (!media.HasValue || media.Value.Uri == null)
        {
            return (false, $"{mediaCategory}/{contentItem.Value}", null);
        }

        var fullUri = media.Value.Uri;
        var fileLength = media.Value.StreamLength;

        if (fileLength.HasValue)
        {
            CheckFileLength(contentType, fileLength.Value);
        }

        string globalUri;
        string? localUri;

        if (fullUri.Scheme == "file")
        {
            localUri = fullUri.AbsolutePath;
            var fileName = Path.GetFileName(localUri);
            globalUri = _fileShare.CreateResourceUri(ResourceKind.Package, new Uri($"{mediaCategory}/{fileName}", UriKind.Relative));
        }
        else
        {
            globalUri = fullUri.ToString();
            localUri = null;
        }

        return (true, globalUri, localUri);
    }

    private bool ShareMedia(ContentItem contentItem, bool isBackground = false)
    {
        try
        {
            var (success, globalUri, localUri) = TryShareContent(contentItem);
            var messageType = isBackground ? Messages.Atom_Second : Messages.Atom;

            if (!success || globalUri == null)
            {
                var errorText = string.Format(LO[nameof(R.MediaNotFound)], globalUri);

                _gameActions.SendMessageWithArgs(Messages.Content, ContentPlacements.Screen, 0, ContentTypes.Text, errorText);
                _gameActions.SendMessageWithArgs(messageType, AtomTypes.Text, errorText);

                return false;
            }

            _gameActions.SendMessageWithArgs(
                Messages.Content,
                contentItem.Placement,
                0,
                contentItem.Type,
                globalUri);

            // TODO: remove after complete switching to Content message
            // {
            var msg = new MessageBuilder(messageType);

            var contentType = contentItem.Type;

            if (contentType == AtomTypes.AudioNew)
            {
                contentType = AtomTypes.Audio; // For backward compatibility; remove later
            }

            msg.Add(contentType).Add(MessageParams.Atom_Uri);

            foreach (var person in _data.AllPersons.Keys)
            {
                var msg2 = new StringBuilder(msg.ToString());

                if (_gameActions.Client.CurrentServer.Contains(person))
                {
                    msg2.Append(Message.ArgsSeparatorChar).Append(localUri ?? globalUri);
                }
                else
                {
                    msg2.Append(Message.ArgsSeparatorChar).Append(globalUri);
                }

                _gameActions.SendMessage(msg2.ToString(), person);
            }

            // }

            return true;
        }
        catch (Exception exc)
        {
            ClientData.Host.OnError(exc);
            return false;
        }
    }

    private void CheckFileLength(string contentType, long fileLength)
    {
        int? maxRecommendedFileLength = contentType == AtomTypes.Image ? _data.Host.MaxImageSizeKb
            : (contentType == AtomTypes.Audio || contentType == AtomTypes.AudioNew ? _data.Host.MaxAudioSizeKb
            : (contentType == AtomTypes.Video ? _data.Host.MaxVideoSizeKb : null));

        if (!maxRecommendedFileLength.HasValue || fileLength <= (long)maxRecommendedFileLength * 1024)
        {
            return;
        }

        // Notify users that the media file is too large and could be downloaded slowly
        var contentName = contentType == AtomTypes.Image ? LO.GetPackagesString(nameof(SIPackages.Properties.Resources.Image)) :
            (contentType == AtomTypes.Audio || contentType == AtomTypes.AudioNew ? LO.GetPackagesString(nameof(SIPackages.Properties.Resources.Audio)) :
            (contentType == AtomTypes.Video ? LO.GetPackagesString(nameof(SIPackages.Properties.Resources.Video)) : R.File));

        var fileLocation = $"{_data.Theme?.Name}, {_data.Question?.Price}";
        var errorMessage = string.Format(LO[nameof(R.OversizedFile)], contentName, fileLocation, maxRecommendedFileLength);

        _gameActions.SendMessageWithArgs(Messages.Replic, ReplicCodes.Special.ToString(), errorMessage);

        if (_data.OversizedMediaNotificationsCount < MaxMediaNotifications)
        {
            _data.OversizedMediaNotificationsCount++;

            // Show message on table
            _gameActions.SendMessageWithArgs(Messages.Atom_Hint, errorMessage);
        }
    }

    internal void OnContentScreenImage(ContentItem contentItem)
    {
        _data.IsPartial = false;
        ShareMedia(contentItem);

        var appSettings = _data.Settings.AppSettings;
        // TODO: provide this flag to client as part of the CONTENT message
        var partialImage = appSettings.PartialImages && !appSettings.FalseStart && _data.Question?.TypeName == QuestionTypes.Simple && !_data.IsAnswer;

        var renderTime = partialImage ? Math.Max(0, appSettings.TimeSettings.PartialImageTime * 10) : 0;
        
        var waitTime = GetContentItemDuration(
            contentItem,
            DefaultImageTime + appSettings.TimeSettings.TimeForMediaDelay * 10);

        var contentTime = renderTime + waitTime;

        _data.AtomTime = contentTime;
        _data.AtomStart = DateTime.UtcNow;

        ScheduleExecution(Tasks.MoveNext, contentTime);

        _data.TimeThinking = 0.0;
        _data.UseBackgroundAudio = !contentItem.WaitForFinish;
    }

    private static int GetContentItemDuration(ContentItem contentItem, int defaultValue) =>
        contentItem.WaitForFinish
            ? (contentItem.Duration > TimeSpan.Zero ? (int)(contentItem.Duration.TotalMilliseconds / 100) : defaultValue)
            : 1;

    internal void OnContentBackgroundAudio(ContentItem contentItem)
    {
        _data.IsPartial = false;
        _data.MediaOk = ShareMedia(contentItem, _data.UseBackgroundAudio);

        var defaultTime = DefaultImageTime + _data.Settings.AppSettings.TimeSettings.TimeForMediaDelay * 10;

        if (_data.MediaOk)
        {
            _data.InitialMediaContentCompletionCount = _data.HaveViewedAtom = _data.Viewers.Count
                + _data.Players.Where(pa => pa.IsHuman && pa.IsConnected).Count()
                + (_data.ShowMan.IsHuman && _data.ShowMan.IsConnected ? 1 : 0);

            defaultTime = DefaultAudioVideoTime;
        }

        var atomTime = GetContentItemDuration(contentItem, defaultTime);

        _data.AtomTime = atomTime;
        _data.AtomStart = DateTime.UtcNow;
        _data.IsPlayingMedia = true;
        _data.IsPlayingMediaPaused = false;

        ScheduleExecution(Tasks.MoveNext, atomTime);

        _data.TimeThinking = 0.0;
    }

    internal void OnContentScreenVideo(ContentItem contentItem)
    {
        _data.IsPartial = false;
        _data.MediaOk = ShareMedia(contentItem);

        var defaultTime = DefaultImageTime + _data.Settings.AppSettings.TimeSettings.TimeForMediaDelay * 10;

        if (_data.MediaOk)
        {
            _data.InitialMediaContentCompletionCount = _data.HaveViewedAtom = _data.Viewers.Count
                + _data.Players.Where(pa => pa.IsHuman && pa.IsConnected).Count()
                + (_data.ShowMan.IsHuman && _data.ShowMan.IsConnected ? 1 : 0);

            defaultTime = DefaultAudioVideoTime;
        }

        int atomTime = GetContentItemDuration(contentItem, defaultTime);

        _data.AtomTime = atomTime;
        _data.AtomStart = DateTime.UtcNow;
        _data.IsPlayingMedia = true;
        _data.IsPlayingMediaPaused = false;

        ScheduleExecution(Tasks.MoveNext, atomTime);

        _data.TimeThinking = 0.0;
    }

    internal void AskToPress()
    {
        if (_data.Settings.AppSettings.FalseStart)
        {
            foreach (var player in _data.Players)
            {
                player.CanPress = true;
            }
        }

        // Let's add a random offset so it will be difficult to press the button in advance (before the frame appears)
        ScheduleExecution(Tasks.AskToTry, 1 + (_data.Settings.AppSettings.Managed ? 0 : Random.Shared.Next(10)), force: true);
    }

    internal void AskDirectAnswer()
    {
        if (_data.Question?.TypeName == QuestionTypes.StakeAll)
        {
            _gameActions.SendMessageWithArgs(Messages.FinalThink, _data.Settings.AppSettings.TimeSettings.TimeForFinalThinking);
        }

        ScheduleExecution(Tasks.AskAnswer, 1, force: true);
    }

    private void Engine_NextQuestion()
    {
        _data.CanMarkQuestion = false;
        _data.AnswererIndex = -1;
        _data.QuestionPlayState.Clear();
        _data.StakerIndex = -1;

        ScheduleExecution(Tasks.MoveNext, 1, force: true);
    }

    private void FinishRound(bool move = true)
    {
        _data.IsQuestionPlaying = false;

        _gameActions.InformSums();
        _gameActions.AnnounceSums();
        _gameActions.SendMessage(Messages.Stop); // Timers STOP

        _data.IsThinking = false;

        _data.IsWaiting = false;
        _data.Decision = DecisionType.None;

        _data.IsRoundEnding = true;
        
        // This is quite ugly bit here but as we interrupt normal flow we need to cut continuation
        // (or we could replace it with a normal move)
        ClearContinuation();

        if (move)
        {
            PlanExecution(Tasks.MoveNext, 40);

            if (ClientData.TInfo.Pause)
            {
                OnPauseCore(false);
            }
        }
        else
        {
            // Round was finished manually. We need to cancel current waiting tasks in a safe way
            _taskRunner.ClearOldTasks();
        }
    }

    internal void OnPauseCore(bool isPauseEnabled)
    {
        // Game host or showman requested a game pause

        if (isPauseEnabled)
        {
            if (ClientData.TInfo.Pause)
            {
                return;
            }

            if (Stop(StopReason.Pause))
            {
                ClientData.TInfo.Pause = true;
                AddHistory("Pause activated");
            }

            return;
        }

        if (StopReason == StopReason.Pause)
        {
            // We are currently moving into pause mode. Resuming
            ClientData.TInfo.Pause = false;
            AddHistory("Immediate pause resume");
            CancelStop();
            return;
        }

        if (!ClientData.TInfo.Pause)
        {
            return;
        }

        ClientData.TInfo.Pause = false;

        var pauseDuration = DateTime.UtcNow.Subtract(ClientData.PauseStartTime);

        var times = new int[Constants.TimersCount];

        for (var i = 0; i < Constants.TimersCount; i++)
        {
            times[i] = (int)(ClientData.PauseStartTime.Subtract(ClientData.TimerStartTime[i]).TotalMilliseconds / 100);
            ClientData.TimerStartTime[i] = ClientData.TimerStartTime[i].Add(pauseDuration);
        }

        if (ClientData.IsPlayingMediaPaused)
        {
            ClientData.IsPlayingMediaPaused = false;
            ClientData.IsPlayingMedia = true;
        }

        if (ClientData.IsThinkingPaused)
        {
            ClientData.IsThinkingPaused = false;
            ClientData.IsThinking = true;
        }

        AddHistory($"Pause resumed ({Runner.PrintOldTasks()} {StopReason})");

        try
        {
            var maxPressingTime = ClientData.Settings.AppSettings.TimeSettings.TimeForThinkingOnQuestion * 10;
            times[1] = maxPressingTime - ResumeExecution();
        }
        catch (Exception exc)
        {
            throw new Exception($"Resume execution error: {PrintHistory()}", exc);
        }

        if (StopReason == StopReason.Decision)
        {
            ExecuteImmediate(); // Decision could be ready
        }

        _gameActions.SpecialReplic(LO[nameof(R.GameResumed)]);
        _gameActions.SendMessageWithArgs(Messages.Pause, isPauseEnabled ? '+' : '-', times[0], times[1], times[2]);
    }

    private void Engine_RoundEmpty()
    {
        _gameActions.ShowmanReplic(GetRandomString(LO[nameof(R.AllQuestions)]));
        FinishRound();
    }

    private void Engine_RoundTimeout()
    {
        _gameActions.SendMessage(Messages.Timeout);
        _gameActions.ShowmanReplic(GetRandomString(LO[nameof(R.AllTime)]));
        FinishRound();
    }

    internal void OnThemeDeleted(int themeIndex)
    {
        if (themeIndex < 0 || themeIndex >= _data.TInfo.RoundInfo.Count)
        {
            var errorMessage = new StringBuilder(themeIndex.ToString())
                .Append(' ')
                .Append(string.Join("|", _data.TInfo.RoundInfo.Select(t => $"({t.Name != QuestionHelper.InvalidThemeName} {t.Questions.Count})")))
                .Append(' ')
                .Append(_data.ThemeIndexToDelete);

            throw new ArgumentException(errorMessage.ToString(), nameof(themeIndex));
        }

        if (_data.ThemeDeleters == null || _data.ThemeDeleters.IsEmpty())
        {
            throw new InvalidOperationException("_data.ThemeDeleters are undefined");
        }

        _data.TInfo.RoundInfo[themeIndex].Name = QuestionHelper.InvalidThemeName;

        _gameActions.SendMessageWithArgs(Messages.Out, themeIndex);

        var playerIndex = _data.ThemeDeleters.Current.PlayerIndex;
        var themeName = _data.TInfo.RoundInfo[themeIndex].Name;

        _gameActions.PlayerReplic(playerIndex, themeName);
        ScheduleExecution(Tasks.MoveNext, 10);
    }

    internal void AnnounceFinalTheme()
    {
        _gameActions.ShowmanReplic($"{GetRandomString(LO[nameof(R.PlayTheme)])} {_data.Theme.Name}");
        _gameActions.SendMessageWithArgs(Messages.QuestionCaption, _data.Theme.Name);
        _gameActions.SendMessageWithArgs(Messages.Theme, _data.Theme.Name, 1);

        ProceedToThemeAndQuestion();
    }

    private void Engine_EndGame()
    {
        // Clearing the table
        _gameActions.SendMessage(Messages.Stop);
        _gameActions.SystemReplic($"{LO[nameof(R.GameResults)]}: ");

        for (var i = 0; i < _data.Players.Count; i++)
        {
            _gameActions.SystemReplic($"{_data.Players[i].Name}: {Notion.FormatNumber(_data.Players[i].Sum)}");
        }

        FillReport();
        ScheduleExecution(Tasks.Winner, 15 + Random.Shared.Next(10));
    }

    public void Dispose() =>
        ClientData.TaskLock.WithLock(
            () =>
            {
                _taskRunner.Dispose();

                if (_data.Stage != GameStage.Before)
                {
                    SendReport();
                }

                Engine.Dispose();
            },
            5000);

    private void SendReport()
    {
        if (_data.ReportSent)
        {
            return;
        }

        FillReport();

        var reviewers = _data.GameResultInfo.Reviews.Keys;

        foreach (var reviewer in reviewers)
        {
            if (string.IsNullOrEmpty(_data.GameResultInfo.Reviews[reviewer]))
            {
                _data.GameResultInfo.Reviews.Remove(reviewer);
            }
        }

        _data.Host.SaveReport(_data.GameResultInfo);
        _data.ReportSent = true;
    }

    private void FillReport()
    {
        if (_data.GameResultInfo.Duration > TimeSpan.Zero)
        {
            return;
        }

        for (var i = 0; i < _data.Players.Count; i++)
        {
            _data.GameResultInfo.Results[_data.Players[i].Name] = _data.Players[i].Sum;
        }

        _data.GameResultInfo.Duration = DateTimeOffset.UtcNow.Subtract(_data.GameResultInfo.StartTime);
    }

    internal bool Stop(StopReason reason)
    {
        if (_stopReason != StopReason.None)
        {
            _tasksHistory.AddLogEntry($"Stop skipped. Current reason: {_stopReason}, new reason: {reason}");
            return false;
        }

        if (reason == StopReason.Decision)
        {
            ClientData.IsWaiting = false; // Preventing double message processing
        }
        else if (reason == StopReason.Appellation && ClientData.IsWaiting)
        {
            StopWaiting();
        }

        _stopReason = reason;
        ExecuteImmediate();

        return true;
    }

    internal void ExecuteImmediate()
    {
        _tasksHistory.AddLogEntry(nameof(ExecuteImmediate));
        _taskRunner.ExecuteImmediate();
    }

    internal void CancelStop() => _stopReason = StopReason.None;

    /// <summary>
    /// Processes decision been made.
    /// </summary>
    private bool OnDecision() => _data.Decision switch
    {
        DecisionType.StarterChoosing => OnDecisionStarterChoosing(),
        DecisionType.QuestionSelection => OnQuestionSelection(),
        DecisionType.Answering => OnDecisionAnswering(),
        DecisionType.AnswerValidating => OnDecisionAnswerValidating(),
        DecisionType.QuestionAnswererSelection => QuestionAnswererSelection(),
        DecisionType.QuestionPriceSelection => OnQuestionPriceSelection(),
        DecisionType.NextPersonStakeMaking => OnDecisionNextPersonStakeMaking(),
        DecisionType.StakeMaking => OnDecisionStakeMaking(),
        DecisionType.NextPersonFinalThemeDeleting => OnNextPersonFinalThemeDeleting(),
        DecisionType.FinalThemeDeleting => OnDecisionFinalThemeDeleting(),
        DecisionType.FinalStakeMaking => OnFinalStakeMaking(),
        DecisionType.AppellationDecision => OnAppellationDecision(),
        _ => false,
    };

    private bool OnQuestionSelection()
    {
        if (_data.ThemeIndex == -1
            || _data.ThemeIndex >= ClientData.TInfo.RoundInfo.Count
            || _data.QuestionIndex == -1
            || _data.QuestionIndex >= ClientData.TInfo.RoundInfo[_data.ThemeIndex].Questions.Count
            || !ClientData.TInfo.RoundInfo[_data.ThemeIndex].Questions[_data.QuestionIndex].IsActive())
        {
            return false;
        }

        StopWaiting();
        ScheduleExecution(Tasks.MoveNext, 1);
        return true;
    }

    private bool QuestionAnswererSelection()
    {
        if (_data.Answerer == null)
        {
            return false;
        }

        StopWaiting();

        var s = _data.ChooserIndex == _data.AnswererIndex ? LO[nameof(R.ToMyself)] : _data.Answerer.Name;
        _gameActions.PlayerReplic(_data.ChooserIndex, s);

        _data.ChooserIndex = _data.AnswererIndex;
        _gameActions.SendMessageWithArgs(Messages.SetChooser, ClientData.ChooserIndex, "+");
        ScheduleExecution(Tasks.MoveNext, 10);
        return true;
    }

    private bool OnQuestionPriceSelection()
    {
        if (_data.CurPriceRight == -1)
        {
            return false;
        }

        StopWaiting();

        _data.CurPriceWrong = _data.CurPriceRight;
        _gameActions.PlayerReplic(_data.AnswererIndex, _data.CurPriceRight.ToString());

        _gameActions.SendMessageWithArgs(Messages.PersonStake, _data.AnswererIndex, 1, _data.CurPriceRight);
        _gameActions.SendMessageWithArgs(Messages.SetChooser, ClientData.ChooserIndex, "+");

        ScheduleExecution(Tasks.MoveNext, 20);

        return true;
    }

    private bool OnFinalStakeMaking()
    {
        if (_data.NumOfStakers != 0)
        {
            return false;
        }

        StopWaiting();
        ProceedToFinalQuestion();

        return true;
    }

    private void ProceedToFinalQuestion()
    {
        _gameActions.ShowmanReplic(LO[nameof(R.ThankYou)]);
        ScheduleExecution(Tasks.MoveNext, 20);
    }

    private bool OnNextPersonFinalThemeDeleting()
    {
        if (_data.ThemeDeleters == null || _data.ThemeDeleters.Current.PlayerIndex == -1)
        {
            return false;
        }

        StopWaiting();
        _gameActions.ShowmanReplic($"{LO[nameof(R.ThemeDeletes)]} {_data.Players[_data.ThemeDeleters.Current.PlayerIndex].Name}");
        _data.ThemeDeleters.MoveBack();
        ScheduleExecution(Tasks.AskToDelete, 1);
        return true;
    }

    private bool OnAppellationDecision()
    {
        StopWaiting();
        ScheduleExecution(Tasks.CheckAppellation, 10);
        return true;
    }

    private bool OnDecisionFinalThemeDeleting()
    {
        if (_data.ThemeIndexToDelete == -1)
        {
            return false;
        }

        StopWaiting();
        ScheduleExecution(Tasks.MoveNext, 1);
        return true;
    }

    private bool OnDecisionStakeMaking()
    {
        if (!_data.StakeType.HasValue)
        {
            return false;
        }

        StopWaiting();

        if (_data.OrderIndex == -1)
        {
            throw new ArgumentException($"{nameof(_data.OrderIndex)} == -1! {_data.OrderHistory}", nameof(_data.OrderIndex));
        }

        var playerIndex = _data.Order[_data.OrderIndex];

        if (playerIndex < 0 || playerIndex >= _data.Players.Count)
        {
            throw new ArgumentException($"{nameof(playerIndex)} == ${playerIndex} but it must be in [0; ${_data.Players.Count - 1}]! ${_data.OrderHistory}", nameof(playerIndex));
        }

        var stakeMaking = string.Join(",", _data.Players.Select(p => p.StakeMaking));
        var stakeSum = _data.StakeType == StakeMode.Sum ? _data.StakeSum.ToString() : "";
        _data.OrderHistory.Append($"Stake received: {playerIndex} {_data.StakeType.Value} {stakeSum} {stakeMaking}").AppendLine();

        if (_data.StakeType == StakeMode.Nominal)
        {
            _gameActions.PlayerReplic(playerIndex, LO[nameof(R.Nominal)]);
            _data.Stake = _data.CurPriceRight;
            _data.StakerIndex = playerIndex;
        }
        else if (_data.StakeType == StakeMode.Sum)
        {
            _data.Stake = _data.StakeSum;
            _data.StakerIndex = playerIndex;
        }
        else if (_data.StakeType == StakeMode.Pass)
        {
            _gameActions.PlayerReplic(playerIndex, LO[nameof(R.Pass)]);
            _data.Players[playerIndex].StakeMaking = false;
        }
        else
        {
            _gameActions.PlayerReplic(playerIndex, LO[nameof(R.VaBank)]);
            _data.Stake = _data.Players[playerIndex].Sum;
            _data.StakerIndex = playerIndex;
            _data.AllIn = true;
        }

        var printedStakeType = _data.StakeType == StakeMode.Nominal ? StakeMode.Sum : _data.StakeType;

        var stakeMessage = new MessageBuilder(Messages.PersonStake, playerIndex, (int)printedStakeType);

        if (printedStakeType == StakeMode.Sum)
        {
            stakeMessage.Add(_data.Stake);
        }

        _gameActions.SendMessage(stakeMessage.Build());

        if (_data.StakeType != StakeMode.Pass)
        {
            for (var i = 0; i < _data.Players.Count; i++)
            {
                var player = _data.Players[i];

                if (i != _data.StakerIndex && player.StakeMaking && player.Sum <= _data.Stake)
                {
                    player.StakeMaking = false;
                    _gameActions.SendMessageWithArgs(Messages.PersonStake, i, 2);
                }
            }
        }

        var stakeMaking2 = string.Join(",", _data.Players.Select(p => p.StakeMaking));
        _data.OrderHistory.Append($"Stake making updated: {stakeMaking2}").AppendLine();

        var stakersCount = _data.Players.Count(p => p.StakeMaking);

        if (stakersCount == 1) // Answerer is defined
        {
            for (var i = 0; i < _data.Players.Count; i++)
            {
                if (_data.Players[i].StakeMaking)
                {
                    _data.StakerIndex = i;
                }
            }

            ScheduleExecution(Tasks.PrintAuctPlayer, 25);
            return true;
        }
        else if (stakersCount == 0)
        {
            _tasksHistory.AddLogEntry("Skipping question");
            Engine.SkipQuestion();
            ScheduleExecution(Tasks.MoveNext, 10);

            return true;
        }

        ScheduleExecution(Tasks.AskStake, 5);
        return true;
    }

    private bool OnDecisionAnswerValidating()
    {
        if (!_data.ShowmanDecision)
        {
            return false;
        }

        if (_data.Answerer == null)
        {
            throw new Exception("_data.Answerer == null");
        }

        StopWaiting();

        int updateSum;

        if (_data.Answerer.AnswerIsRight)
        {
            var showmanReplic = IsSpecialQuestion() ? nameof(R.Bravo) : nameof(R.Right);
            
            var s = new StringBuilder(GetRandomString(LO[showmanReplic]));

            var canonicalAnswer = _data.Question?.Right.FirstOrDefault();
            var isAnswerCanonical = canonicalAnswer != null && (_data.Answerer.Answer ?? "").Simplify().Contains(canonicalAnswer.Simplify());

            if (!HaveMultipleAnswerers())
            {
                if (canonicalAnswer != null && !isAnswerCanonical)
                {
                    s.AppendFormat(" [{0}]", canonicalAnswer);

                    _data.GameResultInfo.AcceptedAnswers.Add(new QuestionReport
                    {
                        ThemeName = _data.Theme.Name,
                        QuestionText = _data.Question?.GetText(),
                        ReportText = _data.Answerer.Answer
                    });
                }

                s.AppendFormat(
                    " (+{0}{1})",
                    _data.CurPriceRight.ToString().FormatNumber(),
                    Math.Abs(_data.Answerer.AnswerIsRightFactor - 1.0) < double.Epsilon ? "" : " * " + _data.Answerer.AnswerIsRightFactor);

                _gameActions.ShowmanReplic(s.ToString());

                s = new StringBuilder(Messages.Person)
                    .Append(Message.ArgsSeparatorChar)
                    .Append('+')
                    .Append(Message.ArgsSeparatorChar)
                    .Append(_data.AnswererIndex)
                    .Append(Message.ArgsSeparatorChar)
                    .Append(_data.CurPriceRight);

                _gameActions.SendMessage(s.ToString());

                updateSum = (int)(_data.CurPriceRight * _data.Answerer.AnswerIsRightFactor);
                _data.Answerer.AddRightSum(updateSum);
                _data.ChooserIndex = _data.AnswererIndex;
                _gameActions.SendMessageWithArgs(Messages.SetChooser, ClientData.ChooserIndex);
                _gameActions.InformSums();

                _data.IsQuestionPlaying = false;
                _data.AnnounceAnswer = false;

                _data.IsThinking = false;
                _gameActions.SendMessageWithArgs(Messages.Timer, 1, MessageParams.Timer_Stop);

                MoveToAnswer(); // Question is answered correctly
                ScheduleExecution(Tasks.MoveNext, 1, force: true);
            }
            else
            {
                _gameActions.ShowmanReplic(s.ToString());

                if (isAnswerCanonical)
                {
                    _data.AnnounceAnswer = false;
                }

                _data.PlayerIsRight = true;
                updateSum = _data.Answerer.FinalStake;
                ScheduleExecution(Tasks.AnnounceStake, 15);
            }
        }
        else
        {
            var s = new StringBuilder();

            if (_data.Answerer.Answer != LO[nameof(R.IDontKnow)])
            {
                s.Append(GetRandomString(LO[nameof(R.Wrong)]));
            }

            if (_data.Settings.AppSettings.IgnoreWrong)
            {
                _data.CurPriceWrong = 0;
            }

            if (_data.QuestionPlayState.AnswerOptions != null && _data.Answerer.Answer != null)
            {
                var answerIndex = Array.FindIndex(_data.QuestionPlayState.AnswerOptions, o => o.Label == _data.Answerer.Answer);

                if (answerIndex > -1)
                {
                    _gameActions.SendMessageWithArgs(Messages.ContentState, ContentPlacements.Screen, answerIndex + 1, ItemState.Wrong);
                }

                if (!HaveMultipleAnswerers())
                {
                    _data.QuestionPlayState.UsedAnswerOptions.Add(_data.Answerer.Answer);
                }
            }

            if (!HaveMultipleAnswerers())
            {
                s.AppendFormat(
                    " (-{0}{1})",
                    _data.CurPriceWrong.ToString().FormatNumber(),
                    Math.Abs(_data.Answerer.AnswerIsRightFactor - 1.0) < double.Epsilon ? "" : " * " + _data.Answerer.AnswerIsRightFactor);
                
                _gameActions.ShowmanReplic(s.ToString());

                if (_data.Answerer.AnswerIsRightFactor == 0)
                {
                    _gameActions.SendMessageWithArgs(Messages.Pass, _data.AnswererIndex);
                    updateSum = -1;
                }
                else
                {
                    s = new StringBuilder(Messages.Person)
                        .Append(Message.ArgsSeparatorChar)
                        .Append('-')
                        .Append(Message.ArgsSeparatorChar)
                        .Append(_data.AnswererIndex)
                        .Append(Message.ArgsSeparatorChar)
                        .Append(_data.CurPriceWrong);

                    _gameActions.SendMessage(s.ToString());

                    updateSum = (int)(_data.CurPriceWrong * _data.Answerer.AnswerIsRightFactor);
                    _data.Answerer.SubtractWrongSum(updateSum);
                    _gameActions.InformSums();

                    if (_data.Answerer.IsHuman)
                    {
                        _data.GameResultInfo.RejectedAnswers.Add(new QuestionReport
                        {
                            ThemeName = _data.Theme.Name,
                            QuestionText = _data.Question?.GetText(),
                            ReportText = _data.Answerer.Answer
                        });
                    }
                }

                _data.Answerer.CanPress = false;
                ScheduleExecution(Tasks.ContinueQuestion, 1);
            }
            else
            {
                _gameActions.ShowmanReplic(s.ToString());
                _data.PlayerIsRight = false;
                updateSum = _data.Answerer.FinalStake;

                ScheduleExecution(Tasks.AnnounceStake, 15);
            }
        }

        if (updateSum >= 0)
        {
            var answerResult = new AnswerResult(_data.AnswererIndex, _data.Answerer.AnswerIsRight, updateSum);
            _data.QuestionHistory.Add(answerResult);
        }

        return true;
    }

    /// <summary>
    /// Skips left question part and moves directly to answer.
    /// </summary>
    internal void MoveToAnswer()
    {
        if (ClientData.IsQuestionFinished)
        {
            return;
        }

        if (_data.QuestionPlayState.AnswerOptions != null)
        {
            _continuation = null; // Erase AskAnswer continuation (TODO: very difficult to track everywhere - can this be simplified?)
        }

        Engine.MoveToAnswer();
    }

    /// <summary>
    /// Tries to continue question play.
    /// </summary>
    public void ContinueQuestion()
    {
        if (IsSpecialQuestion())
        {
            // No need to move to answer as special questions could be different
            // TODO: in the future there could be situations when special questions could be unfinished here
            ScheduleExecution(Tasks.WaitTry, 20);
            return;
        }

        var canAnybodyPress = _data.Players.Any(player => player.CanPress && player.IsConnected);

        if (!canAnybodyPress)
        {
            MoveToAnswer();
            ScheduleExecution(Tasks.WaitTry, 20, force: true);
            return;
        }

        if (_data.QuestionPlayState.AnswerOptions != null)
        {
            var oneOptionLeft = _data.QuestionPlayState.UsedAnswerOptions.Count + 1 == _data.QuestionPlayState.AnswerOptions.Length;

            if (oneOptionLeft)
            {
                MoveToAnswer();
                ScheduleExecution(Tasks.WaitTry, 20, force: true);
                return;
            }
        }

        _data.PendingAnswererIndex = -1;
        _data.AnswererPressDuration = -1;
        _data.PendingAnswererIndicies.Clear();

        if (!ClientData.Settings.AppSettings.FalseStart)
        {
            _gameActions.SendMessageWithArgs(Messages.Try, MessageParams.Try_NotFinished);
        }

        if (ClientData.Settings.AppSettings.FalseStart || ClientData.IsQuestionFinished)
        {
            if (!ClientData.Settings.AppSettings.FalseStart)
            {
                _gameActions.SendMessage(Messages.Resume); // To resume the media
            }

            ScheduleExecution(Tasks.AskToTry, 10, force: true);
            return;
        }

        // Resume question playing
        if (_data.IsPartial)
        {
            _data.InitialPartialTextLength = _data.TextLength;
            _data.PartialIterationCounter = 0;
            ScheduleExecution(Tasks.PrintPartial, 5, force: true);
        }
        else
        {
            _data.IsPlayingMedia = _data.IsPlayingMediaPaused;
            _gameActions.SendMessage(Messages.Resume);

            var waitTime = _data.IsPlayingMedia && _data.HaveViewedAtom < _data.InitialMediaContentCompletionCount
                ? 30 + ClientData.Settings.AppSettings.TimeSettings.TimeForMediaDelay * 10
                : _data.AtomTime;

            ScheduleExecution(Tasks.MoveNext, waitTime, force: true);
        }

        SendTryToPlayers();

        _data.Decision = DecisionType.Pressing;
    }

    private bool IsSpecialQuestion() => _data.Question?.TypeName != QuestionTypes.Simple;

    private bool OnDecisionNextPersonStakeMaking()
    {
        var playerIndex = _data.Order[_data.OrderIndex];
        if (playerIndex == -1)
        {
            return false;
        }

        if (playerIndex >= _data.Players.Count)
        {
            throw new ArgumentException($"{nameof(playerIndex)} {playerIndex} must be in [0;{_data.Players.Count - 1}]");
        }

        StopWaiting();

        var s = $"{LO[nameof(R.StakeMakes)]} {_data.Players[playerIndex].Name}";
        _gameActions.ShowmanReplic(s);

        _data.OrderIndex--;
        ScheduleExecution(Tasks.AskStake, 10);
        return true;
    }

    private bool OnDecisionStarterChoosing()
    {
        if (_data.ChooserIndex == -1)
        {
            return false;
        }

        StopWaiting();

        var msg = string.Format(GetRandomString(LO[nameof(R.InformChooser)]), _data.Chooser.Name);
        _gameActions.ShowmanReplic(msg);
        _gameActions.SendMessageWithArgs(Messages.SetChooser, ClientData.ChooserIndex);
        
        ScheduleExecution(Tasks.MoveNext, 20);

        return true;
    }

    private void RunRoundTimer()
    {
        _data.TimerStartTime[0] = DateTime.UtcNow;
        _gameActions.SendMessageWithArgs(Messages.Timer, 0, MessageParams.Timer_Go, _data.Settings.AppSettings.TimeSettings.TimeOfRound * 10);
    }

    private bool OnDecisionAnswering()
    {
        if (!HaveMultipleAnswerers())
        {
            if (_data.Answerer == null || string.IsNullOrEmpty(_data.Answerer.Answer))
            {
                return false;
            }

            StopWaiting();

            if (_data.QuestionPlayState.AnswerOptions != null)
            {
                var answerIndex = Array.FindIndex(_data.QuestionPlayState.AnswerOptions, o => o.Label == _data.Answerer.Answer);

                if (answerIndex > -1)
                {
                    _gameActions.SendMessageWithArgs(Messages.ContentState, ContentPlacements.Screen, answerIndex + 1, ItemState.Active);
                }

                ScheduleExecution(Tasks.AskRight, 15, force: true);
                return true;
            }
            else
            {
                _gameActions.PlayerReplic(_data.AnswererIndex, _data.Answerer.Answer);
            }

            if (_data.IsOralNow)
            {
                AskRight();
            }
            else
            {
                ScheduleExecution(Tasks.AskRight, 15, force: true);
            }

            return true;
        }

        StopWaiting();

        var s = GetRandomString(LO[nameof(R.LetsSee)]);
        _gameActions.ShowmanReplic(s);

        var answererIndicies = _data.QuestionPlayState.AnswererIndicies.OrderBy(index => _data.Players[index].Sum);
        _data.AnnouncedAnswerersEnumerator = new CustomEnumerator<int>(answererIndicies);
        ScheduleExecution(Tasks.Announce, 15);
        return true;
    }

    public bool HaveMultipleAnswerers() => _data.QuestionPlayState.AreMultipleAnswerers;

    public void StopWaiting()
    {
        _data.IsWaiting = false;
        _data.Decision = DecisionType.None;

        _gameActions.SendMessageWithArgs(Messages.Timer, 2, MessageParams.Timer_Stop);
    }

    internal string PrintHistory() => _tasksHistory.ToString();

    internal void PlanExecution(Tasks task, double taskTime, int arg = 0)
    {
        _tasksHistory.AddLogEntry($"PlanExecution {task} {taskTime} {arg} ({ClientData.TInfo.Pause})");

        if (_taskRunner.IsExecutionPaused)
        {
            _taskRunner.UpdatePausedTask(task, arg, (int)taskTime);
        }
        else
        {
            _taskRunner.ScheduleExecution(task, taskTime, arg, ShouldRunTimer());
        }
    }

    internal void ScheduleExecution(Tasks task, double taskTime, int arg = 0, bool force = false)
    {
        if (_data.IsDeferringAnswer)
        {
            // AskAnswerDeferred task cannot be avoided
            _tasksHistory.AddLogEntry($"AskAnswerDeferred task blocks scheduling ({_taskRunner.CurrentTask}): {task} {arg} {taskTime / 10}");
            return;
        }

        _tasksHistory.AddLogEntry($"Scheduled ({_taskRunner.CurrentTask}): {task} {arg} {taskTime / 10}");
        _taskRunner.ScheduleExecution(task, taskTime, arg, force || ShouldRunTimer());
    }

    private bool ShouldRunTimer() => !_data.Settings.AppSettings.Managed || _data.HostName == null || !_data.AllPersons.ContainsKey(_data.HostName);

    /// <summary>
    /// Executes current task of the game state machine.
    /// </summary>
    public void ExecuteTask(Tasks task, int arg)
    {
        try
        {
            ClientData.TaskLock.WithLock(() =>
            {
                if (Engine == null) // disposed
                {
                    return;
                }

                if (_stopReason != StopReason.None)
                {
                    var (stop, newTask) = ProcessStopReason(task, arg);

                    if (stop)
                    {
                        _tasksHistory.AddLogEntry("Execution stopped");
                        return;
                    }

                    _tasksHistory.AddLogEntry($"Execution proceed with task {newTask}");
                    task = newTask;
                }

                _tasksHistory.AddLogEntry($"{task}:{arg}");

                // Special catch for hanging old tasks
                if (task == Tasks.AskToChoose && _taskRunner.OldTasks.Any())
                {
                    static string oldTaskPrinter(Tuple<Tasks, int, int> t) => $"{t.Item1}:{t.Item2}";

                    ClientData.Host.SendError(
                        new Exception(
                            $"Hanging old tasks: {string.Join(", ", _taskRunner.OldTasks.Select(oldTaskPrinter))};" +
                            $" Task: {task}, param: {arg}, history: {_tasksHistory}"),
                        true);

                    _taskRunner.ClearOldTasks();
                }

                switch (task)
                {
                    case Tasks.MoveNext:
                        MoveNext();
                        break;

                    case Tasks.StartGame:
                        StartGame(arg);
                        break;

                    case Tasks.Package:
                        OnPackage(_data.Package, arg);
                        break;

                    case Tasks.Round:
                        OnRound(_data.Round, arg);
                        break;

                    case Tasks.AskFirst:
                        GiveMoveToPlayerWithMinimumScore();
                        break;

                    case Tasks.WaitFirst:
                        WaitFirst();
                        break;

                    case Tasks.AskToChoose:
                        AskToChoose();
                        break;

                    case Tasks.WaitChoose:
                        WaitChoose();
                        break;

                    case Tasks.Theme:
                    case Tasks.ThemeInfo:
                        OnTheme(_data.Theme, arg, task == Tasks.Theme);
                        break;

                    case Tasks.QuestionType:
                        OnQuestionType(arg);
                        break;

                    case Tasks.AskStake:
                        AskStake(true);
                        break;

                    case Tasks.WaitNext:
                        WaitNext(arg == 0);
                        break;

                    case Tasks.WaitStake:
                        WaitStake();
                        break;

                    case Tasks.PrintAuctPlayer:
                        PrintStakeQuestionPlayer();
                        break;

                    case Tasks.AskToSelectQuestionAnswerer:
                        AskToSelectQuestionAnswerer();
                        break;

                    case Tasks.WaitQuestionAnswererSelection:
                        WaitQuestionAnswererSelection();
                        break;

                    case Tasks.AskToSelectQuestionPrice:
                        AskToSelectQuestionPrice();
                        break;

                    case Tasks.WaitSelectQuestionPrice:
                        WaitSelectQuestionPrice();
                        break;

                    case Tasks.PrintPartial:
                        PrintPartial();
                        break;

                    case Tasks.ShowNextAnswerOption:
                        ShowNextAnswerOption(arg);
                        break;

                    case Tasks.AskToTry:
                        AskToTry();
                        break;

                    case Tasks.WaitTry:
                        WaitTry();
                        break;

                    case Tasks.AskAnswer:
                        AskAnswer();
                        break;

                    case Tasks.AskAnswerDeferred:
                        AskAnswerDeferred();
                        break;

                    case Tasks.WaitAnswer:
                        WaitAnswer();
                        break;

                    case Tasks.AskRight:
                        AskRight();
                        break;

                    case Tasks.WaitRight:
                        WaitRight();
                        break;

                    case Tasks.ContinueQuestion:
                        ContinueQuestion();
                        break;

                    case Tasks.QuestSourComm:
                        QuestionSourcesAndComments(arg);
                        break;

                    case Tasks.PrintAppellation:
                        PrintAppellation();
                        break;

                    case Tasks.WaitAppellationDecision:
                        WaitAppellationDecision();
                        break;

                    case Tasks.CheckAppellation:
                        CheckAppellation();
                        break;

                    case Tasks.AskToDelete:
                        AskToDelete();
                        break;

                    case Tasks.WaitDelete:
                        WaitDelete();
                        break;

                    case Tasks.WaitFinalStake:
                        WaitFinalStake();
                        break;

                    case Tasks.Announce:
                        Announce();
                        break;

                    case Tasks.AnnounceStake:
                        AnnounceStake();
                        break;

                    case Tasks.AnnouncePostStake:
                        AnnouncePostStake();
                        break;

                    case Tasks.WaitReport:
                        WaitReport();
                        break;

                    case Tasks.Winner:
                        Winner();
                        break;

                    case Tasks.GoodLuck:
                        GoodLuck();
                        break;

                    case Tasks.AutoGame:
                        AutoGame?.Invoke();
                        break;
                }
            },
            5000);
        }
        catch (Exception exc)
        {
            _data.Host.SendError(new Exception($"Task: {task}, param: {arg}, history: {_tasksHistory}", exc));
            ScheduleExecution(Tasks.NoTask, 10);
            ClientData.MoveNextBlocked = true;
            _gameActions.SpecialReplic("Game ERROR");
        }
    }

    private void AskAnswerDeferred()
    {
        _data.Decision = DecisionType.None;
        _data.IsDeferringAnswer = false;

        if (!PrepareForAskAnswer())
        {
            ScheduleExecution(Tasks.ContinueQuestion, 1);
            return;
        }

        ScheduleExecution(Tasks.AskAnswer, 1, force: true);
    }

    private (bool, Tasks) ProcessStopReason(Tasks task, int arg)
    {
        var stop = true;
        var newTask = task;

        var stopReasonDetails = _stopReason == StopReason.Move
            ? _data.MoveDirection.ToString()
            : (_stopReason == StopReason.Decision ? ClientData.Decision.ToString() : "");

        _tasksHistory.AddLogEntry($"StopReason {_stopReason} {stopReasonDetails}");

        // Interrupt standard execution and try to do something urgent
        switch (_stopReason)
        {
            case StopReason.Pause:
                _tasksHistory.AddLogEntry($"Pause PauseExecution {task} {arg} {_taskRunner.PrintOldTasks()}");
                _taskRunner.PauseExecution(task, arg);

                ClientData.PauseStartTime = DateTime.UtcNow;

                if (ClientData.IsPlayingMedia)
                {
                    ClientData.IsPlayingMediaPaused = true;
                    ClientData.IsPlayingMedia = false;
                }

                if (ClientData.IsThinking)
                {
                    var startTime = ClientData.TimerStartTime[1];

                    ClientData.TimeThinking += ClientData.PauseStartTime.Subtract(startTime).TotalMilliseconds / 100;
                    ClientData.IsThinkingPaused = true;
                    ClientData.IsThinking = false;
                }

                var times = new int[Constants.TimersCount];

                for (var i = 0; i < Constants.TimersCount; i++)
                {
                    times[i] = (int)(ClientData.PauseStartTime.Subtract(ClientData.TimerStartTime[i]).TotalMilliseconds / 100);
                }

                _gameActions.SpecialReplic(LO[nameof(R.PauseInGame)]);
                _gameActions.SendMessageWithArgs(Messages.Pause, '+', times[0], times[1], times[2]);
                break;

            case StopReason.Decision:
                stop = OnDecision();
                break;

            case StopReason.Answer:
                stop = PrepareForAskAnswer();

                if (stop)
                {
                    ScheduleExecution(Tasks.AskAnswer, 1, force: true);
                }
                break;

            case StopReason.Appellation:
                var savedTask = task == Tasks.WaitChoose ? Tasks.AskToChoose : task;

                _tasksHistory.AddLogEntry($"Appellation PauseExecution {savedTask} {arg} ({_taskRunner.PrintOldTasks()})");

                _taskRunner.PauseExecution(savedTask, arg);
                ScheduleExecution(Tasks.PrintAppellation, 10);
                break;

            case StopReason.Move:
                switch (_data.MoveDirection)
                {
                    case MoveDirections.RoundBack:
                        if (Engine.CanMoveBackRound)
                        {
                            stop = Engine.MoveBackRound();

                            if (stop)
                            {
                                FinishRound(false);
                                _gameActions.SpecialReplic(LO[nameof(R.ShowmanSwitchedToPreviousRound)]);
                            }
                            else
                            {
                                _stopReason = StopReason.None;
                                return (true, task);
                            }
                        }
                        else
                        {
                            stop = false;
                        }

                        break;

                    case MoveDirections.Back:
                        if (Engine.CanMoveBack)
                        {
                            Engine.MoveBack();
                        }
                        else
                        {
                            stop = false;
                        }
                        break;

                    case MoveDirections.Next:
                        // Just perform the current task, no additional processing is required
                        stop = false;

                        if (task == Tasks.PrintPartial) // Skip partial printing
                        {
                            var subText = _data.Text[_data.TextLength..];

                            _gameActions.SendMessageWithArgs(Messages.ContentAppend, ContentPlacements.Screen, 0, ContentTypes.Text, subText.EscapeNewLines());
                            _gameActions.SendMessageWithArgs(Messages.Atom, Constants.PartialText, subText); // deprecated
                            _gameActions.SystemReplic(subText);

                            newTask = Tasks.MoveNext;
                        }

                        break;

                    case MoveDirections.RoundNext:
                        if (Engine.CanMoveNextRound)
                        {
                            stop = Engine.MoveNextRound();
                            
                            if (stop)
                            {
                                FinishRound(false);
                                _gameActions.SpecialReplic(LO[nameof(R.ShowmanSwitchedToNextRound)]);
                            }
                            else
                            {
                                _stopReason = StopReason.None;
                                return (true, task);
                            }
                        }
                        else
                        {
                            stop = false;
                        }
                        break;

                    case MoveDirections.Round:
                        if (Engine.CanMoveNextRound || Engine.CanMoveBackRound)
                        {
                            stop = Engine.MoveToRound(ClientData.TargetRoundIndex);

                            if (stop)
                            {
                                FinishRound(false);
                                _gameActions.SpecialReplic(LO[nameof(R.ShowmanSwitchedToOtherRound)]);
                            }
                            else
                            {
                                _stopReason = StopReason.None;
                                return (true, task);
                            }
                        }
                        else
                        {
                            stop = false;
                        }
                        break;

                    default:
                        stop = false;
                        break;
                }

                if (stop)
                {
                    ScheduleExecution(Tasks.MoveNext, _data.MoveDirection == MoveDirections.Next ? 10 : 30);
                }

                break;

            case StopReason.Wait:
                // TODO: if someone overrides Task after that (skipping AskAnswerDeferred execution), nobody could press the button during this question
                // That's very fragile logic. Think about alternatives
                // The order of calls is important here!
                ScheduleExecution(Tasks.AskAnswerDeferred, _data.WaitInterval, force: true);
                _data.IsDeferringAnswer = true;
                break;
        }

        _stopReason = StopReason.None;

        return (stop, newTask);
    }

    private void GoodLuck()
    {
        _gameActions.ShowmanReplic(LO[nameof(R.GoodLuck)]);

        _data.Stage = GameStage.After;
        OnStageChanged(GameStages.Finished, LO[nameof(R.StageFinished)]);
        _gameActions.InformStage();

        SendStatistics();
        AskForPlayerReviews();
    }

    private void SendStatistics()
    {
        var message = new StringBuilder(LO[nameof(R.GameStatistics)]).Append(':').AppendLine().AppendLine();

        foreach (var player in _data.Players)
        {
            var statistic = player.Statistic;

            message.Append(player.Name).Append(':').AppendLine();
            message.Append("   ").Append(LO[nameof(R.RightAnswers)]).Append(": ").Append(statistic.RightAnswerCount).AppendLine();
            message.Append("   ").Append(LO[nameof(R.WrongAnswers)]).Append(": ").Append(statistic.WrongAnswerCount).AppendLine();
            message.Append("   ").Append(LO[nameof(R.ScoreEarned)]).Append(": ").Append(statistic.RightTotal).AppendLine();
            message.Append("   ").Append(LO[nameof(R.ScoreLost)]).Append(": ").Append(statistic.WrongTotal).AppendLine();

            message.AppendLine();
        }

        _gameActions.SpecialReplic(message.ToString());
    }

    private void AskForPlayerReviews()
    {
        _data.ReportsCount = _data.Players.Count;
        _data.GameResultInfo.Reviews.Clear();

        ScheduleExecution(Tasks.WaitReport, 10 * 60 * 2); // 2 minutes
        WaitFor(DecisionType.Reporting, 10 * 60 * 2, -3);

        var reportString = _data.GameResultInfo.ToString(LO);

        foreach (var item in _data.Players)
        {
            _gameActions.SendMessageToWithArgs(item.Name, Messages.Report, reportString);
        }
    }

    private void WaitRight()
    {
        _gameActions.SendMessage(Messages.Cancel, _data.ShowMan.Name);

        if (_data.Answerer == null)
        {
            ScheduleExecution(Tasks.MoveNext, 10);
            return;
        }

        if (_data.Question == null)
        {
            throw new ArgumentException("_data.Question == null");
        }

        var answer = _data.Answerer.Answer;
        var isRight = answer != null && AnswerChecker.IsAnswerRight(answer, _data.Question.Right);

        _data.Answerer.AnswerIsRight = isRight;
        _data.Answerer.AnswerIsRightFactor = 1.0;

        _data.ShowmanDecision = true;
        OnDecision();
    }

    internal void AddHistory(string message) => _tasksHistory.AddLogEntry(message);

    private void WaitSelectQuestionPrice()
    {
        if (_data.AnswererIndex == -1)
        {
            throw new ArgumentException($"{nameof(_data.AnswererIndex)} == -1", nameof(_data.AnswererIndex));
        }

        _gameActions.SendMessage(Messages.Cancel, _data.Answerer.Name);

        if (_data.IsOralNow)
        {
            _gameActions.SendMessage(Messages.Cancel, _data.ShowMan.Name);
        }

        _data.CurPriceRight = _data.CatInfo.Minimum;
        _data.CurPriceWrong = _data.CatInfo.Minimum;

        OnDecision();
    }

    private void WaitAppellationDecision()
    {
        SendCancellationsToActivePlayers();
        OnDecision();
    }

    private void SendCancellationsToActivePlayers()
    {
        foreach (var player in _data.Players)
        {
            if (player.Flag)
            {
                _gameActions.SendMessage(Messages.Cancel, player.Name);
            }
        }
    }

    private void MoveNext()
    {
        if (_continuation != null)
        {
            _continuation();
            _continuation = null;
            return;
        }

        Engine?.MoveNext();
        ClientData.MoveNextBlocked = false;

        _tasksHistory.AddLogEntry($"Moved -> {Engine?.Stage}");
    }

    private void PrintStakeQuestionPlayer()
    {
        if (_data.StakerIndex == -1)
        {
            throw new ArgumentException($"{nameof(PrintStakeQuestionPlayer)}: {nameof(_data.StakerIndex)} == -1 {_data.OrderHistory}", nameof(_data.StakerIndex));
        }

        _data.ChooserIndex = _data.StakerIndex;
        _data.AnswererIndex = _data.StakerIndex;
        _data.QuestionPlayState.SetSingleAnswerer(_data.StakerIndex);
        _data.CurPriceRight = _data.Stake;
        _data.CurPriceWrong = _data.Stake;

        _gameActions.SendMessageWithArgs(Messages.SetChooser, ClientData.ChooserIndex, "+");

        var msg = $"{Notion.RandomString(LO[nameof(R.NowPlays)])} {_data.Players[_data.StakerIndex].Name} {LO[nameof(R.With)]} {Notion.FormatNumber(_data.Stake)}";

        _gameActions.ShowmanReplic(msg.ToString());
        ScheduleExecution(Tasks.MoveNext, 15 + Random.Shared.Next(10));
    }

    private void WaitNext(bool isSelectingStaker)
    {
        _gameActions.SendMessage(Messages.Cancel, _data.ShowMan.Name);
        _gameActions.SendMessageWithArgs(Messages.Timer, 2, MessageParams.Timer_Stop);

        var playerIndex = isSelectingStaker ? _data.Order[_data.OrderIndex] : _data.ThemeDeleters?.Current.PlayerIndex;

        if (playerIndex == -1) // The showman has not made a decision
        {
            var candidates = _data.Players.Where(p => p.Flag).ToArray();

            if (candidates.Length == 0)
            {
                throw new Exception(
                    "Wait next error (candidates.Length == 0): " +
                    (isSelectingStaker ? "" : _data.ThemeDeleters?.GetRemoveLog()));
            }

            var index = Random.Shared.Next(candidates.Length);
            var newPlayerIndex = _data.Players.IndexOf(candidates[index]);

            if (isSelectingStaker)
            {
                _data.Order[_data.OrderIndex] = newPlayerIndex;
                CheckOrder(_data.OrderIndex);
            }
            else
            {
                try
                {
                    _data.ThemeDeleters?.Current.SetIndex(newPlayerIndex);
                }
                catch (Exception exc)
                {
                    throw new Exception($"Wait delete error ({newPlayerIndex}): " + _data.ThemeDeleters?.GetRemoveLog(), exc);
                }
            }
        }

        OnDecision();
    }

    private void WaitStake()
    {
        if (_data.OrderIndex == -1)
        {
            throw new ArgumentException($"{nameof(_data.OrderIndex)} == -1: {_data.OrderHistory}");
        }

        var playerIndex = _data.Order[_data.OrderIndex];

        if (playerIndex < 0 || playerIndex >= _data.Players.Count)
        {
            throw new ArgumentException($"{nameof(playerIndex)} {playerIndex} must be in [0; {_data.Players.Count - 1}]: {_data.OrderHistory}");
        }

        _gameActions.SendMessage(Messages.Cancel, _data.Players[playerIndex].Name);

        if (_data.IsOralNow)
        {
            _gameActions.SendMessage(Messages.Cancel, _data.ShowMan.Name);
        }

        _gameActions.SendMessageWithArgs(Messages.Timer, 2, MessageParams.Timer_Stop);
        _data.StakeType = _data.StakeTypes.HasFlag(StakeTypes.Nominal) ? StakeMode.Nominal : StakeMode.Pass;

        OnDecision();
    }

    private void AskToSelectQuestionPrice()
    {
        var answerer = _data.Answerer ?? throw new InvalidOperationException("Answerer not defined");

        var s = string.Join(Message.ArgsSeparator, Messages.CatCost, _data.CatInfo.Minimum, _data.CatInfo.Maximum, _data.CatInfo.Step);

        var waitTime = _data.Settings.AppSettings.TimeSettings.TimeForMakingStake * 10;

        _data.IsOralNow = _data.IsOral && answerer.IsHuman;

        if (_data.IsOralNow)
        {
            _gameActions.SendMessage(s, _data.ShowMan.Name);
        }
        
        if (CanPlayerAct())
        {
            _gameActions.SendMessage(s, answerer.Name);

            if (!answerer.IsConnected)
            {
                waitTime = 20;
            }
        }

        _data.StakeModes = StakeModes.Stake;
        AskToMakeStake(StakeReason.Simple, answerer.Name, new (_data.CatInfo.Minimum, _data.CatInfo.Maximum, _data.CatInfo.Step));

        ScheduleExecution(Tasks.WaitSelectQuestionPrice, waitTime);
        WaitFor(DecisionType.QuestionPriceSelection, waitTime, _data.AnswererIndex);
    }

    private void WaitFirst()
    {
        _gameActions.SendMessage(Messages.Cancel, _data.ShowMan.Name);

        if (_data.ChooserIndex == -1)
        {
            _data.ChooserIndex = SelectRandom(_data.Players, p => p.Flag);
        }

        OnDecision();
    }

    private void WaitAnswer()
    {
        if (_data.Round == null)
        {
            throw new ArgumentNullException(nameof(_data.Round));
        }

        if (!HaveMultipleAnswerers())
        {
            if (_data.Answerer == null)
            {
                ScheduleExecution(Tasks.MoveNext, 10);
                return;
            }

            _gameActions.SendMessage(Messages.Cancel, _data.Answerer.Name);

            if (string.IsNullOrEmpty(_data.Answerer.Answer))
            {
                _data.Answerer.Answer = LO[nameof(R.IDontKnow)];
                _data.Answerer.AnswerIsWrong = true;
            }
        }
        else
        {
            for (var i = 0; i < _data.Players.Count; i++)
            {
                if (_data.QuestionPlayState.AnswererIndicies.Contains(i) && string.IsNullOrEmpty(_data.Players[i].Answer))
                {
                    _data.Players[i].Answer = LO[nameof(R.IDontKnow)];
                    _data.Players[i].AnswerIsWrong = true;
                }

                _gameActions.SendMessage(Messages.Cancel, _data.Players[i].Name);
            }

            _data.IsWaiting = true;
        }

        OnDecision();
    }

    private void WaitQuestionAnswererSelection()
    {
        _gameActions.SendMessage(Messages.Cancel, _data.Chooser.Name);

        if (_data.IsOralNow)
        {
            _gameActions.SendMessage(Messages.Cancel, _data.ShowMan.Name);
        }

        var index = SelectRandomOnIndex(_data.Players, index => index != _data.ChooserIndex);

        _data.AnswererIndex = index;
        _data.QuestionPlayState.SetSingleAnswerer(index);

        OnDecision();
    }

    private void WaitTry()
    {
        _data.IsThinking = false;
        _data.Decision = DecisionType.None;

        if (!IsSpecialQuestion())
        {
            _gameActions.SendMessageWithArgs(Messages.EndTry, MessageParams.EndTry_All); // Timer 1 STOP
        }

        _data.AnnounceAnswer = true;

        ScheduleExecution(Tasks.MoveNext, 1, force: true);

        _data.IsQuestionPlaying = false;
    }

    private void WaitFinalStake()
    {
        _gameActions.SendMessageWithArgs(Messages.Timer, 2, MessageParams.Timer_Stop);

        for (var i = 0; i < _data.Players.Count; i++)
        {
            if (_data.QuestionPlayState.AnswererIndicies.Contains(i) && _data.Players[i].FinalStake == -1)
            {
                _gameActions.SendMessage(Messages.Cancel, _data.Players[i].Name);
                _data.Players[i].FinalStake = 1;

                _gameActions.SendMessageWithArgs(Messages.PersonFinalStake, i);
            }
        }

        _data.NumOfStakers = 0;
        OnDecision();
    }

    private void WaitReport()
    {
        try
        {
            foreach (var item in _data.Players)
            {
                _gameActions.SendMessage(Messages.Cancel, item.Name);
            }

            SendReport();
            StopWaiting();
        }
        catch (Exception exc)
        {
            _data.Host.SendError(exc);
        }
    }

    private void AnnouncePostStake()
    {
        if (_data.AnnouncedAnswerersEnumerator == null || !_data.AnnouncedAnswerersEnumerator.MoveNext())
        {
            ScheduleExecution(Tasks.MoveNext, 15, 1, true);
            return;
        }

        var answererIndex = _data.AnnouncedAnswerersEnumerator.Current;
        _data.AnswererIndex = answererIndex;

        _data.PlayerIsRight = _data.Answerer?.Answer == _data.RightOptionLabel;
        AnnounceStakeCore();
        ScheduleExecution(Tasks.AnnouncePostStake, 15);
    }

    private void AnnounceStake()
    {
        AnnounceStakeCore();
        ScheduleExecution(Tasks.Announce, 15);
    }

    private void AnnounceStakeCore()
    {
        if (_data.Answerer == null)
        {
            throw new ArgumentException($"{nameof(_data.Answerer)} == null", nameof(_data.Answerer));
        }

        var msg = new StringBuilder();
        msg.AppendFormat("{0} {1}: {2}", LO[nameof(R.Stake)], _data.Answerer.Name, Notion.FormatNumber(_data.Answerer.FinalStake));
        _gameActions.ShowmanReplic(msg.ToString());

        msg = new StringBuilder(Messages.Person).Append(Message.ArgsSeparatorChar);

        if (_data.PlayerIsRight)
        {
            msg.Append('+');
            _data.Answerer.AddRightSum(_data.Answerer.FinalStake);
        }
        else
        {
            msg.Append('-');
            _data.Answerer.SubtractWrongSum(_data.Answerer.FinalStake);
        }

        msg.Append(Message.ArgsSeparatorChar).Append(_data.AnswererIndex);
        msg.Append(Message.ArgsSeparatorChar).Append(_data.Answerer.FinalStake);

        _gameActions.SendMessage(msg.ToString());
        _gameActions.InformSums();

        _gameActions.SendMessageWithArgs(Messages.PersonStake, _data.AnswererIndex, 1, _data.Answerer.FinalStake);
    }

    private void AskFinalStake()
    {
        var s = GetRandomString(LO[nameof(R.MakeStake)]);
        _gameActions.ShowmanReplic(s);

        _data.NumOfStakers = 0;
        var stakers = new List<(string, StakeSettings)>();

        for (var i = 0; i < _data.Players.Count; i++)
        {
            if (_data.QuestionPlayState.AnswererIndicies.Contains(i))
            {
                if (_data.Players[i].Sum <= 1)
                {
                    _data.Players[i].FinalStake = 1; // only one choice
                    _gameActions.SendMessageWithArgs(Messages.PersonFinalStake, i);
                    continue;
                }

                _data.Players[i].FinalStake = -1;
                _data.NumOfStakers++;
                _gameActions.SendMessage(Messages.FinalStake, _data.Players[i].Name);

                stakers.Add((_data.Players[i].Name, new(1, _data.Players[i].Sum, 1)));
            }
        }

        if (_data.NumOfStakers == 0)
        {
            ProceedToFinalQuestion();
            return;
        }

        _data.IsOralNow = false;
        _data.StakeModes = StakeModes.Stake;
        AskToMakeStake(StakeReason.Hidden, stakers);

        var waitTime = _data.Settings.AppSettings.TimeSettings.TimeForMakingStake * 10;
        ScheduleExecution(Tasks.WaitFinalStake, waitTime);
        WaitFor(DecisionType.FinalStakeMaking, waitTime, -2);
    }

    private void WaitDelete()
    {
        _gameActions.SendMessage(Messages.Cancel, _data.ActivePlayer.Name);

        if (_data.IsOralNow)
        {
            _gameActions.SendMessage(Messages.Cancel, _data.ShowMan.Name);
        }

        _gameActions.SendMessageWithArgs(Messages.Timer, 2, MessageParams.Timer_Stop);

        _data.ThemeIndexToDelete = SelectRandom(_data.TInfo.RoundInfo, item => item.Name != null);

        OnDecision();
    }

    private void Winner()
    {
        var big = _data.Players.Max(player => player.Sum);
        var winnersCount = _data.Players.Count(player => player.Sum == big);

        if (winnersCount == 1)
        {
            for (var i = 0; i < _data.Players.Count; i++)
            {
                if (_data.Players[i].Sum == big)
                {
                    var s = new StringBuilder(_data.Players[i].Name).Append(", ");
                    s.Append(GetRandomString(LO[nameof(R.YouWin)]));

                    _gameActions.ShowmanReplic(s.ToString());
                    _gameActions.SendMessageWithArgs(Messages.Winner, i);
                    break;
                }
            }
        }
        else
        {
            _gameActions.ShowmanReplic(LO[nameof(R.NoWinner)]);
            _gameActions.SendMessageWithArgs(Messages.Winner, -1);
        }

        ScheduleExecution(Tasks.GoodLuck, 20 + Random.Shared.Next(10));
    }

    private void AskToTry()
    {
        if (ClientData.Players.All(p => !p.CanPress))
        {
            ScheduleExecution(Tasks.WaitTry, 3, force: true);
            return;
        }

        if (_data.Settings.AppSettings.FalseStart)
        {
            _gameActions.SendMessage(Messages.Try);
        }

        SendTryToPlayers();

        var maxTime = _data.Settings.AppSettings.TimeSettings.TimeForThinkingOnQuestion * 10;

        _data.TimerStartTime[1] = DateTime.UtcNow;
        _data.IsThinking = true;
        _gameActions.SendMessageWithArgs(Messages.Timer, 1, MessageParams.Timer_Resume);
        _data.Decision = DecisionType.Pressing;

        ScheduleExecution(Tasks.WaitTry, Math.Max(maxTime - _data.TimeThinking, 10), force: true);
    }

    private void SendTryToPlayers()
    {
        for (var i = 0; i < _data.Players.Count; i++)
        {
            if (_data.Players[i].CanPress)
            {
                _gameActions.SendMessage(Messages.YouTry, _data.Players[i].Name);
            }
        }
    }

    private void WaitChoose()
    {
        _gameActions.SendMessage(Messages.Cancel, _data.Chooser.Name);

        if (_data.IsOralNow)
        {
            _gameActions.SendMessage(Messages.Cancel, _data.ShowMan.Name);
        }

        var canChooseTheme = _data.TInfo.RoundInfo.Select(t => t.Questions.Any(QuestionHelper.IsActive)).ToArray();
        var numberOfThemes = canChooseTheme.Where(can => can).Count();

        if (numberOfThemes == 0)
        {
            throw new Exception($"numberOfThemes == 0! GetRoundActiveQuestionsCount: {GetRoundActiveQuestionsCount()}");
        }

        // Random theme index selection
        var k1 = Random.Shared.Next(numberOfThemes);
        var i = -1;

        do if (canChooseTheme[++i]) k1--; while (k1 >= 0);

        var theme = _data.TInfo.RoundInfo[i];
        var numberOfQuestions = theme.Questions.Count(QuestionHelper.IsActive);

        // Random question index selection
        var k2 = Random.Shared.Next(numberOfQuestions);
        var j = -1;

        do if (theme.Questions[++j].IsActive()) k2--; while (k2 >= 0);

        lock (_data.ChoiceLock)
        {
            _data.ThemeIndex = i;
            _data.QuestionIndex = j;
        }

        OnDecision();
    }

    private void OnQuestionType(int arg)
    {
        var returnDelay = 20;

        if (arg == 1)
        {
            if (_data.Question == null)
            {
                throw new Exception(string.Format(LO[nameof(R.StrangeError)] + " {0} {1}", _data.Round.Type, _data.Settings.AppSettings.GameMode));
            }

            var authors = _data.PackageDoc.GetRealAuthors(_data.Question.Info.Authors);

            if (authors.Length > 0)
            {
                var res = new StringBuilder();
                res.AppendFormat(OfObjectPropertyFormat, LO[nameof(R.PAuthors)], LO[nameof(R.OfQuestion)], string.Join(", ", authors));
                _gameActions.ShowmanReplic(res.ToString());
            }
            else
            {
                arg++;
            }
        }

        if (arg == 2)
        {
            var themeComments = _data.Theme.Info.Comments.Text;

            if (themeComments.Length > 0)
            {
                _gameActions.ShowmanReplic(themeComments);
                _gameActions.SendMessageWithArgs(Messages.ThemeComments, themeComments.EscapeNewLines());
                returnDelay = 10;
            }
            else
            {
                arg++;
            }
        }

        if (arg == 3)
        {
            var typeName = _data.Question?.TypeName;

            // Only StakeAll type is supported in final for now
            // This will be removed when full question type support will be implemented
            if (HaveMultipleAnswerers())
            {
                typeName = QuestionTypes.StakeAll;
            }

            var delay = 1;

            switch (typeName)
            {
                case QuestionTypes.Stake:
                    _gameActions.ShowmanReplic(GetRandomString(LO[nameof(R.YouGetAuction)]));
                    delay = 16;
                    break;

                case QuestionTypes.Secret:
                case QuestionTypes.SecretPublicPrice:
                case QuestionTypes.SecretNoQuestion:
                    var replic = new StringBuilder(LO[nameof(R.YouReceiveCat)]);

                    var selectionMode = _data.Question?.Parameters?.FirstOrDefault(p => p.Key == QuestionParameterNames.SelectionMode);

                    if (selectionMode?.Value?.SimpleValue == StepParameterValues.SetAnswererSelect_Any)
                    {
                        replic.Append($". {LO[nameof(R.YouCanKeepCat)]}");
                    }

                    _gameActions.ShowmanReplic(replic.ToString());
                    delay = 10;
                    break;

                case QuestionTypes.NoRisk:
                    _gameActions.ShowmanReplic(LO[nameof(R.SponsoredQuestion)]);
                    delay = 16;
                    break;

                case QuestionTypes.Simple:
                    break;

                case QuestionTypes.StakeAll:
                    delay = 16;
                    break;

                default:
                    OnUnsupportedQuestionType(typeName ?? "");
                    return;
            }

            var msg = new MessageBuilder(Messages.QType, typeName);
            _gameActions.SendMessage(msg.ToString());

            ScheduleExecution(Tasks.MoveNext, delay, force: true);
            return;
        }

        ScheduleExecution(Tasks.QuestionType, returnDelay, arg + 1);
    }

    private void OnUnsupportedQuestionType(string typeName)
    {
        var sp = new StringBuilder(LO[nameof(R.UnknownType)]).Append(' ').Append(typeName);

        _gameActions.SpecialReplic(sp.ToString());
        _gameActions.SpecialReplic(LO[nameof(R.GameWillResume)]);
        _gameActions.ShowmanReplic(LO[nameof(R.ManuallyPlayedQuestion)]);

        Engine.SkipQuestion();
        ScheduleExecution(Tasks.MoveNext, 150, 1);
    }

    private void PrintPartial()
    {
        var text = _data.Text;

        // TODO: try to avoid getting here when such condition is met
        if (_data.TextLength >= text.Length)
        {
            _data.TimeThinking = 0.0;
            ScheduleExecution(Tasks.MoveNext, 1, force: true);
            return;
        }

        _data.PartialIterationCounter++;

        var newTextLength = Math.Min(
            _data.InitialPartialTextLength
                + (int)(_data.Settings.AppSettings.ReadingSpeed * PartialPrintFrequencyPerSecond * _data.PartialIterationCounter),
            text.Length);

        if (newTextLength > _data.TextLength)
        {
            var printingLength = newTextLength - _data.TextLength;

            // Align to next space position
            while (_data.TextLength + printingLength < text.Length && !char.IsWhiteSpace(text[_data.TextLength + printingLength]))
            {
                printingLength++;
            }

            var subText = text.Substring(_data.TextLength, printingLength);

            _gameActions.SendMessageWithArgs(Messages.ContentAppend, ContentPlacements.Screen, 0, ContentTypes.Text, subText.EscapeNewLines());
            _gameActions.SendMessageWithArgs(Messages.Atom, Constants.PartialText, subText);
            _gameActions.SystemReplic(subText);

            _data.TextLength += printingLength;
        }

        if (_data.TextLength < text.Length)
        {
            _data.AtomTime -= (int)(10 * PartialPrintFrequencyPerSecond);
            ScheduleExecution(Tasks.PrintPartial, 10 * PartialPrintFrequencyPerSecond, force: true);
        }
        else
        {
            _data.TimeThinking = 0.0;
            ScheduleExecution(Tasks.MoveNext, Math.Max(_data.AtomTime, 10), force: true);
        }
    }

    private void QuestionSourcesAndComments(int arg)
    {
        var informed = false;

        var textTime = 20;

        if (arg == 1)
        {
            var sources = _data.PackageDoc.GetRealSources(_data.Question.Info.Sources);

            if (sources.Length > 0 && _data.Settings.AppSettings.DisplaySources)
            {
                var text = string.Format(OfObjectPropertyFormat, LO[nameof(R.PSources)], LO[nameof(R.OfQuestion)], string.Join(", ", sources));
                _gameActions.ShowmanReplic(text);
                textTime = GetReadingDurationForTextLength(text.Length);
                informed = true;
            }
            else
            {
                arg++;
            }
        }

        if (arg == 2)
        {
            if (_data.Question.Info.Comments.Text.Length > 0)
            {
                var text = string.Format(
                    OfObjectPropertyFormat,
                    LO[nameof(R.PComments)],
                    LO[nameof(R.OfQuestion)],
                    _data.Question.Info.Comments.Text);
                
                _gameActions.ShowmanReplic(text);
                textTime = GetReadingDurationForTextLength(text.Length);
                informed = true;
            }
            else
            {
                arg++;
            }
        }

        if (arg < 3)
        {
            ScheduleExecution(Tasks.QuestSourComm, textTime, arg + 1);
        }
        else
        {
            ScheduleExecution(Tasks.MoveNext, 1, force: !informed);
        }
    }

    private int GetReadingDurationForTextLength(int textLength)
    {
        var readingSpeed = Math.Max(1, _data.Settings.AppSettings.ReadingSpeed);
        return Math.Max(1, 10 * textLength / readingSpeed);
    }

    internal void Announce()
    {
        if (_data.AnnouncedAnswerersEnumerator == null || !_data.AnnouncedAnswerersEnumerator.MoveNext())
        {
            if (_data.QuestionPlayState.AnswerOptions != null)
            {
                var m = new MessageBuilder(Messages.Answers);
                m.AddRange(_data.Players.Select(p => p.Answer ?? ""));
                _gameActions.SendMessage(m.ToString());
            }

            ScheduleExecution(Tasks.MoveNext, 15, 1, true);
            return;
        }

        var answererIndex = _data.AnnouncedAnswerersEnumerator.Current;
        _data.AnswererIndex = answererIndex;
        var playerAnswer = _data.Answerer?.Answer;
        var answer = string.IsNullOrEmpty(playerAnswer) ? LO[nameof(R.IDontKnow)] : playerAnswer;

        _gameActions.PlayerReplic(answererIndex, answer);

        if (_data.QuestionPlayState.AnswerOptions != null)
        {
            ScheduleExecution(Tasks.Announce, 25, force: true);
        }
        else
        {
            ScheduleExecution(Tasks.AskRight, 35, force: true);
        }
    }

    internal bool PrepareForAskAnswer()
    {
        var buttonPressMode = ClientData.Settings.AppSettings.ButtonPressMode;

        if (buttonPressMode == ButtonPressMode.RandomWithinInterval)
        {
            if (ClientData.PendingAnswererIndicies.Count == 0)
            {
                DumpButtonPressError("ClientData.PendingAnswererIndicies.Count == 0");
                return false;
            }

            var index = ClientData.PendingAnswererIndicies.Count == 1 ? 0 : Random.Shared.Next(ClientData.PendingAnswererIndicies.Count);
            ClientData.PendingAnswererIndex = ClientData.PendingAnswererIndicies[index];
        }

        if (ClientData.PendingAnswererIndex < 0 || ClientData.PendingAnswererIndex >= ClientData.Players.Count)
        {
            DumpButtonPressError($"ClientData.PendingAnswererIndex = {ClientData.PendingAnswererIndex}; ClientData.Players.Count = {ClientData.Players.Count}");
            return false;
        }

        ClientData.AnswererIndex = ClientData.PendingAnswererIndex;
        ClientData.QuestionPlayState.SetSingleAnswerer(ClientData.PendingAnswererIndex);

        if (!ClientData.Settings.AppSettings.FalseStart)
        {
            // Stop question reading
            if (!ClientData.IsQuestionFinished)
            {
                var timeDiff = (int)DateTime.UtcNow.Subtract(ClientData.AtomStart).TotalSeconds * 10;
                ClientData.AtomTime = Math.Max(1, ClientData.AtomTime - timeDiff);
            }
        }

        if (_data.IsThinking)
        {
            var startTime = _data.TimerStartTime[1];
            var currentTime = DateTime.UtcNow;

            ClientData.TimeThinking += currentTime.Subtract(startTime).TotalMilliseconds / 100;
        }

        ClientData.Answerer.CanPress = false;

        _data.IsThinking = false;

        _gameActions.SendMessageWithArgs(Messages.Timer, 1, MessageParams.Timer_Pause, (int)ClientData.TimeThinking);

        _data.IsPlayingMediaPaused = _data.IsPlayingMedia;
        _data.IsPlayingMedia = false;

        return true;
    }

    internal void DumpButtonPressError(string reason)
    {
        var pressMode = ClientData.Settings.AppSettings.ButtonPressMode;
        _data.Host.SendError(new Exception($"{reason} {pressMode}"));
    }

    private void StartGame(int arg)
    {
        var nextArg = arg + 1;
        var extraTime = 0;

        switch (arg)
        {
            case 1:
                _gameActions.ShowmanReplic(LO[nameof(R.ShowmanGreeting)]);
                nextArg = 2;
                break;

            case 2:
                _gameActions.ShowmanReplic($"{LO[nameof(R.GameRules)]}: {BuildRulesString(ClientData.Settings.AppSettings)}");
                nextArg = -1;
                extraTime = 20;
                break;

            default:
                _gameActions.SpecialReplic(LO[nameof(R.WrongGameState)] + " - " + Tasks.StartGame);
                break;
        }

        if (nextArg != -1)
        {
            ScheduleExecution(Tasks.StartGame, 10 + extraTime, nextArg);
        }
        else
        {
            ScheduleExecution(Tasks.MoveNext, 10 + extraTime, 0);
        }
    }

    private string BuildRulesString(AppSettingsCore settings)
    {
        var rules = new List<string>();

        if (settings.GameMode == GameModes.Sport)
        {
            rules.Add(LO[nameof(R.TypeSport)]);
        }

        if (!settings.FalseStart)
        {
            rules.Add(LO[nameof(R.TypeNoFalseStart)]);
        }

        if (settings.Oral)
        {
            rules.Add(LO[nameof(R.TypeOral)]);
        }

        if (settings.IgnoreWrong)
        {
            rules.Add(LO[nameof(R.TypeIgnoreWrong)]);
        }

        if (settings.Managed)
        {
            rules.Add(LO[nameof(R.TypeManaged)]);
        }

        if (settings.AllowEveryoneToPlayHiddenStakes)
        {
            rules.Add(LO[nameof(R.TypeAllowEveryoneToPlayStakes)]);
        }

        if (rules.Count == 0)
        {
            rules.Add(LO[nameof(R.TypeClassic)]);
        }

        return string.Join(", ", rules);
    }

    private void AskToChoose()
    {
        _gameActions.InformSums();
        _gameActions.AnnounceSums();
        _gameActions.SendMessage(Messages.ShowTable);

        if (_data.Chooser == null)
        {
            throw new Exception("_data.Chooser == null");
        }

        if (_gameActions.Client.CurrentServer == null)
        {
            throw new Exception("_actor.Client.CurrentServer == null");
        }

        var msg = new StringBuilder(_data.Chooser.Name).Append(", ");
        var activeQuestionsCount = GetRoundActiveQuestionsCount();

        if (activeQuestionsCount == 0)
        {
            throw new Exception($"activeQuestionsCount == 0 {Engine.Stage}");
        }

        msg.Append(GetRandomString(LO[activeQuestionsCount > 1 ? nameof(R.ChooseQuest) : nameof(R.LastQuest)]));

        _gameActions.ShowmanReplic(msg.ToString());

        lock (_data.ChoiceLock)
        {
            _data.ThemeIndex = -1;
            _data.QuestionIndex = -1;
        }

        _data.UsedWrongVersions.Clear();

        int time;

        if (activeQuestionsCount > 1)
        {
            time = _data.Settings.AppSettings.TimeSettings.TimeForChoosingQuestion * 10;

            var message = $"{Messages.Choose}{Message.ArgsSeparatorChar}1";
            _data.IsOralNow = _data.IsOral && _data.Chooser.IsHuman;

            if (_data.IsOralNow)
            {
                _gameActions.SendMessage(message, _data.ShowMan.Name);
            }
            else if (!_data.Chooser.IsConnected)
            {
                time = 20;
            }

            if (CanPlayerAct())
            {
                _gameActions.SendMessage(message, _data.Chooser.Name);
            }
        }
        else
        {
            time = 20;
        }

        ScheduleExecution(Tasks.WaitChoose, time);
        WaitFor(DecisionType.QuestionSelection, time, _data.ChooserIndex);
    }

    internal bool CanPlayerAct() => !_data.IsOralNow || _data.Settings.AppSettings.OralPlayersActions;

    private void AskToSelectQuestionAnswerer()
    {
        // -- Deprecated
        var msg = new StringBuilder(Messages.Cat);

        for (var i = 0; i < _data.Players.Count; i++)
        {
            msg.Append(Message.ArgsSeparatorChar).Append(_data.Players[i].Flag ? '+' : '-');
        }

        _data.AnswererIndex = -1;

        var waitTime = _data.Settings.AppSettings.TimeSettings.TimeForGivingACat * 10;

        _data.IsOralNow = _data.IsOral && _data.Chooser.IsHuman;
        var playerSelectors = new List<string>();

        if (_data.IsOralNow)
        {
            playerSelectors.Add(_data.ShowMan.Name);
            _gameActions.SendMessage(msg.ToString(), _data.ShowMan.Name);
        }
        else if (!_data.Chooser.IsConnected)
        {
            waitTime = 20;
        }

        if (CanPlayerAct() && _data.Chooser != null)
        {
            playerSelectors.Add(_data.Chooser.Name);
            _gameActions.SendMessage(msg.ToString(), _data.Chooser.Name);
        }

        AskToSelectPlayer(SelectPlayerReason.Answerer, playerSelectors.ToArray());
        ScheduleExecution(Tasks.WaitQuestionAnswererSelection, waitTime);
        WaitFor(DecisionType.QuestionAnswererSelection, waitTime, _data.ChooserIndex);
    }

    /// <summary>
    /// Finds players with minimum sum.
    /// If there is only one player, they got the move.
    /// Otherwise ask showman to select moving player.
    /// </summary>
    private void GiveMoveToPlayerWithMinimumScore()
    {
        var min = _data.Players.Min(player => player.Sum);
        var total = 0;

        for (var i = 0; i < _data.Players.Count; i++)
        {
            if (_data.Players[i].Sum == min)
            {
                _data.Players[i].Flag = true;
                total++;
            }
            else
            {
                _data.Players[i].Flag = false;
            }
        }

        if (total == 1)
        {
            for (var i = 0; i < _data.Players.Count; i++)
            {
                if (_data.Players[i].Flag)
                {
                    _data.ChooserIndex = i;
                    break;
                }
            }

            _data.IsWaiting = true;
            _data.Decision = DecisionType.StarterChoosing;
            OnDecision();
        }
        else
        {
            _data.ChooserIndex = -1;

            // -- Deprecated
            var msg = new StringBuilder(Messages.First);

            for (var i = 0; i < _data.Players.Count; i++)
            {
                msg.Append(Message.ArgsSeparatorChar).Append(_data.Players[i].Flag ? '+' : '-');
            }

            _gameActions.SendMessage(msg.ToString(), _data.ShowMan.Name);
            // -- end
            AskToSelectPlayer(SelectPlayerReason.Chooser, _data.ShowMan.Name);

            var waitTime = _data.Settings.AppSettings.TimeSettings.TimeForShowmanDecisions * 10;
            ScheduleExecution(Tasks.WaitFirst, waitTime);
            WaitFor(DecisionType.StarterChoosing, waitTime, -1);
        }
    }

    private void AskToSelectPlayer(SelectPlayerReason reason, params string[] selectors)
    {
        var msg = new MessageBuilder(Messages.AskSelectPlayer, reason);

        for (var i = 0; i < _data.Players.Count; i++)
        {
            msg.Add(_data.Players[i].Flag ? '+' : '-');
        }

        foreach (var selector in selectors)
        {
            _gameActions.SendMessage(msg.ToString(), selector);
        }

        _data.DecisionMakers.Clear();
        _data.DecisionMakers.AddRange(selectors);
    }

    private void AskRight()
    {
        if (_data.Answerer == null)
        {
            throw new InvalidOperationException("Answerer is null");
        }

        _data.ShowmanDecision = false;

        if (_data.QuestionPlayState.AnswerOptions != null)
        {
            _data.IsWaiting = true;
            _data.Decision = DecisionType.AnswerValidating;

            var rightLabel = ClientData.Question?.Right.FirstOrDefault();

            _data.Answerer.AnswerIsRight = _data.Answerer.Answer == rightLabel;
            _data.Answerer.AnswerIsRightFactor = 1.0;
            _data.ShowmanDecision = true;

            OnDecision();
        }
        else if (!_data.Answerer.IsHuman)
        {
            _data.IsWaiting = true;
            _data.Decision = DecisionType.AnswerValidating;

            _data.Answerer.AnswerIsRight = !_data.Answerer.AnswerIsWrong;
            _data.Answerer.AnswerIsRightFactor = 1.0;
            _data.ShowmanDecision = true;

            OnDecision();
        }
        else
        {
            if (!_data.IsOralNow || HaveMultipleAnswerers())
            {
                SendAnswersInfoToShowman(_data.Answerer.Answer ?? "");
            }

            var waitTime = _data.Settings.AppSettings.TimeSettings.TimeForShowmanDecisions * 10;
            ScheduleExecution(Tasks.WaitRight, waitTime);
            WaitFor(DecisionType.AnswerValidating, waitTime, -1);
        }
    }

    private void SendAnswersInfoToShowman(string answer)
    {
        _gameActions.SendMessage(BuildValidationMessage(_data.Answerer.Name, answer), _data.ShowMan.Name);

        _gameActions.SendMessage(
            BuildValidation2Message(_data.Answerer.Name, answer, _data.AnswerMode == StepParameterValues.AskAnswerMode_Button),
            _data.ShowMan.Name);
    }

    [Obsolete]
    private string BuildValidationMessage(string name, string answer, bool isCheckingForTheRight = true)
    {
        var question = _data.Question ?? throw new InvalidOperationException("Question is null");

        var rightAnswers = question.Right;
        var wrongAnswers = question.Wrong;

        return new MessageBuilder(Messages.Validation, name, answer, isCheckingForTheRight ? '+' : '-', rightAnswers.Count)
            .AddRange(rightAnswers)
            .AddRange(wrongAnswers)
            .Build();
    }

    private string BuildValidation2Message(string name, string answer, bool allowPriceModifications, bool isCheckingForTheRight = true)
    {
        var question = _data.Question ?? throw new InvalidOperationException("Question is null");

        var rightAnswers = question.Right;
        var wrongAnswers = question.Wrong;

        return new MessageBuilder(
            Messages.Validation2,
            name,
            answer,
            isCheckingForTheRight ? '+' : '-',
            allowPriceModifications ? '+' : '-',
            rightAnswers.Count)
            .AddRange(rightAnswers)
            .AddRange(wrongAnswers)
            .Build();
    }

    private void AskAnswer()
    {
        var timeSettings = _data.Settings.AppSettings.TimeSettings;

        if (HaveMultipleAnswerers())
        {
            _gameActions.ShowmanReplic(LO[nameof(R.StartThink)]);

            for (var i = 0; i < _data.Players.Count; i++)
            {
                if (_data.QuestionPlayState.AnswererIndicies.Contains(i))
                {
                    _data.Players[i].Answer = "";
                    _data.Players[i].Flag = true;
                    _gameActions.SendMessage(Messages.Answer, _data.Players[i].Name);
                }
            }

            _data.AnswerCount = _data.QuestionPlayState.AnswererIndicies.Count;
            ScheduleExecution(Tasks.WaitAnswer, timeSettings.TimeForFinalThinking * 10, force: true);
            WaitFor(DecisionType.Answering, timeSettings.TimeForFinalThinking * 10, -2, false);
            return;
        }

        if (_data.Answerer == null)
        {
            ScheduleExecution(Tasks.MoveNext, 10);
            return;
        }

        if (_data.Question?.TypeName == QuestionTypes.Simple)
        {
            _gameActions.SendMessageWithArgs(Messages.EndTry, _data.AnswererIndex);
        }
        else
        {
            _gameActions.SendMessageWithArgs(Messages.StopPlay);
        }

        var waitAnswerTime = _data.Question?.TypeName != QuestionTypes.Simple
            ? timeSettings.TimeForThinkingOnSpecial * 10
            : timeSettings.TimeForPrintingAnswer * 10;

        var useAnswerOptions = _data.QuestionPlayState.AnswerOptions != null;
        _data.IsOralNow = _data.IsOral && _data.Answerer.IsHuman;

        if (useAnswerOptions)
        {
            if (_data.IsOralNow)
            {
                _gameActions.SendMessage(Messages.Answer, _data.ShowMan.Name);
            }
            
            if (CanPlayerAct())
            {
                _gameActions.SendMessage(Messages.Answer, _data.Answerer.Name);
            }
        }
        else
        {
            if (_data.IsOralNow)
            {
                // Showman accepts answer orally
                SendAnswersInfoToShowman($"({LO[nameof(R.AnswerIsOral)]})");
            }
            else // The only place where we do not check CanPlayerAct()
            {
                // TODO: Support forced written answers here
                _gameActions.SendMessage(Messages.Answer, _data.Answerer.Name);
            }
        }

        var answerReplic = useAnswerOptions ? ", " + LO[nameof(R.SelectAnswerOption)] : GetRandomString(LO[nameof(R.YourAnswer)]);
        _gameActions.ShowmanReplic(_data.Answerer.Name + answerReplic);

        _data.Answerer.Answer = "";

        var buttonPressMode = ClientData.Settings.AppSettings.ButtonPressMode;

        if (buttonPressMode != ButtonPressMode.FirstWins)
        {
            InformWrongTries();
        }

        _data.AnswerCount = 1;
        ScheduleExecution(Tasks.WaitAnswer, waitAnswerTime);
        WaitFor(DecisionType.Answering, waitAnswerTime, _data.AnswererIndex);
    }

    private void InformWrongTries()
    {
        for (var i = 0; i < ClientData.PendingAnswererIndicies.Count; i++)
        {
            var playerIndex = ClientData.PendingAnswererIndicies[i];

            if (playerIndex == ClientData.PendingAnswererIndex)
            {
                continue;
            }

            _gameActions.SendMessageWithArgs(Messages.WrongTry, playerIndex);
        }
    }

    private void AskToDelete()
    {
        int playerIndex = -1;

        try
        {
            _data.ThemeDeleters.MoveNext();
            var currentDeleter = _data.ThemeDeleters.Current;

            if (currentDeleter.PlayerIndex == -1)
            {
                var indicies = currentDeleter.PossibleIndicies;

                if (indicies.Count > 1)
                {
                    RequestForCurrentDeleter(indicies);
                    return;
                }
                else if (indicies.Count == 0)
                {
                    throw new Exception("indicies.Count == 0: " + _data.ThemeDeleters.GetRemoveLog());
                }

                currentDeleter.SetIndex(indicies.First());
            }

            playerIndex = currentDeleter.PlayerIndex;

            if (playerIndex < -1 || playerIndex >= _data.Players.Count)
            {
                throw new ArgumentException($"{nameof(playerIndex)}: {_data.ThemeDeleters.GetRemoveLog()}");
            }

            _data.ActivePlayer = _data.Players[playerIndex];

            RequestForThemeDelete();
        }
        catch (Exception exc)
        {
            _data.Host.SendError(new Exception(string.Format("AskToDelete {0}/{1}/{2}", _data.ThemeDeleters.Current.PlayerIndex, playerIndex, _data.Players.Count), exc));
        }
    }

    private void RequestForThemeDelete()
    {
        var msg = new StringBuilder(_data.ActivePlayer.Name)
            .Append(", ")
            .Append(GetRandomString(LO[nameof(R.DeleteTheme)]));

        _gameActions.ShowmanReplic(msg.ToString());

        var message = string.Join(Message.ArgsSeparator, Messages.Choose, 2);
        _data.IsOralNow = _data.IsOral && _data.ActivePlayer.IsHuman;

        var waitTime = _data.Settings.AppSettings.TimeSettings.TimeForChoosingFinalTheme * 10;

        if (_data.IsOralNow)
        {
            _gameActions.SendMessage(message, _data.ShowMan.Name);
        }
        else if (!_data.ActivePlayer.IsConnected)
        {
            waitTime = 20;
        }

        if (CanPlayerAct())
        {
            _gameActions.SendMessage(message, _data.ActivePlayer.Name);
        }

        _data.ThemeIndexToDelete = -1;
        ScheduleExecution(Tasks.WaitDelete, waitTime);
        WaitFor(DecisionType.FinalThemeDeleting, waitTime, _data.Players.IndexOf(_data.ActivePlayer));
    }

    private void RequestForCurrentDeleter(ICollection<int> indicies)
    {
        for (var i = 0; i < _data.Players.Count; i++)
        {
            _data.Players[i].Flag = indicies.Contains(i);
        }

        // -- deprecated
        var msg = new StringBuilder(Messages.FirstDelete);

        for (var i = 0; i < _data.Players.Count; i++)
        {
            msg.Append(Message.ArgsSeparatorChar).Append(_data.Players[i].Flag ? '+' : '-');
        }

        _gameActions.SendMessage(msg.ToString(), _data.ShowMan.Name);
        // -- end
        AskToSelectPlayer(SelectPlayerReason.Deleter, _data.ShowMan.Name);

        var waitTime = _data.Settings.AppSettings.TimeSettings.TimeForShowmanDecisions * 10;
        ScheduleExecution(Tasks.WaitNext, waitTime, 1);
        WaitFor(DecisionType.NextPersonFinalThemeDeleting, waitTime, -1);
    }

    /// <summary>
    /// Определить следующего ставящего
    /// </summary>
    /// <returns>Стоит ли продолжать выполнение</returns>
    private bool DetectNextStaker()
    {
        var errorLog = new StringBuilder()
            .Append(' ').Append(_data.Stake).Append(' ').Append(_data.OrderIndex)
            .Append(' ').Append(string.Join(":", _data.Order))
            .Append(' ').Append(string.Join(":", _data.Players.Select(p => p.Sum)))
            .Append(' ').Append(string.Join(":", _data.Players.Select(p => p.StakeMaking)))
            .Append(' ').Append(string.Join(":", _data.OrderHistory));

        var stage = 0;

        try
        {
            var candidatesAll = Enumerable.Range(0, _data.Order.Length).Except(_data.Order).ToArray(); // Незадействованные игроки
            
            if (_data.OrderIndex < _data.Order.Length - 1)
            {
                // Ещё есть, из кого выбирать

                // Сначала отбросим тех, у кого недостаточно денег для ставки
                var candidates = candidatesAll.Where(n => _data.Players[n].StakeMaking);

                if (candidates.Count() > 1)
                {
                    // У кандидатов должна быть минимальная сумма
                    var minSum = candidates.Min(n => _data.Players[n].Sum);
                    candidates = candidates.Where(n => _data.Players[n].Sum == minSum);
                }

                if (!candidates.Any()) // Никто из оставшихся не может перебить ставку
                {
                    stage = 1;

                    var ind = _data.OrderIndex;

                    if (_data.OrderIndex + candidatesAll.Length > _data.Order.Length)
                    {
                        throw new InvalidOperationException(
                            $"Invalid order index. Order index: {_data.OrderIndex}; " +
                            $"candidates length: {candidatesAll.Length}; order length: {_data.Order.Length}");
                    }

                    for (var i = 0; i < candidatesAll.Length; i++)
                    {
                        _data.Order[ind + i] = candidatesAll[i];
                        CheckOrder(ind + i);
                        _data.Players[candidatesAll[i]].StakeMaking = false;
                    }

                    stage = 2;

                    var stakersCount = _data.Players.Count(p => p.StakeMaking);

                    if (stakersCount == 1)
                    {
                        // Игрок определён
                        for (var i = 0; i < _data.Players.Count; i++)
                        {
                            if (_data.Players[i].StakeMaking)
                            {
                                _data.StakerIndex = i;
                                break;
                            }
                        }

                        _data.ChooserIndex = _data.StakerIndex;
                        _data.AnswererIndex = _data.StakerIndex;
                        _data.QuestionPlayState.SetSingleAnswerer(_data.StakerIndex);
                        _data.CurPriceRight = _data.Stake;
                        _data.CurPriceWrong = _data.Stake;

                        if (_data.AnswererIndex == -1)
                        {
                            _data.Host.SendError(new Exception("this.data.AnswererIndex == -1"), true);
                        }

                        _gameActions.SendMessageWithArgs(Messages.SetChooser, ClientData.ChooserIndex, "+");

                        ScheduleExecution(Tasks.MoveNext, 10, 1);
                        return false;
                    }

                    _data.OrderIndex = -1;
                    AskStake(false);
                    return false;
                }

                stage = 3;

                _data.IsWaiting = false;

                if (candidates.Count() == 1)
                {
                    _data.Order[_data.OrderIndex] = candidates.First();
                    CheckOrder(_data.OrderIndex);
                }
                else
                {
                    // Showman should choose the next staker
                    for (var i = 0; i < _data.Players.Count; i++)
                    {
                        _data.Players[i].Flag = candidates.Contains(i);
                    }

                    // -- deprecated
                    var msg = new StringBuilder(Messages.FirstStake);

                    for (var i = 0; i < _data.Players.Count; i++)
                    {
                        msg.Append(Message.ArgsSeparatorChar).Append(_data.Players[i].Flag ? '+' : '-');
                    }

                    _gameActions.SendMessage(msg.ToString(), _data.ShowMan.Name);
                    // -- end
                    AskToSelectPlayer(SelectPlayerReason.Staker, _data.ShowMan.Name);
                    _data.OrderHistory.AppendLine("Asking showman for the next staker");

                    var time = _data.Settings.AppSettings.TimeSettings.TimeForShowmanDecisions * 10;
                    ScheduleExecution(Tasks.WaitNext, time);
                    WaitFor(DecisionType.NextPersonStakeMaking, time, -1);
                    return false;
                }
            }
            else
            {
                stage = 4;

                // Остался последний игрок, выбор очевиден
                var leftIndex = candidatesAll[0];
                _data.Order[_data.OrderIndex] = leftIndex;
                CheckOrder(_data.OrderIndex);
            }

            return true;
        }
        catch (Exception exc)
        {
            errorLog.Append(' ').Append(stage);
            throw new Exception(errorLog.ToString(), exc);
        }
    }

    public void CheckOrder(int index)
    {
        if (index < 0 || index >= _data.Order.Length)
        {
            throw new ArgumentException($"Value {index} must be in [0; {_data.Order.Length}]", nameof(index));
        }

        var checkedValue = _data.Order[index];

        if (checkedValue == -1)
        {
            throw new Exception("_data.Order[index] == -1");
        }

        for (var i = 0; i < _data.Order.Length; i++)
        {
            var value = _data.Order[i];

            if (value == -1 || i == index)
            {
                continue;
            }

            if (checkedValue == value)
            {
                throw new Exception($"_data.Order contains at least two occurences of {checkedValue}!");
            }
        }
    }

    private void AskStake(bool canDetectNextStakerGuard)
    {
        var cost = _data.Question.Price;

        try
        {
            _data.OrderHistory
                .Append($"AskStake: Order = {string.Join(",", _data.Order)};")
                .Append($" OrderIndex = {_data.OrderIndex};")
                .Append($" StakeMaking = {string.Join(",", _data.Players.Select(p => p.StakeMaking))}")
                .AppendLine();

            IncrementOrderIndex();

            if (_data.Order[_data.OrderIndex] == -1) // Необходимо определить следующего ставящего
            {
                if (!canDetectNextStakerGuard)
                {
                    throw new Exception("!canDetectNextStaker");
                }

                if (!DetectNextStaker())
                {
                    return;
                }

                _data.OrderHistory.Append($"NextStaker = {_data.Order[_data.OrderIndex]}").AppendLine();
            }

            var others = _data.Players.Where((p, index) => index != _data.Order[_data.OrderIndex]); // Те, кто сейчас не делают ставку
            
            if (others.All(p => !p.StakeMaking) && _data.StakerIndex > -1) // Остальные не могут ставить
            {
                // Нельзя повысить ставку
                ScheduleExecution(Tasks.PrintAuctPlayer, 10);
                return;
            }

            var playerIndex = _data.Order[_data.OrderIndex];

            if (playerIndex < 0 || playerIndex >= _data.Players.Count)
            {
                throw new ArgumentException($"Bad {nameof(playerIndex)} value {playerIndex}! It must be in [0; {_data.Players.Count - 1}]");
            }

            var activePlayer = _data.Players[playerIndex];
            var playerMoney = activePlayer.Sum;

            if (_data.Stake != -1 && playerMoney <= _data.Stake) // Could not make stakes
            {
                activePlayer.StakeMaking = false;
                _gameActions.SendMessageWithArgs(Messages.PersonStake, playerIndex, 2);

                var stakersCount = _data.Players.Count(p => p.StakeMaking);

                if (stakersCount == 1) // Answerer is detected
                {
                    for (var i = 0; i < _data.Players.Count; i++)
                    {
                        if (_data.Players[i].StakeMaking)
                        {
                            _data.StakerIndex = i;
                        }
                    }

                    ScheduleExecution(Tasks.PrintAuctPlayer, 10);
                    return;
                }

                ScheduleExecution(Tasks.AskStake, 5);
                return;
            }

            // Detecting possible stake outcomes

            // Only nominal
            if (_data.Stake == -1 && (playerMoney < cost || playerMoney == cost && others.All(p => playerMoney >= p.Sum)))
            {
                var s = new StringBuilder(activePlayer.Name)
                    .Append(", ").Append(LO[nameof(R.YouCanSayOnly)])
                    .Append(' ').Append(LO[nameof(R.Nominal)]);

                _gameActions.ShowmanReplic(s.ToString());

                _data.StakerIndex = playerIndex;
                _data.Stake = cost;
                _gameActions.SendMessageWithArgs(Messages.PersonStake, playerIndex, 1, cost);
                ScheduleExecution(Tasks.AskStake, 5, force: true);
                return;
            }

            var minimumStake = (_data.Stake != -1 ? _data.Stake : cost) + ClientData.StakeStep;
            var minimumStakeAligned = (int)Math.Ceiling((double)minimumStake / ClientData.StakeStep) * ClientData.StakeStep;

            _data.StakeTypes = StakeTypes.AllIn | (_data.StakerIndex == -1 ? StakeTypes.Nominal : StakeTypes.Pass);
            _data.StakeModes = StakeModes.AllIn;

            if (_data.StakerIndex != -1)
            {
                _data.StakeModes |= StakeModes.Pass;
            }

            if (!_data.AllIn && playerMoney >= minimumStakeAligned)
            {
                _data.StakeTypes |= StakeTypes.Stake;
                _data.StakeModes |= StakeModes.Stake;
            }

            _data.StakeVariants[0] = _data.StakerIndex == -1;
            _data.StakeVariants[1] = !_data.AllIn && playerMoney != cost && playerMoney > _data.Stake + ClientData.StakeStep;
            _data.StakeVariants[2] = !_data.StakeVariants[0];
            _data.StakeVariants[3] = true;

            _data.ActivePlayer = activePlayer;

            _data.IsOralNow = _data.IsOral && _data.ActivePlayer.IsHuman;

            var stakeMsg = new MessageBuilder(Messages.Stake);
            var stakeMsg2 = new MessageBuilder(Messages.Stake2);

            for (var i = 0; i < _data.StakeVariants.Length; i++)
            {
                stakeMsg.Add(_data.StakeVariants[i] ? '+' : '-');
            }

            stakeMsg2.Add(_data.StakeTypes);

            stakeMsg.Add(minimumStakeAligned);
            stakeMsg2.Add(minimumStakeAligned);
            stakeMsg2.Add(ClientData.StakeStep);

            var waitTime = _data.Settings.AppSettings.TimeSettings.TimeForMakingStake * 10;

            if (CanPlayerAct())
            {
                _gameActions.SendMessage(stakeMsg.Build(), _data.ActivePlayer.Name);
                _gameActions.SendMessage(stakeMsg2.Build(), _data.ActivePlayer.Name);

                if (!_data.ActivePlayer.IsConnected)
                {
                    waitTime = 20;
                }
            }

            if (_data.IsOralNow)
            {
                stakeMsg.Add(_data.ActivePlayer.Sum); // Send maximum possible value to showman
                stakeMsg.Add(_data.ActivePlayer.Name);
                _gameActions.SendMessage(stakeMsg.Build(), _data.ShowMan.Name);

                stakeMsg2.Add(_data.ActivePlayer.Sum); // Send maximum possible value to showman
                stakeMsg2.Add(_data.ActivePlayer.Name);
                _gameActions.SendMessage(stakeMsg2.Build(), _data.ShowMan.Name);
            }

            var stakeLimit = new StakeSettings(minimumStakeAligned - ClientData.StakeStep, _data.ActivePlayer.Sum, _data.StakeStep);
            AskToMakeStake(StakeReason.HighestPlays, _data.ActivePlayer.Name, stakeLimit);

            _data.StakeType = null;
            _data.StakeSum = -1;
            ScheduleExecution(Tasks.WaitStake, waitTime);
            WaitFor(DecisionType.StakeMaking, waitTime, _data.Players.IndexOf(_data.ActivePlayer));
        }
        catch (Exception exc)
        {
            var orders = string.Join(",", _data.Order);
            var sums = string.Join(",", _data.Players.Select(p => p.Sum));
            var stakeMaking = string.Join(",", _data.Players.Select(p => p.StakeMaking));
            throw new Exception($"AskStake error {sums} {stakeMaking} {orders} {_data.Stake} {_data.OrderIndex} {_data.Players.Count} {_data.OrderHistory}", exc);
        }
    }

    private void AskToMakeStake(StakeReason reason, string name, StakeSettings limit)
    {
        var stakeReplic = new StringBuilder(name).Append(", ").Append(GetRandomString(LO[nameof(R.YourStake)]));
        _gameActions.ShowmanReplic(stakeReplic.ToString());

        AskToMakeStake(reason, new[] { (name, limit) });
    }

    private void AskToMakeStake(StakeReason reason, IEnumerable<(string name, StakeSettings limit)> persons)
    {
        _data.DecisionMakers.Clear();
        _data.StakeLimits.Clear();

        foreach (var (name, limit) in persons)
        {
            var stakeMessage = new MessageBuilder(Messages.AskStake, _data.StakeModes, limit.Minimum, limit.Maximum, limit.Step, reason);

            if (CanPlayerAct())
            {
                _gameActions.SendMessage(stakeMessage.Build(), name);
            }

            if (_data.IsOralNow)
            {
                stakeMessage.Add(name);
                _gameActions.SendMessage(stakeMessage.Build(), _data.ShowMan.Name);
            }

            _data.DecisionMakers.Add(name);
            _data.StakeLimits[name] = limit;
        }

        if (_data.IsOralNow)
        {
            _data.DecisionMakers.Add(_data.ShowMan.Name);
        }
    }

    private void IncrementOrderIndex()
    {
        var breakerGuard = 20; // Temp var

        var initialOrderIndex = _data.OrderIndex == -1 ? _data.Order.Length - 1 : _data.OrderIndex;

        // TODO: Rewrite as for
        do
        {
            _data.OrderIndex++;

            if (_data.OrderIndex == _data.Order.Length)
            {
                _data.OrderIndex = 0;
            }

            breakerGuard--;

            if (breakerGuard == 0)
            {
                throw new Exception($"{nameof(breakerGuard)} == {breakerGuard} ({initialOrderIndex})");
            }

        } while (_data.OrderIndex != initialOrderIndex &&
            _data.Order[_data.OrderIndex] != -1 &&
            !_data.Players[_data.Order[_data.OrderIndex]].StakeMaking);

        if (_data.OrderIndex == initialOrderIndex)
        {
            throw new Exception($"{nameof(_data.OrderIndex)} == {nameof(initialOrderIndex)} ({initialOrderIndex})");
        }

        _data.OrderHistory.AppendFormat("New order index: {0}", _data.OrderIndex).AppendLine();
    }

    private void PrintAppellation()
    {
        if (_data.AppelaerIndex < 0 || _data.AppelaerIndex >= _data.Players.Count)
        {
            _tasksHistory.AddLogEntry($"PrintAppellation resumed ({_taskRunner.PrintOldTasks()})");
            ResumeExecution(40);
            return;
        }

        var appelaer = _data.Players[_data.AppelaerIndex];

        var given = LO[appelaer.IsMale ? nameof(R.HeGave) : nameof(R.SheGave)];
        var apellationReplic = string.Format(LO[nameof(R.PleaseCheckApellation)], given);

        string origin = _data.IsAppelationForRightAnswer
            ? LO[nameof(R.IsApellating)]
            : string.Format(LO[nameof(R.IsConsideringWrong)], appelaer.Name);

        _gameActions.ShowmanReplic($"{_data.AppellationSource} {origin}. {apellationReplic}");

        var validationMessage = BuildValidationMessage(appelaer.Name, appelaer.Answer ?? "", _data.IsAppelationForRightAnswer);
        var validation2Message = BuildValidation2Message(appelaer.Name, appelaer.Answer ?? "", false, _data.IsAppelationForRightAnswer);

        _data.AppellationAwaitedVoteCount = 0;
        _data.AppellationTotalVoteCount = _data.Players.Count + 1; // players and showman
        _data.AppellationPositiveVoteCount = 0;
        _data.AppellationNegativeVoteCount = 0;

        // Showman vote
        if (_data.IsAppelationForRightAnswer)
        {
            _data.AppellationNegativeVoteCount++;
        }
        else
        {
            _data.AppellationPositiveVoteCount++;
        }

        for (var i = 0; i < _data.Players.Count; i++)
        {
            if (i == _data.AppelaerIndex)
            {
                _data.Players[i].ApellationFlag = false;
                _data.AppellationPositiveVoteCount++;
            }
            else if (!_data.IsAppelationForRightAnswer && i == _data.AppellationCallerIndex)
            {
                _data.Players[i].ApellationFlag = false;
                _data.AppellationNegativeVoteCount++;
                _gameActions.SendMessageWithArgs(Messages.PersonApellated, i);
            }
            else
            {
                _data.AppellationAwaitedVoteCount++;
                _data.Players[i].ApellationFlag = true;
                _gameActions.SendMessage(validationMessage, _data.Players[i].Name);
                _gameActions.SendMessage(validation2Message, _data.Players[i].Name);
            }
        }

        var waitTime = _data.Settings.AppSettings.TimeSettings.TimeForShowmanDecisions * 10;
        ScheduleExecution(Tasks.WaitAppellationDecision, waitTime);
        WaitFor(DecisionType.AppellationDecision, waitTime, -2);
    }

    internal int ResumeExecution(int resumeTime = 0) => _taskRunner.ResumeExecution(resumeTime, ShouldRunTimer());

    private void CheckAppellation()
    {
        if (_data.AppelaerIndex < 0 || _data.AppelaerIndex >= _data.Players.Count)
        {
            _tasksHistory.AddLogEntry($"CheckAppellation resumed ({_taskRunner.PrintOldTasks()})");
            ResumeExecution(40);
            return;
        }

        var votingForRight = _data.IsAppelationForRightAnswer;
        var positiveVoteCount = _data.AppellationPositiveVoteCount;
        var negativeVoteCount = _data.AppellationNegativeVoteCount;

        if (votingForRight && positiveVoteCount <= negativeVoteCount || !votingForRight && positiveVoteCount >= negativeVoteCount)
        {
            _gameActions.ShowmanReplic($"{LO[nameof(R.ApellationDenied)]}!");
            _tasksHistory.AddLogEntry($"CheckAppellation denied and resumed normally ({_taskRunner.PrintOldTasks()})");
            ResumeExecution(40);
            return;
        }

        // Commit appellation
        _gameActions.ShowmanReplic($"{LO[nameof(R.ApellationAccepted)]}!");

        if (votingForRight)
        {
            ApplyAppellationForRightAnswer();
        }

        UpdatePlayersSumsAfterAppellation(votingForRight);

        _gameActions.InformSums();
        _gameActions.AnnounceSums();

        _tasksHistory.AddLogEntry($"CheckAppellation resumed normally ({_taskRunner.PrintOldTasks()})");
        ResumeExecution(40);
    }

    private void ApplyAppellationForRightAnswer()
    {
        var appelaer = _data.Players[_data.AppelaerIndex];

        var themeName = _data.Theme.Name;
        var questionText = _data.Question?.GetText();

        // Add appellated answer to game report
        var answerInfo = _data.GameResultInfo.RejectedAnswers.FirstOrDefault(
            answer =>
                answer.ThemeName == themeName
                && answer.QuestionText == questionText
                && answer.ReportText == appelaer.Answer);

        if (answerInfo != null)
        {
            _data.GameResultInfo.RejectedAnswers.Remove(answerInfo);
        }

        _data.GameResultInfo.ApellatedAnswers.Add(new QuestionReport
        {
            ThemeName = themeName,
            QuestionText = questionText,
            ReportText = appelaer.Answer
        });
    }

    private void UpdatePlayersSumsAfterAppellation(bool isVotingForRightAnswer)
    {
        var change = false;
        var singleAnswerer = !HaveMultipleAnswerers();

        for (var i = 0; i < _data.QuestionHistory.Count; i++)
        {
            var historyItem = _data.QuestionHistory[i];
            var index = historyItem.PlayerIndex;
            var player = _data.Players[index];

            if (isVotingForRightAnswer && singleAnswerer && index != _data.AppelaerIndex)
            {
                if (!change)
                {
                    continue;
                }

                if (historyItem.IsRight)
                {
                    player.UndoRightSum(historyItem.Sum);
                }
                else
                {
                    player.UndoWrongSum(historyItem.Sum);
                }
            }
            else if (index == _data.AppelaerIndex)
            {
                if (singleAnswerer)
                {
                    change = true;

                    if (historyItem.IsRight)
                    {
                        player.UndoRightSum(historyItem.Sum);
                        player.SubtractWrongSum(_data.CurPriceWrong);
                    }
                    else
                    {
                        player.UndoWrongSum(historyItem.Sum);
                        player.AddRightSum(_data.CurPriceRight);

                        // TODO: that should be handled by question selection strategy
                        if (Engine.CanMoveBack) // Not the beginning of a round
                        {
                            _data.ChooserIndex = index;
                            _gameActions.SendMessageWithArgs(Messages.SetChooser, ClientData.ChooserIndex);
                        }
                    }
                }
                else
                {
                    var stake = player.FinalStake;

                    if (historyItem.IsRight)
                    {
                        player.UndoRightSum(historyItem.Sum);
                        player.SubtractWrongSum(stake);
                    }
                    else
                    {
                        player.UndoWrongSum(historyItem.Sum);
                        player.AddRightSum(stake);
                    }
                }
            }
        }
    }

    private void OnTheme(Theme theme, int arg, bool isFull)
    {
        var informed = false;

        if (arg == 1)
        {
            var authors = _data.PackageDoc.GetRealAuthors(theme.Info.Authors);

            if (authors.Length > 0)
            {
                informed = true;
                var res = new StringBuilder();
                res.AppendFormat(OfObjectPropertyFormat, LO[nameof(R.PAuthors)], LO[nameof(R.OfTheme)], string.Join(", ", authors));
                _gameActions.ShowmanReplic(res.ToString());
            }
            else
            {
                arg++;
            }
        }

        if (arg == 2)
        {
            var sources = _data.PackageDoc.GetRealSources(theme.Info.Sources);

            if (sources.Length > 0 && _data.Settings.AppSettings.DisplaySources)
            {
                informed = true;
                var res = new StringBuilder();
                res.AppendFormat(OfObjectPropertyFormat, LO[nameof(R.PSources)], LO[nameof(R.OfTheme)], string.Join(", ", sources));
                _gameActions.ShowmanReplic(res.ToString());
            }
            else
            {
                arg++;
            }
        }

        if (arg < 2)
        {
            ScheduleExecution(isFull ? Tasks.Theme : Tasks.ThemeInfo, 20, arg + 1);
        }
        else
        {
            _data.ThemeInfoShown.Add(_data.Theme);
            var delay = informed ? 20 : 1;

            if (isFull)
            {
                ScheduleExecution(Tasks.QuestionType, delay, 1, force: !informed);
            }
            else
            {
                ScheduleExecution(Tasks.MoveNext, delay);
            }
        }
    }

    private void WaitFor(DecisionType decision, int time, int person, bool isWaiting = true)
    {
        _data.TimerStartTime[2] = DateTime.UtcNow;

        _data.IsWaiting = isWaiting;
        _data.Decision = decision;

        _gameActions.SendMessageWithArgs(Messages.Timer, 2, MessageParams.Timer_Go, time, person);
    }

    private void OnPackage(Package package, int stage)
    {
        var informed = false;
        var isRandomPackage = package.Info.Comments.Text.StartsWith(PackageHelper.RandomIndicator);

        var baseTime = 0;

        if (stage == 1)
        {
            if (!isRandomPackage)
            {
                var packageName = package.Name == RandomIndicator ? LO[nameof(R.RandomPackageName)] : package.Name;

                _gameActions.ShowmanReplic(string.Format(OfObjectPropertyFormat, LO[nameof(R.PName)], LO[nameof(R.OfPackage)], packageName));
                informed = true;

                var messageBuilder = new MessageBuilder(Messages.Package).Add(packageName);

                var logoItem = package.LogoItem;

                if (logoItem != null)
                {
                    // Old
                    ShareMedia(logoItem);

                    // New
                    var (success, globalUri, _) = TryShareContent(logoItem);

                    if (success && globalUri != null)
                    {
                        messageBuilder.Add(ContentTypes.Image).Add(globalUri);
                    }
                }

                _gameActions.SendMessage(messageBuilder.ToString());
            }
            else
            {
                stage++;
            }
        }

        if (stage == 2)
        {
            var authors = _data.PackageDoc.GetRealAuthors(package.Info.Authors);

            if (!isRandomPackage && package.Name != RandomIndicator && authors.Length > 0)
            {
                informed = true;
                var res = new StringBuilder();
                res.AppendFormat(OfObjectPropertyFormat, LO[nameof(R.PAuthors)], LO[nameof(R.OfPackage)], string.Join(", ", authors));
                _gameActions.ShowmanReplic(res.ToString());
            }
            else
            {
                stage++;
            }
        }

        if (stage == 3)
        {
            var sources = _data.PackageDoc.GetRealSources(package.Info.Sources);

            if (sources.Length > 0 && _data.Settings.AppSettings.DisplaySources)
            {
                informed = true;
                var res = new StringBuilder();
                res.AppendFormat(OfObjectPropertyFormat, LO[nameof(R.PSources)], LO[nameof(R.OfPackage)], string.Join(", ", sources));
                _gameActions.ShowmanReplic(res.ToString());
            }
            else
            {
                stage++;
            }
        }

        if (stage == 4)
        {
            if (package.Info.Comments.Text.Length > 0 && !isRandomPackage && package.Name != RandomIndicator)
            {
                informed = true;
                var res = new StringBuilder();
                res.AppendFormat(OfObjectPropertyFormat, LO[nameof(R.PComments)], LO[nameof(R.OfPackage)], package.Info.Comments.Text);
                _gameActions.ShowmanReplic(res.ToString());

                baseTime = GetReadingDurationForTextLength(package.Info.Comments.Text.Length);
            }
            else
            {
                stage++;
            }
        }

        if (stage == 5)
        {
            if (!string.IsNullOrWhiteSpace(package.Restriction))
            {
                informed = true;
                var res = new StringBuilder();
                res.AppendFormat(OfObjectPropertyFormat, LO[nameof(R.Restrictions)], LO[nameof(R.OfPackage)], package.Restriction);
                _gameActions.ShowmanReplic(res.ToString());
            }
            else
            {
                stage++;
            }
        }

        if (stage == 6)
        {
            if (!string.IsNullOrWhiteSpace(package.Date) && !isRandomPackage)
            {
                informed = true;
                var res = new StringBuilder();
                res.AppendFormat(OfObjectPropertyFormat, LO[nameof(R.CreationDate)], LO[nameof(R.OfPackage)], package.Date);
                _gameActions.ShowmanReplic(res.ToString());
            }
            else
            {
                stage++;
            }
        }

        if (stage < 6)
        {
            ScheduleExecution(Tasks.Package, baseTime + 15, stage + 1, force: !informed);
        }
        else if (informed)
        {
            ScheduleExecution(Tasks.MoveNext, 10);
        }
        else
        {
            ScheduleExecution(Tasks.MoveNext, 1, force: !informed);
        }
    }

    private void OnRound(Round round, int stage)
    {
        var informed = false;
        var baseTime = stage == 1 ? 30 : 20;

        if (stage == 1)
        {
            _gameActions.InformSums();

            _data.TableInformStage = 0;
            _data.IsRoundEnding = false;

            var roundIndex = Engine.RoundIndex;
            var roundName = LO.GetRoundName(round.Name);

            _data.Stage = GameStage.Round;
            _data.LegacyStage = round.Type == RoundTypes.Final ? GameStages.Final : GameStages.Round;
            OnStageChanged(GameStages.Round, roundName, roundIndex + 1, _data.Rounds.Length);

            _gameActions.InformRound(roundName, roundIndex, _data.RoundStrategy);
            _gameActions.InformRoundContent();

            _gameActions.ShowmanReplic($"{GetRandomString(LO[nameof(R.WeBeginRound)])} {roundName}!");
            _gameActions.SystemReplic(" "); // new line
            _gameActions.SystemReplic(roundName);
        }
        else if (stage == 2)
        {
            var authors = _data.PackageDoc.GetRealAuthors(round.Info.Authors);

            if (authors.Length > 0)
            {
                informed = true;
                var res = new StringBuilder();
                res.AppendFormat(OfObjectPropertyFormat, LO[nameof(R.PAuthors)], LO[nameof(R.OfRound)], string.Join(", ", authors));
                _gameActions.ShowmanReplic(res.ToString());
            }
            else
            {
                stage++;
            }
        }

        if (stage == 3)
        {
            var sources = _data.PackageDoc.GetRealSources(round.Info.Sources);

            if (sources.Length > 0 && _data.Settings.AppSettings.DisplaySources)
            {
                informed = true;
                var res = new StringBuilder();
                res.AppendFormat(OfObjectPropertyFormat, LO[nameof(R.PSources)], LO[nameof(R.OfRound)], string.Join(", ", sources));
                _gameActions.ShowmanReplic(res.ToString());
            }
            else
            {
                stage++;
            }
        }

        if (stage == 4)
        {
            if (round.Info.Comments.Text.Length > 0)
            {
                informed = true;
                var res = new StringBuilder();
                res.AppendFormat(OfObjectPropertyFormat, LO[nameof(R.PComments)], LO[nameof(R.OfRound)], round.Info.Comments.Text);
                _gameActions.ShowmanReplic(res.ToString());

                baseTime = GetReadingDurationForTextLength(round.Info.Comments.Text.Length);
            }
            else
            {
                stage++;
            }
        }

        var adShown = false;

        if (stage == 5)
        {
            // Showing advertisement
            try
            {
                var ad = ClientData.Host.GetAd(LO.Culture.TwoLetterISOLanguageName, out int adId);

                if (!string.IsNullOrEmpty(ad))
                {
                    informed = true;
                    var res = new StringBuilder(LO[nameof(R.Ads)]).Append(": ").Append(ad);

                    _gameActions.ShowmanReplic(res.ToString());
                    _gameActions.SpecialReplic(res.ToString());

                    _gameActions.SendMessageWithArgs(Messages.Ads, ad);

#if !DEBUG
                    // Advertisement could not be skipped
                    ClientData.MoveNextBlocked = !ClientData.Settings.AppSettings.Managed;
#endif
                    adShown = true;

                    OnAdShown(adId);
                }
                else
                {
                    stage++;
                }
            }
            catch (Exception exc)
            {
                _data.Host.SendError(exc);
                stage++;
            }
        }

        if (stage < 5)
        {
            ScheduleExecution(Tasks.Round, baseTime + Random.Shared.Next(10), stage + 1);
        }
        else if (informed)
        {
            ScheduleExecution(Tasks.MoveNext, (adShown ? 40 : 20) + Random.Shared.Next(10));
        }
        else
        {
            ScheduleExecution(Tasks.MoveNext, 1);
        }
    }

    internal void SetAnswererAsActive()
    {
        _data.AnswererIndex = _data.ChooserIndex;
        _data.QuestionPlayState.SetSingleAnswerer(_data.ChooserIndex);

        _gameActions.SendMessageWithArgs(Messages.SetChooser, ClientData.ChooserIndex, '+');

        ScheduleExecution(Tasks.MoveNext, 5);
    }

    internal void SetAnswererByActive(bool canGiveThemselves)
    {
        if (_data.ChooserIndex == -1)
        {
            _data.ChooserIndex = DetectPlayerIndexWithLowestSum();
        }

        if (_data.Chooser == null)
        {
            throw new InvalidOperationException("_data.Chooser == null");
        }

        for (var i = 0; i < _data.Players.Count; i++)
        {
            _data.Players[i].Flag = true;
        }

        if (!canGiveThemselves)
        {
            _data.Chooser.Flag = false;
        }

        var variantsCount = _data.Players.Count(player => player.Flag);

        if (variantsCount == 1)
        {
            for (var i = 0; i < _data.Players.Count; i++)
            {
                if (_data.Players[i].Flag)
                {
                    _data.ChooserIndex = _data.AnswererIndex = i;
                    _data.QuestionPlayState.SetSingleAnswerer(i);
                    _gameActions.SendMessageWithArgs(Messages.SetChooser, ClientData.ChooserIndex);
                }
            }

            _gameActions.ShowmanReplic($"{_data.Answerer.Name}, {LO[nameof(R.CatIsYours)]}!");
            ScheduleExecution(Tasks.MoveNext, 10);
        }
        else
        {
            _gameActions.ShowmanReplic($"{_data.Chooser.Name}, {LO[nameof(R.GiveCat)]}");
            ScheduleExecution(Tasks.AskToSelectQuestionAnswerer, 10 + Random.Shared.Next(10), force: true);
        }
    }

    internal void OnButtonPressStart()
    {
        foreach (var player in _data.Players)
        {
            player.CanPress = true;
        }

        _data.Decision = DecisionType.Pressing;
        _gameActions.SendMessageWithArgs(Messages.Try, MessageParams.Try_NotFinished);

        SendTryToPlayers();
    }

    internal void OnSetTheme(string themeName)
    {
        if (string.IsNullOrWhiteSpace(themeName))
        {
            themeName = _data.Theme?.Name ?? "";
        }

        _gameActions.SendMessageWithArgs(Messages.QuestionCaption, themeName);

        var s = new StringBuilder(LO[nameof(R.Theme)]).Append(": ").Append(themeName);

        _gameActions.ShowmanReplic(s.ToString());

        ScheduleExecution(Tasks.MoveNext, 15);
    }

    internal void OnSimpleAnswer(string answer)
    {
        var normalizedAnswer = (answer ?? LO[nameof(R.AnswerNotSet)]).LeaveFirst(MaxAnswerLength);

        _gameActions.SendMessageWithArgs(Messages.RightAnswer, ContentTypes.Text, normalizedAnswer);

        var answerTime = Math.Max(
            GetReadingDurationForTextLength(normalizedAnswer.Length),
            _data.Settings.AppSettings.TimeSettings.TimeForRightAnswer * 10);

        ScheduleExecution(Tasks.MoveNext, answerTime);
    }

    internal void OnComplexAnswer()
    {
        var last = _data.QuestionHistory.LastOrDefault();
        var answer = _data.Question?.Right.FirstOrDefault();

        if (last == null || !last.IsRight) // There has been no right answer
        {
            var printedAnswer = answer != null ? $"{LO[nameof(R.RightAnswer)]}: {answer}" : LO[nameof(R.RightAnswerInOnTheScreen)];
            _gameActions.ShowmanReplic(printedAnswer);
        }

        if (_data.QuestionPlayState.AnswerOptions != null)
        {
            var answerIndex = Array.FindIndex(_data.QuestionPlayState.AnswerOptions, o => o.Label == answer);

            if (answerIndex > -1)
            {
                _gameActions.SendMessageWithArgs(Messages.ContentState, ContentPlacements.Screen, answerIndex + 1, ItemState.Right);
            }
        }

        _gameActions.SendMessageWithArgs(Messages.RightAnswerStart, ContentTypes.Text, answer ?? "");
    }

    internal void OnRightAnswerOption(string rightOptionLabel)
    {
        _data.RightOptionLabel = rightOptionLabel;
        _gameActions.SendMessageWithArgs(Messages.RightAnswer, ContentTypes.Text, rightOptionLabel);
        var answerTime = _data.Settings.AppSettings.TimeSettings.TimeForRightAnswer;

        if (HaveMultipleAnswerers() && _data.AnnouncedAnswerersEnumerator != null)
        {
            _data.AnnouncedAnswerersEnumerator.Reset();
            ScheduleExecution(Tasks.AnnouncePostStake, (answerTime == 0 ? 2 : answerTime) * 10);
            return;
        }

        ScheduleExecution(Tasks.MoveNext, (answerTime == 0 ? 2 : answerTime) * 10);
    }

    internal void OnAnnouncePrice(NumberSet availableRange)
    {
        var s = new StringBuilder(LO[nameof(R.Cost2)]).Append(": ");

        if (availableRange.Maximum == 0)
        {
            s.Append(LO[nameof(R.MinMaxChoice)]);
        }
        else if (availableRange.Minimum == availableRange.Maximum)
        {
            s.Append(availableRange.Minimum);
        }
        else
        {
            if (availableRange.Step > 0 && availableRange.Step < availableRange.Maximum - availableRange.Minimum)
            {
                s.Append(
                    $"{LO[nameof(R.From)]} {Notion.FormatNumber(availableRange.Minimum)} {LO[nameof(R.From)]} {Notion.FormatNumber(availableRange.Maximum)} " +
                    $"{LO[nameof(R.WithStepOf)]} {Notion.FormatNumber(availableRange.Step)} ({LO[nameof(R.YourChoice)]})");
            }
            else
            {
                s.Append($"{Notion.FormatNumber(availableRange.Minimum)} {LO[nameof(R.Or)]} {Notion.FormatNumber(availableRange.Maximum)} ({LO[nameof(R.YourChoice)]})");
            }
        }

        _gameActions.ShowmanReplic(s.ToString());
        ScheduleExecution(Tasks.MoveNext, 10);
    }

    internal void OnSelectPrice(NumberSet availableRange)
    {
        _data.CatInfo = availableRange;

        if (availableRange.Maximum == 0)
        {
            var possiblePrices = _data.CatInfo;

            possiblePrices.Minimum = _minRoundPrice;
            possiblePrices.Maximum = _maxRoundPrice;
            possiblePrices.Step = possiblePrices.Maximum - possiblePrices.Minimum;

            if (possiblePrices.Step == 0)
            {
                _data.CurPriceRight = possiblePrices.Maximum;
                _data.CurPriceWrong = _data.CurPriceRight;
                _gameActions.SendMessageWithArgs(Messages.PersonStake, _data.AnswererIndex, 1, _data.CurPriceRight);
                ScheduleExecution(Tasks.MoveNext, 1);
            }
            else
            {
                _data.CurPriceRight = -1;
                ScheduleExecution(Tasks.AskToSelectQuestionPrice, 1, force: true);
            }
        }
        else if (availableRange.Minimum == availableRange.Maximum)
        {
            _data.CurPriceWrong = _data.CurPriceRight = availableRange.Minimum;
            _gameActions.SendMessageWithArgs(Messages.PersonStake, _data.AnswererIndex, 1, _data.CurPriceRight);
            ScheduleExecution(Tasks.MoveNext, 1);
        }
        else
        {
            _data.CurPriceRight = -1;
            ScheduleExecution(Tasks.AskToSelectQuestionPrice, 1, force: true);
        }
    }

    internal void SetAnswererByHighestVisibleStake()
    {
        var nominal = _data.Question.Price;

        if (_data.ChooserIndex == -1)
        {
            _data.ChooserIndex = DetectPlayerIndexWithLowestSum(); // TODO: set chooser index at the beginning of round
        }

        _data.Order = new int[_data.Players.Count];

        for (var i = 0; i < _data.Players.Count; i++)
        {
            _data.Players[i].StakeMaking = i == _data.ChooserIndex || _data.Players[i].Sum > nominal;
            _data.Order[i] = -1;
        }

        _data.Stake = _data.StakerIndex = -1;

        _data.Order[0] = _data.ChooserIndex;

        _data.OrderHistory.Clear();

        _data.OrderHistory.Append("Stake making. Initial state. ")
            .Append("Sums: ")
            .Append(string.Join(",", _data.Players.Select(p => p.Sum)))
            .Append(" Order: ")
            .Append(string.Join(",", _data.Order))
            .Append(" Nominal: ")
            .Append(_data.CurPriceRight)
            .AppendLine();

        _data.AllIn = false;
        _data.OrderIndex = -1;
        ScheduleExecution(Tasks.AskStake, 10);
    }

    private int DetectPlayerIndexWithLowestSum()
    {
        var minSum = _data.Players.Min(p => p.Sum);
        return ClientData.Players.TakeWhile(p => p.Sum != minSum).Count();
    }

    internal void SetAnswerersByAllHiddenStakes()
    {
        var answerers = new List<int>();

        for (var i = 0; i < ClientData.Players.Count; i++)
        {
            if (ClientData.Players[i].Sum > 0 || ClientData.Settings.AppSettings.AllowEveryoneToPlayHiddenStakes)
            {
                answerers.Add(i);
            }
        }

        ClientData.QuestionPlayState.SetMultipleAnswerers(answerers);
        AskFinalStake();
    }

    internal void OnSetNoRiskPrice()
    {
        if (_data.ChooserIndex == -1)
        {
            _data.ChooserIndex = DetectPlayerIndexWithLowestSum(); // TODO: set chooser index at the beginning of round
        }

        _data.CurPriceRight *= 2;
        _data.CurPriceWrong = 0;

        _gameActions.ShowmanReplic(
            $"{_data.Chooser!.Name}, {string.Format(LO[nameof(R.SponsoredQuestionInfo)], Notion.FormatNumber(_data.CurPriceRight))}");
        
        _gameActions.SendMessageWithArgs(Messages.PersonStake, _data.AnswererIndex, 1, _data.CurPriceRight, _data.CurPriceWrong);

        ScheduleExecution(Tasks.MoveNext, 20);
    }

    internal void AcceptQuestion()
    {
        if (_data.Answerer == null)
        {
            throw new InvalidOperationException("_data.Answerer == null");
        }

        _gameActions.ShowmanReplic(LO[nameof(R.EasyCat)]);
        _gameActions.SendMessageWithArgs(Messages.Person, '+', _data.AnswererIndex, _data.CurPriceRight);

        _data.Answerer.AddRightSum(_data.CurPriceRight);
        _data.ChooserIndex = _data.AnswererIndex;
        _gameActions.SendMessageWithArgs(Messages.SetChooser, ClientData.ChooserIndex);
        _gameActions.InformSums();

        Engine.SkipQuestion();
        ScheduleExecution(Tasks.MoveNext, 20, 1);
    }

    internal void OnAnswerOptions()
    {
        var screenContentSequence = _data.QuestionPlayState.ScreenContentSequence;
        var answerOptions = _data.QuestionPlayState.AnswerOptions;

        if (answerOptions == null || screenContentSequence == null)
        {
            return;
        }

        var messageBuilder = new MessageBuilder(Messages.Layout)
            .Add(MessageParams.Layout_AnswerOptions)
            .Add(string.Join("|", screenContentSequence.Select(group => string.Join("+", group.Select(serializeContentItem)))))
            .AddRange(answerOptions.Select(o => o.Content.Type));

        _gameActions.SendMessage(messageBuilder.ToString());

        static string serializeContentItem(ContentItem ci) => $"{ci.Type}{(ci.Type == ContentTypes.Text ? "." + ci.Value.Length : "")}";
    }

    internal void ShowAnswerOptions(Action? continuation)
    {
        if (ClientData.QuestionPlayState.AnswerOptions == null)
        {
            throw new InvalidOperationException("AnswerOptions == null");
        }

        var nextTask = ClientData.QuestionPlayState.AnswerOptions.Length > 0 ? Tasks.ShowNextAnswerOption : Tasks.MoveNext;
        ScheduleExecution(nextTask, 1, 0);
        _continuation = continuation;
    }

    internal void ShowNextAnswerOption(int optionIndex)
    {
        if (ClientData.QuestionPlayState.AnswerOptions == null)
        {
            throw new InvalidOperationException("AnswerOptions == null");
        }

        var answerOption = ClientData.QuestionPlayState.AnswerOptions[optionIndex];        

        var messageBuilder = new MessageBuilder(Messages.Content)
            .Add(ContentPlacements.Screen)
            .Add(optionIndex + 1)
            .Add(answerOption.Label);

        if (answerOption.Content.Type == ContentTypes.Text)
        {
            messageBuilder.Add(ContentTypes.Text).Add(answerOption.Content.Value.EscapeNewLines());
        }
        else
        {
            var (success, globalUri, _) = TryShareContent(answerOption.Content);

            if (!success || globalUri == null)
            {
                messageBuilder.Add(ContentTypes.Text).Add(string.Format(LO[nameof(R.MediaNotFound)], globalUri));
            }
            else
            {
                messageBuilder.Add(answerOption.Content.Type).Add(globalUri);
            }
        }

        _gameActions.SendMessage(messageBuilder.ToString());

        var nextTask = optionIndex + 1 < ClientData.QuestionPlayState.AnswerOptions.Length ? Tasks.ShowNextAnswerOption : Tasks.MoveNext;
        ScheduleExecution(nextTask, ClientData.Settings.AppSettings.DisplayAnswerOptionsOneByOne ? 10 : 1, optionIndex + 1);
    }

    internal void OnComplexContent(Dictionary<string, List<ContentItem>> contentTable)
    {
        var contentTime = -1;
        var registeredMediaPlay = false;

        foreach (var (placement, contentList) in contentTable)
        {
            var contentListDuration = 0;

            var messageBuilder = new MessageBuilder(Messages.Content).Add(placement);

            foreach (var contentItem in contentList)
            {
                var legacyBuilder = new MessageBuilder(placement == ContentPlacements.Background && contentTable.Keys.Count > 1 ? Messages.Atom_Second : Messages.Atom);

                messageBuilder.Add(0); // LayoutId = 0 for this content

                int duration;

                if (contentItem.Type == ContentTypes.Text)
                {
                    messageBuilder.Add(contentItem.Type).Add(contentItem.Value.EscapeNewLines());
                    legacyBuilder.Add(contentItem.Type).Add(contentItem.Value);

                    duration = contentItem.Duration > TimeSpan.Zero
                        ? (int)(contentItem.Duration.TotalMilliseconds / 100)
                        : GetContentItemDefaultDuration(contentItem);
                }
                else
                {
                    var (success, globalUri, _) = TryShareContent(contentItem);

                    if (!success || globalUri == null)
                    {
                        messageBuilder.Add(ContentTypes.Text).Add(string.Format(LO[nameof(R.MediaNotFound)], globalUri));
                        legacyBuilder.Add(ContentTypes.Text).Add(string.Format(LO[nameof(R.MediaNotFound)], globalUri));
                        duration = DefaultImageTime + _data.Settings.AppSettings.TimeSettings.TimeForMediaDelay * 10;
                    }
                    else
                    {
                        messageBuilder.Add(contentItem.Type).Add(globalUri);
                        legacyBuilder.Add(contentItem.Type == ContentTypes.Audio ? AtomTypes.Audio : contentItem.Type).Add(MessageParams.Atom_Uri).Add(globalUri);

                        if ((contentItem.Type == ContentTypes.Audio || contentItem.Type == ContentTypes.Video) && !registeredMediaPlay)
                        {
                            registeredMediaPlay = true;
                            _data.IsPlayingMedia = true;
                            _data.IsPlayingMediaPaused = false;

                            _data.InitialMediaContentCompletionCount = _data.HaveViewedAtom = _data.Viewers.Count
                                + _data.Players.Where(pa => pa.IsHuman && pa.IsConnected).Count()
                                + (_data.ShowMan.IsHuman && _data.ShowMan.IsConnected ? 1 : 0);
                        }

                        duration = contentItem.Duration > TimeSpan.Zero
                            ? (int)(contentItem.Duration.TotalMilliseconds / 100)
                            : GetContentItemDefaultDuration(contentItem);
                    }
                }

                contentListDuration += duration;

                _gameActions.SendMessage(legacyBuilder.ToString());
            }

            _gameActions.SendMessage(messageBuilder.ToString());

            contentTime = Math.Max(contentTime, contentListDuration);
        }

        _data.IsPartial = false;
        _data.AtomStart = DateTime.UtcNow;
        _data.AtomTime = contentTime;
        ScheduleExecution(Tasks.MoveNext, contentTime);
        _data.TimeThinking = 0.0;
    }

    private int GetContentItemDefaultDuration(ContentItem contentItem) => contentItem.Type switch
    {
        ContentTypes.Text => GetReadingDurationForTextLength(contentItem.Value.Length),
        ContentTypes.Image or ContentTypes.Html => DefaultImageTime + _data.Settings.AppSettings.TimeSettings.TimeForMediaDelay * 10,
        ContentTypes.Audio or ContentTypes.Video => DefaultAudioVideoTime,
        _ => 0,
    };
}

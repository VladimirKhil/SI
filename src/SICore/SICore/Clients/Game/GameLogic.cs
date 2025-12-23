using Notions;
using SICore.Clients;
using SICore.Clients.Game;
using SICore.Clients.Game.Plugins.Stakes;
using SICore.Clients.Other;
using SICore.Contracts;
using SICore.Extensions;
using SICore.Models;
using SICore.Results;
using SICore.Utils;
using SIData;
using SIEngine.Rules;
using SIPackages;
using SIPackages.Core;
using SIPackages.Models;
using SIUI.Model;
using System.Text;
using Utils.Timers;
using R = SICore.Properties.Resources;

namespace SICore;

// TODO: split this class into aspect classes

/// <summary>
/// Executes SIGame logic implemented as a state machine.
/// </summary>
public sealed class GameLogic : ITaskRunHandler<Tasks>, IDisposable
{
    private const string OfObjectPropertyFormat = "{0} {1}: {2}";

    private const int MaxAnswerLength = 350;

    private const int DefaultAudioVideoTime = 1200; // maximum audio/video duration (120 s)

    private const int DefaultMediaTime = 50;

    /// <summary>
    /// Maximum number of oversized media notifications.
    /// </summary>
    public const int MaxMediaNotifications = 15;

    /// <summary>
    /// Frequency of partial prints per second.
    /// </summary>
    private const double PartialPrintFrequencyPerSecond = 0.5;

    /// <summary>
    /// Execution completion;
    /// </summary>
    private Action? _completion;

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

    private readonly ILocalizer LO; // TODO: localization must be removed from SICore

    internal event Action? AutoGame;

    private readonly HistoryLog _tasksHistory = new();

    private TimeSettings TimeSettings => _state.Settings.AppSettings.TimeSettings;

    public SIEngine.GameEngine Engine { get; } // TODO: remove dependency on GameEngine

    public event Action<GameLogic, GameStages, string, int, int>? StageChanged;

    public event Action<string, int, int>? AdShown;

    internal void OnStageChanged(
        GameStages stage,
        string stageName,
        int progressCurrent = 0,
        int progressTotal = 0) => StageChanged?.Invoke(this, stage, stageName, progressCurrent, progressTotal);

    internal void OnAdShown(int adId) =>
        AdShown?.Invoke(LO.Culture.TwoLetterISOLanguageName, adId, _state.AllPersons.Values.Count(p => p.IsHuman));

    private readonly IFileShare _fileShare;
    private readonly TaskRunner<Tasks> _taskRunner;

    private StopReason _stopReason = StopReason.None;

    internal StopReason StopReason => _stopReason;

    private int _leftTime;

    internal TaskRunner<Tasks> Runner => _taskRunner;

    internal IPinHelper? PinHelper { get; }

    internal StakesPlugin Stakes { get; }

    private readonly GameData _state;

    public GameLogic(
        GameData state,
        GameActions actions,
        SIEngine.GameEngine engine,
        ILocalizer localizer,
        IFileShare fileShare,
        IPinHelper? pinHelper)
    {
        _state = state;
        _gameActions = actions;
        Engine = engine;
        LO = localizer;
        _fileShare = fileShare;
        _taskRunner = new(this);
        PinHelper = pinHelper;
        Stakes = new StakesPlugin(state);
    }

    internal void Run()
    {
        _state.PackageDoc = Engine.Document;

        _state.GameResultInfo.Name = _state.GameName;
        _state.GameResultInfo.Language = _state.Settings.AppSettings.Culture;
        _state.GameResultInfo.PackageName = Engine.PackageName;
        _state.GameResultInfo.PackageAuthors = Engine.Document.Package.Info.Authors.ToArray();
        _state.GameResultInfo.PackageAuthorsContacts = Engine.Document.Package.ContactUri;

        if (_state.Settings.IsAutomatic)
        {
            // The game should be started automatically
            ScheduleExecution(Tasks.AutoGame, Constants.AutomaticGameStartDuration);
            _state.TimerStartTime[2] = DateTime.UtcNow;

            _gameActions.SendMessageWithArgs(Messages.Timer, 2, MessageParams.Timer_Go, Constants.AutomaticGameStartDuration, -2);
        }
    }

    internal void OnFinalRoundSkip()
    {
        _gameActions.ShowmanReplic(LO[nameof(R.NobodyInFinal)]); // TODO: REMOVE (localized by MessageCode)
        _gameActions.ShowmanReplicNew(MessageCode.RoundSkippedNoPlayers);
        ScheduleExecution(Tasks.MoveNext, 15 + Random.Shared.Next(10), 1);
    }

    internal void OnContentScreenHtml(ContentItem contentItem)
    {
        _state.IsPartial = false;
        ShareMedia(contentItem);

        var contentTime = GetContentItemDuration(contentItem, _state.TimeSettings.Image * 10);

        _state.AtomTime = contentTime;
        _state.AtomStart = DateTime.UtcNow;

        ScheduleExecution(Tasks.MoveNext, contentTime);

        _state.TimeThinking = 0.0;
    }

    internal bool ProcessNextAppellationRequest(bool stop)
    {
        var (appellationSource, isAppellationForRightAnswer) =
            _state.QuestionPlay.Appellations[_state.QuestionPlay.AppellationIndex++];

        _state.AppellationCallerIndex = -1;
        _state.AppelaerIndex = -1;

        if (isAppellationForRightAnswer)
        {
            for (var i = 0; i < _state.Players.Count; i++)
            {
                if (_state.Players[i].Name == appellationSource)
                {
                    for (var j = 0; j < _state.QuestionHistory.Count; j++)
                    {
                        var index = _state.QuestionHistory[j].PlayerIndex;

                        if (index == i)
                        {
                            if (!_state.QuestionHistory[j].IsRight)
                            {
                                _state.AppelaerIndex = index;
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
            if (_state.Players.Count(p => p.IsConnected) <= 3)
            {
                // If there are 2 or 3 players, there are already 2 positive votes for the answer
                // from answered player and showman. And only 1 or 2 votes left.
                // So there is no chance to win a vote against the answer
                _gameActions.SendMessageToWithArgs(appellationSource, Messages.UserError, ErrorCode.AppellationFailedTooFewPlayers);
                return false;
            }

            for (var i = 0; i < _state.Players.Count; i++)
            {
                if (_state.Players[i].Name == appellationSource)
                {
                    _state.AppellationCallerIndex = i;
                    break;
                }
            }

            if (_state.AppellationCallerIndex == -1)
            {
                // Only players can appellate
                return false;
            }

            // Last person has right answer and is responsible for appellation
            var count = _state.QuestionHistory.Count;

            if (count > 0 && _state.QuestionHistory[count - 1].IsRight)
            {
                _state.AppelaerIndex = _state.QuestionHistory[count - 1].PlayerIndex;
            }
        }

        if (_state.AppelaerIndex != -1)
        {
            // Appellation started
            if (stop)
            {
                _state.QuestionPlay.AppellationState = AppellationState.Collecting; // To query other appellations
                Stop(StopReason.Appellation);
            }
            
            return true;
        }

        return false;
    }

    private void PostprocessQuestion(int taskTime = 1)
    {
        _tasksHistory.AddLogEntry("Engine_QuestionPostInfo: Appellation activated");

        _state.QuestionPlay.AppellationState = _state.Settings.AppSettings.UseApellations ? AppellationState.Processing : AppellationState.None;
        _state.IsPlayingMedia = false;

        _state.InformStages &= ~(InformStages.Question | InformStages.Layout | InformStages.ContentShape);

        ScheduleExecution(Tasks.QuestionPostInfo, taskTime, force: true);

        if (GetTurnSwitchingStrategy() == TurnSwitchingStrategy.Sequentially)
        {
            _state.ChooserIndex = (_state.ChooserIndex + 1) % _state.Players.Count;
            _gameActions.SendMessageWithArgs(Messages.SetChooser, _state.ChooserIndex);
        }

        if (_state.QuestionPlay.AppellationState != AppellationState.None && _state.QuestionPlay.Appellations.Any())
        {
            ProcessNextAppellationRequest(true);
        }
    }

    internal void OnPackage(Package package)
    {
        _state.Package = package;

        _state.Rounds = _state.Package.Rounds
            .Select((round, index) => new RoundInfo { Index = index, Name = round.Name })
            .ToArray();

        _gameActions.SendMessage(string.Join(Message.ArgsSeparator, Messages.PackageId, package.ID)); // TODO: REMOVE (replaced by PACKAGE_INFO message)
        _gameActions.InformRoundsNames();

        _state.InformStages |= InformStages.RoundNames;

        OnPackage(package, 1);
    }

    internal void OnGameThemes(IEnumerable<string> gameThemes)
    {
        var msg = new MessageBuilder(Messages.GameThemes).AddRange(gameThemes);
        _gameActions.SendVisualMessage(msg);
        _ = gameThemes.TryGetNonEnumeratedCount(out var count);
        ScheduleExecution(Tasks.MoveNext, Math.Max(40, 10 + 10 * count));
    }

    internal void OnRoundStart(Round round, QuestionSelectionStrategyType strategyType)
    {
        _state.Round = round;
        _state.CanMarkQuestion = false;
        _state.AnswererIndex = -1;
        _state.QuestionPlay.Clear();
        _state.ThemeDeleters = null;
        _state.RoundStrategy = strategyType;

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
        _state.StakeStep = (int)Math.Pow(10, Math.Floor(Math.Log10(_minRoundPrice))); // Maximum power of 10 <= _minRoundPrice
    }

    internal void InitThemes(IEnumerable<Theme> themes, bool willPlayAllThemes, bool isFirstPlay, ThemesPlayMode playMode)
    {
        _state.TInfo.RoundInfo.Clear();

        foreach (var theme in themes)
        {
            _state.TInfo.RoundInfo.Add(new ThemeInfo { Name = theme.Name });
        }

        if (willPlayAllThemes)
        {
            var themesReplic = isFirstPlay
                ? $"{GetRandomString(LO[nameof(R.RoundThemes)])}. {LO[nameof(R.WeWillPlayAllOfThem)]}"
                : LO[nameof(R.LetsPlayNextTheme)];

            _gameActions.ShowmanReplic(themesReplic);
        }

        _state.TableInformStageLock.WithLock(() =>
        {
            _gameActions.InformRoundThemesNames(playMode: playMode);
            _state.ThemeComments = themes.Select(theme => theme.Info.Comments.Text).ToArray();
            _state.InformStages |= InformStages.RoundThemesNames;
            _state.ThemesPlayMode = playMode;
            _state.LastVisualMessage = null;
        },
        5000);
    }

    /// <summary>
    /// Gets count of questions left to play in current round.
    /// </summary>
    internal int GetRoundActiveQuestionsCount() => _state.TInfo.RoundInfo.Sum(theme => theme.Questions.Count(QuestionHelper.IsActive));

    internal void OnQuestion(Question question)
    {
        _state.Question = question;

        _gameActions.ShowmanReplic($"{_state.Theme?.Name}, {question.Price}");
        _gameActions.SendVisualMessageWithArgs(Messages.Question, question.Price);

        InitQuestionState(_state.Question);
        ProceedToThemeAndQuestion();
    }

    internal void OnQuestionSelected(int themeIndex, int questionIndex)
    {
        if (_state.Round == null)
        {
            throw new InvalidOperationException("_data.Round == null");
        }

        _gameActions.SendMessageWithArgs(Messages.Choice, themeIndex, questionIndex);
        _state.InformStages |= InformStages.Question;

        _state.Theme = _state.Round.Themes[themeIndex];
        _state.Question = _state.Theme.Questions[questionIndex];

        _state.TInfo.RoundInfo[themeIndex].Questions[questionIndex].Price = Question.InvalidPrice;

        InitQuestionState(_state.Question);
        ProceedToThemeAndQuestion(20);
    }

    private void ProceedToThemeAndQuestion(int delay = 10, bool force = true) => ScheduleExecution(Tasks.QuestionStartInfo, delay, 1, force);

    private void InitQuestionState(Question question)
    {
        // TODO: move all to question play state
        _state.QuestionHistory.Clear();
        _state.PendingAnswererIndex = -1;
        _state.AnswererPressDuration = -1;
        _state.PendingAnswererIndicies.Clear();
        _state.IsQuestionAskPlaying = true;
        _state.IsPlayingMedia = false;
        _state.IsPlayingMediaPaused = false;
        _state.CurPriceRight = _state.CurPriceWrong = question.Price;
        _state.Order = Array.Empty<int>();
        _state.OrderIndex = -1;
        _state.AnswererIndex = -1;
        _state.CanMarkQuestion = false;
        _state.QuestionPlay.Clear();

        var key = _state.QuestionPlay.QuestionKey = $"{Engine.RoundIndex}:{_state.ThemeIndex}:{_state.QuestionIndex}";
        var humanPlayerCount = _state.Players.Where(pa => pa.IsHuman && pa.IsConnected).Count();
        _state.GameResultInfo.IncrementQuestionSeenCount(key, humanPlayerCount);
    }

    internal void OnContentScreenText(string text, bool waitForFinish, TimeSpan duration)
    {
        var contentTime = duration > TimeSpan.Zero ? (int)(duration.TotalMilliseconds / 100) : GetReadingDurationForTextLength(text.Length);

        if (_state.QuestionPlay.IsAnswer)
        {
            contentTime += _state.TimeSettings.RightAnswer * 10;
        }

        _state.AtomTime = contentTime;
        _state.AtomStart = DateTime.UtcNow;
        _state.UseBackgroundAudio = !waitForFinish;

        _state.IsPartial = waitForFinish && IsPartial();

        if (_state.IsPartial)
        {
            _state.Text = text;
            _gameActions.SendContentShape();
            _state.InformStages |= InformStages.ContentShape;
            _state.InitialPartialTextLength = 0;
            _state.PartialIterationCounter = 0;
            _state.TextLength = 0;
            ScheduleExecution(Tasks.PrintPartial, 1);
            return;
        }

        var message = string.Join(Message.ArgsSeparator, Messages.Content, ContentPlacements.Screen, 0, ContentTypes.Text, text.EscapeNewLines());

        _state.ComplexVisualState ??= new IReadOnlyList<string>[1];
        _state.ComplexVisualState[0] = new string[] { message };

        _gameActions.SendMessage(message);
        _gameActions.SystemReplic(text); // TODO: REMOVE: replaced by CONTENT message

        var nextTime = !waitForFinish ? 1 : contentTime;

        ScheduleExecution(Tasks.MoveNext, nextTime);

        _state.TimeThinking = 0.0;
    }

    /// <summary>
    /// Should the question be displayed partially.
    /// </summary>
    private bool IsPartial() =>
        _state.QuestionPlay.UseButtons
            && !_state.Settings.AppSettings.FalseStart
            && _state.Settings.AppSettings.PartialText
            && !_state.QuestionPlay.IsAnswer;

    internal void OnContentReplicText(string text, bool waitForFinish, TimeSpan duration)
    {
        _state.IsPartial = false;
        // There is no need to send content for now, as we can send replic directly
        //_gameActions.SendMessageWithArgs(Messages.Content, ContentPlacements.Replic, 0, ContentTypes.Text, text.EscapeNewLines());
        _gameActions.ShowmanReplic(text);

        var atomTime = !waitForFinish ? 1 : (duration > TimeSpan.Zero ? (int)(duration.TotalMilliseconds / 100) : GetReadingDurationForTextLength(text.Length));

        _state.AtomTime = atomTime;
        _state.AtomStart = DateTime.UtcNow;

        ScheduleExecution(Tasks.MoveNext, atomTime);

        _state.TimeThinking = 0.0;
        _state.UseBackgroundAudio = !waitForFinish;
    }

    private (bool success, string? globalUri, string? localUri, string? error) TryShareContent(ContentItem contentItem)
    {
        if (_state.PackageDoc == null || _state.Package == null)
        {
            throw new InvalidOperationException("_data.Package == null; game not running");
        }

        if (!contentItem.IsRef) // External link
        {
            if (_state.Package.HasQualityControl)
            {
                return (false, null, null, LO[nameof(R.ExternalLinksForbidden)]);
            }

            var link = contentItem.Value;

            if (Uri.TryCreate(link, UriKind.Absolute, out _))
            {
                return (true, link, null, null);
            }

            // There is no file in the package and it's name is not a valid absolute uri.
            // So, considering that the file is missing

            return (false, link, null, null);
        }

        if (_state.Package.HasQualityControl)
        {
            var fileExtension = Path.GetExtension(contentItem.Value)?.ToLowerInvariant();

            if (Quality.FileExtensions.TryGetValue(contentItem.Type, out var allowedExtensions) && !allowedExtensions.Contains(fileExtension))
            {
                return (false, null, null, string.Format(LO[nameof(R.InvalidFileExtension)], contentItem.Value, fileExtension));
            }
        }
        
        var contentType = contentItem.Type;
        var mediaCategory = CollectionNames.TryGetCollectionName(contentType) ?? contentType;
        var media = _state.PackageDoc.TryGetMedia(contentItem);

        if (!media.HasValue || media.Value.Uri == null)
        {
            return (false, $"{mediaCategory}/{contentItem.Value}", null, null);
        }

        var fullUri = media.Value.Uri;
        var fileLength = media.Value.StreamLength;

        if (fileLength.HasValue)
        {
            var (success, error) = CheckFileLength(contentType, fileLength.Value);
            
            if (!success)
            {
                return (false, null, null, error);
            }
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

        return (true, globalUri, localUri, null);
    }

    private string? ShareMedia(ContentItem contentItem)
    {
        try
        {
            var (success, globalUri, _, error) = TryShareContent(contentItem);

            if (!success || globalUri == null)
            {
                var errorText = error ?? string.Format(LO[nameof(R.MediaNotFound)], globalUri);
                var message = string.Join(Message.ArgsSeparator, Messages.Content, ContentPlacements.Screen, 0, ContentTypes.Text, errorText);
                _gameActions.SendMessage(message);

                _state.ComplexVisualState ??= new IReadOnlyList<string>[1];
                _state.ComplexVisualState[0] = new string[] { message };

                return null;
            }

            _gameActions.SendVisualMessageWithArgs(
                Messages.Content,
                contentItem.Placement,
                0,
                contentItem.Type,
                globalUri);

            return globalUri;
        }
        catch (Exception exc)
        {
            _state.Host.SendError(exc, true);
            return null;
        }
    }

    private (bool success, string? error) CheckFileLength(string contentType, long fileLength)
    {
        int? maxRecommendedFileLength = contentType == ContentTypes.Image ? _state.Host.MaxImageSizeKb
            : (contentType == ContentTypes.Audio ? _state.Host.MaxAudioSizeKb
            : (contentType == ContentTypes.Video ? _state.Host.MaxVideoSizeKb
            : null));

        if (!maxRecommendedFileLength.HasValue || fileLength <= (long)maxRecommendedFileLength * 1024)
        {
            return (true, null);
        }

        var fileLocation = $"{_state.Theme?.Name}, {_state.Question?.Price}";

        if (_state.Package.HasQualityControl)
        {
            var error = string.Format(LO[nameof(R.OversizedFileForbidden)], R.File, fileLocation, maxRecommendedFileLength);
            return (false, error);
        }

        // Notify users that the media file is too large and could be downloaded slowly
        var errorMessage = string.Format(LO[nameof(R.OversizedFile)], R.File, fileLocation, maxRecommendedFileLength);
        _gameActions.SendMessageWithArgs(Messages.Replic, ReplicCodes.Special.ToString(), errorMessage); // TODO: REMOVE: replaced by USER_ERROR message
        _gameActions.SendMessageWithArgs(Messages.UserError, ErrorCode.OversizedFile, contentType, maxRecommendedFileLength);

        if (_state.OversizedMediaNotificationsCount < MaxMediaNotifications)
        {
            _state.OversizedMediaNotificationsCount++;

            // Show message on table
            _gameActions.SendMessageWithArgs(Messages.Atom_Hint, errorMessage);
        }

        return (true, null);
    }

    internal void OnContentScreenImage(ContentItem contentItem)
    {
        _state.IsPartial = false;
        ShareMedia(contentItem);

        var appSettings = _state.Settings.AppSettings;
        // TODO: provide this flag to client as part of the CONTENT message
        var partialImage = appSettings.PartialImages && !appSettings.FalseStart && _state.QuestionPlay.UseButtons && !_state.QuestionPlay.IsAnswer;

        var renderTime = partialImage ? Math.Max(0, appSettings.TimeSettings.PartialImageTime * 10) : 0;
        
        var waitTime = GetContentItemDuration(contentItem, _state.TimeSettings.Image * 10);

        var contentTime = renderTime + waitTime;

        _state.AtomTime = contentTime;
        _state.AtomStart = DateTime.UtcNow;

        ScheduleExecution(Tasks.MoveNext, contentTime);

        _state.TimeThinking = 0.0;
        _state.UseBackgroundAudio = !contentItem.WaitForFinish;
    }

    private static int GetContentItemDuration(ContentItem contentItem, int defaultValue) =>
        contentItem.WaitForFinish
            ? (contentItem.Duration > TimeSpan.Zero ? (int)(contentItem.Duration.TotalMilliseconds / 100) : defaultValue)
            : 1;

    private void ClearMediaContent() => _state.QuestionPlay.MediaContentCompletions.Clear();

    internal void OnContentBackgroundAudio(ContentItem contentItem)
    {
        _state.IsPartial = false;
        var globalUri = ShareMedia(contentItem);

        var defaultTime = DefaultMediaTime + TimeSettings.TimeForMediaDelay * 10;

        if (globalUri != null)
        {
            _state.QuestionPlay.MediaContentCompletions[(contentItem.Type, globalUri)] = new Completion(_state.ActiveHumanCount);
            _completion = ClearMediaContent;
            defaultTime = DefaultAudioVideoTime;
        }

        var atomTime = GetContentItemDuration(contentItem, defaultTime);

        _state.AtomTime = atomTime;
        _state.AtomStart = DateTime.UtcNow;
        _state.IsPlayingMedia = true;
        _state.IsPlayingMediaPaused = false;

        ScheduleExecution(Tasks.MoveNext, atomTime);

        _state.TimeThinking = 0.0;
    }

    internal void OnContentScreenVideo(ContentItem contentItem)
    {
        _state.IsPartial = false;
        var globalUri = ShareMedia(contentItem);

        var defaultTime = DefaultMediaTime + TimeSettings.TimeForMediaDelay * 10;

        if (globalUri != null)
        {
            _state.QuestionPlay.MediaContentCompletions[(contentItem.Type, globalUri)] = new Completion(_state.ActiveHumanCount);
            _completion = ClearMediaContent;
            defaultTime = DefaultAudioVideoTime;
        }

        int atomTime = GetContentItemDuration(contentItem, defaultTime);

        _state.AtomTime = atomTime;
        _state.AtomStart = DateTime.UtcNow;
        _state.IsPlayingMedia = true;
        _state.IsPlayingMediaPaused = false;

        ScheduleExecution(Tasks.MoveNext, atomTime);

        _state.TimeThinking = 0.0;
    }

    // Let's add a random offset so it will be difficult to press the button in advance (before the frame appears)
    internal void AskToPress() => ScheduleExecution(Tasks.AskToTry, 1 + (_state.Settings.AppSettings.Managed ? 0 : Random.Shared.Next(10)), force: true);

    internal void AskDirectAnswer()
    {
        if (HaveMultipleAnswerers())
        {
            _gameActions.SendMessageWithArgs(Messages.FinalThink, _state.TimeSettings.HiddenAnswering);
        }

        ScheduleExecution(Tasks.AskAnswer, 1, force: true);
    }

    internal void OnRoundEnded()
    {
        _state.IsQuestionAskPlaying = false;
        _state.IsPlayingMedia = false;

        _gameActions.InformSums();
        _gameActions.SendMessage(Messages.Stop); // Timers STOP

        _state.IsThinking = false;

        _state.IsWaiting = false;
        _state.Decision = DecisionType.None;

        _state.InformStages &= ~(InformStages.RoundContent | 
            InformStages.RoundThemesNames | 
            InformStages.RoundThemesComments | 
            InformStages.Table | 
            InformStages.Theme | 
            InformStages.Question |
            InformStages.Layout |
            InformStages.ContentShape);

        // This is quite ugly bit here but as we interrupt normal flow we need to cut continuation
        // (or we could replace it with a normal move)
        ClearContinuation();

        if (_state.TInfo.Pause)
        {
            OnPauseCore(false);
        }

        _taskRunner.ClearOldTasks();

        PlanExecution(Tasks.MoveNext, 40);
    }

    internal void OnPauseCore(bool isPauseEnabled)
    {
        // Game host or showman requested a game pause

        if (isPauseEnabled)
        {
            if (_state.TInfo.Pause)
            {
                return;
            }

            if (Stop(StopReason.Pause))
            {
                _state.TInfo.Pause = true;
                AddHistory("Pause activated");
            }

            return;
        }

        if (StopReason == StopReason.Pause)
        {
            // We are currently moving into pause mode. Resuming
            _state.TInfo.Pause = false;
            AddHistory("Immediate pause resume");
            CancelStop();
            return;
        }

        if (!_state.TInfo.Pause)
        {
            return;
        }

        _state.TInfo.Pause = false;

        var pauseDuration = DateTime.UtcNow.Subtract(_state.PauseStartTime);

        var times = new int[Constants.TimersCount];

        for (var i = 0; i < Constants.TimersCount; i++)
        {
            times[i] = (int)(_state.PauseStartTime.Subtract(_state.TimerStartTime[i]).TotalMilliseconds / 100);
            _state.TimerStartTime[i] = _state.TimerStartTime[i].Add(pauseDuration);
        }

        if (_state.IsPlayingMediaPaused)
        {
            _state.IsPlayingMediaPaused = false;
            _state.IsPlayingMedia = true;
        }

        if (_state.IsThinkingPaused)
        {
            _state.IsThinkingPaused = false;
            _state.IsThinking = true;
        }

        AddHistory($"Pause resumed ({Runner.PrintOldTasks()} {StopReason})");

        try
        {
            var maxPressingTime = _state.TimeSettings.ButtonPressing * 10;
            times[1] = maxPressingTime - ResumeExecution();
        }
        catch (Exception exc)
        {
            throw new Exception($"Resume execution error: {PrintHistory()}", exc);
        }

        if (StopReason == StopReason.Decision)
        {
            RescheduleTask(); // Decision could be ready
        }

        _gameActions.SendMessageWithArgs(Messages.Pause, isPauseEnabled ? '+' : '-', times[0], times[1], times[2]);
    }

    internal void OnRoundEmpty()
    {
        _gameActions.ShowmanReplic(GetRandomString(LO[nameof(R.AllQuestions)])); // TODO: REMOVE+
        _gameActions.SendMessage(Messages.RoundEnd, "empty");
    }

    internal void OnRoundTimeout()
    {
        _gameActions.SendMessage(Messages.Timeout); // TODO: REMOVE: replaced by ROUND_END TIMEOUT message
        _gameActions.ShowmanReplic(GetRandomString(LO[nameof(R.AllTime)])); // TODO: REMOVE+

        _gameActions.SendMessage(Messages.RoundEnd, "timeout");
    }

    internal void OnRoundEndedManually()
    {
        _gameActions.SendMessage(Messages.RoundEnd, "manual");
    }

    internal void OnThemeDeleted(int themeIndex)
    {
        if (themeIndex < 0 || themeIndex >= _state.TInfo.RoundInfo.Count)
        {
            var errorMessage = new StringBuilder(themeIndex.ToString())
                .Append(' ')
                .Append(string.Join("|", _state.TInfo.RoundInfo.Select(t => $"({t.Name != QuestionHelper.InvalidThemeName} {t.Questions.Count})")))
                .Append(' ')
                .Append(_state.ThemeIndexToDelete);

            throw new ArgumentException(errorMessage.ToString(), nameof(themeIndex));
        }

        if (_state.ThemeDeleters == null || _state.ThemeDeleters.IsEmpty())
        {
            throw new InvalidOperationException("_data.ThemeDeleters are undefined");
        }

        _state.TInfo.RoundInfo[themeIndex].Name = QuestionHelper.InvalidThemeName;

        _gameActions.SendMessageWithArgs(Messages.Out, themeIndex);

        var playerIndex = _state.ThemeDeleters.Current.PlayerIndex;
        var themeName = _state.TInfo.RoundInfo[themeIndex].Name;

        ScheduleExecution(Tasks.MoveNext, 10);
    }

    internal void AnnounceFinalTheme(Question question)
    {
        _gameActions.ShowmanReplic($"{GetRandomString(LO[nameof(R.PlayTheme)])} {_state.Theme.Name}");
        _gameActions.SendMessageWithArgs(Messages.QuestionCaption, _state.Theme.Name);
        _gameActions.SendThemeInfo(overridenQuestionCount: 1);
        
        InitQuestionState(question);
        ProceedToThemeAndQuestion(force: false);
    }

    internal void OnEndGame()
    {
        // Clearing the table
        _gameActions.SendMessage(Messages.Stop);

        FillReport();
        ScheduleExecution(Tasks.Winner, 15 + Random.Shared.Next(10), force: true);
    }

    public void Dispose() =>
        _state.TaskLock.WithLock(
            () =>
            {
                _taskRunner.Dispose();

                if (_state.Stage != GameStage.Before)
                {
                    SendReport();
                }

                Engine.Dispose();
            },
            5000);

    private void SendReport()
    {
        if (_state.ReportSent)
        {
            return;
        }

        FillReport();

        var reviewers = _state.GameResultInfo.Reviews.Keys;

        foreach (var reviewer in reviewers)
        {
            if (string.IsNullOrEmpty(_state.GameResultInfo.Reviews[reviewer]))
            {
                _state.GameResultInfo.Reviews.Remove(reviewer);
            }
        }

        _state.Host.SaveReport(_state.GameResultInfo);
        _state.ReportSent = true;
    }

    private void FillReport()
    {
        if (_state.GameResultInfo.Duration > TimeSpan.Zero)
        {
            return;
        }

        for (var i = 0; i < _state.Players.Count; i++)
        {
            _state.GameResultInfo.Results[_state.Players[i].Name] = _state.Players[i].Sum;
        }

        _state.GameResultInfo.Duration = DateTimeOffset.UtcNow.Subtract(_state.GameResultInfo.StartTime);
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
            _state.IsWaiting = false; // Preventing double message processing
        }
        else if (reason == StopReason.Appellation && _state.IsWaiting)
        {
            StopWaiting();
        }

        _stopReason = reason;

        if (reason == StopReason.Pause || reason == StopReason.Appellation)
        {
            _leftTime = (int)((_taskRunner.FinishingTime - DateTime.UtcNow).TotalMilliseconds / 100);
        }

        _taskRunner.RescheduleTask();

        return true;
    }

    internal void RescheduleTask(int taskTime = 10)
    {
        _tasksHistory.AddLogEntry(nameof(RescheduleTask));
        _taskRunner.RescheduleTask(taskTime);
    }

    internal void CancelStop() => _stopReason = StopReason.None;

    /// <summary>
    /// Processes decision been made.
    /// </summary>
    private bool OnDecision() => _state.Decision switch
    {
        DecisionType.StarterChoosing => OnDecisionStarterChoosing(),
        DecisionType.QuestionSelection => OnDecisionQuestionSelection(),
        DecisionType.Answering => OnDecisionAnswering(),
        DecisionType.AnswerValidating => OnDecisionAnswerValidating(),
        DecisionType.QuestionAnswererSelection => OnDecisionQuestionAnswererSelection(),
        DecisionType.QuestionPriceSelection => OnDecisionQuestionPriceSelection(),
        DecisionType.NextPersonStakeMaking => OnDecisionNextPersonStakeMaking(),
        DecisionType.StakeMaking => OnDecisionStakeMaking(),
        DecisionType.NextPersonFinalThemeDeleting => OnDecisionNextPersonFinalThemeDeleting(),
        DecisionType.ThemeDeleting => OnDecisionThemeDeleting(),
        DecisionType.HiddenStakeMaking => OnDecisionHiddenStakeMaking(),
        DecisionType.Appellation => OnDecisionAppellation(),
        _ => false,
    };

    private bool OnDecisionQuestionSelection()
    {
        if (_state.ThemeIndex == -1
            || _state.ThemeIndex >= _state.TInfo.RoundInfo.Count
            || _state.QuestionIndex == -1
            || _state.QuestionIndex >= _state.TInfo.RoundInfo[_state.ThemeIndex].Questions.Count
            || !_state.TInfo.RoundInfo[_state.ThemeIndex].Questions[_state.QuestionIndex].IsActive())
        {
            return false;
        }

        StopWaiting();
        ScheduleExecution(Tasks.MoveNext, 1, force: true);
        return true;
    }

    private bool OnDecisionQuestionAnswererSelection()
    {
        if (_state.Answerer == null)
        {
            return false;
        }

        StopWaiting();

        var s = _state.ChooserIndex == _state.AnswererIndex ? LO[nameof(R.ToMyself)] : _state.Answerer.Name;

        _state.ChooserIndex = _state.AnswererIndex;
        _gameActions.SendMessageWithArgs(Messages.SetChooser, _state.ChooserIndex, "+");
        ScheduleExecution(Tasks.MoveNext, 10);
        return true;
    }

    private bool OnDecisionQuestionPriceSelection()
    {
        if (_state.CurPriceRight == -1)
        {
            return false;
        }

        StopWaiting();

        _state.CurPriceWrong = _state.CurPriceRight;

        _gameActions.SendMessageWithArgs(Messages.PersonStake, _state.AnswererIndex, 1, _state.CurPriceRight);
        _gameActions.SendMessageWithArgs(Messages.SetChooser, _state.ChooserIndex, "+");

        ScheduleExecution(Tasks.MoveNext, 20);

        return true;
    }

    private bool OnDecisionHiddenStakeMaking()
    {
        if (_state.HiddenStakerCount != 0)
        {
            return false;
        }

        StopWaiting();
        ProceedToHiddenStakesQuestion();

        return true;
    }

    private void ProceedToHiddenStakesQuestion()
    {
        _gameActions.ShowmanReplic(LO[nameof(R.ThankYou)]);
        ScheduleExecution(Tasks.MoveNext, 20);
    }

    private bool OnDecisionNextPersonFinalThemeDeleting()
    {
        if (_state.ThemeDeleters == null || _state.ThemeDeleters.Current.PlayerIndex == -1)
        {
            return false;
        }

        StopWaiting();
        _gameActions.ShowmanReplic($"{LO[nameof(R.ThemeDeletes)]} {_state.Players[_state.ThemeDeleters.Current.PlayerIndex].Name}");
        _state.ThemeDeleters.MoveBack();
        ScheduleExecution(Tasks.AskToDelete, 1);
        return true;
    }

    private bool OnDecisionAppellation()
    {
        StopWaiting();
        ScheduleExecution(Tasks.CheckAppellation, 10);
        return true;
    }

    private bool OnDecisionThemeDeleting()
    {
        if (_state.ThemeIndexToDelete == -1)
        {
            return false;
        }

        StopWaiting();
        ScheduleExecution(Tasks.MoveNext, 1, force: true);
        return true;
    }

    private bool OnDecisionStakeMaking()
    {
        if (!_state.StakeType.HasValue)
        {
            return false;
        }

        StopWaiting();

        if (_state.OrderIndex == -1)
        {
            throw new ArgumentException($"{nameof(_state.OrderIndex)} == -1! {_state.OrderHistory}", nameof(_state.OrderIndex));
        }

        var playerIndex = _state.Order[_state.OrderIndex];

        if (playerIndex < 0 || playerIndex >= _state.Players.Count)
        {
            throw new ArgumentException($"{nameof(playerIndex)} == ${playerIndex} but it must be in [0; ${_state.Players.Count - 1}]! ${_state.OrderHistory}", nameof(playerIndex));
        }

        var stakeMaking = string.Join(",", _state.Players.Select(p => p.StakeMaking));
        var stakeSum = _state.StakeType == StakeMode.Sum ? _state.StakeSum.ToString() : "";
        _state.OrderHistory.Append($"Stake received: {playerIndex} {_state.StakeType.Value} {stakeSum} {stakeMaking}").AppendLine();

        if (_state.StakeType == StakeMode.Nominal)
        {
            _state.Stake = _state.CurPriceRight;
            _state.Stakes.StakerIndex = playerIndex;
        }
        else if (_state.StakeType == StakeMode.Sum)
        {
            _state.Stake = _state.StakeSum;
            _state.Stakes.StakerIndex = playerIndex;
        }
        else if (_state.StakeType == StakeMode.Pass)
        {
            _state.Players[playerIndex].StakeMaking = false;
            var passMsg = new MessageBuilder(Messages.PlayerState, PlayerState.Pass, playerIndex);
            _gameActions.SendMessage(passMsg.ToString());
        }
        else
        {
            _state.Stake = _state.Players[playerIndex].Sum;
            _state.Stakes.StakerIndex = playerIndex;
            _state.AllIn = true;
        }

        var printedStakeType = _state.StakeType == StakeMode.Nominal ? StakeMode.Sum : _state.StakeType;

        var stakeMessage = new MessageBuilder(Messages.PersonStake, playerIndex, (int)printedStakeType);

        if (printedStakeType == StakeMode.Sum)
        {
            stakeMessage.Add(_state.Stake);
        }

        _gameActions.SendMessage(stakeMessage.Build());

        if (_state.StakeType != StakeMode.Pass)
        {
            for (var i = 0; i < _state.Players.Count; i++)
            {
                var player = _state.Players[i];

                if (i != _state.Stakes.StakerIndex && player.StakeMaking && player.Sum <= _state.Stake)
                {
                    player.StakeMaking = false;
                    _gameActions.SendMessageWithArgs(Messages.PersonStake, i, 2);
                }
            }
        }

        var stakeMaking2 = string.Join(",", _state.Players.Select(p => p.StakeMaking));
        _state.OrderHistory.Append($"Stake making updated: {stakeMaking2}").AppendLine();

        if (TryDetectStakesWinner())
        {
            return true;
        }

        var stakerCount = _state.Players.Count(p => p.StakeMaking);

        if (stakerCount == 0)
        {
            _tasksHistory.AddLogEntry("Skipping question");
            _state.SkipQuestion?.Invoke();
            ScheduleExecution(Tasks.MoveNext, 10);

            return true;
        }

        ScheduleExecution(Tasks.AskStake, 5);
        return true;
    }

    private bool OnDecisionAnswerValidating()
    {
        if (!_state.ShowmanDecision)
        {
            return false;
        }

        if (_state.Answerer == null)
        {
            throw new Exception("_data.Answerer == null");
        }

        StopWaiting();

        int updateSum;
        var multipleAnswerers = HaveMultipleAnswerers();

        if (_state.Answerer.AnswerIsRight)
        {
            var showmanReplic = _state.QuestionPlay.UseButtons ? nameof(R.Right) : nameof(R.Bravo);
            var s = new StringBuilder(GetRandomString(LO[showmanReplic]));

            var canonicalAnswer = _state.Question?.Right.FirstOrDefault();
            var isAnswerCanonical = canonicalAnswer != null && (_state.Answerer.Answer ?? "").Simplify().Contains(canonicalAnswer.Simplify());

            if (canonicalAnswer != null && !isAnswerCanonical)
            {
                _state.GameResultInfo.AcceptedAnswers.Add(new QuestionReport
                {
                    ThemeName = _state.Theme.Name,
                    QuestionText = _state.Question?.GetText(),
                    ReportText = _state.Answerer.Answer
                });
            }

            if (_state.Answerer.IsHuman)
            {
                _state.GameResultInfo.IncrementQuestionCorrectCount(_state.QuestionPlay.QuestionKey);
            }

            if (!_state.QuestionPlay.HiddenStakes)
            {
                var outcome = _state.CurPriceRight;
                updateSum = (int)(outcome * _state.Answerer.AnswerValidationFactor);

                s.AppendFormat($" (+{outcome.ToString().FormatNumber()}{PrintRightFactor(_state.Answerer.AnswerValidationFactor)})");

                _gameActions.ShowmanReplic(s.ToString());
                _gameActions.SendMessageWithArgs(Messages.Person, '+', _state.AnswererIndex, updateSum);
                AddRightSum(_state.Answerer, updateSum);
                _gameActions.InformSums();

                if (multipleAnswerers)
                {
                    ScheduleExecution(Tasks.Announce, 15);
                }
                else
                {
                    if (GetTurnSwitchingStrategy() == TurnSwitchingStrategy.ByRightAnswerOnButton &&
                        _state.QuestionPlay.UseButtons &&
                        _state.ChooserIndex != _state.AnswererIndex)
                    {
                        _state.ChooserIndex = _state.AnswererIndex;
                        _gameActions.SendMessageWithArgs(Messages.SetChooser, _state.ChooserIndex);
                    }

                    // TODO: many of these lines are redundand in Special questions
                    _state.IsQuestionAskPlaying = false;

                    _state.IsThinking = false;
                    _gameActions.SendMessageWithArgs(Messages.Timer, 1, MessageParams.Timer_Stop);

                    MoveToAnswer(); // Question is answered correctly
                    ScheduleExecution(Tasks.MoveNext, 1, force: true);
                }
            }
            else
            {
                _gameActions.ShowmanReplic(s.ToString());
                _state.PlayerIsRight = true;
                updateSum = _state.Answerer.PersonalStake;
                ScheduleExecution(Tasks.AnnounceStake, 15);
            }
        }
        else
        {
            var s = new StringBuilder();

            if (_state.Answerer.Answer != LO[nameof(R.IDontKnow)])
            {
                s.Append(GetRandomString(LO[nameof(R.Wrong)])).Append(' ');
            }

            var outcome = _state.CurPriceWrong;

            if (_state.QuestionPlay.AnswerOptions != null && _state.Answerer.Answer != null)
            {
                var answerIndex = Array.FindIndex(_state.QuestionPlay.AnswerOptions, o => o.Label == _state.Answerer.Answer);

                if (answerIndex > -1)
                {
                    _gameActions.SendMessageWithArgs(Messages.ContentState, ContentPlacements.Screen, answerIndex + 1, ItemState.Wrong);
                }

                if (!HaveMultipleAnswerers())
                {
                    _state.QuestionPlay.UsedAnswerOptions.Add(_state.Answerer.Answer);
                }
            }

            if (_state.Answerer.IsHuman)
            {
                _state.GameResultInfo.IncrementQuestionWrongCount(_state.QuestionPlay.QuestionKey);
            }

            if (!_state.QuestionPlay.HiddenStakes)
            {
                s.AppendFormat($"(-{outcome.ToString().FormatNumber()}{PrintRightFactor(_state.Answerer.AnswerValidationFactor)})");                
                _gameActions.ShowmanReplic(s.ToString());

                if (_state.Answerer.AnswerValidationFactor == 0)
                {
                    _gameActions.SendMessageWithArgs(Messages.Pass, _state.AnswererIndex);
                    updateSum = -1;
                }
                else
                {
                    updateSum = (int)(outcome * _state.Answerer.AnswerValidationFactor);
                    _gameActions.SendMessageWithArgs(Messages.Person, '-', _state.AnswererIndex, updateSum);
                    SubtractWrongSum(_state.Answerer, updateSum);
                    _gameActions.InformSums();

                    if (_state.Answerer.IsHuman)
                    {
                        _state.GameResultInfo.RejectedAnswers.Add(new QuestionReport
                        {
                            ThemeName = _state.Theme.Name,
                            QuestionText = _state.Question?.GetText(),
                            ReportText = _state.Answerer.Answer
                        });
                    }
                }

                if (multipleAnswerers)
                {
                    ScheduleExecution(Tasks.Announce, 15);
                }
                else
                {
                    _state.Answerer.CanPress = false;
                    ScheduleExecution(Tasks.ContinueQuestion, 1);
                }
            }
            else
            {
                _gameActions.ShowmanReplic(s.ToString());
                _state.PlayerIsRight = false;
                updateSum = _state.Answerer.PersonalStake;

                ScheduleExecution(Tasks.AnnounceStake, 15);
            }
        }

        if (updateSum >= 0)
        {
            var answerResult = new AnswerResult(_state.AnswererIndex, _state.Answerer.AnswerIsRight, updateSum);
            _state.QuestionHistory.Add(answerResult);
        }

        return true;
    }

    private TurnSwitchingStrategy GetTurnSwitchingStrategy() => _state.Settings.AppSettings.GameMode switch
    {
        GameModes.Tv => TurnSwitchingStrategy.ByRightAnswerOnButton,
        GameModes.Sport => TurnSwitchingStrategy.Never,
        GameModes.Quiz => TurnSwitchingStrategy.Never,
        GameModes.TurnTaking => TurnSwitchingStrategy.Sequentially,
        _ => TurnSwitchingStrategy.Never,
    };

    private static string PrintRightFactor(double factor) => Math.Abs(factor - 1.0) < double.Epsilon ? "" : " * " + factor;

    /// <summary>
    /// Skips left question part and moves directly to answer.
    /// </summary>
    internal void MoveToAnswer()
    {
        if (_state.IsQuestionFinished)
        {
            return;
        }

        if (_state.QuestionPlay.AnswerOptions != null)
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
        if (!_state.QuestionPlay.UseButtons)
        {
            // No need to move to answer as special questions could be different
            // TODO: in the future there could be situations when special questions could be unfinished here
            ScheduleExecution(Tasks.WaitTry, 20);
            return;
        }

        var canAnybodyPress = _state.Players.Any(player => player.CanPress && player.IsConnected);

        if (!canAnybodyPress)
        {
            MoveToAnswer();
            ScheduleExecution(Tasks.WaitTry, 20, force: true);
            return;
        }

        if (_state.QuestionPlay.AnswerOptions != null)
        {
            var oneOptionLeft = _state.QuestionPlay.UsedAnswerOptions.Count + 1 == _state.QuestionPlay.AnswerOptions.Length;

            if (oneOptionLeft)
            {
                MoveToAnswer();
                ScheduleExecution(Tasks.WaitTry, 20, force: true);
                return;
            }
        }

        _state.PendingAnswererIndex = -1;
        _state.AnswererPressDuration = -1;
        _state.PendingAnswererIndicies.Clear();

        _gameActions.SendMessage(Messages.Resume); // To resume the media

        if (_state.Settings.AppSettings.FalseStart || _state.IsQuestionFinished)
        {
            ScheduleExecution(Tasks.AskToTry, 1, 1, true);
            return;
        }

        _gameActions.SendMessageWithArgs(Messages.Try, MessageParams.Try_NotFinished);
        _state.IsPlayingMedia = _state.IsPlayingMediaPaused;

        // Resume question playing
        if (_state.IsPartial)
        {
            _state.InitialPartialTextLength = _state.TextLength;
            _state.PartialIterationCounter = 0;
            ScheduleExecution(Tasks.PrintPartial, 5, force: true);
        }
        else
        {
            var waitTime = _state.IsPlayingMedia && _state.QuestionPlay.MediaContentCompletions.All(p => p.Value.Current > 0)
                ? 30 + _state.Settings.AppSettings.TimeSettings.TimeForMediaDelay * 10
                : _state.AtomTime;

            ScheduleExecution(Tasks.MoveNext, waitTime, force: true);
        }

        SendTryToPlayers();
        _state.Decision = DecisionType.Pressing;
    }

    private bool OnDecisionNextPersonStakeMaking()
    {
        var playerIndex = _state.Order[_state.OrderIndex];
        if (playerIndex == -1)
        {
            return false;
        }

        if (playerIndex >= _state.Players.Count)
        {
            throw new ArgumentException($"{nameof(playerIndex)} {playerIndex} must be in [0;{_state.Players.Count - 1}]");
        }

        StopWaiting();

        var s = $"{LO[nameof(R.StakeMakes)]} {_state.Players[playerIndex].Name}";
        _gameActions.ShowmanReplic(s);

        _state.OrderIndex--;
        ScheduleExecution(Tasks.AskStake, 10);
        return true;
    }

    private bool OnDecisionStarterChoosing()
    {
        if (_state.ChooserIndex == -1)
        {
            return false;
        }

        StopWaiting();

        var msg = string.Format(GetRandomString(LO[nameof(R.InformChooser)]), _state.Chooser.Name);
        _gameActions.ShowmanReplic(msg); // TODO: REMOVE: replaced by SETCHOOSER message
        _gameActions.SendMessageWithArgs(Messages.SetChooser, _state.ChooserIndex, "-", "INITIAL");
        
        ScheduleExecution(Tasks.MoveNext, 20);

        return true;
    }

    private void RunRoundTimer()
    {
        _state.TimerStartTime[0] = DateTime.UtcNow;
        _gameActions.SendMessageWithArgs(Messages.Timer, 0, MessageParams.Timer_Go, _state.TimeSettings.Round * 10);
    }

    private bool OnDecisionAnswering()
    {
        if (!HaveMultipleAnswerers())
        {
            if (_state.Answerer == null || string.IsNullOrEmpty(_state.Answerer.Answer))
            {
                return false;
            }

            StopWaiting();

            if (_state.QuestionPlay.AnswerOptions != null)
            {
                var answerIndex = Array.FindIndex(_state.QuestionPlay.AnswerOptions, o => o.Label == _state.Answerer.Answer);

                if (answerIndex > -1)
                {
                    _gameActions.SendMessageWithArgs(Messages.ContentState, ContentPlacements.Screen, answerIndex + 1, ItemState.Active);
                }

                ScheduleExecution(Tasks.AskRight, 15, force: true);
                return true;
            }
            else
            {
                _gameActions.PlayerReplic(_state.AnswererIndex, _state.Answerer.Answer); // TODO: REMOVE: replaced by PLAYER_ANSWER message
                _gameActions.SendMessageWithArgs(Messages.PlayerAnswer, _state.AnswererIndex, _state.Answerer.Answer);
            }

            if (_state.IsOralNow)
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

        var answererIndicies = _state.QuestionPlay.AnswererIndicies.OrderBy(index => _state.Players[index].Sum);
        _state.AnnouncedAnswerersEnumerator = new CustomEnumerator<int>(answererIndicies);

        if (_state.QuestionPlay.AnswerOptions != null)
        {
            var m = new MessageBuilder(Messages.Answers).AddRange(_state.Players.Select(p => p.Answer ?? ""));
            _gameActions.SendMessage(m.ToString());
            ScheduleExecution(Tasks.MoveNext, 30, 1, true);
            return true;
        }

        ScheduleExecution(Tasks.Announce, 15);
        return true;
    }

    public bool HaveMultipleAnswerers() => _state.QuestionPlay.AreMultipleAnswerers;

    public void StopWaiting()
    {
        _state.IsWaiting = false;
        _state.Decision = DecisionType.None;

        _gameActions.SendMessageWithArgs(Messages.Timer, 2, MessageParams.Timer_Stop);
    }

    internal string PrintHistory() => _tasksHistory.ToString();

    // TODO: currently PlanExecution() is used for interruprions and ScheduleExecution() for normal tasks flow
    // Think about using a universal scheduler which will be able to handle both cases

    internal void PlanExecution(Tasks task, double taskTime, int arg = 0)
    {
        _tasksHistory.AddLogEntry($"PlanExecution {task} {taskTime} {arg} ({_state.TInfo.Pause})");

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
        if (_state.IsDeferringAnswer)
        {
            // AskAnswerDeferred task cannot be avoided
            _tasksHistory.AddLogEntry($"AskAnswerDeferred task blocks scheduling ({_taskRunner.CurrentTask}): {task} {arg} {taskTime / 10}");
            return;
        }

        _tasksHistory.AddLogEntry($"Scheduled ({_taskRunner.CurrentTask}): {task} {arg} {taskTime / 10}");
        _taskRunner.ScheduleExecution(task, taskTime, arg, force || ShouldRunTimer());
    }

    private bool ShouldRunTimer() => !_state.Settings.AppSettings.Managed || _state.HostName == null || !_state.AllPersons.ContainsKey(_state.HostName);

    /// <summary>
    /// Executes current task of the game state machine.
    /// </summary>
    public void ExecuteTask(Tasks task, int arg)
    {
        try
        {
            _state.TaskLock.WithLock(() =>
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
                if (task == Tasks.AskToSelectQuestion && _taskRunner.OldTasks.Any())
                {
                    static string oldTaskPrinter(Tuple<Tasks, int, int> t) => $"{t.Item1}:{t.Item2}";

                    _state.Host.SendError(
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
                        OnPackage(_state.Package, arg);
                        break;

                    case Tasks.Round:
                        OnRound(_state.Round, arg);
                        break;

                    case Tasks.RoundTheme:
                        OnRoundTheme(arg);
                        break;

                    case Tasks.AskFirst:
                        GiveMoveToPlayerWithMinimumScore();
                        break;

                    case Tasks.WaitFirst:
                        WaitFirst();
                        break;

                    case Tasks.AskToSelectQuestion:
                        OnAskToSelectQuestion();
                        break;

                    case Tasks.WaitChoose:
                        WaitChoose();
                        break;

                    case Tasks.QuestionStartInfo:
                        OnQuestionStartInfo(arg);
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

                    case Tasks.AnnounceStakesWinner:
                        OnAnnounceStakesWinner();
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
                        AskToTry(arg);
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

                    case Tasks.QuestionPostInfo:
                        QuestionSourcesAndComments();
                        break;

                    case Tasks.StartAppellation:
                        OnStartAppellation();
                        break;

                    case Tasks.WaitAppellationDecision:
                        WaitAppellationDecision();
                        break;

                    case Tasks.CheckAppellation:
                        OnCheckAppellation();
                        break;

                    case Tasks.AskToDelete:
                        AskToDelete();
                        break;

                    case Tasks.WaitDelete:
                        WaitDelete();
                        break;

                    case Tasks.WaitHiddenStake:
                        WaitHiddenStake();
                        break;

                    case Tasks.Announce:
                        Announce();
                        break;

                    case Tasks.AnnounceStake:
                        AnnounceStake();
                        break;

                    case Tasks.AnnouncePostStakeWithAnswerOptions:
                        AnnouncePostStakeWithAnswerOptions();
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
            _state.Host.SendError(new Exception($"Task: {task}, param: {arg}, history: {_tasksHistory}", exc));
            ScheduleExecution(Tasks.NoTask, 10);
            _state.MoveNextBlocked = true;
            _gameActions.SendMessageWithArgs(Messages.GameError);
        }
    }

    private void OnRoundTheme(int themeIndex)
    {
        if (themeIndex < 0 || themeIndex >= _state.TInfo.RoundInfo.Count)
        {
            throw new ArgumentException($"{nameof(themeIndex)} {themeIndex} must be in [0;{_state.TInfo.RoundInfo.Count - 1}]");
        }

        _gameActions.SendThemeInfo(themeIndex, true);

        if (themeIndex + 1 < _state.TInfo.RoundInfo.Count)
        {
            ScheduleExecution(Tasks.RoundTheme, 19, themeIndex + 1);
        }
        else
        {
            InformTable();
            _state.ThemesPlayMode = ThemesPlayMode.None;
            ScheduleExecution(Tasks.AskFirst, 19);
        }
    }

    private void InformTable() => _state.TableInformStageLock.WithLock(
        () =>
        {
            _gameActions.InformTable();
            _state.InformStages |= InformStages.Table;
        },
        5000);

    private void AskAnswerDeferred()
    {
        _state.Decision = DecisionType.None;
        _state.IsDeferringAnswer = false;

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
            ? _state.MoveDirection.ToString()
            : (_stopReason == StopReason.Decision ? _state.Decision.ToString() : "");

        _tasksHistory.AddLogEntry($"StopReason {_stopReason} {stopReasonDetails}");

        // Interrupt standard execution and try to do something urgent
        switch (_stopReason)
        {
            case StopReason.Pause:
                _tasksHistory.AddLogEntry($"Pause PauseExecution {task} {arg} {_taskRunner.PrintOldTasks()}");
                _taskRunner.PauseExecution(task, arg, _leftTime);

                _state.PauseStartTime = DateTime.UtcNow;

                if (_state.IsPlayingMedia)
                {
                    _state.IsPlayingMediaPaused = true;
                    _state.IsPlayingMedia = false;
                }

                if (_state.IsThinking)
                {
                    var startTime = _state.TimerStartTime[1];

                    _state.TimeThinking += _state.PauseStartTime.Subtract(startTime).TotalMilliseconds / 100;
                    _state.IsThinkingPaused = true;
                    _state.IsThinking = false;
                }

                var times = new int[Constants.TimersCount];

                for (var i = 0; i < Constants.TimersCount; i++)
                {
                    times[i] = (int)(_state.PauseStartTime.Subtract(_state.TimerStartTime[i]).TotalMilliseconds / 100);
                }

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
                var savedTask = task == Tasks.WaitChoose ? Tasks.AskToSelectQuestion : (task == Tasks.WaitDelete ? Tasks.AskToDelete : task);

                _tasksHistory.AddLogEntry($"Appellation PauseExecution {savedTask} {arg} ({_taskRunner.PrintOldTasks()})");

                _taskRunner.PauseExecution(savedTask, arg, _leftTime);
                ScheduleExecution(Tasks.StartAppellation, 10);
                break;

            case StopReason.Move:
                switch (_state.MoveDirection)
                {
                    case MoveDirections.RoundBack:
                        if (Engine.CanMoveBackRound)
                        {
                            stop = Engine.MoveBackRound();

                            if (!stop)
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
                            var subText = _state.Text[_state.TextLength..];

                            _gameActions.SendMessageWithArgs(Messages.ContentAppend, ContentPlacements.Screen, 0, ContentTypes.Text, subText.EscapeNewLines());
                            _gameActions.SystemReplic(subText); // TODO: REMOVE: replaced by CONTENT_APPEND message

                            newTask = Tasks.MoveNext;
                        }
                        else if (task == Tasks.RoundTheme && !_state.Settings.AppSettings.Managed) // Skip all round themes
                        {
                            for (var themeIndex = arg; themeIndex < _state.TInfo.RoundInfo.Count; themeIndex++)
                            {
                                _gameActions.SendThemeInfo(themeIndex, true);
                            }

                            InformTable();
                            _state.ThemesPlayMode = ThemesPlayMode.None;
                            newTask = Tasks.AskFirst;
                        }

                        break;

                    case MoveDirections.RoundNext:
                        if (Engine.CanMoveNextRound)
                        {
                            stop = Engine.MoveNextRound();
                            
                            if (!stop)
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
                            stop = Engine.MoveToRound(_state.TargetRoundIndex);

                            if (!stop)
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
                    ScheduleExecution(Tasks.MoveNext, _state.MoveDirection == MoveDirections.Next ? 10 : 30);
                }

                break;

            case StopReason.Wait:
                // TODO: if someone overrides Task after that (skipping AskAnswerDeferred execution), nobody could press the button during this question
                // That's very fragile logic. Think about alternatives
                // The order of calls is important here!
                ScheduleExecution(Tasks.AskAnswerDeferred, _state.WaitInterval, force: true);
                _state.IsDeferringAnswer = true;
                break;
        }

        _stopReason = StopReason.None;

        return (stop, newTask);
    }

    private void GoodLuck()
    {
        _gameActions.ShowmanReplic(LO[nameof(R.GoodLuck)]);

        _state.Stage = GameStage.After;
        OnStageChanged(GameStages.Finished, LO[nameof(R.StageFinished)]);
        _gameActions.InformStage();

        SendStatistics();
        AskForPlayerReviews();
    }

    private void SendStatistics()
    {
        var msg = new MessageBuilder(Messages.GameStatistics);

        foreach (var (name, statistic) in _state.Statistics)
        {
            msg.AddRange(name, statistic.RightAnswerCount, statistic.WrongAnswerCount, statistic.RightTotal, statistic.WrongTotal);
        }

        _gameActions.SendVisualMessage(msg.Build());
    }

    private void AskForPlayerReviews()
    {
        _state.ReportsCount = _state.Players.Count;
        _state.GameResultInfo.Reviews.Clear();
        _state.GameResultInfo.Completed = true;

        ScheduleExecution(Tasks.WaitReport, 10 * 60 * 2); // 2 minutes
        WaitFor(DecisionType.Reporting, 10 * 60 * 2, -3);

        foreach (var player in _state.Players)
        {
            _gameActions.SendMessageToWithArgs(player.Name, Messages.Report, "");
        }

        _gameActions.AskReview();
    }

    private void WaitRight()
    {
        _gameActions.SendMessage(Messages.Cancel, _state.ShowMan.Name);

        if (_state.Answerer == null)
        {
            ScheduleExecution(Tasks.MoveNext, 10);
            return;
        }

        if (_state.Question == null)
        {
            throw new ArgumentException("_data.Question == null");
        }

        var answer = _state.Answerer.Answer;
        var isRight = answer != null && AnswerChecker.IsAnswerRight(answer, _state.Question.Right);

        _state.Answerer.AnswerIsRight = isRight;
        _state.Answerer.AnswerValidationFactor = 1.0;

        _state.ShowmanDecision = true;
        OnDecision();
    }

    internal void AddHistory(string message) => _tasksHistory.AddLogEntry(message);

    private void WaitSelectQuestionPrice()
    {
        if (_state.AnswererIndex == -1)
        {
            throw new ArgumentException($"{nameof(_state.AnswererIndex)} == -1", nameof(_state.AnswererIndex));
        }

        _gameActions.SendMessage(Messages.Cancel, _state.Answerer.Name);

        if (_state.IsOralNow)
        {
            _gameActions.SendMessage(Messages.Cancel, _state.ShowMan.Name);
        }

        _state.CurPriceRight = _state.StakeRange.Minimum;
        _state.CurPriceWrong = _state.StakeRange.Minimum;

        OnDecision();
    }

    private void WaitAppellationDecision()
    {
        SendCancellationsToActivePlayers();
        OnDecision();
    }

    private void SendCancellationsToActivePlayers()
    {
        foreach (var player in _state.Players)
        {
            if (player.Flag)
            {
                _gameActions.SendMessage(Messages.Cancel, player.Name);
            }
        }
    }

    private void MoveNext()
    {
        if (_completion != null)
        {
            _completion();
            _completion = null;
        }

        if (_continuation != null)
        {
            _continuation();
            _continuation = null;
            return;
        }

        Engine?.MoveNext();
        _state.MoveNextBlocked = false;

        _tasksHistory.AddLogEntry($"Moved -> {Engine?.Stage}");
    }

    private void OnAnnounceStakesWinner()
    {
        var stakerIndex = _state.Stakes.StakerIndex;

        if (stakerIndex == -1)
        {
            throw new ArgumentException($"{nameof(OnAnnounceStakesWinner)}: {nameof(stakerIndex)} == -1 {_state.OrderHistory}", nameof(stakerIndex));
        }

        _state.ChooserIndex = stakerIndex;
        _state.AnswererIndex = stakerIndex;
        _state.QuestionPlay.SetSingleAnswerer(stakerIndex);
        _state.CurPriceRight = _state.Stake;
        _state.CurPriceWrong = _state.Stake;

        _gameActions.SendMessageWithArgs(Messages.SetChooser, _state.ChooserIndex, "+");

        var msg = $"{Notion.RandomString(LO[nameof(R.NowPlays)])} {_state.Players[stakerIndex].Name} {LO[nameof(R.With)]} {Notion.FormatNumber(_state.Stake)}";

        _gameActions.ShowmanReplic(msg.ToString());
        ScheduleExecution(Tasks.MoveNext, 15 + Random.Shared.Next(10));
    }

    private void WaitNext(bool isSelectingStaker)
    {
        _gameActions.SendMessage(Messages.Cancel, _state.ShowMan.Name);
        _gameActions.SendMessageWithArgs(Messages.Timer, 2, MessageParams.Timer_Stop);

        if (!isSelectingStaker && _state.ThemeDeleters != null && _state.ThemeDeleters.IsEmpty())
        {
            throw new InvalidOperationException("_data.ThemeDeleters are empty");
        }

        var playerIndex = isSelectingStaker ? _state.Order[_state.OrderIndex] : _state.ThemeDeleters?.Current.PlayerIndex;

        if (playerIndex == -1) // The showman has not made a decision
        {
            var candidates = _state.Players.Where(p => p.Flag).ToArray();

            if (candidates.Length == 0)
            {
                throw new Exception(
                    "Wait next error (candidates.Length == 0): " +
                    (isSelectingStaker ? "" : _state.ThemeDeleters?.GetRemoveLog()));
            }

            var index = Random.Shared.Next(candidates.Length);
            var newPlayerIndex = _state.Players.IndexOf(candidates[index]);

            if (isSelectingStaker)
            {
                _state.Order[_state.OrderIndex] = newPlayerIndex;
                CheckOrder(_state.OrderIndex);
            }
            else
            {
                try
                {
                    _state.ThemeDeleters?.Current.SetIndex(newPlayerIndex);
                }
                catch (Exception exc)
                {
                    throw new Exception($"Wait delete error ({newPlayerIndex}): " + _state.ThemeDeleters?.GetRemoveLog(), exc);
                }
            }
        }

        OnDecision();
    }

    private void WaitStake()
    {
        if (_state.OrderIndex == -1)
        {
            throw new ArgumentException($"{nameof(_state.OrderIndex)} == -1: {_state.OrderHistory}");
        }

        var playerIndex = _state.Order[_state.OrderIndex];

        if (playerIndex < 0 || playerIndex >= _state.Players.Count)
        {
            throw new ArgumentException($"{nameof(playerIndex)} {playerIndex} must be in [0; {_state.Players.Count - 1}]: {_state.OrderHistory}");
        }

        _gameActions.SendMessage(Messages.Cancel, _state.Players[playerIndex].Name);

        if (_state.IsOralNow)
        {
            _gameActions.SendMessage(Messages.Cancel, _state.ShowMan.Name);
        }

        _gameActions.SendMessageWithArgs(Messages.Timer, 2, MessageParams.Timer_Stop);
        _state.StakeType = _state.StakeTypes.HasFlag(StakeTypes.Nominal) ? StakeMode.Nominal : StakeMode.Pass;

        OnDecision();
    }

    private void AskToSelectQuestionPrice()
    {
        var answerer = _state.Answerer ?? throw new InvalidOperationException("Answerer not defined");

        var s = string.Join(Message.ArgsSeparator, Messages.CatCost, _state.StakeRange.Minimum, _state.StakeRange.Maximum, _state.StakeRange.Step);

        var waitTime = _state.TimeSettings.StakeMaking * 10;

        _state.IsOralNow = _state.IsOral && answerer.IsHuman;

        if (_state.IsOralNow)
        {
            _gameActions.SendMessage(s, _state.ShowMan.Name);
        }
        
        if (CanPlayerAct())
        {
            _gameActions.SendMessage(s, answerer.Name);

            if (!answerer.IsConnected)
            {
                waitTime = 20;
            }
        }

        _state.StakeModes = StakeModes.Stake;
        AskToMakeStake(StakeReason.Simple, answerer.Name, _state.StakeRange);

        ScheduleExecution(Tasks.WaitSelectQuestionPrice, waitTime);
        WaitFor(DecisionType.QuestionPriceSelection, waitTime, _state.AnswererIndex);
    }

    private void WaitFirst()
    {
        _gameActions.SendMessage(Messages.Cancel, _state.ShowMan.Name);

        if (_state.ChooserIndex == -1)
        {
            _state.ChooserIndex = _state.Players.SelectRandom(p => p.Flag);
        }

        OnDecision();
    }

    private void WaitAnswer()
    {
        if (_state.Round == null)
        {
            throw new ArgumentNullException(nameof(_state.Round));
        }

        if (!HaveMultipleAnswerers())
        {
            if (_state.Answerer == null)
            {
                ScheduleExecution(Tasks.MoveNext, 10);
                return;
            }

            _gameActions.SendMessage(Messages.Cancel, _state.Answerer.Name);

            if (string.IsNullOrEmpty(_state.Answerer.Answer))
            {
                _state.Answerer.Answer = LO[nameof(R.IDontKnow)];
                _state.Answerer.AnswerIsWrong = !_state.IsOralNow;
            }
        }
        else
        {
            if (_state.QuestionPlay.ActiveValidationCount > 0)
            {
                _gameActions.SendMessage(Messages.Cancel, _state.ShowMan.Name); // Cancel validation
            }

            for (var i = 0; i < _state.Players.Count; i++)
            {
                if (_state.QuestionPlay.AnswererIndicies.Contains(i) && string.IsNullOrEmpty(_state.Players[i].Answer))
                {
                    _state.Players[i].Answer = LO[nameof(R.IDontKnow)];
                    _state.Players[i].AnswerIsWrong = true;
                }

                _gameActions.SendMessage(Messages.Cancel, _state.Players[i].Name);
            }

            _state.IsWaiting = true;
        }

        OnDecision();
    }

    private void WaitQuestionAnswererSelection()
    {
        _gameActions.SendMessage(Messages.Cancel, _state.Chooser.Name);

        if (_state.IsOralNow)
        {
            _gameActions.SendMessage(Messages.Cancel, _state.ShowMan.Name);
        }

        var index = _state.Players.SelectRandomOnIndex(index => index != _state.ChooserIndex);

        _state.AnswererIndex = index;
        _state.QuestionPlay.SetSingleAnswerer(index);

        OnDecision();
    }

    private void WaitTry()
    {
        _state.IsThinking = false;
        _state.Decision = DecisionType.None;

        if (_state.QuestionPlay.UseButtons)
        {
            _gameActions.SendMessageWithArgs(Messages.EndTry, MessageParams.EndTry_All); // Timer 1 STOP
        }

        ScheduleExecution(Tasks.MoveNext, 1, force: true);

        _state.IsQuestionAskPlaying = false;
    }

    private void WaitHiddenStake()
    {
        _gameActions.SendMessageWithArgs(Messages.Timer, 2, MessageParams.Timer_Stop);

        for (var i = 0; i < _state.Players.Count; i++)
        {
            if (_state.QuestionPlay.AnswererIndicies.Contains(i) && _state.Players[i].PersonalStake == -1)
            {
                _gameActions.SendMessage(Messages.Cancel, _state.Players[i].Name);
                _state.Players[i].PersonalStake = 1;

                _gameActions.SendMessageWithArgs(Messages.PersonFinalStake, i);
            }
        }

        _state.HiddenStakerCount = 0;
        OnDecision();
    }

    private void WaitReport()
    {
        try
        {
            foreach (var item in _state.Players)
            {
                _gameActions.SendMessage(Messages.Cancel, item.Name);
            }

            SendReport();
            StopWaiting();
        }
        catch (Exception exc)
        {
            _state.Host.SendError(exc);
        }
    }

    private void AnnouncePostStakeWithAnswerOptions()
    {
        if (_state.AnnouncedAnswerersEnumerator == null || !_state.AnnouncedAnswerersEnumerator.MoveNext())
        {
            PostprocessQuestion();
            return;
        }

        var answererIndex = _state.AnnouncedAnswerersEnumerator.Current;
        _state.AnswererIndex = answererIndex;

        _state.PlayerIsRight = _state.Answerer?.Answer == _state.RightOptionLabel;
        AnnounceStakeCore();
        ScheduleExecution(Tasks.AnnouncePostStakeWithAnswerOptions, 15);
    }

    private void AnnounceStake()
    {
        AnnounceStakeCore();
        ScheduleExecution(Tasks.Announce, 15);
    }

    private void AnnounceStakeCore()
    {
        var answerer = _state.Answerer;

        if (answerer == null)
        {
            throw new ArgumentException($"{nameof(answerer)} == null", nameof(answerer));
        }

        var stake = answerer.PersonalStake;
        _gameActions.ShowmanReplic($"{LO[nameof(R.Stake)]} {answerer.Name}: {Notion.FormatNumber(stake)}");

        var message = new MessageBuilder(Messages.Person);

        if (_state.PlayerIsRight)
        {
            message.Add('+');
            AddRightSum(answerer, stake);
        }
        else
        {
            message.Add('-');
            SubtractWrongSum(answerer, stake);
        }

        message.Add(_state.AnswererIndex).Add(stake);

        _gameActions.SendMessage(message.ToString());
        _gameActions.InformSums();

        _gameActions.SendMessageWithArgs(Messages.PersonStake, _state.AnswererIndex, 1, stake);
    }

    private void AskHiddenStakes()
    {
        var s = GetRandomString(LO[nameof(R.MakeStake)]);
        _gameActions.ShowmanReplic(s);

        _state.HiddenStakerCount = 0;
        var stakers = new List<(string, StakeSettings)>();

        for (var i = 0; i < _state.Players.Count; i++)
        {
            if (_state.QuestionPlay.AnswererIndicies.Contains(i))
            {
                if (_state.Players[i].Sum <= 1)
                {
                    _state.Players[i].PersonalStake = 1; // only one choice
                    _gameActions.SendMessageWithArgs(Messages.PersonFinalStake, i);
                    continue;
                }

                _state.Players[i].PersonalStake = -1;
                _state.HiddenStakerCount++;
                _gameActions.SendMessage(Messages.FinalStake, _state.Players[i].Name);

                stakers.Add((_state.Players[i].Name, new(1, _state.Players[i].Sum, 1)));
            }
        }

        if (_state.HiddenStakerCount == 0)
        {
            ProceedToHiddenStakesQuestion();
            return;
        }

        _state.IsOralNow = false;
        _state.StakeModes = StakeModes.Stake;
        AskToMakeStake(StakeReason.Hidden, stakers);

        var waitTime = _state.TimeSettings.StakeMaking * 10;
        ScheduleExecution(Tasks.WaitHiddenStake, waitTime);
        WaitFor(DecisionType.HiddenStakeMaking, waitTime, -2);
    }

    private void WaitDelete()
    {
        _gameActions.SendMessage(Messages.Cancel, _state.ActivePlayer.Name);

        if (_state.IsOralNow)
        {
            _gameActions.SendMessage(Messages.Cancel, _state.ShowMan.Name);
        }

        _gameActions.SendMessageWithArgs(Messages.Timer, 2, MessageParams.Timer_Stop);

        _state.ThemeIndexToDelete = _state.TInfo.RoundInfo.SelectRandom(item => item.Name != null);

        OnDecision();
    }

    private void Winner()
    {
        var winnerScore = _state.Players.Max(player => player.Sum);
        var winnerCount = _state.Players.Count(player => player.Sum == winnerScore);

        if (winnerCount == 1)
        {
            for (var i = 0; i < _state.Players.Count; i++)
            {
                if (_state.Players[i].Sum == winnerScore)
                {
                    var s = new StringBuilder(_state.Players[i].Name).Append(", ");
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

    private void AskToTry(int arg)
    {
        if (_state.Players.All(p => !p.CanPress))
        {
            ScheduleExecution(Tasks.WaitTry, 3, force: true);
            return;
        }

        if (_state.Settings.AppSettings.FalseStart || arg == 1)
        {
            _gameActions.SendMessage(Messages.Try);
        }

        SendTryToPlayers();

        var maxTime = _state.TimeSettings.ButtonPressing * 10;

        _state.TimerStartTime[1] = DateTime.UtcNow;
        _state.IsThinking = true;
        _gameActions.SendMessageWithArgs(Messages.Timer, 1, MessageParams.Timer_Resume);
        _state.Decision = DecisionType.Pressing;

        ScheduleExecution(Tasks.WaitTry, Math.Max(maxTime - _state.TimeThinking, 10));
    }

    private void SendTryToPlayers()
    {
        for (var i = 0; i < _state.Players.Count; i++)
        {
            if (_state.Players[i].CanPress)
            {
                _gameActions.SendMessage(Messages.YouTry, _state.Players[i].Name);
            }
        }
    }

    private void WaitChoose()
    {
        _gameActions.SendMessage(Messages.Cancel, _state.Chooser.Name);

        if (_state.IsOralNow)
        {
            _gameActions.SendMessage(Messages.Cancel, _state.ShowMan.Name);
        }

        var canChooseTheme = _state.TInfo.RoundInfo.Select(t => t.Questions.Any(QuestionHelper.IsActive)).ToArray();
        var numberOfThemes = canChooseTheme.Where(can => can).Count();

        if (numberOfThemes == 0)
        {
            throw new Exception($"numberOfThemes == 0! GetRoundActiveQuestionsCount: {GetRoundActiveQuestionsCount()}");
        }

        // Random theme index selection
        var k1 = Random.Shared.Next(numberOfThemes);
        var i = -1;

        do if (canChooseTheme[++i]) k1--; while (k1 >= 0);

        var theme = _state.TInfo.RoundInfo[i];
        var numberOfQuestions = theme.Questions.Count(QuestionHelper.IsActive);

        // Random question index selection
        var k2 = Random.Shared.Next(numberOfQuestions);
        var j = -1;

        do if (theme.Questions[++j].IsActive()) k2--; while (k2 >= 0);

        _state.ThemeIndex = i;
        _state.QuestionIndex = j;

        OnDecision();
    }

    private void OnQuestionStartInfo(int arg)
    {
        var authors = _state.PackageDoc.ResolveAuthors(_state.Question.Info.Authors);

        if (authors.Length > 0)
        {
            var msg = new MessageBuilder(Messages.QuestionAuthors).AddRange(authors);
            _gameActions.SendMessage(msg.ToString());
        }

        var themeComments = _state.Theme.Info.Comments.Text;

        if (themeComments.Length > 0)
        {
            _gameActions.SendMessageWithArgs(Messages.ThemeComments, themeComments.EscapeNewLines()); // TODO: REMOVE: replaced by THEME message
        }

        ScheduleExecution(Tasks.MoveNext, 1, arg + 1, force: true);
    }

    internal void OnQuestionType(string typeName, bool isDefault)
    {
        if (!isDefault) // TODO: This announcement should be handled by the client in the future
        {
            switch (typeName)
            {
                case QuestionTypes.Stake:
                case QuestionTypes.Secret:
                case QuestionTypes.SecretPublicPrice:
                case QuestionTypes.SecretNoQuestion:
                case QuestionTypes.NoRisk:
                case QuestionTypes.Simple:
                case QuestionTypes.StakeAll:
                case QuestionTypes.ForAll:
                    break;

                default:
                    OnUnsupportedQuestionType(typeName); // TODO: emit this by Game Engine
                    return;
            }
        }

        _state.QuestionTypeSettings.TryGetValue(typeName, out var questionTypeRules);
        
        var isNoRisk = questionTypeRules?.PenaltyType == PenaltyType.None;

        if (isNoRisk)
        {
            _state.CurPriceWrong = 0;
        }

        _gameActions.SendVisualMessageWithArgs(Messages.QType, typeName, isDefault, isNoRisk);
        ScheduleExecution(Tasks.MoveNext, isDefault ? 1 : 22, force: true);
    }

    private void OnUnsupportedQuestionType(string typeName)
    {
        var sp = new StringBuilder(LO[nameof(R.UnknownType)]).Append(' ').Append(typeName);

        // TODO: REMOVE+
        _gameActions.ShowmanReplic(LO[nameof(R.ManuallyPlayedQuestion)]);
        // TODO END

        _gameActions.ShowmanReplicNew(MessageCode.UnsupportedQuestion);

        _state.SkipQuestion?.Invoke();
        ScheduleExecution(Tasks.MoveNext, 150, 1);
    }

    private void PrintPartial()
    {
        var text = _state.Text;

        // TODO: try to avoid getting here when such condition is met
        if (_state.TextLength >= text.Length)
        {
            _state.TimeThinking = 0.0;
            ScheduleExecution(Tasks.MoveNext, 1, force: true);
            return;
        }

        _state.PartialIterationCounter++;

        var newTextLength = Math.Min(
            _state.InitialPartialTextLength
                + (int)(_state.Settings.AppSettings.ReadingSpeed * PartialPrintFrequencyPerSecond * _state.PartialIterationCounter),
            text.Length);

        if (newTextLength > _state.TextLength)
        {
            var printingLength = newTextLength - _state.TextLength;

            // Align to next space position
            while (_state.TextLength + printingLength < text.Length && !char.IsWhiteSpace(text[_state.TextLength + printingLength]))
            {
                printingLength++;
            }

            var subText = text.Substring(_state.TextLength, printingLength);

            _gameActions.SendMessageWithArgs(Messages.ContentAppend, ContentPlacements.Screen, 0, ContentTypes.Text, subText.EscapeNewLines());
            _gameActions.SystemReplic(subText); // TODO: REMOVE: replaced by CONTENT_APPEND message

            _state.TextLength += printingLength;
        }

        if (_state.TextLength < text.Length)
        {
            _state.AtomTime -= (int)(10 * PartialPrintFrequencyPerSecond);
            ScheduleExecution(Tasks.PrintPartial, 10 * PartialPrintFrequencyPerSecond, force: true);
        }
        else
        {
            _state.TimeThinking = 0.0;
            ScheduleExecution(Tasks.MoveNext, Math.Max(_state.AtomTime, 10), force: true);
        }
    }

    private void QuestionSourcesAndComments()
    {
        var informed = false;

        var textTime = 1;

        _gameActions.SendMessageWithArgs(Messages.QuestionEnd); // Should be here because only here question is fully processed

        var sources = _state.PackageDoc.ResolveSources(_state.Question.Info.Sources);

        if (sources.Count > 0)
        {
            var msg = new MessageBuilder(Messages.QuestionSources).AddRange(sources);
            _gameActions.SendMessage(msg.Build());
        }

        var comments = _state.Question.Info.Comments.Text;

        if (comments.Length > 0)
        {
            _gameActions.SendVisualMessageWithArgs(Messages.QuestionComments, comments.EscapeNewLines());
            textTime = GetReadingDurationForTextLength(comments.Length);
            informed = true;
        }

        ScheduleExecution(Tasks.MoveNext, textTime, force: !informed);
    }

    private int GetReadingDurationForTextLength(int textLength)
    {
        var readingSpeed = Math.Max(1, _state.Settings.AppSettings.ReadingSpeed);
        return Math.Max(1, 10 * textLength / readingSpeed);
    }

    internal void Announce()
    {
        if (_state.AnnouncedAnswerersEnumerator == null || !_state.AnnouncedAnswerersEnumerator.MoveNext())
        {
            ScheduleExecution(Tasks.MoveNext, 15, 1, true);
            return;
        }

        var answererIndex = _state.AnnouncedAnswerersEnumerator.Current;
        _state.AnswererIndex = answererIndex;
        var playerAnswer = _state.Answerer?.Answer;
        var answer = string.IsNullOrEmpty(playerAnswer) ? LO[nameof(R.IDontKnow)] : playerAnswer;

        _gameActions.PlayerReplic(answererIndex, answer); // TODO: REMOVE: replaced by PLAYER_ANSWER message
        _gameActions.SendMessageWithArgs(Messages.PlayerAnswer, answererIndex, playerAnswer ?? "");

        if (_state.QuestionPlay.ValidateAfterRightAnswer)
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
        var buttonPressMode = _state.Settings.AppSettings.ButtonPressMode;

        if (buttonPressMode == ButtonPressMode.RandomWithinInterval)
        {
            if (_state.PendingAnswererIndicies.Count == 0)
            {
                DumpButtonPressError("_data.PendingAnswererIndicies.Count == 0");
                return false;
            }

            var index = _state.PendingAnswererIndicies.Count == 1 ? 0 : Random.Shared.Next(_state.PendingAnswererIndicies.Count);
            _state.PendingAnswererIndex = _state.PendingAnswererIndicies[index];
        }

        if (_state.PendingAnswererIndex < 0 || _state.PendingAnswererIndex >= _state.Players.Count)
        {
            DumpButtonPressError($"_data.PendingAnswererIndex = {_state.PendingAnswererIndex}; _data.Players.Count = {_state.Players.Count}");
            return false;
        }

        _state.AnswererIndex = _state.PendingAnswererIndex;
        _state.QuestionPlay.SetSingleAnswerer(_state.PendingAnswererIndex);

        if (!_state.Settings.AppSettings.FalseStart)
        {
            // Stop question reading
            if (!_state.IsQuestionFinished)
            {
                var timeDiff = (int)DateTime.UtcNow.Subtract(_state.AtomStart).TotalSeconds * 10;
                _state.AtomTime = Math.Max(1, _state.AtomTime - timeDiff);
            }
        }

        if (_state.IsThinking)
        {
            var startTime = _state.TimerStartTime[1];
            var currentTime = DateTime.UtcNow;

            _state.TimeThinking += currentTime.Subtract(startTime).TotalMilliseconds / 100;
        }

        var answerer = _state.Answerer;

        if (answerer == null)
        {
            DumpButtonPressError("answerer == null");
            return false;
        }

        answerer.CanPress = false;

        _state.IsThinking = false;

        _gameActions.SendMessageWithArgs(Messages.Timer, 1, MessageParams.Timer_Pause, (int)_state.TimeThinking);

        _state.IsPlayingMediaPaused = _state.IsPlayingMedia;
        _state.IsPlayingMedia = false;

        return true;
    }

    internal void DumpButtonPressError(string reason)
    {
        var pressMode = _state.Settings.AppSettings.ButtonPressMode;
        _state.Host.SendError(new Exception($"{reason} {pressMode}"));
    }

    private void StartGame(int arg)
    {
        var nextArg = arg + 1;
        var extraTime = 0;

        switch (arg)
        {
            case 1:
                _gameActions.ShowmanReplic(LO[nameof(R.ShowmanGreeting)]); // TODO: REMOVE (localized by MessageCode)
                _gameActions.ShowmanReplicNew(MessageCode.ShowmanGreeting);
                nextArg = 2;
                break;

            case 2:
                _gameActions.ShowmanReplic($"{LO[nameof(R.GameRules)]}: {BuildRulesString(_state.Settings.AppSettings)}"); // TODO: REMOVE (replaced by OPTIONS2 message)
                nextArg = -1;
                extraTime = 20;
                break;

            default:
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

        if (settings.Managed)
        {
            rules.Add(LO[nameof(R.TypeManaged)]);
        }

        if (rules.Count == 0)
        {
            rules.Add(LO[nameof(R.TypeClassic)]);
        }

        return string.Join(", ", rules);
    }

    private void OnAskToSelectQuestion()
    {
        _gameActions.InformSums();
        _gameActions.SendVisualMessage(Messages.ShowTable);

        if (_state.Chooser == null)
        {
            throw new Exception("_data.Chooser == null");
        }

        if (_gameActions.Client.CurrentNode == null)
        {
            throw new Exception("_actor.Client.CurrentServer == null");
        }

        var msg = new StringBuilder(_state.Chooser.Name).Append(", ");
        var activeQuestionsCount = GetRoundActiveQuestionsCount();

        if (activeQuestionsCount == 0)
        {
            throw new Exception($"activeQuestionsCount == 0 {Engine.Stage}");
        }

        msg.Append(GetRandomString(LO[activeQuestionsCount > 1 ? nameof(R.ChooseQuest) : nameof(R.LastQuest)]));

        _gameActions.ShowmanReplic(msg.ToString()); // TODO: REMOVE (localized by MessageCode)
        _gameActions.ShowmanReplicNew(MessageCode.SelectQuestion, _state.Chooser.Name);

        _state.ThemeIndex = -1;
        _state.QuestionIndex = -1;

        _state.UsedWrongVersions.Clear();

        int time;

        if (activeQuestionsCount > 1)
        {
            time = _state.TimeSettings.QuestionSelection * 10;

            var message = $"{Messages.Choose}{Message.ArgsSeparatorChar}1";
            _state.IsOralNow = _state.IsOral && _state.Chooser.IsHuman;

            if (_state.IsOralNow)
            {
                _gameActions.SendMessage(message, _state.ShowMan.Name);
            }
            else if (!_state.Chooser.IsConnected)
            {
                time = 20;
            }

            if (CanPlayerAct())
            {
                _gameActions.SendMessage(message, _state.Chooser.Name);
            }
        }
        else
        {
            time = 20;
        }

        ScheduleExecution(Tasks.WaitChoose, time);
        WaitFor(DecisionType.QuestionSelection, time, _state.ChooserIndex);
    }

    internal bool CanPlayerAct() => !_state.IsOralNow || _state.Settings.AppSettings.OralPlayersActions;

    private void AskToSelectQuestionAnswerer()
    {
        if (_state.Chooser == null)
        {
            throw new Exception("_data.Chooser == null");
        }

        var canGiveThemselves = _state.Chooser.Flag;
        var append = canGiveThemselves ? $" {LO[nameof(R.YouCanKeepCat)]}" : "";
        _gameActions.ShowmanReplic($"{_state.Chooser.Name}, {LO[nameof(R.GiveCat)]}{append}"); // TODO: REMOVE (localized by MessageCode)
        _gameActions.ShowmanReplicNew(MessageCode.SelectPlayer, _state.Chooser.Name);

        // -- Deprecated
        var msg = new StringBuilder(Messages.Cat);

        for (var i = 0; i < _state.Players.Count; i++)
        {
            msg.Append(Message.ArgsSeparatorChar).Append(_state.Players[i].Flag ? '+' : '-');
        }

        _state.AnswererIndex = -1;

        var waitTime = _state.TimeSettings.PlayerSelection * 10;

        _state.IsOralNow = _state.IsOral && _state.Chooser.IsHuman;
        var playerSelectors = new List<string>();

        if (_state.IsOralNow)
        {
            playerSelectors.Add(_state.ShowMan.Name);
            _gameActions.SendMessage(msg.ToString(), _state.ShowMan.Name);
        }
        else if (!_state.Chooser.IsConnected)
        {
            waitTime = 20;
        }

        if (CanPlayerAct() && _state.Chooser != null)
        {
            playerSelectors.Add(_state.Chooser.Name);
            _gameActions.SendMessage(msg.ToString(), _state.Chooser.Name);
        }

        AskToSelectPlayer(SelectPlayerReason.Answerer, playerSelectors.ToArray());
        ScheduleExecution(Tasks.WaitQuestionAnswererSelection, waitTime);
        WaitFor(DecisionType.QuestionAnswererSelection, waitTime, _state.ChooserIndex);
    }

    /// <summary>
    /// Finds players with minimum sum.
    /// If there is only one player, they got the move.
    /// Otherwise ask showman to select moving player.
    /// </summary>
    private void GiveMoveToPlayerWithMinimumScore()
    {
        var min = _state.Players.Min(player => player.Sum);
        var total = 0;

        for (var i = 0; i < _state.Players.Count; i++)
        {
            if (_state.Players[i].Sum == min)
            {
                _state.Players[i].Flag = true;
                total++;
            }
            else
            {
                _state.Players[i].Flag = false;
            }
        }

        if (total == 1)
        {
            for (var i = 0; i < _state.Players.Count; i++)
            {
                if (_state.Players[i].Flag)
                {
                    _state.ChooserIndex = i;
                    break;
                }
            }

            _state.IsWaiting = true;
            _state.Decision = DecisionType.StarterChoosing;
            OnDecision();
        }
        else
        {
            _gameActions.SendVisualMessage(Messages.ShowTable); // Everybody will see the table during showman's decision

            _state.ChooserIndex = -1;

            // -- Deprecated
            var msg = new StringBuilder(Messages.First);

            for (var i = 0; i < _state.Players.Count; i++)
            {
                msg.Append(Message.ArgsSeparatorChar).Append(_state.Players[i].Flag ? '+' : '-');
            }

            _gameActions.SendMessage(msg.ToString(), _state.ShowMan.Name);
            // -- end
            AskToSelectPlayer(SelectPlayerReason.Chooser, _state.ShowMan.Name);

            var waitTime = _state.TimeSettings.ShowmanDecision * 10;
            ScheduleExecution(Tasks.WaitFirst, waitTime);
            WaitFor(DecisionType.StarterChoosing, waitTime, -1);
        }
    }

    private void AskToSelectPlayer(SelectPlayerReason reason, params string[] selectors)
    {
        var msg = new MessageBuilder(Messages.AskSelectPlayer, reason);

        for (var i = 0; i < _state.Players.Count; i++)
        {
            msg.Add(_state.Players[i].Flag ? '+' : '-');
        }

        foreach (var selector in selectors)
        {
            _gameActions.SendMessage(msg.ToString(), selector);
        }

        _state.DecisionMakers.Clear();
        _state.DecisionMakers.AddRange(selectors);
    }

    private void AskRight()
    {
        if (_state.Answerer == null)
        {
            throw new InvalidOperationException("Answerer is null");
        }

        if (_state.QuestionPlay.AnswerOptions != null)
        {
            _state.IsWaiting = true;
            _state.Decision = DecisionType.AnswerValidating;

            var rightLabel = _state.Question?.Right.FirstOrDefault();

            _state.Answerer.AnswerIsRight = _state.Answerer.Answer == rightLabel;
            _state.Answerer.AnswerValidationFactor = 1.0;
            _state.ShowmanDecision = true;

            OnDecision();
        }
        else if (!_state.Answerer.IsHuman || _state.Answerer.AnswerIsWrong)
        {
            _state.IsWaiting = true;
            _state.Decision = DecisionType.AnswerValidating;

            _state.Answerer.AnswerIsRight = !_state.Answerer.AnswerIsWrong;
            _state.Answerer.AnswerValidationFactor = 1.0;
            _state.ShowmanDecision = true;

            OnDecision();
        }
        else if (_state.Answerer.Answer != null
            && _state.QuestionPlay.Validations.TryGetValue(_state.Answerer.Answer, out var validation)
            && validation.HasValue)
        {
            _state.IsWaiting = true;
            _state.Decision = DecisionType.AnswerValidating;

            _state.Answerer.AnswerIsRight = validation.Value.Item1;
            _state.Answerer.AnswerValidationFactor = validation.Value.Item2;
            _state.ShowmanDecision = true;

            OnDecision();
        }
        else if (_state.QuestionPlay.IsNumericAnswer &&
            int.TryParse(_state.Question?.Right.FirstOrDefault() ?? "", out var rightNumber))
        {
            _state.IsWaiting = true;
            _state.Decision = DecisionType.AnswerValidating;

            var deviation = _state.QuestionPlay.NumericAnswerDeviation;

            _state.Answerer.AnswerIsRight = int.TryParse(_state.Answerer.Answer, out var playerNumber) && 
                Math.Abs(playerNumber - rightNumber) <= deviation;
            
            _state.Answerer.AnswerValidationFactor = 1.0;
            _state.ShowmanDecision = true;
            OnDecision();
        }
        else
        {
            _state.ShowmanDecision = false;

            if (!_state.IsOralNow || HaveMultipleAnswerers())
            {
                SendAnswersInfoToShowman(_state.Answerer.Answer ?? "");
            }

            var waitTime = _state.TimeSettings.ShowmanDecision * 10;
            ScheduleExecution(Tasks.WaitRight, waitTime);
            WaitFor(DecisionType.AnswerValidating, waitTime, -1);
        }
    }

    private void SendAnswersInfoToShowman(string answer)
    {
        _gameActions.SendMessage(
            BuildValidation2Message(_state.Answerer.Name, answer, !_state.QuestionPlay.FlexiblePrice),
            _state.ShowMan.Name);
    }

    private string BuildValidation2Message(string name, string answer, bool allowPriceModifications, bool isCheckingForTheRight = true)
    {
        var question = _state.Question ?? throw new InvalidOperationException("Question is null");

        var rightAnswers = question.Right;
        var wrongAnswers = question.Wrong;

        ICollection<string> appellatedAnswers = Array.Empty<string>();

        if (_state.PackageStatistisProvider != null && !_state.ShowMan.IsHuman)
        {
            appellatedAnswers = _state.PackageStatistisProvider.GetAppellatedAnswers(
                Engine.RoundIndex,
                _state.ThemeIndex,
                _state.QuestionIndex);
        }

        return new MessageBuilder(
            Messages.Validation2,
            name,
            answer,
            isCheckingForTheRight ? '+' : '-',
            allowPriceModifications ? '+' : '-',
            rightAnswers.Count + appellatedAnswers.Count)
            .AddRange(rightAnswers)
            .AddRange(appellatedAnswers)
            .AddRange(wrongAnswers)
            .Build();
    }

    private void AskAnswer()
    {
        if (HaveMultipleAnswerers())
        {
            _gameActions.ShowmanReplic(LO[nameof(R.StartThink)]);

            for (var i = 0; i < _state.Players.Count; i++)
            {
                if (_state.QuestionPlay.AnswererIndicies.Contains(i))
                {
                    _state.Players[i].Answer = "";
                    _state.Players[i].Flag = true;
                    _gameActions.AskAnswer(_state.Players[i].Name, _state.QuestionPlay.IsNumericAnswer ? "number" : "");
                }
            }

            var waitTime = _state.TimeSettings.HiddenAnswering * 10;

            _state.AnswerCount = _state.QuestionPlay.AnswererIndicies.Count;
            ScheduleExecution(Tasks.WaitAnswer, waitTime, force: true);
            WaitFor(DecisionType.Answering, waitTime, -2, false);
            return;
        }

        if (_state.Answerer == null)
        {
            ScheduleExecution(Tasks.MoveNext, 10);
            return;
        }

        var useButtons = _state.QuestionPlay.UseButtons;

        if (useButtons)
        {
            _gameActions.SendMessageWithArgs(Messages.EndTry, _state.AnswererIndex);
        }
        else
        {
            _gameActions.SendMessageWithArgs(Messages.StopPlay);
        }

        var waitAnswerTime = useButtons ? _state.TimeSettings.Answering * 10 : _state.TimeSettings.SoloAnswering * 10;

        var useAnswerOptions = _state.QuestionPlay.AnswerOptions != null;
        _state.IsOralNow = _state.IsOral && _state.Answerer.IsHuman;

        if (useAnswerOptions)
        {
            if (_state.IsOralNow)
            {
                _gameActions.SendMessage(Messages.Answer, _state.ShowMan.Name);
            }

            _gameActions.SendMessage(CanPlayerAct() ? Messages.Answer : Messages.OralAnswer, _state.Answerer.Name);
        }
        else
        {
            if (_state.IsOralNow)
            {
                // Showman accepts answer orally
                SendAnswersInfoToShowman($"({LO[nameof(R.AnswerIsOral)]})");
                _gameActions.SendMessage(Messages.OralAnswer, _state.Answerer.Name);
            }
            else // The only place where we do not check CanPlayerAct()
            {
                _gameActions.SendMessageToWithArgs(
                    _state.Answerer.Name,
                    Messages.Answer,
                    _state.QuestionPlay.IsNumericAnswer ? "number" : "");
            }
        }

        var answerReplic = useAnswerOptions ? ", " + LO[nameof(R.SelectAnswerOption)] : GetRandomString(LO[nameof(R.YourAnswer)]);
        _gameActions.ShowmanReplic(_state.Answerer.Name + answerReplic); // TODO: REMOVE (localized by MessageCode)
        _gameActions.ShowmanReplicNew(useAnswerOptions ? MessageCode.SelectAnswerOption : MessageCode.Answer, _state.Answerer.Name);

        _state.Answerer.Answer = "";

        var buttonPressMode = _state.Settings.AppSettings.ButtonPressMode;

        if (buttonPressMode != ButtonPressMode.FirstWins)
        {
            InformWrongTries();
        }

        _state.AnswerCount = 1;
        ScheduleExecution(Tasks.WaitAnswer, waitAnswerTime);
        WaitFor(DecisionType.Answering, waitAnswerTime, _state.AnswererIndex);
    }

    internal void SendQuestionAnswersToShowman()
    {
        var question = _state.Question;

        if (question == null || _state.QuestionPlay.AnswerOptions != null)
        {
            return;
        }

        var rightAnswers = question.Right;
        var wrongAnswers = question.Wrong;

        ICollection<string> appellatedAnswers = Array.Empty<string>();

        if (_state.PackageStatistisProvider != null && !_state.ShowMan.IsHuman)
        {
            appellatedAnswers = _state.PackageStatistisProvider.GetAppellatedAnswers(
                Engine.RoundIndex,
                _state.ThemeIndex,
                _state.QuestionIndex);
        }

        var message = new MessageBuilder(Messages.QuestionAnswers, rightAnswers.Count + appellatedAnswers.Count)
            .AddRange(rightAnswers)
            .AddRange(appellatedAnswers)
            .AddRange(wrongAnswers)
            .Build();

        _gameActions.SendMessage(message, _state.ShowMan.Name);
    }

    private void InformWrongTries()
    {
        for (var i = 0; i < _state.PendingAnswererIndicies.Count; i++)
        {
            var playerIndex = _state.PendingAnswererIndicies[i];

            if (playerIndex == _state.PendingAnswererIndex)
            {
                continue;
            }

            _gameActions.SendMessageWithArgs(Messages.WrongTry, playerIndex);
            _gameActions.SendMessageWithArgs(Messages.PlayerState, PlayerState.Lost, playerIndex);
        }
    }

    private void AskToDelete()
    {
        int playerIndex = -1;

        try
        {
            _state.ThemeDeleters.MoveNext();
            var currentDeleter = _state.ThemeDeleters.Current;

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
                    throw new Exception("indicies.Count == 0: " + _state.ThemeDeleters.GetRemoveLog());
                }

                currentDeleter.SetIndex(indicies.First());
            }

            playerIndex = currentDeleter.PlayerIndex;

            if (playerIndex < -1 || playerIndex >= _state.Players.Count)
            {
                throw new ArgumentException($"{nameof(playerIndex)}: {_state.ThemeDeleters.GetRemoveLog()}");
            }

            _state.ActivePlayer = _state.Players[playerIndex];

            RequestForThemeDelete();
        }
        catch (Exception exc)
        {
            _state.Host.SendError(new Exception(string.Format("AskToDelete {0}/{1}/{2}", _state.ThemeDeleters.Current.PlayerIndex, playerIndex, _state.Players.Count), exc));
        }
    }

    private void RequestForThemeDelete()
    {
        var msg = new StringBuilder(_state.ActivePlayer.Name)
            .Append(", ")
            .Append(GetRandomString(LO[nameof(R.DeleteTheme)]));

        _gameActions.ShowmanReplic(msg.ToString());

        var message = string.Join(Message.ArgsSeparator, Messages.Choose, 2);
        _state.IsOralNow = _state.IsOral && _state.ActivePlayer.IsHuman;

        var waitTime = _state.TimeSettings.ThemeSelection * 10;

        if (_state.IsOralNow)
        {
            _gameActions.SendMessage(message, _state.ShowMan.Name);
        }
        else if (!_state.ActivePlayer.IsConnected)
        {
            waitTime = 20;
        }

        if (CanPlayerAct())
        {
            _gameActions.SendMessage(message, _state.ActivePlayer.Name);
        }

        _state.ThemeIndexToDelete = -1;
        ScheduleExecution(Tasks.WaitDelete, waitTime);
        WaitFor(DecisionType.ThemeDeleting, waitTime, _state.Players.IndexOf(_state.ActivePlayer));
    }

    private void RequestForCurrentDeleter(ICollection<int> indicies)
    {
        for (var i = 0; i < _state.Players.Count; i++)
        {
            _state.Players[i].Flag = indicies.Contains(i);
        }

        // -- deprecated
        var msg = new StringBuilder(Messages.FirstDelete);

        for (var i = 0; i < _state.Players.Count; i++)
        {
            msg.Append(Message.ArgsSeparatorChar).Append(_state.Players[i].Flag ? '+' : '-');
        }

        _gameActions.SendMessage(msg.ToString(), _state.ShowMan.Name);
        // -- end
        AskToSelectPlayer(SelectPlayerReason.Deleter, _state.ShowMan.Name);

        var waitTime = _state.TimeSettings.ShowmanDecision * 10;
        ScheduleExecution(Tasks.WaitNext, waitTime, 1);
        WaitFor(DecisionType.NextPersonFinalThemeDeleting, waitTime, -1);
    }

    /// <summary>
    /// Определить следующего ставящего
    /// </summary>
    /// <returns>Стоит ли продолжать выполнение</returns>
    private bool DetectNextStaker()
    {
        var candidatesAll = Enumerable.Range(0, _state.Order.Length).Except(_state.Order).ToArray(); // Незадействованные игроки

        if (_state.OrderIndex < _state.Order.Length - 1)
        {
            // Ещё есть, из кого выбирать

            // Сначала отбросим тех, у кого недостаточно денег для ставки
            var candidates = candidatesAll.Where(n => _state.Players[n].StakeMaking);

            if (candidates.Count() > 1)
            {
                // У кандидатов должна быть минимальная сумма
                var minSum = candidates.Min(n => _state.Players[n].Sum);
                candidates = candidates.Where(n => _state.Players[n].Sum == minSum);
            }

            if (!candidates.Any()) // Никто из оставшихся не может перебить ставку
            {
                var ind = _state.OrderIndex;

                if (_state.OrderIndex + candidatesAll.Length > _state.Order.Length)
                {
                    throw new InvalidOperationException(
                        $"Invalid order index. Order index: {_state.OrderIndex}; " +
                        $"candidates length: {candidatesAll.Length}; order length: {_state.Order.Length}");
                }

                for (var i = 0; i < candidatesAll.Length; i++)
                {
                    _state.Order[ind + i] = candidatesAll[i];
                    CheckOrder(ind + i);
                    _state.Players[candidatesAll[i]].StakeMaking = false;
                }

                var passMsg = new MessageBuilder(Messages.PlayerState, PlayerState.Pass).AddRange(candidatesAll.Select(i => (object)i));
                _gameActions.SendMessage(passMsg.ToString());

                if (TryDetectStakesWinner())
                {
                    return false;
                }

                _state.OrderIndex = -1;
                AskStake(false);
                return false;
            }

            _state.IsWaiting = false;

            if (candidates.Count() == 1)
            {
                _state.Order[_state.OrderIndex] = candidates.First();
                CheckOrder(_state.OrderIndex);
            }
            else
            {
                // Showman should choose the next staker
                for (var i = 0; i < _state.Players.Count; i++)
                {
                    _state.Players[i].Flag = candidates.Contains(i);
                }

                // -- deprecated
                var msg = new StringBuilder(Messages.FirstStake);

                for (var i = 0; i < _state.Players.Count; i++)
                {
                    msg.Append(Message.ArgsSeparatorChar).Append(_state.Players[i].Flag ? '+' : '-');
                }

                _gameActions.SendMessage(msg.ToString(), _state.ShowMan.Name);
                // -- end
                AskToSelectPlayer(SelectPlayerReason.Staker, _state.ShowMan.Name);
                _state.OrderHistory.AppendLine("Asking showman for the next staker");

                var time = _state.TimeSettings.ShowmanDecision * 10;
                ScheduleExecution(Tasks.WaitNext, time);
                WaitFor(DecisionType.NextPersonStakeMaking, time, -1);
                return false;
            }
        }
        else
        {
            // Остался последний игрок, выбор очевиден
            var leftIndex = candidatesAll[0];
            _state.Order[_state.OrderIndex] = leftIndex;
            CheckOrder(_state.OrderIndex);
        }

        return true;
    }

    public void CheckOrder(int index)
    {
        if (index < 0 || index >= _state.Order.Length)
        {
            throw new ArgumentException($"Value {index} must be in [0; {_state.Order.Length}]", nameof(index));
        }

        var checkedValue = _state.Order[index];

        if (checkedValue == -1)
        {
            throw new Exception("_data.Order[index] == -1");
        }

        for (var i = 0; i < _state.Order.Length; i++)
        {
            var value = _state.Order[i];

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
        var cost = _state.Question.Price;

        try
        {
            _state.OrderHistory
                .Append($"AskStake: Order = {string.Join(",", _state.Order)};")
                .Append($" OrderIndex = {_state.OrderIndex};")
                .Append($" StakeMaking = {string.Join(",", _state.Players.Select(p => p.StakeMaking))}")
                .AppendLine();

            IncrementOrderIndex();

            if (_state.Order[_state.OrderIndex] == -1) // Необходимо определить следующего ставящего
            {
                if (!canDetectNextStakerGuard)
                {
                    throw new Exception("!canDetectNextStaker");
                }

                if (!DetectNextStaker())
                {
                    return;
                }

                _state.OrderHistory.Append($"NextStaker = {_state.Order[_state.OrderIndex]}").AppendLine();
            }

            var playerIndex = _state.Order[_state.OrderIndex];

            var others = _state.Players.Where((p, index) => index != playerIndex); // Other players
            
            if (others.All(p => !p.StakeMaking) && _state.Stake > -1) // Others cannot make stakes
            {
                // Staker cannot raise anymore
                ScheduleExecution(Tasks.AnnounceStakesWinner, 10);
                return;
            }

            if (playerIndex < 0 || playerIndex >= _state.Players.Count)
            {
                throw new ArgumentException($"Bad {nameof(playerIndex)} value {playerIndex}! It must be in [0; {_state.Players.Count - 1}]");
            }

            var activePlayer = _state.Players[playerIndex];
            var playerMoney = activePlayer.Sum;

            if (_state.Stake != -1 && playerMoney <= _state.Stake || !activePlayer.StakeMaking) // Could not make stakes
            {
                if (activePlayer.StakeMaking)
                {
                    activePlayer.StakeMaking = false;
                    _gameActions.SendMessageWithArgs(Messages.PersonStake, playerIndex, 2);
                }

                if (TryDetectStakesWinner())
                {
                    return;
                }

                ScheduleExecution(Tasks.AskStake, 5);
                return;
            }

            // Detecting possible stake outcomes

            // Only nominal
            if (_state.Stake == -1 && (playerMoney < cost || playerMoney == cost && others.All(p => playerMoney >= p.Sum)))
            {
                var s = new StringBuilder(activePlayer.Name)
                    .Append(", ").Append(LO[nameof(R.YouCanSayOnly)])
                    .Append(' ').Append(LO[nameof(R.Nominal)]);

                _gameActions.ShowmanReplic(s.ToString());

                _state.Stakes.StakerIndex = playerIndex;
                _state.Stake = cost;
                _gameActions.SendMessageWithArgs(Messages.PersonStake, playerIndex, 1, cost);
                ScheduleExecution(Tasks.AskStake, 5, force: true);
                return;
            }

            var minimumStake = (_state.Stake != -1 ? _state.Stake : cost) + _state.StakeStep;
            var minimumStakeAligned = (int)Math.Ceiling((double)minimumStake / _state.StakeStep) * _state.StakeStep;

            _state.StakeTypes = StakeTypes.AllIn | (_state.Stake == -1 ? StakeTypes.Nominal : StakeTypes.Pass);

            if (!_state.AllIn && playerMoney >= minimumStakeAligned)
            {
                _state.StakeTypes |= StakeTypes.Stake;
            }

            _state.StakeVariants[0] = _state.Stake == -1;
            _state.StakeVariants[1] = !_state.AllIn && playerMoney != cost && playerMoney > _state.Stake + _state.StakeStep;
            _state.StakeVariants[2] = !_state.StakeVariants[0];
            _state.StakeVariants[3] = true;

            _state.ActivePlayer = activePlayer;

            _state.IsOralNow = _state.IsOral && _state.ActivePlayer.IsHuman;

            var stakeMsg = new MessageBuilder(Messages.Stake);
            var stakeMsg2 = new MessageBuilder(Messages.Stake2);

            for (var i = 0; i < _state.StakeVariants.Length; i++)
            {
                stakeMsg.Add(_state.StakeVariants[i] ? '+' : '-');
            }

            stakeMsg2.Add(_state.StakeTypes);

            stakeMsg.Add(minimumStakeAligned);
            stakeMsg2.Add(minimumStakeAligned);
            stakeMsg2.Add(_state.StakeStep);

            var waitTime = _state.TimeSettings.StakeMaking * 10;

            if (CanPlayerAct())
            {
                _gameActions.SendMessage(stakeMsg.Build(), _state.ActivePlayer.Name);
                _gameActions.SendMessage(stakeMsg2.Build(), _state.ActivePlayer.Name);

                if (!_state.ActivePlayer.IsConnected)
                {
                    waitTime = 20;
                }
            }

            if (_state.IsOralNow)
            {
                stakeMsg.Add(_state.ActivePlayer.Sum); // Send maximum possible value to showman
                stakeMsg.Add(_state.ActivePlayer.Name);
                _gameActions.SendMessage(stakeMsg.Build(), _state.ShowMan.Name);

                stakeMsg2.Add(_state.ActivePlayer.Sum); // Send maximum possible value to showman
                stakeMsg2.Add(_state.ActivePlayer.Name);
                _gameActions.SendMessage(stakeMsg2.Build(), _state.ShowMan.Name);
            }

            var minimumStakeNew = _state.Stake != -1 ? _state.Stake + _state.StakeStep : cost;
            var minimumStakeAlignedNew = (int)Math.Ceiling((double)minimumStakeNew / _state.StakeStep) * _state.StakeStep;
            
            _state.StakeModes = StakeModes.AllIn;

            if (_state.Stake != -1)
            {
                _state.StakeModes |= StakeModes.Pass;
            }

            if (!_state.AllIn && playerMoney >= minimumStakeAlignedNew)
            {
                _state.StakeModes |= StakeModes.Stake;
            }

            var stakeLimit = new StakeSettings(minimumStakeAlignedNew, _state.ActivePlayer.Sum, _state.StakeStep);
            AskToMakeStake(StakeReason.HighestPlays, _state.ActivePlayer.Name, stakeLimit);

            _state.StakeType = null;
            _state.StakeSum = -1;
            ScheduleExecution(Tasks.WaitStake, waitTime);
            WaitFor(DecisionType.StakeMaking, waitTime, _state.Players.IndexOf(_state.ActivePlayer));
        }
        catch (Exception exc)
        {
            var orders = string.Join(",", _state.Order);
            var sums = string.Join(",", _state.Players.Select(p => p.Sum));
            var stakeMaking = string.Join(",", _state.Players.Select(p => p.StakeMaking));
            throw new Exception($"AskStake error {sums} {stakeMaking} {orders} {_state.Stake} {_state.OrderIndex} {_state.Players.Count} {_state.OrderHistory}", exc);
        }
    }

    internal bool TryDetectStakesWinner()
    {
        var stakerCount = _state.Players.Count(p => p.StakeMaking);

        if (stakerCount == 1) // Answerer is detected
        {
            for (var i = 0; i < _state.Players.Count; i++)
            {
                if (_state.Players[i].StakeMaking)
                {
                    _state.Stakes.StakerIndex = i;
                }
            }

            PlanExecution(Tasks.AnnounceStakesWinner, 10); // Using PlanExecution() as we could be interrupting normal flow
            return true;
        }

        return false;
    }

    private void AskToMakeStake(StakeReason reason, string name, StakeSettings limit)
    {
        var stakeReplic = new StringBuilder(name).Append(", ").Append(GetRandomString(LO[nameof(R.YourStake)]));
        _gameActions.ShowmanReplic(stakeReplic.ToString()); // TODO: REMOVE (localized by MessageCode)
        _gameActions.ShowmanReplicNew(MessageCode.MakeStake, name);

        AskToMakeStake(reason, new[] { (name, limit) });
    }

    private void AskToMakeStake(StakeReason reason, IEnumerable<(string name, StakeSettings limit)> persons)
    {
        _state.DecisionMakers.Clear();
        _state.StakeLimits.Clear();

        foreach (var (name, limit) in persons)
        {
            var stakeMessage = new MessageBuilder(Messages.AskStake, _state.StakeModes, limit.Minimum, limit.Maximum, limit.Step, reason);

            if (CanPlayerAct())
            {
                _gameActions.SendMessage(stakeMessage.Build(), name);
            }

            if (_state.IsOralNow)
            {
                stakeMessage.Add(name);
                _gameActions.SendMessage(stakeMessage.Build(), _state.ShowMan.Name);
            }

            _state.DecisionMakers.Add(name);
            _state.StakeLimits[name] = limit;
        }

        if (_state.IsOralNow)
        {
            _state.DecisionMakers.Add(_state.ShowMan.Name);
        }
    }

    private void IncrementOrderIndex()
    {
        var breakerGuard = 20; // Temp var

        var initialOrderIndex = _state.OrderIndex == -1 ? _state.Order.Length - 1 : _state.OrderIndex;

        // TODO: Rewrite as for
        do
        {
            _state.OrderIndex++;

            if (_state.OrderIndex == _state.Order.Length)
            {
                _state.OrderIndex = 0;
            }

            breakerGuard--;

            if (breakerGuard == 0)
            {
                throw new Exception($"{nameof(breakerGuard)} == {breakerGuard} ({initialOrderIndex})");
            }

        } while (_state.OrderIndex != initialOrderIndex &&
            _state.Order[_state.OrderIndex] != -1 &&
            !_state.Players[_state.Order[_state.OrderIndex]].StakeMaking);

        if (_state.OrderIndex == initialOrderIndex)
        {
            throw new Exception($"{nameof(_state.OrderIndex)} == {nameof(initialOrderIndex)} ({initialOrderIndex})");
        }

        _state.OrderHistory.AppendFormat("New order index: {0}", _state.OrderIndex).AppendLine();
    }

    private void OnStartAppellation()
    {
        if (_state.AppelaerIndex < 0 ||
            _state.AppelaerIndex >= _state.Players.Count ||
            _state.AppellationCallerIndex != -1 && (_state.AppellationCallerIndex < 0 || _state.AppellationCallerIndex >= _state.Players.Count))
        {
            _tasksHistory.AddLogEntry($"OnStartAppellation resumed ({_taskRunner.PrintOldTasks()})");
            ResumeExecution(40);
            return;
        }
        
        _gameActions.SendMessageWithArgs(Messages.Appellation, '+');

        var appelaer = _state.Players[_state.AppelaerIndex];
        var isAppellationForRightAnswer = _state.AppellationCallerIndex == -1;
        var appellationSource = isAppellationForRightAnswer ? appelaer : _state.Players[_state.AppellationCallerIndex];

        var given = LO[appelaer.IsMale ? nameof(R.HeGave) : nameof(R.SheGave)];
        var apellationReplic = string.Format(LO[nameof(R.PleaseCheckApellation)], given);

        string origin = isAppellationForRightAnswer
            ? LO[nameof(R.IsApellating)]
            : string.Format(LO[nameof(R.IsConsideringWrong)], appelaer.Name);

        _gameActions.ShowmanReplic($"{appellationSource} {origin}. {apellationReplic}");

        var validation2Message = BuildValidation2Message(appelaer.Name, appelaer.Answer ?? "", false, isAppellationForRightAnswer);

        _state.AppellationAwaitedVoteCount = 0;
        _state.AppellationTotalVoteCount = _state.Players.Count(p => p.IsConnected) + 1; // players and showman
        _state.AppellationPositiveVoteCount = 0;
        _state.AppellationNegativeVoteCount = 0;

        // Showman vote
        if (isAppellationForRightAnswer)
        {
            _state.AppellationNegativeVoteCount++;
        }
        else
        {
            _state.AppellationPositiveVoteCount++;
        }

        for (var i = 0; i < _state.Players.Count; i++)
        {
            if (i == _state.AppelaerIndex)
            {
                _state.Players[i].AppellationFlag = false;
                _state.AppellationPositiveVoteCount++;
            }
            else if (!isAppellationForRightAnswer && i == _state.AppellationCallerIndex)
            {
                _state.Players[i].AppellationFlag = false;
                _state.AppellationNegativeVoteCount++;
                _gameActions.SendMessageWithArgs(Messages.PersonApellated, i);
                _gameActions.SendMessageWithArgs(Messages.PlayerState, PlayerState.HasAnswered, i);
            }
            else if (_state.Players[i].IsConnected)
            {
                _state.AppellationAwaitedVoteCount++;
                _state.Players[i].AppellationFlag = true;
                _gameActions.SendMessage(validation2Message, _state.Players[i].Name);
            }
        }

        var waitTime = _state.AppellationAwaitedVoteCount > 0 ? _state.TimeSettings.ShowmanDecision * 10 : 1;
        ScheduleExecution(Tasks.WaitAppellationDecision, waitTime);
        WaitFor(DecisionType.Appellation, waitTime, -2);
    }

    internal int ResumeExecution(int resumeTime = 0) => _taskRunner.ResumeExecution(resumeTime, ShouldRunTimer());

    private void OnCheckAppellation()
    {
        try
        {
            if (_state.AppelaerIndex < 0 || _state.AppelaerIndex >= _state.Players.Count)
            {
                _tasksHistory.AddLogEntry($"CheckAppellation resumed ({_taskRunner.PrintOldTasks()})");
                return;
            }

            var votingForRight = _state.AppellationCallerIndex == -1;
            var positiveVoteCount = _state.AppellationPositiveVoteCount;
            var negativeVoteCount = _state.AppellationNegativeVoteCount;

            if (votingForRight && positiveVoteCount <= negativeVoteCount || !votingForRight && positiveVoteCount >= negativeVoteCount)
            {
                _gameActions.ShowmanReplic($"{LO[nameof(R.ApellationDenied)]}!");
                _tasksHistory.AddLogEntry($"CheckAppellation denied and resumed normally ({_taskRunner.PrintOldTasks()})");
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

            _tasksHistory.AddLogEntry($"CheckAppellation resumed normally ({_taskRunner.PrintOldTasks()})");
        }
        finally
        {
            if (_state.QuestionPlay.AppellationIndex >= _state.QuestionPlay.Appellations.Count || !ProcessNextAppellationRequest(false))
            {
                _gameActions.SendMessageWithArgs(Messages.Appellation, '-');
                ResumeExecution(40);
            }
            else
            {
                ScheduleExecution(Tasks.StartAppellation, 10);
            }
        }
    }

    private void ApplyAppellationForRightAnswer()
    {
        var appelaer = _state.Players[_state.AppelaerIndex];

        var themeName = _state.Theme.Name;
        var questionText = _state.Question?.GetText();

        // Add appellated answer to game report
        var answerInfo = _state.GameResultInfo.RejectedAnswers.FirstOrDefault(
            answer =>
                answer.ThemeName == themeName
                && answer.QuestionText == questionText
                && answer.ReportText == appelaer.Answer);

        if (answerInfo != null)
        {
            _state.GameResultInfo.RejectedAnswers.Remove(answerInfo);
        }

        _state.GameResultInfo.ApellatedAnswers.Add(new QuestionReport
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

        var right = new List<object>();
        var wrong = new List<object>();
        var passed = new List<object>();

        // Track for positive appellation cleanup
        var hadPositiveOutcome = false;
        var appelaerHistoryIndex = -1;

        for (var i = 0; i < _state.QuestionHistory.Count; i++)
        {
            var historyItem = _state.QuestionHistory[i];
            var index = historyItem.PlayerIndex;

            if (index < 0 || index >= _state.Players.Count)
            {
                continue;
            }

            var player = _state.Players[index];

            if (isVotingForRightAnswer && singleAnswerer && index != _state.AppelaerIndex)
            {
                if (!change)
                {
                    continue;
                }

                if (historyItem.IsRight)
                {
                    UndoRightSum(player, historyItem.Sum);
                    hadPositiveOutcome = true;
                }
                else
                {
                    UndoWrongSum(player, historyItem.Sum);
                }

                passed.Add(index);
            }
            else if (index == _state.AppelaerIndex)
            {
                appelaerHistoryIndex = i;

                if (singleAnswerer)
                {
                    change = true;

                    if (historyItem.IsRight)
                    {
                        // Negative appellation: changing right to wrong
                        UndoRightSum(player, historyItem.Sum);
                        SubtractWrongSum(player, _state.CurPriceWrong);

                        wrong.Add(index);
                    }
                    else
                    {
                        // Positive appellation: changing wrong to right
                        UndoWrongSum(player, historyItem.Sum);
                        AddRightSum(player, _state.CurPriceRight);

                        right.Add(index);

                        // TODO: that should be handled by question selection strategy
                        if (Engine.CanMoveBack) // Not the beginning of a round
                        {
                            _state.ChooserIndex = index;
                            _gameActions.SendMessageWithArgs(Messages.SetChooser, _state.ChooserIndex);
                        }
                    }
                }
                else
                {
                    var stake = player.PersonalStake;

                    if (historyItem.IsRight)
                    {
                        UndoRightSum(player, historyItem.Sum);
                        SubtractWrongSum(player, stake);

                        wrong.Add(index);
                    }
                    else
                    {
                        UndoWrongSum(player, historyItem.Sum);
                        AddRightSum(player, stake);

                        right.Add(index);
                    }
                }
            }
        }

        // After processing all players, cleanup for positive appellations
        if (isVotingForRightAnswer && singleAnswerer && appelaerHistoryIndex >= 0)
        {
            // Collect player indices that will be removed from history
            var removedPlayerIndices = new HashSet<int>();
            for (var i = appelaerHistoryIndex + 1; i < _state.QuestionHistory.Count; i++)
            {
                removedPlayerIndices.Add(_state.QuestionHistory[i].PlayerIndex);
            }

            // Remove all entries after the appelaer from QuestionHistory
            if (_state.QuestionHistory.Count > appelaerHistoryIndex + 1)
            {
                _state.QuestionHistory.RemoveRange(appelaerHistoryIndex + 1, _state.QuestionHistory.Count - appelaerHistoryIndex - 1);
            }

            // Remove pending appellations for removed players
            _state.QuestionPlay.Appellations.RemoveAll(appellation =>
            {
                var (appellationSource, isAppellationForRightAnswer) = appellation;
                
                // Find if this appellation is from a removed player
                for (var i = 0; i < _state.Players.Count; i++)
                {
                    if (_state.Players[i].Name == appellationSource && removedPlayerIndices.Contains(i))
                    {
                        // Remove positive appellations for removed players
                        if (isAppellationForRightAnswer)
                        {
                            return true;
                        }

                        // If there was a positive outcome being erased, also remove negative appellations
                        if (hadPositiveOutcome)
                        {
                            return true;
                        }
                    }
                }

                return false;
            });
        }

        if (right.Any())
        {
            var rightMsg = new MessageBuilder(Messages.PlayerState, PlayerState.Right).AddRange(right);
            _gameActions.SendMessage(rightMsg.ToString());
        }

        if (wrong.Any())
        {
            var wrongMsg = new MessageBuilder(Messages.PlayerState, PlayerState.Wrong).AddRange(wrong);
            _gameActions.SendMessage(wrongMsg.ToString());
        }

        if (passed.Any())
        {
            var passMsg = new MessageBuilder(Messages.PlayerState, PlayerState.Pass).AddRange(passed);
            _gameActions.SendMessage(passMsg.ToString());
        }
    }

    private void WaitFor(DecisionType decision, int time, int person, bool isWaiting = true)
    {
        _state.TimerStartTime[2] = DateTime.UtcNow;

        _state.IsWaiting = isWaiting;
        _state.Decision = decision;

        _gameActions.SendMessageWithArgs(Messages.Timer, 2, MessageParams.Timer_Go, time, person);
    }

    private void OnPackage(Package package, int stage)
    {
        var informed = false;

        var baseTime = 0;

        if (stage == 1)
        {
            var authors = _state.PackageDoc.ResolveAuthors(package.Info.Authors);

            if (package.Name != Constants.RandomIndicator && authors.Length > 0)
            {
                informed = true;
                var msg = new MessageBuilder(Messages.PackageAuthors).AddRange(authors);
                _gameActions.SendVisualMessage(msg);
                baseTime = 20;
            }
            else
            {
                stage++;
            }
        }

        if (stage == 2)
        {
            var packageName = package.Name == Constants.RandomIndicator ? LO[nameof(R.RandomPackageName)] : package.Name;

            informed = true;

            var messageBuilder = new MessageBuilder(Messages.Package).Add(packageName);

            var logoItem = package.LogoItem;

            if (logoItem != null)
            {
                var (success, globalUri, _, error) = TryShareContent(logoItem);

                if (success && globalUri != null)
                {
                    messageBuilder.Add(ContentTypes.Image).Add(globalUri);
                }
                else if (error != null)
                {
                    messageBuilder.Add(ContentTypes.Text).Add(error);
                }
            }

            _gameActions.SendVisualMessage(messageBuilder);

            var sources = _state.PackageDoc.ResolveSources(package.Info.Sources);

            if (sources.Count > 0)
            {
                var msg = new MessageBuilder(Messages.PackageSources).AddRange(sources);
                _gameActions.SendMessage(msg.ToString());
            }

            if (!string.IsNullOrWhiteSpace(package.Date))
            {
                _gameActions.SendMessageWithArgs(Messages.PackageDate, package.Date);
            }
        }

        if (stage == 3)
        {
            if (package.Info.Comments.Text.Length > 0 && package.Name != Constants.RandomIndicator)
            {
                informed = true;
                _gameActions.SendVisualMessageWithArgs(Messages.PackageComments, package.Info.Comments.Text.EscapeNewLines());

                baseTime = GetReadingDurationForTextLength(package.Info.Comments.Text.Length);
            }
            else
            {
                stage++;
            }
        }

        if (stage == 4)
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

        if (stage < 4)
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
        var baseTime = 20;

        if (stage == 1)
        {
            informed = true;
            _gameActions.InformSums();

            var roundIndex = Engine.RoundIndex;
            var roundName = round.Name;

            _state.Stage = GameStage.Round;
            OnStageChanged(GameStages.Round, roundName, roundIndex + 1, _state.Rounds.Length);

            _gameActions.InformRound(roundName, roundIndex, _state.RoundStrategy);
            _gameActions.InformRoundContent();
            _state.InformStages |= InformStages.RoundContent;

            var authors = _state.PackageDoc.ResolveAuthors(round.Info.Authors);

            if (authors.Length > 0)
            {
                var msg = new MessageBuilder(Messages.RoundAuthors).AddRange(authors);
                _gameActions.SendMessage(msg.ToString());
            }

            var sources = _state.PackageDoc.ResolveSources(round.Info.Sources);

            if (sources.Count > 0)
            {
                var msg = new MessageBuilder(Messages.RoundSources).AddRange(sources);
                _gameActions.SendMessage(msg.ToString());
            }
        }

        if (stage == 2)
        {
            if (round.Info.Comments.Text.Length > 0)
            {
                informed = true;
                _gameActions.SendVisualMessageWithArgs(Messages.RoundComments, round.Info.Comments.Text.EscapeNewLines());

                baseTime = GetReadingDurationForTextLength(round.Info.Comments.Text.Length);
            }
            else
            {
                stage++;
            }
        }

        var adShown = false;

        if (stage == 3)
        {
            // Showing advertisement
            try
            {
                var ad = _state.Host.GetAd(LO.Culture.TwoLetterISOLanguageName, out int adId);

                if (!string.IsNullOrEmpty(ad))
                {
                    informed = true;

                    _gameActions.SendMessageWithArgs(Messages.Ads, ad);

#if !DEBUG
                    // Advertisement could not be skipped
                    _state.MoveNextBlocked = !_state.Settings.AppSettings.Managed;
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
                _state.Host.SendError(exc);
                stage++;
            }
        }

        if (stage < 3)
        {
            ScheduleExecution(Tasks.Round, baseTime + Random.Shared.Next(10), stage + 1);
        }
        else if (informed)
        {
            ScheduleExecution(Tasks.MoveNext, (adShown ? 40 : 20) + Random.Shared.Next(10));
        }
        else
        {
            ScheduleExecution(Tasks.MoveNext, 1, force: true);
        }
    }

    internal void SetAnswererAsActive()
    {
        _state.AnswererIndex = _state.ChooserIndex;
        _state.QuestionPlay.SetSingleAnswerer(_state.ChooserIndex);

        _gameActions.SendMessageWithArgs(Messages.SetChooser, _state.ChooserIndex, '+');

        ScheduleExecution(Tasks.MoveNext, 5);
    }

    internal void SetAnswererByActive(bool canGiveThemselves)
    {
        if (_state.ChooserIndex == -1)
        {
            _state.ChooserIndex = DetectPlayerIndexWithLowestSum();
        }

        if (_state.Chooser == null)
        {
            throw new InvalidOperationException("_data.Chooser == null");
        }

        for (var i = 0; i < _state.Players.Count; i++)
        {
            _state.Players[i].Flag = true;
        }

        if (!canGiveThemselves)
        {
            _state.Chooser.Flag = false;
        }

        var optionCount = _state.Players.Count(player => player.Flag);

        if (optionCount == 1)
        {
            for (var i = 0; i < _state.Players.Count; i++)
            {
                if (_state.Players[i].Flag)
                {
                    _state.ChooserIndex = _state.AnswererIndex = i;
                    _state.QuestionPlay.SetSingleAnswerer(i);
                    _gameActions.SendMessageWithArgs(Messages.SetChooser, _state.ChooserIndex);
                }
            }

            _gameActions.ShowmanReplic($"{_state.Answerer.Name}, {LO[nameof(R.CatIsYours)]}!");
            ScheduleExecution(Tasks.MoveNext, 10);
        }
        else
        {
            ScheduleExecution(Tasks.AskToSelectQuestionAnswerer, 10 + Random.Shared.Next(10), force: true);
        }
    }

    internal void SetAnswerersAll()
    {
        var allConnectedIndicies = new List<int>();
        var allDisconnectedIndicies = new List<object>();

        var hasConnectedPlayers = _state.Players.Any(p => p.IsConnected);

        for (var i = 0; i < _state.Players.Count; i++)
        {
            if (_state.Players[i].IsConnected || !hasConnectedPlayers)
            {
                allConnectedIndicies.Add(i);
            }
            else
            {
                allDisconnectedIndicies.Add(i);
            }
        }

        _state.QuestionPlay.SetMultipleAnswerers(allConnectedIndicies);
        
        var msg = new MessageBuilder(Messages.PlayerState, PlayerState.Answering).AddRange(allConnectedIndicies.Select(i => (object)i));
        _gameActions.SendMessage(msg.ToString());

        if (allDisconnectedIndicies.Count > 0)
        {
            msg = new MessageBuilder(Messages.PlayerState, PlayerState.Pass).AddRange(allDisconnectedIndicies);
            _gameActions.SendMessage(msg.ToString());
        }

        ScheduleExecution(Tasks.MoveNext, 5);
    }

    internal void OnButtonPressStart()
    {
        _state.Decision = DecisionType.Pressing;
        _gameActions.SendMessageWithArgs(Messages.Try, MessageParams.Try_NotFinished);

        SendTryToPlayers();
    }

    internal void OnSetTheme(string themeName)
    {
        if (string.IsNullOrWhiteSpace(themeName))
        {
            themeName = _state.Theme?.Name ?? "";
        }

        _gameActions.SendMessageWithArgs(Messages.QuestionCaption, themeName);

        var s = new StringBuilder(LO[nameof(R.Theme)]).Append(": ").Append(themeName);

        _gameActions.ShowmanReplic(s.ToString());

        ScheduleExecution(Tasks.MoveNext, 15);
    }

    // TODO: think about always using only complex answers as simple answer is a subset of a complex one
    internal void OnSimpleAnswer(string answer)
    {
        var normalizedAnswer = answer.LeaveFirst(MaxAnswerLength);
        _gameActions.SendVisualMessageWithArgs(Messages.RightAnswer, ContentTypes.Text, normalizedAnswer);

        var answerTime = GetReadingDurationForTextLength(normalizedAnswer.Length)
            + _state.TimeSettings.RightAnswer * 10;
        
        ScheduleExecution(Tasks.MoveNext, answerTime);
    }

    // TODO: There are two different messages to inform rigt answer options, refactor this

    internal void OnComplexAnswer()
    {
        var answer = _state.QuestionPlay.RightAnswers.FirstOrDefault() ?? "";

        if (_state.QuestionPlay.AnswerOptions != null)
        {
            OnRightAnswerOptionCore(answer);
            var answerIndex = Array.FindIndex(_state.QuestionPlay.AnswerOptions, o => o.Label == answer);

            if (answerIndex > -1)
            {
                _gameActions.SendMessageWithArgs(Messages.ContentState, ContentPlacements.Screen, answerIndex + 1, ItemState.Right);
            }
        }

        _gameActions.SendMessageWithArgs(Messages.RightAnswerStart, ContentTypes.Text, answer);
    }

    internal void OnRightAnswerOption(string rightOptionLabel)
    {
        OnRightAnswerOptionCore(rightOptionLabel);
        _gameActions.SendMessageWithArgs(Messages.RightAnswer, ContentTypes.Text, rightOptionLabel);
        var answerTime = _state.TimeSettings.RightAnswer;
        answerTime = (answerTime == 0 ? 2 : answerTime) * 10;
        ScheduleExecution(Tasks.MoveNext, answerTime);
    }

    internal void OnRightAnswerOptionCore(string rightOptionLabel)
    {
        if (_state.QuestionPlay.AnswerOptions != null && !_state.QuestionPlay.LayoutShown)
        {
            OnAnswerOptions();
            _state.QuestionPlay.LayoutShown = true;
        }

        if (_state.QuestionPlay.AnswerOptions != null && !_state.QuestionPlay.AnswerOptionsShown)
        {
            for (var i = 0; i < _state.QuestionPlay.AnswerOptions.Length; i++)
            {
                InformAnswerOption(i);
            }

            _state.QuestionPlay.AnswerOptionsShown = true;
        }

        _state.RightOptionLabel = rightOptionLabel;
    }

    private bool DetectRoundTimeout()
    {
        var roundDuration = DateTime.UtcNow.Subtract(_state.TimerStartTime[0]).TotalMilliseconds / 100;

        if (_state.Stage == GameStage.Round && roundDuration >= _state.TimeSettings.Round * 10)
        {
            // Round timeout
            _gameActions.SendMessageWithArgs(Messages.Timer, 0, MessageParams.Timer_Stop);
            return true;
        }

        return false;
    }

    internal bool OnQuestionEnd()
    {
        var timeout = DetectRoundTimeout();
        var nextTaskTime = 1;

        if (HaveMultipleAnswerers() && _state.QuestionPlay.ValidateAfterRightAnswer)
        {
            var validationResult = ValidatePlayersAnswers();

            if (validationResult.HasValue)
            {
                if (validationResult == true)
                {
                    return timeout;
                }

                nextTaskTime = 30;
            }
        }

        PostprocessQuestion(nextTaskTime);
        return timeout;
    }

    private bool? ValidatePlayersAnswers()
    {
        if (_state.AnnouncedAnswerersEnumerator != null)
        {
            _state.AnnouncedAnswerersEnumerator.Reset();

            if (_state.QuestionPlay.HiddenStakes)
            {
                ScheduleExecution(Tasks.AnnouncePostStakeWithAnswerOptions, 1);
                return true;
            }
            else
            {
                CalculateOutcomesByRightAnswerOption();
                return false;
            }
        }

        return null;
    }

    private void CalculateOutcomesByRightAnswerOption()
    {
        if (_state.AnnouncedAnswerersEnumerator == null)
        {
            return;
        }

        while (_state.AnnouncedAnswerersEnumerator.MoveNext())
        {
            var answererIndex = _state.AnnouncedAnswerersEnumerator.Current;

            if (answererIndex < 0 || answererIndex >= _state.Players.Count)
            {
                continue;
            }

            var answerer = _state.Players[answererIndex];
            var isRight = answerer.Answer == _state.RightOptionLabel;

            var message = new MessageBuilder(Messages.Person);
            int outcome;

            if (isRight)
            {
                message.Add('+');
                AddRightSum(answerer, _state.CurPriceRight);
                outcome = _state.CurPriceRight;
            }
            else
            {
                message.Add('-');
                SubtractWrongSum(answerer, _state.CurPriceWrong);
                outcome = _state.CurPriceWrong;
            }

            message.Add(answererIndex).Add(outcome);
            _gameActions.SendMessage(message.ToString());
        }

        _gameActions.InformSums();
    }

    // TODO: OnAnnouncePrice and OnSelectPrice should utilize the same selection strategy
    internal void OnAnnouncePrice(NumberSet availableRange)
    {
        // TODO: send QUESTION_PRICE_RANGE message instead of this
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
            if (availableRange.Step > 0)
            {
                s.Append(
                    $"{LO[nameof(R.From)]} {Notion.FormatNumber(availableRange.Minimum)} {LO[nameof(R.UpTo)]} {Notion.FormatNumber(availableRange.Maximum)} " +
                    $"{LO[nameof(R.WithStepOf)]} {Notion.FormatNumber(availableRange.Step)} ({LO[nameof(R.YourChoice)]})");
            }
            else
            {
                s.Append($"{Notion.FormatNumber(availableRange.Minimum)} {LO[nameof(R.Or)]} {Notion.FormatNumber(availableRange.Maximum)} ({LO[nameof(R.YourChoice)]})");
            }
        }

        _gameActions.ShowmanReplic(s.ToString());
        ScheduleExecution(Tasks.MoveNext, 20);
    }

    internal void OnSelectPrice(NumberSet availableRange)
    {
        if (availableRange.Maximum == 0)
        {
            if (_minRoundPrice == _maxRoundPrice)
            {
                _state.CurPriceRight = _minRoundPrice;
                _state.CurPriceWrong = _state.CurPriceRight;
                _gameActions.SendMessageWithArgs(Messages.PersonStake, _state.AnswererIndex, 1, _state.CurPriceRight);
                ScheduleExecution(Tasks.MoveNext, 1);
            }
            else
            {
                _state.CurPriceRight = -1;
                _state.StakeRange = new StakeSettings(_minRoundPrice, _maxRoundPrice, _maxRoundPrice - _minRoundPrice);

                ScheduleExecution(Tasks.AskToSelectQuestionPrice, 1, force: true);
            }
        }
        else if (availableRange.Minimum == availableRange.Maximum)
        {
            _state.CurPriceWrong = _state.CurPriceRight = availableRange.Minimum;
            _gameActions.SendMessageWithArgs(Messages.PersonStake, _state.AnswererIndex, 1, _state.CurPriceRight);
            ScheduleExecution(Tasks.MoveNext, 1);
        }
        else
        {
            _state.CurPriceRight = -1;
            _state.StakeRange = new StakeSettings(availableRange.Minimum, availableRange.Maximum, availableRange.Step);

            ScheduleExecution(Tasks.AskToSelectQuestionPrice, 1, force: true);
        }
    }

    internal void SetAnswererByHighestVisibleStake()
    {
        if (_state.Question == null)
        {
            throw new InvalidOperationException("_data.Question == null");
        }

        var nominal = _state.Question.Price;

        if (_state.ChooserIndex == -1)
        {
            _state.ChooserIndex = DetectPlayerIndexWithLowestSum(); // TODO: set chooser index at the beginning of round
        }

        _state.Order = new int[_state.Players.Count];
        var passes = new List<object>();

        for (var i = 0; i < _state.Players.Count; i++)
        {
            var canMakeStake = i == _state.ChooserIndex || _state.Players[i].Sum > nominal;
            _state.Players[i].StakeMaking = canMakeStake;
            _state.Order[i] = -1;

            if (!canMakeStake)
            {
                passes.Add(i);
            }
        }

        if (passes.Count > 0)
        {
            var passMsg = new MessageBuilder(Messages.PlayerState, PlayerState.Pass).AddRange(passes);
            _gameActions.SendMessage(passMsg.ToString());
        }

        _state.Stake = -1;
        Stakes.Reset(_state.ChooserIndex);
        _state.Order[0] = _state.ChooserIndex;
        _state.OrderHistory.Clear();

        _state.OrderHistory.Append("Stake making. Initial state. ")
            .AppendLine()
            .Append("Sums: ")
            .Append(string.Join(",", _state.Players.Select(p => p.Sum)))
            .AppendLine()
            .Append("StakeMaking: ")
            .Append(string.Join(",", _state.Players.Select(p => p.StakeMaking)))
            .AppendLine()
            .Append(" Order: ")
            .Append(string.Join(",", _state.Order))
            .AppendLine()
            .Append(" Nominal: ")
            .Append(_state.CurPriceRight)
            .AppendLine();

        _state.AllIn = false;
        _state.OrderIndex = -1;
        ScheduleExecution(Tasks.AskStake, 10);
    }

    private int DetectPlayerIndexWithLowestSum()
    {
        var minSum = _state.Players.Min(p => p.Sum);
        return _state.Players.TakeWhile(p => p.Sum != minSum).Count();
    }

    internal void SetAnswerersByAllHiddenStakes()
    {
        var answerers = new List<int>();
        var passes = new List<object>();

        var hasConnectedPlayers = _state.Players.Any(p => p.IsConnected);

        for (var i = 0; i < _state.Players.Count; i++)
        {
            if ((!hasConnectedPlayers || _state.Players[i].IsConnected) &&
                (_state.Players[i].Sum > 0 || _state.Settings.AppSettings.AllowEveryoneToPlayHiddenStakes))
            {
                answerers.Add(i);
            }
            else
            {
                passes.Add(i);
            }
        }

        var msg = new MessageBuilder(Messages.PlayerState, PlayerState.Answering).AddRange(answerers.Select(i => (object)i));
        _gameActions.SendMessage(msg.ToString());

        if (passes.Count > 0)
        {
            var passMsg = new MessageBuilder(Messages.PlayerState, PlayerState.Pass).AddRange(passes);
            _gameActions.SendMessage(passMsg.ToString());
        }

        _state.QuestionPlay.SetMultipleAnswerers(answerers);
        _state.QuestionPlay.HiddenStakes = true;
        AskHiddenStakes();
    }

    internal void OnMultiplyPrice()
    {
        if (_state.ChooserIndex == -1)
        {
            _state.ChooserIndex = DetectPlayerIndexWithLowestSum(); // TODO: set chooser index at the beginning of round
        }

        var factor = _state.Settings.AppSettings.QuestionForYourselfFactor;

        _state.CurPriceRight *= factor;
        _state.CurPriceWrong *= factor;

        if (factor != 1 || _state.CurPriceRight != _state.CurPriceWrong)
        {
            var replic = string.Format(
                LO[nameof(R.QuestionForYourselfInfo)],
                Notion.FormatNumber(_state.CurPriceRight),
                Notion.FormatNumber(_state.CurPriceWrong),
                factor);

            _gameActions.ShowmanReplic($"{_state.Chooser!.Name}, {replic}");
        }

        _gameActions.SendMessageWithArgs(Messages.PersonStake, _state.AnswererIndex, 1, _state.CurPriceRight, _state.CurPriceWrong);

        ScheduleExecution(Tasks.MoveNext, 20);
    }

    internal void AcceptQuestion()
    {
        if (_state.Answerer == null)
        {
            throw new InvalidOperationException("_data.Answerer == null");
        }

        _gameActions.ShowmanReplic(LO[nameof(R.EasyCat)]);
        _gameActions.SendMessageWithArgs(Messages.Person, '+', _state.AnswererIndex, _state.CurPriceRight);

        AddRightSum(_state.Answerer, _state.CurPriceRight);
        _state.ChooserIndex = _state.AnswererIndex;
        _gameActions.SendMessageWithArgs(Messages.SetChooser, _state.ChooserIndex);
        _gameActions.InformSums();

        _state.SkipQuestion?.Invoke();
        ScheduleExecution(Tasks.MoveNext, 20, 1);
    }

    internal void OnAnswerOptions()
    {
        _gameActions.InformLayout();
        _state.InformStages |= InformStages.Layout;
        _state.LastVisualMessage = null;
        _state.ComplexVisualState = new IReadOnlyList<string>[1 + (_state.QuestionPlay.AnswerOptions?.Length ?? 0)];
    }

    internal void ShowAnswerOptions(Action? continuation)
    {
        if (_state.QuestionPlay.AnswerOptions == null)
        {
            throw new InvalidOperationException("AnswerOptions == null");
        }

        var nextTask = _state.QuestionPlay.AnswerOptions.Length > 0 ? Tasks.ShowNextAnswerOption : Tasks.MoveNext;
        ScheduleExecution(nextTask, 1, 0);
        _continuation = continuation;
    }

    internal void ShowNextAnswerOption(int optionIndex)
    {
        int contentDuration = InformAnswerOption(optionIndex);

        var nextTask = optionIndex + 1 < _state.QuestionPlay.AnswerOptions.Length ? Tasks.ShowNextAnswerOption : Tasks.MoveNext;
        ScheduleExecution(nextTask, _state.Settings.AppSettings.DisplayAnswerOptionsOneByOne ? contentDuration : 1, optionIndex + 1);
    }

    internal int InformAnswerOption(int optionIndex)
    {
        if (_state.QuestionPlay.AnswerOptions == null)
        {
            throw new InvalidOperationException("AnswerOptions == null");
        }

        var answerOption = _state.QuestionPlay.AnswerOptions[optionIndex];

        var messageBuilder = new MessageBuilder(Messages.Content)
            .Add(ContentPlacements.Screen)
            .Add(optionIndex + 1)
            .Add(answerOption.Label);

        int contentDuration;

        if (answerOption.Content.Type == ContentTypes.Text)
        {
            messageBuilder.Add(ContentTypes.Text).Add(answerOption.Content.Value.EscapeNewLines());
            contentDuration = Math.Max(10, GetReadingDurationForTextLength(answerOption.Content.Value.Length));
        }
        else
        {
            var (success, globalUri, _, error) = TryShareContent(answerOption.Content);

            if (!success || globalUri == null)
            {
                messageBuilder.Add(ContentTypes.Text).Add(error ?? string.Format(LO[nameof(R.MediaNotFound)], globalUri));
            }
            else
            {
                messageBuilder.Add(answerOption.Content.Type).Add(globalUri);
            }

            contentDuration = 10;
        }

        _gameActions.SendMessage(messageBuilder.ToString());

        if (_state.ComplexVisualState != null && optionIndex + 1 < _state.ComplexVisualState.Length)
        {
            _state.ComplexVisualState[optionIndex + 1] = new string[] { messageBuilder.ToString() };
        }

        return contentDuration;
    }

    internal void OnComplexContent(Dictionary<string, List<ContentItem>> contentTable)
    {
        var contentTime = -1;
        var registeredMediaPlay = false;
        var visualState = new List<string>();

        foreach (var (placement, contentList) in contentTable)
        {
            var contentListDuration = 0;

            var messageBuilder = new MessageBuilder(Messages.Content).Add(placement);

            foreach (var contentItem in contentList)
            {
                messageBuilder.Add(0); // LayoutId = 0 for this content

                int duration;

                if (contentItem.Type == ContentTypes.Text)
                {
                    messageBuilder.Add(contentItem.Type).Add(contentItem.Value.EscapeNewLines());

                    duration = contentItem.Duration > TimeSpan.Zero
                        ? (int)(contentItem.Duration.TotalMilliseconds / 100)
                        : GetContentItemDefaultDuration(contentItem);

                    if (_state.QuestionPlay.IsAnswer)
                    {
                        duration += _state.TimeSettings.RightAnswer * 10;
                    }
                }
                else
                {
                    var (success, globalUri, _, error) = TryShareContent(contentItem);

                    if (!success || globalUri == null)
                    {
                        var errorText = error ?? string.Format(LO[nameof(R.MediaNotFound)], globalUri);
                        messageBuilder.Add(ContentTypes.Text).Add(errorText);
                        duration = DefaultMediaTime + TimeSettings.TimeForMediaDelay * 10;
                    }
                    else
                    {
                        messageBuilder.Add(contentItem.Type).Add(globalUri);

                        if ((contentItem.Type == ContentTypes.Audio || contentItem.Type == ContentTypes.Video) && !registeredMediaPlay)
                        {
                            registeredMediaPlay = true;
                            _state.IsPlayingMedia = true;
                            _state.IsPlayingMediaPaused = false;

                            _state.QuestionPlay.MediaContentCompletions[(contentItem.Type, globalUri)] = new Completion(_state.ActiveHumanCount);
                            _completion = ClearMediaContent;
                        }

                        duration = contentItem.Duration > TimeSpan.Zero
                            ? (int)(contentItem.Duration.TotalMilliseconds / 100)
                            : GetContentItemDefaultDuration(contentItem);
                    }
                }

                contentListDuration += duration;
            }

            var message = messageBuilder.ToString();
            _gameActions.SendMessage(message);
            visualState.Add(message);

            contentTime = Math.Max(contentTime, contentListDuration);
        }

        _state.ComplexVisualState ??= new IReadOnlyList<string>[1];
        _state.ComplexVisualState[0] = visualState;
        _state.IsPartial = false;
        _state.AtomStart = DateTime.UtcNow;
        _state.AtomTime = contentTime;
        ScheduleExecution(Tasks.MoveNext, contentTime);
        _state.TimeThinking = 0.0;
    }

    private static string GetRandomString(string resource) => Random.Shared.GetRandomString(resource);

    private int GetContentItemDefaultDuration(ContentItem contentItem) => contentItem.Type switch
    {
        ContentTypes.Text => GetReadingDurationForTextLength(contentItem.Value.Length),
        ContentTypes.Image or ContentTypes.Html => _state.TimeSettings.Image * 10,
        ContentTypes.Audio or ContentTypes.Video => DefaultAudioVideoTime,
        _ => 0,
    };

    private void AddRightSum(GamePlayerAccount player, int sum)
    {
        player.Sum += sum;

        var statistic = GetStatistic(player.Name);

        statistic.RightAnswerCount++;
        statistic.RightTotal += sum;
    }

    private void SubtractWrongSum(GamePlayerAccount player, int sum)
    {
        player.Sum -= sum;

        var statistic = GetStatistic(player.Name);
        statistic.WrongAnswerCount++;
        statistic.WrongTotal += sum;
    }

    private void UndoRightSum(GamePlayerAccount player, int sum)
    {
        player.Sum -= sum;

        var statistic = GetStatistic(player.Name);
        statistic.RightAnswerCount--;
        statistic.RightTotal -= sum;
    }

    private void UndoWrongSum(GamePlayerAccount player, int sum)
    {
        player.Sum += sum;

        var statistic = GetStatistic(player.Name);
        statistic.WrongAnswerCount--;
        statistic.WrongTotal -= sum;
    }

    private PlayerStatistic GetStatistic(string name)
    {
        if (!_state.Statistics.TryGetValue(name, out var statistic))
        {
            _state.Statistics[name] = statistic = new PlayerStatistic();
        }

        return statistic;
    }

    internal void OnNumericAnswer() => _gameActions.InformAnswerDeviation(_state.QuestionPlay.NumericAnswerDeviation);

    internal void OnQuestionStart()
    {
        if (_state.Settings.AppSettings.HintShowman)
        {
            // TODO: use SendAnswerInfoToShowman()
            _gameActions.SendMessage(string.Join(Message.ArgsSeparator, Messages.Hint, _state.QuestionPlay.RightAnswers.FirstOrDefault() ?? ""), _state.ShowMan.Name);
        }

        SendQuestionAnswersToShowman();
    }
}

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

    private TimeSettings TimeSettings => _data.Settings.AppSettings.TimeSettings;

    public SIEngine.GameEngine Engine { get; } // TODO: remove dependency on GameEngine

    public event Action<GameLogic, GameStages, string, int, int>? StageChanged;

    public event Action<string, int, int>? AdShown;

    internal void OnStageChanged(
        GameStages stage,
        string stageName,
        int progressCurrent = 0,
        int progressTotal = 0) => StageChanged?.Invoke(this, stage, stageName, progressCurrent, progressTotal);

    internal void OnAdShown(int adId) =>
        AdShown?.Invoke(LO.Culture.TwoLetterISOLanguageName, adId, _data.AllPersons.Values.Count(p => p.IsHuman));

    private readonly IFileShare _fileShare;
    private readonly TaskRunner<Tasks> _taskRunner;

    private StopReason _stopReason = StopReason.None;

    internal StopReason StopReason => _stopReason;

    private int _leftTime;

    internal TaskRunner<Tasks> Runner => _taskRunner;

    internal IPinHelper? PinHelper { get; }

    internal StakesPlugin Stakes { get; }

    private readonly GameData _data;

    public GameLogic(
        GameData data,
        GameActions gameActions,
        SIEngine.GameEngine engine,
        ILocalizer localizer,
        IFileShare fileShare,
        IPinHelper? pinHelper)
    {
        _data = data;
        _gameActions = gameActions;
        Engine = engine;
        LO = localizer;
        _fileShare = fileShare;
        _taskRunner = new(this);
        PinHelper = pinHelper;
        Stakes = new StakesPlugin(data);
    }

    internal void Run()
    {
        _data.PackageDoc = Engine.Document;

        _data.GameResultInfo.Name = _data.GameName;
        _data.GameResultInfo.Language = _data.Settings.AppSettings.Culture;
        _data.GameResultInfo.PackageName = Engine.PackageName;
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

    internal void OnFinalRoundSkip()
    {
        _gameActions.ShowmanReplic(LO[nameof(R.NobodyInFinal)]); // TODO: REMOVE (localized by MessageCode)
        _gameActions.ShowmanReplicNew(MessageCode.RoundSkippedNoPlayers);
        ScheduleExecution(Tasks.MoveNext, 15 + Random.Shared.Next(10), 1);
    }

    internal void OnContentScreenHtml(ContentItem contentItem)
    {
        _data.IsPartial = false;
        ShareMedia(contentItem);

        var contentTime = GetContentItemDuration(contentItem, TimeSettings.ImageTime * 10);

        _data.AtomTime = contentTime;
        _data.AtomStart = DateTime.UtcNow;

        ScheduleExecution(Tasks.MoveNext, contentTime);

        _data.TimeThinking = 0.0;
    }

    internal bool ProcessNextAppellationRequest(bool stop)
    {
        var (appellationSource, isAppellationForRightAnswer) = _data.QuestionPlayState.Appellations.FirstOrDefault();
        _data.QuestionPlayState.Appellations.RemoveAt(0);

        _data.AppellationCallerIndex = -1;
        _data.AppelaerIndex = -1;

        if (isAppellationForRightAnswer)
        {
            for (var i = 0; i < _data.Players.Count; i++)
            {
                if (_data.Players[i].Name == appellationSource)
                {
                    for (var j = 0; j < _data.QuestionHistory.Count; j++)
                    {
                        var index = _data.QuestionHistory[j].PlayerIndex;

                        if (index == i)
                        {
                            if (!_data.QuestionHistory[j].IsRight)
                            {
                                _data.AppelaerIndex = index;
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
            if (_data.Players.Count(p => p.IsConnected) <= 3)
            {
                // If there are 2 or 3 players, there are already 2 positive votes for the answer
                // from answered player and showman. And only 1 or 2 votes left.
                // So there is no chance to win a vote against the answer
                _gameActions.SpecialReplic(string.Format(LO[nameof(R.FailedToAppellateForWrongAnswer)], appellationSource)); // TODO: REMOVE+
                _gameActions.SendMessageToWithArgs(appellationSource, Messages.UserError, ErrorCode.AppellationFailedTooFewPlayers);
                return false;
            }

            for (var i = 0; i < _data.Players.Count; i++)
            {
                if (_data.Players[i].Name == appellationSource)
                {
                    _data.AppellationCallerIndex = i;
                    break;
                }
            }

            if (_data.AppellationCallerIndex == -1)
            {
                // Only players can appellate
                return false;
            }

            // Last person has right answer and is responsible for appellation
            var count = _data.QuestionHistory.Count;

            if (count > 0 && _data.QuestionHistory[count - 1].IsRight)
            {
                _data.AppelaerIndex = _data.QuestionHistory[count - 1].PlayerIndex;
            }
        }

        if (_data.AppelaerIndex != -1)
        {
            // Appellation started
            if (stop)
            {
                _data.QuestionPlayState.AppellationState = AppellationState.Collecting; // To query other appellations
                Stop(StopReason.Appellation);
            }
            
            return true;
        }

        return false;
    }

    private void PostprocessQuestion(int taskTime = 1)
    {
        _tasksHistory.AddLogEntry("Engine_QuestionPostInfo: Appellation activated");

        _data.QuestionPlayState.AppellationState = _data.Settings.AppSettings.UseApellations ? AppellationState.Processing : AppellationState.None;
        _data.IsPlayingMedia = false;

        _data.InformStages &= ~(InformStages.Question | InformStages.Layout | InformStages.ContentShape);

        ScheduleExecution(Tasks.QuestionPostInfo, taskTime, 1, force: true);

        if (GetTurnSwitchingStrategy() == TurnSwitchingStrategy.Sequentially)
        {
            _data.ChooserIndex = (_data.ChooserIndex + 1) % _data.Players.Count;
            _gameActions.SendMessageWithArgs(Messages.SetChooser, _data.ChooserIndex);
        }

        if (_data.QuestionPlayState.AppellationState != AppellationState.None && _data.QuestionPlayState.Appellations.Any())
        {
            ProcessNextAppellationRequest(true);
        }
    }

    internal void OnPackage(Package package)
    {
        _data.Package = package;

        _data.Rounds = _data.Package.Rounds
            .Select((round, index) => new RoundInfo { Index = index, Name = round.Name })
            .ToArray();

        _gameActions.SendMessage(string.Join(Message.ArgsSeparator, Messages.PackageId, package.ID)); // TODO: REMOVE (replaced by PACKAGE_INFO message)
        _gameActions.InformRoundsNames();

        _data.InformStages |= InformStages.RoundNames;

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
        _data.Round = round;
        _data.CanMarkQuestion = false;
        _data.AnswererIndex = -1;
        _data.QuestionPlayState.Clear();
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
        _data.StakeStep = (int)Math.Pow(10, Math.Floor(Math.Log10(_minRoundPrice))); // Maximum power of 10 <= _minRoundPrice
    }

    internal void InitThemes(IEnumerable<Theme> themes, bool willPlayAllThemes, bool isFirstPlay, ThemesPlayMode playMode)
    {
        _data.TInfo.RoundInfo.Clear();

        foreach (var theme in themes)
        {
            _data.TInfo.RoundInfo.Add(new ThemeInfo { Name = theme.Name });
        }

        if (willPlayAllThemes)
        {
            var themesReplic = isFirstPlay
                ? $"{GetRandomString(LO[nameof(R.RoundThemes)])}. {LO[nameof(R.WeWillPlayAllOfThem)]}"
                : LO[nameof(R.LetsPlayNextTheme)];

            _gameActions.ShowmanReplic(themesReplic);
        }

        _data.TableInformStageLock.WithLock(() =>
        {
            _gameActions.InformRoundThemesNames(playMode: playMode);
            _data.ThemeComments = themes.Select(theme => theme.Info.Comments.Text).ToArray();
            _data.InformStages |= InformStages.RoundThemesNames;
            _data.ThemesPlayMode = playMode;
            _data.LastVisualMessage = null;
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

        _gameActions.ShowmanReplic($"{_data.Theme?.Name}, {question.Price}");
        _gameActions.SendVisualMessageWithArgs(Messages.Question, question.Price);

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
        _data.InformStages |= InformStages.Question;

        _data.Theme = _data.Round.Themes[themeIndex];
        _data.Question = _data.Theme.Questions[questionIndex];

        _data.TInfo.RoundInfo[themeIndex].Questions[questionIndex].Price = Question.InvalidPrice;

        InitQuestionState(_data.Question);
        ProceedToThemeAndQuestion(20);
    }

    private void ProceedToThemeAndQuestion(int delay = 10, bool force = true)
    {
        if (!_data.ThemeInfoShown.Contains(_data.Theme))
        {
            ScheduleExecution(Tasks.Theme, delay, 1, force);
        }
        else
        {
            ScheduleExecution(Tasks.QuestionStartInfo, delay, 1, force);
        }
    }

    private void InitQuestionState(Question question)
    {
        // TODO: move all to question play state
        _data.QuestionHistory.Clear();
        _data.PendingAnswererIndex = -1;
        _data.AnswererPressDuration = -1;
        _data.PendingAnswererIndicies.Clear();
        _data.IsQuestionAskPlaying = true;
        _data.IsPlayingMedia = false;
        _data.IsPlayingMediaPaused = false;
        _data.CurPriceRight = _data.CurPriceWrong = question.Price;
        _data.Order = Array.Empty<int>();
        _data.OrderIndex = -1;
        _data.AnswererIndex = -1;
        _data.CanMarkQuestion = false;
        _data.QuestionPlayState.Clear();

        if (_data.Settings.AppSettings.HintShowman)
        {
            // TODO: use SendAnswerInfoToShowman()
            var rightAnswers = question.Right;
            var rightAnswer = rightAnswers.FirstOrDefault() ?? LO[nameof(R.NotSet)];

            _gameActions.SendMessage(string.Join(Message.ArgsSeparator, Messages.Hint, rightAnswer), _data.ShowMan.Name);
        }

        SendQuestionAnswersToShowman();
    }

    internal void OnContentScreenText(string text, bool waitForFinish, TimeSpan duration)
    {
        var contentTime = duration > TimeSpan.Zero ? (int)(duration.TotalMilliseconds / 100) : GetReadingDurationForTextLength(text.Length);

        if (_data.QuestionPlayState.IsAnswer)
        {
            contentTime += _data.Settings.AppSettings.TimeSettings.TimeForRightAnswer * 10;
        }

        _data.AtomTime = contentTime;
        _data.AtomStart = DateTime.UtcNow;
        _data.UseBackgroundAudio = !waitForFinish;

        _data.IsPartial = waitForFinish && IsPartial();

        if (_data.IsPartial)
        {
            _data.Text = text;
            _gameActions.SendContentShape();
            _data.InformStages |= InformStages.ContentShape;
            _data.InitialPartialTextLength = 0;
            _data.PartialIterationCounter = 0;
            _data.TextLength = 0;
            ScheduleExecution(Tasks.PrintPartial, 1);
            return;
        }

        var message = string.Join(Message.ArgsSeparator, Messages.Content, ContentPlacements.Screen, 0, ContentTypes.Text, text.EscapeNewLines());

        _data.ComplexVisualState ??= new IReadOnlyList<string>[1];
        _data.ComplexVisualState[0] = new string[] { message };

        _gameActions.SendMessage(message);
        _gameActions.SystemReplic(text); // TODO: REMOVE: replaced by CONTENT message

        var nextTime = !waitForFinish ? 1 : contentTime;

        ScheduleExecution(Tasks.MoveNext, nextTime);

        _data.TimeThinking = 0.0;
    }

    /// <summary>
    /// Should the question be displayed partially.
    /// </summary>
    private bool IsPartial() =>
        _data.QuestionPlayState.UseButtons
            && !_data.Settings.AppSettings.FalseStart
            && _data.Settings.AppSettings.PartialText
            && !_data.QuestionPlayState.IsAnswer;

    internal void OnContentReplicText(string text, bool waitForFinish, TimeSpan duration)
    {
        _data.IsPartial = false;
        // There is no need to send content for now, as we can send replic directly
        //_gameActions.SendMessageWithArgs(Messages.Content, ContentPlacements.Replic, 0, ContentTypes.Text, text.EscapeNewLines());
        _gameActions.ShowmanReplic(text);

        var atomTime = !waitForFinish ? 1 : (duration > TimeSpan.Zero ? (int)(duration.TotalMilliseconds / 100) : GetReadingDurationForTextLength(text.Length));

        _data.AtomTime = atomTime;
        _data.AtomStart = DateTime.UtcNow;

        ScheduleExecution(Tasks.MoveNext, atomTime);

        _data.TimeThinking = 0.0;
        _data.UseBackgroundAudio = !waitForFinish;
    }

    private (bool success, string? globalUri, string? localUri, string? error) TryShareContent(ContentItem contentItem)
    {
        if (_data.PackageDoc == null || _data.Package == null)
        {
            throw new InvalidOperationException("_data.Package == null; game not running");
        }

        if (!contentItem.IsRef) // External link
        {
            if (_data.Package.HasQualityControl)
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

        if (_data.Package.HasQualityControl)
        {
            var fileExtension = Path.GetExtension(contentItem.Value)?.ToLowerInvariant();

            if (Quality.FileExtensions.TryGetValue(contentItem.Type, out var allowedExtensions) && !allowedExtensions.Contains(fileExtension))
            {
                return (false, null, null, string.Format(LO[nameof(R.InvalidFileExtension)], contentItem.Value, fileExtension));
            }
        }
        
        var contentType = contentItem.Type;
        var mediaCategory = CollectionNames.TryGetCollectionName(contentType) ?? contentType;
        var media = _data.PackageDoc.TryGetMedia(contentItem);

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

                _data.ComplexVisualState ??= new IReadOnlyList<string>[1];
                _data.ComplexVisualState[0] = new string[] { message };

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
            _data.Host.SendError(exc, true);
            return null;
        }
    }

    private (bool success, string? error) CheckFileLength(string contentType, long fileLength)
    {
        int? maxRecommendedFileLength = contentType == ContentTypes.Image ? _data.Host.MaxImageSizeKb
            : (contentType == ContentTypes.Audio ? _data.Host.MaxAudioSizeKb
            : (contentType == ContentTypes.Video ? _data.Host.MaxVideoSizeKb
            : null));

        if (!maxRecommendedFileLength.HasValue || fileLength <= (long)maxRecommendedFileLength * 1024)
        {
            return (true, null);
        }

        var fileLocation = $"{_data.Theme?.Name}, {_data.Question?.Price}";

        if (_data.Package.HasQualityControl)
        {
            var error = string.Format(LO[nameof(R.OversizedFileForbidden)], R.File, fileLocation, maxRecommendedFileLength);
            return (false, error);
        }

        // Notify users that the media file is too large and could be downloaded slowly
        var errorMessage = string.Format(LO[nameof(R.OversizedFile)], R.File, fileLocation, maxRecommendedFileLength);
        _gameActions.SendMessageWithArgs(Messages.Replic, ReplicCodes.Special.ToString(), errorMessage); // TODO: REMOVE: replaced by USER_ERROR message
        _gameActions.SendMessageWithArgs(Messages.UserError, ErrorCode.OversizedFile, contentType, maxRecommendedFileLength);

        if (_data.OversizedMediaNotificationsCount < MaxMediaNotifications)
        {
            _data.OversizedMediaNotificationsCount++;

            // Show message on table
            _gameActions.SendMessageWithArgs(Messages.Atom_Hint, errorMessage);
        }

        return (true, null);
    }

    internal void OnContentScreenImage(ContentItem contentItem)
    {
        _data.IsPartial = false;
        ShareMedia(contentItem);

        var appSettings = _data.Settings.AppSettings;
        // TODO: provide this flag to client as part of the CONTENT message
        var partialImage = appSettings.PartialImages && !appSettings.FalseStart && _data.QuestionPlayState.UseButtons && !_data.QuestionPlayState.IsAnswer;

        var renderTime = partialImage ? Math.Max(0, appSettings.TimeSettings.PartialImageTime * 10) : 0;
        
        var waitTime = GetContentItemDuration(contentItem, TimeSettings.ImageTime * 10);

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

    private void ClearMediaContent() => _data.QuestionPlayState.MediaContentCompletions.Clear();

    internal void OnContentBackgroundAudio(ContentItem contentItem)
    {
        _data.IsPartial = false;
        var globalUri = ShareMedia(contentItem);

        var defaultTime = DefaultMediaTime + TimeSettings.TimeForMediaDelay * 10;

        if (globalUri != null)
        {
            _data.QuestionPlayState.MediaContentCompletions[(contentItem.Type, globalUri)] = new Completion(_data.ActiveHumanCount);
            _completion = ClearMediaContent;
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
        var globalUri = ShareMedia(contentItem);

        var defaultTime = DefaultMediaTime + TimeSettings.TimeForMediaDelay * 10;

        if (globalUri != null)
        {
            _data.QuestionPlayState.MediaContentCompletions[(contentItem.Type, globalUri)] = new Completion(_data.ActiveHumanCount);
            _completion = ClearMediaContent;
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

    // Let's add a random offset so it will be difficult to press the button in advance (before the frame appears)
    internal void AskToPress() => ScheduleExecution(Tasks.AskToTry, 1 + (_data.Settings.AppSettings.Managed ? 0 : Random.Shared.Next(10)), force: true);

    internal void AskDirectAnswer()
    {
        if (_data.QuestionTypeName == QuestionTypes.StakeAll)
        {
            _gameActions.SendMessageWithArgs(Messages.FinalThink, _data.Settings.AppSettings.TimeSettings.TimeForFinalThinking);
        }

        ScheduleExecution(Tasks.AskAnswer, 1, force: true);
    }

    internal void OnRoundEnded()
    {
        _data.IsQuestionAskPlaying = false;
        _data.IsPlayingMedia = false;

        _gameActions.InformSums();
        _gameActions.SendMessage(Messages.Stop); // Timers STOP

        _data.IsThinking = false;

        _data.IsWaiting = false;
        _data.Decision = DecisionType.None;

        _data.InformStages &= ~(InformStages.RoundContent | 
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

        if (_data.TInfo.Pause)
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
            if (_data.TInfo.Pause)
            {
                return;
            }

            if (Stop(StopReason.Pause))
            {
                _data.TInfo.Pause = true;
                AddHistory("Pause activated");
            }

            return;
        }

        if (StopReason == StopReason.Pause)
        {
            // We are currently moving into pause mode. Resuming
            _data.TInfo.Pause = false;
            AddHistory("Immediate pause resume");
            CancelStop();
            return;
        }

        if (!_data.TInfo.Pause)
        {
            return;
        }

        _data.TInfo.Pause = false;

        var pauseDuration = DateTime.UtcNow.Subtract(_data.PauseStartTime);

        var times = new int[Constants.TimersCount];

        for (var i = 0; i < Constants.TimersCount; i++)
        {
            times[i] = (int)(_data.PauseStartTime.Subtract(_data.TimerStartTime[i]).TotalMilliseconds / 100);
            _data.TimerStartTime[i] = _data.TimerStartTime[i].Add(pauseDuration);
        }

        if (_data.IsPlayingMediaPaused)
        {
            _data.IsPlayingMediaPaused = false;
            _data.IsPlayingMedia = true;
        }

        if (_data.IsThinkingPaused)
        {
            _data.IsThinkingPaused = false;
            _data.IsThinking = true;
        }

        AddHistory($"Pause resumed ({Runner.PrintOldTasks()} {StopReason})");

        try
        {
            var maxPressingTime = _data.Settings.AppSettings.TimeSettings.TimeForThinkingOnQuestion * 10;
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

        _gameActions.SystemReplic(LO[nameof(R.GameResumed)]); // TODO: REMOVE: replaced by PAUSE message
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
        _gameActions.SpecialReplic(LO[nameof(R.ShowmanSwitchedToOtherRound)]); // TODO: REMOVE+
        _gameActions.SendMessage(Messages.RoundEnd, "manual");
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

        _gameActions.PlayerReplic(playerIndex, themeName); // TODO: REMOVE: replaced by OUT message
        ScheduleExecution(Tasks.MoveNext, 10);
    }

    internal void AnnounceFinalTheme(Question question)
    {
        _gameActions.ShowmanReplic($"{GetRandomString(LO[nameof(R.PlayTheme)])} {_data.Theme.Name}");
        _gameActions.SendMessageWithArgs(Messages.QuestionCaption, _data.Theme.Name);
        _gameActions.SendThemeInfo(overridenQuestionCount: 1);
        
        InitQuestionState(question);
        ProceedToThemeAndQuestion(force: false);
    }

    internal void OnEndGame()
    {
        // Clearing the table
        _gameActions.SendMessage(Messages.Stop);
        _gameActions.SystemReplic($"{LO[nameof(R.GameResults)]}: "); // TODO: REMOVE: replaced by WINNER message

        for (var i = 0; i < _data.Players.Count; i++)
        {
            _gameActions.SystemReplic($"{_data.Players[i].Name}: {Notion.FormatNumber(_data.Players[i].Sum)}"); // TODO: REMOVE: replaced by WINNER message
        }

        FillReport();
        ScheduleExecution(Tasks.Winner, 15 + Random.Shared.Next(10), force: true);
    }

    public void Dispose() =>
        _data.TaskLock.WithLock(
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
            _data.IsWaiting = false; // Preventing double message processing
        }
        else if (reason == StopReason.Appellation && _data.IsWaiting)
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
    private bool OnDecision() => _data.Decision switch
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
        if (_data.ThemeIndex == -1
            || _data.ThemeIndex >= _data.TInfo.RoundInfo.Count
            || _data.QuestionIndex == -1
            || _data.QuestionIndex >= _data.TInfo.RoundInfo[_data.ThemeIndex].Questions.Count
            || !_data.TInfo.RoundInfo[_data.ThemeIndex].Questions[_data.QuestionIndex].IsActive())
        {
            return false;
        }

        StopWaiting();
        ScheduleExecution(Tasks.MoveNext, 1, force: true);
        return true;
    }

    private bool OnDecisionQuestionAnswererSelection()
    {
        if (_data.Answerer == null)
        {
            return false;
        }

        StopWaiting();

        var s = _data.ChooserIndex == _data.AnswererIndex ? LO[nameof(R.ToMyself)] : _data.Answerer.Name;
        _gameActions.PlayerReplic(_data.ChooserIndex, s); // TODO: REMOVE: replaced by SETCHOOSER message

        _data.ChooserIndex = _data.AnswererIndex;
        _gameActions.SendMessageWithArgs(Messages.SetChooser, _data.ChooserIndex, "+");
        ScheduleExecution(Tasks.MoveNext, 10);
        return true;
    }

    private bool OnDecisionQuestionPriceSelection()
    {
        if (_data.CurPriceRight == -1)
        {
            return false;
        }

        StopWaiting();

        _data.CurPriceWrong = _data.CurPriceRight;

        _gameActions.SendMessageWithArgs(Messages.PersonStake, _data.AnswererIndex, 1, _data.CurPriceRight);
        _gameActions.SendMessageWithArgs(Messages.SetChooser, _data.ChooserIndex, "+");

        ScheduleExecution(Tasks.MoveNext, 20);

        return true;
    }

    private bool OnDecisionHiddenStakeMaking()
    {
        if (_data.HiddenStakerCount != 0)
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

    private bool OnDecisionAppellation()
    {
        StopWaiting();
        ScheduleExecution(Tasks.CheckAppellation, 10);
        return true;
    }

    private bool OnDecisionThemeDeleting()
    {
        if (_data.ThemeIndexToDelete == -1)
        {
            return false;
        }

        StopWaiting();
        ScheduleExecution(Tasks.MoveNext, 1, force: true);
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
            _data.Stake = _data.CurPriceRight;
            _data.Stakes.StakerIndex = playerIndex;
        }
        else if (_data.StakeType == StakeMode.Sum)
        {
            _data.Stake = _data.StakeSum;
            _data.Stakes.StakerIndex = playerIndex;
        }
        else if (_data.StakeType == StakeMode.Pass)
        {
            _gameActions.PlayerReplic(playerIndex, LO[nameof(R.Pass)]); // TODO: REMOVE: replaced by PERSONSTAKE message
            _data.Players[playerIndex].StakeMaking = false;
            var passMsg = new MessageBuilder(Messages.PlayerState, PlayerState.Pass, playerIndex);
            _gameActions.SendMessage(passMsg.ToString());
        }
        else
        {
            _gameActions.PlayerReplic(playerIndex, LO[nameof(R.VaBank)]); // TODO: REMOVE: replaced by PERSONSTAKE message
            _data.Stake = _data.Players[playerIndex].Sum;
            _data.Stakes.StakerIndex = playerIndex;
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

                if (i != _data.Stakes.StakerIndex && player.StakeMaking && player.Sum <= _data.Stake)
                {
                    player.StakeMaking = false;
                    _gameActions.SendMessageWithArgs(Messages.PersonStake, i, 2);
                }
            }
        }

        var stakeMaking2 = string.Join(",", _data.Players.Select(p => p.StakeMaking));
        _data.OrderHistory.Append($"Stake making updated: {stakeMaking2}").AppendLine();

        if (TryDetectStakesWinner())
        {
            return true;
        }

        var stakerCount = _data.Players.Count(p => p.StakeMaking);

        if (stakerCount == 0)
        {
            _tasksHistory.AddLogEntry("Skipping question");
            _data.SkipQuestion?.Invoke();
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
        var multipleAnswerers = HaveMultipleAnswerers();

        if (_data.Answerer.AnswerIsRight)
        {
            var showmanReplic = IsSpecialQuestion() ? nameof(R.Bravo) : nameof(R.Right);            
            var s = new StringBuilder(GetRandomString(LO[showmanReplic]));

            var canonicalAnswer = _data.Question?.Right.FirstOrDefault();
            var isAnswerCanonical = canonicalAnswer != null && (_data.Answerer.Answer ?? "").Simplify().Contains(canonicalAnswer.Simplify());

            if (canonicalAnswer != null && !isAnswerCanonical)
            {
                _data.GameResultInfo.AcceptedAnswers.Add(new QuestionReport
                {
                    ThemeName = _data.Theme.Name,
                    QuestionText = _data.Question?.GetText(),
                    ReportText = _data.Answerer.Answer
                });
            }

            if (!_data.QuestionPlayState.HiddenStakes)
            {
                var outcome = _data.CurPriceRight;
                updateSum = (int)(outcome * _data.Answerer.AnswerValidationFactor);

                s.AppendFormat($" (+{outcome.ToString().FormatNumber()}{PrintRightFactor(_data.Answerer.AnswerValidationFactor)})");

                _gameActions.ShowmanReplic(s.ToString());
                _gameActions.SendMessageWithArgs(Messages.Person, '+', _data.AnswererIndex, updateSum);
                AddRightSum(_data.Answerer, updateSum);
                _gameActions.InformSums();

                if (multipleAnswerers)
                {
                    ScheduleExecution(Tasks.Announce, 15);
                }
                else
                {
                    if (GetTurnSwitchingStrategy() == TurnSwitchingStrategy.ByRightAnswerOnButton &&
                        _data.QuestionPlayState.UseButtons &&
                        _data.ChooserIndex != _data.AnswererIndex)
                    {
                        _data.ChooserIndex = _data.AnswererIndex;
                        _gameActions.SendMessageWithArgs(Messages.SetChooser, _data.ChooserIndex);
                    }

                    // TODO: many of these lines are redundand in Special questions
                    _data.IsQuestionAskPlaying = false;

                    _data.IsThinking = false;
                    _gameActions.SendMessageWithArgs(Messages.Timer, 1, MessageParams.Timer_Stop);

                    MoveToAnswer(); // Question is answered correctly
                    ScheduleExecution(Tasks.MoveNext, 1, force: true);
                }
            }
            else
            {
                _gameActions.ShowmanReplic(s.ToString());
                _data.PlayerIsRight = true;
                updateSum = _data.Answerer.PersonalStake;
                ScheduleExecution(Tasks.AnnounceStake, 15);
            }
        }
        else
        {
            var s = new StringBuilder();

            if (_data.Answerer.Answer != LO[nameof(R.IDontKnow)])
            {
                s.Append(GetRandomString(LO[nameof(R.Wrong)])).Append(' ');
            }

            var outcome = _data.CurPriceWrong;

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

            if (!_data.QuestionPlayState.HiddenStakes)
            {
                s.AppendFormat($"(-{outcome.ToString().FormatNumber()}{PrintRightFactor(_data.Answerer.AnswerValidationFactor)})");                
                _gameActions.ShowmanReplic(s.ToString());

                if (_data.Answerer.AnswerValidationFactor == 0)
                {
                    _gameActions.SendMessageWithArgs(Messages.Pass, _data.AnswererIndex);
                    updateSum = -1;
                }
                else
                {
                    updateSum = (int)(outcome * _data.Answerer.AnswerValidationFactor);
                    _gameActions.SendMessageWithArgs(Messages.Person, '-', _data.AnswererIndex, updateSum);
                    SubtractWrongSum(_data.Answerer, updateSum);
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

                if (multipleAnswerers)
                {
                    ScheduleExecution(Tasks.Announce, 15);
                }
                else
                {
                    _data.Answerer.CanPress = false;
                    ScheduleExecution(Tasks.ContinueQuestion, 1);
                }
            }
            else
            {
                _gameActions.ShowmanReplic(s.ToString());
                _data.PlayerIsRight = false;
                updateSum = _data.Answerer.PersonalStake;

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

    private TurnSwitchingStrategy GetTurnSwitchingStrategy() => _data.Settings.AppSettings.GameMode switch
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
        if (_data.IsQuestionFinished)
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
        if (!_data.QuestionPlayState.UseButtons)
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

        _gameActions.SendMessage(Messages.Resume); // To resume the media

        if (_data.Settings.AppSettings.FalseStart || _data.IsQuestionFinished)
        {
            ScheduleExecution(Tasks.AskToTry, 1, 1, true);
            return;
        }

        _gameActions.SendMessageWithArgs(Messages.Try, MessageParams.Try_NotFinished);
        _data.IsPlayingMedia = _data.IsPlayingMediaPaused;

        // Resume question playing
        if (_data.IsPartial)
        {
            _data.InitialPartialTextLength = _data.TextLength;
            _data.PartialIterationCounter = 0;
            ScheduleExecution(Tasks.PrintPartial, 5, force: true);
        }
        else
        {
            var waitTime = _data.IsPlayingMedia && _data.QuestionPlayState.MediaContentCompletions.All(p => p.Value.Current > 0)
                ? 30 + _data.Settings.AppSettings.TimeSettings.TimeForMediaDelay * 10
                : _data.AtomTime;

            ScheduleExecution(Tasks.MoveNext, waitTime, force: true);
        }

        SendTryToPlayers();
        _data.Decision = DecisionType.Pressing;
    }

    // TODO: this should be removed
    internal bool IsSpecialQuestion() => _data.QuestionTypeName != QuestionTypes.Simple;

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
        _gameActions.ShowmanReplic(msg); // TODO: REMOVE: replaced by SETCHOOSER message
        _gameActions.SendMessageWithArgs(Messages.SetChooser, _data.ChooserIndex, "-", "INITIAL");
        
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
                _gameActions.PlayerReplic(_data.AnswererIndex, _data.Answerer.Answer); // TODO: REMOVE: replaced by PLAYER_ANSWER message
                _gameActions.SendMessageWithArgs(Messages.PlayerAnswer, _data.AnswererIndex, _data.Answerer.Answer);
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

        if (_data.QuestionPlayState.AnswerOptions != null)
        {
            var m = new MessageBuilder(Messages.Answers).AddRange(_data.Players.Select(p => p.Answer ?? ""));
            _gameActions.SendMessage(m.ToString());
            ScheduleExecution(Tasks.MoveNext, 30, 1, true);
            return true;
        }

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

    // TODO: currently PlanExecution() is used for interruprions and ScheduleExecution() for normal tasks flow
    // Think about using a universal scheduler which will be able to handle both cases

    internal void PlanExecution(Tasks task, double taskTime, int arg = 0)
    {
        _tasksHistory.AddLogEntry($"PlanExecution {task} {taskTime} {arg} ({_data.TInfo.Pause})");

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
            _data.TaskLock.WithLock(() =>
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

                    _data.Host.SendError(
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

                    case Tasks.RoundTheme:
                        OnRoundTheme(arg);
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
                        QuestionSourcesAndComments(arg);
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
            _data.Host.SendError(new Exception($"Task: {task}, param: {arg}, history: {_tasksHistory}", exc));
            ScheduleExecution(Tasks.NoTask, 10);
            _data.MoveNextBlocked = true;
            _gameActions.SpecialReplic("Game ERROR"); // TODO: REMOVE+
            _gameActions.SendMessageWithArgs(Messages.GameError);
        }
    }

    private void OnRoundTheme(int themeIndex)
    {
        if (themeIndex < 0 || themeIndex >= _data.TInfo.RoundInfo.Count)
        {
            throw new ArgumentException($"{nameof(themeIndex)} {themeIndex} must be in [0;{_data.TInfo.RoundInfo.Count - 1}]");
        }

        _gameActions.SendThemeInfo(themeIndex, true);

        if (themeIndex + 1 < _data.TInfo.RoundInfo.Count)
        {
            ScheduleExecution(Tasks.RoundTheme, 19, themeIndex + 1);
        }
        else
        {
            InformTable();
            _data.ThemesPlayMode = ThemesPlayMode.None;
            ScheduleExecution(Tasks.AskFirst, 19);
        }
    }

    private void InformTable() => _data.TableInformStageLock.WithLock(
        () =>
        {
            _gameActions.InformTable();
            _data.InformStages |= InformStages.Table;
        },
        5000);

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
            : (_stopReason == StopReason.Decision ? _data.Decision.ToString() : "");

        _tasksHistory.AddLogEntry($"StopReason {_stopReason} {stopReasonDetails}");

        // Interrupt standard execution and try to do something urgent
        switch (_stopReason)
        {
            case StopReason.Pause:
                _tasksHistory.AddLogEntry($"Pause PauseExecution {task} {arg} {_taskRunner.PrintOldTasks()}");
                _taskRunner.PauseExecution(task, arg, _leftTime);

                _data.PauseStartTime = DateTime.UtcNow;

                if (_data.IsPlayingMedia)
                {
                    _data.IsPlayingMediaPaused = true;
                    _data.IsPlayingMedia = false;
                }

                if (_data.IsThinking)
                {
                    var startTime = _data.TimerStartTime[1];

                    _data.TimeThinking += _data.PauseStartTime.Subtract(startTime).TotalMilliseconds / 100;
                    _data.IsThinkingPaused = true;
                    _data.IsThinking = false;
                }

                var times = new int[Constants.TimersCount];

                for (var i = 0; i < Constants.TimersCount; i++)
                {
                    times[i] = (int)(_data.PauseStartTime.Subtract(_data.TimerStartTime[i]).TotalMilliseconds / 100);
                }

                _gameActions.SystemReplic(LO[nameof(R.PauseInGame)]); // TODO: REMOVE: replaced by PAUSE message
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
                var savedTask = task == Tasks.WaitChoose ? Tasks.AskToChoose : (task == Tasks.WaitDelete ? Tasks.AskToDelete : task);

                _tasksHistory.AddLogEntry($"Appellation PauseExecution {savedTask} {arg} ({_taskRunner.PrintOldTasks()})");

                _taskRunner.PauseExecution(savedTask, arg, _leftTime);
                ScheduleExecution(Tasks.StartAppellation, 10);
                break;

            case StopReason.Move:
                switch (_data.MoveDirection)
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
                            var subText = _data.Text[_data.TextLength..];

                            _gameActions.SendMessageWithArgs(Messages.ContentAppend, ContentPlacements.Screen, 0, ContentTypes.Text, subText.EscapeNewLines());
                            _gameActions.SystemReplic(subText); // TODO: REMOVE: replaced by CONTENT_APPEND message

                            newTask = Tasks.MoveNext;
                        }
                        else if (task == Tasks.RoundTheme && !_data.Settings.AppSettings.Managed) // Skip all round themes
                        {
                            for (var themeIndex = arg; themeIndex < _data.TInfo.RoundInfo.Count; themeIndex++)
                            {
                                _gameActions.SendThemeInfo(themeIndex, true);
                            }

                            InformTable();
                            _data.ThemesPlayMode = ThemesPlayMode.None;
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
                            stop = Engine.MoveToRound(_data.TargetRoundIndex);

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
        var msg = new MessageBuilder(Messages.GameStatistics);

        var message = new StringBuilder(LO[nameof(R.GameStatistics)]).Append(':').AppendLine().AppendLine();

        foreach (var (name, statistic) in _data.Statistics)
        {
            msg.AddRange(name, statistic.RightAnswerCount, statistic.WrongAnswerCount, statistic.RightTotal, statistic.WrongTotal);

            message.Append(name).Append(':').AppendLine();
            message.Append("   ").Append(LO[nameof(R.RightAnswers)]).Append(": ").Append(statistic.RightAnswerCount).AppendLine();
            message.Append("   ").Append(LO[nameof(R.WrongAnswers)]).Append(": ").Append(statistic.WrongAnswerCount).AppendLine();
            message.Append("   ").Append(LO[nameof(R.ScoreEarned)]).Append(": ").Append(statistic.RightTotal).AppendLine();
            message.Append("   ").Append(LO[nameof(R.ScoreLost)]).Append(": ").Append(statistic.WrongTotal).AppendLine();

            message.AppendLine();
        }

        _gameActions.SendVisualMessage(msg.Build());
        _gameActions.SpecialReplic(message.ToString()); // TODO: REMOVE+
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
        _data.Answerer.AnswerValidationFactor = 1.0;

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

        _data.CurPriceRight = _data.StakeRange.Minimum;
        _data.CurPriceWrong = _data.StakeRange.Minimum;

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
        _data.MoveNextBlocked = false;

        _tasksHistory.AddLogEntry($"Moved -> {Engine?.Stage}");
    }

    private void OnAnnounceStakesWinner()
    {
        var stakerIndex = _data.Stakes.StakerIndex;

        if (stakerIndex == -1)
        {
            throw new ArgumentException($"{nameof(OnAnnounceStakesWinner)}: {nameof(stakerIndex)} == -1 {_data.OrderHistory}", nameof(stakerIndex));
        }

        _data.ChooserIndex = stakerIndex;
        _data.AnswererIndex = stakerIndex;
        _data.QuestionPlayState.SetSingleAnswerer(stakerIndex);
        _data.CurPriceRight = _data.Stake;
        _data.CurPriceWrong = _data.Stake;

        _gameActions.SendMessageWithArgs(Messages.SetChooser, _data.ChooserIndex, "+");

        var msg = $"{Notion.RandomString(LO[nameof(R.NowPlays)])} {_data.Players[stakerIndex].Name} {LO[nameof(R.With)]} {Notion.FormatNumber(_data.Stake)}";

        _gameActions.ShowmanReplic(msg.ToString());
        ScheduleExecution(Tasks.MoveNext, 15 + Random.Shared.Next(10));
    }

    private void WaitNext(bool isSelectingStaker)
    {
        _gameActions.SendMessage(Messages.Cancel, _data.ShowMan.Name);
        _gameActions.SendMessageWithArgs(Messages.Timer, 2, MessageParams.Timer_Stop);

        if (!isSelectingStaker && _data.ThemeDeleters != null && _data.ThemeDeleters.IsEmpty())
        {
            throw new InvalidOperationException("_data.ThemeDeleters are empty");
        }

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

        var s = string.Join(Message.ArgsSeparator, Messages.CatCost, _data.StakeRange.Minimum, _data.StakeRange.Maximum, _data.StakeRange.Step);

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
        AskToMakeStake(StakeReason.Simple, answerer.Name, _data.StakeRange);

        ScheduleExecution(Tasks.WaitSelectQuestionPrice, waitTime);
        WaitFor(DecisionType.QuestionPriceSelection, waitTime, _data.AnswererIndex);
    }

    private void WaitFirst()
    {
        _gameActions.SendMessage(Messages.Cancel, _data.ShowMan.Name);

        if (_data.ChooserIndex == -1)
        {
            _data.ChooserIndex = _data.Players.SelectRandom(p => p.Flag);
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
                _data.Answerer.AnswerIsWrong = !_data.IsOralNow;
            }
        }
        else
        {
            if (_data.QuestionPlayState.ActiveValidationCount > 0)
            {
                _gameActions.SendMessage(Messages.Cancel, _data.ShowMan.Name); // Cancel validation
            }

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

        var index = _data.Players.SelectRandomOnIndex(index => index != _data.ChooserIndex);

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

        ScheduleExecution(Tasks.MoveNext, 1, force: true);

        _data.IsQuestionAskPlaying = false;
    }

    private void WaitHiddenStake()
    {
        _gameActions.SendMessageWithArgs(Messages.Timer, 2, MessageParams.Timer_Stop);

        for (var i = 0; i < _data.Players.Count; i++)
        {
            if (_data.QuestionPlayState.AnswererIndicies.Contains(i) && _data.Players[i].PersonalStake == -1)
            {
                _gameActions.SendMessage(Messages.Cancel, _data.Players[i].Name);
                _data.Players[i].PersonalStake = 1;

                _gameActions.SendMessageWithArgs(Messages.PersonFinalStake, i);
            }
        }

        _data.HiddenStakerCount = 0;
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

    private void AnnouncePostStakeWithAnswerOptions()
    {
        if (_data.AnnouncedAnswerersEnumerator == null || !_data.AnnouncedAnswerersEnumerator.MoveNext())
        {
            PostprocessQuestion();
            return;
        }

        var answererIndex = _data.AnnouncedAnswerersEnumerator.Current;
        _data.AnswererIndex = answererIndex;

        _data.PlayerIsRight = _data.Answerer?.Answer == _data.RightOptionLabel;
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
        var answerer = _data.Answerer;

        if (answerer == null)
        {
            throw new ArgumentException($"{nameof(answerer)} == null", nameof(answerer));
        }

        var stake = answerer.PersonalStake;
        _gameActions.ShowmanReplic($"{LO[nameof(R.Stake)]} {answerer.Name}: {Notion.FormatNumber(stake)}");

        var message = new MessageBuilder(Messages.Person);

        if (_data.PlayerIsRight)
        {
            message.Add('+');
            AddRightSum(answerer, stake);
        }
        else
        {
            message.Add('-');
            SubtractWrongSum(answerer, stake);
        }

        message.Add(_data.AnswererIndex).Add(stake);

        _gameActions.SendMessage(message.ToString());
        _gameActions.InformSums();

        _gameActions.SendMessageWithArgs(Messages.PersonStake, _data.AnswererIndex, 1, stake);
    }

    private void AskHiddenStakes()
    {
        var s = GetRandomString(LO[nameof(R.MakeStake)]);
        _gameActions.ShowmanReplic(s);

        _data.HiddenStakerCount = 0;
        var stakers = new List<(string, StakeSettings)>();

        for (var i = 0; i < _data.Players.Count; i++)
        {
            if (_data.QuestionPlayState.AnswererIndicies.Contains(i))
            {
                if (_data.Players[i].Sum <= 1)
                {
                    _data.Players[i].PersonalStake = 1; // only one choice
                    _gameActions.SendMessageWithArgs(Messages.PersonFinalStake, i);
                    continue;
                }

                _data.Players[i].PersonalStake = -1;
                _data.HiddenStakerCount++;
                _gameActions.SendMessage(Messages.FinalStake, _data.Players[i].Name);

                stakers.Add((_data.Players[i].Name, new(1, _data.Players[i].Sum, 1)));
            }
        }

        if (_data.HiddenStakerCount == 0)
        {
            ProceedToHiddenStakesQuestion();
            return;
        }

        _data.IsOralNow = false;
        _data.StakeModes = StakeModes.Stake;
        AskToMakeStake(StakeReason.Hidden, stakers);

        var waitTime = _data.Settings.AppSettings.TimeSettings.TimeForMakingStake * 10;
        ScheduleExecution(Tasks.WaitHiddenStake, waitTime);
        WaitFor(DecisionType.HiddenStakeMaking, waitTime, -2);
    }

    private void WaitDelete()
    {
        _gameActions.SendMessage(Messages.Cancel, _data.ActivePlayer.Name);

        if (_data.IsOralNow)
        {
            _gameActions.SendMessage(Messages.Cancel, _data.ShowMan.Name);
        }

        _gameActions.SendMessageWithArgs(Messages.Timer, 2, MessageParams.Timer_Stop);

        _data.ThemeIndexToDelete = _data.TInfo.RoundInfo.SelectRandom(item => item.Name != null);

        OnDecision();
    }

    private void Winner()
    {
        var winnerScore = _data.Players.Max(player => player.Sum);
        var winnerCount = _data.Players.Count(player => player.Sum == winnerScore);

        if (winnerCount == 1)
        {
            for (var i = 0; i < _data.Players.Count; i++)
            {
                if (_data.Players[i].Sum == winnerScore)
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

    private void AskToTry(int arg)
    {
        if (_data.Players.All(p => !p.CanPress))
        {
            ScheduleExecution(Tasks.WaitTry, 3, force: true);
            return;
        }

        if (_data.Settings.AppSettings.FalseStart || arg == 1)
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

        _data.ThemeIndex = i;
        _data.QuestionIndex = j;

        OnDecision();
    }

    private void OnQuestionStartInfo(int arg)
    {
        var authors = _data.PackageDoc.ResolveAuthors(_data.Question.Info.Authors);

        if (authors.Length > 0)
        {
            var msg = new MessageBuilder(Messages.QuestionAuthors).AddRange(authors);
            _gameActions.SendMessage(msg.ToString());
        }

        var themeComments = _data.Theme.Info.Comments.Text;

        if (themeComments.Length > 0)
        {
            _gameActions.ShowmanReplic(themeComments); // TODO: REMOVE: replaced by THEME message
            _gameActions.SendMessageWithArgs(Messages.ThemeComments, themeComments.EscapeNewLines()); // TODO: REMOVE: replaced by THEME message
        }

        ScheduleExecution(Tasks.MoveNext, 1, arg + 1, force: true);
    }

    internal void OnQuestionType(bool isDefault)
    {
        if (!isDefault) // TODO: This announcement should be handled by the client in the future
        {
            switch (_data.QuestionTypeName)
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
                    OnUnsupportedQuestionType(_data.QuestionTypeName); // TODO: emit this by Game Engine
                    return;
            }
        }

        _data.QuestionTypeSettings.TryGetValue(_data.QuestionTypeName, out var questionTypeRules);
        
        var isNoRisk = questionTypeRules?.PenaltyType == PenaltyType.None;

        if (isNoRisk)
        {
            _data.CurPriceWrong = 0;
        }

        _gameActions.SendVisualMessageWithArgs(Messages.QType, _data.QuestionTypeName, isDefault, isNoRisk);
        ScheduleExecution(Tasks.MoveNext, isDefault ? 1 : 22, force: true);
    }

    private void OnUnsupportedQuestionType(string typeName)
    {
        var sp = new StringBuilder(LO[nameof(R.UnknownType)]).Append(' ').Append(typeName);

        // TODO: REMOVE+
        _gameActions.SpecialReplic(sp.ToString()); 
        _gameActions.SpecialReplic(LO[nameof(R.GameWillResume)]);
        _gameActions.ShowmanReplic(LO[nameof(R.ManuallyPlayedQuestion)]);
        // TODO END

        _gameActions.ShowmanReplicNew(MessageCode.UnsupportedQuestion);

        _data.SkipQuestion?.Invoke();
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
            _gameActions.SystemReplic(subText); // TODO: REMOVE: replaced by CONTENT_APPEND message

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
            _gameActions.SendMessageWithArgs(Messages.QuestionEnd); // Should be here because only here question is fully processed

            var sources = _data.PackageDoc.ResolveSources(_data.Question.Info.Sources);

            if (sources.Count > 0)
            {
                var text = string.Format(OfObjectPropertyFormat, LO[nameof(R.PSources)], LO[nameof(R.OfQuestion)], string.Join(", ", sources));
                _gameActions.ShowmanReplic(text); // TODO: REMOVE: replaced by QUESTION_SOURCES message
                var msg = new MessageBuilder(Messages.QuestionSources).AddRange(sources);
                _gameActions.SendMessage(msg.Build());
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
                
                _gameActions.ShowmanReplic(text); // TODO: REMOVE: replaced by QUESTION_COMMENTS message
                _gameActions.SendVisualMessageWithArgs(Messages.QuestionComments, _data.Question.Info.Comments.Text.EscapeNewLines());
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
            ScheduleExecution(Tasks.QuestionPostInfo, textTime, arg + 1);
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
            ScheduleExecution(Tasks.MoveNext, 15, 1, true);
            return;
        }

        var answererIndex = _data.AnnouncedAnswerersEnumerator.Current;
        _data.AnswererIndex = answererIndex;
        var playerAnswer = _data.Answerer?.Answer;
        var answer = string.IsNullOrEmpty(playerAnswer) ? LO[nameof(R.IDontKnow)] : playerAnswer;

        _gameActions.PlayerReplic(answererIndex, answer); // TODO: REMOVE: replaced by PLAYER_ANSWER message
        _gameActions.SendMessageWithArgs(Messages.PlayerAnswer, answererIndex, playerAnswer ?? "");

        if (_data.QuestionPlayState.ValidateAfterRightAnswer)
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
        var buttonPressMode = _data.Settings.AppSettings.ButtonPressMode;

        if (buttonPressMode == ButtonPressMode.RandomWithinInterval)
        {
            if (_data.PendingAnswererIndicies.Count == 0)
            {
                DumpButtonPressError("_data.PendingAnswererIndicies.Count == 0");
                return false;
            }

            var index = _data.PendingAnswererIndicies.Count == 1 ? 0 : Random.Shared.Next(_data.PendingAnswererIndicies.Count);
            _data.PendingAnswererIndex = _data.PendingAnswererIndicies[index];
        }

        if (_data.PendingAnswererIndex < 0 || _data.PendingAnswererIndex >= _data.Players.Count)
        {
            DumpButtonPressError($"_data.PendingAnswererIndex = {_data.PendingAnswererIndex}; _data.Players.Count = {_data.Players.Count}");
            return false;
        }

        _data.AnswererIndex = _data.PendingAnswererIndex;
        _data.QuestionPlayState.SetSingleAnswerer(_data.PendingAnswererIndex);

        if (!_data.Settings.AppSettings.FalseStart)
        {
            // Stop question reading
            if (!_data.IsQuestionFinished)
            {
                var timeDiff = (int)DateTime.UtcNow.Subtract(_data.AtomStart).TotalSeconds * 10;
                _data.AtomTime = Math.Max(1, _data.AtomTime - timeDiff);
            }
        }

        if (_data.IsThinking)
        {
            var startTime = _data.TimerStartTime[1];
            var currentTime = DateTime.UtcNow;

            _data.TimeThinking += currentTime.Subtract(startTime).TotalMilliseconds / 100;
        }

        var answerer = _data.Answerer;

        if (answerer == null)
        {
            DumpButtonPressError("answerer == null");
            return false;
        }

        answerer.CanPress = false;

        _data.IsThinking = false;

        _gameActions.SendMessageWithArgs(Messages.Timer, 1, MessageParams.Timer_Pause, (int)_data.TimeThinking);

        _data.IsPlayingMediaPaused = _data.IsPlayingMedia;
        _data.IsPlayingMedia = false;

        return true;
    }

    internal void DumpButtonPressError(string reason)
    {
        var pressMode = _data.Settings.AppSettings.ButtonPressMode;
        _data.Host.SendError(new Exception($"{reason} {pressMode}"));
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
                _gameActions.ShowmanReplic($"{LO[nameof(R.GameRules)]}: {BuildRulesString(_data.Settings.AppSettings)}"); // TODO: REMOVE (replaced by OPTIONS2 message)
                nextArg = -1;
                extraTime = 20;
                break;

            default:
                _gameActions.SpecialReplic(LO[nameof(R.WrongGameState)] + " - " + Tasks.StartGame); // TODO: REMOVE+
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

    private void AskToChoose()
    {
        _gameActions.InformSums();
        _gameActions.SendVisualMessage(Messages.ShowTable);

        if (_data.Chooser == null)
        {
            throw new Exception("_data.Chooser == null");
        }

        if (_gameActions.Client.CurrentNode == null)
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

        _gameActions.ShowmanReplic(msg.ToString()); // TODO: REMOVE (localized by MessageCode)
        _gameActions.ShowmanReplicNew(MessageCode.SelectQuestion, _data.Chooser.Name);

        _data.ThemeIndex = -1;
        _data.QuestionIndex = -1;

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
        if (_data.Chooser == null)
        {
            throw new Exception("_data.Chooser == null");
        }

        var canGiveThemselves = _data.Chooser.Flag;
        var append = canGiveThemselves ? $" {LO[nameof(R.YouCanKeepCat)]}" : "";
        _gameActions.ShowmanReplic($"{_data.Chooser.Name}, {LO[nameof(R.GiveCat)]}{append}"); // TODO: REMOVE (localized by MessageCode)
        _gameActions.ShowmanReplicNew(MessageCode.SelectPlayer, _data.Chooser.Name);

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
            _gameActions.SendVisualMessage(Messages.ShowTable); // Everybody will see the table during showman's decision

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

        if (_data.QuestionPlayState.AnswerOptions != null)
        {
            _data.IsWaiting = true;
            _data.Decision = DecisionType.AnswerValidating;

            var rightLabel = _data.Question?.Right.FirstOrDefault();

            _data.Answerer.AnswerIsRight = _data.Answerer.Answer == rightLabel;
            _data.Answerer.AnswerValidationFactor = 1.0;
            _data.ShowmanDecision = true;

            OnDecision();
        }
        else if (!_data.Answerer.IsHuman || _data.Answerer.AnswerIsWrong)
        {
            _data.IsWaiting = true;
            _data.Decision = DecisionType.AnswerValidating;

            _data.Answerer.AnswerIsRight = !_data.Answerer.AnswerIsWrong;
            _data.Answerer.AnswerValidationFactor = 1.0;
            _data.ShowmanDecision = true;

            OnDecision();
        }
        else if (_data.Answerer.Answer != null
            && _data.QuestionPlayState.Validations.TryGetValue(_data.Answerer.Answer, out var validation)
            && validation.HasValue)
        {
            _data.IsWaiting = true;
            _data.Decision = DecisionType.AnswerValidating;

            _data.Answerer.AnswerIsRight = validation.Value.Item1;
            _data.Answerer.AnswerValidationFactor = validation.Value.Item2;
            _data.ShowmanDecision = true;

            OnDecision();
        }
        else
        {
            _data.ShowmanDecision = false;

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
        _gameActions.SendMessage(
            BuildValidation2Message(_data.Answerer.Name, answer, _data.AnswerMode == StepParameterValues.AskAnswerMode_Button),
            _data.ShowMan.Name);
    }

    private string BuildValidation2Message(string name, string answer, bool allowPriceModifications, bool isCheckingForTheRight = true)
    {
        var question = _data.Question ?? throw new InvalidOperationException("Question is null");

        var rightAnswers = question.Right;
        var wrongAnswers = question.Wrong;

        ICollection<string> appellatedAnswers = Array.Empty<string>();

        if (_data.PackageStatistisProvider != null)
        {
            appellatedAnswers = _data.PackageStatistisProvider.GetAppellatedAnswers(
                Engine.RoundIndex,
                _data.ThemeIndex,
                _data.QuestionIndex);

            if (appellatedAnswers.Count > 0)
            {
                _data.Host.LogWarning($"Appellated answers count: {appellatedAnswers.Count}");
            }
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

        if (!IsSpecialQuestion())
        {
            _gameActions.SendMessageWithArgs(Messages.EndTry, _data.AnswererIndex);
        }
        else
        {
            _gameActions.SendMessageWithArgs(Messages.StopPlay);
        }

        var waitAnswerTime = IsSpecialQuestion() ? timeSettings.TimeForThinkingOnSpecial * 10 : timeSettings.TimeForPrintingAnswer * 10;

        var useAnswerOptions = _data.QuestionPlayState.AnswerOptions != null;
        _data.IsOralNow = _data.IsOral && _data.Answerer.IsHuman;

        if (useAnswerOptions)
        {
            if (_data.IsOralNow)
            {
                _gameActions.SendMessage(Messages.Answer, _data.ShowMan.Name);
            }

            _gameActions.SendMessage(CanPlayerAct() ? Messages.Answer : Messages.OralAnswer, _data.Answerer.Name);
        }
        else
        {
            if (_data.IsOralNow)
            {
                // Showman accepts answer orally
                SendAnswersInfoToShowman($"({LO[nameof(R.AnswerIsOral)]})");
                _gameActions.SendMessage(Messages.OralAnswer, _data.Answerer.Name);
            }
            else // The only place where we do not check CanPlayerAct()
            {
                _gameActions.SendMessage(Messages.Answer, _data.Answerer.Name);
            }
        }

        var answerReplic = useAnswerOptions ? ", " + LO[nameof(R.SelectAnswerOption)] : GetRandomString(LO[nameof(R.YourAnswer)]);
        _gameActions.ShowmanReplic(_data.Answerer.Name + answerReplic); // TODO: REMOVE (localized by MessageCode)
        _gameActions.ShowmanReplicNew(useAnswerOptions ? MessageCode.SelectAnswerOption : MessageCode.Answer, _data.Answerer.Name);

        _data.Answerer.Answer = "";

        var buttonPressMode = _data.Settings.AppSettings.ButtonPressMode;

        if (buttonPressMode != ButtonPressMode.FirstWins)
        {
            InformWrongTries();
        }

        _data.AnswerCount = 1;
        ScheduleExecution(Tasks.WaitAnswer, waitAnswerTime);
        WaitFor(DecisionType.Answering, waitAnswerTime, _data.AnswererIndex);
    }

    internal void SendQuestionAnswersToShowman()
    {
        var question = _data.Question;

        if (question == null || _data.QuestionPlayState.AnswerOptions != null)
        {
            return;
        }

        var rightAnswers = question.Right;
        var wrongAnswers = question.Wrong;

        ICollection<string> appellatedAnswers = Array.Empty<string>();

        if (_data.PackageStatistisProvider != null)
        {
            appellatedAnswers = _data.PackageStatistisProvider.GetAppellatedAnswers(
                Engine.RoundIndex,
                _data.ThemeIndex,
                _data.QuestionIndex);
        }

        var message = new MessageBuilder(Messages.QuestionAnswers, rightAnswers.Count + appellatedAnswers.Count)
            .AddRange(rightAnswers)
            .AddRange(appellatedAnswers)
            .AddRange(wrongAnswers)
            .Build();

        _gameActions.SendMessage(message, _data.ShowMan.Name);
    }

    private void InformWrongTries()
    {
        for (var i = 0; i < _data.PendingAnswererIndicies.Count; i++)
        {
            var playerIndex = _data.PendingAnswererIndicies[i];

            if (playerIndex == _data.PendingAnswererIndex)
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
        WaitFor(DecisionType.ThemeDeleting, waitTime, _data.Players.IndexOf(_data.ActivePlayer));
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

                var passMsg = new MessageBuilder(Messages.PlayerState, PlayerState.Pass).AddRange(candidatesAll.Select(i => (object)i));
                _gameActions.SendMessage(passMsg.ToString());

                if (TryDetectStakesWinner())
                {
                    return false;
                }

                _data.OrderIndex = -1;
                AskStake(false);
                return false;
            }

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
            // Остался последний игрок, выбор очевиден
            var leftIndex = candidatesAll[0];
            _data.Order[_data.OrderIndex] = leftIndex;
            CheckOrder(_data.OrderIndex);
        }

        return true;
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

            var playerIndex = _data.Order[_data.OrderIndex];

            var others = _data.Players.Where((p, index) => index != playerIndex); // Other players
            
            if (others.All(p => !p.StakeMaking) && _data.Stake > -1) // Others cannot make stakes
            {
                // Staker cannot raise anymore
                ScheduleExecution(Tasks.AnnounceStakesWinner, 10);
                return;
            }

            if (playerIndex < 0 || playerIndex >= _data.Players.Count)
            {
                throw new ArgumentException($"Bad {nameof(playerIndex)} value {playerIndex}! It must be in [0; {_data.Players.Count - 1}]");
            }

            var activePlayer = _data.Players[playerIndex];
            var playerMoney = activePlayer.Sum;

            if (_data.Stake != -1 && playerMoney <= _data.Stake || !activePlayer.StakeMaking) // Could not make stakes
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
            if (_data.Stake == -1 && (playerMoney < cost || playerMoney == cost && others.All(p => playerMoney >= p.Sum)))
            {
                var s = new StringBuilder(activePlayer.Name)
                    .Append(", ").Append(LO[nameof(R.YouCanSayOnly)])
                    .Append(' ').Append(LO[nameof(R.Nominal)]);

                _gameActions.ShowmanReplic(s.ToString());

                _data.Stakes.StakerIndex = playerIndex;
                _data.Stake = cost;
                _gameActions.SendMessageWithArgs(Messages.PersonStake, playerIndex, 1, cost);
                ScheduleExecution(Tasks.AskStake, 5, force: true);
                return;
            }

            var minimumStake = (_data.Stake != -1 ? _data.Stake : cost) + _data.StakeStep;
            var minimumStakeAligned = (int)Math.Ceiling((double)minimumStake / _data.StakeStep) * _data.StakeStep;

            _data.StakeTypes = StakeTypes.AllIn | (_data.Stake == -1 ? StakeTypes.Nominal : StakeTypes.Pass);

            if (!_data.AllIn && playerMoney >= minimumStakeAligned)
            {
                _data.StakeTypes |= StakeTypes.Stake;
            }

            _data.StakeVariants[0] = _data.Stake == -1;
            _data.StakeVariants[1] = !_data.AllIn && playerMoney != cost && playerMoney > _data.Stake + _data.StakeStep;
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
            stakeMsg2.Add(_data.StakeStep);

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

            var minimumStakeNew = _data.Stake != -1 ? _data.Stake + _data.StakeStep : cost;
            var minimumStakeAlignedNew = (int)Math.Ceiling((double)minimumStakeNew / _data.StakeStep) * _data.StakeStep;
            
            _data.StakeModes = StakeModes.AllIn;

            if (_data.Stake != -1)
            {
                _data.StakeModes |= StakeModes.Pass;
            }

            if (!_data.AllIn && playerMoney >= minimumStakeAlignedNew)
            {
                _data.StakeModes |= StakeModes.Stake;
            }

            var stakeLimit = new StakeSettings(minimumStakeAlignedNew, _data.ActivePlayer.Sum, _data.StakeStep);
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

    internal bool TryDetectStakesWinner()
    {
        var stakerCount = _data.Players.Count(p => p.StakeMaking);

        if (stakerCount == 1) // Answerer is detected
        {
            for (var i = 0; i < _data.Players.Count; i++)
            {
                if (_data.Players[i].StakeMaking)
                {
                    _data.Stakes.StakerIndex = i;
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

    private void OnStartAppellation()
    {
        if (_data.AppelaerIndex < 0 || _data.AppelaerIndex >= _data.Players.Count)
        {
            _tasksHistory.AddLogEntry($"OnStartAppellation resumed ({_taskRunner.PrintOldTasks()})");
            ResumeExecution(40);
            return;
        }
        
        _gameActions.SendMessageWithArgs(Messages.Appellation, '+');

        var appelaer = _data.Players[_data.AppelaerIndex];
        var isAppellationForRightAnswer = _data.AppellationCallerIndex == -1;

        var given = LO[appelaer.IsMale ? nameof(R.HeGave) : nameof(R.SheGave)];
        var apellationReplic = string.Format(LO[nameof(R.PleaseCheckApellation)], given);

        string origin = isAppellationForRightAnswer
            ? LO[nameof(R.IsApellating)]
            : string.Format(LO[nameof(R.IsConsideringWrong)], appelaer.Name);

        _gameActions.ShowmanReplic($"{appelaer.Name} {origin}. {apellationReplic}");

        var validation2Message = BuildValidation2Message(appelaer.Name, appelaer.Answer ?? "", false, isAppellationForRightAnswer);

        _data.AppellationAwaitedVoteCount = 0;
        _data.AppellationTotalVoteCount = _data.Players.Count(p => p.IsConnected) + 1; // players and showman
        _data.AppellationPositiveVoteCount = 0;
        _data.AppellationNegativeVoteCount = 0;

        // Showman vote
        if (isAppellationForRightAnswer)
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
                _data.Players[i].AppellationFlag = false;
                _data.AppellationPositiveVoteCount++;
            }
            else if (!isAppellationForRightAnswer && i == _data.AppellationCallerIndex)
            {
                _data.Players[i].AppellationFlag = false;
                _data.AppellationNegativeVoteCount++;
                _gameActions.SendMessageWithArgs(Messages.PersonApellated, i);
                _gameActions.SendMessageWithArgs(Messages.PlayerState, PlayerState.HasAnswered, i);
            }
            else if (_data.Players[i].IsConnected)
            {
                _data.AppellationAwaitedVoteCount++;
                _data.Players[i].AppellationFlag = true;
                _gameActions.SendMessage(validation2Message, _data.Players[i].Name);
            }
        }

        var waitTime = _data.AppellationAwaitedVoteCount > 0 ? _data.Settings.AppSettings.TimeSettings.TimeForShowmanDecisions * 10 : 1;
        ScheduleExecution(Tasks.WaitAppellationDecision, waitTime);
        WaitFor(DecisionType.Appellation, waitTime, -2);
    }

    internal int ResumeExecution(int resumeTime = 0) => _taskRunner.ResumeExecution(resumeTime, ShouldRunTimer());

    private void OnCheckAppellation()
    {
        try
        {
            if (_data.AppelaerIndex < 0 || _data.AppelaerIndex >= _data.Players.Count)
            {
                _tasksHistory.AddLogEntry($"CheckAppellation resumed ({_taskRunner.PrintOldTasks()})");
                return;
            }

            var votingForRight = _data.AppellationCallerIndex == -1;
            var positiveVoteCount = _data.AppellationPositiveVoteCount;
            var negativeVoteCount = _data.AppellationNegativeVoteCount;

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
            if (_data.QuestionPlayState.Appellations.Count == 0 || !ProcessNextAppellationRequest(false))
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

        var right = new List<object>();
        var wrong = new List<object>();
        var passed = new List<object>();

        for (var i = 0; i < _data.QuestionHistory.Count; i++)
        {
            var historyItem = _data.QuestionHistory[i];
            var index = historyItem.PlayerIndex;

            if (index < 0 || index >= _data.Players.Count)
            {
                continue;
            }

            var player = _data.Players[index];

            if (isVotingForRightAnswer && singleAnswerer && index != _data.AppelaerIndex)
            {
                if (!change)
                {
                    continue;
                }

                if (historyItem.IsRight)
                {
                    UndoRightSum(player, historyItem.Sum);
                }
                else
                {
                    UndoWrongSum(player, historyItem.Sum);
                }

                passed.Add(index);
            }
            else if (index == _data.AppelaerIndex)
            {
                if (singleAnswerer)
                {
                    change = true;

                    if (historyItem.IsRight)
                    {
                        UndoRightSum(player, historyItem.Sum);
                        SubtractWrongSum(player, _data.CurPriceWrong);

                        wrong.Add(index);
                    }
                    else
                    {
                        UndoWrongSum(player, historyItem.Sum);
                        AddRightSum(player, _data.CurPriceRight);

                        right.Add(index);

                        // TODO: that should be handled by question selection strategy
                        if (Engine.CanMoveBack) // Not the beginning of a round
                        {
                            _data.ChooserIndex = index;
                            _gameActions.SendMessageWithArgs(Messages.SetChooser, _data.ChooserIndex);
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

    private void OnTheme(Theme theme, int arg, bool isFull)
    {
        var informed = false;

        if (arg == 1)
        {
            var authors = _data.PackageDoc.ResolveAuthors(theme.Info.Authors);

            if (authors.Length > 0)
            {
                informed = true;
                var res = new StringBuilder();
                res.AppendFormat(OfObjectPropertyFormat, LO[nameof(R.PAuthors)], LO[nameof(R.OfTheme)], string.Join(", ", authors));
                _gameActions.ShowmanReplic(res.ToString()); // TODO: REMOVE (replaced by THEME message)
            }
            else
            {
                arg++;
            }
        }

        if (arg == 2)
        {
            var sources = _data.PackageDoc.ResolveSources(theme.Info.Sources);

            if (sources.Count > 0)
            {
                informed = true;
                var res = new StringBuilder();
                res.AppendFormat(OfObjectPropertyFormat, LO[nameof(R.PSources)], LO[nameof(R.OfTheme)], string.Join(", ", sources));
                _gameActions.ShowmanReplic(res.ToString()); // TODO: REMOVE (replaced by THEME message)
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
                ScheduleExecution(Tasks.QuestionStartInfo, delay, 1, force: !informed);
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

        var baseTime = 0;

        if (stage == 1)
        {
            var authors = _data.PackageDoc.ResolveAuthors(package.Info.Authors);

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

            var sources = _data.PackageDoc.ResolveSources(package.Info.Sources);

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

            _data.Stage = GameStage.Round;
            OnStageChanged(GameStages.Round, roundName, roundIndex + 1, _data.Rounds.Length);

            _gameActions.InformRound(roundName, roundIndex, _data.RoundStrategy);
            _gameActions.InformRoundContent();
            _data.InformStages |= InformStages.RoundContent;

            _gameActions.SystemReplic(" "); // new line // TODO: REMOVE: replaced by STAGE message
            _gameActions.SystemReplic(roundName); // TODO: REMOVE: replaced by STAGE message

            var authors = _data.PackageDoc.ResolveAuthors(round.Info.Authors);

            if (authors.Length > 0)
            {
                var msg = new MessageBuilder(Messages.RoundAuthors).AddRange(authors);
                _gameActions.SendMessage(msg.ToString());
            }

            var sources = _data.PackageDoc.ResolveSources(round.Info.Sources);

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
                var ad = _data.Host.GetAd(LO.Culture.TwoLetterISOLanguageName, out int adId);

                if (!string.IsNullOrEmpty(ad))
                {
                    informed = true;

                    _gameActions.SendMessageWithArgs(Messages.Ads, ad);

#if !DEBUG
                    // Advertisement could not be skipped
                    _data.MoveNextBlocked = !_data.Settings.AppSettings.Managed;
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
        _data.AnswererIndex = _data.ChooserIndex;
        _data.QuestionPlayState.SetSingleAnswerer(_data.ChooserIndex);

        _gameActions.SendMessageWithArgs(Messages.SetChooser, _data.ChooserIndex, '+');

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

        var optionCount = _data.Players.Count(player => player.Flag);

        if (optionCount == 1)
        {
            for (var i = 0; i < _data.Players.Count; i++)
            {
                if (_data.Players[i].Flag)
                {
                    _data.ChooserIndex = _data.AnswererIndex = i;
                    _data.QuestionPlayState.SetSingleAnswerer(i);
                    _gameActions.SendMessageWithArgs(Messages.SetChooser, _data.ChooserIndex);
                }
            }

            _gameActions.ShowmanReplic($"{_data.Answerer.Name}, {LO[nameof(R.CatIsYours)]}!");
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

        var hasConnectedPlayers = _data.Players.Any(p => p.IsConnected);

        for (var i = 0; i < _data.Players.Count; i++)
        {
            if (_data.Players[i].IsConnected || !hasConnectedPlayers)
            {
                allConnectedIndicies.Add(i);
            }
            else
            {
                allDisconnectedIndicies.Add(i);
            }
        }

        _data.QuestionPlayState.SetMultipleAnswerers(allConnectedIndicies);
        
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

    // TODO: think about always using only complex answers as simple answer is a subset of a complex one
    internal void OnSimpleAnswer(string answer)
    {
        var normalizedAnswer = answer.LeaveFirst(MaxAnswerLength);
        _gameActions.SendVisualMessageWithArgs(Messages.RightAnswer, ContentTypes.Text, normalizedAnswer);

        var answerTime = GetReadingDurationForTextLength(normalizedAnswer.Length)
            + _data.Settings.AppSettings.TimeSettings.TimeForRightAnswer * 10;
        
        ScheduleExecution(Tasks.MoveNext, answerTime);
    }

    internal void OnComplexAnswer()
    {
        var answer = _data.Question?.Right.FirstOrDefault() ?? ""; // TODO: this value should come from engine

        if (_data.QuestionPlayState.AnswerOptions != null)
        {
            _data.RightOptionLabel = answer;
            var answerIndex = Array.FindIndex(_data.QuestionPlayState.AnswerOptions, o => o.Label == answer);

            if (answerIndex > -1)
            {
                _gameActions.SendMessageWithArgs(Messages.ContentState, ContentPlacements.Screen, answerIndex + 1, ItemState.Right);
            }
        }

        _gameActions.SendMessageWithArgs(Messages.RightAnswerStart, ContentTypes.Text, answer);
    }

    internal void OnRightAnswerOption(string rightOptionLabel)
    {
        _data.RightOptionLabel = rightOptionLabel;
        _gameActions.SendMessageWithArgs(Messages.RightAnswer, ContentTypes.Text, rightOptionLabel);
        var answerTime = _data.Settings.AppSettings.TimeSettings.TimeForRightAnswer;
        answerTime = (answerTime == 0 ? 2 : answerTime) * 10;
        ScheduleExecution(Tasks.MoveNext, answerTime);
    }

    private bool DetectRoundTimeout()
    {
        var roundDuration = DateTime.UtcNow.Subtract(_data.TimerStartTime[0]).TotalMilliseconds / 100;

        if (_data.Stage == GameStage.Round && roundDuration >= _data.Settings.AppSettings.TimeSettings.TimeOfRound * 10)
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

        if (HaveMultipleAnswerers() && _data.QuestionPlayState.ValidateAfterRightAnswer)
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
        if (_data.AnnouncedAnswerersEnumerator != null)
        {
            _data.AnnouncedAnswerersEnumerator.Reset();

            if (_data.QuestionPlayState.HiddenStakes)
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
        if (_data.AnnouncedAnswerersEnumerator == null)
        {
            return;
        }

        while (_data.AnnouncedAnswerersEnumerator.MoveNext())
        {
            var answererIndex = _data.AnnouncedAnswerersEnumerator.Current;

            if (answererIndex < 0 || answererIndex >= _data.Players.Count)
            {
                continue;
            }

            var answerer = _data.Players[answererIndex];
            var isRight = answerer.Answer == _data.RightOptionLabel;

            var message = new MessageBuilder(Messages.Person);
            int outcome;

            if (isRight)
            {
                message.Add('+');
                AddRightSum(answerer, _data.CurPriceRight);
                outcome = _data.CurPriceRight;
            }
            else
            {
                message.Add('-');
                SubtractWrongSum(answerer, _data.CurPriceWrong);
                outcome = _data.CurPriceWrong;
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
                _data.CurPriceRight = _minRoundPrice;
                _data.CurPriceWrong = _data.CurPriceRight;
                _gameActions.SendMessageWithArgs(Messages.PersonStake, _data.AnswererIndex, 1, _data.CurPriceRight);
                ScheduleExecution(Tasks.MoveNext, 1);
            }
            else
            {
                _data.CurPriceRight = -1;
                _data.StakeRange = new StakeSettings(_minRoundPrice, _maxRoundPrice, _maxRoundPrice - _minRoundPrice);

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
            _data.StakeRange = new StakeSettings(availableRange.Minimum, availableRange.Maximum, availableRange.Step);

            ScheduleExecution(Tasks.AskToSelectQuestionPrice, 1, force: true);
        }
    }

    internal void SetAnswererByHighestVisibleStake()
    {
        if (_data.Question == null)
        {
            throw new InvalidOperationException("_data.Question == null");
        }

        var nominal = _data.Question.Price;

        if (_data.ChooserIndex == -1)
        {
            _data.ChooserIndex = DetectPlayerIndexWithLowestSum(); // TODO: set chooser index at the beginning of round
        }

        _data.Order = new int[_data.Players.Count];
        var passes = new List<object>();

        for (var i = 0; i < _data.Players.Count; i++)
        {
            var canMakeStake = i == _data.ChooserIndex || _data.Players[i].Sum > nominal;
            _data.Players[i].StakeMaking = canMakeStake;
            _data.Order[i] = -1;

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

        _data.Stake = -1;
        Stakes.Reset(_data.ChooserIndex);
        _data.Order[0] = _data.ChooserIndex;
        _data.OrderHistory.Clear();

        _data.OrderHistory.Append("Stake making. Initial state. ")
            .Append("Sums: ")
            .Append(string.Join(",", _data.Players.Select(p => p.Sum)))
            .Append("StakeMaking: ")
            .Append(string.Join(",", _data.Players.Select(p => p.StakeMaking)))
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
        return _data.Players.TakeWhile(p => p.Sum != minSum).Count();
    }

    internal void SetAnswerersByAllHiddenStakes()
    {
        var answerers = new List<int>();
        var passes = new List<object>();

        var hasConnectedPlayers = _data.Players.Any(p => p.IsConnected);

        for (var i = 0; i < _data.Players.Count; i++)
        {
            if ((!hasConnectedPlayers || _data.Players[i].IsConnected) &&
                (_data.Players[i].Sum > 0 || _data.Settings.AppSettings.AllowEveryoneToPlayHiddenStakes))
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

        _data.QuestionPlayState.SetMultipleAnswerers(answerers);
        _data.QuestionPlayState.HiddenStakes = true;
        AskHiddenStakes();
    }

    internal void OnMultiplyPrice()
    {
        if (_data.ChooserIndex == -1)
        {
            _data.ChooserIndex = DetectPlayerIndexWithLowestSum(); // TODO: set chooser index at the beginning of round
        }

        var factor = _data.Settings.AppSettings.QuestionForYourselfFactor;

        _data.CurPriceRight *= factor;
        _data.CurPriceWrong *= factor;

        if (factor != 1 || _data.CurPriceRight != _data.CurPriceWrong)
        {
            var replic = string.Format(
                LO[nameof(R.QuestionForYourselfInfo)],
                Notion.FormatNumber(_data.CurPriceRight),
                Notion.FormatNumber(_data.CurPriceWrong),
                factor);

            _gameActions.ShowmanReplic($"{_data.Chooser!.Name}, {replic}");
        }

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

        AddRightSum(_data.Answerer, _data.CurPriceRight);
        _data.ChooserIndex = _data.AnswererIndex;
        _gameActions.SendMessageWithArgs(Messages.SetChooser, _data.ChooserIndex);
        _gameActions.InformSums();

        _data.SkipQuestion?.Invoke();
        ScheduleExecution(Tasks.MoveNext, 20, 1);
    }

    internal void OnAnswerOptions()
    {
        _gameActions.InformLayout();
        _data.InformStages |= InformStages.Layout;
        _data.LastVisualMessage = null;
        _data.ComplexVisualState = new IReadOnlyList<string>[1 + (_data.QuestionPlayState.AnswerOptions?.Length ?? 0)];
    }

    internal void ShowAnswerOptions(Action? continuation)
    {
        if (_data.QuestionPlayState.AnswerOptions == null)
        {
            throw new InvalidOperationException("AnswerOptions == null");
        }

        var nextTask = _data.QuestionPlayState.AnswerOptions.Length > 0 ? Tasks.ShowNextAnswerOption : Tasks.MoveNext;
        ScheduleExecution(nextTask, 1, 0);
        _continuation = continuation;
    }

    internal void ShowNextAnswerOption(int optionIndex)
    {
        if (_data.QuestionPlayState.AnswerOptions == null)
        {
            throw new InvalidOperationException("AnswerOptions == null");
        }

        var answerOption = _data.QuestionPlayState.AnswerOptions[optionIndex];        

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

        if (_data.ComplexVisualState != null && optionIndex + 1 < _data.ComplexVisualState.Length)
        {
            _data.ComplexVisualState[optionIndex + 1] = new string[] { messageBuilder.ToString() };
        }

        var nextTask = optionIndex + 1 < _data.QuestionPlayState.AnswerOptions.Length ? Tasks.ShowNextAnswerOption : Tasks.MoveNext;
        ScheduleExecution(nextTask, _data.Settings.AppSettings.DisplayAnswerOptionsOneByOne ? contentDuration : 1, optionIndex + 1);
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

                    if (_data.QuestionPlayState.IsAnswer)
                    {
                        duration += _data.Settings.AppSettings.TimeSettings.TimeForRightAnswer * 10;
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
                            _data.IsPlayingMedia = true;
                            _data.IsPlayingMediaPaused = false;

                            _data.QuestionPlayState.MediaContentCompletions[(contentItem.Type, globalUri)] = new Completion(_data.ActiveHumanCount);
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

        _data.ComplexVisualState ??= new IReadOnlyList<string>[1];
        _data.ComplexVisualState[0] = visualState;
        _data.IsPartial = false;
        _data.AtomStart = DateTime.UtcNow;
        _data.AtomTime = contentTime;
        ScheduleExecution(Tasks.MoveNext, contentTime);
        _data.TimeThinking = 0.0;
    }

    private static string GetRandomString(string resource) => Random.Shared.GetRandomString(resource);

    private int GetContentItemDefaultDuration(ContentItem contentItem) => contentItem.Type switch
    {
        ContentTypes.Text => GetReadingDurationForTextLength(contentItem.Value.Length),
        ContentTypes.Image or ContentTypes.Html => TimeSettings.ImageTime * 10,
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
        if (!_data.Statistics.TryGetValue(name, out var statistic))
        {
            _data.Statistics[name] = statistic = new PlayerStatistic();
        }

        return statistic;
    }
}

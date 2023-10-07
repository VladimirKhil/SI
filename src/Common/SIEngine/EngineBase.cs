using SIEngine.Core;
using SIEngine.Rules;
using SIPackages;
using SIPackages.Core;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SIEngine;

/// <inheritdoc cref="ISIEngine" />
public abstract class EngineBase : ISIEngine, IDisposable, INotifyPropertyChanged
{
    private bool _isDisposed = false;

    protected ISIEnginePlayHandler PlayHandler { get; }

    protected Func<EngineOptions> OptionsProvider { get; }

    private readonly CancellationTokenSource _cancellationTokenSource = new();

    protected GameStage _stage = GameStage.Begin;

    /// <summary>
    /// Current game state.
    /// </summary>
    public GameStage Stage
    {
        get => _stage;
        protected set
        {
            if (_stage != value)
            {
                _stage = value;
                OnPropertyChanged();
            }
        }
    }

    protected abstract GameRules GameRules { get; }

    protected readonly SIDocument _document;

    public SIDocument Document => _document;

    protected int _roundIndex = -1, _themeIndex = 0, _questionIndex = 0;

    protected Round? _activeRound;

    protected Theme? _activeTheme;

    protected Question? _activeQuestion;

    protected bool _inAutoMode;

    protected object _autoLock = new();

    protected bool _timeout = false;

    private bool _canMoveNext = true;

    private bool _canMoveBack;

    private bool _canMoveNextRound;

    private bool _canMoveBackRound;

    public string PackageName => _document.Package.Name;

    public string ContactUri => _document.Package.ContactUri;

    public int RoundIndex => _roundIndex;

    public int ThemeIndex => _themeIndex;

    public int QuestionIndex => _questionIndex;

    private readonly QuestionEngineFactory _questionEngineFactory;

    protected QuestionEngine? QuestionEngine { get; private set; }

    public bool CanMoveNext
    {
        get => _canMoveNext;
        protected set
        {
            if (_canMoveNext != value)
            {
                _canMoveNext = value;
                OnPropertyChanged();
            }
        }
    }

    public bool CanMoveBack
    {
        get => _canMoveBack;
        protected set
        {
            if (_canMoveBack != value)
            {
                _canMoveBack = value;
                OnPropertyChanged();
                UpdateCanMoveNextRound();
            }
        }
    }

    public bool CanMoveNextRound
    {
        get => _canMoveNextRound;
        protected set
        {
            if (_canMoveNextRound != value)
            {
                _canMoveNextRound = value;
                OnPropertyChanged();
            }
        }
    }
    public bool CanMoveBackRound
    {
        get => _canMoveBackRound;
        protected set
        {
            if (_canMoveBackRound != value)
            {
                _canMoveBackRound = value;
                OnPropertyChanged();
            }
        }
    }

    public bool CanChangeSum() =>
        _stage == GameStage.Question
        || _stage == GameStage.RoundTable
        || _stage == GameStage.AfterFinalThink;

    public bool IsQuestion() => _stage == GameStage.Question;

    public void OnIntroFinished()
    {
        if (_stage == GameStage.GameThemes)
        {
            AutoNext(1000);
        }
    }

    /// <summary>
    /// Is engine currently staying in the intro stage.
    /// </summary>
    public bool IsIntro() => _roundIndex == -1;

    #region Events

    public event Action<Package>? Package;
    public event Action<string[]>? GameThemes;
    public event Action<bool>? NextRound;
    public event Action<Round>? Round;
    public event Action? RoundSkip;
    public event Action<Theme[]>? RoundThemes;
    public event Action<Theme>? Theme;
    public event Action<Question>? Question;
    public event Action<int, int, Theme, Question>? QuestionSelected;

    public event Action? QuestionPostInfo;

    public event Action? ShowScore;
    public event Action? LogScore;
    public event Action<int, int>? EndQuestion;
    public event Action? QuestionFinish;
    public event Action? RoundEmpty;
    public event Action? NextQuestion;
    public event Action? RoundTimeout;

    public event Action<Theme[], bool, bool>? FinalThemes;
    public event Action? WaitDelete;
    public event Action<int>? ThemeSelected;
    public event Action<Theme, Question>? PrepareFinalQuestion;

    [Obsolete]
    public event Action<string>? Sound;

    public event Action<string>? Error;

    public event Action? EndGame;

    #endregion

    protected EngineBase(
        SIDocument document,
        Func<EngineOptions> optionsProvider,
        ISIEnginePlayHandler playHandler,
        QuestionEngineFactory questionEngineFactory)
    {
        _document = document ?? throw new ArgumentNullException(nameof(document));
        OptionsProvider = optionsProvider ?? throw new ArgumentNullException(nameof(optionsProvider));
        PlayHandler = playHandler;
        _questionEngineFactory = questionEngineFactory;
    }

    /// <summary>
    /// Moves engine to the next game state.
    /// </summary>
    public abstract void MoveNext();

    public abstract Tuple<int, int, int> MoveBack();

    #region Fire events

    protected void OnPackage(Package package) => Package?.Invoke(package);

    protected void OnGameThemes(string[] gameThemes) => GameThemes?.Invoke(gameThemes);

    protected void OnNextRound(bool showSign = true) => NextRound?.Invoke(showSign);

    protected void OnRound(Round round) => Round?.Invoke(round);

    protected void OnRoundSkip() => RoundSkip?.Invoke();

    protected void OnRoundThemes(Theme[] roundThemes) => RoundThemes?.Invoke(roundThemes);

    protected void OnTheme(Theme theme) => Theme?.Invoke(theme);

    protected void OnQuestion(Question question) => Question?.Invoke(question);

    protected void OnQuestionSelected(int themeIndex, int questionIndex, Theme theme, Question question) =>
        QuestionSelected?.Invoke(themeIndex, questionIndex, theme, question);

    protected void OnQuestionPostInfo() => QuestionPostInfo?.Invoke();

    protected void OnShowScore() => ShowScore?.Invoke();

    protected void OnLogScore() => LogScore?.Invoke();

    protected void OnQuestionFinish() => QuestionFinish?.Invoke();

    protected void OnEndQuestion(int themeIndex, int questionIndex) => EndQuestion?.Invoke(themeIndex, questionIndex);

    protected void OnRoundEmpty() => RoundEmpty?.Invoke();

    protected void OnNextQuestion() => NextQuestion?.Invoke();

    protected void OnRoundTimeout() => RoundTimeout?.Invoke();

    protected void OnFinalThemes(Theme[] finalThemes, bool willPlayAllThemes, bool isFirstPlay) =>
        FinalThemes?.Invoke(finalThemes, willPlayAllThemes, isFirstPlay);

    protected void OnWaitDelete() => WaitDelete?.Invoke();

    protected void OnThemeSelected(int themeIndex) => ThemeSelected?.Invoke(themeIndex);

    protected void OnPrepareFinalQuestion(Theme theme, Question question) => PrepareFinalQuestion?.Invoke(theme, question);

    [Obsolete]
    protected void OnSound(string name = "") => Sound?.Invoke(name);

    protected void OnError(string error) => Error?.Invoke(error);

    protected void OnEndGame() => EndGame?.Invoke();

    #endregion

    public object SyncRoot { get; } = new object();

    /// <summary>
    /// Number of unanswered questions in round.
    /// </summary>
    public abstract int LeftQuestionsCount { get; }

    /// <summary>
    /// Move game futher automatically after specified amount of time.
    /// </summary>
    /// <param name="milliseconds">Amount of time in milliseconds to delay before moving futher.</param>
    protected async void AutoNext(int milliseconds)
    {
        try
        {
            if (milliseconds < 0)
            {
                throw new ArgumentException(
                    $"Value of milliseconds ({milliseconds}) must be greater or equal to 0",
                    nameof(milliseconds));
            }

            if (OptionsProvider().AutomaticGame)
            {
                lock (_autoLock)
                {
                    if (_inAutoMode)
                    {
                        return;
                    }

                    _inAutoMode = true;
                }

                await Task.Delay(milliseconds, _cancellationTokenSource.Token);

                lock (SyncRoot)
                {
                    if (_isDisposed)
                    {
                        return;
                    }

                    AutoNextCore();
                    MoveNext();
                }

                lock (_autoLock)
                {
                    _inAutoMode = false;
                }
            }
        }
        catch (TaskCanceledException) { }
        catch (Exception exc)
        {
            OnError($"Engine error: {exc}");
        }
    }

    protected virtual void AutoNextCore() { }

    /// <summary>
    /// Moves to the next round.
    /// </summary>
    /// <param name="showSign">Should the logo be shown.</param>
    public virtual bool MoveNextRound(bool showSign = true)
    {
        var moved = true;
        do
        {
            if (_roundIndex + 1 < _document.Package.Rounds.Count)
            {
                _roundIndex++;
                SetActiveRound();
                Stage = GameStage.Round;
            }
            else
            {
                Stage = GameStage.End;
                OnEndGame();
                moved = false;
            }
        } while (_stage == GameStage.Round && !AcceptRound(_activeRound));

        CanMoveBack = false;
        UpdateCanMoveNextRound();
        UpdateCanNext();

        if (moved)
        {
            OnNextRound(showSign);
        }

        return moved;
    }

    protected void UpdateCanMoveNextRound() => CanMoveNextRound = _roundIndex + 1 < _document.Package.Rounds.Count;

    protected void UpdateCanMoveBackRound() => CanMoveBackRound = _roundIndex > 0 || CanMoveBack;

    public virtual bool MoveToRound(int roundIndex, bool showSign = true)
    {
        if (_roundIndex == roundIndex)
        {
            if (CanMoveBack)
            {
                return MoveBackRound();
            }

            return false;
        }

        if (roundIndex < 0 ||
            roundIndex >= _document.Package.Rounds.Count ||
            !AcceptRound(_document.Package.Rounds[roundIndex]))
        {
            return false;
        }

        _roundIndex = roundIndex;
        SetActiveRound();
        Stage = GameStage.Round;

        CanMoveBack = false;
        UpdateCanMoveNextRound();
        UpdateCanNext();
        OnNextRound(showSign);

        return true;
    }

    public static bool AcceptRound(Round? round) =>
        round != null
        && round.Themes.Any(theme => theme.Questions.Any(q => q.Price != SIPackages.Question.InvalidPrice)
        && (round.Type != RoundTypes.Final || round.Themes.Any(theme => theme.Name != null)));

    public virtual bool MoveBackRound()
    {
        if (!CanMoveBackRound)
        {
            return false;
        }

        var moved = true;

        if (CanMoveBack)
        {
            Stage = GameStage.Round;
        }
        else
        {
            do
            {
                if (_roundIndex == 0)
                {
                    // Cannot find suitable round while moving back. So returning forward to the first matching round
                    return MoveNextRound();
                }

                _roundIndex--;
                SetActiveRound();

                Stage = GameStage.Round;
            } while (!AcceptRound(_activeRound));
        }

        CanMoveBack = false;
        UpdateCanNext();

        return moved;
    }

    public abstract bool CanNext();

    public void UpdateCanNext() => CanMoveNext = CanNext();

    protected void SetActiveRound() => _activeRound = _roundIndex < _document.Package.Rounds.Count ? _document.Package.Rounds[_roundIndex] : null;

    public void SetTimeout() => _timeout = true;

    public void SkipQuestion()
    {
        if (_activeRound == null)
        {
            throw new InvalidOperationException("_activeRound is null");
        }

        Stage = _activeRound.Type != RoundTypes.Final ? GameStage.EndQuestion : GameStage.AfterFinalThink;
    }

    /// <summary>
    /// Skips rest of the question and goes directly to answer.
    /// </summary>
    public void MoveToAnswer()
    {
        if (QuestionEngine == null)
        {
            throw new InvalidOperationException("QuestionEngine == null");
        }

        QuestionEngine.MoveToAnswer();
    }

    protected void OnQuestion()
    {
        if (QuestionEngine == null)
        {
            throw new InvalidOperationException("QuestionEngine == null");
        }

        if (!QuestionEngine.PlayNext())
        {
            OnQuestionPostInfo();
            Stage = _activeRound.Type != RoundTypes.Final ? GameStage.EndQuestion : GameStage.AfterFinalThink;
            AutoNext(3000);
        }
    }

    protected void OnFinalQuestion()
    {
        if (QuestionEngine == null)
        {
            throw new InvalidOperationException("QuestionEngine == null");
        }

        if (!QuestionEngine.PlayNext())
        {
            OnQuestionPostInfo();
            Stage = _activeRound.Type != RoundTypes.Final ? GameStage.EndQuestion : GameStage.AfterFinalThink;
            AutoNext(3000);
        }
    }

    public void EndRound()
    {
        OnRoundEmpty();
        DoFinishRound();
    }

    /// <summary>
    /// Ends round and optionally shows current score.
    /// </summary>
    protected void DoFinishRound()
    {
        OnLogScore();

        if (OptionsProvider().ShowScore)
        {
            Stage = GameStage.Score;
            OnShowScore();
            UpdateCanNext();
        }
        else
        {
            MoveNextRound();
        }

        AutoNext(5000);
    }

    protected void OnMoveToQuestion()
    {
        var isFinal = _activeRound!.Type == RoundTypes.Final;
        Stage = isFinal ? GameStage.FinalQuestion : GameStage.Question;

        var options = OptionsProvider();

        if (_activeQuestion == null)
        {
            throw new InvalidOperationException("_activeQuestion == null");
        }

        QuestionEngine = _questionEngineFactory.CreateEngine(
            _activeQuestion,
            new QuestionEngineOptions
            {
                FalseStarts = options.IsPressMode
                    ? (options.IsMultimediaPressMode ? FalseStartMode.Enabled : FalseStartMode.TextContentOnly)
                    : FalseStartMode.Disabled,

                ShowSimpleRightAnswers = options.ShowRight,

                DefaultTypeName = GameRules.GetRulesForRoundType(_activeRound!.Type).DefaultQuestionType,
                ForceDefaultTypeName = isFinal
            });
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed)
        {
            return;
        }

        _cancellationTokenSource.Cancel();
        _document.Dispose();

        _isDisposed = true;
    }

    public abstract void SelectQuestion(int themeIndex, int questionIndex);

    public abstract int OnReady(out bool more);

    public abstract void SelectTheme(int publicThemeIndex);

    public abstract bool RemoveQuestion(int themeIndex, int questionIndex);

    public abstract int? RestoreQuestion(int themeIndex, int questionIndex);
}

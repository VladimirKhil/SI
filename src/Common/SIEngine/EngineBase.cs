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

    protected Round ActiveRound
    {
        get
        {
            if (_activeRound == null)
            {
                throw new InvalidOperationException("_activeRound == null");
            }

            return _activeRound;
        }
    }

    protected Theme? _activeTheme;

    protected Question? _activeQuestion;

    protected Question ActiveQuestion
    {
        get
        {
            if (_activeQuestion == null)
            {
                throw new InvalidOperationException("_activeQuestion == null");
            }

            return _activeQuestion;
        }
    }

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

    private QuestionEngine? _questionEngine;

    protected QuestionEngine QuestionEngine
    {
        get
        {
            if (_questionEngine == null)
            {
                throw new InvalidOperationException("_questionEngine == null");
            }

            return _questionEngine;
        }
    }

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
                UpdateCanMoveBackRound();
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

    #region Events

    public event Action<Package>? Package;
    public event Action<IEnumerable<string>>? GameThemes;
    public event Action<bool>? NextRound;
    public event Action<Round>? Round;
    public event Action? RoundSkip;
    public event Action<Theme>? Theme;
    public event Action<Question>? Question;

    public event Action? QuestionPostInfo;

    public event Action? ShowScore;
    public event Action? LogScore;
    public event Action<int, int>? EndQuestion;
    public event Action? QuestionFinish;
    public event Action? RoundEmpty;
    public event Action? NextQuestion;
    public event Action? RoundTimeout;

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

    protected void OnGameThemes(IEnumerable<string> gameThemes) => GameThemes?.Invoke(gameThemes);

    protected void OnNextRound(bool showSign = true) => NextRound?.Invoke(showSign);

    protected void OnRound(Round round) => Round?.Invoke(round);

    protected void OnRoundSkip() => RoundSkip?.Invoke();

    protected void OnTheme(Theme theme) => Theme?.Invoke(theme);

    protected void OnQuestion(Question question) => Question?.Invoke(question);

    protected void OnQuestionPostInfo() => QuestionPostInfo?.Invoke();

    protected void OnShowScore() => ShowScore?.Invoke();

    protected void OnLogScore() => LogScore?.Invoke();

    protected void OnQuestionFinish() => QuestionFinish?.Invoke();

    protected void OnEndQuestion(int themeIndex, int questionIndex) => EndQuestion?.Invoke(themeIndex, questionIndex);

    protected void OnRoundEmpty() => RoundEmpty?.Invoke();

    protected void OnNextQuestion() => NextQuestion?.Invoke();

    protected void OnRoundTimeout() => RoundTimeout?.Invoke();

    protected void OnError(string error) => Error?.Invoke(error);

    protected void OnEndGame() => EndGame?.Invoke();

    #endregion

    public object SyncRoot { get; } = new object();

    /// <summary>
    /// Moves to the next round.
    /// </summary>
    /// <param name="showSign">Should the logo be shown.</param>
    public virtual bool MoveNextRound(bool showSign = true)
    {
        var moved = true;

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

        CanMoveBack = false;
        UpdateCanMoveNextRound();
        UpdateCanMoveBackRound();
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

        if (roundIndex < 0 || roundIndex >= _document.Package.Rounds.Count)
        {
            return false;
        }

        _roundIndex = roundIndex;
        SetActiveRound();
        Stage = GameStage.Round;

        CanMoveBack = false;
        UpdateCanMoveNextRound();
        UpdateCanMoveBackRound();
        UpdateCanNext();
        OnNextRound(showSign);

        return true;
    }

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
            if (_roundIndex == 0)
            {
                // Cannot find suitable round while moving back. So returning forward to the first matching round
                return MoveNextRound();
            }

            _roundIndex--;
            SetActiveRound();

            Stage = GameStage.Round;
        }

        CanMoveBack = false;
        UpdateCanNext();
        UpdateCanMoveNextRound();
        UpdateCanMoveBackRound();

        return moved;
    }

    public abstract bool CanNext();

    public void UpdateCanNext() => CanMoveNext = CanNext();

    protected void SetActiveRound() => _activeRound = _roundIndex < _document.Package.Rounds.Count ? _document.Package.Rounds[_roundIndex] : null;

    public void SetTimeout() => _timeout = true;

    public void SkipQuestion() => Stage = GameStage.EndQuestion;

    /// <summary>
    /// Skips rest of the question and goes directly to answer.
    /// </summary>
    public void MoveToAnswer() => QuestionEngine.MoveToAnswer();

    protected void OnQuestion()
    {
        if (!QuestionEngine.PlayNext())
        {
            OnQuestionPostInfo();
            Stage = GameStage.EndQuestion;
        }
    }

    protected void EndRound()
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
    }

    protected void OnMoveToQuestion()
    {
        var isFinal = ActiveRound.Type == RoundTypes.Final;
        Stage = GameStage.Question;

        var options = OptionsProvider();

        _questionEngine = _questionEngineFactory.CreateEngine(
            ActiveQuestion,
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

        _document.Dispose();

        _isDisposed = true;
    }
}

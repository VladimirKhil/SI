using SIEngine.Core;
using SIEngine.Models;
using SIEngine.QuestionSelectionStrategies;
using SIEngine.Rules;
using SIPackages;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SIEngine;

/// <summary>
/// Plays SIGame package and calls play handler callbacks.
/// </summary>
/// <remarks>
/// Uses selection strategy to select questions in each game round. Then tranfers question play to question engine.
/// The strategy is selected by game rules and round type.
/// </remarks>
public sealed class GameEngine : EngineBase, INotifyPropertyChanged
{
    private readonly IQuestionEngineFactory _questionEngineFactory;
    private readonly ISIEnginePlayHandler _playHandler;
    private readonly Func<EngineOptions> _optionsProvider;
    private readonly GameRules _gameRules;

    private ISelectionStrategy? _selectionStrategy;

    private ISelectionStrategy SelectionStrategy => _selectionStrategy ?? throw new InvalidOperationException("_selectionStrategy == null");

    private IQuestionEngine? _questionEngine;

    private IQuestionEngine QuestionEngine => _questionEngine ?? throw new InvalidOperationException("_questionEngine == null");


    private GameStage _stage = GameStage.Begin;

    // TODO: hide
    /// <summary>
    /// Current game state.
    /// </summary>
    public GameStage Stage
    {
        get => _stage;
        private set
        {
            if (_stage != value)
            {
                _stage = value;
                OnPropertyChanged();
            }
        }
    }

    private RoundEndReason _roundEndReason;

    private bool _canMoveNext = true;

    private bool _canMoveBack;

    private bool _canMoveNextRound;

    private bool _canMoveBackRound;

    public bool CanMoveNext
    {
        get => _canMoveNext;
        private set
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
        private set
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
        private set
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
        private set
        {
            if (_canMoveBackRound != value)
            {
                _canMoveBackRound = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private Round? _activeRound;

    private Round ActiveRound
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

    private Theme? _activeTheme;

    private Question? _activeQuestion;

    private Question ActiveQuestion
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

    private int _roundIndex = -1, _themeIndex = 0, _questionIndex = 0;

    public string PackageName => _document.Package.Name;

    public string ContactUri => _document.Package.ContactUri;

    public int RoundIndex => _roundIndex;

    public int ThemeIndex => _themeIndex;

    public int QuestionIndex => _questionIndex;

    public GameEngine(
        SIDocument document,
        GameRules gameRules,
        Func<EngineOptions> optionsProvider,
        ISIEnginePlayHandler playHandler,
        IQuestionEngineFactory questionEngineFactory)
        : base(document)
    {
        _gameRules = gameRules;
        _optionsProvider = optionsProvider;
        _playHandler = playHandler;
        _questionEngineFactory = questionEngineFactory;
    }

    /// <summary>
    /// Moves to the next game stage.
    /// </summary>
    public void MoveNext()
    {
        switch (_stage)
        {
            case GameStage.Begin:
                OnBegin();
                break;

            case GameStage.GameThemes:
                OnGameThemes();
                break;

            case GameStage.Round:
                OnRound();
                break;

            case GameStage.SelectingQuestion:
                OnSelectingQuestion();
                break;

            case GameStage.QuestionType:
                OnQuestionType();
                break;

            case GameStage.Question:
                OnQuestion();
                break;

            case GameStage.EndRound:
                OnEndRound();
                break;

            case GameStage.EndGame:
                OnEndGame();
                break;
        }
    }

    private void OnBegin()
    {
        _playHandler.OnPackage(_document.Package);
        UpdateCanMoveNextRound();

        if (_gameRules.ShowGameThemes)
        {
            Stage = GameStage.GameThemes;
        }
        else
        {
            MoveNextRoundInternal();
        }
    }

    private void OnGameThemes()
    {
        var themes = new SortedSet<string>();

        foreach (var round in _document.Package.Rounds)
        {
            foreach (var theme in round.Themes.Where(theme => theme.Questions.Any(q => q.Price != Question.InvalidPrice)))
            {
                themes.Add(theme.Name);
            }
        }

        _playHandler.OnGameThemes(themes);
        MoveNextRoundInternal();
    }

    private void OnRound()
    {
        CanMoveBack = false;

        var strategyType = _gameRules.GetRulesForRoundType(ActiveRound.Type).QuestionSelectionStrategyType;

        _selectionStrategy = QuestionSelectionStrategyFactory.GetStrategy(
            ActiveRound,
            _optionsProvider(),
            strategyType,
            _playHandler,
            SelectQuestion,
            () => EndRoundAndMoveNext(RoundEndReason.Manual));

        if (_selectionStrategy.ShouldPlayRound())
        {
            _playHandler.OnRound(ActiveRound, strategyType);
            Stage = GameStage.SelectingQuestion;
        }
        else
        {
            _playHandler.OnRoundSkip(strategyType); // TODO: think about providing skip reason instead of strategy here
            MoveNextRoundInternal();
        }
    }

    private void OnSelectingQuestion()
    {
        SelectionStrategy.MoveNext();
        UpdateCanNext();
    }

    private void OnQuestionType()
    {
        var questionTypeName = QuestionEngine.QuestionTypeName;
        var isDefault = questionTypeName == _gameRules.GetRulesForRoundType(ActiveRound.Type).DefaultQuestionType;
        _playHandler.OnQuestionType(questionTypeName, isDefault);
        Stage = GameStage.Question;
    }

    private void OnQuestion()
    {
        if (QuestionEngine.PlayNext())
        {
            return;
        }

        var roundTimeout = _playHandler.OnQuestionEnd();

        if (roundTimeout)
        {
            _roundEndReason = RoundEndReason.Timeout;
            Stage = GameStage.EndRound;
        }
        else if (SelectionStrategy.CanMoveNext())
        {
            Stage = GameStage.SelectingQuestion;
            UpdateCanNext();
        }
        else
        {
            _roundEndReason = RoundEndReason.Completed;
            Stage = GameStage.EndRound;
        }
    }

    private void OnEndRound() => EndRoundAndMoveNext(_roundEndReason);

    private void OnEndGame()
    {
        _playHandler.OnPackageEnd();
        Stage = GameStage.None;
    }

    private void UpdateCanMoveNextRound() => CanMoveNextRound = _roundIndex < _document.Package.Rounds.Count;

    private void UpdateCanMoveBackRound() => CanMoveBackRound = _roundIndex > 0 || CanMoveBack;

    /// <summary>
    /// Moves engine to the previous game state.
    /// </summary>
    public void MoveBack()
    {
        SelectionStrategy.MoveBack();
        Stage = GameStage.SelectingQuestion;

        UpdateCanNext();
        CanMoveBack = SelectionStrategy.CanMoveBack();
    }

    private void SetActiveThemeQuestion()
    {
        _activeTheme = ActiveRound.Themes[_themeIndex];
        _activeQuestion = _activeTheme.Questions[_questionIndex];
    }

    private void SelectQuestion(int themeIndex, int questionIndex)
    {
        _themeIndex = themeIndex;
        _questionIndex = questionIndex;

        SetActiveThemeQuestion();
        OnMoveToQuestion();
        UpdateCanNext();
        CanMoveBack = SelectionStrategy.CanMoveBack();
    }

    public bool CanNext() => _stage != GameStage.None && (_stage != GameStage.SelectingQuestion || SelectionStrategy.CanMoveNext());

    private void UpdateCanNext() => CanMoveNext = CanNext();

    /// <summary>
    /// Ends round and moves to the next one.
    /// </summary>
    private void EndRoundAndMoveNext(RoundEndReason reason)
    {
        _playHandler.OnRoundEnd(reason);
        MoveNextRoundInternal();
    }

    /// <summary>
    /// Moves to the next round.
    /// </summary>
    /// <param name="showSign">Should the logo be shown.</param>
    public bool MoveNextRound()
    {
        if (!CanMoveNextRound)
        {
            return false;
        }

        _playHandler.OnRoundEnd(RoundEndReason.Manual);
        MoveNextRoundInternal();
        return true;
    }

    private void MoveNextRoundInternal()
    {
        _roundIndex++;
        SetActiveRound();

        CanMoveBack = false;
        UpdateCanMoveNextRound();
        UpdateCanMoveBackRound();

        if (_roundIndex < _document.Package.Rounds.Count)
        {
            Stage = GameStage.Round;
        }
        else
        {
            Stage = GameStage.EndGame;
        }

        UpdateCanNext();
    }

    public bool MoveToRound(int roundIndex)
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

        _playHandler.OnRoundEnd(RoundEndReason.Manual);

        _roundIndex = roundIndex;
        SetActiveRound();
        Stage = GameStage.Round;

        CanMoveBack = false;
        UpdateCanMoveNextRound();
        UpdateCanMoveBackRound();
        UpdateCanNext();
        return true;
    }

    public bool MoveBackRound()
    {
        if (!CanMoveBackRound)
        {
            return false;
        }

        if (CanMoveBack)
        {
            Stage = GameStage.Round;
        }
        else if (_roundIndex == 0)
        {
            return false;
        }
        else
        {
            _roundIndex--;
            SetActiveRound();
            Stage = GameStage.Round;
        }

        _playHandler.OnRoundEnd(RoundEndReason.Manual);

        CanMoveBack = false;
        UpdateCanNext();
        UpdateCanMoveNextRound();
        UpdateCanMoveBackRound();
        return true;
    }

    private void SetActiveRound() => _activeRound = _roundIndex < _document.Package.Rounds.Count ? _document.Package.Rounds[_roundIndex] : null;

    /// <summary>
    /// Skips rest of the question and goes directly to answer.
    /// </summary>
    public void MoveToAnswer() => QuestionEngine.MoveToAnswer();

    private void OnMoveToQuestion()
    {
        var options = _optionsProvider();

        _questionEngine = _questionEngineFactory.CreateEngine(
            ActiveQuestion,
            new QuestionEngineOptions
            {
                FalseStarts = options.IsPressMode
                    ? (options.IsMultimediaPressMode ? FalseStartMode.Enabled : FalseStartMode.TextContentOnly)
                    : FalseStartMode.Disabled,

                ShowSimpleRightAnswers = options.ShowRight,
                PlaySpecials = options.PlaySpecials,

                DefaultTypeName = _gameRules.GetRulesForRoundType(ActiveRound.Type).DefaultQuestionType
            });

        Stage = GameStage.QuestionType;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

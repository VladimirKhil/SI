using SIEngine.QuestionSelectionStrategies;
using SIEngine.Rules;
using SIPackages;
using SIPackages.Core;

namespace SIEngine;

// TODO: support simple SIGame mode here too
// (allow to provide different GameRules instances here)
// After that, remove SportEngine class

/// <summary>
/// Plays SIGame package and calls play handler callbacks.
/// </summary>
/// <remarks>
/// Uses selection strategy to select questions in each game round. Then tranfers question play to question engine.
/// The strategy is selected by game rules and round type.
/// </remarks>
public sealed class TvEngine : EngineBase
{
    private readonly HashSet<int> _leftFinalThemesIndicies = new();
    private readonly List<Theme> _finalThemes = new();

    protected override GameRules GameRules => WellKnownGameRules.Classic;

    private ISelectionStrategy? _selectionStrategy;

    private ISelectionStrategy SelectionStrategy
    {
        get
        {
            if (_selectionStrategy == null)
            {
                throw new InvalidOperationException("_selectionStrategy == null");
            }

            return _selectionStrategy;
        }
    }

    private void SetActiveThemeQuestion()
    {
        _activeTheme = ActiveRound.Themes[_themeIndex];
        _activeQuestion = _activeTheme.Questions[_questionIndex];
    }

    public bool CanSelectTheme => _stage == GameStage.WaitDelete;

    public TvEngine(
        SIDocument document,
        Func<EngineOptions> optionsProvider,
        ISIEnginePlayHandler playHandler,
        QuestionEngineFactory questionEngineFactory)
        : base(document, optionsProvider, playHandler, questionEngineFactory) { }

    /// <summary>
    /// Moves to the next game stage.
    /// </summary>
    public override void MoveNext()
    {
        switch (_stage)
        {
            case GameStage.Begin:
                #region Begin
                OnPackage(_document.Package);

                if (GameRules.ShowGameThemes)
                {
                    Stage = GameStage.GameThemes;
                }
                else
                {
                    MoveNextRound(false);
                }
                break;
                #endregion

            case GameStage.GameThemes:
                #region GameThemes
                var themes = new SortedSet<string>();

                foreach (var round in _document.Package.Rounds)
                {
                    foreach (var theme in round.Themes.Where(theme => theme.Questions.Any(q => q.Price != SIPackages.Question.InvalidPrice)))
                    {
                        themes.Add(theme.Name);
                    }
                }

                OnGameThemes(themes);
                MoveNextRound(false);
                break;
                #endregion

            case GameStage.Round:
                #region Round
                CanMoveBack = false;
                _timeout = false;

                _selectionStrategy = QuestionSelectionStrategyFactory.GetStrategy(
                    ActiveRound,
                    GameRules.GetRulesForRoundType(ActiveRound.Type).QuestionSelectionStrategyType,
                    PlayHandler,
                    SelectQuestion,
                    EndRound);

                if (_selectionStrategy.ShouldPlayRound())
                {
                    OnRound(ActiveRound);
                    Stage = ActiveRound.Type != RoundTypes.Final ? GameStage.SelectingQuestion : GameStage.FinalThemes;
                }
                else
                {
                    OnRoundSkip();
                    MoveNextRound();
                }

                break;
            #endregion

            case GameStage.SelectingQuestion:
                OnSelectingQuestion();
                break;

            case GameStage.Score:
                MoveNextRound();
                break;

            case GameStage.Question:
                OnQuestion();
                break;

            case GameStage.EndQuestion:
                #region EndQuestion
                OnQuestionFinish();
                OnEndQuestion(_themeIndex, _questionIndex);

                if (_timeout) // Round timeout
                {
                    OnRoundTimeout();
                    DoFinishRound();
                }
                else if (SelectionStrategy.CanMoveNext())
                {
                    Stage = GameStage.SelectingQuestion;
                    OnNextQuestion();
                    UpdateCanNext();
                }
                else
                {
                    EndRound();
                }

                break;
            #endregion

            case GameStage.FinalThemes:
                #region FinalThemes
                var finalThemes = ActiveRound.Themes;
                _finalThemes.Clear();

                for (var i = 0; i < finalThemes.Count; i++)
                {
                    if (!string.IsNullOrEmpty(finalThemes[i].Name) && finalThemes[i].Questions.Any())
                    {
                        _finalThemes.Add(finalThemes[i]);
                    }
                }

                if (_finalThemes.Count > 0)
                {
                    PlayNextFinalTheme(true);
                }
                else
                {
                    DoFinishRound();
                }

                break;
                #endregion

            case GameStage.WaitDelete:
                OnWaitDelete();
                break;

            case GameStage.FinalQuestion:
                OnFinalQuestion();
                break;

            case GameStage.AfterFinalThink:
                if (OptionsProvider().PlayAllQuestionsInFinalRound)
                {
                    _finalThemes.RemoveAt(_themeIndex);

                    if (_finalThemes.Count > 0)
                    {
                        if (SelectionStrategy.ShouldPlayRound())
                        {
                            PlayNextFinalTheme(false);
                            break;
                        }
                        else
                        {
                            OnRoundSkip();
                        }
                    }
                }
                
                DoFinishRound();
                break;

            case GameStage.End:
                break;
        }
    }

    private void OnSelectingQuestion()
    {
        SelectionStrategy.MoveNext();
        UpdateCanNext();
    }

    private void PlayNextFinalTheme(bool isFirstPlay)
    {
        _leftFinalThemesIndicies.Clear();

        for (var i = 0; i < _finalThemes.Count; i++)
        {
            _leftFinalThemesIndicies.Add(i);
        }

        OnFinalThemes(_finalThemes.ToArray(), OptionsProvider().PlayAllQuestionsInFinalRound, isFirstPlay);

        var count = _finalThemes.Count;

        if (count > 1)
        {
            Stage = GameStage.WaitDelete;
            UpdateCanNext();
        }
        else if (count == 1)
        {
            DoPrepareFinalQuestion();
        }
        else
        {
            Stage = GameStage.AfterFinalThink;
            MoveNext();
        }
    }

    public override Tuple<int, int, int> MoveBack()
    {
        var (themeIndex, questionIndex) = SelectionStrategy.MoveBack();
        Stage = GameStage.SelectingQuestion;

        UpdateCanNext();
        CanMoveBack = SelectionStrategy.CanMoveBack();

        return Tuple.Create(themeIndex, questionIndex, ActiveRound.Themes[themeIndex].Questions[questionIndex].Price);
    }

    private void SelectQuestion(int themeIndex, int questionIndex)
    {
        _themeIndex = themeIndex;
        _questionIndex = questionIndex;

        SetActiveThemeQuestion();

        if (!OptionsProvider().PlaySpecials)
        {
            ActiveQuestion.TypeName = QuestionTypes.Default;
        }

        OnMoveToQuestion();
        UpdateCanNext();
        CanMoveBack = true;
    }

    public override void SelectTheme(int themeIndex)
    {
        if (_stage == GameStage.FinalQuestion)
        {
            MoveNext();
            return;
        }

        if (_stage != GameStage.WaitDelete)
        {
            return;
        }

        Stage = GameStage.AfterDelete;
        _themeIndex = themeIndex;
        _questionIndex = 0;

        OnThemeSelected(themeIndex);
        UpdateCanNext();
    }

    private void DoPrepareFinalQuestion()
    {
        _themeIndex = _leftFinalThemesIndicies.First();
        _questionIndex = 0;

        _activeTheme = _finalThemes[_themeIndex];
        _activeQuestion = _activeTheme.Questions[_questionIndex];

        OnPrepareFinalQuestion(_activeTheme, _activeQuestion);
        UpdateCanNext();

        OnMoveToQuestion();
    }

    public override int OnReady(out bool more)
    {
        var result = -1;
        more = false;

        if (_stage == GameStage.AfterDelete)
        {
            result = _themeIndex;
            _leftFinalThemesIndicies.Remove(_themeIndex);

            if (_leftFinalThemesIndicies.Count == 1)
            {
                DoPrepareFinalQuestion();
            }
            else
            {
                Stage = GameStage.WaitDelete;
                more = true;
            }
        }

        UpdateCanNext();
        return result;
    }

    public override bool CanNext() => _stage != GameStage.End
        && _stage != GameStage.WaitDelete
        && (_stage != GameStage.SelectingQuestion || SelectionStrategy.CanMoveNext());
}

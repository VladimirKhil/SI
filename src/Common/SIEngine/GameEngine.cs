using SIEngine.Models;
using SIEngine.QuestionSelectionStrategies;
using SIEngine.Rules;
using SIPackages;
using SIPackages.Core;

namespace SIEngine;

/// <summary>
/// Plays SIGame package and calls play handler callbacks.
/// </summary>
/// <remarks>
/// Uses selection strategy to select questions in each game round. Then tranfers question play to question engine.
/// The strategy is selected by game rules and round type.
/// </remarks>
public sealed class GameEngine : EngineBase
{
    private ISelectionStrategy? _selectionStrategy;

    private ISelectionStrategy SelectionStrategy => _selectionStrategy ?? throw new InvalidOperationException("_selectionStrategy == null");

    private void SetActiveThemeQuestion()
    {
        _activeTheme = ActiveRound.Themes[_themeIndex];
        _activeQuestion = _activeTheme.Questions[_questionIndex];
    }

    public GameEngine(
        SIDocument document,
        GameRules gameRules,
        Func<EngineOptions> optionsProvider,
        ISIEnginePlayHandler playHandler,
        QuestionEngineFactory questionEngineFactory)
        : base(document, gameRules, optionsProvider, playHandler, questionEngineFactory) { }

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
                UpdateCanMoveNextRound();

                if (GameRules.ShowGameThemes)
                {
                    Stage = GameStage.GameThemes;
                }
                else
                {
                    MoveNextRoundInternal();
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
                MoveNextRoundInternal();
                break;
                #endregion

            case GameStage.Round:
                #region Round
                CanMoveBack = false;
                _timeout = false;

                var strategyType = GameRules.GetRulesForRoundType(ActiveRound.Type).QuestionSelectionStrategyType;

                _selectionStrategy = QuestionSelectionStrategyFactory.GetStrategy(
                    ActiveRound,
                    OptionsProvider(),
                    strategyType,
                    PlayHandler,
                    SelectQuestion,
                    () => EndRoundAndMoveNext(RoundEndReason.Manual));

                if (_selectionStrategy.ShouldPlayRound())
                {
                    PlayHandler.OnRound(ActiveRound, strategyType);
                    Stage = GameStage.SelectingQuestion;
                }
                else
                {
                    PlayHandler.OnRoundSkip(strategyType); // TODO: think about providing skip reason instead of strategy here
                    MoveNextRoundInternal();
                }

                break;
                #endregion

            case GameStage.SelectingQuestion:
                OnSelectingQuestion();
                break;

            case GameStage.QuestionType:
                OnQuestionType();
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
                    EndRoundAndMoveNext(RoundEndReason.Timeout);
                }
                else if (SelectionStrategy.CanMoveNext())
                {
                    Stage = GameStage.SelectingQuestion;
                    OnNextQuestion();
                    UpdateCanNext();
                }
                else
                {
                    EndRoundAndMoveNext(RoundEndReason.Completed);
                }

                break;
            #endregion

            case GameStage.EndGame:
                OnEndGame();
                Stage = GameStage.None;
                break;
        }
    }

    private void OnQuestionType()
    {
        var questionTypeName = QuestionEngine.QuestionTypeName;
        var isDefault = questionTypeName == GameRules.GetRulesForRoundType(ActiveRound.Type).DefaultQuestionType;
        PlayHandler.OnQuestionType(questionTypeName, isDefault);
        Stage = GameStage.Question;
    }

    private void OnSelectingQuestion()
    {
        SelectionStrategy.MoveNext();
        UpdateCanNext();
    }

    public override void MoveBack()
    {
        SelectionStrategy.MoveBack();
        Stage = GameStage.SelectingQuestion;

        UpdateCanNext();
        CanMoveBack = SelectionStrategy.CanMoveBack();
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

    public override bool CanNext() => _stage != GameStage.None && (_stage != GameStage.SelectingQuestion || SelectionStrategy.CanMoveNext());

    public bool SkipQuestion()
    {
        if (Stage != GameStage.Question)
        {
            return false;
        }

        Stage = GameStage.EndQuestion;
        return true;
    }

    /// <summary>
    /// Ends round and moves to the next one.
    /// </summary>
    private void EndRoundAndMoveNext(RoundEndReason reason)
    {
        PlayHandler.OnRoundEnd(reason);
        MoveNextRoundInternal();
    }
}

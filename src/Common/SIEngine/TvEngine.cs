using SIEngine.Rules;
using SIPackages;
using SIPackages.Core;

namespace SIEngine;

// TODO: support simple SIGame mode here too
// (allow to provide different GameRules instances here)
// After that, remove SportEngine class

/// <summary>
/// Handles classic SIGame rules.
/// </summary>
public sealed class TvEngine : EngineBase
{
    private readonly Stack<(int, int)> _history = new();
    private readonly Stack<(int, int)> _forward = new();

    private readonly HashSet<(int, int)> _questionsTable = new();

    private readonly HashSet<int> _leftFinalThemesIndicies = new();
    private readonly List<Theme> _finalThemes = new();

    protected override GameRules GameRules => WellKnownGameRules.Classic;

    private void SetActiveThemeQuestion()
    {
        _activeTheme = _activeRound.Themes[_themeIndex];
        _activeQuestion = _activeTheme.Questions[_questionIndex];
    }

    public bool CanSelectQuestion => _stage == GameStage.RoundTable;

    public bool CanSelectTheme => _stage == GameStage.WaitDelete;

    public override int LeftQuestionsCount => _questionsTable.Count; // for debugging only

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
                Stage = GameStage.GameThemes;
                OnPackage(_document.Package);
                break;
                #endregion

            case GameStage.GameThemes:
                #region GameThemes
                OnSound();

                var themes = new List<string>();

                foreach (var round in _document.Package.Rounds.Where(round => round.Type != RoundTypes.Final))
                {
                    foreach (var theme in round.Themes.Where(theme => theme.Questions.Any()))
                    {
                        themes.Add(theme.Name);
                    }
                }

                themes.Sort();
                OnGameThemes(themes.ToArray());

                MoveNextRound(false);
                AutoNext(1000 + Math.Max(3, themes.Count) * 15000 / 18);
                break;
                #endregion

            case GameStage.Round:
                #region Round
                OnSound("beginround.mp3");
                _history.Clear();
                CanMoveBack = false;

                if (PlayHandler.ShouldPlayRound(GameRules.GetRulesForRoundType(_activeRound!.Type).QuestionSelectionStrategyType))
                {
                    OnRound(_activeRound);
                    Stage = _activeRound.Type != RoundTypes.Final ? GameStage.RoundThemes : GameStage.FinalThemes;
                }
                else
                {
                    OnRoundSkip();
                    MoveNextRound();
                }

                _timeout = false;
                AutoNext(7000);
                break;
                #endregion

            case GameStage.RoundThemes:
                #region RoundThemes
                OnSound("cathegories.mp3");

                _questionsTable.Clear();

                for (int i = 0; i < _activeRound.Themes.Count; i++)
                {
                    for (int j = 0; j < _activeRound.Themes[i].Questions.Count; j++)
                    {
                        if (_activeRound.Themes[i].Questions[j].Price != SIPackages.Question.InvalidPrice)
                        {
                            _questionsTable.Add((i, j));
                        }
                    }
                }

                OnRoundThemes(_activeRound.Themes.ToArray());

                Stage = GameStage.RoundTable;
                UpdateCanNext();

                AutoNext(4000 + 1700 * _activeRound.Themes.Count);
                break;
                #endregion

            case GameStage.RoundTable:
                #region RoundTable
                if (_forward.Count > 0)
                {
                    var point = _forward.Pop();
                    UpdateCanNext();

                    _themeIndex = point.Item1;
                    _questionIndex = point.Item2;

                    OnQuestionSelected();
                }

                // Do nothing
                break;
                #endregion

            case GameStage.Score:
                MoveNextRound();
                AutoNext(5000);
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
                    OnSound("timeout.wav");
                    OnRoundTimeout();
                    DoFinishRound();
                }
                else if (_questionsTable.Any()) // There are still questions in round
                {
                    Stage = GameStage.RoundTable;
                    OnNextQuestion();
                    UpdateCanNext();

                    AutoNext(3000);
                }
                else // No questions left
                {
                    EndRound();
                }

                break;
            #endregion

            case GameStage.FinalThemes:
                #region FinalThemes
                OnSound();

                var finalThemes = _activeRound.Themes;
                _finalThemes.Clear();

                for (int i = 0; i < finalThemes.Count; i++)
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
                OnSound();

                if (OptionsProvider().PlayAllQuestionsInFinalRound)
                {
                    _finalThemes.RemoveAt(_themeIndex);

                    if (_finalThemes.Count > 0)
                    {
                        if (PlayHandler.ShouldPlayRound(GameRules.GetRulesForRoundType(_activeRound!.Type).QuestionSelectionStrategyType))
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
            AutoNext(2000);
        }
        else if (count == 1)
        {
            DoPrepareFinalQuestion();
            AutoNext(4000);
        }
        else
        {
            Stage = GameStage.AfterFinalThink;
            MoveNext();
        }
    }

    public override Tuple<int, int, int> MoveBack()
    {
        var data = _history.Pop();
        CanMoveBack = _history.Any();

        _forward.Push(data);

        var theme = data.Item1;
        var question = data.Item2;

        _questionsTable.Add(data);
        Stage = GameStage.RoundTable;

        UpdateCanNext();

        return Tuple.Create(theme, question, _activeRound.Themes[theme].Questions[question].Price);
    }

    public override void SelectQuestion(int theme, int question)
    {
        if (!CanSelectQuestion)
        {
            return;
        }

        _themeIndex = theme;
        _questionIndex = question;

        _forward.Clear();
        UpdateCanNext();

        OnQuestionSelected();
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
        OnSound("shrink.mp3");
        UpdateCanNext();
    }

    private void OnQuestionSelected()
    {
        SetActiveThemeQuestion();

        _history.Push((_themeIndex, _questionIndex));
        CanMoveBack = true;

        if (!OptionsProvider().PlaySpecials)
        {
            _activeQuestion.TypeName = QuestionTypes.Default;
        }

        OnMoveToQuestion();

        _questionsTable.Remove((_themeIndex, _questionIndex));
        OnQuestionSelected(_themeIndex, _questionIndex, _activeTheme, _activeQuestion);
        UpdateCanNext();

        if (_activeQuestion != null && _activeQuestion.TypeName != QuestionTypes.Simple)
        {
            AutoNext(6000);
        }
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

        if (_stage == GameStage.Question)
        {
            if (_activeQuestion.TypeName == QuestionTypes.Simple)
            {
                MoveNext();
            }
        }
        else if (_stage == GameStage.AfterDelete)
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

            OnSound();
            AutoNext(4000);
        }

        UpdateCanNext();
        return result;
    }

    public override bool MoveNextRound(bool showSign = true)
    {
        var result = base.MoveNextRound(showSign);

        if (result)
        {
            _history.Clear();
        }

        return result;
    }

    public override bool MoveToRound(int roundIndex, bool showSign = true)
    {
        var result = base.MoveToRound(roundIndex, showSign);

        if (result)
        {
            _history.Clear();
        }

        return result;
    }

    public override bool MoveBackRound()
    {
        var result = base.MoveBackRound();

        if (result)
        {
            _history.Clear();
        }

        return result;
    }

    public override bool CanNext() => _stage != GameStage.End && (_stage != GameStage.RoundTable || _forward.Count > 0)
        && _stage != GameStage.WaitDelete;

    /// <summary>
    /// Автоматический шаг дальше
    /// </summary>
    /// <param name="milliseconds"></param>
    protected override void AutoNextCore()
    {
        if (CanSelectQuestion)
        {
            var index = Random.Shared.Next(_questionsTable.Count);
            var pair = _questionsTable.Skip(index).First();

            SelectQuestion(pair.Item1, pair.Item2);
            return;
        }

        if (CanSelectTheme)
        {
            var themeIndex = Random.Shared.Next(_leftFinalThemesIndicies.Count);
            themeIndex = _leftFinalThemesIndicies.Skip(themeIndex).First();

            SelectTheme(themeIndex);
        }
    }

    public override bool RemoveQuestion(int themeIndex, int questionIndex) =>
        _questionsTable.Remove((themeIndex, questionIndex));

    public override int? RestoreQuestion(int themeIndex, int questionIndex)
    {
        if (_activeRound == null || themeIndex < 0 || themeIndex >= _activeRound.Themes.Count)
        {
            return null;
        }

        if (questionIndex < 0 || questionIndex >= _activeRound.Themes[themeIndex].Questions.Count)
        {
            return null;
        }

        if (_activeRound.Themes[themeIndex].Questions[questionIndex].Price == SIPackages.Question.InvalidPrice)
        {
            return null;
        }

        _questionsTable.Add((themeIndex, questionIndex));
        return _activeRound.Themes[themeIndex].Questions[questionIndex].Price;
    }
}

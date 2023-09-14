using SIEngine.Rules;
using SIPackages;

namespace SIEngine;

/// <summary>
/// Defines a simplified SIGame engine. Simplified game engine plays questions sequentially.
/// </summary>
public sealed class SportEngine : EngineBase
{
    public override int LeftQuestionsCount => throw new NotImplementedException();

    protected override GameRules GameRules => WellKnownGameRules.Simple;

    public SportEngine(
        SIDocument document,
        Func<EngineOptions> optionsProvider,
        ISIEnginePlayHandler playHandler,
        QuestionEngineFactory questionEngineFactory)
        : base(document, optionsProvider, playHandler, questionEngineFactory) { }

    private void SetActiveTheme() => _activeTheme = _activeRound.Themes[_themeIndex];

    private void SetActiveQuestion() => _activeQuestion = _activeTheme.Questions[_questionIndex];

    public override void MoveNext()
    {
        switch (_stage)
        {
            case GameStage.Begin:
                #region Begin
                //this.Stage = GameStage.GameThemes;
                OnPackage(_document.Package);
                MoveNextRound(false);
                AutoNext(1000);
                break;
                #endregion

            case GameStage.Round:
                #region Round
                OnSound("beginround.mp3");
                CanMoveBack = false;

                OnRound(_activeRound);

                _timeout = false;

                _themeIndex = -1;
                MoveNextTheme();
                Stage = GameStage.Theme;
                UpdateCanNext();

                AutoNext(4000 + 1700 * _activeRound.Themes.Count);
                break;
                #endregion

            case GameStage.Theme:
                #region Theme
                OnTheme(_activeTheme);
                _questionIndex = -1;
                Stage = GameStage.NextQuestion;
                break;
                #endregion

            case GameStage.NextQuestion:
                if (!MoveNextQuestion())
                {
                    if (MoveNextTheme())
                    {
                        Stage = GameStage.Theme;
                        UpdateCanNext();
                        OnNextQuestion();
                        AutoNext(500);
                    }
                    else
                    {
                        EndRound();
                    }

                    break;
                }

                CanMoveBack = _questionIndex > 0 || _themeIndex > 0;
                SetActiveQuestion();
                OnQuestion(_activeQuestion);
                OnMoveToQuestion();
                break;

            case GameStage.Score:
                MoveNextRound();
                AutoNext(5000);
                break;

            case GameStage.Question:
                OnQuestion();
                break;

            case GameStage.FinalQuestion:
                OnFinalQuestion();
                break;

            case GameStage.EndQuestion:
                #region EndQuestion
                OnQuestionFinish();

                if (_timeout) // Round timeout
                {
                    OnSound("timeout.wav");
                    OnRoundTimeout();
                    DoFinishRound();
                }
                else
                {
                    Stage = GameStage.NextQuestion;
                    UpdateCanNext();
                    OnNextQuestion();
                    AutoNext(3000);
                }

                break;
                #endregion

            case GameStage.End:
                break;
        }
    }

    private bool MoveNextQuestion()
    {
        while (_questionIndex + 1 < _activeTheme.Questions.Count)
        {
            _questionIndex++;
            SetActiveQuestion();

            if (_activeQuestion.Price != SIPackages.Question.InvalidPrice)
            {
                return true;
            }
        }

        return false;
    }

    public override Tuple<int, int, int> MoveBack()
    {
        _questionIndex--;

        if (_questionIndex < 0)
        {
            do
            {
                _themeIndex--;

                if (_themeIndex < 0)
                {
                    throw new InvalidOperationException("_themeIndex < 0");
                }

                SetActiveTheme();

                if (_activeTheme.Questions.Any())
                {
                    _questionIndex = _activeTheme.Questions.Count - 1;
                    break;
                }
            } while (_themeIndex >= 0);
        }

        CanMoveBack = _questionIndex > 0 || _themeIndex > 0;

        SetActiveQuestion();
        OnMoveToQuestion();

        return Tuple.Create(_themeIndex, _questionIndex, _activeQuestion.Price);
    }

    private bool MoveNextTheme()
    {
        while (_themeIndex + 1 < _activeRound.Themes.Count)
        {
            _themeIndex++;
            SetActiveTheme();

            if (_activeTheme.Questions.Any(q => q.Price != SIPackages.Question.InvalidPrice))
            {
                return true;
            }
        }

        return false;
    }

    public override bool CanNext() => _stage != GameStage.End;

    public override void SelectQuestion(int theme, int question) => throw new NotSupportedException();

    public override int OnReady(out bool more) => throw new NotImplementedException();

    public override void SelectTheme(int publicThemeIndex) => throw new NotImplementedException();

    public override bool RemoveQuestion(int themeIndex, int questionIndex) => throw new NotImplementedException();

    public override int? RestoreQuestion(int themeIndex, int questionIndex) => throw new NotImplementedException();
}

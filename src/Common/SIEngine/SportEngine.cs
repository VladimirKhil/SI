using SIPackages;
using SIPackages.Core;
using System;
using System.Linq;

namespace SIEngine
{
    /// <summary>
    /// Упрощённая (спортивная) SIGame
    /// </summary>
    public sealed class SportEngine: EngineBase
    {
        public SportEngine(SIDocument document, IEngineSettingsProvider settingsProvider)
            : base(document, settingsProvider)
        {

        }

        private void SetActiveTheme()
        {
            _activeTheme = _activeRound.Themes[_themeIndex];
        }

        private void SetActiveQuestion()
        {
            _activeQuestion = _activeTheme.Questions[_questionIndex];
        }

        /// <summary>
        /// Перейти к следующему шагу игры
        /// </summary>
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
                    _questionIndex++;
                    CanMoveBack = _questionIndex > 0 || _themeIndex > 0;
                    _atomIndex = 0;
                    _isMedia = false;
                    _useAnswerMarker = false;
                    SetActiveQuestion();
                    OnQuestion(_activeQuestion);
                    Stage = GameStage.Question;
                    break;

                case GameStage.Score:
                    MoveNextRound();
                    AutoNext(5000);
                    break;

                case GameStage.Question:
                    OnQuestion();
                    break;

                case GameStage.RightAnswer:
                    #region RightAnswer
                    OnRightAnswer();

                    if (!_useAnswerMarker)
                    {
                        OnSimpleAnswer(_activeQuestion.Right.Count > 0 ? _activeQuestion.Right[0] : " ");
                    }
                    else // Ответ находится в тексте вопроса
                    {
                        var mode = PlayQuestionAtom();
                        if (mode == QuestionPlayMode.InProcess)
                        {
                            Stage = GameStage.RightAnswerProceed;
                            break;
                        }
                    }

                    Stage = GameStage.EndQuestion;
                    AutoNext(3000);
                    break;
                    #endregion

                case GameStage.RightAnswerProceed:
                    #region RightAnswerProceed
                    {
                        var mode = PlayQuestionAtom();
                        if (mode != QuestionPlayMode.InProcess)
                            Stage = _activeRound.Type != RoundTypes.Final ? GameStage.EndQuestion : GameStage.AfterFinalThink;

                        AutoNext(4000);
                        break;
                    }
                    #endregion

                case GameStage.EndQuestion:
                    #region EndQuestion
                    if (_timeout) // Закончилось время раунда
                    {
                        OnSound("timeout.wav");
                        OnRoundTimeout();
                        DoFinishRound();
                    }
                    else if (_questionIndex + 1 < _activeTheme.Questions.Count)
                    {
                        Stage = GameStage.NextQuestion;
                        UpdateCanNext();
                        OnNextQuestion();
                        AutoNext(3000);
                    }
                    else if (MoveNextTheme())
                    {
                        Stage = GameStage.Theme;
                        UpdateCanNext();
                        OnNextQuestion();
                        AutoNext(3000);
                    }
                    else // Закончились вопросы
                    {
                        OnRoundEmpty();
                        DoFinishRound();
                    }

                    break;
                    #endregion

                case GameStage.End:
                    break;
            }
        }

        public override Tuple<int, int, int> MoveBack()
        {
            _questionIndex--;
            if (_questionIndex < 0)
            {
                _themeIndex--;
                SetActiveTheme();
                _questionIndex = _activeTheme.Questions.Count - 1;
            }

            CanMoveBack = _questionIndex > 0 || _themeIndex > 0;

            _atomIndex = 0;
            _isMedia = false;
            _useAnswerMarker = false;
            SetActiveQuestion();

            _stage = GameStage.Question;

            return Tuple.Create(_themeIndex, _questionIndex, _activeQuestion.Price);
        }

        private bool MoveNextTheme()
        {
            while (_themeIndex + 1 < _activeRound.Themes.Count)
            {
                _themeIndex++;
                SetActiveTheme();

                if (_activeTheme.Questions.Any())
                    return true;
            }

            return false;
        }

        protected override bool AcceptRound(Round round) => base.AcceptRound(round) && round.Type != RoundTypes.Final;

        public override bool CanNext() => _stage != GameStage.End;
    }
}

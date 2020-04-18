using SIPackages;
using SIPackages.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SIEngine
{
    /// <summary>
    /// Классическая (телевизионная) SIGame
    /// </summary>
    public sealed class TvEngine : EngineBase
    {
        private readonly Stack<Tuple<int, int>> _history = new Stack<Tuple<int, int>>();
        private readonly Stack<Tuple<int, int>> _forward = new Stack<Tuple<int, int>>();

        private readonly HashSet<Tuple<int, int>> _questionsTable = new HashSet<Tuple<int, int>>();
        private readonly HashSet<int> _themesTable = new HashSet<int>();
        private readonly List<int> _finalMap = new List<int>();

        private void SetActiveThemeQuestion()
        {
            _activeTheme = _activeRound.Themes[_themeIndex];
            _activeQuestion = _activeTheme.Questions[_questionIndex];
        }

        public bool CanSelectQuestion => _stage == GameStage.RoundTable;

        public bool CanSelectTheme => _stage == GameStage.WaitDelete;

        public TvEngine(SIDocument document, IEngineSettingsProvider settingsProvider)
            : base(document, settingsProvider)
        {
            
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
                    Stage = GameStage.GameThemes;
                    OnPackage(_document.Package);
                    break;
                    #endregion

                case GameStage.GameThemes:
                    #region GameThemes
                    OnSound();
                    var themes = new List<string>();
                    foreach (var round in _document.Package.Rounds.Where(round => round.Type != RoundTypes.Final))
                        foreach (var theme in round.Themes)
                            themes.Add(theme.Name);

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

                    OnRound(_activeRound);

                    Stage = _activeRound.Type != RoundTypes.Final ? GameStage.RoundThemes : GameStage.FinalThemes;

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
                            _questionsTable.Add(Tuple.Create(i, j));
                        }
                    }

                    OnRoundThemes(_activeRound.Themes.ToArray());

                    Stage = GameStage.RoundTable;
                    UpdateCanNext();

                    AutoNext(4000 + 1700 * _activeRound.Themes.Count);
                    break;
                    #endregion

                case GameStage.RoundTable:
                    #region RoundTablo
                    if (_forward.Count > 0)
                    {
                        var point = _forward.Pop();
                        UpdateCanNext();
                        _themeIndex = point.Item1;
                        _questionIndex = point.Item2;

                        SetActiveThemeQuestion();

                        OnQuestionSelected(false);
                    }

                    // Do nothing
                    break;
                    #endregion

                case GameStage.Score:
                    MoveNextRound();
                    AutoNext(5000);
                    break;

                case GameStage.Question:
                    #region Question
                    {
                        var playMode = PlayQuestionAtom();
                        var pressMode = _settingsProvider.IsPressMode(_isMedia);
                        if (playMode == QuestionPlayMode.AlreadyFinished)
                        {
                            Stage = (_settingsProvider.ShowRight || _useAnswerMarker ? GameStage.RightAnswer : GameStage.EndQuestion);

                            if (pressMode)
                            {
                                OnWaitTry(_activeQuestion);
                                AutoNext(1000 * (Math.Min(5, _settingsProvider.ThinkingTime)));
                            }
                            else
                                MoveNext();
                        }
                        else
                        {
                            OnQuestionProcessed(_activeQuestion, playMode == QuestionPlayMode.JustFinished, pressMode);
                            AutoNext(1000 * (_settingsProvider.ThinkingTime + _activeQuestion.Scenario.ToString().Length / 20));
                        }

                        break;
                    }
                    #endregion

                case GameStage.RightAnswer:
                    #region RightAnswer
                    OnRightAnswer();

                    if (!_useAnswerMarker)
                    {
                        OnSimpleAnswer(_activeQuestion.Right.Count > 0 ? _activeQuestion.Right[0] : null);
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
                    _questionsTable.Remove(Tuple.Create(_themeIndex, _questionIndex));

                    OnEndQuestion(_themeIndex, _questionIndex);

                    if (_timeout) // Закончилось время раунда
                    {
                        OnSound("timeout.wav");
                        OnRoundTimeout();
                        DoFinishRound();
                    }
                    else if (_questionsTable.Any()) // Не закончились вопросы
                    {
                        Stage = GameStage.RoundTable;
                        OnNextQuestion();
                        UpdateCanNext();

                        AutoNext(3000);
                    }
                    else // Закончились вопросы
                    {
                        OnRoundEmpty();
                        DoFinishRound();
                    }

                    break;
                    #endregion

                case GameStage.FinalThemes:
                    #region FinalThemes
                    OnSound();
                    var finalThemes = _activeRound.Themes;
                    var selectedThemes = new List<Theme>();

                    _themesTable.Clear();
                    _finalMap.Clear();
                    for (int i = 0; i < finalThemes.Count; i++)
                    {
                        if (finalThemes[i].Name != null && finalThemes[i].Questions.Any())
                        {
                            _themesTable.Add(i);
                            _finalMap.Add(i);
                            selectedThemes.Add(finalThemes[i]);
                        }
                    }

                    OnFinalThemes(selectedThemes.ToArray());

                    var count = selectedThemes.Count;
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
                    break;
                    #endregion

                case GameStage.WaitDelete:
                    OnWaitDelete();
                    break;

                case GameStage.FinalQuestion:
                    #region FinalQuestion
                    {
                        var playMode = PlayQuestionAtom();
                        if (playMode != QuestionPlayMode.InProcess)
                        {
                            Stage = GameStage.FinalThink;
                            AutoNext(1000 * (_activeQuestion.Scenario.ToString().Length / 20));
                        }

                        break;
                    }
                    #endregion

                case GameStage.FinalThink:
                    OnSound("finalthink.wav");
                    Stage = _settingsProvider.ShowRight || _useAnswerMarker ? GameStage.RightFinalAnswer : GameStage.AfterFinalThink;
                    OnWaitTry(_activeQuestion, true);
                    AutoNext(38000);
                    break;

                case GameStage.RightFinalAnswer:
                    #region RightFinalAnswer
                    OnSound();
                    if (!_useAnswerMarker)
                    {
                        OnSimpleAnswer(_activeQuestion.Right.Count > 0 ? _activeQuestion.Right[0] : "Ответ не задан!");
                    }
                    else
                    {
                        _atomIndex++;
                        var mode = PlayQuestionAtom();
                        if (mode == QuestionPlayMode.InProcess)
                        {
                            Stage = GameStage.RightAnswerProceed;
                            AutoNext(3000);
                            break;
                        }
                    }

                    Stage = GameStage.AfterFinalThink;
                    AutoNext(4000);
                    break;
                    #endregion

                case GameStage.AfterFinalThink:
                    OnSound();
                    DoFinishRound();
                    break;

                case GameStage.End:
                    break;
            }
        }

        protected override bool AcceptRound(Round round) => round.Type != RoundTypes.Final || round.Themes.Any(theme => theme.Name != null);

        public override Tuple<int, int, int> MoveBack()
        {
            var data = _history.Pop();
            CanMoveBack = _history.Any();

            _forward.Push(data);

            var theme = data.Item1;
            var question = data.Item2;

            if (_stage == GameStage.Round)
            {
                _roundIndex--;
                SetActiveRound();

                CanMoveNextRound = _roundIndex + 1 < _document.Package.Rounds.Count;
                CanMoveBackRound = _roundIndex > 0;
            }

            _questionsTable.Add(data);
            Stage = GameStage.RoundTable;

            UpdateCanNext();

            return Tuple.Create(theme, question, _activeRound.Themes[theme].Questions[question].Price);
        }

        public void SelectQuestion(int theme, int question)
        {
            if (!CanSelectQuestion)
                return;

            _themeIndex = theme;
            _questionIndex = question;

            SetActiveThemeQuestion();

            OnQuestionSelected();
        }

        public void SelectTheme(int publicThemeIndex)
        {
            if (_stage == GameStage.FinalQuestion)
            {
                MoveNext();
                return;
            }

            if (_stage != GameStage.WaitDelete)
                return;

            Stage = GameStage.AfterDelete;
            _themeIndex = _finalMap[publicThemeIndex];
            _questionIndex = 0;

            SetActiveThemeQuestion();

            OnThemeSelected(_themeIndex);
            OnSound("shrink.mp3");
            UpdateCanNext();
        }

        private void OnQuestionSelected(bool clearForward = true)
        {
            _history.Push(Tuple.Create(_themeIndex, _questionIndex));
            CanMoveBack = true;

            if (clearForward)
            {
                _forward.Clear();
                UpdateCanNext();
            }

            if (_activeQuestion.Type.Name != QuestionTypes.Simple && !_settingsProvider.PlaySpecials)
                _activeQuestion.Type.Name = QuestionTypes.Simple;

            OnQuestionSelected(_themeIndex, _questionIndex, _activeTheme, _activeQuestion);

            _atomIndex = 0;
            _isMedia = false;
            _useAnswerMarker = false;
            Stage = GameStage.Question;

            UpdateCanNext();
            if (_activeQuestion != null && _activeQuestion.Type.Name != QuestionTypes.Simple)
                AutoNext(6000);
        }

        private void DoPrepareFinalQuestion()
        {
            _atomIndex = 0;
            _isMedia = false;
            _themeIndex = _themesTable.First();
            _questionIndex = 0;

            SetActiveThemeQuestion();

            OnPrepareFinalQuestion(_activeTheme, _activeQuestion);
            Stage = GameStage.FinalQuestion;
            _useAnswerMarker = false;
            UpdateCanNext();
        }

        public int OnReady(out bool more)
        {
            var result = -1;
            more = false;

            if (_stage == GameStage.Question)
            {
                if (_activeQuestion.Type.Name == QuestionTypes.Simple)
                {
                    MoveNext();
                }
            }
            else if (_stage == GameStage.AfterDelete)
            {
                result = _themeIndex;
                _themesTable.Remove(_themeIndex);

                if (_themesTable.Count == 1)
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
            _history.Clear();
            return base.MoveNextRound(showSign);
        }

        public override void MoveBackRound()
        {
            base.MoveBackRound();
            _history.Clear();
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
                var index = new Random().Next(_questionsTable.Count);
                var pair = _questionsTable.Skip(index).First();

                SelectQuestion(pair.Item1, pair.Item2);
                return;
            }

            if (CanSelectTheme)
            {
                var themeIndex = new Random().Next(_themesTable.Count);
                themeIndex = _themesTable.Skip(themeIndex).First();

                SelectTheme(themeIndex);
            }
        }
    }
}

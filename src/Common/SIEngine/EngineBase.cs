using SIPackages;
using SIPackages.Core;
using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SIEngine
{
    /// <summary>
    /// Класс, реализующий правила игры в СИ
    /// </summary>
    public abstract class EngineBase : IDisposable, INotifyPropertyChanged
    {
        protected IEngineSettingsProvider _settingsProvider;

        protected GameStage _stage = GameStage.Begin;

        /// <summary>
        /// Стадия игры
        /// </summary>
        protected GameStage Stage
        {
            set
            {
                if (_stage != value)
                {
                    _stage = value;
                    OnPropertyChanged();
                }
            }
        }

        protected SIDocument _document;

        protected int _roundIndex = -1, _themeIndex = 0, _questionIndex = 0, _atomIndex = 0;
        protected bool _isMedia;

        protected Round _activeRound;
        protected Theme _activeTheme;
        protected Question _activeQuestion;

        protected bool _inAutoMode;
        protected object _autoLock = new object();

        protected bool _timeout = false;

        protected bool _useAnswerMarker = false;

        private bool _canMoveNext = true;
        private bool _canMoveBack;

        private bool _canMoveNextRound;
        private bool _canMoveBackRound;

        public string PackageName => _document.Package.Name;

        public int RoundIndex => _roundIndex;
        public int ThemeIndex => _themeIndex;
        public int QuestionIndex => _questionIndex;

        private Atom ActiveAtom => _atomIndex < _activeQuestion.Scenario.Count ? _activeQuestion.Scenario[_atomIndex] : null;

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

        public bool CanChangeSum() => _stage == GameStage.Question || _stage == GameStage.RoundTable || _stage == GameStage.AfterFinalThink
            || _stage == GameStage.EndQuestion || _stage == GameStage.RightAnswer;

        public bool CanReturnToQuestion() => _activeRound != null
                && _activeRound.Type != RoundTypes.Final
                && _activeQuestion != null && _activeQuestion.Type.Name == QuestionTypes.Simple;

        public bool CanPress() => _activeQuestion.Type.Name == QuestionTypes.Simple
            && (_stage == GameStage.RightAnswer || _stage == GameStage.EndQuestion);

        public bool IsQuestion() => _stage == GameStage.Question;

        public bool IsWaitingForPress() => _stage == GameStage.Question || _stage == GameStage.RightAnswer || _stage == GameStage.EndQuestion;

        public bool IsQuestionFinished() => _activeQuestion != null && _atomIndex == _activeQuestion.Scenario.Count;

        public void OnIntroFinished()
        {
            if (_stage == GameStage.GameThemes)
                AutoNext(1000);
        }

        /// <summary>
        /// Игровая заставка
        /// </summary>
        /// <returns></returns>
        public bool IsIntro() => _roundIndex == -1;

        #region Events

        public event Action<Package> Package;
        public event Action<string[]> GameThemes;
        public event Action<bool> NextRound;
        public event Action<Round> Round;
        public event Action<Theme[]> RoundThemes;
        public event Action<Theme> Theme;
        public event Action<Question> Question;
        public event Action<int, int, Theme, Question> QuestionSelected;

        public event Action<Atom> QuestionAtom;
        public event Action<string, IMedia> QuestionText;
        public event Action<string> QuestionOral;
        public event Action<IMedia, IMedia> QuestionImage;
        public event Action<IMedia> QuestionSound;
        public event Action<IMedia> QuestionVideo;
        public event Action<Atom> QuestionOther;
        public event Action<Question, bool, bool> QuestionProcessed;
        public event Action<Question, bool> WaitTry;
        public event Action<string> SimpleAnswer;
        public event Action RightAnswer;

        public event Action ShowScore;
        public event Action LogScore;
        public event Action<int, int> EndQuestion;
        public event Action RoundEmpty;
        public event Action NextQuestion;
        public event Action RoundTimeout;

        public event Action<Theme[]> FinalThemes;
        public event Action WaitDelete;
        public event Action<int> ThemeSelected;
        public event Action<Theme, Question> PrepareFinalQuestion;

        public event Action<string> Sound;

        public event Action<string> Error;

        public event Action EndGame;

        #endregion

        public EngineBase(SIDocument document, IEngineSettingsProvider settingsProvider)
        {
            _document = document;
            _settingsProvider = settingsProvider;
        }

        public abstract void MoveNext();
        public abstract Tuple<int, int, int> MoveBack();

        #region Fire events

        protected void OnPackage(Package package)
        {
            Package?.Invoke(package);
        }

        protected void OnGameThemes(string[] gameThemes)
        {
            GameThemes?.Invoke(gameThemes);
        }

        protected void OnNextRound(bool showSign = true)
        {
            NextRound?.Invoke(showSign);
        }

        protected void OnRound(Round round)
        {
            Round?.Invoke(round);
        }

        protected void OnRoundThemes(Theme[] roundThemes)
        {
            RoundThemes?.Invoke(roundThemes);
        }

        protected void OnTheme(Theme theme)
        {
            Theme?.Invoke(theme);
        }

        protected void OnQuestion(Question question)
        {
            Question?.Invoke(question);
        }

        protected void OnQuestionSelected(int themeIndex, int questionIndex, Theme theme, Question question)
        {
            QuestionSelected?.Invoke(themeIndex, questionIndex, theme, question);
        }

        protected void OnQuestionAtom(Atom atom)
        {
            QuestionAtom?.Invoke(atom);
        }

        protected void OnQuestionText(string text, IMedia sound)
        {
            QuestionText?.Invoke(text, sound);
        }

        protected void OnQuestionOral(string oralText)
        {
            QuestionOral?.Invoke(oralText);
        }

        protected void OnQuestionImage(IMedia image, IMedia sound)
        {
            QuestionImage?.Invoke(image, sound);
        }

        protected void OnQuestionSound(IMedia sound)
        {
            QuestionSound?.Invoke(sound);
        }

        protected void OnQuestionVideo(IMedia video)
        {
            QuestionVideo?.Invoke(video);
        }

        protected void OnQuestionOther(Atom atom)
        {
            QuestionOther?.Invoke(atom);
        }

        protected void OnQuestionProcessed(Question question, bool finished, bool pressMode)
        {
            QuestionProcessed?.Invoke(question, finished, pressMode);
        }

        protected void OnWaitTry(Question question, bool final = false)
        {
            WaitTry?.Invoke(question, final);
        }

        protected void OnSimpleAnswer(string answer)
        {
            SimpleAnswer?.Invoke(answer);
        }

        protected void OnRightAnswer()
        {
            RightAnswer?.Invoke();
        }

        protected void OnShowScore()
        {
            ShowScore?.Invoke();
        }

        protected void OnLogScore()
        {
            LogScore?.Invoke();
        }

        protected void OnEndQuestion(int themeIndex, int questionIndex)
        {
            EndQuestion?.Invoke(themeIndex, questionIndex);
        }

        protected void OnRoundEmpty()
        {
            RoundEmpty?.Invoke();
        }

        protected void OnNextQuestion()
        {
            NextQuestion?.Invoke();
        }

        protected void OnRoundTimeout()
        {
            RoundTimeout?.Invoke();
        }

        protected void OnFinalThemes(Theme[] finalThemes)
        {
            FinalThemes?.Invoke(finalThemes);
        }

        protected void OnWaitDelete()
        {
            WaitDelete?.Invoke();
        }

        protected void OnThemeSelected(int themeIndex)
        {
            ThemeSelected?.Invoke(themeIndex);
        }

        protected void OnPrepareFinalQuestion(Theme theme, Question question)
        {
            PrepareFinalQuestion?.Invoke(theme, question);
        }

        protected void OnSound(string name = "")
        {
            Sound?.Invoke(name);
        }

        protected void OnError(string error)
        {
            Error?.Invoke(error);
        }

        protected void OnEndGame()
        {
            EndGame?.Invoke();
        }

        #endregion

        public object SyncRoot { get; } = new object();

        /// <summary>
        /// Автоматический шаг дальше
        /// </summary>
        /// <param name="milliseconds"></param>
        protected async void AutoNext(int milliseconds)
        {
            if (_settingsProvider.AutomaticGame)
            {
                lock (_autoLock)
                {
                    if (_inAutoMode)
                        return;

                    _inAutoMode = true;
                }

                await Task.Delay(milliseconds);

                lock (_autoLock)
                {
                    _inAutoMode = false;

                    if (_document == null)
                        return;

                    AutoNextCore();

                    lock (SyncRoot)
                    {
                        MoveNext();
                    }
                }
            }
        }

        protected virtual void AutoNextCore() { }

        /// <summary>
        /// Перейти к следующему раунду
        /// </summary>
        /// <param name="showSign"></param>
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
            } while (_stage == GameStage.Round && (!_activeRound.Themes.Any(theme => theme.Questions.Any()) || !AcceptRound(_activeRound)));

            CanMoveNextRound = _roundIndex + 1 < _document.Package.Rounds.Count;
            CanMoveBackRound = _roundIndex > 0;

            UpdateCanNext();
            if (moved)
                OnNextRound(showSign);

            CanMoveBack = false;
            return moved;
        }

        protected abstract bool AcceptRound(Round round);

        public virtual void MoveBackRound()
        {
            if (_roundIndex > 0)
            {
                _roundIndex--;
                SetActiveRound();

                CanMoveNextRound = _roundIndex + 1 < _document.Package.Rounds.Count;
                CanMoveBackRound = _roundIndex > 0;

                Stage = GameStage.Round;
                UpdateCanNext();
            }

            CanMoveBack = false;
        }

        public abstract bool CanNext();

        public void UpdateCanNext()
        {
            CanMoveNext = CanNext();
        }

        protected void SetActiveRound()
        {
            _activeRound = _roundIndex < _document.Package.Rounds.Count ? _document.Package.Rounds[_roundIndex] : null;
        }

        public void SetTimeout()
        {
            _timeout = true;
        }

        public void SkipQuestion()
        {
            Stage = _activeRound.Type != RoundTypes.Final ? GameStage.EndQuestion : GameStage.AfterFinalThink;
        }

        /// <summary>
        /// Отобразить вопрос (его часть)
        /// </summary>
        /// <returns>JustFinished, если вопрос был показан полностью
        /// InProcess, если вопрос был показан не полностью,
        /// AlreadyFinished, если вопрос уже был показан ранее</returns>
        protected QuestionPlayMode PlayQuestionAtom()
        {
            if (_atomIndex == _activeQuestion.Scenario.Count)
                return QuestionPlayMode.AlreadyFinished;

            var activeAtom = ActiveAtom;
            _isMedia = false;
            switch (activeAtom.Type)
            {
                case AtomTypes.Text:
                    {
                        var text = CollectText();
                        var sound = GetBackgroundSound();
                        _isMedia = sound != null;

                        OnQuestionText(text, sound);

                        _atomIndex++;
                        break;
                    }

                case AtomTypes.Oral:
                    var oralText = CollectText(AtomTypes.Oral);
                    OnQuestionOral(oralText);

                    _atomIndex++;

                    break;

                case AtomTypes.Image:
                case AtomTypes.Audio:
                case AtomTypes.Video:
                    {
                        // Мультимедийное содержимое
                        var media = GetMedia();
                        if (media == null)
                            break;

                        var isSound = activeAtom.Type == AtomTypes.Audio;
                        var isImage = activeAtom.Type == AtomTypes.Image;

                        if (isImage)
                        {
                            var sound = GetBackgroundSound();
                            _isMedia = sound != null;

                            OnQuestionImage(media, sound);
                        }
                        else
                        {
                            if (isSound)
                            {
                                var backItem = GetBackgroundImageOrText();
                                if (backItem != null)
                                {
                                    if (backItem.Item1 != null)
                                        OnQuestionImage(backItem.Item1, media);
                                    else
                                        OnQuestionText(backItem.Item2, media);
                                }
                                else
                                    OnQuestionSound(media);
                            }
                            else
                            {
                                OnQuestionVideo(media);
                            }

                            _isMedia = true;
                        }

                        _atomIndex++;
                        break;
                    }

                case AtomTypes.Marker:
                    _atomIndex++;
                    if (_atomIndex < _activeQuestion.Scenario.Count)
                        _useAnswerMarker = true; // Прерываем отыгрыш вопроса: остальное - ответ

                    return QuestionPlayMode.AlreadyFinished;

                default:
                    OnQuestionOther(activeAtom);
                    _atomIndex++; // Прочие типы не выводятся
                    break;
            }

            OnQuestionAtom(activeAtom);

            if (_atomIndex == _activeQuestion.Scenario.Count)
                return QuestionPlayMode.JustFinished;

            if (ActiveAtom.Type == AtomTypes.Marker)
            {
                if (_atomIndex + 1 < _activeQuestion.Scenario.Count)
                    _useAnswerMarker = true;

                return QuestionPlayMode.JustFinished;
            }

            return QuestionPlayMode.InProcess;
        }

        /// <summary>
        /// Собрать текст последовательно расположенных элементов вопроса
        /// </summary>
        /// <returns>Собранный текст</returns>
        private string CollectText(string atomType = AtomTypes.Text)
        {
            var text = new StringBuilder();
            while (ActiveAtom != null && ActiveAtom.Type == atomType)
            {
                if (text.Length > 0)
                    text.AppendLine();

                text.Append(ActiveAtom.Text);

                _atomIndex++;
            }

            _atomIndex--;

            return text.ToString();
        }

        /// <summary>
        /// Задать дополнительный звук к текущему элементу
        /// </summary>
        /// <returns></returns>
        private IMedia GetBackgroundSound()
        {
            if (ActiveAtom != null && ActiveAtom.AtomTime == -1 && _atomIndex + 1 < _activeQuestion.Scenario.Count) // Объединить со следующим
            {
                var nextAtom = _activeQuestion.Scenario[_atomIndex + 1];
                if (nextAtom.Type == AtomTypes.Audio)
                {
                    _atomIndex++;
                    return GetMedia();
                }
            }

            return null;
        }

        /// <summary>
        /// Задать дополнительные изображение или текст к текущему элементу
        /// </summary>
        /// <returns></returns>
        private Tuple<IMedia, string> GetBackgroundImageOrText()
        {
            if (ActiveAtom != null && ActiveAtom.AtomTime == -1 && _atomIndex + 1 < _activeQuestion.Scenario.Count) // Объединить со следующим
            {
                var nextAtom = _activeQuestion.Scenario[_atomIndex + 1];
                if (nextAtom.Type == AtomTypes.Image)
                {
                    _atomIndex++;

                    var media = GetMedia();
                    if (media != null)
                        return Tuple.Create(media, (string)null);
                }
                else if (nextAtom.Type == AtomTypes.Text)
                {
                    _atomIndex++;
                    return Tuple.Create((IMedia)null, CollectText());
                }
            }

            return null;
        }

        private IMedia GetMedia()
        {
            try
            {
                return _document.GetLink(ActiveAtom);
            }
            catch (Exception exc)
            {
                OnError(string.Format("При попытке обнаружить ссылку на медиа файл \"{0}\" обнаружена ошибка: {1}", ActiveAtom.Text, exc.Message));
                _atomIndex++;
                return null;
            }
        }

        /// <summary>
        /// Завершить раунд сразу либо сначала показать счёт
        /// </summary>
        protected void DoFinishRound()
        {
            OnLogScore();
            if (_settingsProvider.ShowScore)
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

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void Dispose()
        {
            lock (_autoLock)
            {
                if (_document != null)
                {
                    _document.Dispose();
                    _document = null;
                }
            }
        }
    }
}

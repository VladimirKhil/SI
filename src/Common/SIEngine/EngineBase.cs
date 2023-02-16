using SIEngine.Core;
using SIPackages;
using SIPackages.Core;
using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SIEngine;

/// <inheritdoc cref="ISIEngine" />
public abstract class EngineBase : ISIEngine, IDisposable, INotifyPropertyChanged
{
    private bool _isDisposed = false;

    protected Func<EngineOptions> OptionsProvider { get; }

    private readonly CancellationTokenSource _cancellationTokenSource = new();

    protected GameStage _stage = GameStage.Begin;

    /// <summary>
    /// Стадия игры
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

    protected readonly SIDocument _document;

    public SIDocument Document => _document;

    protected int _roundIndex = -1, _themeIndex = 0, _questionIndex = 0, _atomIndex = 0;

    protected bool _isMedia;

    protected Round? _activeRound;

    protected Theme _activeTheme;

    protected Question? _activeQuestion;

    protected bool _inAutoMode;

    protected object _autoLock = new();

    protected bool _timeout = false;

    protected bool _useAnswerMarker = false;

    private bool _canMoveNext = true;

    private bool _canMoveBack;

    private bool _canMoveNextRound;

    private bool _canMoveBackRound;

    public string PackageName => _document.Package.Name;

    public string ContactUri => _document.Package.ContactUri;

    public int RoundIndex => _roundIndex;

    public int ThemeIndex => _themeIndex;

    public int QuestionIndex => _questionIndex;

    private Atom? ActiveAtom => _atomIndex < _activeQuestion.Scenario.Count ? _activeQuestion.Scenario[_atomIndex] : null;

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
        || _stage == GameStage.AfterFinalThink
        || _stage == GameStage.QuestionPostInfo
        || _stage == GameStage.RightAnswer;

    public bool CanReturnToQuestion() => _activeRound != null
        && _activeRound.Type != RoundTypes.Final
        && _activeQuestion != null && _activeQuestion.Type.Name == QuestionTypes.Simple;

    public bool CanPress() =>
        _activeQuestion.Type.Name == QuestionTypes.Simple
        && (_stage == GameStage.RightAnswer || _stage == GameStage.QuestionPostInfo);

    public bool IsQuestion() => _stage == GameStage.Question;

    public bool IsWaitingForPress() =>
        _stage == GameStage.Question
        || _stage == GameStage.RightAnswer
        || _stage == GameStage.QuestionPostInfo;

    public bool IsQuestionFinished() => _activeQuestion != null && _atomIndex == _activeQuestion.Scenario.Count;

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

    public event Action<Package, IMedia> Package;
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
    public event Action<IMedia> QuestionHtml;
    public event Action<Atom> QuestionOther;
    public event Action<Question, bool, bool> QuestionProcessed;
    public event Action QuestionFinished;
    public event Action<Question, bool> WaitTry;
    public event Action<string> SimpleAnswer;
    public event Action RightAnswer;
    public event Action QuestionPostInfo;

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

    [Obsolete]
    public event Action<string> Sound;

    public event Action<string> Error;

    public event Action EndGame;

    #endregion

    protected EngineBase(SIDocument document, Func<EngineOptions> optionsProvider, QuestionEngineFactory questionEngineFactory)
    {
        _document = document ?? throw new ArgumentNullException(nameof(document));
        OptionsProvider = optionsProvider ?? throw new ArgumentNullException(nameof(optionsProvider));
        _questionEngineFactory = questionEngineFactory;
    }

    public abstract void MoveNext();

    public abstract Tuple<int, int, int> MoveBack();

    #region Fire events

    protected void OnPackage(Package package, IMedia packageLogo) => Package?.Invoke(package, packageLogo);

    protected void OnGameThemes(string[] gameThemes) => GameThemes?.Invoke(gameThemes);

    protected void OnNextRound(bool showSign = true) => NextRound?.Invoke(showSign);

    protected void OnRound(Round round) => Round?.Invoke(round);

    protected void OnRoundThemes(Theme[] roundThemes) => RoundThemes?.Invoke(roundThemes);

    protected void OnTheme(Theme theme) => Theme?.Invoke(theme);

    protected void OnQuestion(Question question) => Question?.Invoke(question);

    protected void OnQuestionSelected(int themeIndex, int questionIndex, Theme theme, Question question) =>
        QuestionSelected?.Invoke(themeIndex, questionIndex, theme, question);

    protected void OnQuestionAtom(Atom atom) => QuestionAtom?.Invoke(atom);

    protected void OnQuestionText(string text, IMedia sound) => QuestionText?.Invoke(text, sound);

    protected void OnQuestionOral(string oralText) => QuestionOral?.Invoke(oralText);

    protected void OnQuestionImage(IMedia image, IMedia sound) => QuestionImage?.Invoke(image, sound);

    protected void OnQuestionSound(IMedia sound) => QuestionSound?.Invoke(sound);

    protected void OnQuestionVideo(IMedia video) => QuestionVideo?.Invoke(video);

    protected void OnQuestionHtml(IMedia html) => QuestionHtml?.Invoke(html);

    protected void OnQuestionOther(Atom atom) => QuestionOther?.Invoke(atom);

    protected void OnQuestionProcessed(Question question, bool finished, bool pressMode) => QuestionProcessed?.Invoke(question, finished, pressMode);

    protected void OnQuestionFinished() => QuestionFinished?.Invoke();

    protected void OnWaitTry(Question question, bool final = false) => WaitTry?.Invoke(question, final);

    protected void OnSimpleAnswer(string answer) => SimpleAnswer?.Invoke(answer);

    protected void OnRightAnswer() => RightAnswer?.Invoke();

    protected void OnQuestionPostInfo() => QuestionPostInfo?.Invoke();

    protected void OnShowScore() => ShowScore?.Invoke();

    protected void OnLogScore() => LogScore?.Invoke();

    protected void OnEndQuestion(int themeIndex, int questionIndex) => EndQuestion?.Invoke(themeIndex, questionIndex);

    protected void OnRoundEmpty() => RoundEmpty?.Invoke();

    protected void OnNextQuestion() => NextQuestion?.Invoke();

    protected void OnRoundTimeout() => RoundTimeout?.Invoke();

    protected void OnFinalThemes(Theme[] finalThemes) => FinalThemes?.Invoke(finalThemes);

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
    /// Number of unanswered questions in the round.
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

        CanMoveNextRound = _roundIndex + 1 < _document.Package.Rounds.Count;
        CanMoveBackRound = _roundIndex > 0;

        UpdateCanNext();

        if (moved)
        {
            OnNextRound(showSign);
        }

        CanMoveBack = false;
        return moved;
    }

    public virtual bool MoveToRound(int roundIndex, bool showSign = true)
    {
        if (_roundIndex == roundIndex ||
            roundIndex < 0 ||
            roundIndex >= _document.Package.Rounds.Count ||
            !AcceptRound(_document.Package.Rounds[roundIndex]))
        {
            return false;
        }

        _roundIndex = roundIndex;
        SetActiveRound();
        Stage = GameStage.Round;

        CanMoveNextRound = _roundIndex + 1 < _document.Package.Rounds.Count;
        CanMoveBackRound = _roundIndex > 0;

        UpdateCanNext();
        OnNextRound(showSign);

        CanMoveBack = false;
        return true;
    }

    public virtual bool AcceptRound(Round? round) =>
        round != null && round.Themes.Any(theme => theme.Questions.Any(q => q.Price != SIPackages.Question.InvalidPrice));

    public virtual bool MoveBackRound()
    {
        if (!CanMoveBackRound)
        {
            return false;
        }

        var moved = true;
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

        CanMoveNextRound = _roundIndex + 1 < _document.Package.Rounds.Count;
        CanMoveBackRound = _roundIndex > 0;
        UpdateCanNext();

        CanMoveBack = false;
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
    /// Skips rest of the question and goes directly to the answer.
    /// </summary>
    public void MoveToAnswer()
    {
        if (Stage == GameStage.Question && _atomIndex < _activeQuestion.Scenario.Count)
        {
            do
            {
                if (ActiveAtom.Type == AtomTypes.Marker)
                {
                    _atomIndex++;
                    if (_atomIndex < _activeQuestion.Scenario.Count)
                    {
                        _useAnswerMarker = true;
                    }

                    break;
                }

                _atomIndex++;
            } while (_atomIndex < _activeQuestion.Scenario.Count);
        }

        SetAnswerStage();
    }

    private void SetAnswerStage()
    {
        Stage = OptionsProvider().ShowRight || _useAnswerMarker ? GameStage.RightAnswer : GameStage.QuestionPostInfo;
    }

    protected void OnQuestion()
    {
        if (QuestionEngine != null)
        {
            if (!QuestionEngine.PlayNext())
            {
                OnQuestionFinished();
                Stage = GameStage.QuestionPostInfo;
                MoveNext();
            }

            return;
        }

        var playMode = PlayQuestionAtom();
        var options = OptionsProvider();
        var pressMode = _isMedia ? options.IsMultimediaPressMode : options.IsPressMode;

        if (playMode == QuestionPlayMode.AlreadyFinished)
        {
            OnQuestionFinished();
            SetAnswerStage();

            if (pressMode)
            {
                OnWaitTry(_activeQuestion);
                AutoNext(1000 * Math.Min(5, options.ThinkingTime));
            }
            else
            {
                MoveNext();
            }
        }
        else
        {
            OnQuestionProcessed(_activeQuestion, playMode == QuestionPlayMode.JustFinished, pressMode);
            AutoNext(1000 * (Math.Min(1, options.ThinkingTime) + _activeQuestion.Scenario.ToString().Length / 20));
        }
    }

    /// <summary>
    /// Отобразить вопрос (его часть)
    /// </summary>
    /// <returns>
    /// <see cref="QuestionPlayMode.JustFinished" />, если вопрос был показан полностью
    /// <see cref="QuestionPlayMode.InProcess" />, если вопрос был показан не полностью,
    /// <see cref="QuestionPlayMode.AlreadyFinished" />, если вопрос уже был показан ранее
    /// </returns>
    protected QuestionPlayMode PlayQuestionAtom()
    {
        if (_atomIndex == _activeQuestion.Scenario.Count)
        {
            return QuestionPlayMode.AlreadyFinished;
        }

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
                    // Multimedia content
                    var media = GetMedia();

                    if (media == null)
                    {
                        break;
                    }

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
                                {
                                    OnQuestionImage(backItem.Item1, media);
                                }
                                else
                                {
                                    OnQuestionText(backItem.Item2, media);
                                }
                            }
                            else
                            {
                                OnQuestionSound(media);
                            }
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

            case AtomTypes.Html:
                var html = GetMedia();

                if (html == null)
                {
                    break;
                }

                OnQuestionHtml(html);

                _atomIndex++;
                break;

            case AtomTypes.Marker:
                _atomIndex++;

                if (_atomIndex < _activeQuestion.Scenario.Count)
                {
                    _useAnswerMarker = true; // Прерываем отыгрыш вопроса: остальное - ответ
                }

                return QuestionPlayMode.AlreadyFinished;

            default:
                OnQuestionOther(activeAtom);
                _atomIndex++; // Прочие типы не выводятся
                break;
        }

        OnQuestionAtom(activeAtom);

        if (_atomIndex == _activeQuestion.Scenario.Count)
        {
            return QuestionPlayMode.JustFinished;
        }

        if (ActiveAtom.Type == AtomTypes.Marker)
        {
            if (_atomIndex + 1 < _activeQuestion.Scenario.Count)
            {
                _useAnswerMarker = true;
            }

            return QuestionPlayMode.JustFinished;
        }

        return QuestionPlayMode.InProcess;
    }

    protected void ProcessRightAnswer()
    {
        OnRightAnswer();

        if (!_useAnswerMarker)
        {
            OnSimpleAnswer(_activeQuestion.Right.FirstOrDefault() ?? " ");
        }
        else // Ответ находится в тексте вопроса
        {
            PlayQuestionAtom();
            Stage = GameStage.RightAnswerProceed;
            AutoNext(3000);
            return;
        }

        Stage = GameStage.QuestionPostInfo;
        AutoNext(3000);
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
            {
                text.AppendLine();
            }

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
    private Tuple<IMedia?, string?> GetBackgroundImageOrText()
    {
        if (ActiveAtom != null && ActiveAtom.AtomTime == -1 && _atomIndex + 1 < _activeQuestion.Scenario.Count) // Объединить со следующим
        {
            var nextAtom = _activeQuestion.Scenario[_atomIndex + 1];
            if (nextAtom.Type == AtomTypes.Image)
            {
                _atomIndex++;

                var media = GetMedia();
                if (media != null)
                    return Tuple.Create((IMedia?)media, (string?)null);
            }
            else if (nextAtom.Type == AtomTypes.Text)
            {
                _atomIndex++;
                return Tuple.Create((IMedia?)null, (string?)CollectText());
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
    protected void OnMoveToQuestion(bool isFinal = false)
    {
        Stage = isFinal ? GameStage.FinalQuestion : GameStage.Question;

        if (OptionsProvider().UseNewEngine && _activeQuestion != null)
        {
            QuestionEngine = _questionEngineFactory.CreateEngine(_activeQuestion, isFinal);
        }
    }

    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public event PropertyChangedEventHandler PropertyChanged;

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

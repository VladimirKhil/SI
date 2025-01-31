using SIPackages;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SIEngine;

// TODO: remove this class

public abstract class EngineBase : IDisposable, INotifyPropertyChanged
{
    private bool _isDisposed = false;

    protected readonly SIDocument _document;

    // TODO: hide
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

    // TODO: investigate and eliminate duplicates
    public event Action<int, int>? EndQuestion;
    public event Action? QuestionFinish;
    public event Action? NextQuestion;
    // TODO: end

    #endregion

    protected EngineBase(SIDocument document)
    {
        _document = document ?? throw new ArgumentNullException(nameof(document));
    }

    #region Fire events

    protected void OnQuestionFinish() => QuestionFinish?.Invoke();

    protected void OnEndQuestion(int themeIndex, int questionIndex) => EndQuestion?.Invoke(themeIndex, questionIndex);

    protected void OnNextQuestion() => NextQuestion?.Invoke();

    #endregion

    protected void UpdateCanMoveNextRound() => CanMoveNextRound = _roundIndex < _document.Package.Rounds.Count;

    protected void UpdateCanMoveBackRound() => CanMoveBackRound = _roundIndex > 0 || CanMoveBack;

    public abstract bool CanNext();

    public void UpdateCanNext() => CanMoveNext = CanNext();

    protected void SetActiveRound() => _activeRound = _roundIndex < _document.Package.Rounds.Count ? _document.Package.Rounds[_roundIndex] : null;

    public void SetTimeout() => _timeout = true;

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

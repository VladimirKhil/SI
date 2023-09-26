using SIEngine;
using SImulator.ViewModel.Contracts;
using System.Windows.Input;

namespace SImulator.ViewModel.Listeners;

/// <inheritdoc cref="IExtendedListener" />
public sealed class PresentationListener : IExtendedListener
{
    private readonly ISIEngine _engine;

    public bool IsMediaEnded { get; set; }

    public ICommand Next { get; set; }

    public ICommand Back { get; set; }

    public ICommand NextRound { get; set; }

    public ICommand PreviousRound { get; set; }

    public ICommand Stop { get; set; }

    public event Action? MediaStart;

    public event Action? MediaEnd;

    public event Action<double>? MediaProgress;

    public event Action? RoundThemesFinished;

    public event Action<int>? AnswerSelected;

    public PresentationListener(ISIEngine engine) => _engine = engine;

    public void OnQuestionSelected(int theme, int question) => _engine.SelectQuestion(theme, question);

    public void OnThemeSelected(int themeIndex)
    {
        _engine.SelectTheme(themeIndex);
        _engine.OnReady(out _);
    }

    public void OnAnswerSelected(int answerIndex) => AnswerSelected?.Invoke(answerIndex);

    private readonly object _moveLock = new();

    public void AskNext()
    {
        lock (_moveLock)
        {
            if (Next?.CanExecute(null) == true)
            {
                Next.Execute(null);
            }
        }
    }

    public void AskBack()
    {
        lock (_moveLock)
        {
            if (_engine.CanMoveBack)
            {
                Back?.Execute(null);
            }
        }
    }

    public void AskNextRound()
    {
        lock (_moveLock)
        {
            if (NextRound?.CanExecute(null) == true)
            {
                NextRound.Execute(null);
            }
        }
    }

    public void AskBackRound()
    {
        lock (_moveLock)
        {
            if (PreviousRound?.CanExecute(null) == true)
            {
                PreviousRound.Execute(null);
            }
        }
    }

    public void AskStop()
    {
        if (Stop?.CanExecute(null) == true)
        {
            Stop.Execute(null);
        }
    }

    public void OnMediaStart()
    {
        MediaStart?.Invoke();
        IsMediaEnded = false;
    }

    public void OnMediaEnd()
    {
        if (_engine.IsIntro())
        {
            return;
        }

        IsMediaEnded = true;
        MediaEnd?.Invoke();
    }

    public void OnMediaProgress(double progress) => MediaProgress?.Invoke(progress);

    public void OnIntroFinished() => _engine.OnIntroFinished();

    public void OnRoundThemesFinished() => RoundThemesFinished?.Invoke();
}

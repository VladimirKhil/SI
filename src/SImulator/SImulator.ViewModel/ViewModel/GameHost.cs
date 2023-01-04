using SIEngine;
using SImulator.ViewModel.Core;
using System.Diagnostics;
using System.Windows.Input;

namespace SImulator.ViewModel;

public sealed class GameHost : IExtendedGameHost
{
    private readonly ISIEngine _engine;

    public bool IsMediaEnded { get; set; }

    public ICommand Next { get; set; }

    public ICommand Back { get; set; }

    public ICommand NextRound { get; set; }

    public ICommand PreviousRound { get; set; }

    public ICommand Stop { get; set; }

    public event Action<int> ThemeDeleted;

    public event Action MediaStart;

    public event Action MediaEnd;

    public event Action<double> MediaProgress;

    public event Action RoundThemesFinished;

    public GameHost(ISIEngine engine) => _engine = engine;

    public async void OnQuestionSelected(int theme, int question)
    {
        try
        {
            ((TvEngine)_engine).SelectQuestion(theme, question);
            await Task.Delay(700);
            ((TvEngine)_engine).OnReady(out _);
        }
        catch (Exception exc)
        {
            Trace.TraceError("OnQuestionSelected error: " + exc.Message);
        }
    }

    public void OnThemeSelected(int themeIndex)
    {
        ((TvEngine)_engine).SelectTheme(themeIndex);
        ((TvEngine)_engine).OnReady(out _);
    }

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

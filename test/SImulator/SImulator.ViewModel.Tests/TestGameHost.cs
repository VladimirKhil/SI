using SIEngine;
using SImulator.ViewModel.Contracts;
using System.Windows.Input;

namespace SImulator.ViewModel.Tests;

internal sealed class TestGameHost : IExtendedListener
{
    private readonly EngineBase _engine;

    public bool IsMediaEnded { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public ICommand Next { get => throw new NotImplementedException(); set { } }
    public ICommand Back { get => throw new NotImplementedException(); set { } }
    public ICommand NextRound { get => throw new NotImplementedException(); set { } }
    public ICommand PreviousRound { get => throw new NotImplementedException(); set { } }
    public ICommand Stop { get => throw new NotImplementedException(); set { } }

    public event Action MediaStart { add { } remove { } }
    public event Action MediaEnd { add { } remove { } }
    public event Action<double> MediaProgress { add { } remove { } }
    public event Action RoundThemesFinished { add { } remove { } }
    public event Action<int>? AnswerSelected;

    public TestGameHost(EngineBase engine)
    {
        _engine = engine;
    }

    public void AskBack()
    {
        throw new NotImplementedException();
    }

    public void AskBackRound()
    {
        throw new NotImplementedException();
    }

    public void AskNext()
    {
        throw new NotImplementedException();
    }

    public void AskNextRound()
    {
        throw new NotImplementedException();
    }

    public void AskStop()
    {
        throw new NotImplementedException();
    }

    public void OnIntroFinished()
    {
        throw new NotImplementedException();
    }

    public void OnMediaEnd()
    {
        throw new NotImplementedException();
    }

    public void OnMediaProgress(double progress)
    {
        throw new NotImplementedException();
    }

    public void OnMediaStart()
    {
        throw new NotImplementedException();
    }

    public void OnQuestionSelected(int theme, int question)
    {
        ((TvEngine)_engine).SelectQuestion(theme, question);
    }

    public void OnReady()
    {
        throw new NotImplementedException();
    }

    public void OnRoundThemesFinished()
    {
        throw new NotImplementedException();
    }

    public void OnThemeSelected(int themeIndex)
    {
        throw new NotImplementedException();
    }

    public void OnAnswerSelected(int answerIndex)
    {
        throw new NotImplementedException();
    }
}

﻿using SImulator.ViewModel.Contracts;
using System.Windows.Input;

namespace SImulator.ViewModel.Listeners;

/// <inheritdoc cref="IExtendedListener" />
public sealed class PresentationListener : IExtendedListener
{
    public bool IsMediaEnded { get; set; }

    public ICommand? Next { get; set; }

    public ICommand? Back { get; set; }

    public ICommand? NextRound { get; set; }

    public ICommand? PreviousRound { get; set; }

    public ICommand? Stop { get; set; }

    public event Action? MediaStart;

    public event Action? MediaEnd;

    public event Action<double>? MediaProgress;

    public event Action? RoundThemesFinished;

    public event Action<int>? AnswerSelected;

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
            if (Back?.CanExecute(null) == true)
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
        IsMediaEnded = true;
        MediaEnd?.Invoke();
    }

    public void OnMediaProgress(double progress) => MediaProgress?.Invoke(progress);

    public void OnRoundThemesFinished() => RoundThemesFinished?.Invoke();
}

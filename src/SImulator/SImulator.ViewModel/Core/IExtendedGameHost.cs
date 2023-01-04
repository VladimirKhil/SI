using System.Windows.Input;

namespace SImulator.ViewModel.Core;

public interface IExtendedGameHost : IGameHost
{
    bool IsMediaEnded { get; set; }

    ICommand Next { get; set; }

    ICommand Back { get; set; }

    ICommand NextRound { get; set; }

    ICommand PreviousRound { get; set; }

    ICommand Stop { get; set; }

    event Action MediaStart;

    event Action MediaEnd;

    event Action<double> MediaProgress;

    event Action RoundThemesFinished;
}

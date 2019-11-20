using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace SImulator.ViewModel.Core
{
    public interface IExtendedGameHost: IGameHost
    {
		bool IsMediaEnded { get; set; }

		ICommand Next { get; set; }
		ICommand Back { get; set; }
		ICommand NextRound { get; set; }
		ICommand PreviousRound { get; set; }

		ICommand Stop { get; set; }

		event Action<int> ThemeDeleted;
		event Action MediaStart;
		event Action MediaEnd;
		event Action<double> MediaProgress;
		event Action RoundThemesFinished;
	}
}

using System;

namespace SIEngine
{
    public interface IEngineSettingsProvider
    {
		bool IsPressMode(bool isMultimediaQuestion);
        bool ShowRight { get; }
		bool ShowScore { get; }
		bool AutomaticGame { get; }
		bool PlaySpecials { get; }
		int ThinkingTime { get; }
    }
}

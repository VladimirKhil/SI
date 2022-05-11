namespace SIEngine
{
    /// <summary>
    /// Provides <see cref="ISIEngine" /> settings allowing them to be changed during the play.
    /// </summary>
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

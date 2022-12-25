using SIEngine;

namespace SICore;

internal sealed class DefaultEngineSettingsProvider : IEngineSettingsProvider
{
    internal static DefaultEngineSettingsProvider Instance = new();

    private DefaultEngineSettingsProvider() { }

    public bool IsPressMode(bool isMultimediaQuestion) => true;

    public bool ShowRight => true;

    public bool ShowScore => false;

    public bool AutomaticGame => false;

    public bool PlaySpecials => true;

    public int ThinkingTime => 0;

    public object WorkingLock { get; } = new object();
}

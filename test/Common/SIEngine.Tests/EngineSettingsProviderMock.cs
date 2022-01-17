namespace SIEngine.Tests
{
    internal sealed class EngineSettingsProviderMock : IEngineSettingsProvider
    {
        public bool ShowRight => true;

        public bool ShowScore => false;

        public bool AutomaticGame => false;

        public bool PlaySpecials => true;

        public int ThinkingTime => 3;

        public bool IsPressMode(bool isMultimediaQuestion) => true;
    }
}

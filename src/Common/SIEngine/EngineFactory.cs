using SIPackages;

namespace SIEngine
{
    /// <summary>
    /// Provides a method for creating SIGame engine.
    /// </summary>
    public static class EngineFactory
    {
        /// <summary>
        /// Creates the engine.
        /// </summary>
        /// <param name="classical">Should the engine be classical (or simple otherwise).</param>
        /// <param name="document">SIGame package to play.</param>
        /// <param name="settingsProvider">Settings provider.</param>
        /// <returns>Created engine.</returns>
        public static ISIEngine CreateEngine(bool classical, SIDocument document, IEngineSettingsProvider settingsProvider) =>
            classical
                ? (ISIEngine)new TvEngine(document, settingsProvider)
                : new SportEngine(document, settingsProvider);
    }
}

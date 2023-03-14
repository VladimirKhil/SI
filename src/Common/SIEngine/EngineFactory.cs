using SIEngine.Core;
using SIPackages;

namespace SIEngine;

// TODO: Merge two engines into one and provide different strategies for its question selection logic

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
    /// <param name="optionsProvider">Options provider.</param>
    /// <param name="playHandler">Question engine play handler.</param>
    /// <returns>Created engine.</returns>
    public static ISIEngine CreateEngine(
        bool classical,
        SIDocument document,
        Func<EngineOptions> optionsProvider,
        IQuestionEnginePlayHandler playHandler) =>
        classical
            ? new TvEngine(document, optionsProvider, new QuestionEngineFactory(playHandler))
            : new SportEngine(document, optionsProvider, new QuestionEngineFactory(playHandler));
}

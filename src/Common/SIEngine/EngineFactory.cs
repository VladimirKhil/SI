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
    /// <param name="questionPlayHandler">Question engine play handler.</param>
    /// <returns>Created engine.</returns>
    public static ISIEngine CreateEngine(
        bool classical,
        SIDocument document,
        Func<EngineOptions> optionsProvider,
        ISIEnginePlayHandler playHandler,
        IQuestionEnginePlayHandler questionPlayHandler) =>
        classical
            ? new TvEngine(document, optionsProvider, playHandler, new QuestionEngineFactory(questionPlayHandler))
            : new SportEngine(document, optionsProvider, playHandler, new QuestionEngineFactory(questionPlayHandler));
}

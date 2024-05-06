using SIEngine.Core;
using SIEngine.Rules;
using SIPackages;

namespace SIEngine;

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
        new GameEngine(
            document,
            classical ? WellKnownGameRules.Classic : WellKnownGameRules.Simple,
            optionsProvider,
            playHandler,
            new QuestionEngineFactory(questionPlayHandler));
}

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
    /// <param name="gameRules">Game rules.</param>
    /// <param name="document">SIGame package to play.</param>
    /// <param name="optionsProvider">Options provider.</param>
    /// <param name="questionPlayHandler">Question engine play handler.</param>
    /// <returns>Created engine.</returns>
    public static GameEngine CreateEngine(
        GameRules gameRules,
        SIDocument document,
        Func<EngineOptions> optionsProvider,
        ISIEnginePlayHandler playHandler,
        IQuestionEnginePlayHandler questionPlayHandler) =>
        new(
            document,
            gameRules,
            optionsProvider,
            playHandler,
            new QuestionEngineFactory(questionPlayHandler));
}
